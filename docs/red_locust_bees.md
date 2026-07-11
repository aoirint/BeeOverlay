# RedLocustBees Class Analysis

`RedLocustBees` is a game-supplied Lethal Company class that represents red locust bee behavior.
This reference records observed members and state-transition conditions of that class.

The target game version is Lethal Company v73.
When updating the supported game version, replace this file's analysis with findings for the new version instead of adding version-specific documents.

## Subject and scope

- Subject: the `RedLocustBees` class in Lethal Company v73.
- Scope: observed members and spatial conditions for transitions from state 0 to states 1 and 2.

## Observed members

- `thisEnemyIndex`: stable per-bee key used for tracking.
- `hive`: the current hive and its `transform.position`.
- `eye`: origin for sight checks; the bee's body position is used when it is unavailable.
- `defenseDistance`: distance used for the proximity check between the hive and local player.
- `lastKnownHivePosition`: remembered position used when evaluating a missing hive.
- `syncedLastKnownHivePosition`: private field read with Harmony's `AccessTools.FieldRefAccess` to determine whether the remembered position has synchronized.

If `syncedLastKnownHivePosition` cannot be read, treat its state as unknown rather than false.

## State 0 to state 1 conditions

### Player line of sight

`CheckLineOfSightForPlayer(360f, 16, 1)` checks whether the bee can see the local player from its eye.
Its distance gate is 16 units.

- Sight checks and distance calculations use the local player's actual position.

### Hive proximity

Compare the distance between the local player's body and the hive with `RedLocustBees.defenseDistance`.
This check uses the player's body position, not the camera position.

## State 0 to state 2 conditions

The following observations cover the spatial gates related to `IsHiveMissing()`.
The actual transition also depends on game-side conditions, including hive state.

### `IsHiveMissing()` spatial gates

Use `lastKnownHivePosition` as the reference point and check these conditions from the bee's eye:

- A distance below 4 units enters the near-distance gate.
- A distance below 8 units with a clear linecast enters the line-of-sight gate.
- Do not evaluate the condition when `syncedLastKnownHivePosition` is false.

`hive.isHeld` is not part of these spatial gates.
