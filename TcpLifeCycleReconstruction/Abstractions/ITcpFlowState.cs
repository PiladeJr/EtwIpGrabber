using EtwIpGrabber.TcpLifeCycleReconstruction.Models;
using EtwIpGrabber.TdhParsing.Normalization.Models;

namespace EtwIpGrabber.TcpLifeCycleReconstruction.Abstractions
{
    internal interface ITcpFlowStore
    {
        TcpFlowInstance GetOrCreate(
            in TcpFlowKey key,
            in TcpEvent firstEvent);

        bool TryRemove(
            in TcpFlowKey key,
            out TcpFlowInstance? flow);

        IEnumerable<TcpFlowInstance> EnumerateActive();
    }
}
