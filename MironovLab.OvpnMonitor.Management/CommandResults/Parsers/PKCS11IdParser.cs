using System;
using MironovLab.OpenVPN.Management.Core;
using MironovLab.OpenVPN.Management.Exceptions;

namespace MironovLab.OpenVPN.Management.CommandResults.Parsers
{
    internal static class PKCS11IdParser
    {
        public static int ParseCount(string input)
        {
            ParseUtils.ParseSimpleMessage(input, out var messageType, out var value);
            if (messageType == Constants.MessageTypes.PKCS11IdCount && int.TryParse(value, out var result))
                return result;

            throw ParsingException.Create(input);
        }

        public static PKCS11IdEntry ParseGet(string input)
        {
            var num = 0;
            var id = string.Empty;
            var blob = Array.Empty<byte>();
            foreach (var pair in ParseUtils.ParseKeyValuePairs(input))
            {
                switch (pair.Key)
                {
                    case Constants.MessageTypes.PKCS11IdGet:
                        num = int.Parse(pair.Value);
                        break;
                    case "ID":
                        id = pair.Value;
                        break;
                    case "BLOB":
                        blob = Convert.FromBase64String(pair.Value);
                        break;
                    default:
                        throw ParsingException.Create(input);
                }
            }

            return new PKCS11IdEntry(num, id, blob);
        }
    }
}
