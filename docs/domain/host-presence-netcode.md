# Host-presence Netcode bridge

## Scope

This document records the Unity Netcode integration constraints for Lethal
Company v81 used by a client-to-host presence handshake. It does not define
BeeOverlay's enablement policy; see
[the overlay model](../architecture/overlay-model.md) for that decision.

## Required integration shape

The compile-only `LethalCompany.GameLibs.Steam` v81.0.5 reference exposes
`NetworkBehaviour`, `ServerRpc`, `ClientRpc`, `ServerRpcParams`, and
`ClientRpcParams` from `Unity.Netcode`.

RPC methods must be declared on a `NetworkBehaviour`. A component attached to
the game's HUD object can therefore receive a client request on the server and
send a targeted client response using the request's sender client ID. The
server RPC must permit a non-owning client when the client is only asking the
host to identify its installed mod.

`NetworkManager.Singleton.IsClient` identifies an active client connection and
`IsHost` identifies the listen-server host. `NetworkBehaviour.OnNetworkDespawn`
is the lifecycle boundary at which a bridge can clear connection-scoped state.

## Evidence and limits

The members above are verified by the v81.0.5 compile-time GameLibs reference.
The dynamic-HUD bridge follows the same `NetworkBehaviour` surrogate pattern as
CruiserJumpPractice; its [client feature support issue][cruiser-client-support]
explains why a custom RPC is necessary when a client must ask the host for a
mod-specific result.
Runtime verification in a clean v81 host/client session remains pending.

This pattern only establishes that the responding host loaded compatible
BeeOverlay RPC code. It is not a general mod-list API and it does not prove an
exact package version.

[cruiser-client-support]: https://github.com/aoirint/CruiserJumpPractice/issues/1
