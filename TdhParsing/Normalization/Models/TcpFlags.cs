namespace EtwIpGrabber.TdhParsing.Normalization.Models
{
    /// <summary>
    /// Rappresenta i flag TCP normalizzati
    /// estratti dal payload ETW.
    ///
    /// I valori sono derivati dal campo TcpFlags
    /// dell'evento runtime e convertiti in una
    /// rappresentazione semanticamente utilizzabile.
    /// </summary>
    public enum TcpFlags : byte
    {
        None = 0,
        SYN = 1,
        ACK = 2,
        FIN = 4,
        RST = 8
    }
}