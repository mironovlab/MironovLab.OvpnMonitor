using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using MironovLab.OpenVPN.Management.Common;
using MironovLab.OpenVPN.Management.Common.Parsers;

namespace MironovLab.OpenVPN.Management.CommandResults.Parsers
{
    internal class LogParser : ParserBase<List<LogRecord>>
    {
        private readonly int _logRecordCount;

        public LogParser(int logRecordCount, ILoggerFactory loggerFactory) : base(loggerFactory)
        {
            _logRecordCount = logRecordCount;
        }

        protected override List<LogRecord> Parse(IEnumerable<string> lines)
        {
            var result = new List<LogRecord>(_logRecordCount);
            var parser = new LogRecordParser();

            foreach (var line in lines)
            {
                result.Add(parser.Parse(line));
            }

            return result;
        }
    }
}
