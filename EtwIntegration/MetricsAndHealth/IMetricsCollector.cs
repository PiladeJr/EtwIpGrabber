namespace EtwIpGrabber.EtwIntegration.MetricsAndHealth
{
    public interface IMetricsCollector
    {
        void IncrementDroppedEvents();
        long Dropped { get; }
    }
}
