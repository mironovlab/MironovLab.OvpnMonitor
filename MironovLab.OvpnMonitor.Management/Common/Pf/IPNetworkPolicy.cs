using System;
using System.Net;
using System.Text;

namespace MironovLab.OpenVPN.Management.Common.Pf
{
    public sealed class IPNetworkPolicy : SubnetPolicy, IEquatable<IPNetworkPolicy>
    {
        public IPNetwork2 IPNetwork { get; }

        public IPNetworkPolicy(PolicyType policyType, IPNetwork2 ipNetwork) : base(policyType)
        {
            IPNetwork = ipNetwork ?? throw new ArgumentNullException(nameof(ipNetwork));
            if (!IsIPAddressValid(ipNetwork.Network))
                throw new ArgumentException(Resources.IPNetworkRule_WrongAddressFamily);
        }

        public bool Equals(IPNetworkPolicy other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            return base.Equals(other) && IPNetwork.Equals(other.IPNetwork);
        }

        public override bool Equals(object obj)
        {
            return ReferenceEquals(this, obj) || obj is IPNetworkPolicy other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (base.GetHashCode() * 397) ^ IPNetwork.GetHashCode();
            }
        }

        protected override void ToStringInternal(StringBuilder sb)
        {
            sb.Append(IPNetwork);
        }

        public static bool TryParse(string policyString, out IPNetworkPolicy policy)
        {
            policy = null;

            if (!TryPreParse(policyString, out var policyType, out policyString))
                return false;

            return TryParse(policyType, policyString, out policy);
        }

        public static bool TryParse(PolicyType policyType, string policyString, out IPNetworkPolicy policy)
        {
            policy = null;

            if (string.IsNullOrEmpty(policyString))
                return false;

            if (!IPNetwork2.TryParse(policyString, out var ipNetwork))
                return false;

            try
            {
                policy = new IPNetworkPolicy(policyType, ipNetwork);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public new static IPNetworkPolicy Parse(string policyString)
        {
            if (policyString == null) throw new ArgumentNullException(nameof(policyString));

            if (!TryParse(policyString, out var result))
                throw new FormatException(Resources.ParseFail);

            return result;
        }

        public new static IPNetworkPolicy Parse(PolicyType policyType, string policyString)
        {
            if (policyString == null) throw new ArgumentNullException(nameof(policyString));

            if (!TryParse(policyType, policyString, out var result))
                throw new FormatException(Resources.ParseFail);

            return result;
        }
    }
}
