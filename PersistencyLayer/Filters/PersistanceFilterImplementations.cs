using EtwIpGrabber.TcpLifeCycleReconstruction.Models;

namespace EtwIpGrabber.PersistencyLayer.Filters
{
    public sealed class NetworkScopePersistenceFilter(NetworkScopeFilters allowed)
          : IPersistenceFilter
    {
        private readonly NetworkScopeFilters _allowed = allowed;

        public bool ShouldPersist(TcpConnectionLifecycle connection)
        {
            var scope = (NetworkScopeFilters)(1 << (int)connection.Classification);
            return (_allowed & scope) != 0;
        }
    }
}
