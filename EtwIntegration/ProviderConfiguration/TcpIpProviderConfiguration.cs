using EtwIpGrabber.EtwIntegration.ProviderConfiguration.Abstractions;
using EtwIpGrabber.EtwIntegration.ProviderConfiguration.Native;

namespace EtwIpGrabber.EtwIntegration.ProviderConfiguration
{
    /// <summary>
    /// Implementazione del configuratore per il provider <c>Microsoft-Windows-TCPIP</c>.
    /// </summary>
    /// <remarks>
    /// Questo componente abilita il provider sulla sessione ETW tramite
    /// <c>EnableTraceEx2</c>, specificando Level, Keywords e parametri avanzati
    /// di enablement.
    /// <br/>
    /// L'utilizzo di <c>EVENT_ENABLE_PROPERTY_PROCESS_START_KEY</c> consente al
    /// kernel di arricchire gli eventi con una Process Start Key univoca,
    /// mitigando il rischio di PID reuse durante la ricostruzione del
    /// lifecycle TCP.
    ///
    /// Questo è fondamentale per:
    /// <list type="bullet">
    ///   <item><description>correlazione PID → Processo;</description></item>
    ///   <item><description>detection accuracy;</description></item>
    ///   <item><description>calcolo corretto del Community ID associato a processi.</description></item>
    /// </list>
    /// L'invocazione richiede l'enablement del provider
    /// ma non garantisce l'immediata emissione di eventi:
    /// 
    /// il dispatch avverrà solo dopo che:
    /// <list type="bullet">
    ///   <item><description>la sessione ETW è attiva;</description></item>
    ///   <item><description>un consumer ha eseguito OpenTrace();</description></item>
    ///   <item><description>ProcessTrace() è in esecuzione.</description></item>
    /// </list>
    /// </remarks>
    public sealed class TcpIpProviderConfigurator : IEtwProviderConfigurator
    {
        private readonly TcpIpProviderDescriptor _descriptor;

        /// <summary>
        /// Inizializza il configuratore con il descriptor del provider TCPIP.
        /// </summary>
        public TcpIpProviderConfigurator()
        {
            _descriptor = new TcpIpProviderDescriptor();
        }

        /// <summary>
        /// Abilita il provider TCPIP sulla sessione ETW specificata.
        /// </summary>
        /// <param name="sessionHandle">
        /// Handle kernel (<c>TRACEHANDLE</c>) della sessione ETW attiva.
        /// </param>
        public unsafe void EnableProvider(ulong sessionHandle)
        {
            Guid provider = _descriptor.Guid;

            const uint EVENT_CONTROL_CODE_ENABLE_PROVIDER = 1;
            const uint ENABLE_TRACE_PARAMETERS_VERSION_2 = 2;
            const uint EVENT_ENABLE_PROPERTY_PROCESS_START_KEY = 0x00000010;

            ENABLE_TRACE_PARAMETERS parameters = new ENABLE_TRACE_PARAMETERS
            {
                Version = ENABLE_TRACE_PARAMETERS_VERSION_2,
                EnableProperty = EVENT_ENABLE_PROPERTY_PROCESS_START_KEY,
                ControlFlags = 0,
                SourceId = Guid.Empty,
                EnableFilterDesc = IntPtr.Zero,
                FilterDescCount = 0
            };

            uint result = EtwEnableTraceNative.EnableTraceEx2(
                sessionHandle,
                ref provider,
                EVENT_CONTROL_CODE_ENABLE_PROVIDER,
                _descriptor.Level,
                _descriptor.Keywords,
                0,
                0,
                (IntPtr)(&parameters));

            if (result != 0)
                throw new InvalidOperationException(
                    $"EnableTraceEx2 TCPIP failed: {result}");
        }
    }
}
