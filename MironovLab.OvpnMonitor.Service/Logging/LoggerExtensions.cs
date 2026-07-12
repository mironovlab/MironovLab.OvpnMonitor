using System;
using Microsoft.Extensions.Logging;
using MironovLab.OvpnMonitor.Service.Configuration;

namespace MironovLab.OvpnMonitor.Service.Logging
{
    public static class LoggerExtensions
    {
        public static ILogger<T> CreateLogger<T>(this ILoggerFactory factory, string hostName)
        {
            if (factory == null)
            {
                throw new ArgumentNullException(nameof(factory));
            }
            return new Logger<T>(factory, hostName);
        }

        public static ILogger<T> CreateLogger<T>(this ILoggerFactory factory, OpenVPNConfiguration config)
        {
            if (factory == null)
            {
                throw new ArgumentNullException(nameof(factory));
            }
            return new Logger<T>(factory, $"{config.HostAddress}:{config.Port:D}");
        }
    }
}
