using EtwIpGrabber.EtwStructure.RealTimeConsumer;
using EtwIpGrabber.TdhParsing.Normalization.Models;

namespace EtwIpGrabber.TdhParsing
{
    internal unsafe interface ITcpEtwParser
    {
        bool TryParse(
            in EventRecordSnapshot snapshot,
            out TcpEvent tcpEvent);
    }
}
