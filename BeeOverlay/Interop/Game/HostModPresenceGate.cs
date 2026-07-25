#nullable enable

extern alias LethalCompany;
using LethalCompany;
using LethalCompany::Unity.Netcode;

namespace BeeOverlay.Interop.Game;

internal enum HostPresenceRequestResult
{
    Stop,
    Sent,
}

/// <summary>
/// Tracks delayed, bounded host-presence requests for a lobby connection.
/// </summary>
internal sealed class HostModPresenceGate
{
    private const float RequestDelaySeconds = 3f;

    private HostModPresenceBehaviour? behaviour;

    public bool IsOverlayAllowed { get; private set; } = true;

    public void Attach(HUDManager hud)
    {
        behaviour = hud.GetComponent<HostModPresenceBehaviour>()
            ?? hud.gameObject.AddComponent<HostModPresenceBehaviour>();
        BeginHostPresenceCheck(behaviour);
    }

    public void BeginHostPresenceCheck(HostModPresenceBehaviour bridge)
    {
        behaviour = bridge;
        NetworkManager? network = NetworkManager.Singleton;
        if (network is null || !network.IsClient)
        {
            IsOverlayAllowed = true;
            return;
        }

        if (network.IsHost)
        {
            IsOverlayAllowed = true;
            return;
        }

        IsOverlayAllowed = false;
        bridge.ScheduleHostPresenceRequest(RequestDelaySeconds);
    }

    public HostPresenceRequestResult TryRequestHostPresence()
    {
        if (IsOverlayAllowed)
        {
            return HostPresenceRequestResult.Stop;
        }

        NetworkManager? network = NetworkManager.Singleton;
        if (network is null || !network.IsClient || network.IsHost)
        {
            return HostPresenceRequestResult.Stop;
        }

        if (behaviour is null)
        {
            return HostPresenceRequestResult.Stop;
        }

        behaviour.RequestHostPresenceServerRpc();
        return HostPresenceRequestResult.Sent;
    }

    public void ConfirmHostPresence()
    {
        NetworkManager? network = NetworkManager.Singleton;
        if (network is not null && network.IsClient && !network.IsHost)
        {
            IsOverlayAllowed = true;
        }
    }

    public void Reset()
    {
        IsOverlayAllowed = true;
    }
}
