using EtwIpGrabber.TcpLifeCycleReconstruction.Abstractions;
using EtwIpGrabber.TcpLifeCycleReconstruction.Models;
using EtwIpGrabber.TdhParsing.Normalization.Models;

namespace EtwIpGrabber.TcpLifeCycleReconstruction.Reconstruction
{
    internal sealed class DefaultTcpLifecycleReconstructor
        : ITcpLifecycleReconstructor
    {
        private readonly ITcpFlowTracker _tracker;
        private readonly ITcpFlowStore _store;
        private readonly ITcpConnectionFinalizer _finalizer;

        public DefaultTcpLifecycleReconstructor(
            ITcpFlowTracker tracker,
            ITcpFlowStore store,
            ITcpConnectionFinalizer finalizer)
        {
            _tracker = tracker;
            _store = store;
            _finalizer = finalizer;
        }

        public void Process(in TcpEvent evt)
        {
            var flow = _tracker.GetOrCreate(evt);

            var shouldFinalize = flow.Apply(evt);

            if (!shouldFinalize)
                return;

            var lifecycle = _finalizer.Finalize(flow);

            _store.TryRemove(flow.Key, out _);

            OnConnectionFinalized(lifecycle);
        }

        private void OnConnectionFinalized(
            TcpConnectionLifecycle lifecycle)
        {
            // hook verso persistence pipeline
            // (SQLite writer / channel / etc.)
        }
    }
}
