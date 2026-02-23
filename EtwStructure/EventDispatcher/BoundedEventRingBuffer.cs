using EtwIpGrabber.EtwStructure.MetricsAndHealth;
using EtwIpGrabber.EtwStructure.RealTimeConsumer;

namespace EtwIpGrabber.EtwStructure.EventDispatcher
{
    /// <summary>
    /// Fornisce un bounded ring buffer per salvare record di eventi. Permette operazioni di enqueue e dequeue in tempo costante, con gestione della concorrenza e tracking degli eventi scartati.
    /// </summary>
    /// Questo buffer implementa un modello SPSC
    /// (Single Producer / Single Consumer):
    /// <list type="bullet">
    ///   <item><description>Producer: thread ETW (ProcessTrace);</description></item>
    ///   <item><description>Consumer: reconstruction pipeline.</description></item>
    /// </list>
    /// 
    /// L'assenza di sincronizzazione completa su _readIndex
    /// è sicura solo in questo modello.
    /// L'uso in scenari MPMC richiederebbe barriere di memoria
    /// aggiuntive o atomic CAS.
    /// <param name="capacity">il massimo numero di record che il buffer può contenere.</param>
    /// <param name="metrics">Un'istanza di IMetricsCollector usata per tracciare le metriche relative al processing di eventi. quali il numero di eventi droppati.</param>
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
