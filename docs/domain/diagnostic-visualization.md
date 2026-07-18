# Diagnostic Visualization for Lethal Company Mods

## Target

- Game: Lethal Company v73
- Steam manifest ID: `1749099131234587692`

The `HUDManager` members below are base-game members. The Unity UI and
renderer APIs are framework APIs used to create mod-owned visuals; verify them
against the Unity assemblies referenced by the target game when updating the
game dependency.

## Purpose

Use diagnostic visualization to make a mod's current interpretation of game
state inspectable without changing game state. This applies to HUD text, world
markers, guide lines, and wireframe volumes.

Keep game-specific mechanics in their own analysis document. This document
defines reusable rendering, ownership, and lifecycle practices.

## Patch and access targets

### `HUDManager`

| Member | Declaration | Role |
| --- | --- | --- |
| Singleton | `public static HUDManager Instance { get; private set; }` | Resolve the current HUD manager; it can be unavailable before HUD setup. |
| HUD root | `public GameObject HUDContainer` | Parent custom HUD objects below this game-owned container. |
| Setup | `private void Awake()` | A postfix observes that this manager's base setup has completed. |
| Frame update | `private void Update()` | A postfix supplies a HUD-timed update hook. |
| Tip API | `public void DisplayTip(string headerText, string bodyText, bool isWarning = false, bool useSave = false, string prefsKey = "LC_Tip1")` | Use the game's transient tip presentation when persistent custom content is not needed. |

### Legacy Unity UI

| Member | Declaration | Role |
| --- | --- | --- |
| Text component | `UnityEngine.UI.Text` | The legacy text component used for a mod-owned status label under the HUD container. |
| Layout component | `RectTransform` | Defines anchors, pivot, offsets, and size in the HUD canvas. |
| Built-in font | `Resources.GetBuiltinResource<Font>("Arial.ttf")` | Supplies an available font for a legacy `Text` component. |

### World-space Unity renderers

| Member | Declaration | Role |
| --- | --- | --- |
| Guide line | `LineRenderer` | Renders a segment or a sampled polyline without creating a collider. |
| Marker factory | `GameObject.CreatePrimitive(PrimitiveType.Sphere)` | Creates a visible spherical marker with a renderer and a `SphereCollider`. |
| Collider removal | `Object.Destroy(marker.GetComponent<Collider>())` | Makes a primitive marker inert to game physics and raycasts. |
| Material | `new Material(Shader.Find("Sprites/Default"))` | Provides a mod-owned material for runtime-created renderers. |

## Sample once, render consistently

Sample the relevant game objects and positions once for an update, then derive
every HUD row and world guide from that snapshot. This prevents a HUD label and
a guide line from disagreeing because they read game state at different times.

Keep three categories distinct in code and labels:

- **Direct observation**: a game member or game method result.
- **Derived condition**: a calculation using documented game values.
- **Diagnostic proxy**: an intentionally incomplete stand-in for a game
  condition.

Name proxies as proxies. Do not let a visual approximation appear to be a
complete game-state decision.

## HUD lifecycle

`HUDManager.Instance` and `HUDManager.Instance.HUDContainer` are not valid
until the HUD exists. A mod-owned root remains reusable only while it is still
parented to the current `HUDContainer`; scene transitions or HUD recreation can
replace that container.

For a legacy text label, create a child `GameObject` with `RectTransform` and
`UnityEngine.UI.Text`, parent it with `SetParent(container, false)`, then set
the rect anchors, offsets, font, alignment, and text color. The `false` keeps
the child in the container's local canvas coordinate system.

### Attach custom content to the current `HUDContainer`

#### Resolve `HUDManager.Instance.HUDContainer` whenever attaching — recommended

This uses the game-owned container that is current for the active HUD. Compare
the remembered parent with the current container before reuse, then destroy the
mod-owned root and rebuild it when they differ.

#### Cache a canvas or a previous container indefinitely

The cached object can be detached or belong to a HUD that has been replaced.
It does not establish that new content appears in the active player HUD.

### Choose text presentation

#### Create `UnityEngine.UI.Text` below the HUD container — recommended for persistent diagnostics

It gives the mod a separately owned label whose content can be refreshed each
HUD update. Supplying a `Font` and a `RectTransform` is required; a bare text
component has neither the intended layout nor a guaranteed readable font.

#### Use `HUDManager.DisplayTip(...)`

This is appropriate for short, game-styled notifications. The game owns tip
visibility and replacement, so it is not a stable surface for a continuously
updated diagnostic table.

#### Assume a TextMeshPro component or create a separate canvas

The HUD container is the integration point established by the game. A TMP
component or separate canvas adds a different dependency and does not by itself
inherit the active HUD's lifecycle or layout.

## World guides

Use simple world-space geometry for quantities that are hard to infer from
text alone. Mark sampled positions with a small offset so terrain and model
meshes do not hide them, draw lines from the actual origin used by the game
check, and use one color family per domain concept.

### Draw a segment

#### Configure a two-point `LineRenderer` in world space — recommended

Create it on a mod-owned child object, set `positionCount = 2` and
`useWorldSpace = true`, then update positions with `SetPosition(0, start)` and
`SetPosition(1, end)`. Set widths and both endpoint colors explicitly. Use a
white or otherwise untinted material when the line color is changed per update;
a tinted material would alter the intended endpoint colors.

#### Draw a mesh or create one object per frame

A mesh adds unnecessary geometry management for a segment. Recreating objects
per update loses stable ownership and creates avoidable allocation and teardown
work.

### Mark a position

#### Create a primitive sphere and remove its collider — recommended

`CreatePrimitive(PrimitiveType.Sphere)` supplies a visible `MeshRenderer`, but
also creates a `SphereCollider`. Assign a mod-owned material, set the marker's
scale and offset, and destroy that collider before the marker participates in
the scene.

#### Keep the primitive collider

The marker can then affect physics queries and gameplay interactions, violating
the diagnostic-only boundary.

### Show a radial threshold

#### Build a wireframe from owned `LineRenderer` circles — recommended

Represent the equator, latitude, and meridian circles with line renderers. The
result exposes a radius without obscuring players or level geometry, and each
line remains owned by the same subject view.

#### Scale an opaque primitive sphere

An opaque sphere hides the geometry the diagnostic is meant to explain. It
also creates a collider unless it is explicitly removed.

## Resource and teardown rules

Keep a per-subject view object that owns the `GameObject` instances, renderers,
and materials created for that subject. When the source subject disappears,
hide or destroy its view in the same update.

Destroy owned Unity objects and runtime-created materials when a view is
retired. Do not destroy game-owned objects, shared HUD containers, or objects
that another mod may own.

## Change checklist

1. The source of every displayed value is direct, derived, or explicitly a
   proxy.
2. HUD and world guides use a coherent update snapshot.
3. `HUDManager.Instance.HUDContainer` is available before attachment, and a
   replaced container cannot leave duplicate or detached mod content.
4. Each legacy text label has a `RectTransform`, a font, and an explicit
   layout.
5. Every primitive marker has its collider removed; guide objects have no
   gameplay, network, or raycast role.
6. Removed game subjects release their corresponding visual resources.
