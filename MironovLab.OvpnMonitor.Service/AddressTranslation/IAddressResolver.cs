using System.Net;

namespace MironovLab.OvpnMonitor.Service.AddressTranslation
{
    internal interface IAddressResolver
    {
        IPEndPoint Translate(IPEndPoint realAddress);
    }
}
