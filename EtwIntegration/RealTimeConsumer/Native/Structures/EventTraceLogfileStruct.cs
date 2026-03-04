using System.Runtime.InteropServices;

namespace EtwIpGrabber.EtwIntegration.RealTimeConsumer.Native.Structures
{
    [StructLayout(LayoutKind.Explicit, CharSet = CharSet.Unicode)]
    public struct EVENT_TRACE_LOGFILE
    {
        [FieldOffset(0)] public IntPtr LogFileName;
        [FieldOffset(8)] public IntPtr LoggerName;
        [FieldOffset(16)] public long CurrentTime;
        [FieldOffset(24)] public uint BuffersRead;
        [FieldOffset(28)] public uint ProcessTraceMode;

        [FieldOffset(32)]
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 88)]
        public byte[] CurrentEventRecord;

        [FieldOffset(120)]
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 280)]
        public byte[] LogfileHeader;

        [FieldOffset(400)] public IntPtr BufferCallback;
        [FieldOffset(408)] public uint BufferSize;
        [FieldOffset(412)] public uint Filled;
        [FieldOffset(416)] public uint EventsLost;

        [FieldOffset(424)] public IntPtr EventCallback;
        [FieldOffset(424)] public IntPtr EventRecordCallback;

        [FieldOffset(432)] public uint IsKernelTrace;
        [FieldOffset(440)] public IntPtr Context;
    }
}
