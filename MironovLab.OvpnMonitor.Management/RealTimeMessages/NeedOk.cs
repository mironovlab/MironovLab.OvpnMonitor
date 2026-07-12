using MironovLab.OpenVPN.Management.Common;

namespace MironovLab.OpenVPN.Management.RealTimeMessages
{
    public class NeedOk : NeedMessage
    {
        public override RealTimeMessageType Type => RealTimeMessageType.NeedOk;
        public NeedOkRequestType RequestType { get; }

        internal NeedOk(NeedOkRequestType requestType, string userMessage) : base(userMessage)
        {
            RequestType = requestType;
        }
    }
}
