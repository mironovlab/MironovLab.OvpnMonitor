using System.Net;
using MironovLab.OpenVPN.Management.Core;
using MironovLab.OpenVPN.Management.Exceptions;

namespace MironovLab.OpenVPN.Management.Common.Parsers
{
    internal class StateRecordParser
    {
        private readonly string[] _data = new string[8];

        public StateRecord Parse(string stateRecord)
        {
            if (ParseUtils.SequentialSplit(stateRecord, Constants.MessageParamSplitter, _data) == _data.Length)
            {
                var dateTime = ParseUtils.DateTimeFromUnixTime(_data[0]);
                StateName state;
                switch (_data[1])
                {
                    case "CONNECTING":
                        state = StateName.Connecting;
                        break;
                    case "WAIT":
                        state = StateName.Waiting;
                        break;
                    case "AUTH":
                        state = StateName.Authenticating;
                        break;
                    case "GET_CONFIG":
                        state = StateName.GettingConfiguration;
                        break;
                    case "ASSIGN_IP":
                        state = StateName.AssigningIP;
                        break;
                    case "ADD_ROUTES":
                        state = StateName.AddingRoutes;
                        break;
                    case "CONNECTED":
                        state = StateName.Connected;
                        break;
                    case "RECONNECTING":
                        state = StateName.Reconnecting;
                        break;
                    case "EXITING":
                        state = StateName.Exiting;
                        break;
                    case "RESOLVE":
                        state = StateName.Resolving;
                        break;
                    default:
                        state = StateName.Undefined;
                        break;
                }

                var description = string.IsNullOrEmpty(_data[2]) ? null : _data[2];
                IPAddress.TryParse(_data[3], out var localAddress);

                IPEndPoint remoteServer = null;
                if (IPAddress.TryParse(_data[4], out var remoteIP) && int.TryParse(_data[5], out var remotePort))
                    remoteServer = new IPEndPoint(remoteIP, remotePort);

                IPEndPoint listening = null;
                if (IPAddress.TryParse(_data[6], out var listeningIP) && int.TryParse(_data[7], out var listeningPort))
                    listening = new IPEndPoint(listeningIP, listeningPort);

                return new StateRecord(dateTime, state, description, localAddress, remoteServer, listening);
            }

            throw ParsingException.Create(stateRecord);
        }
    }
}
