using System.Runtime.InteropServices;

namespace EtwIpGrabber.TdhParsing.Metadata
{
    /// <summary>
    /// Handle RAII per buffer unmanaged contenenti informazioni <c>TRACE_EVENT_INFO</c>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Incapsula un buffer non gestito allocato tramite <see cref="Marshal.AllocHGlobal"/> 
    /// contenente:
    /// </para>
    /// <list type="bullet">
    ///   <item><description><see cref="Buffer"/>: puntatore al buffer unmanaged</description></item>
    ///   <item><description><see cref="Size"/>: dimensione in byte del buffer</description></item>
    /// </list>
    /// <para>
    /// Il buffer viene utilizzato nelle fasi successive di:
    /// </para>
    /// <list type="bullet">
    ///   <item><description>layout discovery</description></item>
    ///   <item><description>TDH property formatting</description></item>
    /// </list>
    /// <para>
    /// Il buffer deve essere trattato come immutabile e read-only.
    /// La memoria viene rilasciata automaticamente tramite <see cref="Marshal.FreeHGlobal"/> 
    /// alla chiamata di <see cref="Dispose"/>.
    /// </para>
    /// </remarks>
    public sealed class TraceEventInfoHandle(IntPtr buffer, uint size) : IDisposable
    {
        /// <summary>
        /// Puntatore al buffer unmanaged contenente <c>TRACE_EVENT_INFO</c>.
        /// </summary>
        public IntPtr Buffer { get; } = buffer;

        /// <summary>
        /// Dimensione in byte del buffer allocato.
        /// </summary>
        public uint Size { get; } = size;

        /// <summary>
        /// Rilascia il buffer unmanaged tramite <see cref="Marshal.FreeHGlobal"/>.
        /// </summary>
        public void Dispose()
        {
            if (Buffer != IntPtr.Zero)
            {
                Marshal.FreeHGlobal(Buffer);
            }
        }
    }
}
