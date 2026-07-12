using System.Collections.Generic;
using System.Linq;
using MironovLab.OpenVPN.Management.Common.Parsers;
using MironovLab.OpenVPN.Management.Core;
using MironovLab.OpenVPN.Management.Exceptions;

namespace MironovLab.OpenVPN.Management.RealTimeMessages.Parsers
{
    internal class EchoParser : RealTimeMessageParser<Echo>
    {
        public override RealTimeMessageType MessageType => RealTimeMessageType.Echo;

        protected override Echo ParseInternal(IEnumerable<string> lines)
        {
            var parsed = new string[2];
            var line = lines.First();
            if (ParseUtils.SequentialSplit(line, Constants.SourceTypeDataSplitter, parsed) == 2 &&
                parsed[0] == Constants.RealTimeMessages.Echo)
            {
                var parser = new EchoItemParser();
                return new Echo(parser.Parse(parsed[1]));
            }

            throw ParsingException.Create(line);
        }
    }
}
