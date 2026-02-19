namespace EtwIpGrabber.EtwStructure.SessionManager.Abstraction
{
    public interface IEtwSessionController: IDisposable
    {
        void StartOrAttach();
        void Stop();
        ulong SessionHandle { get; }
        string SessionName { get; }
        bool IsRunning { get; }
    }
}
