using EtwIpGrabber.EtwStructure.RealTimeConsumer.Native.Structures;
using EtwIpGrabber.TdhParsing.Decoder;
using EtwIpGrabber.TdhParsing.Normalization.Models;

namespace EtwIpGrabber.TdhParsing.Normalization
{
    internal class TcpEventNormalizer
    {
        public TcpEventNormalizer(){}
        public TcpEvent Normalize(
            in RawTcpDecodedEvent raw,
            in EVENT_HEADER header)
        {
            return new TcpEvent
            {
                TimestampUtc = ConversionUtil.ConvertTimestamp(header.TimeStamp),

                ProcessId =raw.ProcessId,

                LocalIP = ConversionUtil.Ntohl(raw.LocalAddress),

                RemoteIP = ConversionUtil.Ntohl(raw.RemoteAddress),

                LocalPort = ConversionUtil.Ntohs(raw.LocalPort),

                RemotePort = ConversionUtil.Ntohs(raw.RemotePort),

                Direction = ConversionUtil.DecodeDirection(raw.Direction),

                Flags = ConversionUtil.DecodeFlags(raw.TcpFlags),

                EventType = ConversionUtil.MapEventType(header.EventDescriptor)
            };
        }
    }
}
