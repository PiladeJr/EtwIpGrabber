namespace EtwIpGrabber.TcpLifeCycleReconstruction.Models.Enumerations
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
