using EtwIpGrabber.EtwStructure.EventDispatcher;
using EtwIpGrabber.TdhParsing;
using EtwIpGrabber.TdhParsing.Normalization;

namespace EtwIpGrabber
{
    internal sealed class TcpParseWorker(
        BoundedEventRingBuffer buffer,
        ITcpEtwParser parser,
        ILogger<TcpParseWorker> logger)
        : BackgroundService
    {
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                if (!buffer.TryDequeue(out var snapshot))
                {
                    await Task.Delay(5, stoppingToken); 
                    continue;
                }

                if (!parser.TryParse(snapshot, out var tcp))
                    continue;

                logger.LogInformation(
                    "TCP {Local}:{LPort} -> {Remote}:{RPort}",
                    ConversionUtil.FormatIPv4(tcp.LocalIP),
                    tcp.LocalPort,
                    ConversionUtil.FormatIPv4(tcp.RemoteIP),
                    tcp.RemotePort);
            }
        }
    }
}
