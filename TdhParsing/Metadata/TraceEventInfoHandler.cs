using System.Runtime.InteropServices;

namespace EtwIpGrabber.TdhParsing.Metadata
{
    public sealed class TraceEventInfoHandle(IntPtr buffer, uint size) : IDisposable
    {
        public IntPtr Buffer { get; } = buffer;
        public uint Size { get; } = size;

        public void Dispose()
        {
            if (Buffer != IntPtr.Zero)
            {
                Marshal.FreeHGlobal(Buffer);
            }
        }
    }
}
