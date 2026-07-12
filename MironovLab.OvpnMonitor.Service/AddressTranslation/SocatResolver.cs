using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MironovLab.OvpnMonitor.Service.Configuration;
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MironovLab.OvpnMonitor.Service.AddressTranslation
{
    public class SocatResolver : BackgroundService, IAddressResolver
    {
        private readonly ILogger<SocatResolver> _logger;
        private readonly string _socatModuleName;
        private readonly IPAddress _sourceIpAddress;
        private readonly TimeSpan _waitTimeout;
        private readonly MemoryCache _cache;
        private readonly ConcurrentDictionary<IPEndPoint, TaskCompletionSource<IPEndPoint>> _reqs = new ConcurrentDictionary<IPEndPoint, TaskCompletionSource<IPEndPoint>>();

        public SocatResolver(AddressTranslatorConfiguration configuration, ILogger<SocatResolver> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _socatModuleName = configuration.SocatModuleName;
            IPAddress.TryParse(configuration.SourceIPAddress, out _sourceIpAddress);
            _waitTimeout = configuration.WaitingTimeout;
            _cache = new MemoryCache(new MemoryCacheOptions());
            logger.LogInformation("Socat address resolving module is created");
        }

        public IPEndPoint Translate(IPEndPoint realAddress)
        {
            if (_sourceIpAddress != null && !Equals(realAddress.Address, _sourceIpAddress))
            {
                _logger.LogTrace("Real address is not an interesting one {0}", realAddress.Address);
                return realAddress;
            }

            if (TryTranslateFromCache(out var result))
                return result;

            var tcs = new TaskCompletionSource<IPEndPoint>();
            _reqs[realAddress] = tcs;
            using var cts = new CancellationTokenSource(_waitTimeout);
            cts.Token.Register(() =>
            {
                _logger.LogWarning("Could not find rule for endpoint {0}", realAddress);
                tcs.TrySetResult(null);
            });

            if (TryTranslateFromCache(out result))
                return result;

            result = tcs.Task.Result;
            if (result == null)
                _logger.LogDebug("Failed to translate endpoint {0}", realAddress);
            else
                _logger.LogInformation("Translated endpoint {0} to address {1}", realAddress, realAddress);
            return result;

            bool TryTranslateFromCache(out IPEndPoint result)
            {
                if (_cache.TryGetValue<TranslationEntry>(realAddress, out var rule))
                {
                    _logger.LogTrace("Address translation from cache: {0} -> {1}", realAddress, rule.Source);
                    result = rule.Source;
                    return true;
                }

                result = realAddress;
                return false;
            }
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await Task.Yield();
            while (true)
            {
                _logger.LogInformation("Starting process");
                using var process = new Process();
                try
                {
                    process.StartInfo = new ProcessStartInfo("journalctl", $"-f -u {_socatModuleName}")
                    {
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        StandardOutputEncoding = Encoding.UTF8,
                    };

                    if (!process.Start())
                    {
                        _logger.LogError("Could not start journalctl process!");
                        return;
                    }

                    using var stdout = process.StandardOutput;
                    while (!stdout.EndOfStream)
                    {
                        if (stoppingToken.IsCancellationRequested)
                            return;
                        try
                        {
                            var line = await WaitFor(stdout, "accepting UDP connection from");
                            var @params = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                            var src = IPEndPoint.Parse(@params.Last());
                            _logger.LogTrace("Read source endpoint: {0}", src);
                            line = await WaitFor(stdout, "forked off child process");
                            @params = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                            var psid = @params.Last();
                            _logger.LogTrace("Read forked process ID: {0}", psid);
                            line = await WaitFor(stdout, $"socat[{psid}] N successfully connected from local address");
                            @params = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                            var dst = IPEndPoint.Parse(@params.Last());
                            _logger.LogTrace("Read destination endpoint: {0}", dst);

                            var rule = new TranslationEntry(src, dst);
                            _logger.LogDebug("Adding rule to cache: {0}", rule);
                            _cache.Set(dst, rule);
                            if (_reqs.TryRemove(dst, out var req))
                            {
                                req.TrySetResult(src);
                                _logger.LogDebug("Translated: {0} -> {1}", dst, src);
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Failed to parse rule");
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error with working with journalctl process");
                    throw;
                }
                finally
                {
                    process.Kill();
                    process.WaitForExit();
                }
            }
        }

        private async Task<string> WaitFor(StreamReader stdout, string text)
        {
            while (true)
            {
                var line = await stdout.ReadLineAsync();
                if (line.Contains(text, StringComparison.OrdinalIgnoreCase))
                    return line;
                _logger.LogTrace("Read line (skipped): {0}", line);
            }
        }
    }
}
