using EtwIpGrabber.EtwStructure;
using EtwIpGrabber.EtwStructure.EventDispatcher;
using EtwIpGrabber.EtwStructure.MetricsAndHealth;
using EtwIpGrabber.EtwStructure.ProviderConfiguration.Abstractions;
using EtwIpGrabber.EtwStructure.RealTimeConsumer;
using EtwIpGrabber.EtwStructure.SessionManager.Abstraction;

namespace EtwIpGrabber.Workers
{
    public class EtwCollectionWorker(
            ILogger<EtwCollectionWorker> logger,
            IEtwSessionController session,
            IEtwProviderConfigurator provider,
            IRealtimeEtwConsumer consumer,
            EtwTelemetryMonitor monitor) : BackgroundService
    {
        private readonly ILogger<EtwCollectionWorker> _logger = logger;
        private readonly IEtwSessionController _session = session;
        private readonly IEtwProviderConfigurator _provider = provider;
        private readonly IRealtimeEtwConsumer _consumer = consumer;
        private readonly EtwTelemetryMonitor _monitor = monitor;

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                _logger.LogInformation("Starting ETW bootstrap...");

                _session.StartOrAttach();

                _provider.EnableProvider(_session.SessionHandle);

                _consumer.Start(_session.SessionName);

                _logger.LogInformation("ETW bootstrap completed successfully.");

                await _monitor.RunAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "ETW bootstrap FAILED");
                throw; // importante: lascia crashare dopo aver loggato
            }
        }
    }
}
