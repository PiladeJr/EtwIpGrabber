using System.Runtime.InteropServices;

namespace EtwIpGrabber.EtwStructure.ProviderConfiguration.Native
{
    /// <summary>
    /// Parametri usati da <c>EnableTraceEx2</c> per configurare l'enablement
    /// di un manifest provider ETW.
    /// </summary>
    /// <remarks>
    /// Questa struct permette di:
    /// <list type="bullet">
    ///   <item><description>abilitare proprietà aggiuntive lato kernel;</description></item>
    ///   <item><description>configurare filtri;</description></item>
    ///   <item><description>associare metadata di correlazione.</description></item>
    /// </list>
    /// Version:
    /// <list type="bullet">
    ///   <item><description>deve essere impostata a <c>2</c> per abilitare le funzionalità moderne;</description></item>
    ///   <item><description>non esiste una <c>ENABLE_TRACE_PARAMETERS_V2</c>: la versione è gestita tramite il campo <c>Version</c>.</description></item>
    /// </list>
    /// EnableProperty:
    /// <list type="bullet">
    ///   <item><description>permette di richiedere enrichment degli eventi lato kernel;</description></item>
    ///   <item><description><c>EVENT_ENABLE_PROPERTY_PROCESS_START_KEY</c> abilita la correlazione
    /// tramite Process Start Key, evitando errori dovuti al riuso dei PID.</description></item>
    /// </list>
    /// EnableFilterDesc:
    /// <list type="bullet">
    ///   <item><description>puntatore opzionale a filtri lato provider (non usato in questa fase).</description></item>
    /// </list>
    /// </remarks>
    [StructLayout(LayoutKind.Sequential)]
    internal struct ENABLE_TRACE_PARAMETERS
    {
        public uint Version;
        public uint EnableProperty;
        public uint ControlFlags;
        public Guid SourceId;
        public IntPtr EnableFilterDesc;
        public uint FilterDescCount;
    }
    /// <summary>
    /// Wrapper P/Invoke per la funzione Win32 <c>EnableTraceEx2</c>.
    /// </summary>
    /// <remarks>
    /// Questa API abilita un provider ETW manifest-based su una sessione attiva
    /// in modalità realtime o file-based.
    ///
    /// A differenza dei kernel providers legacy (abilitati tramite EnableFlags),
    /// i manifest providers:
    /// <list type="bullet">
    ///   <item><description>sono isolati per sessione;</description></item>
    ///   <item><description>supportano Level + Keyword filtering;</description></item>
    ///   <item><description>espongono eventi TDH-compatible;</description></item>
    ///   <item><description>non interferiscono con altri consumer di sistema.</description></item>
    /// </list>
    /// Utilizzata per abilitare:
    /// <list type="bullet">
    ///   <item><description><c>Microsoft-Windows-TCPIP</c> sulla sessione ETW creata dal SessionController.</description></item>
    /// </list>
    /// </remarks>
    internal static class EtwEnableTraceNative
    {
        [DllImport("advapi32.dll", CharSet = CharSet.Unicode)]
        public static extern uint EnableTraceEx2(
            ulong TraceHandle,
            ref Guid ProviderId,
            uint ControlCode,
            byte Level,
            ulong MatchAnyKeyword,
            ulong MatchAllKeyword,
            uint Timeout,
            IntPtr EnableParameters);
    }
}
