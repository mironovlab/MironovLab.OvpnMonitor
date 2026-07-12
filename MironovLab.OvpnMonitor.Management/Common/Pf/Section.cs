using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace MironovLab.OpenVPN.Management.Common.Pf
{
    public abstract class Section<T> : IEnumerable<T> where T : Policy
    {
        public PolicyType DefaultPolicyType { get; set; }
        public abstract ICollection<T> Policies { get; }

        internal Section(PolicyType defaultPolicyType)
        {
            DefaultPolicyType = defaultPolicyType;
        }

        public void Add(T policy)
        {
            if (policy == null) throw new ArgumentNullException(nameof(policy));
            Policies.Add(policy);
        }

        public sealed override string ToString()
        {
            var sb = new StringBuilder();
            ToString(sb);
            return sb.ToString();
        }

        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            return Policies.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable)Policies).GetEnumerator();
        }

        internal abstract string GetName();

        internal void ToString(StringBuilder sb)
        {
            string type;
            switch(DefaultPolicyType)
            {
                case PolicyType.Drop:
                    type = SectionHelper.PolicyTypeDrop;
                    break;
                case PolicyType.Accept:
                    type = SectionHelper.PolicyTypeAccept;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(DefaultPolicyType), DefaultPolicyType, null);
            }

            sb.Append(Constants.SectionEnclosureStart);
            sb.Append(GetName());
            sb.Append(Constants.WhiteSpace);
            sb.Append(type);
            sb.Append(Constants.SectionEnclosureEnd);
            sb.Append(Constants.NewLine);

            foreach (var rule in Policies)
            {
                rule.ToString(sb);
                sb.Append(Constants.NewLine);
            }
        }

        protected static bool TryParseHeader(string sectionText, string sectionName, out PolicyType defaultPolicyType)
        {
            return SectionHelper.TryParseHeader(sectionText, out var sectionNameParsed, out defaultPolicyType)
                   && sectionNameParsed.Equals(sectionName, StringComparison.OrdinalIgnoreCase);
        }
    }
}
