namespace EtwIpGrabber.EtwIntegration.SessionManager.Configuration
{
    /// <summary>
    /// Definisce il contratto minimo necessario per la configurazione di una sessione 
    /// ETW (Event Tracing for Windows), includendo le dimensioni dei buffer e
    /// modalità di registrazione.
    /// </summary>
    /// <remarks>
    /// Future Configurazioni dell'ETW, quali il SessionController, NativeMapper o il DiagnosticLayer
    /// si baseranno su questa interfaccia.</remarks>
    public interface IEtwSessionConfig
    {
        /// <summary>
        /// Nome univoco a livello kernel della sessione ETW, utilizzato per identificare e gestire 
        /// la sessione durante la sua durata.
        /// </summary>
        string SessionName { get; }
        /// <summary>
        /// La dimensione del singolo buffer in kilobyte (KB) utilizzato per la raccolta degli eventi. 
        /// </summary>
        /// <remarks>Un buffer di dimensioni più grande permetterà di gestire più eventi, a scapito
        /// della memoria allocata. Dosare le dimensioni del buffer in base al carico di payload
        /// previsto</remarks>
        uint BufferSizeKb { get; }
        /// <summary>
        /// La preallocazione minima di buffer da riservare per la sessione ETW. 
        /// </summary>
        /// <remarks>
        /// Questo valore è cruciale per assicurare che la sessione ETW abbia risorse sufficienti
        /// per gestire il carico di eventi previsto.</remarks>
        uint MinimumBuffers { get; }
        /// <summary>
        /// La preallocazione massima di buffer da riservare per la sessione ETW.
        /// </summary>
        /// <remarks>
        /// Il valore indica La burst capability della sessione, ovvero la capacità di 
        /// gestire picchi di eventi senza perdere dati.
        /// </remarks>
        uint MaximumBuffers { get; }
        /// <summary>
        /// Indica il tempo in secondi dopo il quale i buffer pieni vengono forzatamente
        /// svuotati e scritti su disco o inviati al consumatore in tempo reale.
        /// </summary>
        /// <remarks>
        /// Il FlushTimerSeconds è un parametro critico per bilanciare la latenza e l'efficienza della sessione ETW.
        /// </remarks>
        uint FlushTimerSeconds { get; }
        /// <summary>
        /// Valore per indicare se la sessione ETW deve operare in modalità tempo reale.
        /// </summary>
        /// <remarks>
        /// Per quest'implementazione, la modalità tempo reale è sempre abilitata, in quanto l'obiettivo del servizio
        /// è quello di fornire tutte le connessioni in tempo reale al consumatore. 
        /// Tuttavia, questa proprietà è esposta per consentire eventuali estensioni future
        /// </remarks>
        bool RealTimeMode { get; }
        /// <summary>
        /// Indica se la sessione ETW deve essere collegata al
        /// global kernel logger context (SystemTraceProvider).
        /// </summary>
        /// <remarks>
        /// Quando abilitato:
        /// <list type="bullet">
        ///   <item><description>la sessione utilizza l'unica istanza globale del kernel logger;</description></item>
        ///   <item><description>viene condivisa con strumenti di sistema (es. WPR, netsh trace);</description></item>
        ///   <item><description>può interferire con altri consumer kernel-level.</description></item>
        /// </list>
        /// 
        /// Questa modalità è richiesta esclusivamente per
        /// provider kernel legacy abilitati tramite EnableFlags.
        /// 
        /// Per provider manifest-based (es. Microsoft-Windows-TCPIP)
        /// deve restare disabilitata per evitare conflitti con
        /// sessioni di sistema esistenti.
        /// </remarks>
        bool SystemLoggerMode { get; }
        /// <summary>
        /// Rappresenta la bitmask combinata delle modalità di registrazione configurate per la sessione ETW nativa.
        /// </summary>
        /// <remarks>
        /// Il parametro viene esposto per permettere una mappatura su EVENT_TRACE_PROPERTIES.LogFileMode.
        /// </remarks>
        uint LogFileMode { get; }
    }
}

