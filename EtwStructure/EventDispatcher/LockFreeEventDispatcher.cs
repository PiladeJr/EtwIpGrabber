using System.Collections.Concurrent;

namespace EtwIpGrabber.EtwStructure.EventDispatcher
{
    public sealed class LockFreeEventDispatcher : IEventDispatcher
    {
        private readonly ConcurrentQueue<IntPtr> _queue;

        public unsafe bool TryEnqueue(EVENT_RECORD* record)
        {
            _queue.Enqueue((IntPtr)record);
            return true;
        }
    }
}
