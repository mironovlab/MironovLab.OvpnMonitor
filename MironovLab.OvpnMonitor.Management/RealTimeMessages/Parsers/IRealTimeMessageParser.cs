using System;

namespace MironovLab.OpenVPN.Management.RealTimeMessages.Parsers
{
    internal interface IRealTimeMessageParser : IDisposable
    {
        RealTimeMessageType MessageType { get; }
        bool ObjectIsReady { get; }
        void Parse(string dataLine);
    }
}
