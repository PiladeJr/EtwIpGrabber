using System.Runtime.InteropServices;

namespace EtwIpGrabber.EtwStructure.RealTimeConsumer.Native.Structures
{
    [StructLayout(LayoutKind.Sequential)]
    public struct EVENT_TRACE
    {
        public EVENT_HEADER Header;
        public uint InstanceId;
        public uint ParentInstanceId;
        public Guid ParentGuid;
        public IntPtr MofData;
        public uint MofLength;
        public uint ClientContext;
    }
}
