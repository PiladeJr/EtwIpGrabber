using EtwIpGrabber.TcpLifeCycleReconstruction.Models.Enumerations;
using EtwIpGrabber.TdhParsing.Normalization.Models;

namespace EtwIpGrabber.TcpLifeCycleReconstruction.Models
{
    internal sealed class TcpFlowInstance(
        in TcpFlowKey key,
        in TcpEvent firstEvent)
    {
        public readonly TcpFlowKey Key = key;
        public string ProcessName = firstEvent.ProcessName;

        public readonly DateTime FirstSeenUtc = firstEvent.TimestampUtc;
        public DateTime LastSeenUtc = firstEvent.TimestampUtc;
        public DateTime? EndUtc;

        public bool SeenConnect;
        public bool SeenAccept;
        public bool SeenClose;
        public bool SeenRetransmit;
        public bool SeenDisconnect;

        public TcpLifecycleState State = firstEvent.EventType switch
        {
            TcpEventType.Connect => TcpLifecycleState.Connecting,
            TcpEventType.Accept => TcpLifecycleState.Established,
            _ => TcpLifecycleState.New
        };
        public bool Apply(in TcpEvent e)
        {
            LastSeenUtc = e.TimestampUtc;

            switch (e.EventType)
            {
                case TcpEventType.Connect:
                    SeenConnect = true;
                    State = TcpLifecycleState.Connecting;
                    break;

                case TcpEventType.Accept:

                    if (State == TcpLifecycleState.New)
                        State = TcpLifecycleState.Established;
                    else
                        State = TcpLifecycleState.Established;
                    break;

                case TcpEventType.Close:
                    SeenClose = true;
                    EndUtc = e.TimestampUtc;
                    State = TcpLifecycleState.Closed;
                    return true;

                case TcpEventType.Disconnect:
                    SeenDisconnect = true;
                    EndUtc = e.TimestampUtc;
                    State = TcpLifecycleState.Aborted;
                    return true;

                case TcpEventType.Retransmit:
                    SeenRetransmit = true;
                    break;
            }

            return false;
        }
    }
}
