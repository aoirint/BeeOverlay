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

Install BeeOverlay on the lobby host and on every non-host client that should
see its diagnostic overlay. A non-host client waits for the host's BeeOverlay
presence response and keeps the overlay disabled if the host does not have the
mod installed.

## Configuration

BepInEx creates `BepInEx/config/com.aoirint.BeeOverlay.cfg` after the first
launch.

| Setting | Default | Behavior |
| --- | --- | --- |
| `General.Enabled` | `true` | Global switch for BeeOverlay. Set it to `false` to hide every overlay element without changing game behavior. |
| `General.HudEnabled` | `true` | Shows the HUD status text when the global switch is enabled. |
| `General.BeeMarkerEnabled` | `true` | Shows bee markers. |
| `General.HiveMarkerEnabled` | `true` | Shows hive markers. |
| `General.KnownHiveMarkerEnabled` | `true` | Shows remembered-hive markers. |
| `General.PlayerMarkerEnabled` | `true` | Shows local-player markers. |
| `General.PlayerSightLineEnabled` | `true` | Shows bee-to-player sight lines. |
| `General.BeeSightRangeSphereEnabled` | `true` | Shows bee 16-unit sight-range spheres. |
| `General.HiveDefenseSphereEnabled` | `true` | Shows hive defense-range spheres. |
| `General.KnownHiveNearSphereEnabled` | `true` | Shows remembered-hive 4-unit spheres. |
| `General.KnownHiveLineOfSightSphereEnabled` | `true` | Shows remembered-hive 8-unit line-of-sight spheres. |
| `General.KnownHiveProbeLineEnabled` | `true` | Shows bee-to-remembered-hive probe lines. |
| `General.HivePickupSightLineEnabled` | `true` | Shows bee-to-hive pickup-proxy sight lines. |

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
