using EtwIpGrabber.Utils.ConnectionExtendedInfo;

namespace EtwIpGrabber.PersistencyLayer.Filters
{
    internal static class TableResolver
    {
        public static string LifecycleTable(NetworkScope scope) => scope switch
        {
            NetworkScope.Private => "internal_tcp_lifecycle",
            NetworkScope.Public => "public_tcp_lifecycle",
            _ => "tcp_lifecycle"
        };
    }
}
