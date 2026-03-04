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

        // Tutti i flag degli eventi osservabili per questa connessione TCP.
            public bool SeenConnect;
            public bool SeenAccept;
            public bool SeenSend;
            public bool SeenReceive;
            public bool SeenDisconnect;
            public bool SeenRetransmit;
            public bool SeenReconnect;
            public bool SeenFail;

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
                    SeenAccept = true;
                    State = TcpLifecycleState.Established;
                    break;

                case TcpEventType.Send:
                    SeenSend = true;
                    if (State == TcpLifecycleState.New)
                        State = TcpLifecycleState.Connecting;
                    break;

                case TcpEventType.Receive:
                    SeenReceive = true;
                    State = TcpLifecycleState.Established;
                    break;

                case TcpEventType.Disconnect:
                    SeenDisconnect = true;
                    EndUtc = e.TimestampUtc;
                    State = TcpLifecycleState.Closing;
                    return true;

                case TcpEventType.Fail:
                    SeenFail = true;
                    EndUtc = e.TimestampUtc;
                    State = TcpLifecycleState.Aborted;
                    return true;

                case TcpEventType.Retransmit:
                    SeenRetransmit = true;
                    break;

                case TcpEventType.Reconnect:
                    SeenReconnect = true;
                    State = TcpLifecycleState.Connecting;
                    break;
            }

            return false;
        }
    }
}
