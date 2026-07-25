# Bee AI Break

## Target

- Game: Lethal Company v81
- Steam manifest ID: `6423525044216269478`

## Overview

Bee AI Break is the community name for a glitch in which players move a hive
away from Circuit Bees without the bees recognizing that it is missing, which
can make hive collection safer.

When the glitch is active, the bees remain in state 0. State 0 does not enter
defensive state 1 merely because a player is near the bees: the bees must also
see a player inside the hive's defense radius.

The relevant base-game predicate is `RedLocustBees.IsHiveMissing()`. While it
returns `false`, the bee remains in state 0 unless another state-0 condition
moves it to defensive state 1.

This is a timing- and position-dependent behavior, not a guarantee that the
bee is harmless. State 0 still sends the bee toward its known-hive position,
and it can enter state 1 when it sees a player within the hive defense radius.

See [RedLocustBees implementation analysis](red-locust-bees.md) for the
broader class surface and state-machine context.

## Terms

- **Hive:** the current physical `RedLocustBees.hive` object.
- **Known hive:** `lastKnownHivePosition`, the position the bee uses to decide
  whether its hive is missing. It is distinct from the hive's current
  position.
- **Probe:** the distance and line-of-sight gate from the bee's `eye` to the
  known-hive position. The missing-hive predicate only evaluates the hive
  after this gate passes.

## State transition conditions

While in state 0, a bee evaluates the missing-hive condition before the
player-sight condition. Either condition can move it out of state 0.

### Hive defense

The bee enters defensive state 1 only when all of the following are true:

- The missing-hive predicate returned `false` for the same update.
- `CheckLineOfSightForPlayer(360f, 16, 1)` returns a player.
- That player's body is inside the hive's current `defenseDistance` radius.

The v81 prefab serializes `defenseDistance` as 10. The live field remains the
authoritative value because it is serialized game data.

## Missing-hive probe

The predicate first requires `syncedLastKnownHivePosition`. Until that flag is
true, it returns `false`.

Once synchronized, it evaluates the hive only when either condition is met:

- The bee's eye is less than 4 units from the known-hive position.
- The bee's eye is less than 8 units from the known-hive position and the
  linecast to that position is clear.

Therefore, a blocked linecast does not prevent the probe at less than 4 units.
At 4 to less than 8 units, a blocker prevents the probe; at 8 units or more,
the predicate does not probe the hive.

## Missing-hive result

After the probe passes, the predicate returns `true` when either condition is
met:

- The hive is held.
- The hive is more than 6 units from the known-hive position and is not both
  placed and visible to the bee.

A hive is placed and visible only when it is not held, is within 9 units of
the bee's eye, and has a clear linecast from that eye. When the predicate does
not find the hive missing, it updates the known-hive position to the hive's
position plus 0.5 units upward.

## Maintaining Bee AI Break

Keeping the bee in state 0 requires both transition conditions to remain
false:

- **Hive defense:** no player must satisfy both the 16-unit sight check and
  the hive-defense-radius check. When a player carries the hive, this normally
  means keeping that player outside the bee's sight range or blocking the
  bee-to-player sight line.
- **Missing hive:** the bee-to-known-hive probe must not pass. Keep the bee at
  least 4 units from the known-hive position; when it is less than 8 units
  away, the linecast to that position must be blocked.

The two conditions are evaluated independently. Preventing the missing-hive
probe alone does not prevent defensive state 1.

For practice, BeeOverlay's bee-to-known-hive probe line and its 4- and 8-unit
guides show the gate that must remain closed. Its bee-to-hive sight line and
known-hive marker show the separate current- and remembered-position checks.
The bee-to-player sight line and 16-unit sphere show the defensive sight
condition. The overlay observes these conditions; it does not change the bee's
state or the linecasts.
