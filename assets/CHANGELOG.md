# Changelog

## Unreleased

### Added

- Added `General.Enabled` to disable all BeeOverlay functionality.
- Added `General.AllowGuestEnabled` to let lobby hosts allow or disallow guest use.
- Added `Overlay.Enabled` and controls for independently choosing BeeOverlay's
  HUD and world-guide elements.

### Changed

- Lobby hosts must install BeeOverlay before non-host players can use its
  diagnostic overlay. This keeps diagnostic features from being used in a lobby
  without the host's knowledge.
- Hosts can disable guest use with `General.AllowGuestEnabled`.

## v0.1.0 - 2026-07-18 UTC

### Added

- Added a diagnostic overlay for RedLocustBees in Lethal Company v73. It shows
  bee and hive information alongside relevant spatial checks, so you can see
  why the game's bee behavior is taking place.
- Added three-dimensional wireframe spheres for distance guides. They replace
  flat circular guides to make the displayed ranges easier to interpret from
  different camera angles.

### Notes

- Compatibility: Lethal Company v73 (Steam manifest ID
  `1749099131234587692`).
- Install BeeOverlay only on the client where you want to see the diagnostic
  overlay; it does not change game behavior.
