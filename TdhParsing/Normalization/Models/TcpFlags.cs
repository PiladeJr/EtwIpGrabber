namespace EtwIpGrabber.TdhParsing.Normalization.Models
{
    public enum TcpFlags : byte
    {
        None = 0,
        SYN = 1,
        ACK = 2,
        FIN = 4,
        RST = 8
    }
}