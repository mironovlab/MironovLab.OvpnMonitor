using System;
using System.Collections.Generic;
using System.Net;

namespace MironovLab.OpenVPN.Management.CommandResults
{
    public class Status
    {
        public string Title { get; }
        public DateTime Time { get; }
        public IReadOnlyCollection<Client> Clients { get; }
        public IReadOnlyCollection<Route> RoutingTable { get; }
        public int MaxBcastMcastQueueLength { get; }

        internal Status(string title, DateTime time, IReadOnlyCollection<Client> clients, IReadOnlyCollection<Route> routingTable, int maxBcastMcastQueueLength)
        {
            Title = title;
            Time = time;
            Clients = clients;
            RoutingTable = routingTable;
            MaxBcastMcastQueueLength = maxBcastMcastQueueLength;
        }

        public class Client
        {
            public string CommonName { get; }
            public IPEndPoint RealAddress { get; }
            public IPAddress VirtualAddress { get; }
            public IPAddress VirtualIPv6Address { get; }
            public long BytesReceived { get; }
            public long BytesSent { get; }
            public DateTime ConnectedSince { get; }
            public string UserName { get; }
            public int ClientID { get; }
            public int PeerID { get; }
            public string DataChannelCipher { get; }

            internal Client(string commonName, IPEndPoint realAddress, IPAddress virtualAddress, IPAddress virtualIPv6Address, long bytesReceived, long bytesSent, DateTime connectedSince, string userName, int clientId, int peerId, string dataChannelCipher)
            {
                CommonName = commonName;
                RealAddress = realAddress;
                VirtualAddress = virtualAddress;
                VirtualIPv6Address = virtualIPv6Address;
                BytesReceived = bytesReceived;
                BytesSent = bytesSent;
                ConnectedSince = connectedSince;
                UserName = userName;
                ClientID = clientId;
                PeerID = peerId;
                DataChannelCipher = dataChannelCipher;
            }
        }

        public class Route
        {
            public string VirtualAddress { get; }
            public string CommonName { get; }
            public string RealAddress { get; }
            public DateTime LastRef { get; }

            internal Route(string virtualAddress, string commonName, string realAddress, DateTime lastRef)
            {
                VirtualAddress = virtualAddress;
                CommonName = commonName;
                RealAddress = realAddress;
                LastRef = lastRef;
            }
        }
    }
}
