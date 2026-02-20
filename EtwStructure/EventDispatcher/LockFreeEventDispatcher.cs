using EtwIpGrabber.EtwStructure.RealTimeConsumer.Native.Structures;
using System.Collections.Concurrent;

namespace EtwIpGrabber.EtwStructure.EventDispatcher
{
    /// <summary>
    /// Implementazione base del dispatcher utilizzando <see cref="ConcurrentQueue{T}"/>
    /// come staging buffer per gli eventi ETW.
    /// </summary>
    /// <remarks>
    /// Questa classe riceve i puntatori agli <c>EVENT_RECORD</c> dalla callback ETW
    /// e li inoltra alla pipeline interna. In questa fase
    /// il puntatore viene semplicemente convertito in <c>IntPtr</c> e accodato.
    /// <para>
    /// Nota: questa implementazione è provvisoria e verrà sostituita con una coda
    /// lock-free a dimensione fissa per gestire backpressure e prevenire crescita
    /// incontrollata della memoria sotto carico elevato.
    /// </para>
    /// </remarks>
    public sealed class LockFreeEventDispatcher : IEventDispatcher
    {
        /// <summary>
        /// Coda concorrente utilizzata come staging buffer per i puntatori agli eventi.
        /// </summary>
        private readonly ConcurrentQueue<IntPtr> _queue;

        /// <summary>
        /// Inizializza una nuova istanza di <see cref="LockFreeEventDispatcher"/>.
        /// </summary>
        public LockFreeEventDispatcher()
        {
            _queue = new ConcurrentQueue<IntPtr>();
        }

        /// <summary>
        /// Tenta di accodare un evento ETW per l'elaborazione asincrona.
        /// </summary>
        /// <param name="record">Puntatore alla struttura nativa <c>EVENT_RECORD</c>.</param>
        /// <returns>
        /// Sempre <c>true</c> (la coda è unbounded e l'accodamento non può fallire).
        /// </returns>
        public unsafe bool TryEnqueue(EVENT_RECORD* record)
        {
            _queue.Enqueue((IntPtr)record);
            return true;
        }
    }
}
