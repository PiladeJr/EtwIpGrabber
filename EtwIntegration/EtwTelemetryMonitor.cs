using EtwIpGrabber.EtwStructure.EventDispatcher;
using EtwIpGrabber.EtwStructure.MetricsAndHealth;

namespace EtwIpGrabber.EtwStructure
{
    public sealed class EtwTelemetryMonitor(BoundedEventRingBuffer buffer, EtwMetricsCollector metrics, ILogger<EtwTelemetryMonitor> logger)
    {
        private readonly BoundedEventRingBuffer _buffer = buffer;
        private readonly EtwMetricsCollector _metrics = metrics;
        private readonly ILogger<EtwTelemetryMonitor> _logger = logger;

        public async Task RunAsync(CancellationToken token)
        {
            long last = 0;

            while (!token.IsCancellationRequested)
            {
                await Task.Delay(1000, token);

                long now = _buffer.Enqueued;
                long rate = now - last;
                last = now;

                _logger.LogInformation(
                    "ETW TCPIP Ingestion | Events/sec: {Rate}, QueueDepth: {Depth}, Dropped: {Dropped}",
                    rate,
                    _buffer.Depth,
                    _metrics.Dropped);
            }
        }
    }
}
