using EtwIpGrabber.PersistencyLayer.Filters;
using EtwIpGrabber.PersistencyLayer.Repository;
using EtwIpGrabber.Workers.FanOut;

namespace EtwIpGrabber.Workers.Data
{
    internal sealed class TcpLifecyclePersistenceWorker(
        TcpPersistenceChannel channel,
        ITcpConnectionRepository repo,
        IPersistenceFilter filter,
        ILogger<TcpLifecyclePersistenceWorker> logger)
        : BackgroundService
    {
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await foreach (var lifecycle in channel.Channel.Reader.ReadAllAsync(stoppingToken))
            {
                try
                {
                    if (!filter.ShouldPersist(lifecycle))
                        continue;

                    await repo.InsertLifecycleAsync(lifecycle, stoppingToken);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Failed to persist lifecycle");
                }
            }
        }
    }
}
