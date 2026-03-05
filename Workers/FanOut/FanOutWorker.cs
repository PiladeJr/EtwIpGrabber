using EtwIpGrabber.TcpLifeCycleReconstruction.Models;
using System.Threading.Channels;

namespace EtwIpGrabber.Workers.FanOut
{
    internal sealed class TcpLifecycleFanOutWorker(
        ChannelReader<TcpConnectionLifecycle> input,
        TcpLoggerChannel logger,
        TcpPersistenceChannel persistence)
        : BackgroundService
    {
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await foreach (var lifecycle in input.ReadAllAsync(stoppingToken))
            {
                await logger.Channel.Writer.WriteAsync(lifecycle, stoppingToken);

                await persistence.Channel.Writer.WriteAsync(lifecycle, stoppingToken);
            }
        }
    }
}
