using EtwIpGrabber.TcpLifeCycleReconstruction.Models;

namespace EtwIpGrabber.Utils.ConnectionExtendedInfo.LocalRemoteInversion;

/// <summary>
/// Normalizza gli endpoint di un TcpConnectionLifecycle prima della persistenza.
/// Corregge il caso in cui ETW emette l'evento lato server prima di quello lato client,
/// causando l'inversione di LocalIP/LocalPort con RemoteIP/RemotePort.
/// </summary>
/// <remarks>
/// <para>
/// Il trigger per lo swap è la presenza di RemoteIP nella lista degli indirizzi
/// locali della macchina. Se RemoteIP è un indirizzo locale, significa che
/// la tupla è stata catturata dal socket lato server e i ruoli sono invertiti.
/// </para>
/// <para>
/// Direction, CommunityId e tutti gli altri campi rimangono invariati:
/// Direction è già calcolata correttamente dal reconstructor indipendentemente
/// dall'ordine degli endpoint.
/// </para>
/// </remarks>
internal sealed class LifecycleEndpointNormalizer(LocalAddressCache localAddresses)
{
    /// <summary>
    /// Restituisce il lifecycle con endpoint corretti.
    /// Se non è necessario lo swap, restituisce l'istanza originale invariata (zero allocazioni).
    /// </summary>
    public TcpConnectionLifecycle Normalize(TcpConnectionLifecycle lifecycle)
    {
        if (!localAddresses.IsLocalAddress(lifecycle.RemoteIP))
            return lifecycle; // caso normale, nessuna inversione

        // Swap LocalIP <-> RemoteIP e LocalPort <-> RemotePort
        return new TcpConnectionLifecycle
        {
            ProcessId = lifecycle.ProcessId,
            ProcessName = lifecycle.ProcessName,
            LocalIP = lifecycle.RemoteIP,
            LocalPort = lifecycle.RemotePort,
            RemoteIP = lifecycle.LocalIP,
            RemotePort = lifecycle.LocalPort,
            Direction = lifecycle.Direction,
            Classification = lifecycle.Classification,
            StartAt = lifecycle.StartAt,
            EndAt = lifecycle.EndAt,
            Duration = lifecycle.Duration,
            Outcome = lifecycle.Outcome,
            Handshake = lifecycle.Handshake,
            CommunityId = lifecycle.CommunityId,
        };
    }
}