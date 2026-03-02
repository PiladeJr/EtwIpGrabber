namespace EtwIpGrabber.TcpLifeCycleReconstruction.Models
{
    internal enum TcpLifecycleState : byte
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
