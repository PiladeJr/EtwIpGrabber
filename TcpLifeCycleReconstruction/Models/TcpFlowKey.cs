using EtwIpGrabber.TdhParsing.Normalization.Models;

namespace EtwIpGrabber.TcpLifeCycleReconstruction.Models
{
    internal readonly struct TcpFlowKey(
        uint localIp,
        ushort localPort,
        uint remoteIp,
        ushort remotePort,
        TcpDirection direction,
        uint processId) : IEquatable<TcpFlowKey>
    {
        public readonly uint LocalIp = localIp;
        public readonly ushort LocalPort = localPort;

        public readonly uint RemoteIp = remoteIp;
        public readonly ushort RemotePort = remotePort;

        public readonly TcpDirection Direction = direction;
        public readonly uint ProcessId = processId;

        public static TcpFlowKey FromEvent(in TcpEvent e)
            => new(
                e.LocalIP,
                e.LocalPort,
                e.RemoteIP,
                e.RemotePort,
                e.Direction,
                e.ProcessId);

        public bool Equals(TcpFlowKey other)
            => LocalIp == other.LocalIp &&
               LocalPort == other.LocalPort &&
               RemoteIp == other.RemoteIp &&
               RemotePort == other.RemotePort &&
               Direction == other.Direction &&
               ProcessId == other.ProcessId;

        public override bool Equals(object? obj)
            => obj is TcpFlowKey other && Equals(other);

        public override int GetHashCode()
            => HashCode.Combine(
                LocalIp,
                LocalPort,
                RemoteIp,
                RemotePort,
                Direction,
                ProcessId);
    }
}
