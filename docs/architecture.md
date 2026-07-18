# BeeOverlay Architecture

BeeOverlay is a diagnostic mod that visualizes state 0 spatial conditions for
Lethal Company's `RedLocustBees` in the HUD and as 3D guides from the same
frame.
See [red-locust-bees.md](red-locust-bees.md) for analysis of the current game version.
See [diagnostic-visualization.md](diagnostic-visualization.md) for reusable HUD
and world-guide rendering practices.

## Components

- Entry point: `Plugin` in `BeeOverlay/Plugin.cs`.
- Update hook: Harmony postfix on `HUDManager.Update`.
- Display manager: `Overlay`.
- Per-bee world display: `BeeView`.

`Plugin.Awake()` creates the `Overlay` and applies the Harmony patches.
`Overlay.Tick()` prepares the HUD, enumerates bees, renders world guides, and
rebuilds the upper-left status every frame.

## HUD lifecycle

`Overlay` creates the upper-left status UI under `HUDManager.Instance.HUDContainer`.
If no HUD container exists, or its parent changes during a scene transition,
the overlay hides its old displays and recreates them.

- `RedLocustBees` are sorted by `thisEnemyIndex`.
- HUD `bee:*` labels use one-based ordinals after sorting.
- `BeeView` internal keys and logs use `thisEnemyIndex` for stable tracking.
- The status is rebuilt each frame rather than cached, so stale rows disappear
  when a bee despawns or has incomplete navigation data.

## 3D guides

For bees with a hive, the overlay renders only the spatial conditions relevant to retaining state 0.

| Subject | Display | Color |
| --- | --- | --- |
| Hive | `defenseDistance` wireframe sphere and marker | Green |
| `lastKnownHivePosition` | Marker, 4-unit sphere, 8-unit sphere, and line from the bee eye | Blue shades |
| Bee eye | Sight-range wireframe sphere and marker | Yellow |
| Local player | Sight line from the bee eye and marker | Red |
| Sight to current hive | Line from the bee eye to the hive | White; gray when the condition is not met |
| Inactive or blocked | Lines | Gray |

Markers are drawn slightly above their sampled positions so terrain, hive meshes, and bee bodies do not obscure them.
World-marker colliders are removed so the overlay cannot affect gameplay
physics, raycasts, or other mods that inspect nearby colliders.

### Rendering conventions

- The player-side endpoint of the rendered sight line is lowered by 0.35 units
  so a flickering red line is less likely to cross the center of the view.
- Each distance guide is a wireframe sphere made from an equator, two latitude
  rings, and three meridians. This preserves the former guide's approximate
  vertex count while making its three-dimensional shape easier to read.
- The `bee-hive` HUD label is a predictive pickup proxy, not a player-collider visibility check.
- `hive.isHeld` is intentionally not visualized. The overlay focuses on
  positions that could lead to state 2 if the hive is held.
- `SEEN` means that a target is visible or that the relevant distance and linecast conditions are satisfied.
- `blocked` means that a target is not visible, the linecast is obstructed, or the distance condition is not satisfied.
- `INSIDE` and `outside` indicate whether the local player is within `defenseDistance`.

## Upper-left status

The status uses this format:

```text
Bee Overlay | bees=2
bee:1  bee-player=6.20u/SEEN  hive-player=5.12u/INSIDE  bee-hive=7.10u/SEEN  bee-knownHive=3.82u/SEEN
bee:2  bee-player=10.85u/blocked  hive-player=8.34u/outside  bee-hive=9.44u/blocked  bee-knownHive=9.80u/blocked
```

- `bee-player`: distance from the bee eye to the local player's body and whether the bee sees the local player.
- `hive-player`: distance from the hive to the local player's body and whether it is within `defenseDistance`.
- `bee-hive`: distance from the bee eye to the current hive and whether it is below 16 units with a clear linecast.
- `bee-knownHive`: distance from the bee eye to `lastKnownHivePosition` and whether the linecast is clear.

HUD text colors match the world-display entity colors: red for `bee-player`,
green for `hive-player`, yellow for `bee:*`, white for `bee-hive`, and blue for
`bee-knownHive`.

## Logging policy

The mod does not emit regular heartbeat logs.
Distance, visibility, and state 0 retention information are consolidated in the HUD and 3D display.
When logs are needed, add temporary logging focused on a specific investigation target.

## Verification

Build with:

```powershell
dotnet build BeeOverlay.sln -c Release
```

In game, verify the following:

- `Bee Overlay | bees=N` appears in the upper-left corner.
- A green `defenseDistance` wireframe sphere appears around each hive.
- The bee-to-local-player line is red when the bee sees the player and gray otherwise.
- A green or gray line appears from the bee eye to the hive.
- A blue marker, intermediate 4-unit sphere, and lighter 8-unit sphere appear for `lastKnownHivePosition`.
- The line from the bee eye to `lastKnownHivePosition` is blue when the spatial gate for `IsHiveMissing()` could be satisfied.
