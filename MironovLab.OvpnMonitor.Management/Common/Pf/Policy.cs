using System;
using System.Text;

namespace MironovLab.OpenVPN.Management.Common.Pf
{
    public abstract class Policy : IEquatable<Policy>
    {
        private const char PolicyTypeDeny = '-';
        private const char PolicyTypeAllow = '+';

        public PolicyType PolicyType { get; }

        internal Policy(PolicyType policyType)
        {
            PolicyType = policyType;
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            ToString(sb);
            return sb.ToString();
        }

        public bool Equals(Policy other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            return PolicyType == other.PolicyType;
        }

        public override bool Equals(object obj)
        {
            if (obj is null) return false;
            if (ReferenceEquals(this, obj)) return true;
            return obj.GetType() == GetType() && Equals((Policy)obj);
        }

        public override int GetHashCode()
        {
            return (int)PolicyType;
        }

        internal void ToString(StringBuilder sb)
        {
            switch (PolicyType)
            {
                case PolicyType.Drop:
                    sb.Append(PolicyTypeDeny);
                    break;
                case PolicyType.Accept:
                    sb.Append(PolicyTypeAllow);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(PolicyType), PolicyType, null);
            }

            ToStringInternal(sb);
        }

        protected abstract void ToStringInternal(StringBuilder sb);

        protected static bool TryPreParse(string ruleString, out PolicyType policyType, out string rule)
        {
            policyType = default;
            rule = null;
            ruleString = ruleString?.Trim();

            if (string.IsNullOrEmpty(ruleString))
                return false;

            rule = ruleString.Substring(1).Trim();

            if (string.IsNullOrEmpty(rule))
                return false;

            switch (ruleString[0])
            {
                case PolicyTypeDeny:
                    policyType = PolicyType.Drop;
                    return true;
                case PolicyTypeAllow:
                    policyType = PolicyType.Accept;
                    return true;
                default:
                    return false;
            }
        }
    }
}
