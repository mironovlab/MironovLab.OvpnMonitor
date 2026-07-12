using System;
using System.Text;
using MironovLab.OpenVPN.Management.Core;

namespace MironovLab.OpenVPN.Management.Common.Pf
{
    public sealed class ClientPolicy : Policy, IEquatable<ClientPolicy>
    {
        public string CommonName { get; }

        public ClientPolicy(PolicyType policyType, string commonName) : base(policyType)
        {
            commonName = ParseUtils.SanitizeNewLine(commonName);
            if (string.IsNullOrWhiteSpace(commonName)) throw new ArgumentNullException(nameof(commonName));

            CommonName = commonName;
        }

        public bool Equals(ClientPolicy other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            return base.Equals(other) && string.Equals(CommonName, other.CommonName, StringComparison.OrdinalIgnoreCase);
        }

        public override bool Equals(object obj)
        {
            return ReferenceEquals(this, obj) || obj is ClientPolicy other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (base.GetHashCode() * 397) ^ StringComparer.OrdinalIgnoreCase.GetHashCode(CommonName);
            }
        }

        protected override void ToStringInternal(StringBuilder sb)
        {
            sb.Append(CommonName);
        }

        public static bool TryParse(string ruleString, out ClientPolicy rule)
        {
            rule = null;

            if (!TryPreParse(ruleString, out var ruleType, out ruleString))
                return false;

            return TryParse(ruleType, ruleString, out rule);
        }

        public static bool TryParse(PolicyType policyType, string ruleString, out ClientPolicy rule)
        {
            rule = null;

            if (string.IsNullOrEmpty(ruleString))
                return false;

            try
            {
                rule = new ClientPolicy(policyType, ruleString);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public static ClientPolicy Parse(string policyString)
        {
            if (policyString == null) throw new ArgumentNullException(nameof(policyString));

            if (!TryParse(policyString, out var result))
                throw new FormatException(Resources.ParseFail);

            return result;
        }

        public static ClientPolicy Parse(PolicyType policyType, string policyString)
        {
            if (policyString == null) throw new ArgumentNullException(nameof(policyString));

            if (!TryParse(policyType, policyString, out var result))
                throw new FormatException(Resources.ParseFail);

            return result;
        }
    }
}
