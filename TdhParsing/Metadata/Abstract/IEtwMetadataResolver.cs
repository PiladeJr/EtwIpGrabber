using EtwIpGrabber.EtwStructure.RealTimeConsumer.Native.Structures;

namespace EtwIpGrabber.TdhParsing.Metadata.Abstract
{
    /// <summary>
    /// 
    /// </summary>
    public unsafe interface IEtwMetadataResolver
    {
        TraceEventInfoHandle Resolve(
            TDH_EVENT_RECORD* replayRecord);
    }
}
