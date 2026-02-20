namespace EtwIpGrabber.EtwStructure.MetricsAndHealth
{
    public sealed class EtwMetricsCollector : IMetricsCollector
    {
        private long _dropped;

        public void IncrementDroppedEvents()
        {
            Interlocked.Increment(ref _dropped);
        }

        public long Dropped => _dropped;
    }
}
