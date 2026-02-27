using EtwIpGrabber.EtwStructure.RealTimeConsumer.Native.Structures;

namespace EtwIpGrabber.TdhParsing.Metadata.Abstract
{
    /// <summary>
    /// Contratto per la risoluzione dinamica del metadata TDH associato a un
    /// evento ETW manifest-based.
    /// </summary>
    /// <remarks>
    /// Questa interfaccia rappresenta il punto di ingresso della pipeline di parsing TDH,
    /// responsabile della trasformazione:
    /// <code>
    /// EVENT_RECORD → TRACE_EVENT_INFO
    /// </code>
    /// Il metadata risultante (<c>TRACE_EVENT_INFO</c>) conterrà:
    /// <list type="bullet">
    ///   <item><description>numero di proprietà (<c>PropertyCount</c>);</description></item>
    ///   <item><description>descrittori delle proprietà (<c>EVENT_PROPERTY_INFO[]</c>);</description></item>
    ///   <item><description>informazioni di tipo (<c>InType</c>/<c>OutType</c>);</description></item>
    ///   <item><description>eventuali mapping (<c>MapNameOffset</c>).</description></item>
    /// </list>
    /// Tali informazioni sono necessarie per:
    /// <list type="bullet">
    ///   <item><description>il property walk runtime;</description></item>
    ///   <item><description>il decoding dinamico tramite <c>TdhFormatProperty</c>.</description></item>
    /// </list>
    /// <para>
    /// Version-aware: il layout delle proprietà TCPIP può variare tra Windows 10,
    /// Windows 11 e versioni del provider <c>Microsoft-TCPIP</c>. Il resolver deve
    /// discriminare per ProviderId/EventId/Version/Opcode e implementare caching.
    /// </para>
    /// </remarks>
    public unsafe interface IEtwMetadataResolver
    {
        /// <summary>
        /// Firma del metodo di risoluzione del metadata TDH per un evento ETW specifico.
        /// </summary>
        /// <remarks>
        /// Se il metadata è già presente in cache, restituisce il buffer esistente;
        /// altrimenti:
        /// <list type="bullet">
        ///   <item><description>invoca <c>TdhGetEventInformation</c>;</description></item>
        ///   <item><description>alloca un buffer unmanaged;</description></item>
        ///   <item><description>memorizza il risultato nel pool.</description></item>
        /// </list>
        /// Il buffer restituito è:
        /// <list type="bullet">
        ///   <item><description>read-only;</description></item>
        ///   <item><description>lifetime-managed dal resolver.</description></item>
        /// </list>
        /// Non deve essere liberato manualmente dal chiamante.
        /// <para>
        /// ABI requirement: il parametro <paramref name="replayRecord"/> deve essere
        /// ABI-compatible con <c>EVENT_RECORD</c>, costruito runtime tramite ReplayContext
        /// e contenere puntatori UserData pinned. Layout non compatibili causano
        /// metadata corruption, crash runtime o <c>ERROR_INVALID_PARAMETER (87)</c>.
        /// </para>
        /// </remarks>
        /// <param name="replayRecord">
        /// <c>EVENT_RECORD</c> ricostruito runtime contenente UserData ed ExtendedData pinned.
        /// </param>
        /// <returns>
        /// Handle unmanaged al buffer <c>TRACE_EVENT_INFO</c> utilizzabile nelle fasi
        /// di layout discovery e decoding TDH.
        /// </returns>
        TraceEventInfoHandle Resolve(
            TDH_EVENT_RECORD* replayRecord);
    }
}
