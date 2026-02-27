namespace EtwIpGrabber.TdhParsing.Layout
{
    /// <summary>
    /// Rappresenta il layout runtime di decoding
    /// di un evento TCPIP ETW manifest-based.
    ///
    /// <para>
    /// Questa struttura NON rappresenta il metadata,
    /// ma una sua proiezione runtime ottimizzata
    /// per il decoding sequenziale del payload evento.
    /// </para>
    ///
    /// <para>
    /// Viene costruita durante la fase di:
    /// </para>
    /// <code>
    /// TRACE_EVENT_INFO
    ///     ↓
    /// TcpEventLayoutBuilder
    ///     ↓
    /// TcpEventLayout
    /// </code>
    ///
    /// <para>
    /// Gli indici vengono successivamente utilizzati dal SequentialTdhDecoder
    /// Per effettuare Property Walk + Offset Tracking
    /// </para>
    /// <para>
    /// <b>IMPORTANTE:</b>
    /// </para>
    /// <list type="bullet">
    /// <item>Il layout è manifest-version-aware</item>
    /// <item>Deve essere costruito una sola volta per EventId/Version</item>
    /// <item>È safe da cache-are tramite TdhEventKey</item>
    /// </list>
    /// </summary>
    internal sealed class TcpEventLayout
    {
        public int AddressFamilyIndex = -1;
        /// <summary>
        /// l'indice della proprietà LocalAddress, indica l'indirizzo di origine di una connessione
        /// </summary>
        public int LocalAddressIndex = -1;
        /// <summary>
        /// l'indice della proprietà RemoteAddress, indica l'indirizzo di destinazione di una connessione
        /// </summary>
        public int RemoteAddressIndex = -1;
        /// <summary>
        /// l'indice della proprietà LocalPort, indica la porta di origine di una connessione
        /// </summary>
        public int LocalPortIndex = -1;
        /// <summary>
        /// l'indice della proprietà RemotePort, indica la porta di destinazione di una connessione
        /// </summary>
        public int RemotePortIndex = -1;
        /// <summary>
        /// l'indice della proprietà ProcessId, indica il processo associato all'evento TCPIP (es. processo che ha aperto la connessione)
        /// </summary>
        public int ProcessIdIndex = -1;

        public int DirectionIndex = -1;
        /// <summary>
        /// l'indice della proprietà TcpFlags, indica i flag TCP associati a un evento (es. SYN, ACK, FIN)
        /// </summary>
        public int TcpFlagsIndex = -1;

        public bool DirectionHasMap;
        /// <summary>
        /// flag inserito per indicare un evento TCPIP manifest-based supportato. Se il campo, durante la fase di binding, viene impostato a false, l'evento viene scartato
        /// </summary>
        public bool Supported;
    }
}
