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
while applying those values. The overlay currently samples HUD and world-guide
values separately; see [Per-frame update and cleanup](#per-frame-update-and-cleanup)
for the resulting snapshot-improvement requirement.

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

`Overlay.Tick()` rebuilds the complete status text from the current sorted bee
set. It then hides every view whose key was not observed during that tick. The
status text is a single rich-text block rather than one UI object per row, so a
despawned bee cannot leave a stale row behind.

The current implementation calculates world-guide values in `DrawBee()` and
then independently calculates status values in `GetBeeStatusLine()`. It does
not yet use the shared snapshot required by the diagnostic-visualization domain
guidance, so values can differ if game state changes between those reads. A
future refactor should sample each bee once, then supply that immutable sample
to both rendering paths.

World guides are visual-only. Their colliders are removed, and they do not add
physics, raycast, or network components. This preserves the diagnostic role of
the overlay and avoids changing gameplay or other mods' nearby-object queries.
