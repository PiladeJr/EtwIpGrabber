using System.Runtime.InteropServices;

namespace EtwIpGrabber.EtwStructure.RealTimeConsumer.Native.Structures
{
    [StructLayout(LayoutKind.Sequential)]
    public struct EVENT_RECORD
    {
        public EVENT_HEADER EventHeader;
        public ETW_BUFFER_CONTEXT BufferContext;
        public ushort ExtendedDataCount;
        public ushort UserDataLength;
        public IntPtr ExtendedData;
        public IntPtr UserData;
        public IntPtr UserContext;
    }

    public struct EVENT_HEADER_EXTENDED_DATA_ITEM
    {
        public ushort Reserved1;
        public ushort ExtType;
        public ushort Linkage;
        public ushort DataSize;
        public ulong DataPtr;
    }
}
