namespace EtwIpGrabber.EtwIntegration.ProviderConfiguration
{
    /// <summary>
    /// Descrive il provider manifest-based <c>Microsoft-Windows-TCPIP</c>.
    /// </summary>
    /// <remarks>
    /// Questo provider espone eventi semantici relativi al lifecycle TCP:
    /// <list type="bullet">
    ///   <item><description>TcpConnect;</description></item>
    ///   <item><description>TcpAccept;</description></item>
    ///   <item><description>TcpDisconnect;</description></item>
    ///   <item><description>TcpTimeout;</description></item>
    ///   <item><description>TcpConnectionRundown.</description></item>
    /// </list>
    /// Gli eventi rappresentano transizioni della TCP state machine
    /// all'interno di <c>tcpip.sys</c> e non singoli segmenti di rete.
    ///
    /// ProviderGuid:
    /// <list type="bullet">
    ///   <item><description>GUID del provider <c>Microsoft-Windows-TCPIP</c>.</description></item>
    /// </list>
    /// TRACE_LEVEL_INFORMATION:
    /// <list type="bullet">
    ///   <item><description>livello minimo necessario per ricevere eventi di lifecycle.</description></item>
    /// </list>
    /// TCPIP_KEYWORD:
    /// <list type="bullet">
    ///   <item><description>keyword che abilita il tracing TCP.</description></item>
    /// </list>
    /// </remarks>
    public sealed class TcpIpProviderDescriptor
    {
        public static readonly Guid ProviderGuid =
            new("7DD42A49-5329-4832-8DFD-43D979153A88");

        public const byte TRACE_LEVEL_INFORMATION = 4;

        // Keyword per eventi TCPIP
        public const ulong TCPIP_KEYWORD =
            0x10 |   // Connect
            0x20 |   // Disconnect
            0x40 |   // Retransmit
            0x80;    // Accept

        public Guid Guid => ProviderGuid;
        public byte Level => TRACE_LEVEL_INFORMATION;
        public ulong Keywords => TCPIP_KEYWORD;
    }
}
