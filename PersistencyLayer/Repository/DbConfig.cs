namespace EtwIpGrabber.PersistencyLayer.Repository
{
    internal static class DbConfig
    {
        public static readonly string ConnectionString =
            $"Data Source={Path.Combine(AppContext.BaseDirectory, "Connections.db")}";
    }
}
