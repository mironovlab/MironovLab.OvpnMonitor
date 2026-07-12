using MironovLab.OpenVPN.Management.Common;

namespace MironovLab.OpenVPN.Management.RealTimeMessages
{
    public class Log : RealTimeMessage
    {
        public override RealTimeMessageType Type => RealTimeMessageType.Log;
        public LogRecord LogRecord { get; }

        internal Log(LogRecord logRecord)
        {
            LogRecord = logRecord;
        }
    }
}
