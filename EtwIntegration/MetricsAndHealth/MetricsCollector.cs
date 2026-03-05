namespace EtwIpGrabber.EtwIntegration.MetricsAndHealth
{
    /// <summary>
    /// Implementazione thread-safe del collector di metriche
    /// per la pipeline ETW realtime.
    /// </summary>
    /// <remarks>
    /// Questo componente raccoglie indicatori di salute
    /// relativi alla capacità della pipeline di ingestione
    /// di gestire il flusso di eventi proveniente da ETW.
    /// 
    /// Il contatore <see cref="Dropped"/> rappresenta il numero
    /// di eventi scartati dal dispatcher (es. ring buffer pieno),
    /// ed è un indicatore diretto di:
    /// <list type="bullet">
    ///   <item><description>saturazione della pipeline;</description></item>
    ///   <item><description>latenza eccessiva del consumer;</description></item>
    ///   <item><description>dimensionamento insufficiente del buffer;</description></item>
    ///   <item><description>necessità di backpressure o batching.</description></item>
    /// </list>
    /// 
    /// Nota:
    /// Un incremento di questo contatore NON implica perdita
    /// di eventi lato ETW kernel (EventsLost), ma esclusivamente
    /// scarto intenzionale lato user-mode per preservare
    /// la stabilità del servizio.
    /// 
    /// L'uso di <see cref="Interlocked"/> garantisce
    /// incrementi atomici senza lock nel contesto della
    /// callback ETW.
    /// </remarks>
    public sealed class EtwMetricsCollector : IMetricsCollector
    {
        private long _dropped;

        /// <summary>
        /// Incrementa in modo atomico il numero di eventi
        /// scartati dalla pipeline.
        /// </summary>
        public void IncrementDroppedEvents()
        {
            Interlocked.Increment(ref _dropped);
        }

        /// <summary>
        /// Numero totale di eventi scartati dal dispatcher
        /// a causa di overflow o backpressure.
        /// </summary>
        public long Dropped => _dropped;
    }
}
