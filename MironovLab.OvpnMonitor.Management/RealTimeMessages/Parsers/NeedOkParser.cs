using System.Collections.Generic;
using System.Linq;
using MironovLab.OpenVPN.Management.Common;
using MironovLab.OpenVPN.Management.Core;
using MironovLab.OpenVPN.Management.Exceptions;

namespace MironovLab.OpenVPN.Management.RealTimeMessages.Parsers
{
    internal class NeedOkParser : NeedMessagesParser<NeedOk>
    {
        private readonly string[] _data = new string[3];
        public override RealTimeMessageType MessageType => RealTimeMessageType.NeedOk;

        protected override NeedOk ParseInternal(IEnumerable<string> lines)
        {
            var needOkLine = lines.First();
            SplitUserMessage(needOkLine, out needOkLine, out var userMessage);
            ParseUtils.ParseSimpleMessage(needOkLine, out var messageType, out var text);

            if (messageType != Constants.RealTimeMessages.NeedOk)
                throw ParsingException.Create(needOkLine);

            var count = ParseUtils.SequentialSplit(needOkLine, Constants.WhiteSpace, _data, Constants.SingleQuote, Constants.SingleQuote);
            if (count < 2)
                throw ParsingException.Create(needOkLine);

            for (var i = 0; i < count; i++)
                _data[i] = _data[i].Trim();

            NeedOkRequestType requestType;
            switch (_data[1])
            {
                case Constants.CommandArguments.NeedOkTokenInsertionRequest:
                    requestType = NeedOkRequestType.TokenInsertionRequest;
                    break;
                default:
                    throw ParsingException.Create(needOkLine);
            }

            return new NeedOk(requestType, userMessage);
        }
    }
}
