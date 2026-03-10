using System.Collections.Frozen;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace EtwIpGrabber.Utils.ConnectionExtendedInfo.LocalRemoteInversion;

/// <summary>
/// Classe di utilità per effettuare il caching degli indirizzi IP locali della macchina.
/// </summary>
/// <remarks>
/// Effettua la cache degli indirizzi IPv4 locali della macchina corrente.
/// Enumerata una volta all'avvio tramite <see href="https://learn.microsoft.com/en-us/dotnet/api/system.net.networkinformation.networkinterface?view=net-10.0">NetworkInterface </see>,
/// copre tutte le interfacce attive: fisica, virtuale, VPN, loopback.
/// 
/// <para>
/// Utile esclusivamente per evitare di introdurre un grado di confusione all'agent
/// che consumerà il servizio a causa di eventi emessi da ETW che invertono indirizzi
/// locali e remoti (es. Nel caso di un DNS lookup, l'evento di risposta potrebbe mostrare
/// l'evento del socket server con l'indirizzo del client come "remoto" e l'indirizzo del
/// server DNS come "locale"). L'inversione non influisce in alcun modo con l'integrità dei dati
/// in quanto valori come outcome e direction sono calcolati a priore e in modo indipendente.
/// l'inversione avverrà solo al momento della persistenza di una connessione.
/// </para>
/// </remarks>
internal sealed class LocalAddressCache
{
    private readonly FrozenSet<uint> _localAddresses;

    public LocalAddressCache()
    {
        _localAddresses = BuildLocalAddressSet();
    }

    /// <summary>
    /// Restituisce true se l'indirizzo IP (host byte order uint)
    /// appartiene a un'interfaccia locale della macchina.
    /// </summary>
    public bool IsLocalAddress(uint ip) => _localAddresses.Contains(ip);

    private static FrozenSet<uint> BuildLocalAddressSet()
    {
        var addresses = new HashSet<uint>();

        foreach (var netInt in NetworkInterface.GetAllNetworkInterfaces())
        {
            // Considera solo interfacce operative
            if (netInt.OperationalStatus != OperationalStatus.Up)
                continue;

            foreach (var unicast in netInt.GetIPProperties().UnicastAddresses)
            {
                // Solo IPv4
                if (unicast.Address.AddressFamily != AddressFamily.InterNetwork)
                    continue;

                // Converti in uint host byte order (stesso formato usato da TcpConnectionLifecycle)
                var bytes = unicast.Address.GetAddressBytes(); // big-endian
                uint ip = ((uint)bytes[0])
                        | ((uint)bytes[1] << 8)
                        | ((uint)bytes[2] << 16)
                        | ((uint)bytes[3] << 24);

                addresses.Add(ip);
            }
        }

        return addresses.ToFrozenSet();
    }
}