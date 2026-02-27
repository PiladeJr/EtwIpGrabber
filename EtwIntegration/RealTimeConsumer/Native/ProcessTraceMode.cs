namespace EtwIpGrabber.EtwStructure.RealTimeConsumer.Native
{
    /// <summary>
    /// Valori bitflag usati per configurare la modalità di consumo
    /// nella chiamata a <c>OpenTrace</c> per il consumer realtime.
    /// </summary>
    /// <remarks>
    /// <list type="bullet">
    ///     <item><description><c>PROCESS_TRACE_MODE_REAL_TIME</c>: consente il consumo degli eventi in tempo reale.</description></item>
    ///     <item><description><c>PROCESS_TRACE_MODE_EVENT_RECORD</c>: richiede che gli eventi siano consegnati tramite <c>EVENT_RECORD_CALLBACK</c> in formato TDH-compatible (consente accesso a payload strutturati).</description></item>
    /// </list>
    ///
    /// L'uso combinato dei due flag abilita il parsing moderno tramite
    /// <c>EVENT_RECORD</c>, l'accesso ai payload strutturati e la compatibilità
    /// con provider manifest-based.
    /// </remarks>
    public static class ProcessTraceMode
    {
        /// <summary>
        /// Abilita il consumo realtime degli eventi (bit flag per OpenTrace).
        /// </summary>
        public const uint PROCESS_TRACE_MODE_REAL_TIME = 0x00000100;

        /// <summary>
        /// Richiede la consegna degli eventi tramite EVENT_RECORD_CALLBACK
        /// (TDH-compatible), necessario per provider manifest-based.
        /// </summary>
        public const uint PROCESS_TRACE_MODE_EVENT_RECORD = 0x10000000;
    }
}
