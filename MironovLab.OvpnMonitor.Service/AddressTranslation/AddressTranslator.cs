using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using MironovLab.OvpnMonitor.Service.Configuration;
using IPAddress = System.Net.IPAddress;

namespace MironovLab.OvpnMonitor.Service.AddressTranslation
{
    public class AddressTranslator : BackgroundService, IAddressResolver
    {
        private readonly ILogger<AddressTranslator> _logger;
        private readonly string _proto;
        private readonly int _localPort;
        private readonly IPEndPoint _target;
        private readonly TimeSpan _waitTimeout;
        private readonly MemoryCache _cache;
        private readonly ConcurrentDictionary<IPEndPoint, TaskCompletionSource<IPEndPoint>> _reqs = new ConcurrentDictionary<IPEndPoint, TaskCompletionSource<IPEndPoint>>();

        public AddressTranslator(AddressTranslatorConfiguration configuration, ILogger<AddressTranslator> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _proto = configuration.Proto;
            _localPort = configuration.LocalPort;
            _target = new IPEndPoint(IPAddress.Parse(configuration.TargetIPAddress), configuration.TargetPort);
            _waitTimeout = configuration.WaitingTimeout;
            _cache = new MemoryCache(new MemoryCacheOptions());
            logger.LogInformation("NAT address resolving module is created");
        }

        public IPEndPoint Translate(IPEndPoint realAddress)
        {
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

            return tcs.Task.Result;

            bool TryTranslateFromCache(out IPEndPoint result)
            {
                if (_cache.TryGetValue<TranslationRule>(realAddress, out var rule))
                {
                    _logger.LogTrace("Address translation: {0} -> {1}", realAddress, rule.Incoming.Source);
                    result = rule.Incoming.Source;
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
                    process.StartInfo = new ProcessStartInfo("conntrack", "-E -n")
                    {
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        StandardOutputEncoding = Encoding.UTF8,
                    };

                    if (!process.Start())
                    {
                        _logger.LogError("Could not start conntrack process!");
                        return;
                    }

                    using var stdout = process.StandardOutput;
                    while (!stdout.EndOfStream)
                    {
                        if (stoppingToken.IsCancellationRequested)
                            return;

                        var line = await stdout.ReadLineAsync();
                        _logger.LogTrace("NAT rule string read: {0}", line);
                        var @params = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                        if (string.Equals(@params[1], _proto, StringComparison.OrdinalIgnoreCase))
                        {
                            using var enumerator = @params
                                .Select(x => x.Split('='))
                                .Where(x => x.Length == 2)
                                .GetEnumerator();
                            if (Parse(line, enumerator, out var incomingEntry) &&
                                Parse(line, enumerator, out var outgoingEntry))
                            {
                                var rule = new TranslationRule(incomingEntry, outgoingEntry);
                                if (incomingEntry.Destination.Port == _localPort &&
                                    outgoingEntry.Source.Equals(_target))
                                {
                                    _logger.LogDebug("Adding rule to cache: {0}", rule);
                                    _cache.Set(outgoingEntry.Destination, rule);
                                    if (_reqs.TryRemove(outgoingEntry.Destination, out var req))
                                    {
                                        req.TrySetResult(rule.Incoming.Source);
                                        _logger.LogDebug("Translated: {0} -> {1}", outgoingEntry.Destination, incomingEntry.Source);
                                    }
                                }
                                else
                                {
                                    _logger.LogTrace("Rule {0} was parsed, but didn't pass conditions", rule);
                                }
                            }
                            else
                            {
                                _logger.LogDebug("Rule {0} was not parsed. Skipped.", line);
                            }
                        }
                        else
                        {
                            _logger.LogDebug("Rule {0} was skipped because of protocol: {1}", line, @params[1]);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error with working with conntrack process");
                    throw;
                }
                finally
                {
                    process.Kill();
                    process.WaitForExit();
                }
            }
        }

        private bool Parse(string line, IEnumerator<string[]> enumerator, out TranslationEntry entry)
        {
            entry = default;
            IPAddress srcAddr = null, dstAddr = null;
            int? srcPort = null, dstPort = null;
            while ((srcAddr == null || dstAddr == null || srcPort == null || dstPort == null) && enumerator.MoveNext())
            {
                var item = enumerator.Current;
                switch (item[0])
                {
                    case "src":
                        srcAddr = IPAddress.Parse(item[1]);
                        break;
                    case "dst":
                        dstAddr = IPAddress.Parse(item[1]);
                        break;
                    case "sport":
                        srcPort = int.Parse(item[1]);
                        break;
                    case "dport":
                        dstPort = int.Parse(item[1]);
                        break;
                    default:
                        _logger.LogWarning("Unexpected string format: {0}", line);
                        break;
                }
            }

            if (srcAddr != null && srcPort != null && dstAddr != null && dstPort != null)
            {
                entry = new TranslationEntry(new IPEndPoint(srcAddr, srcPort.Value), new IPEndPoint(dstAddr, dstPort.Value));
                return true;
            }
            return false;
        }
    }
}
