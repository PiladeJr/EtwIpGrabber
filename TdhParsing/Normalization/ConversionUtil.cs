using EtwIpGrabber.EtwStructure.RealTimeConsumer.Native.Structures;
using EtwIpGrabber.TdhParsing.Normalization.Models;
using System.Runtime.CompilerServices;


namespace EtwIpGrabber.TdhParsing.Normalization
{
    /// <summary>
    /// Utility statica utilizzata nella fase di normalizzazione del dato TCP.
    /// 
    /// Converte i valori ottenuti dal decoding TDH (RawTcpDecodedEvent)
    /// in un formato semanticamente corretto e utilizzabile a livello applicativo.
    ///
    /// In particolare:
    /// - Converte valori da network byte order (big-endian) a host order
    /// - Effettua parsing e formattazione IPv4
    /// - Traduce codici raw ETW (Direction, Flags, EventDescriptor)
    ///   in enum di dominio
    ///
    /// Questa classe viene utilizzata esclusivamente da:
    /// <see cref="TcpEventNormalizer"/>
    /// </summary>
    public static class ConversionUtil
    {
        /// <summary>
        /// Converte una porta TCP da network byte order (big-endian)
        /// a host byte order. (little-endian)
        /// </summary>
        /// <remarks>
        /// I campi provenienti dal payload ETW sono serializzati in network order.
        /// Questa funzione applica manualmente lo swap dei byte.
        /// <para><b>ATTENZIONE: </b></para> 
        /// Sfruttando il metodo <c>TdhFormatProperties</c> le porte ottenute sono già in
        /// host byte order, quindi questa conversione non è necessaria e non deve essere
        /// applicata, Il metodo è mantenuto per completezza e per eventuali casi futuri
        /// in cui si dovesse lavorare con dati raw in network byte order.
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ushort Ntohs(ushort value)
        {
            return (ushort)(
                (value >> 8) |
                (value << 8));
        }

        /// <summary>
        /// Converte un indirizzo IPv4 da network byte order
        /// a host byte order.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint Ntohl(uint value)
        {
            return
                (value >> 24) |
                ((value >> 8) & 0x0000FF00) |
                ((value << 8) & 0x00FF0000) |
                (value << 24);
        }

        /// <summary>
        /// Converte il timestamp ETW (FILETIME)
        /// in formato UTC .NET.
        /// </summary>
        public static DateTime ConvertTimestamp(long timestamp)
        {
            return DateTime.FromFileTimeUtc(timestamp);
        }

        /// <summary>
        /// Traduce il valore raw della proprietà Direction
        /// nell'enum di dominio <see cref="TcpDirection"/>.
        /// </summary>
        public static TcpDirection DecodeDirection(byte raw)
        {
            return raw switch
            {
                1 => TcpDirection.Outbound,
                2 => TcpDirection.Inbound,
                _ => TcpDirection.Unknown
            };
        }

        /// <summary>
        /// Converte il byte raw dei flag TCP
        /// nell'enum <see cref="TcpFlags"/>.
        /// </summary>
        public static TcpFlags DecodeFlags(byte raw)
        {
            return (TcpFlags)raw;
        }

        /// <summary>
        /// Mappa l'EventDescriptor ETW
        /// in un tipo evento TCP semanticamente significativo.
        /// </summary>
        public static TcpEventType MapEventType(in EVENT_DESCRIPTOR desc)
        {
            return desc.Id switch
            {
                10 => TcpEventType.Send,
                11 => TcpEventType.Receive,
                12 => TcpEventType.Connect,
                13 => TcpEventType.Disconnect,
                14 => TcpEventType.Retransmit,
                15 => TcpEventType.Accept,
                16 => TcpEventType.Reconnect,
                17 => TcpEventType.Fail,
                _ => TcpEventType.Unknown
            };
        }

        /// <summary>
        /// Effettua il parsing di una stringa IPv4
        /// nel corrispondente valore UInt32.
        /// </summary>
        /// <remarks>
        /// Utilizzato durante il decoding TDH per convertire
        /// le proprietà string-based in formato numerico.
        /// </remarks>
        public static uint ParseIPv4(string value)
        {
            if (!System.Net.IPAddress.TryParse(value, out var ip))
                return 0;

            var addr = ip.GetAddressBytes();

            if (addr.Length != 4)
                return 0;

            return
                ((uint)addr[0] << 24) |
                ((uint)addr[1] << 16) |
                ((uint)addr[2] << 8) |
                addr[3];
        }

        /// <summary>
        /// Converte un indirizzo IPv4 UInt32
        /// in rappresentazione dotted-decimal.
        /// </summary>
        public static string FormatIPv4(uint ip)
        {
            return $"{ip & 0xFF}." +
                   $"{(ip >> 8) & 0xFF}." +
                   $"{(ip >> 16) & 0xFF}." +
                   $"{(ip >> 24) & 0xFF}";
        }
    }
}
