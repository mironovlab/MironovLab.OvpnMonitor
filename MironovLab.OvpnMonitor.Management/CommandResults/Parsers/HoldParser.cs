using System;
using MironovLab.OpenVPN.Management.Common;
using MironovLab.OpenVPN.Management.Core;
using MironovLab.OpenVPN.Management.Exceptions;

namespace MironovLab.OpenVPN.Management.CommandResults.Parsers
{
    internal static class HoldParser
    {
        public static Switch Parse(string holdResult)
        {
            ParseUtils.ParseSimpleMessage(holdResult, out var messageType, out var text);
            switch (messageType)
            {
                case Constants.MessageTypes.Success:
                    var keyValue = new string[2];
                    if (ParseUtils.SequentialSplit(text, Constants.NameValueSplitter, keyValue) == keyValue.Length &&
                        keyValue[0] == Constants.Commands.Hold && int.TryParse(keyValue[1], out var hold))
                        return Convert.ToBoolean(hold) ? Switch.On : Switch.Off;
                    break;

                case Constants.MessageTypes.Error:
                    throw new CommandResultError(text);
            }

            throw ParsingException.Create(holdResult);
        }
    }
}
