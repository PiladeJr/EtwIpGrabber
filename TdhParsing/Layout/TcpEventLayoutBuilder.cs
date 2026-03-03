using EtwIpGrabber.TdhParsing.Layout.Struct;

namespace EtwIpGrabber.TdhParsing.Layout
{
    /// <summary>
    /// Costruisce il layout runtime di decoding
    /// a partire dal metadata TDH manifest-based.
    ///
    /// <para>
    /// Input:
    /// </para>
    /// <list type="bullet">
    /// <item>Buffer TRACE_EVENT_INFO</item>
    /// </list>
    ///
    /// <para>
    /// Output:
    /// </para>
    /// <list type="bullet">
    /// <item>TcpEventLayout</item>
    /// </list>
    ///
    /// <para>
    /// Durante la Build:
    /// </para>
    /// <list type="number">
    /// <item>Effettua pointer arithmetic su TRACE_EVENT_INFO</item>
    /// <item>Accede all'array EVENT_PROPERTY_INFO[]</item>
    /// <item>Itera sulle proprietà manifest</item>
    /// <item>Effettua binding runtime Name → Index</item>
    /// </list>
    ///
    /// <para>
    /// Il layout risultante rappresenta un <c> Decode Plan </c>
    /// Che verrà utilizzato dal SequentialTdhDecoder per:
    /// </para>
    /// <list type="bullet">
    /// <item>Avanzamento offset payload</item>
    /// <item>Estrazione proprietà IPv4</item>
    /// <item>Filtro AddressFamily</item>
    /// </list>
    ///
    /// <para><b> IMPORTANTE:</b></para>
    /// <list type="bullet">
    /// <item>TRACE_EVENT_INFO è seguito da EVENT_PROPERTY_INFO[]</item>
    /// <item>L'header deve essere allineato a 8 byte</item>
    /// <item>Errori di offset ⇒ decoding fallito</item>
    /// </list>
    ///
    /// </summary>
    internal unsafe sealed class TcpEventLayoutBuilder(ILogger<TcpEventLayoutBuilder> logger)
    {
        private readonly ILogger<TcpEventLayoutBuilder> _logger = logger;
        public TcpEventLayout Build(IntPtr traceInfoBuffer)
        {
            // Creazione layout vuoto
            var layout = new TcpEventLayout();
            // recupero del header TRACE_EVENT_INFO dal puntatore al buffer
            var info = (TRACE_EVENT_INFO*)traceInfoBuffer;
            // Calcolo dell'offset allineato per accedere all'array EVENT_PROPERTY_INFO[]
            int headerSize = sizeof(TRACE_EVENT_INFO);
            int alignedHeaderSize = (headerSize + 7) & ~7;
            // questa operazione è necessaria in quanto in TRACE_EVENT_INFO è presente un campo di dimensione variabile (EventPropertyInfoArray(ANYSIZE.ARRAY))
            // proprietà impossibile da rappresentare in struct C# a causa del vincolo di layout statico.
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

                //=========================================================================\\
                // log inserito per motivi di debug, utile a capire il nome delle proprietà\\
                // e il relativo indice runtime durante la fase di binding.                \\
                // Se il binding fallisce, questo log aiuta a identificare quali proprietà \\
                // non sono state mappate correttamente.                                   \\
                // in tal caso, è possibile aggiungerle allo switch di BindProperty        \\
                // per supportare nuove versioni del provider manifest.                    \\
                //=========================================================================\\
                // _logger.LogDebug("TDH Property: {Name} - Index {Index}",name,i)         \\

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
        /// <summary>
        /// Effettua il binding tra una proprietà manifest-based
        /// e il relativo indice runtime nel payload evento.
        ///
        /// <para>
        /// Per ogni proprietà descritta nel metadata
        /// (<see cref="EVENT_PROPERTY_INFO"/>),
        /// associa il nome runtime alla posizione
        /// sequenziale nel payload ETW (<c>UserData</c>).
        /// </para>
        ///
        /// <para>
        /// Il binding avviene tramite:
        /// </para>
        /// <list type="bullet">
        /// <item>Nome proprietà (manifest)</item>
        /// <item>Indice runtime nel payload</item>
        /// </list>
        ///
        /// <para>
        /// Il risultato viene salvato all'interno di <see cref="TcpEventLayout"/>
        /// Consentendo al SequentialTdhDecoder
        /// Di effettuare un Offset-aware Sequential Payload Walk
        /// </para>
        ///
        /// <para><b> IMPORTANTE: </b></para>
        /// Il nome della proprietà NON è stabile tra versioni
        /// Windows 10 / Windows 11.
        /// <para>
        /// Alias come:
        /// <code>
        /// saddr / LocalAddr
        /// sport / LocalPort
        /// pid   / ProcessId
        /// </code>
        /// Devono essere gestiti esplicitamente.
        /// </para>
        /// Questo rappresenta il principale meccanismo
        /// di compatibilità manifest-version-aware.
        ///
        /// <para>
        /// Per le proprietà mappate (es. Direction):
        /// </para>
        /// <list type="bullet">
        /// <item>
        /// Verifica la presenza del flag:
        /// <see cref="PropertyFlags.PropertyParamFixedMap"/>
        /// </item>
        /// <item>
        /// Indica che il valore runtime richiede
        /// lookup tramite EVENT_MAP_INFO
        /// durante la fase di decoding.
        /// </item>
        /// </list>
        ///
        /// <para>
        /// Un binding incompleto comporta:
        /// </para>
        /// <list type="bullet">
        /// <item>layout.Supported = false</item>
        /// <item>evento ignorato dal parser</item>
        /// </list>
        /// </summary>
        /// <param name="name">
        /// Nome manifest della proprietà runtime.
        /// </param>
        /// <param name="index">
        /// Posizione sequenziale nel payload ETW.
        /// </param>
        /// <param name="prop">
        /// Metadata TDH associato alla proprietà.
        /// </param>
        /// <param name="layout">
        /// Layout runtime da popolare.
        /// </param>
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