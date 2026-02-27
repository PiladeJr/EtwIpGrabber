using EtwIpGrabber.EtwStructure.RealTimeConsumer;
using System.Collections.Concurrent;

namespace EtwIpGrabber.EtwStructure.EventDispatcher
{
    /// <summary>
    /// Implementazione base del dispatcher utilizzando <see cref="ConcurrentQueue{T}"/>
    /// come staging buffer per gli eventi ETW.
    /// </summary>
    /// <remarks>
    /// Questa classe riceve snapshot copiati degli EVENT_RECORD
    /// prodotti dalla callback ETW.
    /// 
    /// Il contenuto di EVENT_RECORD (UserData, ExtendedData)
    /// è valido esclusivamente per la durata della callback
    /// e deve essere copiato prima del return.
    /// 
    /// L'uso di uno snapshot garantisce che:
    /// <list type="bullet">
    ///   <item><description>la memoria ETW non venga riutilizzata dal kernel;</description></item>
    ///   <item><description>non si verifichino use-after-return;</description></item>
    ///   <item><description>la pipeline possa elaborare eventi asincroni in sicurezza.</description></item>
    /// </list>
    /// </remarks>
    public sealed class LockFreeEventDispatcher : IEventDispatcher
    {
        /// <summary>
        /// Coda concorrente utilizzata come staging buffer per i puntatori agli eventi.
        /// </summary>
        private readonly ConcurrentQueue<EventRecordSnapshot> _queue;

        /// <summary>
        /// Inizializza una nuova istanza di <see cref="LockFreeEventDispatcher"/>.
        /// </summary>
        public LockFreeEventDispatcher()
        {
            _queue = new ConcurrentQueue<EventRecordSnapshot>();
        }

        /// <summary>
        /// Tenta di accodare un evento ETW per l'elaborazione asincrona.
        /// </summary>
        /// <param name="snapshot">Snapshot copia del puntatore alla struttura nativa <c>EVENT_RECORD</c></param>
        /// <returns>
        /// Sempre <c>true</c> (la coda è unbounded e l'accodamento non può fallire).
        /// </returns>
        public unsafe bool TryEnqueue(EventRecordSnapshot snapshot)
        {
            _queue.Enqueue(snapshot);
            return true;
        }
    }
}
