#nullable enable

extern alias LethalCompany;

using LethalCompany::Unity.Netcode;

namespace BeeOverlay.Interop.Game;

/// <summary>
/// Netcode bridge for the host-presence handshake.
/// </summary>
internal sealed class HostModPresenceBehaviour : NetworkBehaviour
{
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
        Plugin.Controller.ResetHostModPresence();
        base.OnNetworkDespawn();
    }
}
