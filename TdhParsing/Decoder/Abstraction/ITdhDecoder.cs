using EtwIpGrabber.EtwStructure.RealTimeConsumer.Native.Structures;
using EtwIpGrabber.TdhParsing.Layout;
using EtwIpGrabber.TdhParsing.Layout.Struct;

namespace EtwIpGrabber.TdhParsing.Decoder.Abstraction
{
    internal unsafe interface ITdhDecoder
    {
        bool TryDecode(
            TDH_EVENT_RECORD* record,
            TRACE_EVENT_INFO* info,
            TcpEventLayout layout,
            out RawTcpDecodedEvent decoded);
    }
}
