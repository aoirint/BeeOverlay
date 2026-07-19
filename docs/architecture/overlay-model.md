# Overlay Model

## Responsibility

`Plugin.Awake()` is the composition root: it creates one `Overlay` and installs
Harmony patches, but does not own diagnostic or rendering policy.
The `HUDManager.Update` postfix drives `Overlay.Tick()`.
Files under `BeeOverlay/Interop/` divide the implementation by boundary:

- `Overlay.cs` owns the HUD lifecycle, per-frame orchestration, and shared
  resources.
- `Overlay.Diagnostics.cs` samples base-game state and builds diagnostic
  values and status labels.
- `Rendering/Overlay.BeeView.cs` owns per-bee Unity rendering objects.
- `HudUpdatePatch.cs` is the thin Harmony callback that delegates to the
  overlay.

The partial `Overlay` files deliberately remain one lifecycle owner.
Separating them into independently mutable services would make it easier for
HUD text and world guides to observe different frame state without solving the
existing snapshot boundary described in
[Rendering lifecycle](rendering-lifecycle.md#per-frame-update-and-cleanup).
A separate Core module is not used because the current diagnostic model is
dominated by Unity vectors and base-game observations; introducing one would
add transport abstractions without isolating meaningful framework-free policy.

The game meanings of bee state, sight, and hive tests are defined in
[../domain/red-locust-bees.md](../domain/red-locust-bees.md). The HUD update
and world-rendering integration are defined in
[../domain/diagnostic-visualization.md](../domain/diagnostic-visualization.md).
This document defines only BeeOverlay's interpretation and presentation of
those values.

## Subjects and identity

The overlay enumerates `RedLocustBees`, sorts them by `thisEnemyIndex`, and
uses that value as the `BeeView` dictionary key. The HUD uses one-based
ordinals after sorting so that rows remain readable without making display
ordinals into persistent identities.

A bee without a readable hive has a status row but no spatial guides. A view
not seen during the current tick is hidden, preventing guides from a despawned
bee from remaining in the scene.

## Diagnostic model

| Label | Mod model | Boundary |
| --- | --- | --- |
| `bee-player` | Bee-eye to local-player observation. | Reports the base-game player sight result and player-body distance. |
| `hive-player` | Local-player distance from the current hive. | Reports whether the body position is inside `defenseDistance`. |
| `bee-hive` | Pickup-position proxy. | It is not a player-collider visibility result. |
| `bee-knownHive` | Remembered-hive spatial and synchronization probe. | It is not the complete `IsHiveMissing()` result. |

The overlay deliberately does not visualize `hive.isHeld`. Its purpose is to
make spatial conditions inspectable, not to restate every game-side branch.

## Presentation decisions

Entity colors identify the subject first: bee is yellow, hive is green,
remembered hive is blue, player is red, and the pickup proxy is white. Gray
indicates blocked or inactive guides. The HUD and world guides use the same
colors so a row can be matched to its geometry without a second legend.

The mod uses `SEEN` and `blocked` for the displayed sight or spatial
condition, and `INSIDE` / `outside` for the hive defence-radius comparison.
These are overlay labels, not names for base-game behaviour states.
