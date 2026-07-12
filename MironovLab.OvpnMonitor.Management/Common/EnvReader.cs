using System;
using System.Collections.Generic;
using System.Net;
using MironovLab.OpenVPN.Management.Core;

namespace MironovLab.OpenVPN.Management.Common
{
    public class EnvReader
    {
        public DateTime Time { get; }
        public IPEndPoint RealAddress { get; }
        public IPAddress VirtualAddress { get; }
        public string CommonName { get; }
        public string ClientVersion { get; }
        public string ClientUIVersion { get; }
        public Platform Platform { get; }

        public EnvReader(IReadOnlyDictionary<string, string> envVariables)
        {
            if (envVariables.TryGetValue("time_unix", out var timeUnix))
                Time = ParseUtils.DateTimeFromUnixTime(timeUnix);

            if ((envVariables.TryGetValue("trusted_ip", out var trustedIp) ||
                 envVariables.TryGetValue("trusted_ip6", out trustedIp)) &&
                envVariables.TryGetValue("trusted_port", out var trustedPort))
                RealAddress = new IPEndPoint(IPAddress.Parse(trustedIp), int.Parse(trustedPort));

            if (envVariables.TryGetValue("ifconfig_pool_remote_ip", out var virtualAddress))
                VirtualAddress = IPAddress.Parse(virtualAddress);

            if (envVariables.TryGetValue("IV_VER", out var clientVersion))
                ClientVersion = clientVersion;


            if (envVariables.TryGetValue("IV_GUI_VER", out var guiVersion))
                ClientUIVersion = guiVersion;

            if (envVariables.TryGetValue("common_name", out var commonName))
                CommonName = commonName;

            if (envVariables.TryGetValue("IV_PLAT", out var platform))
            {
                switch (platform)
                {
                    case "linux":
                        Platform = Platform.Linux;
                        break;
                    case "solaris":
                        Platform = Platform.Solaris;
                        break;
                    case "openbsd":
                        Platform = Platform.OpenBSD;
                        break;
                    case "mac":
                        Platform = Platform.Mac;
                        break;
                    case "netbsd":
                        Platform = Platform.NetBSD;
                        break;
                    case "freebsd":
                        Platform = Platform.FreeBSD;
                        break;
                    case "win":
                        Platform = Platform.Windows;
                        break;
                    case "ios":
                        Platform = Platform.iOS;
                        break;
                    case "android":
                        Platform = Platform.Android;
                        break;
                }
            }
        }
    }
}
