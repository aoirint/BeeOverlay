# Overlay Model

## Responsibility

`Plugin.Awake()` creates one `Overlay` and installs Harmony patches.
The `HUDManager.Update` postfix drives `Overlay.Tick()`. The overlay samples
the current bees, builds the HUD status, and updates the corresponding
`BeeView` world guides in the same tick.

The game meanings of bee state, sight, and hive tests are defined in
[../domain/red-locust-bees.md](../domain/red-locust-bees.md). This document
defines only BeeOverlay's interpretation and presentation of those values.

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

