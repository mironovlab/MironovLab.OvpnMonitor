using System;
using System.Collections.Generic;
using System.Net;
using MironovLab.OpenVPN.Management.Core;

namespace MironovLab.OpenVPN.Management.RealTimeMessages.Parsers
{
    internal class ClientParser : RealTimeMessageParser<Client>
    {
        private const string Env = "ENV";
        public override RealTimeMessageType MessageType => RealTimeMessageType.Client;

        protected override Client ParseInternal(IEnumerable<string> lines)
        {
            var notificationType = ClientNotificationType.Undefined;
            var cid = 0;
            int? kid = null;
            IPNetwork2 addr = null;
            bool? pri = null;
            Dictionary<string, string> environmentalVariables = null;

            var parameters = new string[3];
            foreach (var line in lines)
            {
                ParseUtils.ParseSimpleMessage(line, out var messageType, out var data);

                if (messageType != Constants.RealTimeMessages.Client)
                    break;

                ParseUtils.SequentialSplit(data, Constants.MessageParamSplitter, parameters);
                if (parameters[0] != Env)
                {
                    switch (parameters[0])
                    {
                        case "CONNECT":
                            notificationType = ClientNotificationType.Connect;
                            break;
                        case "REAUTH":
                            notificationType = ClientNotificationType.ReAuth;
                            break;
                        case "ESTABLISHED":
                            notificationType = ClientNotificationType.Established;
                            break;
                        case "DISCONNECT":
                            notificationType = ClientNotificationType.Disconnect;
                            break;
                        case "ADDRESS":
                            notificationType = ClientNotificationType.Address;
                            break;
                    }

                    switch (notificationType)
                    {
                        case ClientNotificationType.Connect:
                        case ClientNotificationType.ReAuth:
                            cid = int.Parse(parameters[1]);
                            kid = int.Parse(parameters[2]);
                            break;
                        case ClientNotificationType.Established:
                        case ClientNotificationType.Disconnect:
                            cid = int.Parse(parameters[1]);
                            break;
                        case ClientNotificationType.Address:
                            cid = int.Parse(parameters[1]);
                            addr = IPNetwork2.Parse(parameters[2]);
                            pri = Convert.ToBoolean(int.Parse(parameters[3]));
                            goto returnResult;
                    }
                }
                else
                {
                    if (parameters[1] != Constants.EndOfResult)
                    {
                        if (ParseUtils.SequentialSplit(data, Constants.MessageParamSplitter, parameters, 2) == 2 &&
                            ParseUtils.SequentialSplit(parameters[1], Constants.NameValueSplitter, parameters, 2) == 2)
                        {
                            if (environmentalVariables == null)
                                environmentalVariables = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                            environmentalVariables[parameters[0]] = parameters[1];
                        }
                    }
                    else
                    {
                        break;
                    }
                }
            }

            returnResult:
            return new Client(notificationType, cid, kid, addr, pri, environmentalVariables);
        }
    }
}
