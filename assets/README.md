# BeeOverlay

BeeOverlay visualizes RedLocustBees spatial checks for practicing the Bee AI
Break glitch.

## Compatibility

- Lethal Company v81 (2026-04-17 UTC, Manifest ID:
  `6423525044216269478`)
    - Test environment
        - [BepInExPack][bepinexpack-package] v5.4.2305 (2026-03-17 UTC)

## Screenshots

<details>
<summary>Overview: Bee, hive, player, and known-hive spatial checks</summary>

![BeeOverlay visualizing bee, hive, player, and known-hive spatial checks](https://raw.githubusercontent.com/aoirint/BeeOverlay/main/docs/screenshots/beeoverlay_usage_001.webp)

</details>

<details>
<summary>Use case: Blocked bee-to-hive and bee-to-known-hive Sight checks</summary>

![BeeOverlay showing blocked bee-to-hive and bee-to-known-hive Sight checks](https://raw.githubusercontent.com/aoirint/BeeOverlay/main/docs/screenshots/beeoverlay_usage_002.webp)

</details>

## Bee AI Break glitch

Bee AI Break is the community name for a glitch in which players move a hive
away from Circuit Bees without the bees recognizing that it is missing, which
can make hive collection safer.

When the glitch is active, the bees stay calm. They do not become defensive
merely because a player is nearby; they must also see that player inside the
hive's defense radius.

While a Circuit Bee is near its hive, it leaves its calm behavior when either
condition is met:

- **Hive defense.**
    - The bee sees a player within 16 units.
    - The player is within 10 units of the hive.
- **Missing hive.**
    - The bee-to-known-hive probe reaches the known-hive position:
        - The bee is less than 4 units away.
        - The bee is less than 8 units away with a clear probe line.

For Bee AI Break to work, both conditions must remain false while the hive is
carried away. NavMesh boundaries or terrain features can sometimes make these
conditions possible.

While carrying the hive away:

- Keep the player carrying the hive outside the bee's 16-unit sight range or
  block the bee-to-player sight line.
- Do not hold the hive while the bee is less than 4 units from its known-hive
  position.
- When the bee is 4 to less than 8 units from its known-hive position, block
  the probe line with solid cover.
- When the bee is 8 units or more from its known-hive position, it does not
  notice that a held hive is missing, but the 16-unit sight condition must
  still remain false.

BeeOverlay shows the relevant ranges, marker, and probe line without changing
game behavior. See the
[Bee AI Break documentation](https://github.com/aoirint/BeeOverlay/blob/main/docs/domain/bee-ai-break.md)
for technical details.

## What it does

- Displays a HUD summary for each `RedLocustBees` instance.
- Draws the bee, hive, known-hive position, and local-player spatial
  guides.
- Shows relevant sight, distance, and line-of-sight conditions without changing
  game behavior.

## Who needs to install

Lobby hosts must install BeeOverlay. This prevents non-host players from using
BeeOverlay's diagnostic features in a lobby without the host's knowledge.

Non-host players only need to install BeeOverlay when they want to use its
diagnostic overlay. Their overlay remains disabled unless the lobby host has
also installed BeeOverlay.

## Configuration

BepInEx creates `BepInEx/config/com.aoirint.BeeOverlay.cfg` after the first
launch.

| Setting | Default | Behavior |
| --- | --- | --- |
| `General.Enabled` | `true` | Set to `false` to disable all BeeOverlay functionality. |
| `General.AllowGuestEnabled` | `true` | When hosting, allows non-host players to use BeeOverlay. |
| `Overlay.Enabled` | `true` | Shows BeeOverlay elements. |
| `Overlay.HudEnabled` | `true` | Shows the HUD status text. |
| `Overlay.BeeMarkerEnabled` | `true` | Shows bee markers. |
| `Overlay.HiveMarkerEnabled` | `true` | Shows hive markers. |
| `Overlay.KnownHiveMarkerEnabled` | `true` | Shows known-hive markers. |
| `Overlay.PlayerMarkerEnabled` | `true` | Shows local-player markers. |
| `Overlay.PlayerSightLineEnabled` | `true` | Shows bee-to-player sight lines. |
| `Overlay.BeeSightRangeSphereEnabled` | `true` | Shows bee 16-unit sight-range spheres. |
| `Overlay.HiveDefenseSphereEnabled` | `true` | Shows hive defense-range spheres. |
| `Overlay.KnownHiveNearSphereEnabled` | `true` | Shows known-hive 4-unit spheres. |
| `Overlay.KnownHiveLineOfSightSphereEnabled` | `true` | Shows known-hive 8-unit line-of-sight spheres. |
| `Overlay.KnownHiveProbeLineEnabled` | `true` | Shows bee-to-known-hive probe lines. |
| `Overlay.HivePickupSightLineEnabled` | `true` | Shows bee-to-hive pickup-proxy sight lines. |

Changes made through a BepInEx configuration UI apply on the next HUD update.
BeeOverlay does not watch direct edits to the generated configuration file.

## AI Disclosure

Some parts of this project were developed with AI tools based on large language
models (LLMs), including agent-based tools. The project maintainer reviews the
code. This disclosure is made in compliance with Thunderstore and community
policies.

[bepinexpack-package]: https://thunderstore.io/c/lethal-company/p/BepInEx/BepInExPack/
