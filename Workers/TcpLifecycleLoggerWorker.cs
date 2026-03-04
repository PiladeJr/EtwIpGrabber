using EtwIpGrabber.TcpLifeCycleReconstruction.Models;
using EtwIpGrabber.TcpLifeCycleReconstruction.Models.Enumerations;
using EtwIpGrabber.TdhParsing.Normalization;
using System.Threading.Channels;

namespace EtwIpGrabber.Workers
{
    internal sealed class TcpLifecycleLoggerWorker(
        Channel<TcpConnectionLifecycle> channel,
        ILogger<TcpLifecycleLoggerWorker> logger)
        : BackgroundService
    {
        protected override async Task ExecuteAsync( CancellationToken stoppingToken)
        {
            var reader = channel.Reader;
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
                while (await reader.WaitToReadAsync(stoppingToken))
                {
                    while (reader.TryRead(out var lifecycle))
                    {
                        logger.LogInformation(
                            "TCP Lifecycle" +
                            Environment.NewLine + "Process: { Process}" +
                            Environment.NewLine + "{Local}:{LPort} → {Remote}:{RPort}"+
                            Environment.NewLine + "Start: {Start:O}"+
                            Environment.NewLine + "End: { End:O}"+
                            Environment.NewLine + "Duration: {Duration}"+
                            Environment.NewLine + "Outcome: {Outcome}, Handshake stage: {Handshake}"+
                            Environment.NewLine + "CommunityId: {Cid}",

                            lifecycle.ProcessName,
                            ConversionUtil.FormatIPv4(lifecycle.LocalIP),
                            lifecycle.LocalPort,
                            ConversionUtil.FormatIPv4(lifecycle.RemoteIP),
                            lifecycle.RemotePort,
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
    }
}