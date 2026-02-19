using System.Runtime.InteropServices;

namespace EtwIpGrabber.EtwStructure.RealTimeConsumer.Native
{
    internal static class NativeEtwConsumer
    {
        [DllImport("advapi32.dll", CharSet = CharSet.Unicode)]
        public static extern ulong OpenTrace(
            ref EVENT_TRACE_LOGFILE logfile);

        [DllImport("advapi32.dll")]
        public static extern uint ProcessTrace(
            ulong[] handleArray,
            uint handleCount,
            IntPtr startTime,
            IntPtr endTime);

        [DllImport("advapi32.dll")]
        public static extern uint CloseTrace(ulong traceHandle);
    }
}
