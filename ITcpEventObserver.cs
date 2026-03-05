using EtwIpGrabber.TcpLifeCycleReconstruction.Models;
using EtwIpGrabber.TdhParsing.Normalization.Models;

namespace EtwIpGrabber
{
    public interface ITcpEventObserver
    {
        void OnTcpEvent(TcpEvent evt, TcpFlowInstance flow);
    }
}
