using System.Runtime.InteropServices;

namespace EtwIpGrabber.TdhParsing.Layout.Struct
{
    /// <summary>
    /// Descrive una singola proprietà appartenente al payload runtime
    /// di un evento ETW manifest-based.
    ///
    /// <para>
    /// Questa struttura non viene mai istanziata direttamente,
    /// ma è parte di un array variabile immediatamente successivo
    /// al buffer <see cref="TRACE_EVENT_INFO"/>.
    /// </para>
    ///
    /// <para>
    /// Ogni elemento rappresenta:
    /// </para>
    /// <list type="bullet">
    /// <item>Il tipo dati runtime (InType)</item>
    /// <item>Il formato di output desiderato (OutType)</item>
    /// <item>La presenza di mappe di conversione</item>
    /// <item>Informazioni strutturali (array / nested struct)</item>
    /// </list>
    ///
    /// <para>
    /// Viene utilizzata durante il:
    /// </para>
    /// <list type="bullet">
    /// <item>Layout Discovery</item>
    /// <item>Sequential Property Decoding</item>
    /// </list>
    /// ⚠️ IMPORTANTE:
    /// <list type="bullet">
    /// <item>Contiene union overlay</item>
    /// <item>Il significato dei campi dipende dai Flags</item>
    /// <item>Interpretazione errata ⇒ ERROR_INVALID_PARAMETER (87)</item>
    /// </list>
    ///
    /// <remarks>
    /// TDH Documentation:
    /// <see href="https://learn.microsoft.com/it-it/windows/win32/api/tdh/ns-tdh-event_property_info"> EVENT_PROPERTY_INFO </see>
    /// </remarks>
    /// </summary>
    [StructLayout(LayoutKind.Explicit)]
    public struct EVENT_PROPERTY_INFO
    {
        [FieldOffset(0)]
        public uint Flags;

        [FieldOffset(4)]
        public uint NameOffset;

        // UNION #1
        [FieldOffset(8)]
        public PROPERTY_NON_STRUCT_TYPE NonStructType;

        [FieldOffset(8)]
        public PROPERTY_STRUCT_TYPE StructType;

        [FieldOffset(8)]
        public PROPERTY_CUSTOM_SCHEMA_TYPE CustomSchemaType;

        // UNION #2
        [FieldOffset(16)]
        public ushort Count;

        [FieldOffset(16)]
        public ushort CountPropertyIndex;

        // UNION #3
        [FieldOffset(18)]
        public ushort Length;

        [FieldOffset(18)]
        public ushort LengthPropertyIndex;

        // UNION #4 (Tags / Reserved)
        [FieldOffset(20)]
        public uint Reserved;
    }
    /// <summary>
    /// Descrive una proprietà non strutturata (primitive type)
    /// presente nel payload ETW runtime.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct PROPERTY_NON_STRUCT_TYPE
    {
        public ushort InType;
        public ushort OutType;
        public uint MapNameOffset;
    }
    /// <summary>
    /// Descrive una proprietà composta (nested struct)
    /// all'interno del payload ETW runtime.
    /// </summary>
    /// <remarks>
    /// Permette a TDH di interpretare eventi contenenti
    /// proprietà strutturate.
    /// 
    /// </remarks>>
    [StructLayout(LayoutKind.Sequential)]
    public struct PROPERTY_STRUCT_TYPE
    {
        public ushort StructStartIndex;
        public ushort NumOfStructMembers;
        public uint Padding;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct PROPERTY_CUSTOM_SCHEMA_TYPE
    {
        public ushort InType;
        public ushort OutType;
        public uint CustomSchemaOffset;
    }
}
