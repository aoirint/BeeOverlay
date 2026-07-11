# Agent Instructions

## Agent Skills

Use repository-local Agent Skills from:

- `.agents/skills/`

## Project Directory Structure

- `BeeOverlay/` contains the mod source and project file.
- `BeeOverlay.sln` is the solution entry point.
- `assets/` contains Thunderstore package metadata and package-facing files.
- `docs/` contains developer documentation.
    - `red_locust_bees.md` covers the implementation and behavior of Lethal
      Company's `RedLocustBees`.
  - `architecture.md` covers BeeOverlay UI, visualization, and architecture
      decisions.
  - `icon-authoring.md` covers package-icon authoring and rendering.

## Icon Assets

When changing `assets/icon.svg` or regenerating `assets/icon.png`, follow
`docs/icon-authoring.md`.

## Implementation Analysis

Use `.agents/skills/implementation-analysis-quality-check/` when creating,
updating, or reviewing implementation-analysis documentation.
