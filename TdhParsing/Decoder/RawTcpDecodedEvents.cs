namespace EtwIpGrabber.TdhParsing.Decoder
{
    internal struct RawTcpDecodedEvent
    {
        public uint LocalAddress;
        public uint RemoteAddress;
        public ushort LocalPort;
        public ushort RemotePort;
        public ushort AddressFamily;
        public uint ProcessId;
        public byte Direction;
        public byte TcpFlags;

        public bool IsIpv4;
    }
}
