using MironovLab.OpenVPN.Management.Common;

namespace MironovLab.OpenVPN.Management.RealTimeMessages
{
    public class State : RealTimeMessage
    {
        public override RealTimeMessageType Type => RealTimeMessageType.State;
        public StateRecord SenderRecord { get; }

        internal State(StateRecord senderRecord)
        {
            SenderRecord = senderRecord;
        }
    }
}
