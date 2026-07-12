using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using MironovLab.OpenVPN.Management.Common;
using MironovLab.OpenVPN.Management.Common.Parsers;

namespace MironovLab.OpenVPN.Management.CommandResults.Parsers
{
    internal class EchoParser : ParserBase<List<EchoItem>>
    {
        public EchoParser(ILoggerFactory loggerFactory) : base(loggerFactory)
        {
        }

        protected override List<EchoItem> Parse(IEnumerable<string> lines)
        {
            var result = new List<EchoItem>();
            var parser = new EchoItemParser();

            foreach (var line in lines)
            {
                result.Add(parser.Parse(line));
            }

            return result;
        }
    }
}
