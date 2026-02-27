namespace EtwIpGrabber.EtwStructure.SessionManager.Configuration.Implementation
{
    /// <summary>
    /// Factory per la creazione della configurazione ETW
    /// predefinita adatta al consumo realtime del provider
    /// Microsoft-Windows-TCPIP.
    /// </summary>
    /// <remarks>
    /// La configurazione risultante:
    /// <list type="bullet">
    ///   <item><description>abilita il realtime dispatch;</description></item>
    ///   <item><description>disabilita il kernel system logger;</description></item>
    ///   <item><description>utilizza un profilo di buffering
    ///   ottimizzato per workload di rete ad alta frequenza.</description></item>
    /// </list>
    /// </remarks>
    public sealed class DefaultEtwSessionConfig
    {
        public static EtwSessionConfig Create()
        {
            return new EtwSessionConfig(
                sessionName: "EtwTcpLifecycleSession",
                tuningProfile: BufferTuningProfile.HighThroughput(),
                realTime: true,
                systemLogger: false
            );
        }
    }
}
