using System.Collections.Generic;
using System.Linq;
using MironovLab.OpenVPN.Management.Common;
using MironovLab.OpenVPN.Management.Core;

namespace MironovLab.OpenVPN.Management.RealTimeMessages.Parsers
{
    internal class NotifyParser : RealTimeMessageParser<Notify>
    {
        public override RealTimeMessageType MessageType => RealTimeMessageType.State;

        protected override Notify ParseInternal(IEnumerable<string> lines)
        {
            ParseUtils.ParseSimpleMessage(lines.First(), out _, out var notifyData);
            var parameters = new string[3];
            if (ParseUtils.SequentialSplit(notifyData, Constants.MessageParamSplitter, parameters) == parameters.Length)
            {
                var severity = parameters[0];
                var type = parameters[1];
                var text = parameters[2];
                NotifySeverity severityEnum;
                NotifyType typeEnum;

                switch (severity.ToLowerInvariant())
                {
                    case "info":
                        severityEnum = NotifySeverity.Info;
                        break;

                    default:
                        severityEnum = NotifySeverity.Other;
                        break;
                }

                switch (type.ToLowerInvariant())
                {
                    case "remote-exit":
                        typeEnum = NotifyType.RemoteExit;
                        break;

                    case "server-pushed-connection-reset":
                        typeEnum = NotifyType.ServerPushedConnectionReset;
                        break;

                    case "server-pushed-halt":
                        typeEnum = NotifyType.ServerPushedHalt;
                        break;

                    default:
                        typeEnum = NotifyType.Other;
                        break;
                }

                return new Notify(severityEnum, severity, typeEnum, type, text);
            }

            return new Notify(NotifySeverity.Other, null, NotifyType.Other, null, null);
        }
    }
}
