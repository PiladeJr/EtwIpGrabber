using EtwIpGrabber.TcpLifeCycleReconstruction.Abstractions;
using EtwIpGrabber.TcpLifeCycleReconstruction.Models;
using EtwIpGrabber.TdhParsing.Normalization.Models;

namespace EtwIpGrabber.TcpLifeCycleReconstruction.Tracking
{
    internal sealed class DefaultTcpFlowTracker(
        ITcpFlowStore store,TcpFlowReuseGuard reuseGuard) : ITcpFlowTracker
    {
        private readonly ITcpFlowStore _store = store;
        private readonly TcpFlowReuseGuard _reuseGuard = reuseGuard;

        public TcpFlowInstance GetOrCreate(
            in TcpEvent evt)
        {
            var key = TcpFlowKey.FromEvent(evt);

            var flow = _store.GetOrCreate(key, evt);

            // tuple reuse detection
            if (_reuseGuard.ShouldSplit(flow, evt.TimestampUtc))
            {
                _store.TryRemove(key, out _);

                flow = _store.GetOrCreate(key, evt);
            }

            return flow;
        }

        public bool ShouldSplit(
            TcpFlowInstance flow,
            DateTime ts)
        {
            return _reuseGuard.ShouldSplit(flow, ts);
        }
    }
}
