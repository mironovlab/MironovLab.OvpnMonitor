namespace MironovLab.OpenVPN.Management.CommandResults
{
    public class PKCS11IdEntry
    {
        public int EntryNumber { get; }
        public string ID { get; }
        public byte[] Blob { get; }

        internal PKCS11IdEntry(int entryNumber, string id, byte[] blob)
        {
            EntryNumber = entryNumber;
            ID = id;
            Blob = blob;
        }
    }
}
