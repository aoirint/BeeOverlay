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

| Decision | Options | Recommended approach | Why |
| --- | --- | --- | --- |
| Observe a behaviour transition | Patch `DoAIInterval()`; patch `IsHiveMissing()`; poll fields in a separate update | Patch `DoAIInterval()` when the transition and its resulting state matter. | It is the state-machine owner and establishes the order between the missing-hive test, sight test, and state change. |
| Observe only the missing-hive predicate | Patch `DoAIInterval()`; patch `IsHiveMissing()` | Patch `IsHiveMissing()`. | It isolates the predicate from unrelated state-0 and state-2 work, while retaining the exact private no-argument target. |
| Reproduce the missing-hive spatial test | Use bee root position; use camera position; use `EnemyAI.eye` | Use `EnemyAI.eye` and the documented `Physics.Linecast` mask. | The base predicate measures from `eye.position`; substituting another origin changes both distance gates and line-of-sight results. |
| Handle hive-position synchronization | Read `lastKnownHivePosition` alone; also gate on `syncedLastKnownHivePosition`; infer position from `hive.transform` | Keep the flag and position together. | The base predicate refuses to evaluate until synchronization completes, and the remembered position is intentionally distinct from the hive's current transform. |

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
