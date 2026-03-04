using EtwIpGrabber.TcpLifeCycleReconstruction.Abstractions;
using EtwIpGrabber.TcpLifeCycleReconstruction.Models;
using EtwIpGrabber.TcpLifeCycleReconstruction.Models.Enumerations;

namespace EtwIpGrabber.TcpLifeCycleReconstruction.Finalization
{
    internal sealed class TcpConnectionFinalizer(
        ICommunityIdProvider communityId) : ITcpConnectionFinalizer
    {
        private readonly ICommunityIdProvider _communityId = communityId;

        public TcpConnectionLifecycle Finalize(
            TcpFlowInstance flow)
        {
            var startAt = flow.FirstSeenUtc;

            var endAt = flow.EndUtc ?? flow.LastSeenUtc;

            if (endAt < startAt) endAt = startAt;

            var duration = endAt - startAt;

            var communityId = _communityId.Compute(
                new TcpCommunityIdKey(
                    flow.Key.LocalIp,
                    flow.Key.LocalPort,
                    flow.Key.RemoteIp,
                    flow.Key.RemotePort
                ));

            return new TcpConnectionLifecycle
            {
                ProcessId = flow.Key.ProcessId,
                ProcessName = flow.ProcessName,

                LocalIP = flow.Key.LocalIp,
                LocalPort = flow.Key.LocalPort,

                RemoteIP = flow.Key.RemoteIp,
                RemotePort = flow.Key.RemotePort,

                StartAt = startAt,
                EndAt = endAt,
                Duration = duration,

                Outcome = DetermineOutcome(flow),
                Handshake = DetermineStage(flow),

                CommunityId = communityId
            };
        }
        /// <summary>
        /// Determina l'esito finale della connessione TCP in base agli eventi osservati nel flow.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Il provider ETW <c>Microsoft-Windows-TCPIP</c> non espone direttamente gli stati TCP
        /// (<c>SYN</c>, <c>SYN-ACK</c>, <c>ACK</c>), quindi l'esito viene dedotto
        /// inferenzialmente dalla sequenza di eventi.
        /// </para>
        /// <para>Regole utilizzate:</para>
        /// <list type="bullet">
        ///   <item><description><see cref="TcpConnectionOutcome.Closed">Closed: </see><c>Connect → Accept → Close</c>.</description></item>
        ///   <item><description><see cref="TcpConnectionOutcome.Aborted">Aborted: </see><c>Connect → Accept → Disconnect</c>.</description></item>
        ///   <item><description><see cref="TcpConnectionOutcome.Refused">Refused: </see><c>Connect → Disconnect</c> senza <c>Accept</c>.</description></item>
        ///   <item><description><see cref="TcpConnectionOutcome.Timeout">Timeout: </see>flow terminato dal timeout sweeper.</description></item>
        ///   <item><description><see cref="TcpConnectionOutcome.Established">Established: </see>connessione stabilita senza evento di chiusura osservato.</description></item>
        ///   <item><description><see cref="TcpConnectionOutcome.Unknown">Unknown: </see>sequenza incompleta o non classificabile.</description></item>
        /// </list>
        /// <b>Edge case:</b>
        /// <para>
        /// Un flusso che mostra solo <c>Connect</c> e <c>Disconnect</c> senza <c>Accept</c>
        /// potrebbe rappresentare un tentativo di connessione interrotto prematuramente:
        /// </para>
        /// <list type="bullet">
        ///     <item><description>un app che chiude il socket immediatamente.</description></item>
        ///     <item><description>un handshake abortito localmente</description></item>
        /// </list>
        /// Interpreto questo caso come <see cref="TcpConnectionOutcome.Aborted"> Aborted</see> piuttosto che <see cref="TcpConnectionOutcome.Refused"> Refused</see>,
        /// </remarks>
        /// <param name="flow">Istanza di flow contenente stato e flag osservati durante il lifecycle.</param>
        /// <returns>L'esito finale della connessione TCP.</returns>
        private static TcpConnectionOutcome DetermineOutcome(TcpFlowInstance flow)
        {
            // traffico dati ⇒ connessione riuscita
            if (flow.SeenSend || flow.SeenReceive)
            {
                if (flow.SeenDisconnect)
                    return TcpConnectionOutcome.Closed;

                return TcpConnectionOutcome.Established;
            }

            // handshake lato server
            if (flow.SeenAccept)
            {
                if (flow.SeenDisconnect)
                    return TcpConnectionOutcome.Closed;

                return TcpConnectionOutcome.Established;
            }

            // tentativo rifiutato
            if (flow.SeenConnect && flow.SeenDisconnect)
                return TcpConnectionOutcome.Refused;

            // errore TCP
            if (flow.SeenFail)
                return TcpConnectionOutcome.Aborted;

            // timeout gestito dallo sweeper
            if (flow.State == TcpLifecycleState.TimedOut)
                return TcpConnectionOutcome.Timeout;

            // edge case: host irraggiungibile o firewall drop
            if (flow.SeenRetransmit && !flow.SeenReceive)
                return TcpConnectionOutcome.Timeout;

            return TcpConnectionOutcome.Unknown;
        }

        private static TcpHandshakeStage DetermineStage(TcpFlowInstance flow)
        {
            if (flow.SeenDisconnect)
                return TcpHandshakeStage.Closing;

            if (flow.SeenSend || flow.SeenReceive || flow.SeenAccept)
                return TcpHandshakeStage.Established;

            if (flow.SeenConnect)
                return TcpHandshakeStage.SynSent;

            return TcpHandshakeStage.None;
        }
    }
}
