using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using MironovLab.OpenVPN.Management.Core;
using MironovLab.OpenVPN.Management.Exceptions;

namespace MironovLab.OpenVPN.Management.CommandResults.Parsers
{
    internal class StatusParser : ParserBase<Status>
    {
        private const string ClientListTableName = "CLIENT_LIST";
        private const string RoutingTableName = "ROUTING_TABLE";
        private readonly string[] _data = new string[4];

        public StatusParser(ILoggerFactory loggerFactory) : base(loggerFactory)
        {
        }

        protected override Status Parse(IEnumerable<string> lines)
        {
            var title = string.Empty;
            var time = DateTime.MinValue;
            var clients = new List<Status.Client>();
            var routingTable = new List<Status.Route>();
            TableReader clientsTableReader = null;
            TableReader routingTableReader = null;
            var maxBroadcastQueueLength = 0;

            foreach (var line in lines)
            {
                try
                {
                    ParseUtils.SequentialSplit(line, Constants.MessageParamSplitter, _data);

                    switch (_data[0])
                    {
                        case "TITLE":
                            title = _data[1];
                            break;
                        case "TIME":
                            time = ParseUtils.DateTimeFromUnixTime(long.Parse(_data[2]));
                            break;
                        case Constants.TableHeader:
                            switch (_data[1])
                            {
                                case ClientListTableName:
                                    clientsTableReader = new TableReader(line);
                                    break;
                                case RoutingTableName:
                                    routingTableReader = new TableReader(line);
                                    break;
                                default:
                                    throw new ParsingException(string.Format(Resources.CommandResultParsing_UnexpectedTableName, _data[1]));
                            }
                            break;
                        case ClientListTableName:
                            if (clientsTableReader == null)
                                throw ParsingException.Create(line);

                            clientsTableReader.ReadRow(line);
                            var client = new Status.Client(
                                clientsTableReader["Common Name"],
                                ParseUtils.ParseIPEndPoint(clientsTableReader["Real Address"]),
                                ParseUtils.ParseIPAddressSafe(clientsTableReader["Virtual Address"]),
                                ParseUtils.ParseIPAddressSafe(clientsTableReader["Virtual IPv6 Address"]),
                                long.Parse(clientsTableReader["Bytes Received"]),
                                long.Parse(clientsTableReader["Bytes Sent"]),
                                ParseUtils.DateTimeFromUnixTime(clientsTableReader["Connected Since (time_t)"]),
                                clientsTableReader["Username"],
                                int.Parse(clientsTableReader["Client ID"]),
                                int.Parse(clientsTableReader["Peer ID"]),
                                clientsTableReader["Data Channel Cipher"]);
                            clients.Add(client);
                            break;
                        case RoutingTableName:
                            if (routingTableReader == null)
                                throw ParsingException.Create(line);

                            routingTableReader.ReadRow(line);
                            var route = new Status.Route(
                                routingTableReader["Virtual Address"],
                                routingTableReader["Common Name"],
                                routingTableReader["Real Address"],
                                ParseUtils.DateTimeFromUnixTime(routingTableReader["Last Ref (time_t)"]));
                            routingTable.Add(route);
                            break;
                        case "GLOBAL_STATS":
                            maxBroadcastQueueLength = int.Parse(_data[2]);
                            break;
                    }
                }
                catch (Exception e)
                {
                    Logger.LogError(e, $"Error parse line: {line}");
                }
            }

            return new Status(title, time, clients, routingTable, maxBroadcastQueueLength);
        }
    }
}
