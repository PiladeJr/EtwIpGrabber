using EtwIpGrabber.TdhParsing.Metadata;
using System.Collections.Concurrent;


namespace EtwIpGrabber.TdhParsing.Layout
{
    /// <summary>
    /// Cache runtime dei layout TCPIP evento-specifici.
    ///
    /// <para>
    /// Ogni layout è identificato da:
    /// </para>
    /// <list type="bullet">
    /// <item>ProviderId</item>
    /// <item>EventId</item>
    /// <item>Version</item>
    /// <item>Opcode</item>
    /// </list>
    ///
    /// <para>
    /// (incapsulati in <see cref="TdhEventKey"/>)
    /// </para>
    ///
    /// <para>
    /// Il caching è fondamentale per:
    /// </para>
    /// <list type="bullet">
    /// <item>Supporto Windows 10 / Windows 11</item>
    /// <item>Manifest version drift</item>
    /// <item>Riduzione chiamate TDH runtime</item>
    /// </list>
    ///
    /// Evita di ricostruire il layout ad ogni evento,
    /// </summary>
    internal sealed class TcpEventLayoutCache
    {
        private readonly ConcurrentDictionary<TdhEventKey,TcpEventLayout> _cache = new();

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
