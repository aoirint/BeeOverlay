# Bee AI Break

## Target

- Game: Lethal Company v81
- Steam manifest ID: `6423525044216269478`

## Evidence

- Managed `RedLocustBees` decompilation SHA-256:
  `48f51b49dbb88e57e65f82366e32acecf4c0e1622839748e7d92bd6a470b4c52`.
- Exported `RedLocustBees` prefab SHA-256:
  `786d048138bccf552b2851bc278b9e97679a444b983323bb483ef007bf625fac`.

## Overview

Bee AI Break is the community name for a glitch in which players move a hive
away from Circuit Bees without the bees recognizing that it is missing. This
keeps the bees in state 0. State 0 does not enter defensive state 1 merely
because a player is near the bees: the bees must also see a player inside the
hive's defense radius.

The resulting separation can make hive collection safer.

The relevant base-game predicate is `RedLocustBees.IsHiveMissing()`. While it
returns `false`, the bee remains in state 0 unless another state-0 condition
moves it to defensive state 1.

This is a timing- and position-dependent behavior, not a guarantee that the
bee is harmless. State 0 still sends the bee toward its known-hive position,
and it can enter state 1 when it sees a player within the hive defense radius.

## Terms

- **Hive:** the current physical `RedLocustBees.hive` object.
- **Known hive:** `lastKnownHivePosition`, the position the bee uses to decide
  whether its hive is missing. It is distinct from the hive's current
  position.
- **Probe:** the distance and line-of-sight gate from the bee's `eye` to the
  known-hive position. The missing-hive predicate only evaluates the hive
  after this gate passes.

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

## State-0 consequences

State 0 checks the missing-hive predicate before its player-sight check. If
the predicate returns `true`, the bee enters state 2. Otherwise, it can enter
state 1 when it sees a player within 16 units and that player's body is inside
the hive's defense radius. The v81 prefab sets that radius to 10 units.

For practice, BeeOverlay's bee-to-known-hive probe line and its 4- and 8-unit
guides show the gate that must remain closed. Its bee-to-hive sight line and
known-hive marker show the separate current- and remembered-position checks.
The overlay observes these conditions; it does not change the bee's state or
the linecasts.
