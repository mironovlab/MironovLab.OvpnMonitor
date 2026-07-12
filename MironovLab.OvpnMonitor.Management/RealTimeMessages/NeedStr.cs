namespace MironovLab.OpenVPN.Management.RealTimeMessages
{
    public class NeedStr : NeedMessage
    {
        public override RealTimeMessageType Type => RealTimeMessageType.NeedStr;
        public string InputType { get; }

        internal NeedStr(string inputType, string userMessage) : base(userMessage)
        {
            InputType = inputType;
        }
    }
}
