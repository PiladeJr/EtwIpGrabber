using System.Runtime.InteropServices;

namespace EtwIpGrabber.Utils
{
    //deprecated struct. considera se eliminarla o mantenerla
    [StructLayout(LayoutKind.Sequential)]
    struct NetworkConnectionInfo
    {
        public ulong SrcAddr;
        public ulong DstAddr;
        public uint SrcPort;
        public uint DstPort;
        public uint ProcessId;
        public uint InterfaceIndex;
    }
}
