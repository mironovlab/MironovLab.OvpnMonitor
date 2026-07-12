using System.Collections.Generic;
using System.Net;
using System;
using MironovLab.OpenVPN.Management.Core;

namespace MironovLab.OpenVPN.Management.Common.Pf
{
    public sealed class Subnets : Section<SubnetPolicy>
    {
        internal const string SectionName = "SUBNETS";

        public override ICollection<SubnetPolicy> Policies { get; } = new List<SubnetPolicy>();

        public Subnets(PolicyType defaultPolicyType) : base(defaultPolicyType)
        {
        }

        public void Add(PolicyType policyType)
        {
            Add(new UnknownPolicy(policyType));
        }

        public void Add(PolicyType policyType, IPAddress ipAddress)
        {
            Add(new IPAddressPolicy(policyType, ipAddress));
        }

        public void Add(PolicyType policyType, IPNetwork2 ipNetwork)
        {
            Add(new IPNetworkPolicy(policyType, ipNetwork));
        }

        public void Add(PolicyType policyType, string subnet)
        {
            Add(SubnetPolicy.Parse(policyType, subnet));
        }

        public void Add(string policyString)
        {
            Add(SubnetPolicy.Parse(policyString));
        }

        internal override string GetName()
        {
            return SectionName;
        }

        public static bool TryParse(string subnetsSection, out Subnets subnets)
        {
            subnets = default;

            if (string.IsNullOrWhiteSpace(subnetsSection))
                return false;

            var strings = ParseUtils.SplitAndSanitize(subnetsSection);
            return TryParse(strings, out subnets);
        }

        public static Subnets Parse(string subnetsSection)
        {
            if (subnetsSection == null) throw new ArgumentNullException(nameof(subnetsSection));

            if (!TryParse(subnetsSection, out var result))
                throw new FormatException(Resources.ParseFail);

            return result;
        }

        internal static bool TryParse(IEnumerable<string> strings, out Subnets subnets)
        {
            subnets = default;

            using (var enumerator = strings.GetEnumerator())
            {
                if (!enumerator.MoveNext())
                    return false;

                if (!TryParseHeader(enumerator.Current, SectionName, out var policyType))
                    return false;

                subnets = new Subnets(policyType);

                while (enumerator.MoveNext())
                {
                    if (!SubnetPolicy.TryParse(enumerator.Current, out var policy))
                        return false;

                    subnets.Add(policy);
                }
            }

            return true;
        }
    }
}
