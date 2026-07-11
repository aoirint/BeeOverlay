# RedLocustBees Implementation Analysis

## Target

- Game: Lethal Company v73
- Steam manifest ID: `1749099131234587692`

When the target version changes, update both identifiers and replace the
observations in this document rather than creating a version-specific copy.

## Scope

This document records observed implementation behavior of Lethal Company's
`RedLocustBees` class. It does not document BeeOverlay UI, visualization, or
architecture decisions.

## Confirmed observations

### `RedLocustBees` members

- `RedLocustBees.thisEnemyIndex`: stable per-bee tracking key.
- `RedLocustBees.hive`: current hive reference; the hive position is available
  from `RedLocustBees.hive.transform.position`.
- `RedLocustBees.eye`: origin used for sight checks.
- `RedLocustBees.defenseDistance`: distance used for hive-proximity checks.
- `RedLocustBees.lastKnownHivePosition`: remembered hive position used by
  missing-hive evaluation.
- `RedLocustBees.syncedLastKnownHivePosition`: private synchronization flag for
  `RedLocustBees.lastKnownHivePosition`.

### `RedLocustBees`: state 0 to state 1

#### `RedLocustBees.CheckLineOfSightForPlayer()`

`RedLocustBees.CheckLineOfSightForPlayer(360f, 16, 1)` checks whether the bee
can see the local player from `RedLocustBees.eye`. Its distance gate is 16
units.

#### `RedLocustBees.defenseDistance`

`RedLocustBees.defenseDistance` is compared with the distance between the hive
and the local player's body position. This check uses the body position rather
than the camera position.

### `RedLocustBees`: state 0 to state 2

#### `RedLocustBees.IsHiveMissing()` spatial gates

The following spatial gates were observed inside
`RedLocustBees.IsHiveMissing()`:

- A distance below 4 units from `RedLocustBees.eye` to
  `RedLocustBees.lastKnownHivePosition` enters the near-distance gate.
- A distance below 8 units with a clear linecast between
  `RedLocustBees.eye` and `RedLocustBees.lastKnownHivePosition` enters the
  line-of-sight gate.
- The spatial gates are not evaluated when
  `RedLocustBees.syncedLastKnownHivePosition` is false.

`RedLocustBees.IsHiveMissing()` also depends on hive state. These observations
describe its spatial gates only; they do not claim to enumerate every condition
that can cause a state transition.
