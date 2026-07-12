using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Microsoft.Extensions.Logging;
using MironovLab.OpenVPN.Management.Core;

namespace MironovLab.OpenVPN.Management.CommandResults.Parsers
{
    internal class VersionParser : ParserBase<Version>
    {
        private static readonly IFormatProvider DateFormatProvider = CultureInfo.CreateSpecificCulture("en-US");

        public VersionParser(ILoggerFactory loggerFactory) : base(loggerFactory)
        {
        }

        protected override Version Parse(IEnumerable<string> lines)
        {
            var versionString = string.Empty;
            var productName = string.Empty;
            System.Version productVersion = null;
            var buildSource = string.Empty;
            var buildTarget = string.Empty;
            List<string> modules = null;
            var builtOn = DateTime.MinValue;
            System.Version managementVersion = null;

            foreach (var line in lines)
            {
                ParseUtils.ParseSimpleMessage(line, out var messageType, out var version);

                switch (messageType)
                {
                    case "OpenVPN Version":
                        versionString = version;
                        var dataParts = ParseUtils.SplitDataParts(version, Constants.WhiteSpace, '[', ']');
                        productName = dataParts[0];
                        productVersion = System.Version.Parse(dataParts[1]);
                        var pos = 2;
                        if (dataParts[2].StartsWith("git", StringComparison.OrdinalIgnoreCase))
                            buildSource = dataParts[pos++];
                        buildTarget = dataParts[pos++];
                        var endModulesPos = dataParts.IndexOf("built", pos);
                        if (endModulesPos < 0)
                            break;
                        
                        modules = new List<string>(endModulesPos - pos);
                        for (var i = pos; i < endModulesPos; i++)
                            modules.Add(dataParts[i]);
                        builtOn = DateTime.Parse(string.Join(Constants.WhiteSpace.ToString(), dataParts.Skip(dataParts.Count - 3)), DateFormatProvider);
                        break;
                    case "Management Version":
                        if (int.TryParse(version, out var verMajor))
                            managementVersion = new System.Version(verMajor, 0);
                        else if (!System.Version.TryParse(version, out managementVersion))
                            managementVersion = new System.Version();
                        break;
                }
            }

            return new Version(versionString, productName, productVersion, buildSource, buildTarget, (IReadOnlyCollection<string>)modules ?? Array.Empty<string>(), builtOn, managementVersion);
        }
    }
}
