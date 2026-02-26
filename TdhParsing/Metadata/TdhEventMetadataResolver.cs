using EtwIpGrabber.EtwStructure.RealTimeConsumer.Native.Structures;
using EtwIpGrabber.TdhParsing.Metadata.Abstract;
using EtwIpGrabber.TdhParsing.Metadata.Native;
using System.Runtime.InteropServices;

namespace EtwIpGrabber.TdhParsing.Metadata
{
    internal unsafe sealed class TdhEventMetadataResolver(TraceEventInfoBufferPool pool) : IEtwMetadataResolver
    {
        private readonly TraceEventInfoBufferPool _pool = pool;

        public TraceEventInfoHandle Resolve(
            TDH_EVENT_RECORD* replayRecord)
        {
            var key = new TdhEventKey(
                replayRecord->EventHeader);

            return _pool.GetOrAdd(
                key,
                () => BuildTraceEventInfo(
                    replayRecord));
        }

        private static TraceEventInfoHandle BuildTraceEventInfo(
            TDH_EVENT_RECORD* record)
        {
            const uint ERROR_SUCCESS = 0;
            const uint ERROR_INSUFFICIENT_BUFFER = 122;

            uint size = 0;

            var status =
                TdhNativeMethods.TdhGetEventInformation(
                    record,
                    0,
                    IntPtr.Zero,
                    IntPtr.Zero,
                    ref size);

            if (status != ERROR_INSUFFICIENT_BUFFER)
                throw new InvalidOperationException(
                    $"TDH size query failed: {status}");

            var buffer =
                Marshal.AllocHGlobal((int)size);

            status =
                TdhNativeMethods.TdhGetEventInformation(
                    record,
                    0,
                    IntPtr.Zero,
                    buffer,
                    ref size);

            if (status != ERROR_SUCCESS)
            {
                Marshal.FreeHGlobal(buffer);
                throw new InvalidOperationException(
                    $"TDH fetch failed: {status}");
            }

            return new TraceEventInfoHandle(
                buffer,
                size);
        }
    }
}
