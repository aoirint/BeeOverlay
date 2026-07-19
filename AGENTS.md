# Agent Instructions

## Agent Skills

Repository-local Agent Skills are deployed to `.agents/skills/` by
[APM](https://github.com/microsoft/apm). Do not edit that generated directory
directly.

## APM-managed Skills

- `apm.yml` pins the selected public
  [aoirint/skills](https://github.com/aoirint/skills); `apm.lock.yaml` records
  their resolved commits and content hashes.
- The initial pin is an explicit maintainer-approved exception to the normal
  seven-day dependency cooldown.
- To restore the committed Skill set, run `apm install --frozen` from the
  repository root, then run `apm audit --ci`.
- Make all Skill changes in the public
  [aoirint/skills](https://github.com/aoirint/skills) repository. This
  repository only selects, pins, and deploys those Skills.
- To update a Skill dependency, review its source, commit pin, license, and
  cooldown first. Update `apm.yml`, run `apm lock`, review `apm.lock.yaml`,
  run `apm install --frozen` and `apm audit --ci`, then commit the manifest,
  lockfile, and generated `.agents/skills/` changes together.

## Project Directory Structure

- `BeeOverlay/` contains the mod source and project file.
- `BeeOverlay.sln` is the solution entry point.
- `assets/` contains Thunderstore package metadata and package-facing files.
- `docs/` contains developer documentation.
    - `domain/` contains versioned base-game and reusable implementation
      knowledge without BeeOverlay-specific product decisions.
    - `architecture/` contains BeeOverlay models, workflows, responsibilities,
      and design decisions; it links to the domain knowledge it uses.
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

## Documentation Skill

Use `.agents/skills/mod-documentation-quality-check/` when creating, restructuring,
maintaining, or reviewing developer documentation.

## Documentation Boundaries

Add base-game or reusable technical knowledge to `docs/domain/`. Add a new
domain document when an architecture document needs knowledge not already
documented there. Add BeeOverlay-specific models, logic, workflows, and design
decisions to `docs/architecture/`; do not duplicate base-game analysis there.
