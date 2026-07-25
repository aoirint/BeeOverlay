# Changelog

All notable changes to this project are documented in this file.

This changelog is the canonical developer-facing release history. The
Thunderstore-facing package changelog in `assets/CHANGELOG.md` is derived from
stable release entries in this file and rewritten for users.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/).

## Unreleased

### Fixed

- Reset the selected bee when leaving or disconnecting from a lobby.

## v0.3.0 - 2026-07-25 UTC

### Added

- Added bee selection.
    - Added a rebindable **Select Next Bee** InputUtils action, bound to `B`
      by default.
    - Limits world markers and spatial guides to one selected bee. BeeOverlay
      starts with no selection, cycles through the sorted HUD rows and back to
      no selection, and shows diagnostics only while a bee is selected.
    - Displays a game-styled HUD Tip when target selection is requested with no
      bees available, or when a selected bee is removed and selection returns
      to empty.

### Notes

- Runtime dependency: LethalCompany InputUtils v0.7.13 is required for the
  rebindable target-selection action. Thunderstore installs receive it through
  the package manifest; manual installations must provide it separately.
- This stable release supersedes v0.3.0-alpha.1. Thunderstore publication is
  enabled for this stable version.

## v0.3.0-alpha.1 - 2026-07-25 UTC

### Added

- Added bee selection.
    - Added a rebindable **Select Next Bee** InputUtils action, bound to `B`
      by default.
    - Limits world markers and spatial guides to one selected bee. BeeOverlay
      starts with no selection, cycles through the sorted HUD rows and back to
      no selection, and shows diagnostics only while a bee is selected.
    - Displays a game-styled HUD Tip when target selection is requested with no
      bees available, or when a selected bee is removed and selection returns
      to empty.

### Notes

- Runtime dependency: LethalCompany InputUtils v0.7.13 is required for the
  rebindable target-selection action. Thunderstore installs receive it through
  the package manifest; manual installations must provide it separately.
- This prerelease is published to GitHub only. Thunderstore publication remains
  limited to stable releases.

## v0.2.0 - 2026-07-25 UTC

### Added

- Added `General.Enabled` to disable all BeeOverlay functionality.
- Added `General.AllowGuestEnabled`, which lets a host explicitly allow non-host
  players to use BeeOverlay. It defaults to `true`.
- Added `Overlay.Enabled` as the local presentation switch, plus
  `Overlay.HudEnabled` and detailed, independently switchable world-guide
  settings for individual presentation elements.
- Added a host-presence handshake that keeps a non-host client's overlay hidden
  unless the lobby host responds from BeeOverlay. The client sends at most three
  requests on a delayed, bounded-frequency schedule instead of polling during
  frame updates.

### Changed

- Updated the Lethal Company compatibility target and compile-time GameLibs
  reference from v73 to v81 (Steam manifest ID `6423525044216269478`).

### Notes

- Compatibility: Lethal Company v81 (2026-04-17 UTC, Manifest ID:
  `6423525044216269478`).
- This stable release supersedes v0.2.0-alpha.1. Thunderstore publication is
  enabled for this stable version.

## v0.2.0-alpha.1 - 2026-07-20 UTC

### Changed

- Updated the Lethal Company compatibility target and compile-time GameLibs
  reference from v73 to v81 (Steam manifest ID `6423525044216269478`).

### Notes

- Compatibility: Lethal Company v81 (2026-04-17 UTC, Manifest ID:
  `6423525044216269478`).
- This prerelease is published to GitHub only. Thunderstore publication remains
  limited to stable releases.

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

- Compatibility:
    - Compatible with Lethal Company v73 (2025-10-04 UTC, Manifest ID:
      `1749099131234587692`).
        - Backfilled as reference compatibility information while preparing the
          v0.2.0 release.
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
