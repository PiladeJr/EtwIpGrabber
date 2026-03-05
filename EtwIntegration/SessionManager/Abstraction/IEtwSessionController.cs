namespace EtwIpGrabber.EtwIntegration.SessionManager.Abstraction
{
    /// <summary>
    /// Definisce l'interfaccia per il controller del lifecycle di una sessione ETW.
    /// include i metodi per avviare, attaccare, fermare la sessione e proprietà per accedere
    /// </summary>
    /// <remarks>
    /// Il controller non assume ownership esclusiva
    /// della sessione ETW.
    /// 
    /// In caso di restart del servizio,
    /// può attaccarsi a una sessione esistente
    /// precedentemente creata dallo stesso nome.
    /// </remarks>
    public interface IEtwSessionController: IDisposable
    {
        void StartOrAttach();
        void Stop();
        ulong SessionHandle { get; }
        string SessionName { get; }
        bool IsRunning { get; }
    }
}
