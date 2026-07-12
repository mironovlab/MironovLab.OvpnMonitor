namespace MironovLab.OpenVPN.Management.RealTimeMessages
{
    public class ByteCountCli : ByteCount
    {
        public override RealTimeMessageType Type => RealTimeMessageType.ByteCountCli;
        public int ClientID { get; }

        internal ByteCountCli(int cid, long bytesIn, long bytesOut) : base(bytesIn, bytesOut)
        {
            ClientID = cid;
        }
    }
}
