using EtwIpGrabber.EtwStructure.RealTimeConsumer.Native.Structures;
using EtwIpGrabber.TdhParsing.Layout.Struct;
using System.Runtime.InteropServices;

namespace EtwIpGrabber.TdhParsing.Metadata.Native
{
    internal static class TdhNativeMethods
    {
        /// <summary>
        /// Chiamata nativa all'api ETW di Windows TdhGetEventInformation. 
        /// </summary>
        /// <remarks>
        /// La funzione restituisce informazioni più dettagliate su un evento ETW specifico.
        /// In particolare restituisce le dimensioni del buffer necessario per contenere le informazioni dell'evento,
        /// e il buffer stesso.
        /// </remarks>
        /// <param name="pEvent">Il record dell'evento passato all'EventRecordCallback</param>
        /// <param name="TdhContextCount">il numero di elementi presenti nel context</param>
        /// <param name="pTdhContext">la matrice di valori di contesto. Qesto campo è inizializzato solo nei sistemi WPP o legacy. nel nostro caso verrà inizializzato a null</param>
        /// <param name="pBuffer">il Buffer allocato dall'utente per ricevere le informazioni sull'evento.</param>
        /// <param name="pBufferSize">Dimensioni, in byte, del buffer pBuffer . Se la funzione ha esito positivo, questo parametro riceve le dimensioni del buffer usato. Se il buffer è troppo piccolo, la funzione restituisce ERROR_INSUFFICIENT_BUFFER e imposta questo parametro sulla dimensione del buffer necessaria. Se la dimensione del buffer è zero in input, nessun dato viene restituito nel buffer e questo parametro riceve le dimensioni del buffer necessarie.</param>
        /// <returns>il codice di errore relativo alla chiamata della funzione:
        /// <list type="bullet">
        /// <item><description>ERROR_SUCCESS: Indica che la chiamata è andata a buon fine e restituisce buffer e dimensione</description></item>
        /// <item><description>ERROR_INSUFFICIENT_BUFFER: Indica che il buffer fornito è insufficiente. In questo caso, la funzione restituisce la dimensione necessaria del buffer attraverso il parametro pBufferSize.</description></item>
        /// </list>
        /// </returns>
        [DllImport("tdh.dll", CharSet = CharSet.Unicode)]
        public static extern unsafe uint TdhGetEventInformation(
            void* pEvent,
            uint TdhContextCount,
            IntPtr pTdhContext,
            IntPtr pBuffer,
            ref uint pBufferSize);

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

        internal unsafe delegate uint TdhNativeCall(
            IntPtr buffer,
            ref uint size);
    }
}
