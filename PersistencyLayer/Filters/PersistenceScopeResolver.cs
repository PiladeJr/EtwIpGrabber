namespace EtwIpGrabber.PersistencyLayer.Filters
{
    /// <summary>
    /// Classe di utilità per poter aggiungere degli argomenti di filtro alla creazione del servizio.
    /// </summary>
    /// <remarks>
    /// Permette di aggiungere l'argomento <c>--scope=</c><see cref="NetworkScopeFilters">[filtro]</see>
    /// per specificare su quale tabella effettuare la persistenza dei dati. Ad esempio,
    /// se voglio salvare solo le connessioni pubbliche, posso avviare il servizio con l'argomento <c>--scope=Public</c>.
    /// <para>
    /// <b>Nota: </b> il servizio si avvia con il filtro di default <see cref="NetworkScopeFilters.Private">Private</see>
    /// se non viene specificato alcun argomento o se l'argomento è malformato.
    /// </para>
    /// </remarks>
    internal static class PersistenceScopeResolver
    {
        public static NetworkScopeFilters Resolve(string[] args)
        {
            var arg = args.FirstOrDefault(a =>
                a.StartsWith("--scope=", StringComparison.OrdinalIgnoreCase));

            if (arg == null)
                return NetworkScopeFilters.Private; // default

            var value = arg.Split('=', 2)[1];

            if (Enum.TryParse<NetworkScopeFilters>(value, true, out var scope))
                return scope;

            return NetworkScopeFilters.Private;
        }
    }
}
