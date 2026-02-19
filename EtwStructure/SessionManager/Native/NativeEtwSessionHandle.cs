namespace EtwIpGrabber.EtwStructure.SessionManager.Native
{
    /// <summary>
    /// Separa la gestione dell'handle della sessione ETW.
    /// </summary>
    /// <remarks>
    /// Isola il TRACEHANDLE (alias EVENT_TRACE_SESSION_INFO.Handle) e fornisce un'interfaccia semplice 
    /// per gestirlo.
    /// </remarks>
    public sealed class NativeEtwSessionHandle
    {
        public ulong Handle { get; private set; }

        public void Set(ulong handle)
        {
            Handle = handle;
        }

        public bool IsValid => Handle != 0;
    }
}
