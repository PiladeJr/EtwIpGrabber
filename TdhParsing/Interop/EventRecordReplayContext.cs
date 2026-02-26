using EtwIpGrabber.EtwStructure.RealTimeConsumer;
using EtwIpGrabber.EtwStructure.RealTimeConsumer.Native.Structures;
using System.Runtime.InteropServices;
using TraceReloggerLib;

namespace EtwIpGrabber.TdhParsing.Interop
{
    internal unsafe ref struct EventRecordReplayContext
    {
        private GCHandle _userDataHandle;
        private GCHandle _extendedDataHandle;

        public EventRecordReplayContext(
            in EventRecordSnapshot snapshot,
            out byte* userData,
            out EVENT_HEADER_EXTENDED_DATA_ITEM* extendedData)
        {
            if (snapshot.UserData is { Length: > 0 })
            {
                _userDataHandle =
                    GCHandle.Alloc(
                        snapshot.UserData,
                        GCHandleType.Pinned);

                userData =
                    (byte*)
                    _userDataHandle
                        .AddrOfPinnedObject();
            }
            else
            {
                userData = null;
            }

            if (snapshot.ExtendedData is { Length: > 0 })
            {
                _extendedDataHandle =
                    GCHandle.Alloc(
                        snapshot.ExtendedData,
                        GCHandleType.Pinned);

                extendedData =
                    (EVENT_HEADER_EXTENDED_DATA_ITEM*)
                    _extendedDataHandle
                        .AddrOfPinnedObject();
            }
            else
            {
                extendedData = null;
            }
        }

        public void Dispose()
        {
            if (_userDataHandle.IsAllocated)
                _userDataHandle.Free();

            if (_extendedDataHandle.IsAllocated)
                _extendedDataHandle.Free();
        }
    }
}