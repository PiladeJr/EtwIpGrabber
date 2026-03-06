namespace EtwIpGrabber.Utils.ConnectionExtendedInfo
{
    /// <summary>
    /// Classe di utilità per la classificazione delle reti, basata su indirizzi IP.
    /// </summary>
    /// <remarks>
    /// Si tratta di un elemento extra che può essere utile per l'analisi delle connessioni
    /// e per la fase di persistenza dei dati, consentendo di categorizzare le connessioni in
    /// <list type="bullet">
    ///     <item><description>Loopback</description></item>
    ///     <item><description>Multicast</description></item>
    ///     <item><description>BroadCast</description></item>
    ///     <item><description>Private</description></item>
    ///     <item><description>Public</description></item>
    ///     <item><description>Special</description></item>
    ///     <item><description>Unknown</description></item>
    /// </list>
    /// Posso poi filtrare in base alle categorie per decidere quali salvare su database
    /// e quali droppare (per esempio i loopback).
    /// </remarks>
    public static class NetworkClassification
    {
        public static NetworkScope Classify(uint ip)
        {
            byte a = (byte)(ip & 0xFF);
            byte b = (byte)((ip >> 8) & 0xFF);
            byte c = (byte)((ip >> 16) & 0xFF);
            byte d = (byte)((ip >> 24) & 0xFF);

            // 127.0.0.0/8
            if (a == 127)
                return NetworkScope.Loopback;

            // 224.0.0.0/4
            if (a >= 224 && a <= 239)
                return NetworkScope.Multicast;

            // 255.255.255.255
            if (ip == 0xFFFFFFFF)
                return NetworkScope.Broadcast;

            // 10.0.0.0/8 reti private di classe A
            if (a == 10)
                return NetworkScope.Private;

            // 172.16.0.0 – 172.31.255.255 reti private di classe B
            if (a == 172 && b >= 16 && b <= 31)
                return NetworkScope.Private;

            // 192.168.0.0/16 reti private di classe C
            if (a == 192 && b == 168)
                return NetworkScope.Private;

            // 0.0.0.0/8 indirizzi speciali
            if (a == 0 && b == 0 && c == 0 && d == 0)
                return NetworkScope.Special;

            return NetworkScope.Public;
        }
    }
}
