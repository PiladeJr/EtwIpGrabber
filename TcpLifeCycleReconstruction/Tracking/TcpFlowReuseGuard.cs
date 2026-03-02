using EtwIpGrabber.TcpLifeCycleReconstruction.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EtwIpGrabber.TcpLifeCycleReconstruction.Tracking
{
    internal sealed class TcpFlowReuseGuard(TimeSpan? reuseThreshold = null)
    {
        private readonly TimeSpan _reuseThreshold =
                reuseThreshold ?? TimeSpan.FromSeconds(2);

        public bool ShouldSplit(
            TcpFlowInstance flow,
            DateTime incomingTs)
        {
            // still active → same lifecycle
            if (flow.State != TcpLifecycleState.Closed &&
               flow.State != TcpLifecycleState.Aborted &&
               flow.State != TcpLifecycleState.TimedOut)
                return false;

            // closed long ago → new lifecycle
            return incomingTs - flow.LastSeenUtc
                > _reuseThreshold;
        }
    }
}
