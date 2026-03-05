using EtwIpGrabber.EtwIntegration.RealTimeConsumer.Native.Structures;
using EtwIpGrabber.TdhParsing.Decoder;
using EtwIpGrabber.TdhParsing.Normalization.Models;
using EtwIpGrabber.Utils.ProcessNameResolver;

namespace EtwIpGrabber.TdhParsing.Normalization
{
    /// <summary>
    /// Responsabile della trasformazione del dato TCP raw in un oggetto di dominio 
    /// semanticamente significativo.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Rappresenta la fase finale della pipeline di parsing TDH:
    /// </para>
    /// <code>
    /// RawTcpDecodedEvent → TcpEvent
    /// </code>
    /// <para>
    /// In questa fase vengono applicate:
    /// </para>
    /// <list type="bullet">
    ///   <item><description>Conversioni endianess (IP, porte)</description></item>
    ///   <item><description>Mapping EventId → TcpEventType</description></item>
    ///   <item><description>Conversione timestamp ETW → UTC</description></item>
    ///   <item><description>Traduzione Direction e Flags</description></item>
    /// </list>
    /// <para>
    /// L'output prodotto è pronto per:
    /// </para>
    /// <list type="bullet">
    ///   <item><description>Connection lifecycle reconstruction</description></item>
    ///   <item><description>Community-ID computation</description></item>
    ///   <item><description>Persistenza su DB</description></item>
    /// </list>
    /// </remarks>
    internal class TcpEventNormalizer(IProcessNameResolver processNameResolver)
    {
        /// <summary>
        /// Normalizza il payload TCP raw decodificato utilizzando le informazioni 
        /// presenti nell'header dell'evento ETW.
        /// </summary>
        /// <remarks>
        /// <para>
        /// La normalizzazione trasforma i valori in network byte order provenienti dal 
        /// <see cref="SequentialTdhDecoder"/> in valori semanticamente significativi:
        /// </para>
        /// <list type="bullet">
        ///   <item><description>Indirizzi IP: da <c>uint</c> in network byte order a host byte order</description></item>
        ///   <item><description>Porte: già in host byte order, nessuna conversione necessaria</item>
        ///   <item><description>Direction: da codice raw a enum <see cref="TcpDirection"/></description></item>
        ///   <item><description>Flags: da codice raw a enum <see cref="TcpFlags"/></description></item>
        ///   <item><description>Timestamp: da FILETIME a <see cref="DateTime"/> UTC</description></item>
        ///   <item><description>EventType: da EventDescriptor.Id a enum <see cref="TcpEventType"/></description></item>
        /// </list>
        /// </remarks>
        /// <param name="raw">
        /// Struttura ottenuta dal decoder TDH contenente i valori estratti dal payload 
        /// in runtime representation (network byte order).
        /// </param>
        /// <param name="header">
        /// Header ETW contenente timestamp e <c>EventDescriptor</c> per il mapping 
        /// del tipo di evento.
        /// </param>
        /// <returns>
        /// Istanza di <see cref="TcpEvent"/> completamente normalizzata e pronta per 
        /// l'elaborazione nelle fasi successive della pipeline.
        /// </returns>
        public TcpEvent Normalize(          //ignora il worning di rendere il metodo statico. se reso tale l'app crasha :(
            in RawTcpDecodedEvent raw,
            in EVENT_HEADER header)
        {
            return new TcpEvent
            {
                TimestampUtc = ConversionUtil.ConvertTimestamp(header.TimeStamp),

                ProcessId = raw.ProcessId,
                ProcessName = processNameResolver.Resolve(raw.ProcessId),

                LocalIP = ConversionUtil.Ntohl(raw.LocalAddress),
                RemoteIP = ConversionUtil.Ntohl(raw.RemoteAddress),

                LocalPort = raw.LocalPort,
                RemotePort = raw.RemotePort,

                Direction = ConversionUtil.DecodeDirection(raw.Direction),
                Flags = ConversionUtil.DecodeFlags(raw.TcpFlags),

                EventType = ConversionUtil.MapEventType(header.EventDescriptor)
            };
        }
    }
}
