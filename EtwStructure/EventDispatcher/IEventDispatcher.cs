namespace EtwIpGrabber.EtwStructure.EventDispatcher
{
    public interface IEventDispatcher
    {
        bool TryEnqueue(EVENT_RECORD* record);
    }
}
