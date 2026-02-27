namespace EtwIpGrabber.TdhParsing.Normalization.Models
{
    /// <summary>
    /// Tipologia di evento TCP derivata
    /// dall'EventId ETW.
    ///
    /// Utilizzata per identificare lo
    /// stato della connessione durante
    /// la ricostruzione del lifecycle.
    /// </summary>
    public enum TcpEventType : byte
    {
        Connect,
        Accept,
        Disconnect,
        Retransmit,
        Close,
        Unknown
    }
}