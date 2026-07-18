# RedLocustBees Implementation Analysis

## Target

- Game: Lethal Company v73
- Steam manifest ID: `1749099131234587692`

Use the members below as the version-specific Harmony or reflection targets.
Reconfirm their declarations when changing the target game version.

## Patch and access targets

### `RedLocustBees : EnemyAI`

| Member | Declaration | Role |
| --- | --- | --- |
| Hive reference | `public GrabbableObject hive` | Read the current hive and its `transform.position`; its held state is `hive.isHeld`. |
| Defence radius | `public int defenseDistance` | The player-to-hive radius used when entering and leaving defensive pursuit. |
| Remembered hive position | `public Vector3 lastKnownHivePosition` | The position against which missing-hive checks are made. |
| Position-sync flag | `private bool syncedLastKnownHivePosition` | Gate for `IsHiveMissing()`; access with non-public instance binding if required. |
| AI update | `public override void DoAIInterval()` | State-machine update containing the transitions below. |
| Missing-hive test | `private bool IsHiveMissing()` | Use `AccessTools.Method(typeof(RedLocustBees), "IsHiveMissing")` for a private-method patch. |
| Placed-hive test | `private bool IsHivePlacedAndInLOS()` | Companion predicate used by `IsHiveMissing()` and state 2. |
| Hive-position sync | `public void SyncLastKnownHivePositionServerRpc(Vector3 hivePosition)` | Sends the current remembered position before ownership changes. |
| Hive-position apply | `public void SyncLastKnownHivePositionClientRpc(Vector3 hivePosition)` | Assigns `lastKnownHivePosition` and sets the sync flag. |

### `EnemyAI`

| Member | Declaration | Role |
| --- | --- | --- |
| Sight origin | `public Transform eye` | Origin of bee line-of-sight and hive-distance tests. |
| Sight check | `public PlayerControllerB CheckLineOfSightForPlayer(float width = 45f, int range = 60, int proximityAwareness = -1)` | The state-0 defensive check calls it as `CheckLineOfSightForPlayer(360f, 16, 1)`. |

## Implementation choices

### Observe a behaviour transition

#### Patch `DoAIInterval()` — recommended when the transition and resulting state matter

`DoAIInterval()` owns the relevant state machine and establishes the order
between the missing-hive test, sight test, and state change. Patch it when the
transition itself is the subject of the implementation.

#### Patch `IsHiveMissing()`

This isolates the missing-hive predicate, but omits unrelated work in states 0
and 2. Choose it when the predicate rather than the state transition is the
subject of the implementation.

#### Poll fields in a separate update

This observes state after an unspecified point in the game update sequence.
It is unsuitable when the order of the base-game checks is significant.

### Reproduce the missing-hive spatial test

#### Use `EnemyAI.eye` — recommended

The base predicate measures both distance gates and linecasts from
`eye.position`. Use the documented linecast mask with this origin.

#### Use the bee root transform or camera position

Neither is the origin used by `IsHiveMissing()`; substituting either changes
the distance and line-of-sight result.

### Handle hive-position synchronization

#### Keep `lastKnownHivePosition` and `syncedLastKnownHivePosition` together — recommended

The base predicate refuses to evaluate until synchronization completes, and
the remembered position is intentionally distinct from the hive transform.

#### Infer the position from `hive.transform` alone

This loses both the synchronization gate and the remembered-position semantics,
so it cannot reproduce the base missing-hive decision.

## State and lifecycle

`RedLocustBees.DoAIInterval()` owns the relevant behaviour states.

- **State 0:** calls `IsHiveMissing()` first. If it returns false, the bee can enter state 1 when `CheckLineOfSightForPlayer(360f, 16, 1)` returns a player whose `transform.position` is less than `defenseDistance` from the hive.
- **State 1:** returns to state 0 when the target is invalid or more than `defenseDistance + 5f` from the hive; it enters state 2 when the target is holding `hive`.
- **State 2:** returns to state 0 when `IsHivePlacedAndInLOS()` succeeds and no player is within `defenseDistance`; otherwise it searches or pursues.

`IsHiveMissing()` returns false until `syncedLastKnownHivePosition` is true.
It evaluates the hive only when the distance from `eye.position` to
`lastKnownHivePosition` is below 4 units, or below 8 units with no `Physics.Linecast`
hit using `StartOfRound.Instance.collidersAndRoomMaskAndDefault`. Within that
gate it returns true when the hive is held, or when its position is more than
6 units from `lastKnownHivePosition` and `IsHivePlacedAndInLOS()` is false.

`IsHivePlacedAndInLOS()` returns false for a held hive, a hive more than 9
units from `eye.position`, or a blocked linecast using the same mask.

## Change checklist

1. Patch `DoAIInterval()` for state-transition timing; patch `IsHiveMissing()`
   only for the missing-hive predicate itself.
2. Keep the `Vector3` parameter on both hive-position RPC targets.
3. Treat `syncedLastKnownHivePosition` as a required precondition, not as a
   replacement for the remembered position.
4. Use `EnemyAI.eye`, not the camera or bee root transform, for the documented
   sight and distance checks.
