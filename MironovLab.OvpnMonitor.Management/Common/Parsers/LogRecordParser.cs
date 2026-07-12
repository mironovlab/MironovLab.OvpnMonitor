using MironovLab.OpenVPN.Management.Core;
using MironovLab.OpenVPN.Management.Exceptions;

namespace MironovLab.OpenVPN.Management.Common.Parsers
{
    internal class LogRecordParser
    {
        private readonly string[] _data = new string[3];

        public LogRecord Parse(string logRecord)
        {
            if (ParseUtils.SequentialSplit(logRecord, Constants.MessageParamSplitter, _data) == _data.Length)
            {
                var dateTime = ParseUtils.DateTimeFromUnixTime(_data[0]);
                var flags = LogRecordFlags.None;
                foreach (var flagChar in _data[1])
                {
                    switch (flagChar)
                    {
                        case 'I':
                            flags |= LogRecordFlags.Informational;
                            break;
                        case 'F':
                            flags |= LogRecordFlags.FatalError;
                            break;
                        case 'N':
                            flags |= LogRecordFlags.NonFatalError;
                            break;
                        case 'W':
                            flags |= LogRecordFlags.Warning;
                            break;
                        case 'D':
                            flags |= LogRecordFlags.Debug;
                            break;
                    }
                }

                return new LogRecord(dateTime, flags, _data[2]);
            }

            throw ParsingException.Create(logRecord);
        }
    }
}
