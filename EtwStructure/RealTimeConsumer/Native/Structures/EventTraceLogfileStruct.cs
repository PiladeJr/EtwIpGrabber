using System.Runtime.InteropServices;

namespace EtwIpGrabber.EtwStructure.RealTimeConsumer.Native
{
    [StructLayout(LayoutKind.Explicit, CharSet = CharSet.Unicode)]
    public unsafe struct EVENT_TRACE_LOGFILE
    {
        [FieldOffset(0)]
        public string LogFileName;

        [FieldOffset(8)]
        public string LoggerName;

        [FieldOffset(16)]
        public long CurrentTime;

        [FieldOffset(24)]
        public uint BuffersRead;

        // UNION 1
        [FieldOffset(28)]
        public uint LogFileMode;

        [FieldOffset(28)]
        public uint ProcessTraceMode;

        [FieldOffset(32)]
        public EVENT_TRACE CurrentEvent;

        [FieldOffset(72)]
        public TRACE_LOGFILE_HEADER LogfileHeader;

        [FieldOffset(328)]
        public IntPtr BufferCallback;

        [FieldOffset(336)]
        public uint BufferSize;

        [FieldOffset(340)]
        public uint Filled;

        [FieldOffset(344)]
        public uint EventsLost;

        // UNION 2
        [FieldOffset(352)]
        public IntPtr EventCallback;

        [FieldOffset(352)]
        public IntPtr EventRecordCallback;

        [FieldOffset(360)]
        public uint IsKernelTrace;

        [FieldOffset(368)]
        public IntPtr Context;
    }
}
