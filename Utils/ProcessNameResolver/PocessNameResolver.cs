using System.Collections.Concurrent;
using System.Diagnostics;

namespace EtwIpGrabber.Utils.ProcessNameResolver
{
    /// <summary>
    /// Implementa la risoluzione runtime del nome del processo
    /// associato ad un identificatore PID estratto da un evento ETW TCP/IP,
    /// introducendo un meccanismo di caching per ridurre il costo
    /// delle operazioni di lookup.
    /// </summary>
    /// <remarks>
    /// Rappresenta uno step opzionale della fase di normalizzazione,
    /// arricchisce il modello <see cref="TcpEvent"/>
    /// con un informazione aggiuntiva utile per analisi e monitoraggio future.
    ///
    /// <para>
    /// Il nome del processo viene recuperato tramite
    /// <see cref="System.Diagnostics.Process.GetProcessById(int)"/>.
    /// </para>
    ///
    /// <para>
    /// Poiché il PID è derivato da un evento ETW kernel-level:
    /// </para>
    /// <list type="bullet">
    /// <item>il processo potrebbe essere già terminato al momento della risoluzione;</item>
    /// <item>il servizio potrebbe non avere privilegi sufficienti;</item>
    /// <item>il processo potrebbe essere protetto (PPL).</item>
    /// </list>
    ///
    /// In tali casi, la risoluzione può fallire senza compromettere
    /// la pipeline di parsing. <b>Avviare l'applicazione come servizio localSystem o 
    /// come applicazione con permessi di amministratore per ridurre le possibilità che ciò accada</b>
    /// </remarks>

    internal sealed class ProcessNameResolver : IProcessNameResolver
    {
        /// <summary>
        /// Dizionario in memoria per la cache dei nomi dei processi, 
        /// con PID come chiave e nome del processo come valore.
        /// </summary>
        private readonly ConcurrentDictionary<uint, string> _cache = new();
        /// <summary>
        /// Risolve il nome di un processo dato il suo PID e lo memorizza
        /// in una cache in memoria per accessi futuri.
        /// </summary>
        /// <param name="pid">l'id del processo da cui estrarre il nome</param>
        /// <returns>il nome del processo risolto o una stringa di fallback in caso di errore</returns>
        public string Resolve(uint pid)
        {
            if (pid == 0)
                return "System";

            return _cache.GetOrAdd(pid, GetProcessNameByPID);
        }
        /// <summary>
        /// Recupera il nome di un processo dato il suo PID.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Nella pipeline di elaborazione degli eventi ETW e successivo parsing con TDH,
        /// non mi è possibile recuperare il nome del processo in fase di parsing, in quanto 
        /// non è presente come proprietà nell'EVENT_HEADER degli eventi TCPIP. Nemmeno all'interno di
        /// EVENT_HEADER_EXTENDED_DATA_ITEM. L'unica informazione disponibile è il PID del processo associato.
        /// </para>
        /// Per ovviare a ciò, mi avvalgo dell'API di sistema <c>Process.GetProcessById</c> 
        /// per recuperare il processo associato al PID e da esso estraggo il nome.
        /// Questa operazione è relativamente costosa, quindi implemento una cache in memoria per evitare 
        /// chiamate ripetute per lo stesso PID.
        /// </remarks>
        /// <param name="pid">L'id del processo ottenuto dal parsing dell'evento TCP e da cui estrarre il nome</param>
        /// <returns>Il nome del processo recuperato</returns>
        private static string GetProcessNameByPID(uint pid)
        {
            try
            {
                using var proc = Process.GetProcessById((int)pid);
                return proc.ProcessName;
            }
            catch
            {
                // Ricopre il caso in cui la chiamata ritorna un codice di errore
                // per privilegi insufficienti (AccessDenied) o per processi
                // terminati (NoSuchProcess), o altri codici di errori.
                // *IMPORTANTE* avviare l'applicazione come servizio di sistema o come amministratore
                return "Exited"; 
            }
        }
    }
}
