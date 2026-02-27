using EtwIpGrabber.EtwStructure.RealTimeConsumer.Native.Structures;

namespace EtwIpGrabber.TdhParsing.Metadata
{
    /// <summary>
    /// Chiave univoca per il caching delle informazioni di metadata TDH degli eventi ETW.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Identifica il layout manifest-based di un evento TCPIP ETW mediante i campi:
    /// </para>
    /// <list type="bullet">
    ///   <item><description><see cref="ProviderId"/></description></item>
    ///   <item><description><see cref="Id"/> (da <c>EventDescriptor.Id</c>)</description></item>
    ///   <item><description><see cref="Version"/> (da <c>EventDescriptor.Version</c>)</description></item>
    ///   <item><description><see cref="Opcode"/> (da <c>EventDescriptor.Opcode</c>)</description></item>
    /// </list>
    /// <para>
    /// La chiave è necessaria in quanto <c>TRACE_EVENT_INFO</c> è:
    /// </para>
    /// <list type="bullet">
    ///   <item><description>runtime-specific</description></item>
    ///   <item><description>version-aware</description></item>
    ///   <item><description>opcode-dependent</description></item>
    /// </list>
    /// <para>
    /// Eventi con stesso <c>EventId</c> ma <c>Version</c> differente possono esporre 
    /// layout di proprietà diversi su Windows 10/11.
    /// </para>
    /// <para>
    /// Il caching per chiave evita:
    /// </para>
    /// <list type="bullet">
    ///   <item><description>chiamate ripetute a <c>TdhGetEventInformation</c> (costose)</description></item>
    ///   <item><description>parsing manifest runtime per-event</description></item>
    /// </list>
    /// <para>
    /// migliorando drasticamente il throughput sotto carico.
    /// </para>
    /// </remarks>
    internal readonly struct TdhEventKey(in EVENT_HEADER header)
    {
        /// <summary>
        /// GUID del provider ETW.
        /// </summary>
        public readonly Guid ProviderId = header.ProviderId;

        /// <summary>
        /// Identificatore dell'evento dal descriptor.
        /// </summary>
        public readonly ushort Id = header.EventDescriptor.Id;

        /// <summary>
        /// Versione dell'evento dal descriptor.
        /// </summary>
        public readonly byte Version = header.EventDescriptor.Version;

        /// <summary>
        /// Opcode dell'evento ottenuto dal descriptor. 
        /// (Il tracciamento di questo valore è necessario per il fatto che può variare tra versioni di windows differente: WIN 10/11)
        /// </summary>
        public readonly byte Opcode = header.EventDescriptor.Opcode;

        /// <summary>
        /// Confronta questa chiave con un'altra per uguaglianza strutturale.
        /// </summary>
        /// <param name="other">Chiave da confrontare.</param>
        /// <returns><c>true</c> se tutte le proprietà sono uguali; altrimenti, <c>false</c>.</returns>
        public bool Equals(TdhEventKey other)
            => ProviderId == other.ProviderId
            && Id == other.Id
            && Version == other.Version
            && Opcode == other.Opcode;

        /// <summary>
        /// Confronta questa chiave con un oggetto per uguaglianza.
        /// </summary>
        /// <param name="obj">Oggetto da confrontare.</param>
        /// <returns><c>true</c> se <paramref name="obj"/> è una <see cref="TdhEventKey"/> uguale; altrimenti, <c>false</c>.</returns>
        public override bool Equals(object? obj)
            => obj is TdhEventKey other && Equals(other);

        /// <summary>
        /// Calcola l'hash code per questa chiave combinando tutti i campi.
        /// </summary>
        /// <returns>Hash code calcolato.</returns>
        public override int GetHashCode()
            => HashCode.Combine(
                ProviderId,
                Id,
                Version,
                Opcode);
    }
}
