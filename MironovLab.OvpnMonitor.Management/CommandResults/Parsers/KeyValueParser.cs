using System;
using MironovLab.OpenVPN.Management.Common;
using MironovLab.OpenVPN.Management.Core;
using MironovLab.OpenVPN.Management.Exceptions;

namespace MironovLab.OpenVPN.Management.CommandResults.Parsers
{
    internal static class KeyValueParser
    {
        public static int ParseMute(string muteResult)
        {
            return ParseInt(muteResult, Constants.Commands.Mute);
        }

        public static int ParsePid(string pidResult)
        {
            return ParseInt(pidResult, Constants.Commands.Pid);
        }

        public static int ParseVerb(string verbResult)
        {
            return ParseInt(verbResult, Constants.Commands.Verb);
        }

        public static AuthRetryType ParseAuthRetryType(string authRetryTypeResult)
        {
            var parseResult = Parse(authRetryTypeResult, Constants.Commands.AuthRetry);
            switch (parseResult)
            {
                case Constants.CommandArguments.AuthRetryTypeNone:
                    return AuthRetryType.None;
                case Constants.CommandArguments.AuthRetryTypeNoInteract:
                    return AuthRetryType.NoInteract;
                case Constants.CommandArguments.AuthRetryTypeInteract:
                    return AuthRetryType.Interact;
                default:
                    throw new ArgumentOutOfRangeException(nameof(authRetryTypeResult), parseResult, null);
            }
        }

        private static int ParseInt(string resultLine, string commandName)
        {
            if (int.TryParse(Parse(resultLine, commandName), out var result))
                return result;

            throw ParsingException.Create(resultLine);
        }

        private static string Parse(string result, string commandName)
        {
            ParseUtils.ParseSimpleMessage(result, out var messageType, out var text);
            switch (messageType)
            {
                case Constants.MessageTypes.Success:
                    var keyValue = new string[2];
                    if (ParseUtils.SequentialSplit(text, Constants.NameValueSplitter, keyValue) == keyValue.Length && keyValue[0] == commandName)
                        return keyValue[1];
                    break;

                case Constants.MessageTypes.Error:
                    throw new CommandResultError(text);
            }

            throw ParsingException.Create(result);
        }
    }
}
