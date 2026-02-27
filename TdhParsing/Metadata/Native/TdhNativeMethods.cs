using EtwIpGrabber.TdhParsing.Layout.Struct;
using System.Runtime.InteropServices;

namespace EtwIpGrabber.TdhParsing.Metadata.Native
{
    /// <summary>
    /// Wrapper P/Invoke per le API TDH (Trace Data Helper) usate nella risoluzione
    /// del metadata degli eventi ETW manifest-based.
    /// </summary>
    /// <remarks>
    /// La classe contiene le seguenti API:
    /// <list type="bullet">
    ///     <item><description><see href="https://learn.microsoft.com/it-it/windows/win32/api/tdh/nf-tdh-tdhgeteventinformation">TdhGetEventInformation()</see></description></item>
    ///     <item><description><see href="https://learn.microsoft.com/it-it/windows/win32/api/tdh/nf-tdh-tdhformatproperty">TdhFormatProperty()</see></description></item>
    /// </list>
    /// <para>
    /// Note:
    /// </para>
    /// <para>
    /// la firma di <c>TdhGetEventInformation</c> deve rispettare il mapping
    /// Win32 LLP64 dei tipi C:
    /// <list type="bullet">
    ///   <item><description><c>ULONG</c> -> <c>uint</c> (32 bit anche su x64);</description></item>
    ///   <item><description><c>PULONG</c> -> <c>uint*</c>.</description></item>
    /// </list>
    /// la firma di <c>TdhFormatProperty</c> deve rispettare il mapping Win32 LLP64 dei tipi C:
    /// <list type="bullet">
    ///   <item><description><paramref name="PointerSize"/><c>ULONG</c> -> <c>uint</c>, NON <c>ulong</c>.</description></item>
    ///   <item><description><paramref name="BufferSize"/><c>PULONG</c> -> <c>uint*</c>.</description></item>
    /// </list>
    /// Il mancato rispetto del modello LLP64 di Windows provoca <c>ERROR_INVALID_PARAMETER (87)</c> 
    /// già alla prima property.
    /// </para>
    /// L'utilizzo errato di tipi a 64 bit (es. <c>ulong</c>) provoca:
    /// <list type="bullet">
    ///   <item><description>stack misalignment;</description></item>
    ///   <item><description>validazione fallita interna a TDH;</description></item>
    ///   <item><description><c>ERROR_INVALID_PARAMETER (87)</c> Alla prima proprietà, non necessariamente al valore errato</description></item>
    /// </list>
    /// </remarks>
    internal static class TdhNativeMethods
    {
        /// <summary>
        /// Recupera le informazioni di decoding (<c>TRACE_EVENT_INFO</c>) associate
        /// a un <c>EVENT_RECORD</c> ETW.
        /// </summary>
        /// <remarks>
        /// La chiamata deve essere eseguita in due fasi:
        /// <list type="number">
        ///   <item><description>
        ///     Size query: <c>pBuffer = NULL</c> → restituisce <c>ERROR_INSUFFICIENT_BUFFER</c>
        ///     e valorizza <paramref name="pBufferSize"/> con la dimensione richiesta.
        ///   </description></item>
        ///   <item><description>
        ///     Fetch: allocare <paramref name="pBuffer"/> con dimensione corretta →
        ///     restituisce <c>ERROR_SUCCESS</c>.
        ///   </description></item>
        /// </list>
        /// Il buffer risultante contiene:
        /// <list type="bullet">
        ///   <item><description>header <c>TRACE_EVENT_INFO</c>;</description></item>
        ///   <item><description>array <c>EVENT_PROPERTY_INFO[]</c>;</description></item>
        ///   <item><description>string table (offset-based).</description></item>
        /// </list>
        /// Il contenuto è version- e manifest-dependent, quindi non deve essere riutilizzato
        /// tra eventi diversi senza discriminare per ProviderId/EventId/Version/Opcode.
        /// Il puntatore <paramref name="pEvent"/> deve essere ABI-compatible con <c>EVENT_RECORD</c>.
        /// Snapshot non pinned o layout non compatibili causano crash runtime o metadata corruption.
        /// </remarks>
        /// <param name="pEvent">Record dell'evento passato a <c>EventRecordCallback</c>.</param>
        /// <param name="TdhContextCount">Numero di elementi presenti nel context.</param>
        /// <param name="pTdhContext">
        /// Matrice di valori di contesto; in genere <c>IntPtr.Zero</c> (WPP/legacy only).
        /// </param>
        /// <param name="pBuffer">Buffer allocato dall'utente per ricevere le informazioni dell'evento.</param>
        /// <param name="pBufferSize">
        /// Dimensione, in byte, del buffer <paramref name="pBuffer"/>. In output, contiene la
        /// dimensione usata o quella necessaria se il buffer è insufficiente.
        /// </param>
        /// <returns>Codice di errore Win32 (0 indica successo, 122 indica che la dimensione del buffer è insufficiente).</returns>
        [DllImport("tdh.dll", CharSet = CharSet.Unicode)]
        public static extern unsafe uint TdhGetEventInformation(
            void* pEvent,
            uint TdhContextCount,
            IntPtr pTdhContext,
            IntPtr pBuffer,
            ref uint pBufferSize);

        /// <summary>
        /// Decodifica e formatta il valore di una singola proprietà ETW utilizzando il metadata 
        /// manifest-based contenuto in <c>TRACE_EVENT_INFO</c>.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Questa funzione rappresenta il meccanismo principale di parsing dinamico degli eventi 
        /// ETW manifest-based e consente di convertire una proprietà raw presente nel payload 
        /// (<paramref name="UserData"/>) in una rappresentazione testuale coerente con il tipo 
        /// definito dal provider.
        /// </para>
        /// <para>
        /// Il metodo utilizza il metadata TDH (<c>TRACE_EVENT_INFO</c>) e il payload runtime 
        /// (<paramref name="UserData"/>) per interpretare correttamente la proprietà in base a 
        /// <paramref name="PropertyInType"/> e <paramref name="PropertyOutType"/>.
        /// </para>
        /// <para><b>SEQUENTIAL DECODING:</b></para>
        /// <para>
        /// <c>TdhFormatProperty</c> consuma una porzione del payload indicata tramite il parametro 
        /// <paramref name="UserData"/> e restituisce il numero di byte utilizzati tramite 
        /// <paramref name="UserDataConsumed"/>.
        /// </para>
        /// <para>
        /// Questo consente di iterare sequenzialmente sulle proprietà e avanzare manualmente 
        /// l'offset nel payload secondo il pattern:
        /// </para>
        /// <code>
        /// offset += UserDataConsumed
        /// </code>
        /// <para>
        /// necessario per il parsing manifest-based in assenza di layout statico.
        /// </para>
        /// <para><b>MAP SUPPORT:</b></para>
        /// <para>
        /// Se <c>PropertyFlags</c> indica la presenza di mapping (<c>PropertyParamFixedMap</c>), 
        /// è possibile fornire <paramref name="pMapInfo"/> per convertire valori enumerati in stringhe. 
        /// In caso contrario deve essere impostato a <c>NULL</c>.
        /// </para>
        /// <para><b>BUFFER MANAGEMENT:</b></para>
        /// <para>
        /// Se il buffer fornito è insufficiente, la funzione restituisce <c>ERROR_INSUFFICIENT_BUFFER (122)</c>
        /// e aggiorna <paramref name="BufferSize"/> con la dimensione richiesta.
        /// Il chiamante deve quindi riallocare il buffer e reinvocare la funzione fino a ottenere 
        /// <c>ERROR_SUCCESS</c>.
        /// </para>
        /// <seealso href="https://learn.microsoft.com/it-it/windows/win32/api/tdh/nf-tdh-tdhformatproperty">Documentazione Microsoft: TdhFormatProperty</seealso>
        /// </remarks>
        /// <param name="pEventInfo">
        /// Puntatore al metadata <c>TRACE_EVENT_INFO</c> associato all'evento runtime.
        /// </param>
        /// <param name="pMapInfo">
        /// Puntatore opzionale a <c>EVENT_MAP_INFO</c> per la conversione di proprietà 
        /// enumerative. Deve essere <c>NULL</c> se non utilizzato.
        /// </param>
        /// <param name="PointerSize">
        /// Dimensione dei puntatori del processo chiamante (4 o 8 byte).
        /// Deve essere impostato a <c>(uint)IntPtr.Size</c>.
        /// </param>
        /// <param name="PropertyInType">
        /// Tipo runtime della proprietà (<c>TDH_INTYPE_*</c>).
        /// </param>
        /// <param name="PropertyOutType">
        /// Tipo di output desiderato (<c>TDH_OUTTYPE_*</c>).
        /// </param>
        /// <param name="PropertyLength">
        /// Lunghezza della proprietà in byte. Può essere zero se determinata 
        /// dinamicamente dal metadata.
        /// </param>
        /// <param name="UserDataLength">
        /// Numero di byte rimanenti nel payload dell'evento.
        /// </param>
        /// <param name="UserData">
        /// Puntatore alla posizione corrente nel payload ETW.
        /// </param>
        /// <param name="BufferSize">
        /// Dimensione del buffer di output (in caratteri Unicode).
        /// Viene aggiornato in caso di <c>ERROR_INSUFFICIENT_BUFFER</c>.
        /// </param>
        /// <param name="Buffer">
        /// Buffer di output per il valore formattato.
        /// </param>
        /// <param name="UserDataConsumed">
        /// Numero di byte del payload consumati dalla proprietà.
        /// Utilizzato per avanzare l'offset nel parsing sequenziale.
        /// </param>
        /// <returns>
        /// Codice di stato Win32:
        /// <list type="bullet">
        ///   <item><description><c>ERROR_SUCCESS (0)</c></description></item>
        ///   <item><description><c>ERROR_INSUFFICIENT_BUFFER (122)</c></description></item>
        ///   <item><description><c>ERROR_INVALID_PARAMETER (87)</c></description></item>
        ///   <item><description><c>ERROR_EVT_INVALID_EVENT_DATA (15001)</c></description></item>
        /// </list>
        /// </returns>
        [DllImport("tdh.dll", CharSet = CharSet.Unicode)]
        public static extern unsafe uint TdhFormatProperty(
            TRACE_EVENT_INFO* pEventInfo,
            void* pMapInfo,
            uint PointerSize,
            ushort PropertyInType,
            ushort PropertyOutType,
            ushort PropertyLength,
            ushort UserDataLength,
            byte* UserData,
            uint* BufferSize,
            char* Buffer,
            ushort* UserDataConsumed);

        /// <summary>
        /// Delegate di utilità per invocazioni native che restituiscono un buffer e una dimensione.
        /// </summary>
        /// <param name="buffer">Buffer unmanaged da allocare/riempire.</param>
        /// <param name="size">Dimensione richiesta o utilizzata.</param>
        /// <returns>Codice di errore Win32.</returns>
        internal unsafe delegate uint TdhNativeCall(IntPtr buffer, ref uint size);
    }
}
