using EtwIpGrabber.EtwIntegration.RealTimeConsumer.Native.Structures;
using EtwIpGrabber.TdhParsing.Decoder.Abstraction;
using EtwIpGrabber.TdhParsing.Layout;
using EtwIpGrabber.TdhParsing.Layout.Struct;
using EtwIpGrabber.TdhParsing.Metadata.Native;
using EtwIpGrabber.TdhParsing.Normalization;
using System.Runtime.InteropServices;

namespace EtwIpGrabber.TdhParsing.Decoder
{
    /// <summary>
    /// Implementa il decoding sequenziale del payload binario
    /// di un evento TCPIP ETW manifest-based.
    ///
    /// <para>
    /// Rappresenta il terzo step della pipeline TDH:
    /// </para>
    /// <code>
    /// TDH_EVENT_RECORD + TRACE_EVENT_INFO + TcpEventLayout
    ///     ↓
    /// Sequential Payload Walk
    ///     ↓
    /// RawTcpDecodedEvent
    /// </code>
    ///
    /// <para>
    /// Questa classe è responsabile di:
    /// </para>
    /// <list type="bullet">
    /// <item>Interpretare il payload binario ETW (<c>UserData</c>)</item>
    /// <item>Applicare il layout runtime version-aware</item>
    /// <item>Effettuare il property walk sequenziale</item>
    /// <item>Avanzare l'offset tramite <c>UserDataConsumed</c></item>
    /// <item>Filtrare eventi non IPv4</item>
    /// </list>
    ///
    /// Il decoding avviene tramite la chiamata all'api nativa <c>TdhFormatProperty</c>
    /// Che utilizza il metadata TDH per:
    /// <list type="bullet">
    /// <item>Interpretare il payload runtime</item>
    /// <item>Applicare eventuali mappe di conversione</item>
    /// <item>Restituire la dimensione effettiva consumata</item>
    /// </list>
    ///
    /// <para><b> IMPORTANTE: </b></para>
    /// <list type="bullet">
    /// <item>Il payload ETW è binario e non self-describing</item>
    /// <item>L'offset deve essere avanzato correttamente</item>
    /// <item>Se la chiamata a TdhFormatProperty genera ERROR_INVALID_PARAMETER (87) è presente un type mismatch con i parametri passati alla chiamata</item>
    /// </list>
    /// </summary>
    internal unsafe sealed class SequentialTdhDecoder : ITdhDecoder
    {
        /// <summary>
        /// Effettua il decoding sequenziale del payload evento
        /// utilizzando il metadata TDH e il layout runtime.
        ///
        /// <para>
        /// Per ogni proprietà definita nel manifest:
        /// </para>
        /// <list type="number">
        /// <item>Invoca TdhFormatProperty</item>
        /// <item>Ottiene il valore formattato</item>
        /// <item>Recupera i byte consumati</item>
        /// <item>Avanza l'offset payload</item>
        /// </list>
        ///
        /// <para>
        /// Il binding runtime:
        /// </para>
        /// <code>
        /// PropertyIndex → TcpEventLayout → RawTcpDecodedEvent
        /// </code>
        ///
        /// <para>
        /// Permette di:
        /// </para>
        /// <list type="bullet">
        /// <item>Estrarre IPv4</item>
        /// <item>Filtrare AF_INET</item>
        /// <item>Popolare il risultato grezzo</item>
        /// </list>
        /// </summary>
        public bool TryDecode(
            TDH_EVENT_RECORD* record,
            TRACE_EVENT_INFO* info,
            TcpEventLayout layout,
            out RawTcpDecodedEvent decoded)
        {
            decoded = default;

            decoded.ProcessId = record->EventHeader.ProcessId;
            byte* userData =
                (byte*)record->UserData;

            ushort userDataLength =
                record->UserDataLength;

            int propertyCount =
                (int)info->PropertyCount;

            var propArray =
                GetPropertyArray(info);

            int offset = 0;

            //TODO: Verifica se eliminare la seguente istruzione TryParseProcessContext(record, ref decoded)

            for (int i = 0; i < propertyCount; i++)
            {
                var prop = &propArray[i];

                ushort consumed = 0;
                uint bufferSize = 128;

                char* stackBuffer = stackalloc char[128];
                char* buffer = stackBuffer;

                IntPtr heapBuffer = IntPtr.Zero;

                //===========================================================//
                // Tentativo di formattare la proprietà, se il buffer è      //
                // insufficiente, aumenta la dimensione del buffer e riprova //
                //===========================================================//
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

                if (!BindDecodedField(value, layout, i, ref decoded))
                    return false;

                offset += consumed;
            }
            
            if (!decoded.IsIpv4)
                return false;

            return true;
        }
        /// <summary>
        /// Recupera l'array runtime di proprietà <see cref="EVENT_PROPERTY_INFO"/>
        /// associato all'evento.
        ///
        /// <para>
        /// TRACE_EVENT_INFO è seguito in memoria da:
        /// </para>
        /// <code>
        /// EVENT_PROPERTY_INFO[PropertyCount]
        /// </code>
        ///
        /// <para>
        /// L'header deve essere allineato a 8 byte prima di accedere all'array.
        /// </para>
        ///
        /// <para>
        /// Un offset errato comporta:
        /// </para>
        /// <list type="bullet">
        /// <item>ERROR_INVALID_PARAMETER</item>
        /// <item>Decode fallito</item>
        /// </list>
        /// </summary>
        private static EVENT_PROPERTY_INFO* GetPropertyArray(TRACE_EVENT_INFO* info)
        {
            int headerSize = sizeof(TRACE_EVENT_INFO);
            int alignedHeader = (headerSize + 7) & ~7;

            return (EVENT_PROPERTY_INFO*)
                ((byte*)info + alignedHeader);
        }
        /// <summary>
        /// Effettua il binding tra una proprietà runtime e il relativo campo TCP/IP.
        ///
        /// <para>
        /// Utilizza gli indici precedentemente costruiti nel <see cref="TcpEventLayout"/>
        /// per associare:
        /// </para>
        /// <code>
        /// PropertyIndex → Tcp Field
        /// </code>
        ///
        /// <para>
        /// Il campo AddressFamily viene utilizzato
        /// per filtrare eventi IPv6.
        /// </para>
        ///
        /// <para>
        /// AF_INET (2) ⇒ IPv4 valido
        /// </para>
        /// </summary>
        private static bool BindDecodedField(
            string value,
            TcpEventLayout layout,
            int index,
            ref RawTcpDecodedEvent decoded)
        {
            switch (index)
            {
                case var _ when index == layout.AddressFamilyIndex:
                    if (!ushort.TryParse(value, out decoded.AddressFamily))
                        return false;

                    // se diverso da AF_INET (2), ovvero un evento non IPv4, scarta l'evento
                    if (decoded.AddressFamily != 2)
                        return false;
                    break;

                case var _ when index == layout.LocalAddressIndex:
                    decoded.LocalAddress =
                        ConversionUtil.ParseIPv4(value);

                    decoded.IsIpv4 = true;
                    break;

                case var _ when index == layout.RemoteAddressIndex:
                    decoded.RemoteAddress = ConversionUtil.ParseIPv4(value);
                    break;

                case var _ when index == layout.LocalPortIndex:
                    if (!ushort.TryParse(value, out decoded.LocalPort))
                        return false;
                    break;

                case var _ when index == layout.RemotePortIndex:
                    if (!ushort.TryParse(value, out decoded.RemotePort))
                        return false;
                    break;
            }

            return true;
        }
    }
}