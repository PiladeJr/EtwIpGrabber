namespace EtwIpGrabber.TdhParsing.Layout
{
    /// <summary>
    /// Flags TDH associati ad una proprietà evento manifest-based.
    ///
    /// <para>
    /// Questi flag sono presenti nel campo <c>Flags</c>
    /// della struttura <see cref="EVENT_PROPERTY_INFO"/>.
    /// </para>
    ///
    /// <para>
    /// Vengono utilizzati durante la fase di Layout Discovery per:
    /// </para>
    /// <list type="bullet">
    /// <item>Determinare se una proprietà è strutturata</item>
    /// <item>Capire se la lunghezza è dinamica</item>
    /// <item>Stabilire la presenza di mappe di conversione runtime</item>
    /// <item>Identificare proprietà con schema custom</item>
    /// </list>
    ///
    /// <para>
    /// Nel contesto TCPIP ETW, vengono utilizzati principalmente per:
    /// </para>
    /// <list type="bullet">
    /// <item>Identificare campi Direction mappati (enum runtime)</item>
    /// <item>Determinare se è necessario un lookup via EVENT_MAP_INFO</item>
    /// </list>
    ///
    /// <remarks>
    /// TDH Documentation:
    /// <see href="https://learn.microsoft.com/it-it/windows/win32/api/tdh/ne-tdh-property_flags"> PropertyFlags</see>
    /// </remarks>
    /// </summary>
    [Flags]
    public enum PropertyFlags : uint
    {
        PropertyStruct = 0x1,
        PropertyParamLength = 0x2,
        PropertyParamCount = 0x4,
        PropertyWBEMXmlFragment = 0x8,
        PropertyParamFixedLength = 0x10,
        PropertyParamFixedCount = 0x20,
        PropertyHasTags = 0x40,
        PropertyHasCustomSchema = 0x80,
        PropertyParamFixedMap = 0x100
    }
}
