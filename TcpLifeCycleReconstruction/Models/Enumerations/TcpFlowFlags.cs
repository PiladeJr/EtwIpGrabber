namespace EtwIpGrabber.TcpLifeCycleReconstruction.Models.Enumerations
{
    [Flags]
    public enum TcpFlowFlags
    {
        None = 0,
        Connect = 1 << 0,
        Accept = 1 << 1,
        Send = 1 << 2,
        Receive = 1 << 3,
        Disconnect = 1 << 4,
        Retransmit = 1 << 5,
        Reconnect = 1 << 6,
        Fail = 1 << 7
      
    }
}
