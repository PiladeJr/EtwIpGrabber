using EtwIpGrabber.TcpLifeCycleReconstruction.Abstractions;
using EtwIpGrabber.TcpLifeCycleReconstruction.Models;
using EtwIpGrabber.Utils.ConnectionUtility;

namespace EtwIpGrabber.TcpLifeCycleReconstruction.Finalization
{
    internal sealed class TcpConnectionFinalizer(
        ICommunityIdProvider communityId) : ITcpConnectionFinalizer
    {
        private readonly ICommunityIdProvider _communityId = communityId;

        public TcpConnectionLifecycle Finalize(
            TcpFlowInstance flow)
        {
            var startAt = flow.FirstSeenUtc;

            var endAt = flow.EndUtc ?? flow.LastSeenUtc;

            if (endAt < startAt) endAt = startAt;

            var duration = endAt - startAt;

            var CommunityId = _communityId.Compute(
                new TcpCommunityIdKey(
                    flow.Key.LocalIp,
                    flow.Key.LocalPort,
                    flow.Key.RemoteIp,
                    flow.Key.RemotePort
                ));

            return new TcpConnectionLifecycle
            {
                ProcessId = flow.Key.ProcessId,
                ProcessName = flow.ProcessName,

                LocalIP = flow.Key.LocalIp,
                LocalPort = flow.Key.LocalPort,

                RemoteIP = flow.Key.RemoteIp,
                RemotePort = flow.Key.RemotePort,

                Direction = ConnectionUtils.DetermineDirection(flow),
                Classification = ConnectionUtils.ClassifyConnection(flow.Key.LocalIp,flow.Key.RemoteIp),

                StartAt = startAt,
                EndAt = endAt,
                Duration = duration,

                Outcome = ConnectionUtils.DetermineOutcome(flow),
                Handshake = ConnectionUtils.DetermineStage(flow),

                CommunityId = CommunityId
            };
        }
    }
}
