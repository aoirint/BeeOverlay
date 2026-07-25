# Changelog

## Unreleased

### Changed

- Lobby hosts must install BeeOverlay before non-host players can use its
  diagnostic overlay. This keeps diagnostic features from being used in a lobby
  without the host's knowledge.
- Hosts can enable guest use with `General.GuestEnabled`. Overlay visibility
  settings now use the `Overlay` category.

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
