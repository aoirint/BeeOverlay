#nullable enable

extern alias LethalCompany;
using LethalCompany;
using LethalCompany::Unity.Netcode;

namespace BeeOverlay.Interop.Game;

/// <summary>
/// Tracks the one-time, delayed host-presence request for a lobby connection.
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

    public void RequestHostPresence()
    {
        NetworkManager? network = NetworkManager.Singleton;
        if (network is not null && network.IsClient && !network.IsHost)
        {
            behaviour?.RequestHostPresenceServerRpc();
        }
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
