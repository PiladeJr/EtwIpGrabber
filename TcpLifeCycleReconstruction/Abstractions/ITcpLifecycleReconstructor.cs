using EtwIpGrabber.TdhParsing.Normalization.Models;

namespace EtwIpGrabber.TcpLifeCycleReconstruction.Abstractions
{
    internal interface ITcpLifecycleReconstructor
    {
        void Process(in TcpEvent evt);
    }
}
