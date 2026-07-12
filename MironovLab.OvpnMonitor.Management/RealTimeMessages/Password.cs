using MironovLab.OpenVPN.Management.Common;

namespace MironovLab.OpenVPN.Management.RealTimeMessages
{
    public class Password : RealTimeMessage
    {
        public override RealTimeMessageType Type => RealTimeMessageType.Password;
        public string Message { get; }
        public PasswordMessageType MessageType { get; }
        public PasswordType PasswordType { get; }
        public string CustomText { get; }

        internal Password(string message, PasswordMessageType messageType, PasswordType passwordType, string customText)
        {
            Message = message;
            MessageType = messageType;
            PasswordType = passwordType;
            CustomText = customText;
        }
    }
}
