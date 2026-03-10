using EtwIpGrabber.PersistencyLayer.Filters;
using EtwIpGrabber.PersistencyLayer.Repository;
using EtwIpGrabber.TcpLifeCycleReconstruction.Models;

namespace EtwIpGrabber.Workers.Data
{
    internal sealed class TcpFlowPersistenceWorker(
        TcpFlowPersistenceChannel channel,
        ITcpConnectionRepository repo,
        IPersistenceFilter filter,
        ILogger<TcpFlowPersistenceWorker> logger)
        : BackgroundService
    {
        // Soglia: sotto questa quantità → flush immediato
        // Sopra → batch aggregato
        private const int BatchThreshold = 50;
        private const int MaxBatchSize = 200;

        private readonly Dictionary<string, TcpFlowInstance> _pending = new(MaxBatchSize);

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var reader = channel.Channel.Reader;

            while (!stoppingToken.IsCancellationRequested)
            {
                _pending.Clear();

                if (!await reader.WaitToReadAsync(stoppingToken))
                    break;

                // Leggi quanti item sono disponibili ORA (senza attendere)
                while (_pending.Count < MaxBatchSize && reader.TryRead(out var flow))
                {
                    // Deduplica: se arrivano 10 eventi sullo stesso flow,
                    // persisti solo l'ultimo stato
                    _pending[flow.CommunityId] = flow;
                }

                var count = _pending.Count;

                if (count == 0) continue;

                if (count < BatchThreshold)
                {
                    // BASSO CARICO → flush immediato uno per uno
                    // Nessuna latenza, nessuna attesa
                    await PersistImmediateAsync(_pending.Values, stoppingToken);
                }
                else
                {
                    // ALTO CARICO → batch unico, massima efficienza
                    await PersistBatchAsync(_pending.Values, stoppingToken);
                }
            }
        }

        private async Task PersistImmediateAsync(
            IEnumerable<TcpFlowInstance> flows,
            CancellationToken ct)
        {
            foreach (var flow in flows)
            {
                try
                {
                    await repo.UpsertFlowAsync(flow, ct);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Failed to persist flow {CommunityId}", flow.CommunityId);
                }
            }
        }

        private async Task PersistBatchAsync(
            IEnumerable<TcpFlowInstance> flows,
            CancellationToken ct)
        {
            try
            {
                await repo.UpsertFlowBatchAsync(flows.ToList(), ct);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to persist flow batch");
            }
        }
    }
}
