# Agent Instructions

## Agent Skills

Use repository-local Agent Skills from:

- `.agents/skills/`

## Documentation Structure

- Keep `docs/red_locust_bees.md` focused on the implementation and behavior of
  Lethal Company's `RedLocustBees`. Put BeeOverlay UI, visualization, and
  architecture decisions in `docs/architecture.md`.
- Record the target game version and Steam manifest ID. When the target version
  changes, update both values and replace the findings in the existing document
  instead of creating version-specific copies.
- Name the owning class for documented members and methods. For example, use
  `RedLocustBees.CheckLineOfSightForPlayer()` rather than
  `CheckLineOfSightForPlayer()` in headings.
- Add related game classes under `Observed members` in their own class-named
  subsection, and explain how each one affects `RedLocustBees` behavior.

## Implementation Analysis

When creating or updating implementation-analysis documentation:

### Evidence and reasoning

- Ground findings in the target-version implementation and, when available,
  runtime observations. Record the version and the relevant class or member,
  not a machine-specific source location.
- Separate confirmed observations from inferences. State the limiting condition
  when a game-side transition or behavior cannot be determined completely.
- Keep documented facts minimal enough for an independent reviewer to verify.
  Do not add speculative causal chains as facts.
- Trace the assignments, calls, and guards that update a value before using the
  value to explain behavior. Do not infer a state-transition condition from a
  related condition alone.

### Source material and local information

- Do not include local absolute paths, user names, home directories, or other
  machine-specific locations in tracked documentation, source comments, logs,
  commit messages, or pull-request text.
- Do not reproduce decompiled proprietary method bodies or substantial portions
  of proprietary code. Refer to the class and member, then write a concise
  behavioral summary or the minimum necessary signature detail.
- Runtime logs may be quoted when they are needed as evidence. Mask sensitive
  or machine-specific information. Prefer including only the portion needed to
  support the finding and preserve relevant uncertainty.
