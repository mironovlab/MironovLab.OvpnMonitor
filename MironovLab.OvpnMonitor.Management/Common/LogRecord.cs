using System;

namespace MironovLab.OpenVPN.Management.Common
{
    public readonly struct LogRecord
    {
        public readonly DateTime DateTime;
        public readonly LogRecordFlags Flags;
        public readonly string MessageText;

        internal LogRecord(DateTime dateTime, LogRecordFlags flags, string messageText)
        {
            DateTime = dateTime;
            Flags = flags;
            MessageText = messageText;
        }
    }

    [Flags]
    public enum LogRecordFlags
    {
        None = 0,
        Informational = 1,
        FatalError = 1 << 1,
        NonFatalError = 1 << 2,
        Warning = 1 << 3,
        Debug = 1 << 4,
    }
}
