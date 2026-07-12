using System;
using System.Collections.Generic;
using MironovLab.OpenVPN.Management.Core;

namespace MironovLab.OpenVPN.Management.Common.Pf
{
    public sealed class Clients : Section<ClientPolicy>
    {
        internal const string SectionName = "CLIENTS";

        public override ICollection<ClientPolicy> Policies { get; } = new HashSet<ClientPolicy>();

        public Clients(PolicyType defaultPolicyType) : base(defaultPolicyType)
        {
        }

        public void Add(PolicyType policyType, string commonName)
        {
            Add(new ClientPolicy(policyType, commonName));
        }

        internal override string GetName()
        {
            return SectionName;
        }

        public static bool TryParse(string clientsSection, out Clients clients)
        {
            clients = default;

            if (string.IsNullOrWhiteSpace(clientsSection))
                return false;

            var strings = ParseUtils.SplitAndSanitize(clientsSection);
            return TryParse(strings, out clients);
        }

        public static Clients Parse(string clientsSection)
        {
            if (clientsSection == null) throw new ArgumentNullException(nameof(clientsSection));

            if (!TryParse(clientsSection, out var result))
                throw new FormatException(Resources.ParseFail);

            return result;
        }

        internal static bool TryParse(IEnumerable<string> strings, out Clients clients)
        {
            clients = default;

            using (var enumerator = strings.GetEnumerator())
            {
                if (!enumerator.MoveNext())
                    return false;

                if (!TryParseHeader(enumerator.Current, SectionName, out var policyType))
                    return false;

                clients = new Clients(policyType);

                while (enumerator.MoveNext())
                {
                    if (!ClientPolicy.TryParse(enumerator.Current, out var rule))
                        return false;

                    clients.Add(rule);
                }

                return true;
            }
        }
    }
}
