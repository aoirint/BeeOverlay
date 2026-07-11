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

## Implementation surface

### `EnemyAI`

`RedLocustBees` inherits the following `EnemyAI` implementation surface.

#### Fields

| Member | C# type | Role in `RedLocustBees` behavior |
| --- | --- | --- |
| `EnemyAI.thisEnemyIndex` | `int` | Stable per-bee tracking key. |
| `EnemyAI.eye` | `Transform` | Origin used for sight checks. |

#### Methods

`RedLocustBees` inherits this method from `EnemyAI`:

```csharp
PlayerControllerB EnemyAI.CheckLineOfSightForPlayer(
    float width,
    int range,
    int proximityAwareness
)
```

Role: finds a player that satisfies the configured sight-check parameters for
enemy AI behavior.

### `RedLocustBees`

#### Fields

| Member | C# type | Role |
| --- | --- | --- |
| `RedLocustBees.hive` | `GrabbableObject` | Current hive reference; its position is available from `RedLocustBees.hive.transform.position`. |
| `RedLocustBees.defenseDistance` | `int` | Distance used for hive-proximity checks. |
| `RedLocustBees.lastKnownHivePosition` | `Vector3` | Remembered hive position used by missing-hive evaluation. |
| `RedLocustBees.syncedLastKnownHivePosition` | `bool` | Private synchronization flag for `RedLocustBees.lastKnownHivePosition`. |

#### Methods

```csharp
bool RedLocustBees.IsHiveMissing()
```

Role: evaluates whether `RedLocustBees` considers its hive missing.

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
