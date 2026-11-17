# Development Operations

## Dependency updates

Run from the repository root in PowerShell.
NuGet applies a seven-day cooldown to automatic version selection.

For a plugin dependency update:

```powershell
dotnet restore BeeOverlay/BeeOverlay.csproj `
  --configfile BeeOverlay/nuget.config --force-evaluate --use-lock-file
dotnet restore BeeOverlay/BeeOverlay.csproj `
  --configfile BeeOverlay/nuget.config --locked-mode
```
