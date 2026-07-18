# Agent Instructions

## Agent Skills

Use repository-local Agent Skills from:

- `.agents/skills/`

## Project Directory Structure

- `BeeOverlay/` contains the mod source and project file.
- `BeeOverlay.sln` is the solution entry point.
- `assets/` contains Thunderstore package metadata and package-facing files.
- `docs/` contains developer documentation.
    - `red-locust-bees.md` covers the implementation and behavior of Lethal
      Company's `RedLocustBees`.
    - `architecture.md` covers BeeOverlay UI, visualization, and architecture
      decisions.
    - `icon-authoring.md` covers package-icon authoring and rendering.

## Icon Assets

When changing `assets/icon.svg` or regenerating `assets/icon.png`, follow
`docs/icon-authoring.md`.

## Local Prerelease Builds

When building a prerelease DLL for local installation or runtime validation,
pass a BepInEx-compatible plugin metadata version:

```powershell
dotnet build BeeOverlay.sln -c Release /p:BepInExPluginVersion=0.0.0
```

BepInEx 5 validates plugin metadata as `System.Version` and rejects SemVer
prerelease suffixes. Keep the project `Version` as the release identity; do
not add a persistent `BepInExPluginVersion` override to the project file.

## Implementation Analysis

Use `.agents/skills/implementation-analysis-quality-check/` when creating,
updating, or reviewing implementation-analysis documentation.
