using EtwIpGrabber.TcpLifeCycleReconstruction.Abstractions;
using EtwIpGrabber.TcpLifeCycleReconstruction.Models;
using EtwIpGrabber.TcpLifeCycleReconstruction.Models.Enumerations;
using System.Threading.Channels;

namespace EtwIpGrabber.TcpLifeCycleReconstruction.Timeout
{
    internal sealed class TcpTimeoutSweeper(
    ITcpFlowStore store,
    ITcpConnectionFinalizer finalizer,
    Channel<TcpConnectionLifecycle> output)
    : ITcpTimeoutSweeper
    {
        private readonly ITcpFlowStore _store = store;
        private readonly ITcpConnectionFinalizer _finalizer = finalizer;
        private readonly Channel<TcpConnectionLifecycle> _output = output;

        private readonly TimeSpan _newTimeout = TimeSpan.FromSeconds(60);
        private readonly TimeSpan _connectingTimeout = TimeSpan.FromSeconds(30);
        private readonly TimeSpan _establishedTimeout = TimeSpan.FromMinutes(5);
        private readonly TimeSpan _closingTimeout = TimeSpan.FromMinutes(2);

        public async Task SweepAsync(DateTime nowUtc, CancellationToken stoppingToken)
        {
            foreach (var flow in _store.EnumerateActive())
            {
                if (flow.State is TcpLifecycleState.Closed
                    or TcpLifecycleState.Aborted
                    or TcpLifecycleState.TimedOut)
                    continue;

                var timeout = GetTimeout(flow.State);

                if (nowUtc - flow.LastSeenUtc < timeout)
                    continue;

                if (_store.TryRemove(flow.Key, out var removed) && removed is not null)
                {
                    removed.State = TcpLifecycleState.TimedOut;
                    removed.EndUtc ??= removed.LastSeenUtc;

                    var lifecycle = _finalizer.Finalize(removed);

                    await _output.Writer.WriteAsync(lifecycle, stoppingToken);
                }
            }
        }

        private TimeSpan GetTimeout(TcpLifecycleState state)
            => state switch
            {
                TcpLifecycleState.New => _newTimeout,
                TcpLifecycleState.Connecting => _connectingTimeout,
                TcpLifecycleState.Established => _establishedTimeout,
                TcpLifecycleState.Closing => _closingTimeout,
                _ => TimeSpan.FromSeconds(30)
            };
    }
}
