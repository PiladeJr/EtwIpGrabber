using EtwIpGrabber.TcpLifeCycleReconstruction.Models;
using System.Threading.Channels;

namespace EtwIpGrabber.Workers.Data
{
    public sealed class TcpFlowPersistenceChannel(Channel<TcpFlowInstance> channel)
    {
        public Channel<TcpFlowInstance> Channel { get; } = channel;
    }
}
