using EtwIpGrabber.TcpLifeCycleReconstruction.Models;
using EtwIpGrabber.Utils.ConnectionClassification;

namespace EtwIpGrabber.PersistencyLayer.Filters
{
    public sealed class NetworkScopePersistenceFilter(NetworkScopeFilters allowed)
          : IPersistenceFilter
    {
        private readonly NetworkScopeFilters _allowed = allowed;

        public bool ShouldPersist(TcpConnectionLifecycle connection)
        {
            NetworkScopeFilters scope = connection.Classification switch
            {
                NetworkScope.Loopback => NetworkScopeFilters.Loopback,
                NetworkScope.Private => NetworkScopeFilters.Private,
                NetworkScope.Public => NetworkScopeFilters.Public,
                NetworkScope.Multicast => NetworkScopeFilters.Multicast,
                NetworkScope.Broadcast => NetworkScopeFilters.Broadcast,
                _ => NetworkScopeFilters.None
            };

            return (_allowed & scope) != 0;
        }
    }
}
