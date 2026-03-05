using EtwIpGrabber.TcpLifeCycleReconstruction.Models;
using System.Threading.Channels;

namespace EtwIpGrabber.Workers.FanOut
{
    public sealed class TcpLoggerChannel(Channel<TcpConnectionLifecycle> channel)
    {
        public Channel<TcpConnectionLifecycle> Channel { get; } = channel;
    }
}
