namespace EtwIpGrabber.TdhParsing.Normalization.Models
{
    /// <summary>
    /// Evento TCP normalizzato pronto
    /// per le fasi successive di:
    /// 
    /// - Lifecycle Reconstruction
    /// - Community-ID Generation
    /// - Persistenza
    /// 
    /// Tutti i valori sono:
    /// - convertiti in host byte order
    /// - semanticamente interpretabili
    /// </summary>
    public sealed class TcpEvent
    {
        public DateTime TimestampUtc { get; set; }
        public uint ProcessId { get; set; }

        public uint LocalIP { get; set; }
        public ushort LocalPort { get; set; }

        public uint RemoteIP { get; set; }
        public ushort RemotePort { get; set; }

        public TcpDirection Direction { get; set; }
        public TcpEventType EventType { get; set; }
        public TcpFlags Flags { get; set; }
    }
}
