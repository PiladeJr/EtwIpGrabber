using EtwIpGrabber.EtwStructure.EventDispatcher;
using EtwIpGrabber.EtwStructure.RealTimeConsumer.Native;
using EtwIpGrabber.EtwStructure.RealTimeConsumer.Native.Structures;
using System.Runtime.InteropServices;

namespace EtwIpGrabber.EtwStructure.RealTimeConsumer
{
    /// <summary>
    /// Implementa il consumo realtime degli eventi ETW provenienti dalla sessione TCPIP.
    /// </summary>
    /// <remarks>
    /// Responsabilità:
    /// <list type="bullet">
    ///   <item><description>apertura della sessione tramite <c>OpenTrace</c>;</description></item>
    ///   <item><description>registrazione della callback <c>EVENT_RECORD</c>;</description></item>
    ///   <item><description>avvio del loop <c>ProcessTrace</c> su thread dedicato;</description></item>
    ///   <item><description>inoltro degli eventi alla pipeline interna.</description></item>
    /// </list>
    /// La callback ETW deve essere pinned per evitare invalidazione da parte del GC.
    /// </remarks>
    public sealed class RealtimeEtwConsumer : IRealtimeEtwConsumer
    {
        private ulong _traceHandle;
        private Thread _processingThread;
        private readonly IEventDispatcher _dispatcher;
        private EventRecordCallback _callback;
        
        private GCHandle _callbackHandle;

        /// <summary>
        /// Inizializza una nuova istanza di <see cref="RealtimeEtwConsumer"/>.
        /// </summary>
        /// <param name="dispatcher">Dispatcher per l'accodamento degli eventi ETW.</param>
        public RealtimeEtwConsumer(IEventDispatcher dispatcher)
        {
            _dispatcher = dispatcher;
        }

        /// <summary>
        /// Avvia il consumo realtime degli eventi ETW per la sessione specificata.
        /// </summary>
        /// <param name="sessionName">Nome della sessione ETW a cui collegarsi.</param>
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

        /// <summary>
        /// Loop bloccante eseguito su thread dedicato che elabora gli eventi ETW.
        /// </summary>
        private void ProcessLoop()
        {
            NativeEtwConsumer.ProcessTrace(
                new[] { _traceHandle },
                1,
                IntPtr.Zero,
                IntPtr.Zero);
        }

        /// <summary>
        /// Callback invocata da ETW per ogni evento ricevuto dalla sessione realtime.
        /// </summary>
        /// <param name="record">Puntatore alla struttura nativa <c>EVENT_RECORD</c>.</param>
        private unsafe void OnEventRecord(EVENT_RECORD* record)
        {
            _dispatcher.TryEnqueue(record);
        }

        /// <summary>
        /// Rilascia le risorse native e libera il GC handle della callback.
        /// </summary>
        public void Dispose()
        {
            NativeEtwConsumer.CloseTrace(_traceHandle);

            if (_callbackHandle.IsAllocated)
                _callbackHandle.Free();
        }
    }
}
