namespace EtwIpGrabber.EtwIntegration.SessionManager.Configuration
{
    /// <summary>
    /// Rappresenta un profilo di tuning per la configurazione dei buffer in una sessione 
    /// ETW (Event Tracing for Windows).
    /// </summary>
    /// <remarks>
    /// Ha lo scopo di evitare configurazioni arbitrariamente errate e fornire profili predefiniti per scenari comuni,
    /// come configurazioni standard e ad alta velocità. Tutto ciò serve per evitare drop di eventi 
    /// dal kernel a causa di buffer troppo piccoli o insufficienti, e per garantire prestazioni ottimali 
    /// in base alle esigenze specifiche dell'applicazione.
    /// </remarks>
    public sealed class BufferTuningProfile
    {
        /// <summary>
        /// La dimensione del buffer in kilobyte. 
        /// Valori troppo piccoli (ad esempio, inferiori a 32 KB) possono causare drop di eventi,
        /// </summary>
        public uint BufferSizeKb { get; }
        /// <summary>
        /// Valore minimo di buffer che la sessione deve utilizzare.
        /// Indica la steady state dei buffer, ovvero quanti buffer la sessione utilizza normalmente.
        /// </summary>
        public uint MinimumBuffers { get; }
        /// <summary>
        /// Valore massimo di buffer che la sessione può utilizzare.
        /// Indica la burst absortion dei buffer, ovvero quanti buffer la sessione può utilizzare in caso di picchi di eventi.
        /// Valori troppo bassi possono causare drop di eventi se la sessione non riesce a 
        /// liberare i buffer abbastanza velocemente,
        /// </summary>
        public uint MaximumBuffers { get; }
        /// <summary>
        /// Latenza di flush dei buffer in secondi. 
        /// Valori più bassi possono ridurre la latenza ma aumentare l'overhead,
        /// </summary>
        public uint FlushTimerSeconds { get; }

        private BufferTuningProfile(
            uint bufferSizeKb,
            uint minBuffers,
            uint maxBuffers,
            uint flushTimerSeconds)
        {
            BufferSizeKb = bufferSizeKb;
            MinimumBuffers = minBuffers;
            MaximumBuffers = maxBuffers;
            FlushTimerSeconds = flushTimerSeconds;
        }
        /// <summary>
        /// Crea un profilo di tuning standard con valori bilanciati.
        /// </summary>
        /// <returns>
        /// Il profilo di tuning standard con:
        ///    <br/> - dimensione del buffer di 64 KB,
        ///    <br/> - minimo di 128 buffer,
        ///    <br/> - massimo di 1024 buffer,
        ///    <br/> - timer di flush di 1 secondo.
        /// </returns>
        public static BufferTuningProfile Standard() =>
            new(
                bufferSizeKb: 64,
                minBuffers: 128,
                maxBuffers: 1024,
                flushTimerSeconds: 1);
        /// <summary>
        /// Crea un profilo di tuning per high troughput. Utile nel caso in cui
        /// si prevede un numero elevato di connessioni (scan, server, ecc.)
        /// </summary>
        /// <returns>
        /// Il profilo di tuning per High Trouthput con:
        ///    <br/> - dimensione del buffer di 128 KB,
        ///    <br/> - minimo di 256 buffer,
        ///    <br/> - massimo di 2048 buffer,
        ///    <br/> - timer di flush di 1 secondo.
        /// </returns>
        public static BufferTuningProfile HighThroughput() =>
            new(
                bufferSizeKb: 128,
                minBuffers: 256,
                maxBuffers: 2048,
                flushTimerSeconds: 1);
    }
}
