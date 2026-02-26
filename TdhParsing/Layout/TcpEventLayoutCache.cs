using EtwIpGrabber.TdhParsing.Metadata;
using System.Collections.Concurrent;


namespace EtwIpGrabber.TdhParsing.Layout
{
    internal sealed class TcpEventLayoutCache
    {
        private readonly ConcurrentDictionary<
            TdhEventKey,
            TcpEventLayout> _cache = new();

        public TcpEventLayout GetOrAdd(
            in TdhEventKey key,
            Func<TdhEventKey, TcpEventLayout> factory)
        {
            return _cache.GetOrAdd(
                key,
                factory);
        }
    }
}
