using EtwIpGrabber.EtwIntegration.RealTimeConsumer;
using EtwIpGrabber.EtwIntegration.RealTimeConsumer.Native.Structures;
using System.Runtime.InteropServices;

namespace EtwIpGrabber.TdhParsing.Interop
{
    /// <summary>
    /// Runtime replay context utilizzato per ricostruire puntatori nativi ABI-safe
    /// a partire da uno <see cref="EventRecordSnapshot"/> managed.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Gli <c>EVENT_RECORD</c> forniti da ETW durante la <c>EventRecordCallback</c>
    /// sono validi esclusivamente per la durata della callback stessa.
    /// Dopo il ritorno dalla callback:
    /// </para>
    ///
    /// <list type="bullet">
    /// <item>I buffer kernel vengono riutilizzati</item>
    /// <item>I puntatori <c>UserData</c> diventano invalidi</item>
    /// <item>Qualsiasi accesso successivo genera comportamento indefinito</item>
    /// </list>
    ///
    /// <para>
    /// Per consentire il parsing asincrono tramite TDH al di fuori della callback,
    /// il payload dell'evento viene snapshot-tato in managed memory
    /// (<see cref="EventRecordSnapshot"/>).
    /// </para>
    ///
    /// <para>
    /// Questa classe:
    /// </para>
    ///
    /// <list type="number">
    /// <item>Pinna gli array managed (<c>UserData</c>, <c>ExtendedData</c>)</item>
    /// <item>Ottiene puntatori stabili ai buffer pinned</item>
    /// <item>Li espone come payload replay runtime TDH-compatible</item>
    /// </list>
    ///
    /// <para>
    /// I puntatori restituiti possono essere utilizzati per:
    /// </para>
    ///
    /// <list type="bullet">
    /// <item><c>TdhGetEventInformation</c></item>
    /// <item><c>TdhFormatProperty</c></item>
    /// </list>
    ///
    /// <para>
    /// ⚠️ IMPORTANTE:
    /// </para>
    ///
    /// <list type="bullet">
    /// <item>
    /// I puntatori restituiti sono validi esclusivamente per la durata di questo contesto.
    /// </item>
    /// <item>
    /// Non devono essere memorizzati né utilizzati dopo il Dispose.
    /// </item>
    /// <item>
    /// Il rilascio del pin causa dangling pointers e crash nelle API TDH.
    /// </item>
    /// </list>
    ///
    /// <para>
    /// Questa struttura deve essere:
    /// </para>
    ///
    /// <list type="bullet">
    /// <item>Allocata su stack (<c>ref struct</c>)</item>
    /// <item>Utilizzata nello stesso scope del parsing TDH</item>
    /// </list>
    /// </remarks>
    internal unsafe ref struct EventRecordReplayContext
    {
        /// <summary>
        /// Handle GC utilizzato per pinning del payload UserData.
        /// Impedisce al GC di spostare il buffer durante il parsing TDH.
        /// </summary>
        private GCHandle _userDataHandle;
        /// <summary>
        /// Handle GC utilizzato per pinning dei dati estesi ETW.
        /// </summary>
        private GCHandle _extendedDataHandle;

        /// <summary>
        /// Costruisce il contesto di replay pinning i buffer snapshot-tati.
        /// </summary>
        /// <param name="snapshot">
        /// Snapshot managed-safe dell'evento ETW originale.
        /// </param>
        /// <param name="userData">
        /// Puntatore nativo al payload evento.
        /// Valido fino alla Dispose del contesto.
        /// </param>
        /// <param name="extendedData">
        /// Puntatore ai dati estesi ETW.
        /// Valido fino alla Dispose del contesto.
        /// </param>
        public EventRecordReplayContext(
            in EventRecordSnapshot snapshot,
            out byte* userData,
            out EVENT_HEADER_EXTENDED_DATA_ITEM* extendedData)
        {
            // Pinning del payload UserData eseguito a runtime
            if (snapshot.UserData is { Length: > 0 })
            {
                _userDataHandle =
                    GCHandle.Alloc(
                        snapshot.UserData,
                        GCHandleType.Pinned);

                userData =
                    (byte*)
                    _userDataHandle
                        .AddrOfPinnedObject();
            }
            else
            {
                userData = null;
            }
            // Pinning dei dati estesi (se presenti)
            if (snapshot.ExtendedData is { Length: > 0 })
            {
                _extendedDataHandle =
                    GCHandle.Alloc(
                        snapshot.ExtendedData,
                        GCHandleType.Pinned);

                extendedData =
                    (EVENT_HEADER_EXTENDED_DATA_ITEM*)
                    _extendedDataHandle
                        .AddrOfPinnedObject();
            }
            else
            {
                extendedData = null;
            }
        }
        /// <summary>
        /// Rilascia i pin GCHandle associati ai buffer replay.
        /// Deve essere invocato al termine del parsing TDH.
        /// </summary>
        public void Dispose()
        {
            if (_userDataHandle.IsAllocated)
                _userDataHandle.Free();

            if (_extendedDataHandle.IsAllocated)
                _extendedDataHandle.Free();
        }
    }
}