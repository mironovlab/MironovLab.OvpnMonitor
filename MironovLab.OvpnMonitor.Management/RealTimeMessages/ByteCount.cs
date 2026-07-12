namespace MironovLab.OpenVPN.Management.RealTimeMessages
{
    public class ByteCount : RealTimeMessage
    {
        public override RealTimeMessageType Type => RealTimeMessageType.ByteCount;
        public long BytesIn { get; }
        public long BytesOut { get; }

        internal ByteCount(long bytesIn, long bytesOut)
        {
            BytesIn = bytesIn;
            BytesOut = bytesOut;
        }
    }
}
