using EtwIpGrabber.PersistencyLayer.Repository;

namespace EtwIpGrabber.Workers.Data
{
    internal sealed class DbInitializerWorker(
        ILogger<DbInitializerWorker> logger)
        : BackgroundService
    {
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                await DatabaseInitializer.InitializeAsync(stoppingToken);
                logger.LogInformation("Database schema initialized");
            }
            catch (Exception ex)
            {
                logger.LogCritical(ex, "Database initialization failed");
                throw;
            }
        }
    }
}
