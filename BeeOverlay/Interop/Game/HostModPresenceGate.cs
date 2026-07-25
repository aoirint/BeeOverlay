#nullable enable

extern alias LethalCompany;
extern alias UnityEngine;

using System;
using LethalCompany;
using LethalCompany::Unity.Netcode;
using UnityEngine::UnityEngine;

namespace BeeOverlay.Interop.Game;

/// <summary>
/// Allows a non-host client to present the overlay only after the host answers
/// a BeeOverlay presence request.
/// </summary>
internal sealed class HostModPresenceGate
{
    private const float RequestIntervalSeconds = 2f;

    private HostModPresenceBehaviour? behaviour;
    private HUDManager? hudManager;
    private NetworkManager? confirmedNetwork;
    private float nextRequestTime;

    public void Attach(HUDManager hud)
    {
        if (ReferenceEquals(hud, hudManager))
        {
            return;
        }

        hudManager = hud;
        behaviour = hud.GetComponent<HostModPresenceBehaviour>()
            ?? hud.gameObject.AddComponent<HostModPresenceBehaviour>();
        Reset();
    }

    public bool IsHostPresent()
    {
        NetworkManager? network = NetworkManager.Singleton;
        if (network is null || !network.IsClient)
        {
            Reset();
            return true;
        }

        if (network.IsHost)
        {
            return true;
        }

        if (ReferenceEquals(network, confirmedNetwork))
        {
            return true;
        }

        if (behaviour is not null && Time.unscaledTime >= nextRequestTime)
        {
            nextRequestTime = Time.unscaledTime + RequestIntervalSeconds;
            behaviour.RequestHostPresenceServerRpc();
        }

        return false;
    }

    public void ConfirmHostPresence()
    {
        NetworkManager? network = NetworkManager.Singleton;
        if (network is not null && network.IsClient && !network.IsHost)
        {
            confirmedNetwork = network;
        }
    }

    public void Reset()
    {
        confirmedNetwork = null;
        nextRequestTime = 0f;
    }
}
