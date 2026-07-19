# Rendering Lifecycle

This document applies the reusable rendering practices in
[Diagnostic visualization](../domain/diagnostic-visualization.md) to
BeeOverlay's owned HUD and world-view objects.

## Ownership

`Overlay` owns its HUD root, status text, per-bee `BeeView` instances, and
the runtime materials used by those views. It does not own `HUDManager`, the
vanilla HUD container, or game-owned bee and hive objects.

A `BeeView` owns the visual objects for one `thisEnemyIndex`. It receives
world-guide values from `Overlay`; it does not independently query game state
while applying those values. HUD text and world guides consume the same
immutable Core frame.

## HUD replacement

The overlay attaches its root under `HUDManager.Instance.HUDContainer`. That
container can change across scene transitions. The current
`TryEnsureHudRoot()` compares its cached `attachedHudContainer` with the
current container on every tick:

1. If no container exists, hide all world views and wait.
2. If the cached container matches, reuse the existing HUD root.
3. If the container changed, destroy the old HUD root, clear the view
   dictionary, and create a new root and status text under the new container.

Rebuilding is intentional: retaining UI below an old canvas risks detached
status text and world guides that no longer have a matching HUD.

This is incomplete ownership cleanup. The current implementation neither
verifies that `hudRoot` is still parented to the current container nor disposes
the `DontDestroyOnLoad` world roots before clearing their references. Before
relying on HUD replacement, add an explicit `BeeView` disposal path and verify
the root's actual parent; then replace the current procedure with that lifecycle.

## Per-frame update and cleanup

`Overlay.Present()` rebuilds the complete status text from the current sorted
Core frame. It then hides every view whose key was not observed during that
update. The status text is a single rich-text block rather than one UI object
per row, so a despawned bee cannot leave a stale row behind.

`BeeObservationSource` captures the local player and each bee once per update.
Core derives distances and probe conditions from that observation, and
`Overlay.Present()` supplies the same `BeeDiagnostic` to status formatting and
world rendering. The presenter does not retain the frame after the update;
only the Unity view objects survive for reuse.

World guides are visual-only. Their colliders are removed, and they do not add
physics, raycast, or network components. This preserves the diagnostic role of
the overlay and avoids changing gameplay or other mods' nearby-object queries.
