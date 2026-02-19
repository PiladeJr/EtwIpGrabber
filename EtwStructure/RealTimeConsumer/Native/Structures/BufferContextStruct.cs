using System;
using System.Runtime.InteropServices;

namespace EtwIpGrabber.EtwStructure.RealTimeConsumer.Native.Structures
{
    [StructLayout(LayoutKind.Sequential)]
    public struct ETW_BUFFER_CONTEXT
    {
        public ushort ProcessorNumber;
        public ushort Alignment;
        public ushort LoggerId;
    }
}
