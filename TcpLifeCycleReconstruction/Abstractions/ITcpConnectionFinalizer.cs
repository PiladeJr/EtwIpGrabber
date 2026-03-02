using EtwIpGrabber.TcpLifeCycleReconstruction.Models;

namespace EtwIpGrabber.TcpLifeCycleReconstruction.Abstractions
{
    internal interface ITcpConnectionFinalizer
    {
        TcpConnectionLifecycle Finalize(
            TcpFlowInstance flow);
    }
}
