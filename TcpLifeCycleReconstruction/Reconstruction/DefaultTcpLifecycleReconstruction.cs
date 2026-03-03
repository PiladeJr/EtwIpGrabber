using EtwIpGrabber.TcpLifeCycleReconstruction.Abstractions;
using EtwIpGrabber.TcpLifeCycleReconstruction.Models;
using EtwIpGrabber.TdhParsing.Normalization.Models;
using System.Threading.Channels;

namespace EtwIpGrabber.TcpLifeCycleReconstruction.Reconstruction
{
    internal sealed class DefaultTcpLifecycleReconstructor(
        ITcpFlowTracker tracker,
        ITcpFlowStore store,
        ITcpConnectionFinalizer finalizer,
        ChannelWriter<TcpConnectionLifecycle> output,
        ILogger<DefaultTcpLifecycleReconstructor> logger)
                : ITcpLifecycleReconstructor
    {
        private readonly ITcpFlowTracker _tracker = tracker;
        private readonly ITcpFlowStore _store = store;
        private readonly ITcpConnectionFinalizer _finalizer = finalizer;
        private readonly ILogger<DefaultTcpLifecycleReconstructor> _logger = logger;
        private readonly ChannelWriter<TcpConnectionLifecycle> _output = output;
        public void Process(in TcpEvent evt)
        {
            var flow = _tracker.GetOrCreate(evt);

            var shouldFinalize = flow.Apply(evt);

            if (!shouldFinalize)
                return;

            var lifecycle = _finalizer.Finalize(flow);

            _store.TryRemove(flow.Key, out _);

            if (!_output.TryWrite(lifecycle))
            {
                _logger.LogError("Lifecycle channel full - dropping connection");
            }
        }

        private void OnConnectionFinalized(
            TcpConnectionLifecycle lifecycle)
        {
            // hook verso persistence pipeline
            // (SQLite writer / channel / etc.)
        }
    }
}
