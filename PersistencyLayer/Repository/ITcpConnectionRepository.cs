using EtwIpGrabber.TcpLifeCycleReconstruction.Models;

namespace EtwIpGrabber.PersistencyLayer.Repository
{
    public interface ITcpConnectionRepository
    {
        Task UpsertFlowAsync(TcpFlowInstance flow, CancellationToken ct);

        Task InsertLifecycleAsync(TcpConnectionLifecycle lifecycle, CancellationToken ct);
    }
}
