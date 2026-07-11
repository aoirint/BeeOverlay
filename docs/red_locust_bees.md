# RedLocustBees Class Analysis

This reference records observed members and state-transition conditions of Lethal Company's `RedLocustBees` class.

The target game version is Lethal Company v73.
When updating the supported game version, replace this file's analysis with findings for the new version instead of adding version-specific documents.

## Observed members

`RedLocustBees` is a game-supplied Lethal Company class that represents red locust bee behavior.
Related game classes can be added here in their own class-named subsections when they affect `RedLocustBees` behavior.

### `RedLocustBees`

- `thisEnemyIndex`: stable per-bee key used for tracking.
- `hive`: the current hive and its `transform.position`.
- `eye`: origin for sight checks; the bee's body position is used when it is unavailable.
- `defenseDistance`: distance used for the proximity check between the hive and local player.
- `lastKnownHivePosition`: remembered position used when evaluating a missing hive.
- `syncedLastKnownHivePosition`: private field read with Harmony's `AccessTools.FieldRefAccess` to determine whether the remembered position has synchronized.

If `syncedLastKnownHivePosition` cannot be read, treat its state as unknown rather than false.

## State transitions

### `RedLocustBees`: state 0 to state 1

#### `RedLocustBees.CheckLineOfSightForPlayer()`

`CheckLineOfSightForPlayer(360f, 16, 1)` checks whether the bee can see the local player from its eye.
Its distance gate is 16 units.

- Sight checks and distance calculations use the local player's actual position.

#### `RedLocustBees.defenseDistance`

Compare the distance between the local player's body and the hive with `RedLocustBees.defenseDistance`.
This check uses the player's body position, not the camera position.

### `RedLocustBees`: state 0 to state 2

The following observations cover the spatial gates related to `RedLocustBees.IsHiveMissing()`.
The actual transition also depends on game-side conditions, including hive state.

#### `RedLocustBees.IsHiveMissing()` spatial gates

Use `lastKnownHivePosition` as the reference point and check these conditions from the bee's eye:

- A distance below 4 units enters the near-distance gate.
- A distance below 8 units with a clear linecast enters the line-of-sight gate.
- Do not evaluate the condition when `syncedLastKnownHivePosition` is false.

`hive.isHeld` is not part of these spatial gates.
