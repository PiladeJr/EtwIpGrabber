using EtwIpGrabber.EtwStructure.RealTimeConsumer.Native.Structures;

namespace EtwIpGrabber.TdhParsing.Metadata
{
    internal readonly struct TdhEventKey(in EVENT_HEADER header)
    {
        public readonly Guid ProviderId = header.ProviderId;
        public readonly ushort Id = header.EventDescriptor.Id;
        public readonly byte Version = header.EventDescriptor.Version;
        public readonly byte Opcode = header.EventDescriptor.Opcode;

        public bool Equals(TdhEventKey other)
            => ProviderId == other.ProviderId
            && Id == other.Id
            && Version == other.Version
            && Opcode == other.Opcode;

        public override bool Equals(object? obj)
            => obj is TdhEventKey other && Equals(other);

        public override int GetHashCode()
            => HashCode.Combine(
                ProviderId,
                Id,
                Version,
                Opcode);
    }
}
