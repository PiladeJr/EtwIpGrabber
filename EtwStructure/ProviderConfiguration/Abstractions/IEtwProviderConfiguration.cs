namespace EtwIpGrabber.EtwStructure.ProviderConfiguration.Abstractions
{
    /// <summary>
    /// Definisce il contratto per l'abilitazione di provider ETW manifest-based
    /// su una sessione già attiva.
    /// </summary>
    /// <remarks>
    /// Questo layer è responsabile esclusivamente dell'enablement del provider
    /// tramite <c>EnableTraceEx2</c> e non deve conoscere:
    /// <list type="bullet">
    ///   <item><description>parsing degli eventi;</description></item>
    ///   <item><description>pipeline di consumo;</description></item>
    ///   <item><description>logica TCP;</description></item>
    ///   <item><description>persistenza.</description></item>
    /// </list>
    /// L'abilitazione avviene a livello di sessione ETW e non interferisce con
    /// altri consumer presenti nel sistema (es. Windows Defender).
    /// <br/>
    /// I manifest providers sono registrati globalmente nel sistema,
    /// ma lo stato di abilitazione (Level/Keywords) è mantenuto
    /// per-sessione dal sottosistema ETW.
    /// </remarks>
    public interface IEtwProviderConfigurator
    {
        /// <summary>
        /// Abilita il provider ETW sulla sessione specificata.
        /// </summary>
        /// <param name="sessionHandle">
        /// Handle kernel (<c>TRACEHANDLE</c>) della sessione ETW ottenuto da <c>StartTrace()</c>.
        /// </param>
        void EnableProvider(ulong sessionHandle);
    }
}
