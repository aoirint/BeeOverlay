# RedLocustBees Implementation Analysis

## Target

- Game: Lethal Company v73
- Steam manifest ID: `1749099131234587692`

When the target version changes, update both identifiers and replace the
observations in this document rather than creating a version-specific copy.

## Scope

This document records observed implementation behavior of Lethal Company's
`RedLocustBees` class. It does not document UI, visualization, or architecture
decisions.

## Implementation reference

### `EnemyAI`

`RedLocustBees` inherits the following `EnemyAI` members.

#### Fields

| Member | C# type | Role in `RedLocustBees` behavior |
| --- | --- | --- |
| `thisEnemyIndex` | `int` | Stable per-bee tracking key. |
| `eye` | `Transform` | Origin used for sight checks. |

#### Methods

| Method | Return type | Parameters | Role |
| --- | --- | --- | --- |
| `CheckLineOfSightForPlayer` | `PlayerControllerB` | `float width`, `int range`, `int proximityAwareness` | Finds a player that satisfies the configured sight-check parameters for enemy AI behavior. |

### `RedLocustBees`

#### Fields

| Member | C# type | Role |
| --- | --- | --- |
| `hive` | `GrabbableObject` | Current hive reference; its position is available from `RedLocustBees.hive.transform.position`. |
| `defenseDistance` | `int` | Distance used for hive-proximity checks. |
| `lastKnownHivePosition` | `Vector3` | Remembered hive position used by missing-hive evaluation. |
| `syncedLastKnownHivePosition` | `bool` | Private synchronization flag for `RedLocustBees.lastKnownHivePosition`. |

#### Methods

| Method | Return type | Parameters | Role |
| --- | --- | --- | --- |
| `IsHiveMissing` | `bool` | None | Evaluates whether `RedLocustBees` considers its hive missing. |

## Behavior analysis

### `RedLocustBees`: state 0 to state 1

`RedLocustBees.CheckLineOfSightForPlayer(360f, 16, 1)` checks whether the bee
can see the local player from `EnemyAI.eye`. Its distance gate is 16 units.

`RedLocustBees.defenseDistance` is compared with the distance between the hive
and the local player's body position. This check uses the body position rather
than the camera position.

### `RedLocustBees`: state 0 to state 2

#### `RedLocustBees.IsHiveMissing()` spatial gates

The following spatial gates were observed inside
`RedLocustBees.IsHiveMissing()`:

- A distance below 4 units from `EnemyAI.eye` to
  `RedLocustBees.lastKnownHivePosition` enters the near-distance gate.
- A distance below 8 units with a clear linecast between
  `EnemyAI.eye` and `RedLocustBees.lastKnownHivePosition` enters the
  line-of-sight gate.
- The spatial gates are not evaluated when
  `RedLocustBees.syncedLastKnownHivePosition` is false.

`RedLocustBees.IsHiveMissing()` also depends on hive state. These observations
describe its spatial gates only; they do not claim to enumerate every condition
that can cause a state transition.

## Overlay domain model

BeeOverlay uses these observations to visualize state-0 spatial context. It
does not modify the game's AI or claim to reproduce every state-transition
condition.

### Spatial points

| Term | Source | Meaning in the overlay |
| --- | --- | --- |
| Bee eye | `EnemyAI.eye` | Origin for bee sight and remembered-hive probes. |
| Current hive | `RedLocustBees.hive.transform.position` | Current pickup position. |
| Remembered hive | `RedLocustBees.lastKnownHivePosition` | Position considered by missing-hive spatial gates. |
| Local player body | `PlayerControllerB` body position | Point used for hive-distance display. |

All displayed distances are Unity world units sampled during the current HUD
update. Marker offsets are visual-only and do not change the sampled point.

### Direct observations

- `bee-player` calls `CheckLineOfSightForPlayer(360f, 16, 1)` and reports the
  bee-eye-to-player-body distance.
- `hive-player` compares the local-player-body distance with
  `defenseDistance` around the current hive.
- `bee-knownHive` evaluates the documented spatial and synchronization gates
  for `IsHiveMissing()` against `lastKnownHivePosition`.

`bee-knownHive` is intentionally not a complete `IsHiveMissing()` result.
Hive state remains an additional game-side condition.

### Pickup-position proxy

`bee-hive` tests a clear linecast from the bee eye to the current hive under
the 16-unit sight range. It helps answer whether the hive point could stand in
for a player at that position. It is not a player-collider visibility result
and must not be presented as one.

### Display conventions

- `SEEN` means the corresponding direct visibility call or spatial probe is
  satisfied.
- `blocked` means the visibility or spatial condition is not satisfied.
- `INSIDE` and `outside` apply only to the player position relative to
  `defenseDistance`.
- Gray guides indicate inactive or blocked diagnostic output; they do not show
  that the game skipped the underlying code.

### Change checklist

Before changing a probe, confirm:

1. The target game version and manifest still match the implementation evidence.
2. The probe is labelled as a direct observation or a proxy.
3. A proxy cannot be read as a complete game-state decision.
4. Overlay geometry has no colliders and cannot affect physics or raycasts.
