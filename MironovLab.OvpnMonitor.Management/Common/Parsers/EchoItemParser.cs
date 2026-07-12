using MironovLab.OpenVPN.Management.Core;
using MironovLab.OpenVPN.Management.Exceptions;

namespace MironovLab.OpenVPN.Management.Common.Parsers
{
    internal class EchoItemParser
    {
        private readonly string[] _data = new string[2];

        public EchoItem Parse(string echoItem)
        {
            if (ParseUtils.SequentialSplit(echoItem, Constants.MessageParamSplitter, _data) == _data.Length)
            {
                var dateTime = ParseUtils.DateTimeFromUnixTime(_data[0]);
                return new EchoItem(dateTime, _data[1]);
            }

            throw ParsingException.Create(echoItem);
        }
    }
}
