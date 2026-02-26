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
        private readonly ILogger<RealtimeEtwConsumer> _logger;

        private ulong _traceHandle;
        private Thread _processingThread;
        private readonly IEventDispatcher _dispatcher;
        private EventRecordCallback _callback;
        private IntPtr _loggerNamePtr;
        private IntPtr _logfilePtr;

        private GCHandle _callbackHandle;

        /// <summary>
        /// Inizializza una nuova istanza di <see cref="RealtimeEtwConsumer"/>.
        /// </summary>
        /// <remarks>
        /// <b>Attenzione:</b>
        /// EVENT_TRACE_LOGFILE deve rimanere allocato per tutta
        /// la durata di ProcessTrace().
        /// 
        /// ETW non effettua una copia della struttura ma mantiene
        /// il puntatore interno per il dispatch degli eventi.
        /// 
        /// Il rilascio anticipato della memoria causa:
        /// <list type="bullet">
        ///   <item><description>heap corruption;</description></item>
        ///   <item><description>AccessViolationException;</description></item>
        ///   <item><description>terminazione del processo in ntdll.dll.</description></item>
        /// </list> 
        /// </remarks>
        /// <param name="dispatcher">Dispatcher per l'accodamento degli eventi ETW.</param>
        public RealtimeEtwConsumer(IEventDispatcher dispatcher, ILogger<RealtimeEtwConsumer> logger)
        {
            _dispatcher = dispatcher;
            _logger = logger;
        }

        /// <summary>
        /// Avvia il consumo realtime degli eventi ETW per la sessione specificata.
        /// </summary>
        /// <param name="sessionName">Nome della sessione ETW a cui collegarsi.</param>
        public unsafe void Start(string sessionName)
        {
            // Alloca LoggerName (LPWSTR)
            _loggerNamePtr = Marshal.StringToHGlobalUni(sessionName);

            //Alloca unmanaged EVENT_TRACE_LOGFILE
            int size = Marshal.SizeOf<EVENT_TRACE_LOGFILE>() +
                Marshal.SizeOf<TRACE_LOGFILE_HEADER>();
            _logfilePtr = Marshal.AllocHGlobal(size);

            Span<byte> span = new((void*)_logfilePtr, size);
            span.Clear();

            var logfile = (EVENT_TRACE_LOGFILE*)_logfilePtr;

            logfile->LoggerName = _loggerNamePtr;

            logfile->ProcessTraceMode =
                ProcessTraceMode.PROCESS_TRACE_MODE_REAL_TIME |
                ProcessTraceMode.PROCESS_TRACE_MODE_EVENT_RECORD;

            //Inizializza delegate PRIMA di usarlo
            _callback = OnEventRecord;

            //Pin delegate (lifetime ETW session)
            _callbackHandle = GCHandle.Alloc(_callback);

            //Ora ottieni function pointer unmanaged
            logfile->EventRecordCallback =
                Marshal.GetFunctionPointerForDelegate(_callback);

            //OpenTrace
            _traceHandle = NativeEtwConsumer.OpenTrace(_logfilePtr);

            if (_traceHandle == ulong.MaxValue)
                throw new InvalidOperationException("OpenTrace failed.");

            //Start ProcessTrace loop su thread dedicato
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
            uint result = NativeEtwConsumer.ProcessTrace(
            [_traceHandle],
            1,
            IntPtr.Zero,
            IntPtr.Zero);

            if (result == 0)
            {
                _logger.LogInformation("ProcessTrace exited normally.");
                return;
            }

            const uint ERROR_CANCELLED = 1223;
            const uint ERROR_WMI_INSTANCE_NOT_FOUND = 4201;

            if (result == ERROR_CANCELLED ||
                result == ERROR_WMI_INSTANCE_NOT_FOUND)
            {
                _logger.LogWarning(
                    "ProcessTrace stopped: {Code}", result);
                return;
            }

            _logger.LogCritical(
                "ProcessTrace FAILED with error: {Code}", result);
        }

        /// <summary>
        /// Callback invocata da ETW per ogni evento ricevuto dalla sessione realtime.
        /// </summary>
        /// <param name="record">Puntatore alla struttura nativa <c>EVENT_RECORD</c>.</param>
        private unsafe void OnEventRecord(EVENT_RECORD* record)
        {
            var snapshot = new EventRecordSnapshot
            {
                Header = record->EventHeader,
                ExtendedDataCount = record->ExtendedDataCount,
                UserDataLength = record->UserDataLength,
                UserData = new byte[record->UserDataLength],
                BufferContext = record->BufferContext,
                ExtendedData =
                    new byte[
                        record->ExtendedDataCount *
                        sizeof(EVENT_HEADER_EXTENDED_DATA_ITEM)]
            };

            if (record->UserData != IntPtr.Zero)
            {
                Marshal.Copy(
                    record->UserData,
                    snapshot.UserData,
                    0,
                    record->UserDataLength);
            }

            if (record->ExtendedData != IntPtr.Zero)
            {
                Marshal.Copy(
                    record->ExtendedData,
                    snapshot.ExtendedData,
                    0,
                    snapshot.ExtendedData.Length);
            }

            _dispatcher.TryEnqueue(snapshot);
        }

        /// <summary>
        /// Rilascia le risorse native e libera il GC handle della callback.
        /// </summary>
        public void Dispose()
        {
            if (_traceHandle != 0)
            {
                NativeEtwConsumer.CloseTrace(_traceHandle);
                _traceHandle = 0;
            }

            // ETW non deve più accedere alla memoria prima di liberarla
            if (_logfilePtr != IntPtr.Zero)
            {
                Marshal.FreeHGlobal(_logfilePtr);
                _logfilePtr = IntPtr.Zero;
            }

            if (_loggerNamePtr != IntPtr.Zero)
            {
                Marshal.FreeHGlobal(_loggerNamePtr);
                _loggerNamePtr = IntPtr.Zero;
            }

            if (_callbackHandle.IsAllocated)
            {
                _callbackHandle.Free();
            }
        }
    }
}
