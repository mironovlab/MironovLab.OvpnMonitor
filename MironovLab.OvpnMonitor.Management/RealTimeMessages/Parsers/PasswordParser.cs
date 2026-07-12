using System.Collections.Generic;
using System.Linq;
using MironovLab.OpenVPN.Management.Common;
using MironovLab.OpenVPN.Management.Core;
using MironovLab.OpenVPN.Management.Exceptions;

namespace MironovLab.OpenVPN.Management.RealTimeMessages.Parsers
{
    internal class PasswordParser : RealTimeMessageParser<Password>
    {
        private readonly string[] _data = new string[3];
        public override RealTimeMessageType MessageType => RealTimeMessageType.Password;

        protected override Password ParseInternal(IEnumerable<string> lines)
        {
            var passwordLine = lines.First();
            ParseUtils.ParseSimpleMessage(passwordLine, out var messageType, out var text);
            if (messageType != Constants.RealTimeMessages.Password)
                throw ParsingException.Create(passwordLine);

            var splitter = text.IndexOf(Constants.SourceTypeDataSplitter) < 0
                ? Constants.WhiteSpace
                : Constants.SourceTypeDataSplitter;
                var count = ParseUtils.SequentialSplit(text, splitter, _data, Constants.SingleQuote, Constants.SingleQuote);
            if (count < 2)
                throw ParsingException.Create(passwordLine);

            for (var i = 0; i < count; i++)
                _data[i] = _data[i].Trim(Constants.WhiteSpace, Constants.SourceTypeDataSplitter, Constants.SingleQuote);

            PasswordMessageType pwdMsgType;
            PasswordType passwordType;
            string customText = null;

            switch (_data[0])
            {
                case "Need":
                    pwdMsgType = PasswordMessageType.Need;
                    break;
                case "Verification Failed":
                    pwdMsgType = PasswordMessageType.VerificationFailed;
                    break;
                default:
                    throw ParsingException.Create(passwordLine);
            }

            switch (_data[1])
            {
                case "Private Key":
                    passwordType = PasswordType.PrivateKey;
                    break;
                case "Auth":
                    passwordType = PasswordType.Auth;
                    break;
                default:
                    passwordType = PasswordType.CustomText;
                    customText = _data[1];
                    break;
            }

            return new Password(passwordLine, pwdMsgType, passwordType, customText);
        }
    }
}
