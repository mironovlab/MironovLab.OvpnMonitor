using System;
using System.Text;

namespace MironovLab.OpenVPN.Management.Common.Pf
{
    public sealed class UnknownPolicy : SubnetPolicy
    {
        internal const string Unknown = "unknown";

        public UnknownPolicy(PolicyType policyType) : base(policyType)
        {
        }

        protected override void ToStringInternal(StringBuilder sb)
        {
            sb.Append(Unknown);
        }

        public static bool TryParse(string policyString, out UnknownPolicy rule)
        {
            rule = null;

            if (!TryPreParse(policyString, out var ruleType, out policyString))
                return false;

            return TryParse(ruleType, policyString, out rule);
        }

        public static bool TryParse(PolicyType policyType, string policyString, out UnknownPolicy rule)
        {
            rule = null;

            if (string.IsNullOrEmpty(policyString))
                return false;

            if (!policyString.Equals(Unknown, StringComparison.OrdinalIgnoreCase))
                return false;

            rule = new UnknownPolicy(policyType);
            return true;
        }

        public new static UnknownPolicy Parse(string policyString)
        {
            if (policyString == null) throw new ArgumentNullException(nameof(policyString));

            if (!TryParse(policyString, out var result))
                throw new FormatException(Resources.ParseFail);

            return result;
        }

        public new static UnknownPolicy Parse(PolicyType policyType, string policyString)
        {
            if (policyString == null) throw new ArgumentNullException(nameof(policyString));

            if (!TryParse(policyType, policyString, out var result))
                throw new FormatException(Resources.ParseFail);

            return result;
        }
    }
}
