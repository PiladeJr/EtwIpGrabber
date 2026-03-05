using EtwIpGrabber.EtwIntegration.SessionManager.Configuration;
using System.Runtime.InteropServices;

namespace EtwIpGrabber.EtwIntegration.SessionManager.Native
{
    /// <summary>
    /// Factory responsabile della creazione della struttura nativa
    /// <c>EVENT_TRACE_PROPERTIES</c> conforme ai requisiti ABI del sottosistema ETW.
    /// </summary>
    /// <remarks>
    /// ETW richiede che <c>EVENT_TRACE_PROPERTIES</c> venga passato a <c>StartTrace</c>/<c>ControlTrace</c>
    /// come un singolo blocco di memoria contiguo con il layout:
    /// <code>
    /// [ EVENT_TRACE_PROPERTIES ][ wchar LoggerName[] ]
    /// </code>
    /// Il marshalling managed standard non è sufficiente perché:
    /// <list type="bullet">
    ///   <item><description>il CLR non garantisce layout contiguo per struct + stringhe;</description></item>
    ///   <item><description>ETW si aspetta che <c>LoggerNameOffset</c> punti a una <c>wchar*</c> interna allo stesso buffer;</description></item>
    ///   <item><description>un layout errato causa <c>ERROR_INVALID_PARAMETER</c> o attach failure.</description></item>
    /// </list>
    /// Questo metodo:
    /// <list type="bullet">
    ///   <item><description>alloca manualmente il blocco nativo;</description></item>
    ///   <item><description>azzera la memoria;</description></item>
    ///   <item><description>inizializza in-place <c>EVENT_TRACE_PROPERTIES</c>;</description></item>
    ///   <item><description>copia il nome della sessione immediatamente dopo la struct.</description></item>
    /// </list>
    /// Il buffer restituito deve essere liberato dal chiamante con <c>Marshal.FreeHGlobal()</c>
    /// dopo <c>StartTrace()</c> o <c>ControlTrace()</c>.
    /// Il mancato rilascio causa memory leak nel processo del servizio.
    /// <br/>
    /// Il valore di <c>Wnode.Flags</c> viene impostato a
    /// WNODE_FLAG_TRACED_GUID (0x00020000),
    /// necessario affinché ETW interpreti il buffer come
    /// tracing session properties e non come MOF registration.
    /// 
    /// Un valore errato causa StartTrace failure con
    /// ERROR_INVALID_PARAMETER.
    /// </remarks>
    public sealed class EtwSessionPropertiesFactory
    {
        /// <summary>
        /// Crea un buffer nativo contenente <c>EVENT_TRACE_PROPERTIES</c> e il nome della sessione.
        /// </summary>
        /// <remarks>
        /// Questo buffer verrà passato a:
        /// <list type="bullet">
        ///   <item><description><c>StartTrace()</c></description></item>
        ///   <item><description><c>ControlTrace()</c></description></item>
        /// </list>
        /// La proprietà <c>Wnode.ClientContext</c> viene impostata a <c>1</c> per richiedere timestamp
        /// basati su QueryPerformanceCounter (QPC) invece di SystemTime, necessari per precisione
        /// sub-millisecondo nella ricostruzione del lifecycle TCP.
        /// </remarks>
        /// <param name="config">Configurazione immutabile della sessione ETW.</param>
        /// <returns>
        /// Puntatore a un buffer unmanaged contenente:
        /// <c>[EVENT_TRACE_PROPERTIES][SessionName]</c>
        /// </returns>
        public unsafe IntPtr Create(IEtwSessionConfig config)
        {
            // Dimensione della struct nativa EVENT_TRACE_PROPERTIES
            int propsSize = sizeof(EVENT_TRACE_PROPERTIES);

            // Dimensione del nome della sessione in UTF-16 (+1 per null terminator)
            int nameSize = (config.SessionName.Length + 1) * sizeof(char);

            // Dimensione totale del buffer contiguo richiesto da ETW
            int totalSize = propsSize + nameSize;

            // Allocazione memoria unmanaged
            IntPtr buffer = Marshal.AllocHGlobal(totalSize);

            // Azzera completamente il blocco per evitare valori garbage
            new Span<byte>((void*)buffer, totalSize).Clear();

            // Reinterpretazione del buffer come EVENT_TRACE_PROPERTIES*
            var props = (EVENT_TRACE_PROPERTIES*)buffer;

            // ETW richiede che Wnode.BufferSize rappresenti la dimensione totale
            // dell'intero blocco contiguo
            props->Wnode.BufferSize = (uint)totalSize;

            // WNODE_FLAG_TRACED_GUID indica una sessione di tracing attiva
            props->Wnode.Flags = 0x00020000;

            // ClientContext = 1 → Timestamp basati su QueryPerformanceCounter (QPC)
            // Necessario per ordering accurato degli eventi TCP
            props->Wnode.ClientContext = 1;

            // Parametri configurabili della sessione ETW
            props->LogFileMode = config.LogFileMode;
            props->FlushTimer = config.FlushTimerSeconds;
            props->MinimumBuffers = config.MinimumBuffers;
            props->MaximumBuffers = config.MaximumBuffers;
            props->BufferSize = config.BufferSizeKb;

            // Offset (in byte) dove verrà scritto il LoggerName
            props->LoggerNameOffset = (uint)propsSize;

            // Puntatore alla zona del buffer dove scrivere il nome della sessione
            char* namePtr = (char*)((byte*)buffer + props->LoggerNameOffset);

            // Copia della stringa UTF-16 nel buffer unmanaged
            fixed (char* src = config.SessionName)
            {
                Buffer.MemoryCopy(
                    src,
                    namePtr,
                    nameSize,
                    config.SessionName.Length * sizeof(char));
            }

            // Null terminator richiesto da ETW
            namePtr[config.SessionName.Length] = '\0';

            return buffer;
        }
    }
}

