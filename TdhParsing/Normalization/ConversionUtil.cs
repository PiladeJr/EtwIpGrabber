using EtwIpGrabber.EtwStructure.RealTimeConsumer.Native.Structures;
using EtwIpGrabber.TdhParsing.Normalization.Models;
using System.Runtime.CompilerServices;


namespace EtwIpGrabber.TdhParsing.Normalization
{
    public static class ConversionUtil
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ushort Ntohs(ushort value)
        {
            return (ushort)(
                (value >> 8) |
                (value << 8));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint Ntohl(uint value)
        {
            return
                (value >> 24) |
                ((value >> 8) & 0x0000FF00) |
                ((value << 8) & 0x00FF0000) |
                (value << 24);
        }
        public static DateTime ConvertTimestamp(long timestamp)
        {
            return DateTime.FromFileTimeUtc(timestamp);
        }
        public static TcpDirection DecodeDirection(byte raw)
        {
            return raw switch
            {
                1 => TcpDirection.Outbound,
                2 => TcpDirection.Inbound,
                _ => TcpDirection.Unknown
            };
        }
        public static TcpFlags DecodeFlags(byte raw)
        {
            return (TcpFlags)raw;
        }

        public static TcpEventType MapEventType(in EVENT_DESCRIPTOR desc)
        {
            return desc.Id switch
            {
                10 => TcpEventType.Connect,
                11 => TcpEventType.Accept,
                12 => TcpEventType.Disconnect,
                14 => TcpEventType.Retransmit,
                15 => TcpEventType.Close,
                _ => TcpEventType.Unknown
            };
        }

        public static uint ParseIPv4(string value)
        {
            Span<byte> bytes = stackalloc byte[4];

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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string FormatIPv4(uint ip)
        {
            return $"{ip & 0xFF}." +
                   $"{(ip >> 8) & 0xFF}." +
                   $"{(ip >> 16) & 0xFF}." +
                   $"{(ip >> 24) & 0xFF}";
        }
    }
}
