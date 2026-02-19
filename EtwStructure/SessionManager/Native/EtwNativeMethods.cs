using System.Runtime.InteropServices;

namespace EtwIpGrabber.EtwStructure.SessionManager.Native
{
    [StructLayout(LayoutKind.Explicit)]
    public struct WNODE_HEADER
    {
        [FieldOffset(0)]
        public uint BufferSize;

        [FieldOffset(4)]
        public uint ProviderId;

        [FieldOffset(8)]
        public ulong HistoricalContext;

        [FieldOffset(16)]
        public long TimeStamp;

        [FieldOffset(24)]
        public Guid Guid;

        [FieldOffset(40)]
        public uint ClientContext;

        [FieldOffset(44)]
        public uint Flags;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 8)]
    public struct EVENT_TRACE_PROPERTIES
    {
        public WNODE_HEADER Wnode;
        public uint BufferSize;
        public uint MinimumBuffers;
        public uint MaximumBuffers;
        public uint MaximumFileSize;
        public uint LogFileMode;
        public uint FlushTimer;
        public uint EnableFlags;
        public int AgeLimit;
        public uint NumberOfBuffers;
        public uint FreeBuffers;
        public uint EventsLost;
        public uint BuffersWritten;
        public uint LogBuffersLost;
        public uint RealTimeBuffersLost;
        public IntPtr LoggerThreadId;
        public uint LogFileNameOffset;
        public uint LoggerNameOffset;
    }

    /// <summary>
    /// Wrapper Win32 per le chiamate alle API ETW di gestione delle sessioni.
    /// </summary>
    /// <remarks>
    /// - StartTrace(): avvia una sessione 
    /// <br/> <see href="https://learn.microsoft.com/en-us/windows/win32/api/evntrace/nf-evntrace-starttracea"/>
    /// <br/>
    /// - ControlTrace(): usato per chiudere una sessione in combinazione con EVENT_CONTROL_TRACE_STOP
    /// <br/> <see href="https://learn.microsoft.com/en-us/windows/win32/api/evntrace/nf-evntrace-controltracea"/>
    /// </remarks>
    internal static class EtwNativeMethods
    {
        [DllImport("advapi32.dll", CharSet = CharSet.Unicode)]
        public static extern uint StartTrace(
        out ulong sessionHandle,
        string sessionName,
        IntPtr properties);

        [DllImport("advapi32.dll", CharSet = CharSet.Unicode)]
        public static extern uint ControlTrace(
            ulong sessionHandle,
            string sessionName,
            IntPtr properties,
            uint controlCode);
    }
}
