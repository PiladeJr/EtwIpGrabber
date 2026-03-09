using EtwIpGrabber.PersistencyLayer.Filters;
using EtwIpGrabber.PersistencyLayer.Repository;

namespace EtwIpGrabber.Workers.Data
{
    internal sealed class TcpFlowPersistenceWorker(
        TcpFlowPersistenceChannel channel,
        ITcpConnectionRepository repo,
        IPersistenceFilter filter,
        ILogger<TcpFlowPersistenceWorker> logger)
        : BackgroundService
    {
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await foreach (var flow in channel.Channel.Reader.ReadAllAsync(stoppingToken))
            {
                try
                {
                   // if (!filter.ShouldPersistFlow(flow))
                   //     continue;
                        
                    await repo.UpsertFlowAsync(flow, stoppingToken);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Failed to persist flow");
                }
            }
        }
    }
}
