namespace EtwIpGrabber.TcpLifeCycleReconstruction.Storage
{
    using EtwIpGrabber.TcpLifeCycleReconstruction.Abstractions;
    using EtwIpGrabber.TcpLifeCycleReconstruction.Models;
    using EtwIpGrabber.TdhParsing.Normalization.Models;
    using System.Collections.Concurrent;

    internal sealed class ConcurrentTcpFlowStore(
        int initialCapacity = 4096) : ITcpFlowStore
    {
        private readonly ConcurrentDictionary<TcpFlowKey, TcpFlowInstance>
            _flows = new( concurrencyLevel: Environment.ProcessorCount,
                capacity: initialCapacity);

        public TcpFlowInstance GetOrCreate(
            in TcpFlowKey key,
            in TcpEvent firstEvent)
        {
            // assegnazioni necessarie solo per evitare errori di cattura di variabili
            // nei lambda, nonostante i parametri siano in readonly struct
            var k = key; 
            var fe = firstEvent; 
            return _flows.GetOrAdd( key, _ => new TcpFlowInstance(k, fe));
        }

        public bool TryRemove( in TcpFlowKey key, out TcpFlowInstance? flow)
        {
            return _flows.TryRemove(key, out flow);
        }

        public IEnumerable<TcpFlowInstance> EnumerateActive()
            => _flows.Values;
    }
}
