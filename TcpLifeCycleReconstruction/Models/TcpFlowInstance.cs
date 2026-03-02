using EtwIpGrabber.TdhParsing.Normalization.Models;

namespace EtwIpGrabber.TcpLifeCycleReconstruction.Models
{
    internal sealed class TcpFlowInstance(
        in TcpFlowKey key,
        in TcpEvent firstEvent)
    {
        public readonly TcpFlowKey Key = key;

        public readonly DateTime FirstSeenUtc = firstEvent.TimestampUtc;
        public DateTime LastSeenUtc = firstEvent.TimestampUtc;
        public DateTime? EndUtc;

        public bool SeenConnect;
        public bool SeenAccept;
        public bool SeenClose;
        public bool SeenDisconnect;

        public TcpLifecycleState State = firstEvent.EventType switch
        {
            TcpEventType.Connect => TcpLifecycleState.Connecting,
            TcpEventType.Accept => TcpLifecycleState.Established,
            _ => TcpLifecycleState.New
        };
    }
}
