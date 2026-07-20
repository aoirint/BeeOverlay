# Changelog

All notable changes to this project are documented in this file.

This changelog is the canonical developer-facing release history. The
Thunderstore-facing package changelog in `assets/CHANGELOG.md` is derived from
stable release entries in this file and rewritten for users.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/).

## Unreleased

### Changed

- Updated the Lethal Company compatibility target and compile-time GameLibs
  reference from v73 to v81 (Steam manifest ID `6423525044216269478`).

### Notes

- The v81 target is supported by managed-code and asset inspection. Runtime
  validation of HUD behavior in a clean v81 game profile remains pending.

## v0.1.0 - 2026-07-18 UTC

### Added

- Added the initial stable RedLocustBees diagnostic overlay for Lethal Company
  v73.
- Added latitude-and-longitude wireframe spheres that make the overlay's
  distance guides easier to read in three dimensions.

### Changed

- Stabilized the release, package, and publishing workflow used to distribute
  the overlay through GitHub Releases and Thunderstore.

### Notes

- Compatibility: Lethal Company v73 (Steam manifest ID
  `1749099131234587692`).
- This stable release supersedes the v0.1.0-alpha.1 through v0.1.0-alpha.3
  prereleases. Thunderstore publishing is enabled for this stable version.

## v0.1.0-alpha.3 - 2026-07-12 UTC

### Added

- Replaced circular spatial guides with latitude-and-longitude wireframe spheres
  for clearer three-dimensional distance cues.

### Changed

- Ported the current CruiserJumpPractice build workflow to BeeOverlay with only
  project-specific identifiers changed.
- Removed one trailing whitespace character from the ported workflow because
  BeeOverlay's `actionlint` rejects it.

### Notes

- This prerelease creates a GitHub prerelease and build artifact. Thunderstore
  publishing remains limited to stable releases.

## v0.1.0-alpha.2 - 2026-07-12 UTC

### Changed

- Updated release metadata without changing the CI workflow.

### Notes

- The reported BepInEx prerelease metadata problem remains under investigation.

## v0.1.0-alpha.1 - 2026-07-11 UTC

### Added

- Added a RedLocustBees diagnostic overlay for Lethal Company v73.
- Added locked dependency restore, CI linting, package artifacts, and guarded
  GitHub Release and Thunderstore publishing workflows.
- Added Thunderstore package metadata, package-facing documentation, and icons.

### Notes

- This prerelease creates a GitHub prerelease and build artifact. Thunderstore
  publishing remains limited to stable releases.
