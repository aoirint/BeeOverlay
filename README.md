# BeeOverlay

[![Build](https://github.com/aoirint/BeeOverlay/actions/workflows/build.yml/badge.svg)](https://github.com/aoirint/BeeOverlay/actions/workflows/build.yml)
[![Lint](https://github.com/aoirint/BeeOverlay/actions/workflows/lint.yml/badge.svg)](https://github.com/aoirint/BeeOverlay/actions/workflows/lint.yml)

BeeOverlay is a diagnostic overlay mod for Lethal Company. It visualizes the
state-related spatial checks used by `RedLocustBees`, including the bee, hive,
remembered hive position, and local player.

## Compatibility

The current analysis and implementation target Lethal Company v73.

## Installation

1. Install BepInEx 5 for Lethal Company.
2. Build the release assembly or obtain `com.aoirint.BeeOverlay.dll`.
3. Copy the DLL into the game's `BepInEx/plugins/` directory.
4. Launch Lethal Company. The overlay is created automatically when the game
   HUD is available.

## Development

Install the .NET 10 SDK, then restore packages and build the solution.

```powershell
dotnet restore BeeOverlay.sln --locked-mode
dotnet build BeeOverlay.sln --configuration Release
```

Run the same formatting check used by CI before committing changes.

```powershell
dotnet format BeeOverlay.sln --no-restore --verify-no-changes
```

## Documentation

- [RedLocustBees implementation analysis](docs/red_locust_bees.md)
- [BeeOverlay architecture](docs/architecture.md)

## License

This project is licensed under the [MIT License](LICENSE).
