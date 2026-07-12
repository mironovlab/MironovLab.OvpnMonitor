using System;
using System.Net;

namespace MironovLab.OvpnMonitor.Service.AddressTranslation
{
    public readonly struct TranslationEntry
    {
        public readonly IPEndPoint Source;
        public readonly IPEndPoint Destination;

        public TranslationEntry(IPEndPoint source, IPEndPoint destination)
        {
            Source = source ?? throw new ArgumentNullException(nameof(source));
            Destination = destination ?? throw new ArgumentNullException(nameof(destination));
        }

        public override string ToString()
        {
            return $"{Source} -> {Destination}";
        }
    }
}
