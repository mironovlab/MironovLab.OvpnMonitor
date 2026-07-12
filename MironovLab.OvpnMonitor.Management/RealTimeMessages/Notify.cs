using MironovLab.OpenVPN.Management.Common;

namespace MironovLab.OpenVPN.Management.RealTimeMessages
{
    public class Notify : RealTimeMessage
    {
        public override RealTimeMessageType Type => RealTimeMessageType.Notify;
        public NotifySeverity Severity { get; }
        public string SeverityText { get; }
        public NotifyType NotifyType { get; }
        public string NotifyTypeText { get; }
        public string Text { get; }

        internal Notify(NotifySeverity severity, string severityText, NotifyType type, string notifyTypeText, string text)
        {
            Severity = severity;
            SeverityText = severityText;
            NotifyType = type;
            NotifyTypeText = notifyTypeText;
            Text = text;
        }
    }
}
