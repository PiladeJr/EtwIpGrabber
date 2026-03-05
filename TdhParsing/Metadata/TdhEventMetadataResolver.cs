using EtwIpGrabber.EtwIntegration.RealTimeConsumer.Native.Structures;
using EtwIpGrabber.TdhParsing.Metadata.Abstract;
using EtwIpGrabber.TdhParsing.Metadata.Native;
using System.Runtime.InteropServices;

namespace EtwIpGrabber.TdhParsing.Metadata
{
    /// <summary>
    /// Responsabile della risoluzione e caching delle informazioni di decoding TDH 
    /// (<c>TRACE_EVENT_INFO</c>) per eventi ETW TCPIP.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Questa classe implementa la trasformazione <c>EVENT_RECORD</c> → <c>TRACE_EVENT_INFO</c>
    /// utilizzando <c>TdhGetEventInformation</c>.
    /// </para>
    /// <para>
    /// Il risultato viene memorizzato nel <see cref="TraceEventInfoBufferPool"/> per evitare 
    /// lookup ripetuti del manifest runtime.
    /// </para>
    /// <para><b>PERFORMANCE CRITICAL:</b></para>
    /// <para>
    /// <c>TdhGetEventInformation</c> è una chiamata relativamente costosa. Invocarla per ogni 
    /// evento degrada significativamente il throughput del consumer ETW realtime.
    /// </para>
    /// <para><b>VERSION AWARE:</b></para>
    /// <para>
    /// Il layout delle proprietà TCPIP può variare tra:
    /// </para>
    /// <list type="bullet">
    ///   <item><description>Windows 10</description></item>
    ///   <item><description>Windows 11</description></item>
    ///   <item><description>build del provider</description></item>
    /// </list>
    /// <para>
    /// Il caching deve quindi essere discriminato mediante <see cref="TdhEventKey"/>.
    /// </para>
    /// <para><b>ABI REQUIREMENTS:</b></para>
    /// <para>
    /// L'input <c>TDH_EVENT_RECORD</c> deve essere:
    /// </para>
    /// <list type="bullet">
    ///   <item><description>ricostruito runtime</description></item>
    ///   <item><description>pinned</description></item>
    ///   <item><description>ABI-compatible</description></item>
    /// </list>
    /// <para>
    /// in quanto TDH accede direttamente ai campi <c>EventHeader</c>, <c>UserData</c> 
    /// e <c>UserDataLength</c>.
    /// </para>
    /// </remarks>
    internal unsafe sealed class TdhEventMetadataResolver(TraceEventInfoBufferPool pool) : IEtwMetadataResolver
    {
        private readonly TraceEventInfoBufferPool _pool = pool;
        
        /// <summary>
        /// Restituisce il buffer <c>TRACE_EVENT_INFO</c> associato ad un evento ETW.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Se presente nel pool, restituisce il metadata cached; altrimenti:
        /// </para>
        /// <list type="number">
        ///   <item><description>invoca <c>TdhGetEventInformation</c></description></item>
        ///   <item><description>alloca buffer unmanaged</description></item>
        ///   <item><description>memorizza il risultato nel pool</description></item>
        /// </list>
        /// <para>
        /// Il buffer restituito deve essere considerato read-only e lifetime-managed dal pool.
        /// </para>
        /// <para><b>IMPORTANTE:</b> Non deve essere liberato manualmente dal caller.</para>
        /// </remarks>
        /// <param name="replayRecord">
        /// <c>EVENT_RECORD</c> ricostruito runtime tramite <c>ReplayContext</c>.
        /// Deve contenere puntatori <c>UserData</c> pinned.
        /// </param>
        /// <returns>Handle al buffer contenente <c>TRACE_EVENT_INFO</c>.</returns>
        public TraceEventInfoHandle Resolve(
            TDH_EVENT_RECORD* replayRecord)
        {
            var key = new TdhEventKey(
                replayRecord->EventHeader);

            return _pool.GetOrAdd(
                key,
                () => BuildTraceEventInfo(
                    replayRecord));
        }

        /// <summary>
        /// Esegue la risoluzione runtime del metadata TDH per un evento ETW.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Implementa il pattern a due fasi richiesto da <c>TdhGetEventInformation</c>:
        /// </para>
        /// <list type="number">
        ///   <item><description>Prima chiamata con dimensione buffer = 0 per ottenere la dimensione richiesta dinamicamente</description></item>
        ///   <item><description>Seconda chiamata per l'allocazione unmanaged del buffer</description></item>
        ///   <item><description>Fetch del <c>TRACE_EVENT_INFO</c></description></item>
        /// </list>
        /// <para>
        /// Il buffer risultante contiene <c>TRACE_EVENT_INFO</c> + <c>EVENT_PROPERTY_INFO[]</c>,
        /// utilizzato nelle fasi successive di layout discovery e property walk.
        /// </para>
        /// <para><b>IMPORTANTE:</b> In caso di fallimento della seconda chiamata, 
        /// il buffer viene esplicitamente liberato per evitare memory leak.</para>
        /// </remarks>
        /// <param name="record">Puntatore al record ETW da processare.</param>
        /// <returns>Handle al buffer contenente le informazioni TDH.</returns>
        /// <exception cref="InvalidOperationException"/>
        /// Generata se <c>TdhGetEventInformation</c> fallisce in una delle due fasi.
        private static TraceEventInfoHandle BuildTraceEventInfo(
            TDH_EVENT_RECORD* record)
        {
            // Codici di errore TDH dichiarati esplicitamente
            const uint ERROR_SUCCESS = 0;
            const uint ERROR_INSUFFICIENT_BUFFER = 122;

            uint size = 0;
            // Prima chiamata per ottenere la dimensione richiesta
            var status =
                TdhNativeMethods.TdhGetEventInformation(
                    record,
                    0,
                    IntPtr.Zero,
                    IntPtr.Zero, //buffer dichiarato nullo appositamente
                    ref size);
            // TDH dovrebbe restituire ERROR_INSUFFICIENT_BUFFER
            // per indicare la dimensione necessaria, errori diversi indicano un fallimento
            if (status != ERROR_INSUFFICIENT_BUFFER)
                throw new InvalidOperationException(
                    $"TDH size query failed: {status}");
            // Allocazione unmanaged del buffer con la dimensione restituita
            var buffer =
                Marshal.AllocHGlobal((int)size);
            // Seconda chiamata per ottenere il TRACE_EVENT_INFO
            status =
                TdhNativeMethods.TdhGetEventInformation(
                    record,
                    0,
                    IntPtr.Zero,
                    buffer,
                    ref size);
            // Se la chiamata fallisce, liberare il buffer allocato per evitare memory leak
            // Successivamente lanciare un'eccezione per segnalare il fallimento
            if (status != ERROR_SUCCESS)
            {
                Marshal.FreeHGlobal(buffer);
                throw new InvalidOperationException(
                    $"TDH fetch failed: {status}");
            }
            return new TraceEventInfoHandle(
                buffer,
                size);
        }
    }
}
