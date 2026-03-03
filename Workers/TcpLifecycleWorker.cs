using EtwIpGrabber.TcpLifeCycleReconstruction.Abstractions;
using EtwIpGrabber.TdhParsing.Normalization.Models;
using System.Threading.Channels;

namespace EtwIpGrabber.Workers
{
    internal sealed class TcpLifecycleWorker(
        Channel<TcpEvent> channel,
        ITcpLifecycleReconstructor reconstructor,
        ITcpTimeoutSweeper sweeper)
        : BackgroundService
    {
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var reader = channel.Reader;
            var sweepInterval = TimeSpan.FromSeconds(10);
            var nextSweep = DateTime.UtcNow + sweepInterval;

            while (!stoppingToken.IsCancellationRequested)
            {
                var delayTask = Task.Delay(sweepInterval, stoppingToken);
                var readTask = reader.WaitToReadAsync(stoppingToken).AsTask();

                var completed = await Task.WhenAny(readTask, delayTask);
                if (completed == readTask && readTask.Result)
                {
                    while (reader.TryRead(out var evt))
                    {
                        reconstructor.Process(evt);
                    }
                }

                var now = DateTime.UtcNow;

                if (now >= nextSweep)
                {
                    await sweeper.SweepAsync(now, stoppingToken);
                    nextSweep = now + sweepInterval;
                }
            }
        }
        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            await base.StopAsync(cancellationToken);
        }
    }
}
