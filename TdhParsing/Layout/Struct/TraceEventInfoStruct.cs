using System.Runtime.InteropServices;
using EtwIpGrabber.EtwStructure.RealTimeConsumer.Native.Structures;

namespace EtwIpGrabber.TdhParsing.Layout.Struct
{
    /// <summary>
    /// Rappresenta il metadata manifest-based associato ad un evento ETW runtime.
    /// <para>
    /// Questa struttura viene restituita dalla chiamata nativa:
    /// <c>TdhGetEventInformation</c>
    /// </para>
    /// <para>
    /// Contiene tutte le informazioni necessarie per interpretare il payload
    /// binario (<c>UserData</c>) di un evento ETW manifest-based.
    /// </para>
    ///
    /// <para>
    /// In particolare include:
    /// </para>
    /// <list type="bullet">
    /// <item>Descrizione delle proprietà runtime</item>
    /// <item>Tipo input/output delle proprietà (InType / OutType)</item>
    /// <item>Offset dei nomi proprietà</item>
    /// <item>Mappe di conversione opzionali</item>
    /// </list>
    ///
    /// <para>
    /// TDH utilizza questo buffer per:
    /// </para>
    /// <list type="bullet">
    /// <item>Effettuare il property walk</item>
    /// <item>Decodificare sequenzialmente il payload evento</item>
    /// <item>Applicare mappe di conversione runtime</item>
    /// </list>
    /// <para>
    /// ⚠️ IMPORTANTE:
    /// </para>
    /// <list type="bullet">
    /// <item>La struttura è seguita in memoria da un array variabile di <see cref="EVENT_PROPERTY_INFO"/></item>
    /// <item>Il layout deve essere interpretato tramite pointer arithmetic</item>
    /// <item>Qualsiasi mismatch ABI causa AccessViolationException</item>
    /// </list>
    ///
    /// <remarks>
    /// TDH Documentation:
    /// <see href="https://learn.microsoft.com/it-it/windows/win32/api/tdh/ns-tdh-trace_event_info"> TRACE_EVENT_INFO</see>
    /// </remarks>
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct TRACE_EVENT_INFO
    {
        public Guid ProviderGuid;
        public Guid EventGuid;
        public EVENT_DESCRIPTOR EventDescriptor;
        public uint DecodingSource;

        public uint ProviderNameOffset;
        public uint LevelNameOffset;
        public uint ChannelNameOffset;
        public uint KeywordsNameOffset;
        public uint TaskNameOffset;
        public uint OpcodeNameOffset;
        public uint EventMessageOffset;
        public uint ProviderMessageOffset;
        public uint BinaryXMLOffset;
        public uint BinaryXMLSize;

        public uint EventNameOffset;   // union
        public uint EventAttributesOffset; // union

        public uint PropertyCount;
        public uint TopLevelPropertyCount;
        public uint Flags;
    }
}
