# BeeOverlay Architecture

BeeOverlay is a diagnostic overlay for the state-0 spatial conditions of
`RedLocustBees`. Its game-specific implementation knowledge is documented in
[../domain/red-locust-bees.md](../domain/red-locust-bees.md); reusable rendering
knowledge is documented in
[../domain/diagnostic-visualization.md](../domain/diagnostic-visualization.md).

- [Overlay model](overlay-model.md) defines the diagnostic subjects, display
  meanings, and deliberate proxy boundaries.
- [Rendering lifecycle](rendering-lifecycle.md) defines ownership and
  replacement of HUD and world-view objects.

