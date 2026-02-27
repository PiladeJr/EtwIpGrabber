using EtwIpGrabber.EtwStructure.RealTimeConsumer.Native.Structures;
using EtwIpGrabber.TdhParsing.Layout;
using EtwIpGrabber.TdhParsing.Layout.Struct;

namespace EtwIpGrabber.TdhParsing.Decoder.Abstraction
{
    /// <summary>
    /// Definisce il contratto per il decoding sequenziale
    /// del payload binario di un evento ETW TCPIP manifest-based.
    ///
    /// <para>
    /// L'implementazione concreta è responsabile di:
    /// </para>
    /// <list type="bullet">
    /// <item>Interpretare il payload binario (<c>UserData</c>)</item>
    /// <item>Applicare il layout runtime version-aware</item>
    /// <item>Effettuare il property walk sequenziale</item>
    /// <item>Avanzare l'offset tramite <c>UserDataConsumed</c></item>
    /// <item>Filtrare eventi non IPv4</item>
    /// </list>
    ///
    /// <para>
    /// Il decoding deve essere:
    /// </para>
    /// <list type="bullet">
    /// <item>Manifest-aware</item>
    /// <item>Version-aware (Windows 10 / 11)</item>
    /// <item>ABI-safe</item>
    /// </list>
    /// </summary>
    internal unsafe interface ITdhDecoder
    {
        /// <summary>
        /// Effettua il decoding sequenziale del payload evento.
        /// </summary>
        /// <param name="record">
        /// Evento ETW runtime ABI-compatible.
        /// </param>
        /// <param name="info">
        /// Metadata manifest-based dell'evento.
        /// </param>
        /// <param name="layout">
        /// Decode plan runtime costruito dal LayoutBuilder.
        /// </param>
        /// <param name="decoded">
        /// Output contenente i campi TCP/IP estratti.
        /// </param>
        /// <returns>
        /// True se il decoding è riuscito;
        /// False se l'evento non è IPv4 o non supportato.
        /// </returns>
        bool TryDecode(
            TDH_EVENT_RECORD* record,
            TRACE_EVENT_INFO* info,
            TcpEventLayout layout,
            out RawTcpDecodedEvent decoded);
    }
}
