using System;

namespace MironovLab.OpenVPN.Management.Common.Pf
{
    internal class SectionHelper
    {
        public const string PolicyTypeDrop = "DROP";
        public const string PolicyTypeAccept = "ACCEPT";

        public static bool TryParseHeader(string header, out string sectionName, out PolicyType defaultPolicyType)
        {
            sectionName = null;
            defaultPolicyType = default;

            if (!header.StartsWith(Constants.SectionEnclosureStart.ToString()) || !header.EndsWith(Constants.SectionEnclosureEnd.ToString()))
                return false;

            header = header.Substring(1, header.Length - 2).Trim();
            if (string.IsNullOrWhiteSpace(header))
                return false;

            var splitHeader = header.Split(new[] { Constants.WhiteSpace }, StringSplitOptions.RemoveEmptyEntries);
            sectionName = splitHeader[0];
            if (splitHeader.Length < 2)
                return false;

            var sectionTypeStr = splitHeader[1];

            if (sectionTypeStr.Equals(PolicyTypeDrop, StringComparison.OrdinalIgnoreCase) ||
                sectionTypeStr.Equals("DENY", StringComparison.OrdinalIgnoreCase))
            {
                defaultPolicyType = PolicyType.Drop;
                return true;
            }

            if (sectionTypeStr.Equals(PolicyTypeAccept, StringComparison.OrdinalIgnoreCase))
            {
                defaultPolicyType = PolicyType.Accept;
                return true;
            }

            return false;
        }
    }
}
