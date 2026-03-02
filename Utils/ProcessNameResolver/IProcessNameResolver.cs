namespace EtwIpGrabber.Utils.ProcessNameResolver
{
    /// <summary>
    /// Definisce il contratto per la risoluzione del nome del processo
    /// associato ad un identificatore PID estratto da un evento ETW TCP/IP.
    /// </summary> 
    /// <remarks>
    /// L'implementazione concreta può prevedere:
    /// <list type="bullet">
    /// <item>caching dei risultati;</item>
    /// <item>gestione dei processi terminati;</item>
    /// <item>fallback su nomi sconosciuti;</item>
    /// <item>restrizioni di accesso a processi protetti.</item>
    /// </list>
    /// </remarks>

    public interface IProcessNameResolver
    {
        /// <summary>
        /// Risolve il nome del processo associato al PID specificato.
        /// </summary>
        /// <param name="pid">
        /// Identificatore del processo ottenuto dal contesto ETW runtime.
        /// </param>
        /// <returns>
        /// Nome del processo se disponibile; in caso contrario,
        /// una stringa di fallback definita dall'implementazione.
        /// </returns>
        string Resolve(uint pid);
    }
}
