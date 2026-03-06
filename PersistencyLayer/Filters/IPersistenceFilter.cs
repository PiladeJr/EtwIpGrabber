using EtwIpGrabber.TcpLifeCycleReconstruction.Models;

namespace EtwIpGrabber.PersistencyLayer.Filters
{
    public interface IPersistenceFilter
    {
        bool ShouldPersist(TcpConnectionLifecycle connection);
        bool ShouldPersistFlow(TcpFlowInstance flow);
    }
}
