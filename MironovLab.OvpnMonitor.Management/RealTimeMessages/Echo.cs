using MironovLab.OpenVPN.Management.Common;

namespace MironovLab.OpenVPN.Management.RealTimeMessages
{
    public class Echo : RealTimeMessage
    {
        public override RealTimeMessageType Type => RealTimeMessageType.Echo;
        public EchoItem EchoItem { get; }

        internal Echo(EchoItem echoItem)
        {
            EchoItem = echoItem;
        }
    }
}
