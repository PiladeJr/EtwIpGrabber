using EtwIpGrabber.EtwStructure.EventDispatcher;
using EtwIpGrabber.EtwStructure.MetricsAndHealth;

namespace EtwIpGrabber
{
    public sealed class EtwTelemetryMonitor(
        BoundedEventRingBuffer buffer,
        EtwMetricsCollector metrics)
    {
        private readonly BoundedEventRingBuffer _buffer = buffer;
        private readonly EtwMetricsCollector _metrics = metrics;
        private static readonly ILogger<EtwTelemetryMonitor> _logger =
            LoggerFactory.Create(builder =>
            {
                builder.AddConsole();
            }).CreateLogger<EtwTelemetryMonitor>();

        public async Task RunAsync(CancellationToken token)
        {
            long last = 0;

            while (!token.IsCancellationRequested)
            {
                await Task.Delay(1000, token);

                long now = _buffer.Enqueued;
                long rate = now - last;
                last = now;

                _logger.LogInformation(@"
                ETW TCPIP INGESTION
                Events/sec : {rate}
                QueueDepth : {depth}
                Dropped    : {dropped}",
                rate,
                _buffer.Depth,
                _metrics.Dropped);
            }
        }
    }
}
