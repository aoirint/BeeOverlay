# Agent Instructions

Use repository-local Agent Skills from:

- `.agents/skills/`

## Implementation Analysis

When creating or updating implementation-analysis documentation:

- Keep `docs/red_locust_bees.md` focused on the implementation and behavior of
  Lethal Company's `RedLocustBees`. Put BeeOverlay UI, visualization, and
  architecture decisions in `docs/architecture.md`.
- Record the target game version. When the target version changes, replace the
  findings in the existing document instead of creating version-specific copies.
- Name the owning class for documented members and methods. For example, use
  `RedLocustBees.CheckLineOfSightForPlayer()` rather than
  `CheckLineOfSightForPlayer()` in headings.
- Add related game classes under `Observed members` in their own class-named
  subsection, and explain how each one affects `RedLocustBees` behavior.
- Separate confirmed observations from inferences. State the limiting condition
  when a game-side transition or behavior cannot be determined completely.
