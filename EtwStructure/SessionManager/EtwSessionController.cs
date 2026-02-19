using EtwIpGrabber.EtwStructure.SessionManager.Abstraction;
using EtwIpGrabber.EtwStructure.SessionManager.Configuration;
using EtwIpGrabber.EtwStructure.SessionManager.Native;
using System.Runtime.InteropServices;

namespace EtwIpGrabber.EtwStructure.SessionManager
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
        /// Il metodo avvia una sessione ETW con le proprietà specificate nella configurazione.
        /// Se la sessione esiste già, richiama il metodo <see cref="Attach(nint)"/> per recuperare l'handle e usarlo
        /// per recuperare l'historical context.<br/>
        /// L'invocazione di <see cref="StartOrAttach"/> garantisce:
        /// <list type="bullet">
        ///   <item><description>recovery post crash;</description></item>
        ///   <item><description>restart SCM safe</description></item>
        ///   <item><description>nessuna sessione duplicata</description></item>
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
        /// Arresta la sessione ETW in modo deterministico e crash-safe.
        /// </summary>
        /// <remarks>
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
        /// Chiude la sessione ETW richiamando il metodo <see cref="Stop"/> per garantire
        /// la terminazione corretta durante lo shutdown del servizio Windows.
        /// </summary>
        /// <remarks>
        /// L'invocazione di <see cref="Dispose"/> garantisce:
        /// <list type="bullet">
        ///   <item><description>rilascio del kernel logger;</description></item>
        ///   <item><description>assenza di sessioni ETW orfane dopo lo shutdown del servizio;</description></item>
        ///   <item><description>possibilità di riavviare il servizio senza strumenti esterni per terminare sessioni ETW residue.</description></item>
        /// </list>
        /// </remarks>
        public void Dispose()
        {
            Stop();

        }
    }
}
