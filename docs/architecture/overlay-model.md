# Overlay Model

## Responsibility

`Plugin.Awake()` constructs `PluginController` before installing Harmony
patches. `PluginController` is the composition root: it connects the Core frame
handler to the game observation source and Unity presenter, and binds the
`General.Enabled` BepInEx setting. The
`HUDManager.Update` postfix delegates one update to that controller and guards
the game callback from observation, presentation, and logging failures.

The implementation has two dependency boundaries:

- `Core/` owns plain observations, derived diagnostic models, thresholds, the
  frame-building use case, the frame handler, and its observation and
  presentation ports. It has no BepInEx, Harmony, Unity, or Lethal Company
  references.
- `Interop/` owns live `RedLocustBees` and player sampling, private-field and
  physics access, HUD lifecycle, status formatting, Unity rendering objects,
  and the Harmony callback.

`Interop/Game/BeeObservationSource.cs` converts live objects and Unity vectors
to one `OverlayFrameObservation`. `BuildOverlayFrameUseCase` sorts subjects and
derives distances and diagnostic conditions. `Overlay` then presents the
resulting immutable `OverlayFrame`; neither the HUD path nor `BeeView` reads
game state.

The frame is transient rather than stored across updates. BeeOverlay does not
compare frames, smooth values, or retain diagnostic history. The only retained
Core presentation state is the selected world-guide identity, which exists
because target cycling is an explicit cross-frame rule; the observation and
derived frame remain transient.

## Enablement

`General.Enabled` defaults to `true` and controls global overlay operation; it
does not unload the plugin.
`General.AllowGuestEnabled` defaults to `true`; when the local player hosts, it
authorizes non-host players to use BeeOverlay. `Overlay.Enabled` controls local
presentation independently from `General.Enabled`. `Overlay.HudEnabled` selects
HUD text. The remaining `Overlay.*` world-guide settings independently select every marker,
Sight line, and Sphere: bee, hive, known-hive, and player markers;
bee-to-player, bee-to-known-hive, and bee-to-hive pickup-proxy lines; and the
bee sight-range, hive defense-range, known-hive near, and known-hive
line-of-sight spheres. Every setting defaults to `true`.
`PluginController` reads the live BepInEx values for every HUD callback and
passes plain values into Core. When the global or overlay switch is `false`, or
all selected presentation elements are `false`, the frame handler hides all mod-owned
presentation and does not capture game state or build a frame. Otherwise, the
selected elements update from the same derived frame. This makes a
configuration-API change effective on the next HUD update while keeping BepInEx
types outside Core. Direct edits to the generated configuration file are not
watched.

## Host presence

The host always presents its own overlay. A non-host client is fail-closed: it
does not capture observations or present mod-owned HUD or world guides until
the host answers BeeOverlay's presence request. A missing answer, including a
host that has not installed BeeOverlay, therefore leaves the overlay hidden.

`HudUpdatePatch` attaches the Interop-only `HostModPresenceBehaviour` to the
HUD object. When the local network client spawns that bridge, it waits three
seconds, then retries at five-second intervals until it receives a response or
has sent three requests. The initial delay avoids the immediate lobby-entry
period, when the overlay has no planet-side use, without coupling the retry
schedule to the lever-triggered landing and dungeon-generation work. The HUD
update reads only the cached Boolean; it does not poll or retry the network
request every frame.

A BeeOverlay host answers only the requesting client with a client RPC that
includes its current `General.AllowGuestEnabled` authorization. `HostModPresenceGate`
records that answer for the active network connection.
The bridge clears the confirmation when its network object despawns. Core
receives only the resulting enablement Boolean and continues to have no Unity
or Netcode dependency.

BeeOverlay chooses the targeted custom-RPC option from
[the host authorization domain knowledge](../domain/host-authorization.md).
It gives the host an explicit `General.AllowGuestEnabled` decision without a general
mod-list protocol, which the pinned GameLibs reference does not provide. A
client-only setting is rejected because it would let a guest enable diagnostic
features without host authorization. This decision treats the host's installed
configuration as the consent signal that prevents that abuse.

This is a presence check, not an exact-version negotiation. Its external
Netcode assumptions and evidence are documented in
[../domain/host-authorization.md](../domain/host-authorization.md).

The game meanings of bee state, sight, and hive tests are defined in
[../domain/red-locust-bees.md](../domain/red-locust-bees.md). The HUD update
and world-rendering integration are defined in
[../domain/diagnostic-visualization.md](../domain/diagnostic-visualization.md).
This document defines only BeeOverlay's interpretation and presentation of
those values.

## Subjects and identity

The observation source enumerates `RedLocustBees` and captures
`thisEnemyIndex` as the stable subject identity. Core sorts observations by
that identity. The presenter uses it as the `BeeView` dictionary key, while
the HUD uses one-based ordinals after sorting so that rows remain readable
without making display ordinals into persistent identities.

A bee without a readable hive remains in the Core frame and, when selected, has
a status row but no spatial guides. A view not seen during the current update
is hidden, preventing guides from a despawned bee from remaining in the scene.

## World-guide selection

The HUD and world markers present at most one selected bee. The HUD is hidden
while no bee is selected, keeping it aligned with the one set of scene guides
shown beside other practice tools.

Selection starts empty. The InputUtils **Select Next Bee** action, bound to `B`
by default, advances through the current sorted frame and then returns to no
selection, hiding the HUD when no target remains. `WorldGuideSelection` retains
the stable `thisEnemyIndex` rather than the display ordinal, so inserting
another bee does not silently retarget existing guides.

When the action is pressed while the frame has no bees, Core keeps selection
empty and emits a `No bee found to select.` HUD Tip. If the retained identity
is absent from a later frame, Core returns to empty instead of choosing another
bee and emits a `Selected bee was removed.` HUD Tip. Core selects those fixed
messages; Interop is responsible for calling `HUDManager.DisplayTip`, so Core
continues to have no game or Unity dependency.

The external action-registration and dependency contract is documented in
[InputUtils integration](../domain/input-utils.md). Interop owns the
`LcInputActions` subclass and converts its nullable one-frame trigger into the
Core `IOverlayInput` port. Core owns selection order and lifetime.

## Frame model

`OverlayFrameObservation` is the direct-observation mirror for one update. It
contains plain vectors and values, keeps the player's body and sight-target
positions distinct, represents an absent hive explicitly, and preserves the
difference between an unreadable private sync field and a game value of
`false`.

`OverlayFrame` combines each observation with values derived by Core. Both the
HUD and world-guide presenter receive the same derived object. This deliberately
changes the old implementation, which called sight and physics queries once
for HUD text and again for world guides; a changing game state can no longer
make those two presentations disagree within one update.

## Diagnostic model

| Label | Mod model | Boundary |
| --- | --- | --- |
| `bee-player` | Bee-eye to local-player observation. | Reports the base-game player sight result and player-body distance. |
| `hive-player` | Local-player distance from the current hive. | Reports whether the body position is inside `defenseDistance`. |
| `bee-hive` | Pickup-position proxy. | It is not a player-collider visibility result. |
| `bee-knownHive` | Known-hive spatial and synchronization probe. | It is not the complete `IsHiveMissing()` result. |

The overlay deliberately does not visualize `hive.isHeld`. Its purpose is to
make spatial conditions inspectable, not to restate every game-side branch.

## Presentation decisions

Entity colors identify the subject first: bee is yellow, hive is green,
known hive is blue, player is red, and the pickup proxy is white. Gray
indicates blocked or inactive guides. The HUD and world guides use the same
colors so a row can be matched to its geometry without a second legend.

The mod uses `SEEN` and `blocked` for the displayed sight or spatial
condition, and `INSIDE` / `outside` for the hive defence-radius comparison.
These are overlay labels, not names for base-game behaviour states.
