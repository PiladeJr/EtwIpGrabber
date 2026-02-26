namespace EtwIpGrabber.TdhParsing.Layout
{
    internal sealed class TcpEventLayout
    {
        public int AddressFamilyIndex = -1;
        public int LocalAddressIndex = -1;
        public int RemoteAddressIndex = -1;
        public int LocalPortIndex = -1;
        public int RemotePortIndex = -1;
        public int ProcessIdIndex = -1;
        public int DirectionIndex = -1;
        public int TcpFlagsIndex = -1;

        public bool DirectionHasMap;
        public bool Supported;
    }
}
