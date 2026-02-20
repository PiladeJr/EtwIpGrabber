using EtwIpGrabber.EtwStructure.RealTimeConsumer.Native.Structures;

namespace EtwIpGrabber.EtwStructure.RealTimeConsumer
{
    public unsafe struct EventRecordSnapshot
    {
        public EVENT_HEADER Header;
        public ushort ExtendedDataCount;
        public ushort UserDataLength;
        public byte[] UserData;
        public byte[] ExtendedData;
    }
}