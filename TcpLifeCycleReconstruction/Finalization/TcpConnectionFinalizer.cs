

using EtwIpGrabber.TcpLifeCycleReconstruction.Abstractions;
using EtwIpGrabber.TcpLifeCycleReconstruction.Models;

namespace EtwIpGrabber.TcpLifeCycleReconstruction.Finalization
{
    internal sealed class TcpConnectionFinalizer
        : ITcpConnectionFinalizer
    {
        private readonly ICommunityIdProvider _communityId;

        public TcpConnectionFinalizer(
            ICommunityIdProvider communityId)
        {
            _communityId = communityId;
        }

        public TcpConnectionLifecycle Finalize(
            TcpFlowInstance flow)
        {
            var startAt = flow.FirstSeenUtc;

            var endAt = flow.EndUtc ?? flow.LastSeenUtc;

            if (endAt < startAt)
                endAt = startAt;

            var duration = endAt - startAt;

            var communityKey = new TcpCommunityIdKey(
                flow.Key.LocalIp,
                flow.Key.LocalPort,
                flow.Key.RemoteIp,
                flow.Key.RemotePort);

            var communityId =
                _communityId.Compute(communityKey);

            return new TcpConnectionLifecycle
            {
                ProcessId = flow.Key.ProcessId,
                ProcessName = string.Empty, // opzionale, se disponibile

                LocalIP = flow.Key.LocalIp,
                LocalPort = flow.Key.LocalPort,

                RemoteIP = flow.Key.RemoteIp,
                RemotePort = flow.Key.RemotePort,

                StartAt = startAt,
                EndAt = endAt,
                Duration = duration,

                CommunityId = communityId
            };
        }
    }
}
