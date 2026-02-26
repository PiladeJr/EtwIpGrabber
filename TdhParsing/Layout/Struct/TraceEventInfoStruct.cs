using System.Runtime.InteropServices;
using EtwIpGrabber.EtwStructure.RealTimeConsumer.Native.Structures;

namespace EtwIpGrabber.TdhParsing.Layout.Struct
{
    [StructLayout(LayoutKind.Sequential)]
    public struct TRACE_EVENT_INFO
    {
        public Guid ProviderGuid;
        public Guid EventGuid;
        public EVENT_DESCRIPTOR EventDescriptor;
        public uint DecodingSource;

        public uint ProviderNameOffset;
        public uint LevelNameOffset;
        public uint ChannelNameOffset;
        public uint KeywordsNameOffset;
        public uint TaskNameOffset;
        public uint OpcodeNameOffset;
        public uint EventMessageOffset;
        public uint ProviderMessageOffset;
        public uint BinaryXMLOffset;
        public uint BinaryXMLSize;

        public uint EventNameOffset;   // union
        public uint EventAttributesOffset; // union

        public uint PropertyCount;
        public uint TopLevelPropertyCount;
        public uint Flags;
    }
}
