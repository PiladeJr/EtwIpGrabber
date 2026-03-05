using EtwIpGrabber.EtwIntegration.SessionManager.Abstraction;
using EtwIpGrabber.EtwIntegration.SessionManager.Configuration;
using EtwIpGrabber.EtwIntegration.SessionManager.Native;
using System.Runtime.InteropServices;

namespace EtwIpGrabber.EtwIntegration.SessionManager
{
    /// <summary>
    /// Gestisce il lifecycle completo di una ETW realtime session in modo crash-safe,
    /// includendo creazione, attach a sessione esistente e stop deterministico.
    /// </summary>
    public sealed class EtwSessionController : IEtwSessionController
    {
        /// <summary>
        /// Configurazione immutabile della sessione ETW (nome, buffer, flush timer, log mode).
        /// </summary>
        private readonly IEtwSessionConfig _config;
        /// <summary>
        /// Wrapper dell'handle kernel (TRACEHANDLE) della sessione ETW.
        /// </summary>
        private readonly NativeEtwSessionHandle _handle;
        /// <summary>
        /// Factory per la creazione di EVENT_TRACE_PROPERTIES in formato ABI-safe.
        /// </summary>
        private readonly EtwSessionPropertiesFactory _factory;

        /// <summary>
        /// Indica se il controller è attualmente attaccato a una sessione ETW valida.
        /// </summary>
        private volatile bool _running;

        /// <summary>
        /// Restituisce l'handle kernel della sessione ETW attiva.
        /// </summary>
        public ulong SessionHandle => _handle.Handle;
        /// <summary>
        /// Restituisce il nome della sessione ETW.
        /// </summary>
        public string SessionName => _config.SessionName;
        /// <summary>
        /// Indica se il controller è attualmente attivo.
        /// </summary>
        public bool IsRunning => _running;

        /// <summary>
        /// Inizializza il controller con configurazione e factory delle proprietà ETW.
        /// </summary>
        public EtwSessionController(
            IEtwSessionConfig config,
            EtwSessionPropertiesFactory factory)
        {
            _config = config;
            _factory = factory;
            _handle = new NativeEtwSessionHandle();
        }

        /// <summary>
        /// Avvia una nuova sessione ETW oppure si attacca a una esistente in caso di crash o restart.
        /// </summary>
        /// <remarks>
        /// Se la sessione esiste già (es. dopo crash del servizio),
        /// ETW restituisce ERROR_ALREADY_EXISTS.
        /// 
        /// In questo caso viene eseguito un attach tramite:
        /// <list type="bullet">
        ///   <item><description>ControlTrace(EVENT_TRACE_CONTROL_QUERY);</description></item>
        ///   <item><description>lettura di Wnode.HistoricalContext;</description></item>
        ///   <item><description>recovery del TRACEHANDLE kernel.</description></item>
        /// </list>
        /// 
        /// Questo pattern consente:
        /// <list type="bullet">
        ///   <item><description>restart SCM-safe;</description></item>
        ///   <item><description>crash recovery;</description></item>
        ///   <item><description>assenza di duplicazione sessioni ETW.</description></item>
        /// </list>
        /// </remarks>
        public void StartOrAttach()
        {
            IntPtr propsBuffer = _factory.Create(_config);

            try
            {
                uint result = EtwNativeMethods.StartTrace(
            out ulong handle,
            _config.SessionName,
            propsBuffer);

                if (result == 0)
                {
                    _handle.Set(handle);
                    _running = true;
                    return;
                }

                const uint ERROR_ALREADY_EXISTS = 183;

                if (result == ERROR_ALREADY_EXISTS)
                {
                    Attach(propsBuffer);
                    return;
                }

                throw new InvalidOperationException(
                    $"StartTrace failed: {result}");

            }
            finally
            {
                Marshal.FreeHGlobal(propsBuffer);
            }
        }

        private unsafe void Attach(nint propsBuffer)
        {
            // ControlTrace con EVENT_TRACE_CONTROL_QUERY recupera le session properties e statistics,
            // incluso l'handle. Utilizziamo questo per attaccarci alla sessione esistente.
            const uint EVENT_TRACE_CONTROL_QUERY = 0;

            uint result = EtwNativeMethods.ControlTrace(
                0,
                _config.SessionName,
                propsBuffer,
                EVENT_TRACE_CONTROL_QUERY);

            if (result != 0)
                throw new InvalidOperationException(
                    $"Attach failed: {result}");

            var props = (EVENT_TRACE_PROPERTIES*)propsBuffer;

            _handle.Set(props->Wnode.HistoricalContext);
            _running = true;
        }

        /// <summary>
        /// Arresta la sessione ETW in modo deterministico e crash-safe. Impedendo la generazione di nuovi eventi
        /// </summary>
        /// <remarks>
        /// Gli eventi già presenti nei buffer ETW
        /// possono essere ancora dispatchati al consumer
        /// fino alla terminazione di ProcessTrace().
        /// Il metodo gestisce:
        /// <list type="bullet">
        ///     <item><description>stop della sessione avvenuto con successo: Return code 0;</description></item>
        ///     <item><description>sessione già terminata: Return code 4021;</description></item>
        ///     <item><description>per qualsiasi altro errore il ritorno dell'eccezionie contenente il codice di ritorno</description></item>
        /// </list>
        /// </remarks>
        public void Stop()
        {
            if (!_running)
                return;

            const uint EVENT_TRACE_CONTROL_STOP = 1;
            const uint ERROR_WMI_INSTANCE_NOT_FOUND = 4201;

            IntPtr propsBuffer = _factory.Create(_config);

            try
            {
                uint result = EtwNativeMethods.ControlTrace(
                    _handle.Handle,
                    _config.SessionName,
                    propsBuffer,
                    EVENT_TRACE_CONTROL_STOP);

                if (result == 0)
                {
                    _running = false;
                    _handle.Set(0);
                    return;
                }

                if (result == ERROR_WMI_INSTANCE_NOT_FOUND)
                {
                    _running = false;
                    _handle.Set(0);
                    return;
                }

                throw new InvalidOperationException(
                    $"ETW Stop failed: {result}");
            }
            finally
            {
                Marshal.FreeHGlobal(propsBuffer);
            }
        }
        /// <summary>
        /// Arresta la sessione ETW privata creata dal servizio.
        /// </summary>
        /// <remarks>
        /// L'invocazione di <see cref="Dispose"/> garantisce:
        /// <list type="bullet">
        ///   <item><description>terminazione della realtime tracing session;</description></item>
        ///   <item><description>assenza di sessioni ETW private orfane dopo shutdown;</description></item>
        ///   <item><description>possibilità di riavvio del servizio senza cleanup manuale.</description></item>
        /// </list>
        /// 
        /// Questa operazione non interagisce con il global
        /// NT Kernel Logger (SystemTraceProvider)
        /// e non impatta altri consumer di sistema
        /// (es. Windows Defender, WPR, netsh trace).
        /// </remarks>
        public void Dispose()
        {
            Stop();

        }
    }
}
