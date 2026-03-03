using EtwIpGrabber.TcpLifeCycleReconstruction.Models;
using EtwIpGrabber.TdhParsing.Normalization;
using System.Threading.Channels;

namespace EtwIpGrabber.Workers
{
    internal sealed class TcpLifecycleLoggerWorker(
        Channel<TcpConnectionLifecycle> channel,
        ILogger<TcpLifecycleLoggerWorker> logger)
        : BackgroundService
    {
        protected override async Task ExecuteAsync(
            CancellationToken stoppingToken)
        {
            var reader = channel.Reader;

            while (await reader.WaitToReadAsync(stoppingToken))
            {
                while (reader.TryRead(out var lifecycle))
                {
                    logger.LogInformation(
                        @"TCP Lifecycle
                        Process: {Process}
                        {Local}:{LPort} → {Remote}:{RPort}
                        Start: {Start:O}
                        End: {End:O}
                        Duration: {Duration}
                        CommunityId: {Cid}",

                        lifecycle.ProcessName,
                        ConversionUtil.FormatIPv4(lifecycle.LocalIP),
                        lifecycle.LocalPort,
                        ConversionUtil.FormatIPv4(lifecycle.RemoteIP),
                        lifecycle.RemotePort,
                        lifecycle.StartAt,
                        lifecycle.EndAt,
                        lifecycle.Duration,
                        lifecycle.CommunityId);
                }
            }
        }
    }
}