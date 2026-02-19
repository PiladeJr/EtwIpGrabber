namespace EtwIpGrabber.EtwStructure.RealTimeConsumer
{
    public interface IRealtimeEtwConsumer : IDisposable
    {
        void Start(string sessionName);
    }

}
