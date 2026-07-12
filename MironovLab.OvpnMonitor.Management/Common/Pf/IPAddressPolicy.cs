using System;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace MironovLab.OpenVPN.Management.Common.Pf
{
    public sealed class IPAddressPolicy : SubnetPolicy, IEquatable<IPAddressPolicy>
    {
        public IPAddress IPAddress { get; }

        public IPAddressPolicy(PolicyType policyType, IPAddress ipAddress) : base(policyType)
        {
            IPAddress = ipAddress ?? throw new ArgumentNullException(nameof(ipAddress));
            if (!IsIPAddressValid(ipAddress))
                throw new ArgumentException(Resources.IPAddressRule_WrongAddressFamily);
        }

        public bool Equals(IPAddressPolicy other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            return base.Equals(other) && IPAddress.Equals(other.IPAddress);
        }

        public override bool Equals(object obj)
        {
            return ReferenceEquals(this, obj) || obj is IPAddressPolicy other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (base.GetHashCode() * 397) ^ IPAddress.GetHashCode();
            }
        }

        protected override void ToStringInternal(StringBuilder sb)
        {
            sb.Append(IPAddress);
        }

        public static bool TryParse(string policyString, out IPAddressPolicy policy)
        {
            policy = null;

            if (!TryPreParse(policyString, out var ruleType, out policyString))
                return false;

            return TryParse(ruleType, policyString, out policy);
        }

        public static bool TryParse(PolicyType policyType, string policyString, out IPAddressPolicy policy)
        {
            policy = null;

            if (string.IsNullOrEmpty(policyString))
                return false;

            if (!IPAddress.TryParse(policyString, out var ipAddress))
            {
                if (IPNetwork2.TryParse(policyString, out var ipNetwork))
                {
                    switch (ipNetwork.AddressFamily)
                    {
                        case AddressFamily.InterNetwork when ipNetwork.Cidr != 32:
                            return false;
                        case AddressFamily.InterNetworkV6 when ipNetwork.Cidr != 128:
                            return false;
                        default:
                            ipAddress = ipNetwork.Network;
                            break;
                    }
                }
                else
                {
                    return false;
                }
            }

            try
            {
                policy = new IPAddressPolicy(policyType, ipAddress);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public new static IPAddressPolicy Parse(string policyString)
        {
            if (policyString == null) throw new ArgumentNullException(nameof(policyString));

            if (!TryParse(policyString, out var result))
                throw new FormatException(Resources.ParseFail);

            return result;
        }

        public new static IPAddressPolicy Parse(PolicyType policyType, string policyString)
        {
            if (policyString == null) throw new ArgumentNullException(nameof(policyString));

            if (!TryParse(policyType, policyString, out var result))
                throw new FormatException(Resources.ParseFail);

            return result;
        }
    }
}
