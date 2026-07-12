using System.Collections.Generic;
using System.Linq;
using MironovLab.OpenVPN.Management.Core;

namespace MironovLab.OpenVPN.Management.RealTimeMessages.Parsers
{
    internal class BytesCountCliParser : RealTimeMessageParser<ByteCountCli>
    {
        public override RealTimeMessageType MessageType => RealTimeMessageType.ByteCountCli;

        protected override ByteCountCli ParseInternal(IEnumerable<string> lines)
        {
            var parameters = ParseUtils.SplitRealTimeMessageDataParts(lines.First());
            var cid = int.Parse(parameters[1]);
            var bytesIn = long.Parse(parameters[2]);
            var bytesOut = long.Parse(parameters[3]);
            return new ByteCountCli(cid, bytesIn, bytesOut);
        }
    }
}
