namespace EtwIpGrabber.TdhParsing.Normalization.Models
{
    /// <summary>
    /// Evento TCP normalizzato estratto da ETW e pronto per le fasi successive
    /// della pipeline.
    /// </summary>
    /// <remarks>
    /// Rappresenta il risultato della decodifica e normalizzazione di un evento
    /// ETW proveniente dal provider <c>Microsoft-Windows-TCPIP</c>. Tutti i valori
    /// sono:
    /// <list type="bullet">
    ///   <item><description>convertiti in host byte order;</description></item>
    ///   <item><description>semanticamente interpretabili;</description></item>
    ///   <item><description>da interpretare in una fase successiva di Lifecycle Reconstruction</description></item>
    ///   <item><description>Persistibile su database.</description></item>
    /// </list>
    /// </remarks>
    public sealed class TcpEvent
    {
        /// <summary>
        /// Timestamp dell'evento in formato UTC.
        /// </summary>
        /// <remarks>
        /// Derivato dal campo <c>EventHeader.TimeStamp</c> e convertito
        /// a <see cref="DateTime"/> per semplicità d'uso.
        /// </remarks>
        public DateTime TimestampUtc { get; set; }

        /// <summary>
        /// Identificatore del processo (PID) che ha generato l'evento.
        /// </summary>
        /// <remarks>
        /// Estratto dal payload dell'evento ETW. Correlato alla Process Start Key
        /// per mitigare il rischio di PID reuse.
        /// </remarks>
        public uint ProcessId { get; set; }

        /// <summary>
        /// Il nome del processo che ha generato l'evento.
        /// </summary>
        /// <remarks>
        /// questo campo non è presente all'interno dell'evento ETW originale,
        /// ma viene arricchito durante la fase di normalizzazione effettuando una chiamata
        /// al metodo <see cref="System.Diagnostics.Process.GetProcessById(int)"/> passando il 
        /// campo <see cref="ProcessId"/>. 
        /// </remarks>
        public string? ProcessName { get; set; }

        /// <summary>
        /// Indirizzo IP locale della connessione TCP (host byte order).
        /// </summary>
        public uint LocalIP { get; set; }

        /// <summary>
        /// Porta locale della connessione TCP.
        /// </summary>
        public ushort LocalPort { get; set; }

        /// <summary>
        /// Indirizzo IP remoto della connessione TCP (host byte order).
        /// </summary>
        public uint RemoteIP { get; set; }

        /// <summary>
        /// Porta remota della connessione TCP.
        /// </summary>
        public ushort RemotePort { get; set; }

        /// <summary>
        /// Direzione della connessione (outbound o inbound).
        /// </summary>
        public TcpDirection Direction { get; set; }

        /// <summary>
        /// Tipo di evento TCP (Connect, Accept, Disconnect, Timeout, ecc.).
        /// </summary>
        public TcpEventType EventType { get; set; }

        /// <summary>
        /// Flag TCP associati all'evento (SYN, ACK, FIN, RST, ecc.).
        /// </summary>
        public TcpFlags Flags { get; set; }
    }
}
