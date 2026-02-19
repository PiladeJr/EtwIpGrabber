using EtwIpGrabber.EtwStructure.EventDispatcher;
using EtwIpGrabber.EtwStructure.RealTimeConsumer.Native;
using EtwIpGrabber.EtwStructure.RealTimeConsumer.Native.Structures;
using System.Runtime.InteropServices;

namespace EtwIpGrabber.EtwStructure.RealTimeConsumer
{
    public sealed class RealtimeEtwConsumer : IRealtimeEtwConsumer
    {
        private ulong _traceHandle;
        private Thread _processingThread;

        private readonly IEventDispatcher _dispatcher;

        private EventRecordCallback _callback;
        private GCHandle _callbackHandle;

        public RealtimeEtwConsumer(IEventDispatcher dispatcher)
        {
            _dispatcher = dispatcher;
        }

        public unsafe void Start(string sessionName)
        {
            EVENT_TRACE_LOGFILE logfile = new EVENT_TRACE_LOGFILE
            {
                LoggerName = sessionName,
                ProcessTraceMode =
                    ProcessTraceMode.PROCESS_TRACE_MODE_REAL_TIME |
                    ProcessTraceMode.PROCESS_TRACE_MODE_EVENT_RECORD
            };

            _callback = OnEventRecord;
            _callbackHandle = GCHandle.Alloc(_callback);

            logfile.EventRecordCallback =
                Marshal.GetFunctionPointerForDelegate(_callback);

            _traceHandle = NativeEtwConsumer.OpenTrace(ref logfile);

            _processingThread = new Thread(ProcessLoop)
            {
                IsBackground = true
            };

            _processingThread.Start();
        }

        private void ProcessLoop()
        {
            NativeEtwConsumer.ProcessTrace(
                new[] { _traceHandle },
                1,
                IntPtr.Zero,
                IntPtr.Zero);
        }

        private unsafe void OnEventRecord(EVENT_RECORD* record)
        {
            _dispatcher.TryEnqueue(record);
        }

        public void Dispose()
        {
            NativeEtwConsumer.CloseTrace(_traceHandle);

            if (_callbackHandle.IsAllocated)
                _callbackHandle.Free();
        }
    }
}
