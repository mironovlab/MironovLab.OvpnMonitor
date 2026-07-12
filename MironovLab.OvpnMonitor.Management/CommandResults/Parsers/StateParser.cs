using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using MironovLab.OpenVPN.Management.Common;
using MironovLab.OpenVPN.Management.Common.Parsers;

namespace MironovLab.OpenVPN.Management.CommandResults.Parsers
{
    internal class StateParser : ParserBase<List<StateRecord>>
    {
        private readonly int _count;

        public StateParser(ILoggerFactory loggerFactory) : this(0, loggerFactory)
        {
        }

        public StateParser(int count, ILoggerFactory loggerFactory) : base(loggerFactory)
        {
            _count = count;
        }

        protected override List<StateRecord> Parse(IEnumerable<string> lines)
        {
            var result = new List<StateRecord>(_count);
            var parser = new StateRecordParser();

            foreach (var line in lines)
            {
                result.Add(parser.Parse(line));
            }

            return result;
        }
    }
}
