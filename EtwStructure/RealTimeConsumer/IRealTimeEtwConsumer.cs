namespace EtwIpGrabber.EtwStructure.RealTimeConsumer
{
    /// <summary>
    /// Contratto per il consumo in realtime degli eventi ETW provenienti da
    /// una sessione attiva.
    /// </summary>
    /// <remarks>
    /// L'implementazione è responsabile di:
    /// <list type="bullet">
    ///   <item><description>aprire la sessione tramite <c>OpenTrace</c>;</description></item>
    ///   <item><description>avviare il loop di consumo tramite <c>ProcessTrace</c>;</description></item>
    ///   <item><description>ricevere eventi tramite la callback <c>EVENT_RECORD_CALLBACK</c>;</description></item>
    ///   <item><description>inoltrare gli eventi alla pipeline interna (dispatcher).</description></item>
    /// </list>
    /// <para>
    /// La callback ETW deve essere non-bloccante per evitare perdita di eventi
    /// (EventsLost). L'implementazione deve gestire la sincronizzazione e
    /// l'eventuale buffering esterno per mantenere throughput e stabilità.
    /// </para>
    /// </remarks>
    public interface IRealtimeEtwConsumer : IDisposable
    {
        /// <summary>
        /// Avvia il consumo realtime degli eventi ETW per la sessione specificata.
        /// </summary>
        /// <param name="sessionName">
        /// Nome della sessione ETW a cui collegarsi.
        /// </param>
        void Start(string sessionName);
    }


}
