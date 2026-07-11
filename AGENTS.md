# Agent Instructions

## Agent Skills

Use repository-local Agent Skills from:

- `.agents/skills/`

## Project Directory Structure

- `BeeOverlay/` contains the mod source and project file.
- `BeeOverlay.sln` is the solution entry point.
- `docs/` contains developer documentation.
  - `red_locust_bees.md` covers the implementation and behavior of Lethal
    Company's `RedLocustBees`.
  - `architecture.md` covers BeeOverlay UI, visualization, and architecture
    decisions.

## Implementation Analysis

When creating or updating implementation-analysis documentation:

### Documentation structure

- Organize analysis documentation into separate files by concern. Keep one
  concern per file, and add a file when a new independent concern emerges.
- Record the target game version and Steam manifest ID. When the target version
  changes, update both values and replace the findings in the existing document
  instead of creating version-specific copies.
- Separate the implementation reference from behavior analysis. The reference
  identifies the code to inspect; the analysis records what the implementation
  does.
- Use an `Implementation reference` section for fields and methods relevant to
  the analysis. Do not call it an implementation surface when it includes
  non-public members.
- Group the reference by owning class using class-named subsections. Within
  such a subsection, list member and method names without repeating the class
  name. Use qualified names such as `ClassName.MethodName()` in headings,
  cross-references, and other contexts where the owner is not already clear.
- List fields and methods separately. For each field, record its C# type and
  role; for each method, record its return type, parameters, and role.
- Add related game classes in their own class-named subsection, and explain
  how each one affects the primary subject class.

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
