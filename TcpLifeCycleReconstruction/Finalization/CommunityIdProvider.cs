using EtwIpGrabber.TcpLifeCycleReconstruction.Abstractions;
using EtwIpGrabber.TcpLifeCycleReconstruction.Models;
using EtwIpGrabber.Utils.CommunityIdResolver;
using System.Net;

namespace EtwIpGrabber.TcpLifeCycleReconstruction.Finalization
{
    internal sealed class CommunityIdProvider(CommunityIDGenerator generator) : ICommunityIdProvider
    {
        private readonly CommunityIDGenerator _generator = generator;

        public string Compute(in TcpCommunityIdKey key)
        {
            var src = new IPAddress(key.IpA);
            var dst = new IPAddress(key.IpB);

            return _generator.community_id_v1(
                src,
                dst,
                key.PortA,
                key.PortB,
                CommunityIDGenerator.Protocol.TCP);
        }
    }
}
