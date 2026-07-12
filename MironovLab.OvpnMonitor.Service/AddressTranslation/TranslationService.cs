using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MironovLab.OvpnMonitor.Service.Configuration;
using MironovLab.OvpnMonitor.Service.Logging;

namespace MironovLab.OvpnMonitor.Service.AddressTranslation
{
    public class TranslationService : IHostedService, IDisposable, IAddressResolver
    {
        private readonly IAddressResolver _addressResolver;
        private readonly BackgroundService _service;

        public TranslationService(IOptions<AddressTranslatorConfiguration> configuration, ILoggerFactory loggerFactory)
        {
            var config = configuration.Value;
            var logger = loggerFactory.CreateLogger<TranslationService>(config.SourceIPAddress);
            logger.LogDebug("Translation method: {0}", config.Method);
            switch (config.Method)
            {
                case ResolvingMethod.None:
                    break;
                case ResolvingMethod.Conntrack:
                    var conntrack = new AddressTranslator(config, loggerFactory.CreateLogger<AddressTranslator>());
                    _addressResolver = conntrack;
                    _service = conntrack;
                    break;
                case ResolvingMethod.Socat:
                    var socat = new SocatResolver(config, loggerFactory.CreateLogger<SocatResolver>());
                    _addressResolver = socat;
                    _service = socat;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(config.Method), config.Method, null);
            }
        }

        public IPEndPoint Translate(IPEndPoint realAddress)
        {
            return _addressResolver == null
                ? realAddress
                : _addressResolver.Translate(realAddress);
        }

        public void Dispose()
        {
            _service?.Dispose();
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            return _service?.StartAsync(cancellationToken) ?? Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return _service?.StopAsync(cancellationToken) ?? Task.CompletedTask;
        }
    }
}
