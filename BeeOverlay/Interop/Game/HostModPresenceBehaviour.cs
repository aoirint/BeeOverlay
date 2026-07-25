#nullable enable

extern alias LethalCompany;
extern alias UnityEngine;

using System.Collections;
using LethalCompany::Unity.Netcode;
using UnityEngine::UnityEngine;

namespace BeeOverlay.Interop.Game;

/// <summary>
/// Netcode bridge for the host-presence handshake.
/// </summary>
internal sealed class HostModPresenceBehaviour : NetworkBehaviour
{
    private Coroutine? scheduledRequest;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        Plugin.Controller.BeginHostModPresenceCheck(this);
    }

    public void ScheduleHostPresenceRequest(float delaySeconds)
    {
        if (scheduledRequest is not null)
        {
            StopCoroutine(scheduledRequest);
        }

        scheduledRequest = StartCoroutine(RequestHostPresenceAfterDelay(delaySeconds));
    }

    [ServerRpc(RequireOwnership = false)]
    public void RequestHostPresenceServerRpc(ServerRpcParams parameters = default)
    {
        if (!IsServer)
        {
            return;
        }

        ConfirmHostPresenceClientRpc(new ClientRpcParams
        {
            Send = new ClientRpcSendParams
            {
                TargetClientIds = new[] { parameters.Receive.SenderClientId },
            },
        });
    }

    [ClientRpc]
    public void ConfirmHostPresenceClientRpc(ClientRpcParams parameters = default)
    {
        Plugin.Controller.ConfirmHostModPresence();
    }

    public override void OnNetworkDespawn()
    {
        if (scheduledRequest is not null)
        {
            StopCoroutine(scheduledRequest);
            scheduledRequest = null;
        }

        Plugin.Controller.ResetHostModPresence();
        base.OnNetworkDespawn();
    }

    private IEnumerator RequestHostPresenceAfterDelay(float delaySeconds)
    {
        yield return new WaitForSecondsRealtime(delaySeconds);
        scheduledRequest = null;
        Plugin.Controller.RequestHostModPresence();
    }
}
