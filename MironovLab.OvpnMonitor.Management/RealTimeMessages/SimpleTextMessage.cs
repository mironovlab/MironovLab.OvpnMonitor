namespace MironovLab.OpenVPN.Management.RealTimeMessages
{
    public class SimpleTextMessage : RealTimeMessage
    {
        public sealed override RealTimeMessageType Type { get; }
        public string Text { get; }

        internal SimpleTextMessage(RealTimeMessageType type, string text)
        {
            Type = type;
            Text = text;
        }
    }
}
