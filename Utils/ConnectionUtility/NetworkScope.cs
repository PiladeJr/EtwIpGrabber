namespace EtwIpGrabber.Utils.ConnectionUtility
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
