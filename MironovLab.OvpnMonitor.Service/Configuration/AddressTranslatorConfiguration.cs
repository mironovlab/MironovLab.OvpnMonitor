using System;

namespace MironovLab.OvpnMonitor.Service.Configuration
{
    public class AddressTranslatorConfiguration
    {
        public ResolvingMethod Method { get; set; }
        public string Proto { get; set; }
        public int LocalPort { get; set; }
        public string TargetIPAddress { get; set; }
        public int TargetPort { get; set; }
        public string SourceIPAddress { get; set; }
        public TimeSpan WaitingTimeout { get; set; }
        public string SocatModuleName { get; set; }
    }

    public enum ResolvingMethod
    {
        None,
        Conntrack ,
        Socat,
    }
}
