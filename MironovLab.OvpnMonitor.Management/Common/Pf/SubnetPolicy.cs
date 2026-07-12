using System;
using System.Net;
using System.Net.Sockets;

namespace MironovLab.OpenVPN.Management.Common.Pf
{
    public abstract class SubnetPolicy : Policy
    {
        internal SubnetPolicy(PolicyType policyType) : base(policyType)
        {
        }

        public static bool TryParse(string policyString, out SubnetPolicy rule)
        {
            rule = null;

            if (UnknownPolicy.TryParse(policyString, out var unknownRule))
            {
                rule = unknownRule;
                return true;
            }

            if (IPAddressPolicy.TryParse(policyString, out var ipAddressRule))
            {
                rule = ipAddressRule;
                return true;
            }

            if (IPNetworkPolicy.TryParse(policyString, out var ipNetworkRule))
            {
                rule = ipNetworkRule;
                return true;
            }

            return false;
        }

        public static bool TryParse(PolicyType policyType, string policyString, out SubnetPolicy rule)
        {
            rule = null;

            if (UnknownPolicy.TryParse(policyType, policyString, out var unknownRule))
            {
                rule = unknownRule;
                return true;
            }

            if (IPAddressPolicy.TryParse(policyType, policyString, out var ipAddressRule))
            {
                rule = ipAddressRule;
                return true;
            }

            if (IPNetworkPolicy.TryParse(policyType, policyString, out var ipNetworkRule))
            {
                rule = ipNetworkRule;
                return true;
            }

            return false;
        }

        public static SubnetPolicy Parse(string policyString)
        {
            if (policyString == null) throw new ArgumentNullException(nameof(policyString));

            if (!TryParse(policyString, out var result))
                throw new FormatException(Resources.ParseFail);

            return result;
        }

        public static SubnetPolicy Parse(PolicyType policyType, string policyString)
        {
            if (policyString == null) throw new ArgumentNullException(nameof(policyString));

            if (!TryParse(policyType, policyString, out var result))
                throw new FormatException(Resources.ParseFail);

            return result;
        }

        internal static bool IsIPAddressValid(IPAddress ipAddress)
        {
            var addressFamily = ipAddress.AddressFamily;

            return addressFamily == AddressFamily.InterNetwork ||
                   addressFamily == AddressFamily.InterNetworkV6;
        }
    }
}
