using System.Collections.Concurrent;

namespace EtwIpGrabber.TdhParsing.Metadata
{
    /// <summary>
    /// Pool di caching per i buffer TRACE_EVENT_INFO risolti runtime.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Associa ad ogni TdhEventKey il relativo metadata TDH
    /// </para>
    /// Riduce drasticamente:
    /// <list type="bullet">
    ///     <item><description>manifest parsing runtime</description></item>
    ///     <item><description>chiamate ripetute a TdhGetEventInformation</description></item>
    /// </list>
    /// migliorando:
    /// <list type="bullet">
    ///     <item><description>latenza di decoding</description></item>
    ///     <item><description>throughput sotto carico ETW realtime</description></item>
    /// </list>
    ///
    /// Il lifetime dei buffer unmanaged è gestito centralmente
    /// e rilasciato alla Dispose del pool.
    /// </remarks>
    internal sealed class TraceEventInfoBufferPool : IDisposable
    {
        private readonly ConcurrentDictionary<
            TdhEventKey,
            TraceEventInfoHandle> _cache = new();

        public unsafe TraceEventInfoHandle GetOrAdd(
            in TdhEventKey key,
            Func<TraceEventInfoHandle> factory)
        {
            return _cache.GetOrAdd(key, _ => factory());
        }

        public void Dispose()
        {
            foreach (var handle in _cache.Values)
            {
                handle.Dispose();
            }
            _cache.Clear();
        }
    }
}