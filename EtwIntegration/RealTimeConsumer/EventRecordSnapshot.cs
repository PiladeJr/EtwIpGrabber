using EtwIpGrabber.EtwIntegration.RealTimeConsumer.Native.Structures;

namespace EtwIpGrabber.EtwIntegration.RealTimeConsumer
{
    /// <summary>
    /// Rappresenta una copia managed-safe di un EVENT_RECORD
    /// valida oltre la durata della callback ETW.
    /// </summary>
    /// <remarks>
    /// Gli EVENT_RECORD forniti da ETW sono allocati
    /// su buffer kernel riutilizzabili e diventano
    /// invalidi immediatamente dopo il return dalla
    /// EventRecordCallback.
    /// 
    /// Questa struttura consente:
    /// <list type="bullet">
    ///   <item><description>handoff tra thread ETW e pipeline;</description></item>
    ///   <item><description>processing asincrono sicuro;</description></item>
    ///   <item><description>assenza di use-after-return.</description></item>
    /// </list>
    /// </remarks>
    public unsafe struct EventRecordSnapshot
    {
        public EVENT_HEADER Header;
        public ETW_BUFFER_CONTEXT BufferContext;
        public ushort ExtendedDataCount;
        public ushort UserDataLength;
        public byte[] UserData;
        public byte[] ExtendedData;
    }
}