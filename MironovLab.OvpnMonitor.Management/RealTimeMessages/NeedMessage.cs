namespace MironovLab.OpenVPN.Management.RealTimeMessages
{
    public abstract class NeedMessage : RealTimeMessage
    {
        public string UserMessage { get; }

        protected NeedMessage(string userMessage)
        {
            UserMessage = userMessage;
        }
    }
}
