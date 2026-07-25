# BeeOverlay

BeeOverlay visualizes RedLocustBees spatial checks for practicing and
researching the Bee AI Break glitch.

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
| `Overlay.Enabled` | `true` | Shows BeeOverlay elements when `General.Enabled` is `true`. |
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

## Documentation

For implementation details, see the
[repository documentation](https://github.com/aoirint/BeeOverlay/tree/main/docs).

## AI Disclosure

Some parts of this project were developed with AI tools based on large language
models (LLMs), including agent-based tools. The project maintainer reviews the
code. This disclosure is made in compliance with Thunderstore and community
policies.

[bepinexpack-package]: https://thunderstore.io/c/lethal-company/p/BepInEx/BepInExPack/
