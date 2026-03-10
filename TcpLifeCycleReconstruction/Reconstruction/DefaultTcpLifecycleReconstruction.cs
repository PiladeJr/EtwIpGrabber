using EtwIpGrabber.TcpLifeCycleReconstruction.Abstractions;
using EtwIpGrabber.TcpLifeCycleReconstruction.Models;
using EtwIpGrabber.TdhParsing.Normalization.Models;
using EtwIpGrabber.Workers.Data;
using System.Threading.Channels;

namespace EtwIpGrabber.TcpLifeCycleReconstruction.Reconstruction
{
    internal sealed class DefaultTcpLifecycleReconstructor(
        ITcpFlowTracker tracker,
        ITcpFlowStore store,
        ITcpConnectionFinalizer finalizer,
        ChannelWriter<TcpConnectionLifecycle> output,
        TcpFlowPersistenceChannel flowChannel,
        ILogger<DefaultTcpLifecycleReconstructor> logger
        //mute temporaneo dell'hook finché non ho un wrapper di implementazione
        //, ITcpEventObserver observer
        ): ITcpLifecycleReconstructor
    {
        private readonly ITcpFlowTracker _tracker = tracker;
        private readonly ITcpFlowStore _store = store;
        private readonly ITcpConnectionFinalizer _finalizer = finalizer;
        private readonly ILogger<DefaultTcpLifecycleReconstructor> _logger = logger;
        private readonly ChannelWriter<TcpConnectionLifecycle> _output = output;
        private readonly TcpFlowPersistenceChannel _flowChannel = flowChannel;
        //private readonly ITcpEventObserver _observer = observer
        public void Process(in TcpEvent evt)
        {
            var flow = _tracker.GetOrCreate(evt);

            var shouldFinalize = flow.Apply(evt);

            if (!_flowChannel.Channel.Writer.TryWrite(flow))
            {
                _logger.LogWarning("Flow channel full - dropping flow update for {CommunityId}",
                    flow.CommunityId);
            }

            // hook per analisi degli eventi dal codice.
            // _observer.OnTcpEvent(evt, flow) mutato temporaneamente finché non ho un implementazione concreta

            if (!shouldFinalize)
                return;

            var lifecycle = _finalizer.Finalize(flow);

            _store.TryRemove(flow.Key, out _);

            if (!_output.TryWrite(lifecycle))
            {
                _logger.LogError("Lifecycle channel full - dropping connection");
            }
        }
    }
}
