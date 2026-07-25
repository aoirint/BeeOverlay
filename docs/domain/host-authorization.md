# Host authorization

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
the game's HUD object can therefore schedule delayed, bounded-frequency client
requests after its network spawn, receive a request on the server, and send a
targeted client response using the request's sender client ID. The server RPC
must permit a non-owning client when the client is only asking the host to
identify its installed mod.

`NetworkManager.Singleton.IsClient` identifies an active client connection and
`IsHost` identifies the listen-server host. `NetworkBehaviour.OnNetworkDespawn`
is the lifecycle boundary at which a bridge can clear connection-scoped state.

## Host-consent need

Client-side diagnostic capabilities can be used without changing game state.
When a host must be able to prevent their use in a lobby, an integration needs a
verifiable host authorization path. An absent authorization or a negative
authorization value must leave the client-side capability unavailable.

This requirement prevents a guest from self-authorizing a capability the host
has not allowed. It does not require a general authentication, anti-cheat,
version-negotiation, or mod-list protocol.

## Technical options

An integration can provide host authorization in several ways:

- A targeted custom RPC can carry one authorization value from the host to the
  requesting client. It requires compatible `NetworkBehaviour` code on both
  peers, but adds no general discovery protocol.
- A shared mod-list or version-negotiation protocol can establish installed
  packages and compatibility more broadly. The v81 GameLibs reference does not
  expose such a general protocol, so an integration would need to supply it.
- A client-only setting needs no network integration, but cannot establish or
  enforce host authorization.

These options describe available integration techniques. The product decision
about which one to use belongs to the architecture documentation.

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
