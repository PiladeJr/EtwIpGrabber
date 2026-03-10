using EtwIpGrabber.EtwIntegration;
using EtwIpGrabber.EtwIntegration.EventDispatcher;
using EtwIpGrabber.EtwIntegration.MetricsAndHealth;
using EtwIpGrabber.EtwIntegration.ProviderConfiguration.Abstractions;
using EtwIpGrabber.EtwIntegration.RealTimeConsumer;
using EtwIpGrabber.EtwIntegration.SessionManager.Abstraction;

namespace EtwIpGrabber.Workers
{
    public class EtwCollectionWorker(
            ILogger<EtwCollectionWorker> logger,
            IEtwSessionController session,
            IEtwProviderConfigurator provider,
            IRealtimeEtwConsumer consumer,
            EtwTelemetryMonitor monitor,
            BoundedEventRingBuffer buffer,
            IMetricsCollector metrics) : BackgroundService
    {
        private readonly ILogger<EtwCollectionWorker> _logger = logger;
        private readonly IEtwSessionController _session = session;
        private readonly IEtwProviderConfigurator _provider = provider;
        private readonly IRealtimeEtwConsumer _consumer = consumer;
        private readonly EtwTelemetryMonitor _monitor = monitor;
        private readonly BoundedEventRingBuffer _buffer = buffer;
        private readonly IMetricsCollector _metrics = metrics;

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
            catch (TaskCanceledException)
            {
                _logger.LogInformation("ETW collection is stopping due to cancellation.");
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "ETW bootstrap FAILED");
                throw; // importante: lascia crashare dopo aver loggato
            }
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation(
                "EtwCollectionWorker shutdown stats - Enqueued to buffer: {Enqueued}, Dequeued from buffer: {Dequeued}, Dropped (buffer full): {Dropped}",
                _buffer.Enqueued,
                _buffer.Dequeued,
                _metrics.Dropped);

            await base.StopAsync(cancellationToken);
        }
    }
}
