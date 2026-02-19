using EtwIpGrabber.EtwStructure.RealTimeConsumer.Native.Structures;
using System.Runtime.InteropServices;

namespace EtwIpGrabber.EtwStructure.RealTimeConsumer.Native
{
    [UnmanagedFunctionPointer(CallingConvention.Winapi)]
    public unsafe delegate void EventRecordCallback(
        EVENT_RECORD* eventRecord);
}
