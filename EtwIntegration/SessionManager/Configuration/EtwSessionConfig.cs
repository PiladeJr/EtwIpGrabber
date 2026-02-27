namespace EtwIpGrabber.EtwStructure.SessionManager.Configuration
{
    /// <summary>
    /// Rappresenta la configurazione immutabile di una sessione ETW realtime,
    /// incapsulando i parametri necessari per la creazione di una tracing session
    /// dedicata al consumo di provider manifest-based (es. Microsoft-Windows-TCPIP).
    /// </summary>
    /// <remarks>
    /// La configurazione definisce:
    /// <list type="bullet">
    ///   <item><description>topologia della sessione (private vs system logger);</description></item>
    ///   <item><description>buffering strategy del kernel ETW runtime;</description></item>
    ///   <item><description>flush latency;</description></item>
    ///   <item><description>modalità realtime di dispatch verso il consumer.</description></item>
    /// </list>
    /// Questa configurazione viene successivamente mappata su
    /// <c>EVENT_TRACE_PROPERTIES</c> durante l'inizializzazione della sessione.
    /// </remarks>
    public sealed class EtwSessionConfig : IEtwSessionConfig
    {
        public string SessionName { get; }

        public uint BufferSizeKb { get; }
        public uint MinimumBuffers { get; }
        public uint MaximumBuffers { get; }
        public uint FlushTimerSeconds { get; }

        public bool RealTimeMode { get; }
        public bool SystemLoggerMode { get; }

        public uint LogFileMode { get; }

        public EtwSessionConfig(
            string sessionName,
            BufferTuningProfile tuningProfile,
            bool realTime = true,
            bool systemLogger = false)
        {
            SessionName = sessionName;

            BufferSizeKb = tuningProfile.BufferSizeKb;
            MinimumBuffers = tuningProfile.MinimumBuffers;
            MaximumBuffers = tuningProfile.MaximumBuffers;
            FlushTimerSeconds = tuningProfile.FlushTimerSeconds;

            RealTimeMode = realTime;
            SystemLoggerMode = systemLogger;

            LogFileMode = ComposeLogMode();
        }
        /// <summary>
        /// Combina i valori delle bitmask per il LogFileMode.
        /// </summary>
        /// <remarks>
        /// ETW richiede che <c>EVENT_TRACE_PROPERTIES.LogFileMode</c>
        /// sia espresso come bitmask di flag Win32.
        /// 
        /// Questo metodo incapsula:
        /// <list type="bullet">
        ///   <item><description>EVENT_TRACE_REAL_TIME_MODE;</description></item>
        ///   <item><description>EVENT_TRACE_SYSTEM_LOGGER_MODE;</description></item>
        /// </list>
        /// evitando che il resto della pipeline debba conoscere
        /// direttamente i flag nativi.
        /// 
        /// Nel contesto di provider manifest-based (es. TCPIP),
        /// <c>SystemLoggerMode</c> deve restare disabilitato,
        /// in quanto il kernel logger globale è necessario solo
        /// per legacy kernel providers.
        /// </remarks>
        private uint ComposeLogMode()
        {
            const uint EVENT_TRACE_REAL_TIME_MODE = 0x00000100;
            const uint EVENT_TRACE_SYSTEM_LOGGER_MODE = 0x02000000;

            uint mode = 0;

            if (RealTimeMode)
                mode |= EVENT_TRACE_REAL_TIME_MODE;

            if (SystemLoggerMode)
                mode |= EVENT_TRACE_SYSTEM_LOGGER_MODE;

            return mode;
        }
    }
}
