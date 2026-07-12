using System.Collections.Generic;
using System.Linq;
using MironovLab.OpenVPN.Management.Core;

namespace MironovLab.OpenVPN.Management.RealTimeMessages.Parsers
{
    internal class ByteCountParser : RealTimeMessageParser<ByteCount>
    {
        public override RealTimeMessageType MessageType => RealTimeMessageType.ByteCount;

        protected override ByteCount ParseInternal(IEnumerable<string> lines)
        {
            var parameters = ParseUtils.SplitRealTimeMessageDataParts(lines.First());
            var bytesIn = long.Parse(parameters[1]);
            var bytesOut = long.Parse(parameters[2]);
            return new ByteCount(bytesIn, bytesOut);
        }
    }
}
