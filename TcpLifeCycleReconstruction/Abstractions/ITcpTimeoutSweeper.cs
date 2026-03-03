namespace EtwIpGrabber.TcpLifeCycleReconstruction.Abstractions
{
    internal interface ITcpTimeoutSweeper
    {
         Task SweepAsync(DateTime nowUtc, CancellationToken stoppingToken);
    }
}
