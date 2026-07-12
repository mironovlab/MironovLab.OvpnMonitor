using System;
using MironovLab.OpenVPN.Management.Core;

namespace MironovLab.OpenVPN.Management.RealTimeMessages.Parsers
{
    internal abstract class NeedMessagesParser<T> : RealTimeMessageParser<T> where T : RealTimeMessage
    {
        protected void SplitUserMessage(string line, out string messageText, out string userMessage)
        {
            messageText = line;
            userMessage = null;

            var pos = line.IndexOf("MSG:", StringComparison.Ordinal);
            if (pos >= 0)
            {
                messageText = line.Substring(0, pos).TrimEnd();
                ParseUtils.ParseSimpleMessage(line.Substring(pos), out _, out userMessage);
            }
        }
    }
}
