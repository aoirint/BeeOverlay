# Diagnostic Visualization for Lethal Company Mods

## Purpose

Use diagnostic visualization to make a mod's current interpretation of game
state inspectable without changing the game state itself. This applies to HUD
text, world markers, guide lines, and wireframe volumes.

Keep game-specific mechanics in their own analysis document. This document
defines reusable rendering and lifecycle practices.

## Sample once, render consistently

Sample the relevant game objects and positions once for an update, then derive
every HUD row and world guide from that same snapshot. This avoids a HUD label
and a guide line disagreeing because they read the game at different times.

Keep three categories distinct in both code and labels:

- **Direct observation**: a game member or game method result.
- **Derived condition**: a calculation using documented game values.
- **Diagnostic proxy**: an intentionally incomplete stand-in for a game
  condition.

Name proxies as proxies. Do not let a visual approximation appear to be a
complete game-state decision.

## HUD lifecycle

Attach custom HUD content below the game's current HUD container. Treat the
container as transient: scenes and HUD reconstruction can replace it.

- Reuse the existing root only while it remains attached to the current
  container.
- Hide or destroy obsolete visual objects before attaching replacements.
- Rebuild status rows from the current object set when stale rows would be
  misleading.
- Use stable game identifiers for internal keys; use separately defined,
  human-friendly ordinals only for display.

## World guides

Use simple world-space geometry for quantities that are hard to infer from
text alone:

- Mark sampled positions with a small offset so terrain and model meshes do
  not hide them.
- Draw lines from the actual origin used by the game check.
- Draw distance thresholds as wireframe volumes rather than opaque meshes.
- Use one color family per domain concept and muted colors for inactive or
  blocked conditions.

Treat all guide objects as visual-only. Remove colliders and do not add
physics, raycast targets, network objects, or gameplay components.

## Resource and teardown rules

Keep a per-subject view object that owns the GameObjects, renderers, and
materials created for that subject. When the source subject disappears, remove
its view in the same update.

Destroy owned Unity objects when a view is retired. Do not destroy game-owned
objects, shared HUD containers, or objects that another mod may own.

## Change checklist

Before changing diagnostic rendering, confirm:

1. The source of every displayed value is direct, derived, or explicitly a
   proxy.
2. HUD and world guides use a coherent update snapshot.
3. Scene or HUD replacement cannot leave duplicate or detached visuals.
4. All diagnostic geometry is inert to physics and raycasts.
5. Removed game subjects release their corresponding visual resources.
