using EtwIpGrabber.EtwStructure.MetricsAndHealth;
using EtwIpGrabber.EtwStructure.RealTimeConsumer;
using EtwIpGrabber.EtwStructure.RealTimeConsumer.Native.Structures;

namespace EtwIpGrabber.EtwStructure.EventDispatcher
{
    public unsafe sealed class BoundedEventRingBuffer(
        int capacity,
        IMetricsCollector metrics) : IEventDispatcher
    {
        private readonly EventRecordSnapshot[] _buffer = new EventRecordSnapshot[capacity];

        private int _writeIndex;
        private int _readIndex;

        // Campi utilizzati per fasi di test
        private long _enqueued;
        private long _dequeued;

        private readonly int _capacity = capacity;
        private readonly IMetricsCollector _metrics = metrics;

        public long Enqueued => _enqueued;
        public long Dequeued => _dequeued;
        public int Depth => (_writeIndex - _readIndex + _capacity) % _capacity;

        public bool TryEnqueue(EventRecordSnapshot snapshot)
        {
            int next = (_writeIndex + 1) % _capacity;

            if (next == _readIndex)
            {
                _metrics.IncrementDroppedEvents();
                return false;
            }

            _buffer[_writeIndex] = snapshot;
            Volatile.Write(ref _writeIndex, next);
            Interlocked.Increment(ref _enqueued);

            return true;
        }

        public bool TryDequeue(out EventRecordSnapshot snapshot)
        {
            int currentWrite = Volatile.Read(ref _writeIndex);
            if (_readIndex == currentWrite)
            {
                snapshot = default;
                return false;
            }

            snapshot = _buffer[_readIndex];
            _readIndex = (_readIndex + 1) % _capacity;
            Interlocked.Increment(ref _dequeued);

            return true;
        }
    }

}
