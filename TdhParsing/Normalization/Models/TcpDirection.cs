namespace EtwIpGrabber.TdhParsing.Normalization.Models
{
    /// <summary>
    /// Direzione della connessione TCP
    /// rispetto all'host locale.
    ///
    /// Il valore è ottenuto dal campo
    /// Direction dell'evento ETW.
    /// </summary>
    public enum TcpDirection : byte
    {
        Inbound,
        Outbound,
        Unknown
    }
}