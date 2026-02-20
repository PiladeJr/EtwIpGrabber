namespace EtwIpGrabber.EtwStructure.SessionManager.Configuration.Implementation
{
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
