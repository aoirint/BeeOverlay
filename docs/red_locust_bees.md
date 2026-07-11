# RedLocustBees Analysis Notes

This developer reference documents the state 0 retention and transition conditions of `RedLocustBees` visualized by BeeOverlay.
The current target game version is Lethal Company v73.
When updating the supported game version, replace this file's analysis with findings for the new version instead of adding version-specific documents.
See [architecture.md](architecture.md) for the implementation and visualization design.

## Scope and goal

- Target: `RedLocustBees` in the current Lethal Company v73 release line.
- Goal: identify locations that keep bees from leaving state 0.
- Scope: spatial conditions for transitions from state 0 to states 1 and 2.

## Game data observed

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
- The hive pickup proxy treats a player picking up the hive as `player ≒ hive`; it does not test the player's collider itself.

### Hive proximity

Compare the distance between the local player's body and the hive with `RedLocustBees.defenseDistance`.
This check uses the player's body position, not the camera position.

### Hive pickup proxy

Check the linecast and distance from the bee's eye to the current hive position.

- When the distance is below 16 units and the linecast is clear, the bee is likely able to see a player picking up the hive at that position.
- When the linecast is blocked or the distance is at least 16 units, this spatial condition is not met.

## State 0 to state 2 conditions

Because avoiding the state 0 to state 1 transition is the primary goal, only the spatial gates related to `IsHiveMissing()` are considered for state 2.
The actual transition still depends on game-side conditions, including hive state.

### `IsHiveMissing()` spatial gates

Use `lastKnownHivePosition` as the reference point and check these conditions from the bee's eye:

- A distance below 4 units enters the near-distance gate.
- A distance below 8 units with a clear linecast enters the line-of-sight gate.
- Do not evaluate the condition when `syncedLastKnownHivePosition` is false.

This analysis excludes `hive.isHeld`.
Its purpose is to identify positions that could lead to state 2 assuming the hive is held.
