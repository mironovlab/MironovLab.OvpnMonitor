namespace MironovLab.OvpnMonitor.Service.Configuration
{
    public class AppConfiguration
    {
        public OpenVPNConfiguration[] OpenVPNConfigurations { get; set; }
        public MySqlConfiguration MySqlConfiguration { get; set; }
        public AddressTranslatorConfiguration AddressTranslatorConfiguration { get; set; }
    }
}
