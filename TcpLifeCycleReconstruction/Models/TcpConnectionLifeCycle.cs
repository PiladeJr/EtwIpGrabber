namespace EtwIpGrabber.TcpLifeCycleReconstruction.Models
{
    internal sealed class TcpConnectionLifecycle
    {
        public uint ProcessId { get; init; }
        public string ProcessName { get; init; } = string.Empty;

        public uint LocalIP { get; init; }
        public ushort LocalPort { get; init; }

        public uint RemoteIP { get; init; }
        public ushort RemotePort { get; init; }

        public DateTime StartAt { get; init; }
        public DateTime EndAt { get; init; }

        public TimeSpan Duration { get; init; }

        public string CommunityId { get; init; } = string.Empty;
    }
}
