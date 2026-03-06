using EtwIpGrabber.TcpLifeCycleReconstruction.Models;
using EtwIpGrabber.TcpLifeCycleReconstruction.Models.Enumerations;
using EtwIpGrabber.TdhParsing.Normalization;
using EtwIpGrabber.TdhParsing.Normalization.Models;
using EtwIpGrabber.Workers.FanOut;

namespace EtwIpGrabber.Workers
{
    internal sealed class TcpLifecycleLoggerWorker(
        TcpLoggerChannel channel,
        ILogger<TcpLifecycleLoggerWorker> logger)
        : BackgroundService
    {
        protected override async Task ExecuteAsync( CancellationToken stoppingToken)
        {
            var reader = channel.Channel.Reader;
            var outcomeCount = new Dictionary<TcpConnectionOutcome, long>
            {
                { TcpConnectionOutcome.Closed, 0 },
                { TcpConnectionOutcome.Refused, 0 },
                { TcpConnectionOutcome.Timeout, 0 },
                { TcpConnectionOutcome.Established, 0 },
                { TcpConnectionOutcome.Aborted, 0 },
                { TcpConnectionOutcome.Unknown, 0 }
            };

            try
            {
                await foreach (var lifecycle in reader.ReadAllAsync(stoppingToken))
                {
                    var directionArrow = GetDirectionArrow(lifecycle.Direction);

                    logger.LogInformation(
                        "TCP Lifecycle" +
                        Environment.NewLine + "Process: {Process}" +
                        Environment.NewLine + "{Direction} - {Local}:{LPort} {Arrow} {Remote}:{RPort}" +
                        Environment.NewLine + "Classification: {Classification}" +
                        Environment.NewLine + "Start: {Start:O}" +
                        Environment.NewLine + "End: {End:O}" +
                        Environment.NewLine + "Duration: {Duration}" +
                        Environment.NewLine + "Outcome: {Outcome}, Handshake stage: {Handshake}" +
                        Environment.NewLine + "CommunityId: {Cid}",

                        lifecycle.ProcessName,
                        FormatDirection(lifecycle.Direction),
                        ConversionUtil.FormatIPv4(lifecycle.LocalIP),
                        lifecycle.LocalPort,
                        directionArrow,
                        ConversionUtil.FormatIPv4(lifecycle.RemoteIP),
                        lifecycle.RemotePort,
                        lifecycle.Classification,
                        lifecycle.StartAt,
                        lifecycle.EndAt,
                        lifecycle.Duration,
                        lifecycle.Outcome,
                        lifecycle.Handshake,
                        lifecycle.CommunityId);

                    if (outcomeCount.TryGetValue(lifecycle.Outcome, out long value))
                    {
                        outcomeCount[lifecycle.Outcome] = ++value;
                    }
                }
            }
            catch (TaskCanceledException) 
            { 
                //ignore
            }
            finally
            {
                logger.LogInformation(
                    "Service shutdown - TCP Connection Outcomes Summary:" +
                    Environment.NewLine + "Closed: {Closed}" +
                    Environment.NewLine + "Refused: {Refused}" +
                    Environment.NewLine + "Timeout: {Timeout}" +
                    Environment.NewLine + "Established: {Established}" +
                    Environment.NewLine + "Aborted: {Aborted}" +
                    Environment.NewLine + "Unknown: {Unknown}",
                    outcomeCount[TcpConnectionOutcome.Closed],
                    outcomeCount[TcpConnectionOutcome.Refused],
                    outcomeCount[TcpConnectionOutcome.Timeout],
                    outcomeCount[TcpConnectionOutcome.Established],
                    outcomeCount[TcpConnectionOutcome.Aborted],
                    outcomeCount[TcpConnectionOutcome.Unknown]);
            }
        }

        private static string GetDirectionArrow(TcpDirection direction) => direction switch
        {
            TcpDirection.Outbound => "→",
            TcpDirection.Inbound => "←",
            TcpDirection.Local => "<->",
            TcpDirection.Unknown => "~>",
            _ => "?"
        };

        private static string FormatDirection(TcpDirection direction) => direction switch
        {
            TcpDirection.Outbound => "Outbound",
            TcpDirection.Inbound => "Inbound",
            TcpDirection.Local => "Local",
            TcpDirection.Unknown => "Unknown",
            _ => "Unknown"
        };
    }
}