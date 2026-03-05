using EtwIpGrabber.EtwIntegration.RealTimeConsumer.Native.Structures;
using System.Runtime.InteropServices;

namespace EtwIpGrabber.EtwIntegration.RealTimeConsumer.Native
{
    /// <summary>
    /// Delegate invocato da ETW per ogni evento ricevuto dalla sessione realtime.
    /// </summary>
    /// <remarks>
    /// Questa funzione viene eseguita nel contesto del thread di consegna ETW
    /// e rappresenta il punto di ingresso della pipeline di raccolta.
    /// Deve essere minima e non-bloccante:
    /// <list type="bullet">
    ///   <item><description>non effettuare parsing TDH;</description></item>
    ///   <item><description>
    ///   evitare operazioni di lunga durata o blocchi;
    ///   allocazioni managed sono consentite purché non introducano
    ///   latenza significativa nella callback.</description></item>
    ///   <item><description>non effettuare I/O;</description></item>
    ///   <item><description>non eseguire operazioni di lunga durata.</description></item>
    /// </list>
    /// La callback deve limitarsi a copiare o accodare il puntatore
    /// <c>EVENT_RECORD</c> in una coda lock-free/staging per l'elaborazione
    /// successiva, evitando perdita di eventi lato kernel (EventsLost).
    /// <para>
    /// Importante: l'istanza del delegate deve essere pinned (ad esempio usando
    /// <c>GCHandle.Alloc(delegate, GCHandleType.Pinned)</c>) per tutta la durata
    /// di <c>ProcessTrace</c> così che il runtime non la sposti invalidando il
    /// puntatore nativo alla funzione.
    /// </para>
    /// </remarks>
    /// <param name="eventRecord">Puntatore alla struttura nativa <c>EVENT_RECORD</c> fornita da ETW.</param>
    [UnmanagedFunctionPointer(CallingConvention.Winapi)]
    public unsafe delegate void EventRecordCallback(
        EVENT_RECORD* record);
}
