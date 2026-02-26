namespace EtwIpGrabber.TdhParsing.Normalization.Models
{
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