namespace EtwIpGrabber.EtwStructure.SessionManager.Configuration
{
    /// <summary>
    /// Classe responsabile di garantire che la configurazione di una sessione ETW 
    /// sia valida prima di tentare di avviarla, prevenendo Crash a Runtime.
    /// Controlla parametri come BufferSizeKb, MinimumBuffers, MaximumBuffers e SessionName
    /// per assicurarsi che rispettino i requisiti minimi e siano coerenti tra loro.
    /// </summary>
    public static class EtwSessionConfigValidator
    {
        public static void Validate(IEtwSessionConfig config)
        {
            ArgumentNullException.ThrowIfNull(config);
            switch (config)
            {
                case { BufferSizeKb: < 32 }:
                    throw new ArgumentException("BufferSizeKb too small");

                case { MaximumBuffers: var max, MinimumBuffers: var min } when max < min:
                    throw new ArgumentException("MaxBuffers < MinBuffers");

                case { MinimumBuffers: < 64 }:
                    throw new ArgumentException("MinBuffers unsafe for TCP ETW");

                case var c when string.IsNullOrWhiteSpace(c.SessionName):
                    throw new ArgumentException("SessionName required");

                default:
                    break;
            }
        }
    }
}
