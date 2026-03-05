using EtwIpGrabber.EtwIntegration.EventDispatcher;
using EtwIpGrabber.TdhParsing;
using EtwIpGrabber.TdhParsing.Normalization;
using EtwIpGrabber.TdhParsing.Normalization.Models;
using System.Threading.Channels;

namespace EtwIpGrabber.Workers
{
    internal sealed class TcpParseWorker(
        BoundedEventRingBuffer buffer,
        ITcpEtwParser parser,
        Channel<TcpEvent> channel
        //,ILogger<TcpParseWorker> logger
        ): BackgroundService
    {
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try {
                while (!stoppingToken.IsCancellationRequested)
                {
                    if (!buffer.TryDequeue(out var snapshot))
                    {
                        await Task.Delay(5, stoppingToken);
                        continue;
                    }

                    if (!parser.TryParse(snapshot, out var tcp))
                        continue;
/*
                    logger.LogInformation(
                    @"TCP {EventType}
                        {Local}:{LPort} → {Remote}:{RPort}
                        Process: id = {Pid}; name = {PName}
                        Direction: {Dir}
                        Flags: {Flags}
                        TimestampUtc: {Ts:O}",
                        tcp!.EventType,
                        ConversionUtil.FormatIPv4(tcp.LocalIP),
                        tcp.LocalPort,
                        ConversionUtil.FormatIPv4(tcp.RemoteIP),
                        tcp.RemotePort,
                        tcp.ProcessId,
                        tcp.ProcessName,
                        tcp.Direction,
                        tcp.Flags,
                        tcp.TimestampUtc)
*/                    
                    await channel.Writer.WriteAsync(tcp!, stoppingToken);
                }
            }
            catch (TaskCanceledException)
            {
               //ignore
            }
            
        }
        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            channel.Writer.TryComplete();
            await base.StopAsync(cancellationToken);
        }
    }
}
