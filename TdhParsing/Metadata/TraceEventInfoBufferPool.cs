using System.Collections.Concurrent;

namespace EtwIpGrabber.TdhParsing.Metadata
{
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