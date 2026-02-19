using EtwIpGrabber.EtwStructure.RealTimeConsumer.Native.Structures;

namespace EtwIpGrabber.EtwStructure.EventDispatcher
{
    public interface IEventDispatcher
    {
        unsafe bool TryEnqueue(EVENT_RECORD* record);
    }
}
