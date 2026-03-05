using EtwIpGrabber.EtwIntegration.RealTimeConsumer;

namespace EtwIpGrabber.EtwIntegration.EventDispatcher
{
    /// <summary>
    /// Contratto per l'inoltro degli eventi ETW dalla callback realtime
    /// alla pipeline di elaborazione.
    /// </summary>
    /// <remarks>
    /// Il dispatcher funge da staging buffer tra:
    /// <list type="bullet">
    ///   <item><description>il thread ETW (producer);</description></item>
    ///   <item><description>il motore di ricostruzione TCP (consumer).</description></item>
    /// </list>
    /// L'implementazione deve essere:
    /// <list type="bullet">
    ///   <item><description>thread-safe;</description></item>
    ///   <item><description>non bloccante;</description></item>
    ///   <item><description>lock-free (idealmente).</description></item>
    /// </list>
    /// Il metodo <c>TryEnqueue</c> deve essere eseguito in tempo costante
    /// per evitare backpressure sulla pipeline ETW.
    /// </remarks>
    public interface IEventDispatcher
    {
        /// <summary>
        /// Tenta di accodare un evento ETW per l'elaborazione asincrona.
        /// </summary>
        /// <param name="record">Puntatore alla struttura nativa <c>EVENT_RECORD</c>.</param>
        /// <returns>
        /// <c>true</c> se l'evento è stato accodato con successo;
        /// <c>false</c> se la coda è piena o l'accodamento non è riuscito.
        /// </returns>
        unsafe bool TryEnqueue(EventRecordSnapshot snapshot);
    }
}
