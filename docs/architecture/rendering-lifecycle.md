# Rendering Lifecycle

This document applies the reusable rendering practices in
[Diagnostic visualization](../domain/diagnostic-visualization.md) to
BeeOverlay's owned HUD and world-view objects.

## Ownership

`Overlay` owns its HUD root, status text, per-bee `BeeView` instances, and
the runtime materials used by those views. It does not own `HUDManager`, the
vanilla HUD container, or game-owned bee and hive objects.

A `BeeView` owns the visual objects for one `thisEnemyIndex`. It receives
sampled values from `Overlay`; it does not independently query game state.
This keeps HUD text and world guides consistent within a tick.

## HUD replacement

The overlay attaches its root under `HUDManager.Instance.HUDContainer`. That
container can change across scene transitions. `TryEnsureHudRoot()` therefore
checks the parent on every tick:

1. If no container exists, hide all world views and wait.
2. If the existing root remains under the current container, reuse it.
3. If the container changed, destroy the old HUD root, clear cached views, and
   create a new root and status text under the new container.

Rebuilding is intentional: retaining UI below an old canvas risks detached
status text and world guides that no longer have a matching HUD.

## Per-frame update and cleanup

`Overlay.Tick()` rebuilds the complete status text from the current sorted bee
set. It then hides every view whose key was not observed during that tick. The
status text is a single rich-text block rather than one UI object per row, so a
despawned bee cannot leave a stale row behind.

World guides are visual-only. Their colliders are removed, and they do not add
physics, raycast, or network components. This preserves the diagnostic role of
the overlay and avoids changing gameplay or other mods' nearby-object queries.
