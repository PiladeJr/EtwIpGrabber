using EtwIpGrabber.TcpLifeCycleReconstruction.Models;
using System.Threading.Channels;

namespace EtwIpGrabber.Workers.FanOut
{
    public sealed class TcpPersistenceChannel(Channel<TcpConnectionLifecycle> channel)
    {
        public Channel<TcpConnectionLifecycle> Channel { get; } = channel;
    }
}