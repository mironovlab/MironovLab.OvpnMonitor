using System.Collections.Generic;
using System.Linq;
using MironovLab.OpenVPN.Management.Core;
using MironovLab.OpenVPN.Management.Exceptions;

namespace MironovLab.OpenVPN.Management.RealTimeMessages.Parsers
{
    internal class SimpleTextMessageParser : RealTimeMessageParser<SimpleTextMessage>
    {
        public override RealTimeMessageType MessageType { get; }

        public SimpleTextMessageParser(RealTimeMessageType messageType)
        {
            MessageType = messageType;
        }

        protected override SimpleTextMessage ParseInternal(IEnumerable<string> lines)
        {
            var line = lines.First();
            ParseUtils.ParseSimpleMessage(line, out var messageType, out var text);
            string messageTypeToCompareWith;
            switch (MessageType)
            {
                case RealTimeMessageType.Fatal:
                    messageTypeToCompareWith = Constants.RealTimeMessages.Fatal;
                    break;
                case RealTimeMessageType.Hold:
                    messageTypeToCompareWith = Constants.RealTimeMessages.Hold;
                    break;
                case RealTimeMessageType.Info:
                    messageTypeToCompareWith = Constants.RealTimeMessages.Info;
                    break;
                default:
                    messageTypeToCompareWith = string.Empty;
                    break;
            }

            if (messageType != messageTypeToCompareWith)
                throw ParsingException.Create(line);

            return new SimpleTextMessage(MessageType, text);
        }
    }
}
