using System;
using System.Runtime.InteropServices;

namespace EtwIpGrabber.EtwStructure.RealTimeConsumer.Native.Structures
{
    [StructLayout(LayoutKind.Sequential)]
    public struct ETW_BUFFER_CONTEXT
    {
        public byte ProcessorNumber;
        public byte Alignment;
        public ushort LoggerId;
    }
}
