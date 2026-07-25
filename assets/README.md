# BeeOverlay

BeeOverlay visualizes RedLocustBees spatial checks.

## Compatibility

- Lethal Company v81 (2026-04-17 UTC, Manifest ID:
  `6423525044216269478`)
    - Analysis evidence
        - The target is verified from the supplied managed-code and asset
          exports. In-game HUD validation remains pending.
    - Package dependency
        - [BepInExPack][bepinexpack-package] v5.4.2305 (2026-03-17 UTC)

## What it does

- Displays a HUD summary for each `RedLocustBees` instance.
- Draws the bee, hive, remembered hive position, and local-player spatial
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
| `General.GuestEnabled` | `true` | When hosting, allows non-host players to use BeeOverlay. |
| `Overlay.Enabled` | `true` | Shows BeeOverlay elements when `General.Enabled` is `true`. |
| `Overlay.HudEnabled` | `true` | Shows the HUD status text. |
| `Overlay.BeeMarkerEnabled` | `true` | Shows bee markers. |
| `Overlay.HiveMarkerEnabled` | `true` | Shows hive markers. |
| `Overlay.KnownHiveMarkerEnabled` | `true` | Shows remembered-hive markers. |
| `Overlay.PlayerMarkerEnabled` | `true` | Shows local-player markers. |
| `Overlay.PlayerSightLineEnabled` | `true` | Shows bee-to-player sight lines. |
| `Overlay.BeeSightRangeSphereEnabled` | `true` | Shows bee 16-unit sight-range spheres. |
| `Overlay.HiveDefenseSphereEnabled` | `true` | Shows hive defense-range spheres. |
| `Overlay.KnownHiveNearSphereEnabled` | `true` | Shows remembered-hive 4-unit spheres. |
| `Overlay.KnownHiveLineOfSightSphereEnabled` | `true` | Shows remembered-hive 8-unit line-of-sight spheres. |
| `Overlay.KnownHiveProbeLineEnabled` | `true` | Shows bee-to-remembered-hive probe lines. |
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
