namespace EtwIpGrabber.TcpLifeCycleReconstruction.Models.Enumerations
{
    /// <summary>
    /// Enum di utilità per rappresentare l'esito di una connessione TCP, 
    /// utile per la ricostruzione del ciclo di vita delle connessioni TCP.
    /// </summary>
    /// <remarks>
    /// Gli eventi ottenuti dal provider ETW <c>Microsoft-Windows-TCPIP</c>
    /// NON forniscono un'informazione esplicita sul flag della connessione
    /// (SYN,SYN-ACK,ACK.RST,FIN,PSH,...). il provider espone solo l'id dell'evento
    /// (Connect,Accept,Close,Disconnect,Retransmit) e le informazioni di contesto (PID, IP, Port,...).
    /// <para>
    /// Posso determinare l'esito di una connessione però osservando la sequenza di eventi che
    /// si verificano per una data connessione e da lì determinare se la connessione è stata
    /// stabilita, rifiutata, chiusa, abortita o se è andata in timeout.
    /// </para>
    /// </remarks>
    public enum TcpConnectionOutcome
    {
        /// <summary>
        /// Outcome di default, quando non è possibile determinare l'esito della connessione TCP.
        /// </summary>
        Unknown,
        /// <summary>
        /// Un evento connect (<b>Opzionale</b>, alcuni eventi emettono solo accept e close),
        /// seguito da un accept e un close alla fine
        /// </summary>
        Established,
        /// <summary>
        /// un evento connect seguito da un disconnect o da un close senza un accept.
        /// </summary>
        Refused,
        /// <summary>
        /// un evento connect senza accept o close. L'evento viene chiuso dallo sweeper dopo un timeout stabilito
        /// </summary>
        Timeout,
        /// <summary>
        /// una connessione che presenta gli eventi connect o accept e seguita da un disconnect
        /// </summary>
        Aborted,
        /// <summary>
        /// una connessione stabilita con un evento close alla fine. si differenzia dall'established 
        /// puramente per l'emissione di una duration.
        /// </summary>
        Closed
    }
    /// <summary>
    /// Enum di utilità per specificare lo stadio del processo di handshake TCP
    /// in cui si ferma una connessione
    /// </summary>
    /// <remarks>
    /// Similmente all'enum <see cref="TcpConnectionOutcome"/> anche in questo caso non è 
    /// possibile determinare lo stadio del processo di handshake TCP. Posso però
    /// fare un ipotesi osservando la sequenza di eventi che si verificano per una data connessione 
    /// e da lì determinare se la connessione è stata
    /// </remarks>
    public enum TcpHandshakeStage
    {
        /// <summary>
        /// Stato di default quando non è possibile determinare lo stadio di una connessione -> Unknown
        /// </summary>
        None,
        /// <summary>
        /// un evento che ha visto un connect
        /// </summary>
        SynSent,
        /// <summary>
        /// un evento che ha visto un accept
        /// </summary>
        Established,
        /// <summary>
        /// un evento che ha visto un disconnect o un close
        /// </summary>
        Closing
    }
}
