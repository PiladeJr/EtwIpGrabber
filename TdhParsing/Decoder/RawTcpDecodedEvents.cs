namespace EtwIpGrabber.TdhParsing.Decoder
{
    /// <summary>
    /// Rappresenta il risultato grezzo del decoding
    /// del payload TCPIP ETW.
    ///
    /// <para>
    /// Questa struttura costituisce l'output della fase:
    /// </para>
    /// <code>
    /// SequentialTdhDecoder
    /// </code>
    ///
    /// <para>
    /// Contiene i valori estratti direttamente dal payload
    /// binario evento, senza normalizzazione:
    /// </para>
    /// <list type="bullet">
    /// <item>Indirizzi IPv4 (network byte order)</item>
    /// <item>Porte TCP (network byte order)</item>
    /// <item>AddressFamily</item>
    /// <item>ProcessId</item>
    /// <item>Direction</item>
    /// <item>Flags TCP</item>
    /// </list>
    ///
    /// <para><b>IMPORTANTE:</b></para>
    /// <list type="bullet">
    /// <item>I valori sono ancora in formato runtime</item>
    /// <item>Non pronti per logging o persistenza</item>
    /// <item>IPv4 valido ⇔ IsIpv4 = true</item>
    /// </list>
    /// </summary>
    internal struct RawTcpDecodedEvent
    {
        public uint LocalAddress;
        public uint RemoteAddress;
        public ushort LocalPort;
        public ushort RemotePort;
        public ushort AddressFamily;
        public uint ProcessId;
        public byte Direction;
        public byte TcpFlags;

        public bool IsIpv4;
    }
}
