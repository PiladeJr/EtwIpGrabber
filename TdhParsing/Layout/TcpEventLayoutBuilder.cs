using EtwIpGrabber.TdhParsing.Layout.Struct;

namespace EtwIpGrabber.TdhParsing.Layout
{
    internal unsafe sealed class TcpEventLayoutBuilder(ILogger<TcpEventLayoutBuilder> logger)
    {
        private readonly ILogger<TcpEventLayoutBuilder> _logger = logger;
        public TcpEventLayout Build(IntPtr traceInfoBuffer)
        {
            var layout = new TcpEventLayout();

            var info = (TRACE_EVENT_INFO*)traceInfoBuffer;

            int headerSize = sizeof(TRACE_EVENT_INFO);
            int alignedHeaderSize = (headerSize + 7) & ~7;

            var propArray = (EVENT_PROPERTY_INFO*) ((byte*)traceInfoBuffer + alignedHeaderSize);

            for (int i = 0; i < info->PropertyCount; i++)
            {
                var prop = &propArray[i];

                var namePtr =
                    (char*)
                    ((byte*)traceInfoBuffer +
                     prop->NameOffset);

                var name =
                    new string(namePtr);

                _logger.LogDebug("TDH Property: {Name} - Index {Index}",name,i);

                BindProperty(
                    name,
                    i,
                    prop,
                    layout);
            }

            layout.Supported =
                layout.LocalAddressIndex >= 0 &&
                layout.RemoteAddressIndex >= 0 &&
                layout.LocalPortIndex >= 0 &&
                layout.RemotePortIndex >= 0;

            return layout;
        }

        private static unsafe void BindProperty(
            string name,
            int index,
            EVENT_PROPERTY_INFO* prop,
            TcpEventLayout layout)
        {
            switch (name)
            {
                case "LocalAddress":
                case "LocalAddr":
                case "saddr":
                    layout.LocalAddressIndex = index;
                    break;

                case "RemoteAddress":
                case "RemoteAddr":
                case "daddr":
                    layout.RemoteAddressIndex = index;
                    break;

                case "LocalPort":
                case "sport":
                    layout.LocalPortIndex = index;
                    break;

                case "RemotePort":
                case "dport":
                    layout.RemotePortIndex = index;
                    break;

                case "ProcessId":
                case "PID":
                case "pid":
                    layout.ProcessIdIndex = index;
                    break;

                case "AddressFamily":
                    layout.AddressFamilyIndex = index;
                    break;

                case "Direction":
                case "af":
                    layout.DirectionIndex = index;

                    if ((prop->Flags &
                         (uint)PropertyFlags
                             .PropertyParamFixedMap) != 0)
                    {
                        layout.DirectionHasMap = true;
                    }
                    break;

                case "TcpFlags":
                    layout.TcpFlagsIndex = index;
                    break;
            }
        }
    }
}