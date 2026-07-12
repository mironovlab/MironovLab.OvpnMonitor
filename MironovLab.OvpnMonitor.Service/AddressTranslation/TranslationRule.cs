namespace MironovLab.OvpnMonitor.Service.AddressTranslation
{
    public readonly struct TranslationRule
    {
        public readonly TranslationEntry Incoming;
        public readonly TranslationEntry Outgoing;

        public TranslationRule(TranslationEntry incoming, TranslationEntry outgoing)
        {
            Incoming = incoming;
            Outgoing = outgoing;
        }

        public override string ToString()
        {
            return $"In: {Incoming}, Out: {Outgoing}";
        }
    }
}
