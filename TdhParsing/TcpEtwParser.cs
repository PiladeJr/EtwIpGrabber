using EtwIpGrabber.EtwIntegration.RealTimeConsumer;
using EtwIpGrabber.EtwIntegration.RealTimeConsumer.Native.Structures;
using EtwIpGrabber.TdhParsing.Decoder.Abstraction;
using EtwIpGrabber.TdhParsing.Interop;
using EtwIpGrabber.TdhParsing.Layout;
using EtwIpGrabber.TdhParsing.Layout.Struct;
using EtwIpGrabber.TdhParsing.Metadata;
using EtwIpGrabber.TdhParsing.Metadata.Abstract;
using EtwIpGrabber.TdhParsing.Normalization;
using EtwIpGrabber.TdhParsing.Normalization.Models;

namespace EtwIpGrabber.TdhParsing
{
    internal sealed class TcpEtwParser(
        IEtwMetadataResolver metadata,
        TcpEventLayoutCache layoutCache,
        TcpEventLayoutBuilder builder,
        ITdhDecoder decoder,
        TcpEventNormalizer normalizer,
        ILogger<TcpEtwParser> logger)
        : ITcpEtwParser
    {
        public unsafe bool TryParse(
            in EventRecordSnapshot snapshot,
            out TcpEvent? tcpEvent)
        {
            tcpEvent = null;

            try
            {
                using var replay =
                    new EventRecordReplayContext(
                        snapshot,
                        out var userData,
                        out var extendedData);

                TDH_EVENT_RECORD record = default;

                record.EventHeader = snapshot.Header;
                record.BufferContext = snapshot.BufferContext;
                record.UserDataLength = snapshot.UserDataLength;
                record.ExtendedDataCount = snapshot.ExtendedDataCount;
                record.UserContext = null;

                record.UserData = userData;
                record.ExtendedData = extendedData;

                var meta =
                    metadata.Resolve(&record);

                var info =
                    (TRACE_EVENT_INFO*)
                    meta.Buffer;

                var key =
                    new TdhEventKey(snapshot.Header);

                logger.LogDebug(
                    "TDH Parse attempt: Id={Id} Ver={Ver} Op={Op}",
                    key.Id,
                    key.Version,
                    key.Opcode);

                var layout =
                    layoutCache.GetOrAdd(
                        key,
                        _ => builder.Build(meta.Buffer));

                if (!layout.Supported &&
                    key.Id == 10)   // <-- lifecycle only
                {
                    logger.LogWarning(
                        "Unsupported layout for lifecycle Id={Id} Op={Op}",
                        key.Id,
                        key.Opcode);
                }

                if (!decoder.TryDecode(
                        &record,
                        info,
                        layout,
                        out var raw))
                {
                    logger.LogDebug(
                        "TDH decode failed for EventId={Id}",
                        key.Id);
                    return false;
                }

                tcpEvent =
                    normalizer.Normalize(
                        raw,
                        snapshot.Header);

                return true;
            }
            catch (AccessViolationException av)
            {
                logger.LogCritical(av,
                    "TDH AccessViolation — likely manifest drift or struct mismatch");

                Environment.FailFast("TDH ABI violation", av);

                return false;
            }
            catch (Exception ex)
            {
                logger.LogError(ex,
                    "TDH parsing failed BEFORE layout Id={Id} Ver={Ver} Op={Op}",
                    snapshot.Header.EventDescriptor.Id,
                    snapshot.Header.EventDescriptor.Version,
                    snapshot.Header.EventDescriptor.Opcode);

                return false;
            }
        }
    }
}
