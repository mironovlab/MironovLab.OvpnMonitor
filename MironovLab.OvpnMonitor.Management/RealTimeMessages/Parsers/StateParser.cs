using System.Collections.Generic;
using System.Linq;
using MironovLab.OpenVPN.Management.Common.Parsers;

namespace MironovLab.OpenVPN.Management.RealTimeMessages.Parsers
{
    internal class StateParser : RealTimeMessageParser<State>
    {
        public override RealTimeMessageType MessageType => RealTimeMessageType.State;

        protected override State ParseInternal(IEnumerable<string> lines)
        {
            var parser = new StateRecordParser();
            return new State(parser.Parse(lines.First()));
        }
    }
}
