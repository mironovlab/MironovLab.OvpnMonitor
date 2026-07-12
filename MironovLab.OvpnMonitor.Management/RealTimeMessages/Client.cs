using System.Collections.Generic;
using System.Net;
using MironovLab.OpenVPN.Management.Common;

namespace MironovLab.OpenVPN.Management.RealTimeMessages
{
    public class Client : RealTimeMessage
    {
        public override RealTimeMessageType Type => RealTimeMessageType.Client;
        public ClientNotificationType NotificationType { get; }
        public int ClientID { get; }
        public int? KeyID { get; }
        public IPNetwork2 VirtualAddress { get; }
        public bool? Primary { get; }
        public IReadOnlyDictionary<string, string> EnvironmentalVariables { get; }

        internal Client(ClientNotificationType notificationType, int clientId, int? keyId, IPNetwork2 virtualAddress, bool? primary, IReadOnlyDictionary<string, string> environmentalVariables)
        {
            NotificationType = notificationType;
            ClientID = clientId;
            KeyID = keyId;
            VirtualAddress = virtualAddress;
            Primary = primary;
            EnvironmentalVariables = environmentalVariables;
        }

        public EnvReader GetEnvironmentVariablesReader()
        {
            return new EnvReader(EnvironmentalVariables);
        }
    }

    public enum ClientNotificationType
    {
        Undefined,
        Connect,
        ReAuth,
        Established,
        Disconnect,
        Address,
    }
}
