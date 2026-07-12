using System;
using System.Net;

namespace MironovLab.OpenVPN.Management.Common
{
    public readonly struct StateRecord
    {
        public readonly DateTime DateTime;
        public readonly StateName StateName;
        public readonly string Description;
        public readonly IPAddress LocalAddress;
        public readonly IPEndPoint RemoteServer;
        public readonly IPEndPoint Listening;

        internal StateRecord(DateTime dateTime, StateName stateName, string description, IPAddress localAddress, IPEndPoint remoteServer, IPEndPoint listening)
        {
            DateTime = dateTime;
            StateName = stateName;
            Description = description;
            LocalAddress = localAddress;
            RemoteServer = remoteServer;
            Listening = listening;
        }
    }

    public enum StateName
    {
        Undefined,
        Connecting,
        Waiting,
        Authenticating,
        GettingConfiguration,
        AssigningIP,
        AddingRoutes,
        Connected,
        Reconnecting,
        Exiting,
        Resolving,
    }
}
