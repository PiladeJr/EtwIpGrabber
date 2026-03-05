namespace EtwIpGrabber.TcpLifeCycleReconstruction.Models.Enumerations
{
    public enum TcpLifecycleState : byte
    {
        New,
        Connecting,
        Established,
        Closing,
        Closed,
        Aborted,
        TimedOut
    }
}
