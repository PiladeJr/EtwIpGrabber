using EtwIpGrabber.EtwStructure.RealTimeConsumer.Native.Structures;
using System.Runtime.InteropServices;

namespace EtwIpGrabber.EtwStructure.RealTimeConsumer.Native
{
    /// <summary>
    /// Wrapper P/Invoke per le API ETW utilizzate dal consumer realtime.
    /// </summary>
    /// <remarks>
    /// Le API esposte consentono di collegarsi a una sessione ETW attiva
    /// (creata tramite <c>StartTrace</c>) e ricevere eventi in streaming:
    /// <list type="bullet">
    ///   <item><description><c>OpenTrace</c> — apre una sessione ETW esistente in modalità realtime.</description></item>
    ///   <seealso href="https://learn.microsoft.com/it-it/windows/win32/api/evntrace/nf-evntrace-opentracew">OpenTrace (MSDN)</seealso>
    ///   <item><description><c>ProcessTrace</c> — avvia il loop di ricezione degli eventi (bloccante).</description></item>
    ///   <seealso href="https://learn.microsoft.com/it-it/windows/win32/api/evntrace/nf-evntrace-processtrace">ProcessTrace (MSDN)</seealso>    
    ///   <item><description><c>CloseTrace</c> — chiude il consumer e rilascia le risorse associate.</description></item>
    ///   <seealso href="https://learn.microsoft.com/it-it/windows/win32/api/evntrace/nf-evntrace-closetrace">CloseTrace (MSDN)</seealso>
    /// </list>
    /// <para>
    /// Il loop di <c>ProcessTrace</c> è bloccante e deve essere eseguito su un thread dedicato.
    /// </para> 
    /// </remarks>
    internal static class NativeEtwConsumer
    {
        /// <summary>
        /// Apri una sessione ETW in modalità realtime e restituisce un handle
        /// da usare con <c>ProcessTrace</c> e <c>CloseTrace</c>.
        /// </summary>
        /// <param name="logfile">Struttura <c>EVENT_TRACE_LOGFILE</c> inizializzata dal chiamante.</param>
        /// <returns>Un valore di tipo <c>TRACEHANDLE</c> (mappato a <c>ulong</c>).</returns>
        [DllImport("advapi32.dll", CharSet = CharSet.Unicode)]
        public static extern ulong OpenTrace(
            ref EVENT_TRACE_LOGFILE logfile);

        /// <summary>
        /// Avvia il loop di elaborazione degli eventi per gli handle forniti.
        /// </summary>
        /// <param name="handleArray">Array di trace handle ottenuti da <c>OpenTrace</c>.</param>
        /// <param name="handleCount">Numero di handle nel <paramref name="handleArray"/>.</param>
        /// <param name="startTime">Timestamp di inizio (opzionale, normalmente <c>IntPtr.Zero</c>).</param>
        /// <param name="endTime">Timestamp di fine (opzionale, normalmente <c>IntPtr.Zero</c>).</param>
        /// <returns>Codice di errore Win32 (0 indica successo).</returns>
        [DllImport("advapi32.dll")]
        public static extern uint ProcessTrace(
            ulong[] handleArray,
            uint handleCount,
            IntPtr startTime,
            IntPtr endTime);

        /// <summary>
        /// Chiude il trace consumer associato all'handle fornito e rilascia
        /// le risorse allocate dal sistema.
        /// </summary>
        /// <param name="traceHandle">Handle restituito da <c>OpenTrace</c>.</param>
        /// <returns>Codice di errore Win32 (0 indica successo).</returns>
        [DllImport("advapi32.dll")]
        public static extern uint CloseTrace(ulong traceHandle);
    }

}
