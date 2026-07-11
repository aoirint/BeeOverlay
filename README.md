# BeeOverlay

[![Build](https://github.com/aoirint/BeeOverlay/actions/workflows/build.yml/badge.svg)](https://github.com/aoirint/BeeOverlay/actions/workflows/build.yml)
[![Lint](https://github.com/aoirint/BeeOverlay/actions/workflows/lint.yml/badge.svg)](https://github.com/aoirint/BeeOverlay/actions/workflows/lint.yml)

A [Lethal Company][lethal-company-steam] diagnostic overlay mod that visualizes
RedLocustBees spatial checks.

The current analysis and implementation target Lethal Company v73.

## Installation

1. Install BepInEx 5 for Lethal Company.
2. Build the release assembly or obtain `com.aoirint.BeeOverlay.dll`.
3. Copy the DLL into the game's `BepInEx/plugins/` directory.
4. Launch Lethal Company. The overlay is created automatically when the game
   HUD is available.

## Development

Install [.NET SDK 10.0][dotnet-sdk-download] or later.

Install [PowerShell 7][powershell-install].

Install [Visual Studio 2022][visual-studio-download].

Install [Docker][docker-install] if you plan to use the documented local
Markdown lint command.

Install [ShellCheck][shellcheck-repo], [`actionlint`][actionlint-repo], and
[pinact][pinact-repo] if you plan to run GitHub Actions quality checks locally.

Restore NuGet packages.

```powershell
dotnet restore --locked-mode
```

Open `BeeOverlay.sln` in Visual Studio.

## Quality checks

Run the relevant checks before opening a pull request.

### C# format

- Language version:
  [C# 13.0](https://learn.microsoft.com/en-us/dotnet/csharp/whats-new/csharp-13)
- Target framework:
  [.NET Standard 2.1](https://learn.microsoft.com/en-us/dotnet/standard/net-standard?tabs=net-standard-2-1)

```powershell
dotnet format --no-restore --verify-no-changes
```

`dotnet format` is an aggregate formatter that checks whitespace, built-in code
style, and fixable analyzer diagnostics. Roslyn analyzers also run during build,
including diagnostics that cannot be automatically fixed.

### Markdown lint

Markdown is checked with [`markdownlint-cli2`][markdownlint-cli2-repo].
The pinned Docker image below is the documented local command so contributors do
not need a local Node.js project, but Docker is not required.
Other installation methods are acceptable when they run the same
`markdownlint-cli2` version with this repository's configuration.
The image's default working directory is `/workdir`, so mount the repository
there. Run the Docker command without network access and as a non-root user.

On Windows with PowerShell, use UID/GID `1000:1000`:

```powershell
docker run --rm --network none --user 1000:1000 -v ".:/workdir" davidanson/markdownlint-cli2:v0.22.1@sha256:0ed9a5f4c77ef447da2a2ac6e67caf74b214a7f80288819565e8b7d2ac148fe5
```

On Linux, use `sudo docker` and pass the host user's UID and GID:

```bash
sudo docker run --rm --network none --user "$(id -u):$(id -g)" -v ".:/workdir" davidanson/markdownlint-cli2:v0.22.1@sha256:0ed9a5f4c77ef447da2a2ac6e67caf74b214a7f80288819565e8b7d2ac148fe5
```

When updating Markdown lint tooling, update the documented local command and
the CI action together after the repository cooldown period has elapsed.

### GitHub Actions lint

GitHub Actions workflows are checked with [`actionlint`][actionlint-repo].

`actionlint` checks workflow syntax, expressions, runner labels, and action
metadata. The pyflakes integration remains disabled because this repository
does not currently contain Python files. Revisit that setting if Python scripts
are added.

```powershell
actionlint -pyflakes=
```

When updating CI, use cooldown-compliant pinned releases. The workflow downloads
Linux release archives directly and verifies their SHA256 values before running
them. It caches only the archives, not the extracted executables, so cached
downloads are still verified before use.

### GitHub Actions pinning

GitHub Actions and reusable workflows are checked with [pinact][pinact-repo] so
external actions stay pinned to full commit SHAs with synchronized version
comments.

```powershell
pinact run --check --min-age 7
```

For local fixes or maintenance updates, use the same cooldown setting:

```powershell
# Pin or refresh version comments.
pinact run --min-age 7

# Update pinned actions after the repository cooldown period.
pinact run --update --min-age 7
```

Set `GITHUB_TOKEN` when possible so pinact can query GitHub's API with normal
authenticated rate limits. Install pinact from its upstream releases,
package-manager integrations, or another trusted distribution.

CI downloads the Linux amd64 release archive directly and verifies its SHA256
before running pinact. It caches only the archive, not the extracted executable,
so cached downloads are still verified before use.

## Package management

### Dependency updates

To update the lock file after modifying package references, run:

```powershell
dotnet restore --use-lock-file
```

### .NET and C# tooling updates

This project separates the SDK used to build and format the mod from the target
framework that controls runtime compatibility.

- Keep `TargetFramework` on `netstandard2.1` unless Lethal Company, BepInEx,
  Unity, or compile-only dependencies require a compatibility change.
- Prefer supported LTS SDKs for routine maintenance. Use an STS or newer SDK
  major only when it solves a specific compiler, formatter, analyzer, CI, or
  Visual Studio problem.
- Keep `LangVersion` explicit. Before increasing it, confirm SDK, Visual Studio
  2022, and dependency compatibility, then update the C# format summary above.
- For analyzer updates, update `BeeOverlay/packages.lock.json`, review new
  diagnostics, and separate mechanical formatting from intentional rule or
  code changes when practical.
- Preserve existing restore, format, build, and Markdown lint behavior by
  default. Record compatibility checks and verification commands in the pull
  request, and defer the update when the impact is unclear.

Maintenance references:

- [.NET releases and support](https://learn.microsoft.com/en-us/dotnet/core/releases-and-support)
- [.NET SDK, MSBuild, and Visual Studio versioning](https://learn.microsoft.com/en-us/dotnet/core/porting/versioning-sdk-msbuild-vs)
- [Configure C# language version](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/configure-language-version)
- [`dotnet format`](https://learn.microsoft.com/en-us/dotnet/core/tools/dotnet-format)

## GitHub Actions

The repository uses [GitHub Actions][github-actions-docs] for CI.

### Action pinning

Action versions are pinned with [pinact][pinact-repo]. Actions and other
executable CI tooling should be updated after the repository cooldown period
has elapsed. Keep SHA pins and version comments synchronized when updating
pinned actions.

```powershell
# Pin
pinact run --min-age 7

# Update
pinact run --update --min-age 7
```

### GitHub Actions configuration

#### GitHub Variables

This repository currently does not use GitHub Actions variables.

| Name | Used by | Description |
| :--- | :------ | :---------- |
| None | Not applicable | No repository variables are currently used. |

#### GitHub Secrets

| Name | Used by | Description |
| :--- | :------ | :---------- |
| `THUNDERSTORE_TOKEN` | `.github/workflows/build.yml` | Thunderstore service-account token used only when publishing a stable release. |

## Build

```powershell
# Debug build
DOTNET_CLI_UI_LANGUAGE=en dotnet build

# Release build
DOTNET_CLI_UI_LANGUAGE=en dotnet build --configuration Release
```

## Release

The build workflow creates an artifact for every push to `main`. It treats
`0.0.0` and already-tagged versions as edge builds, so neither creates a GitHub
Release or publishes to Thunderstore.

For a stable release:

1. Update the canonical [CHANGELOG.md](CHANGELOG.md).
2. Derive the user-facing stable release notes in `assets/CHANGELOG.md`.
3. Replace `0.0.0` in `BeeOverlay/BeeOverlay.csproj` with the release version.
4. Push the commit to `main`.

The workflow packages the following files at the ZIP root, creates an immutable
GitHub Release, and publishes stable releases to Thunderstore:

- `com.aoirint.BeeOverlay.dll`
- `assets/manifest.json` as `manifest.json`
- `assets/icon.png`
- `assets/README.md` as `README.md`
- `assets/CHANGELOG.md` as `CHANGELOG.md`
- `LICENSE`

## Documentation

- [RedLocustBees implementation analysis](docs/red_locust_bees.md)
- [BeeOverlay architecture](docs/architecture.md)

## License

This project is licensed under the [MIT License](LICENSE).

## AI Disclosure

Some parts of this project were developed with AI tools based on large language
models (LLMs), including agent-based tools. The project maintainer reviews the
code. This disclosure is made in compliance with Thunderstore and community
policies.

[actionlint-repo]: https://github.com/rhysd/actionlint
[docker-install]: https://docs.docker.com/get-started/get-docker/
[dotnet-sdk-download]: https://dotnet.microsoft.com/en-us/download/dotnet/10.0
[github-actions-docs]: https://docs.github.com/en/actions
[lethal-company-steam]: https://store.steampowered.com/app/1966720/Lethal_Company/
[markdownlint-cli2-repo]: https://github.com/DavidAnson/markdownlint-cli2
[pinact-repo]: https://github.com/suzuki-shunsuke/pinact
[powershell-install]: https://learn.microsoft.com/en-us/powershell/scripting/install/install-powershell-on-windows
[shellcheck-repo]: https://www.shellcheck.net/
[visual-studio-download]: https://visualstudio.microsoft.com/en-us/vs/
