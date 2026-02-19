namespace EtwIpGrabber.EtwStructure.SessionManager.Configuration
{
    /// <summary>
    /// Classe responsabile di rappresentare la configurazione immutabile di una sessione ETW,
    /// incapsulando i parametri necessari per la creazione e gestione della sessione.
    /// Specifica i parametri di <see cref="IEtwSessionConfig"/> 
    /// </summary>
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
            bool systemLogger = true)
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
        /// ETW richiede EVENT_TRACE_PROPERTIES.LogFileMode come bitmask di flag, quindi questa funzione
        /// combina i flag necessari in base alle proprietà RealTimeMode e SystemLoggerMode.
        /// Evita che il consumer conosca i flag win32, previene combinazioni non valide (es. RealTimeMode=true e SystemLoggerMode=true)
        /// e mantiene l'implementazione dei flag incapsulata all'interno di questa classe. 
        /// Prevede future estensioni come EVENT_TRACE_PRIVATE_LOGGER_MODE
        /// </remarks>
        /// <returns></returns>
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
