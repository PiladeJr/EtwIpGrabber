using System.Runtime.InteropServices;

namespace EtwIpGrabber.TdhParsing.Layout.Struct
{
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

    [StructLayout(LayoutKind.Sequential)]
    public struct PROPERTY_NON_STRUCT_TYPE
    {
        public ushort InType;
        public ushort OutType;
        public uint MapNameOffset;
    }

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
