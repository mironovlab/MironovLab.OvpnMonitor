using MironovLab.OpenVPN.Management.Core;
using MironovLab.OpenVPN.Management.Exceptions;
using System.Collections.Generic;
using System.Linq;

namespace MironovLab.OpenVPN.Management.RealTimeMessages.Parsers
{
    internal class NeedStrParser : NeedMessagesParser<NeedStr>
    {
        private readonly string[] _data = new string[3];
        public override RealTimeMessageType MessageType => RealTimeMessageType.NeedStr;

        protected override NeedStr ParseInternal(IEnumerable<string> lines)
        {
            var needStrLine = lines.First();
            SplitUserMessage(needStrLine, out needStrLine, out var userMessage);
            ParseUtils.ParseSimpleMessage(needStrLine, out var messageType, out var text);

            if (messageType != Constants.RealTimeMessages.NeedStr)
                throw ParsingException.Create(needStrLine);

            var count = ParseUtils.SequentialSplit(needStrLine, Constants.WhiteSpace, _data, Constants.SingleQuote, Constants.SingleQuote);
            if (count < 2)
                throw ParsingException.Create(needStrLine);

            for (var i = 0; i < count; i++)
                _data[i] = _data[i].Trim();

            return new NeedStr(_data[1], userMessage);
        }
    }
}
