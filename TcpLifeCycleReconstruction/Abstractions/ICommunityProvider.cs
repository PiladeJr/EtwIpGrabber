using EtwIpGrabber.TcpLifeCycleReconstruction.Models;

namespace EtwIpGrabber.TcpLifeCycleReconstruction.Abstractions
{
    internal interface ICommunityIdProvider
    {
        string Compute(in TcpCommunityIdKey key);
    }
}
