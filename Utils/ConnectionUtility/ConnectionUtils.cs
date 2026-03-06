using EtwIpGrabber.TcpLifeCycleReconstruction.Models;
using EtwIpGrabber.TcpLifeCycleReconstruction.Models.Enumerations;
using EtwIpGrabber.TdhParsing.Normalization.Models;

namespace EtwIpGrabber.Utils.ConnectionUtility
{
    public static class ConnectionUtils
    {
        public static NetworkScope ClassifyConnection(uint localIp, uint remoteIp)
        {
            NetworkScope local = NetworkClassification.Classify(localIp);
            NetworkScope remote = NetworkClassification.Classify(remoteIp);

            // loopback puro: 127.0.0.0 -> 127.0.0.0
            if (local == NetworkScope.Loopback && remote == NetworkScope.Loopback)
                return NetworkScope.Loopback;

            // multicast o broadcast: 
            if (local == NetworkScope.Multicast || remote == NetworkScope.Multicast)
                return NetworkScope.Multicast;

            if (local == NetworkScope.Broadcast || remote == NetworkScope.Broadcast)
                return NetworkScope.Broadcast;

            // internet: almeno uno dei due è pubblico
            if (local == NetworkScope.Public || remote == NetworkScope.Public)
                return NetworkScope.Public;

            // LAN: entrambe devono essere private
            if (local == NetworkScope.Private && remote == NetworkScope.Private)
                return NetworkScope.Private;

            return NetworkScope.Unknown;
        }

        public static TcpDirection DetermineDirection(TcpFlowInstance flow)
        {
            if (flow.Key.LocalIp == flow.Key.RemoteIp)
                return TcpDirection.Local;

            if (flow.SeenConnect)
                return TcpDirection.Outbound;

            if (flow.SeenAccept)
                return TcpDirection.Inbound;

            if (flow.SeenSend)
                return TcpDirection.Outbound;

            if (flow.SeenReceive)
                return TcpDirection.Inbound;

            bool localEphemeral = flow.Key.LocalPort >= 49152;
            bool remoteEphemeral = flow.Key.RemotePort >= 49152;

            if (localEphemeral && !remoteEphemeral)
                return TcpDirection.Outbound;

            if (!localEphemeral && remoteEphemeral)
                return TcpDirection.Inbound;

            return TcpDirection.Unknown;
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
        public static TcpConnectionOutcome DetermineOutcome(TcpFlowInstance flow)
        {
            // traffico dati => connessione riuscita
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
            if (flow.SeenRetransmit && !flow.SeenReceive && !flow.SeenSend)
                return TcpConnectionOutcome.Timeout;

            return TcpConnectionOutcome.Unknown;
        }

        /// <summary>
        /// Determina lo stadio dell'handshake TCP osservato per il flow.
        /// </summary>
        /// <remarks>
        /// Il provider ETW non espone direttamente i flag TCP (SYN, ACK, FIN, RST),
        /// quindi lo stato dell'handshake viene dedotto 
        /// osservando gli eventi generati dal provider TCPIP.
        ///
        /// Lo stadio restituito rappresenta la fase più avanzata osservata
        /// durante il flow e non necessariamente tutte le fasi attraversate
        /// dalla connessione.
        ///
        /// Eventi utilizzati per l'inferenza:
        /// <list type="bullet">
        ///     <item><description><see cref="TcpFlowInstance.SeenSend"/> </description></item>
        ///     <item><description><see cref="TcpFlowInstance.SeenReceive"/> </description></item>
        ///     <item><description><see cref="TcpFlowInstance.SeenAccept"/> </description></item>
        ///     <item><description><see cref="TcpFlowInstance.SeenConnect"/> </description></item>
        ///     <item><description><see cref="TcpFlowInstance.SeenDisconnect"/> </description></item>
        /// </list>
        ///
        /// Logica di inferenza:
        /// <list type="bullet">
        ///     <item><description>Send / Receive / Accept → <see cref="TcpHandshakeStage.Established"/></description></item>
        ///     <item><description>Connect → <see cref="TcpHandshakeStage.SynSent"/></description></item>
        ///     <item><description>Disconnect → <see cref="TcpHandshakeStage.Closing"/></description></item>
        /// </list>
        ///
        /// Nota:
        /// L'assenza di alcuni eventi non implica necessariamente che
        /// la fase non sia avvenuta; l'evento potrebbe non essere stato
        /// osservato a causa dell'inizio della sessione ETW o di perdita
        /// di eventi.
        /// </remarks>
        /// <param name="flow">
        /// Istanza del flow contenente i flag degli eventi osservati.
        /// </param>
        /// <returns>
        /// Lo stadio più avanzato osservato per la connessione.
        /// </returns>
        public static TcpHandshakeStage DetermineStage(TcpFlowInstance flow)
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
