using EtwIpGrabber.EtwStructure.RealTimeConsumer.Native.Structures;
using EtwIpGrabber.TdhParsing.Decoder.Abstraction;
using EtwIpGrabber.TdhParsing.Layout;
using EtwIpGrabber.TdhParsing.Layout.Struct;
using EtwIpGrabber.TdhParsing.Metadata.Native;
using EtwIpGrabber.TdhParsing.Normalization;
using System.Runtime.InteropServices;

namespace EtwIpGrabber.TdhParsing.Decoder
{
    internal unsafe sealed class SequentialTdhDecoder : ITdhDecoder
    {
        public bool TryDecode(
            TDH_EVENT_RECORD* record,
            TRACE_EVENT_INFO* info,
            TcpEventLayout layout,
            out RawTcpDecodedEvent decoded)
        {
            decoded = default;

            byte* userData =
                (byte*)record->UserData;

            ushort userDataLength =
                record->UserDataLength;

            int propertyCount =
                (int)info->PropertyCount;

            int headerSize =
                sizeof(TRACE_EVENT_INFO);

            int alignedHeader =
                (headerSize + 7) & ~7;

            var propArray =
                (EVENT_PROPERTY_INFO*)
                ((byte*)info +
                 alignedHeader);
            var pointerSize = CalculatePointerSize(record);

            int offset = 0;

            for (int i = 0; i < propertyCount; i++)
            {
                var prop = &propArray[i];

                ushort propLength =
                    (prop->Flags & (uint)PropertyFlags.PropertyParamFixedLength) != 0
                        ? prop->Length
                        : (ushort)0;

                ushort consumed = 0;
                uint bufferSize = 128;
                bool useMap = true;

                char* stackBuffer = stackalloc char[128];
                char* buffer = stackBuffer;

                IntPtr heapBuffer = IntPtr.Zero;

                while (true)
                {
                    var status = TdhNativeMethods.TdhFormatProperty(
                            info,
                            null,
                            (uint)IntPtr.Size,
                            prop->NonStructType.InType,
                            prop->NonStructType.OutType,
                            prop->Length,
                            userDataLength,
                            userData + offset,
                            &bufferSize,
                            buffer,
                            &consumed);

                    if (status == 0) // ERROR_SUCCESS
                        break;

                    if (status == 122) // ERROR_INSUFFICIENT_BUFFER
                    {
                        if (heapBuffer != IntPtr.Zero)
                            Marshal.FreeHGlobal(heapBuffer);

                        heapBuffer =
                            Marshal.AllocHGlobal((int)(bufferSize * 2));

                        buffer = (char*)heapBuffer;
                        bufferSize *= 2;
                        continue;
                    }

                    return false;
                }

                string value = new(buffer);
                if (heapBuffer != IntPtr.Zero)
                    Marshal.FreeHGlobal(heapBuffer);

                switch (i)
                {
                    case var _ when i == layout.AddressFamilyIndex:
                        if (!ushort.TryParse(value, out decoded.AddressFamily))
                            return false;

                        if (decoded.AddressFamily != 2)
                            return false;
                        break;

                    case var _ when i == layout.LocalAddressIndex:
                        decoded.LocalAddress =
                            ConversionUtil.ParseIPv4(value);
                        decoded.IsIpv4 = true;
                        break;

                    case var _ when i == layout.RemoteAddressIndex:
                        decoded.RemoteAddress =
                            ConversionUtil.ParseIPv4(value);
                        break;

                    case var _ when i == layout.LocalPortIndex:
                        if (!ushort.TryParse(value, out decoded.LocalPort))
                            return false;
                        break;

                    case var _ when i == layout.RemotePortIndex:
                        if (!ushort.TryParse(value, out decoded.RemotePort))
                            return false;
                        break;
                }

                offset += consumed;
            }
            
            if (!decoded.IsIpv4)
                return false;

            return true;
        }

        private static uint CalculatePointerSize(TDH_EVENT_RECORD* record)
        {
             uint pointerSize =
                (record->EventHeader.Flags & 0x20) != 0
                    ? 4u
                    : 8u;
            return pointerSize;
        }
    }
}