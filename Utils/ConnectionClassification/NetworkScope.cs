namespace EtwIpGrabber.Utils.ConnectionClassification
{
    public enum NetworkScope : byte 
    {
        Loopback,
        Multicast,
        Broadcast,
        Private,
        Public,
        Special,
        Unknown
    }
}
