namespace EtwIpGrabber.PersistencyLayer.Filters
{
    [Flags]
    public enum NetworkScopeFilters
    {
        None = 0,
        Loopback = 1 << 0,
        Private = 1 << 1,
        Public = 1 << 2,
        Multicast = 1 << 3,
        Broadcast = 1 << 4,

        All = Loopback | Private | Public | Multicast | Broadcast
    }
}
