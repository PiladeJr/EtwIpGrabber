namespace EtwIpGrabber.PersistencyLayer.Filters
{
    internal static class PersistenceScopeResolver
    {
        public static NetworkScopeFilters Resolve(string[] args)
        {
            var arg = args.FirstOrDefault(a => 
                a.StartsWith("--scope=", StringComparison.OrdinalIgnoreCase) ||
                a.StartsWith("-s=", StringComparison.OrdinalIgnoreCase));

            if (arg == null)
                return NetworkScopeFilters.Private; // default

            var value = arg.Split('=', 2)[1];

            if (Enum.TryParse<NetworkScopeFilters>(value, true, out var scope))
                return scope;

            return NetworkScopeFilters.Private;
        }
    }
}
