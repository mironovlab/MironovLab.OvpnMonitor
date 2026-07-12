using System;
using System.Collections.Generic;

namespace MironovLab.OpenVPN.Management.CommandResults
{
    public class Version
    {
        public string VersionString { get; }
        public string ProductName { get; }
        public System.Version ProductVersion { get; }
        public string BuildSource { get; }
        public string BuildTarget { get; }
        public IReadOnlyCollection<string> Modules { get; }
        public DateTime BuiltOn { get; }
        public System.Version ManagementVersion { get; }

        internal Version(string versionString, string productName, System.Version productVersion, string buildSource, string buildTarget, IReadOnlyCollection<string> modules, DateTime builtOn, System.Version managementVersion)
        {
            VersionString = versionString;
            ProductName = productName;
            ProductVersion = productVersion;
            BuildSource = buildSource;
            BuildTarget = buildTarget;
            Modules = modules;
            BuiltOn = builtOn;
            ManagementVersion = managementVersion;
        }
    }
}
