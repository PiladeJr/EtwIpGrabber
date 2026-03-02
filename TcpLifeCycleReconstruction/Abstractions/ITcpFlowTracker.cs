using EtwIpGrabber.TcpLifeCycleReconstruction.Models;
using EtwIpGrabber.TdhParsing.Normalization.Models;

namespace EtwIpGrabber.TcpLifeCycleReconstruction.Abstractions
{
    internal interface ITcpFlowTracker
    {
        TcpFlowInstance GetOrCreate(
            in TcpEvent evt);

        bool ShouldSplit(
            TcpFlowInstance flow,
            DateTime ts);
    }
}
