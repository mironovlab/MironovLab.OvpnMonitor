using System;
using System.Collections.Generic;
using System.Text;
using MironovLab.OpenVPN.Management.Core;

namespace MironovLab.OpenVPN.Management.Common.Pf
{
    public class PacketFilter
    {
        public Clients Clients { get; }
        public Subnets Subnets { get; }

        public PacketFilter() : this(null, null)
        {
        }

        public PacketFilter(Clients clients) : this(clients, null)
        {
        }

        public PacketFilter(Subnets subnets) : this(null, subnets)
        {
        }

        public PacketFilter(Clients clients, Subnets subnets)
        {
            Clients = clients ?? new Clients(PolicyType.Accept);
            Subnets = subnets ?? new Subnets(PolicyType.Accept);
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            Clients.ToString(sb);
            Subnets.ToString(sb);
            sb.Append(Constants.SectionEnclosureStart);
            sb.Append(Constants.EndOfResult);
            sb.Append(Constants.SectionEnclosureEnd);
            sb.Append(Constants.NewLine);
            return sb.ToString();
        }

        public static bool TryParse(string pfData, out PacketFilter packetFilter)
        {
            packetFilter = null;

            var strings = ParseUtils.SplitAndSanitize(pfData);
            using (var enumerator = strings.GetEnumerator())
            {
                if (!enumerator.MoveNext())
                    return false;

                Clients clients = null;
                Subnets subnets = null;
                var sectionReader = new SectionReader();
                while (true)
                {
                    if (sectionReader.EndOfFile)
                        return false;

                    if (!SectionHelper.TryParseHeader(enumerator.Current, out var sectionName, out _) && sectionName != Constants.EndOfResult)
                        return false;

                    switch (sectionName)
                    {
                        case Clients.SectionName:
                            if (clients != null)
                                return false;
                            if (!Clients.TryParse(sectionReader.ReadSection(enumerator), out clients))
                                return false;
                            break;

                        case Subnets.SectionName:
                            if (subnets != null)
                                return false;
                            if (!Subnets.TryParse(sectionReader.ReadSection(enumerator), out subnets))
                                return false;
                            break;

                        case Constants.EndOfResult:
                            packetFilter = new PacketFilter(clients, subnets);
                            return true;

                        default:
                            return false;
                    }
                }
            }
        }

        public static PacketFilter Parse(string pfData)
        {
            if (pfData == null) throw new ArgumentNullException(nameof(pfData));

            if (!TryParse(pfData, out var result))
                throw new FormatException(Resources.ParseFail);

            return result;
        }

        private class SectionReader
        {
            public bool EndOfFile { get; private set; }

            public IEnumerable<string> ReadSection(IEnumerator<string> enumerator)
            {
                var current = enumerator.Current;
                if (!IsHeader(current))
                    yield break;

                yield return current;

                while (enumerator.MoveNext())
                {
                    current = enumerator.Current;

                    if (IsHeader(current))
                        yield break;

                    yield return current;
                }

                EndOfFile = true;
            }

            private static bool IsHeader(string header)
            {
                return !string.IsNullOrEmpty(header) &&
                       header[0] == Constants.SectionEnclosureStart &&
                       header[header.Length - 1] == Constants.SectionEnclosureEnd;
            }
        }
    }
}
