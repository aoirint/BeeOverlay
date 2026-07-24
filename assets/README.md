# BeeOverlay

BeeOverlay visualizes RedLocustBees spatial checks.

## Compatibility

- Lethal Company v81 (2026-04-17 UTC, Manifest ID:
  `6423525044216269478`)
    - Analysis evidence
        - The target is verified from the supplied managed-code and asset
          exports. In-game HUD validation remains pending.
    - Package dependency
        - [BepInExPack][bepinexpack-package] v5.4.2305 (2026-03-17 UTC)

## What it does

- Displays a HUD summary for each `RedLocustBees` instance.
- Draws the bee, hive, remembered hive position, and local-player spatial
  guides.
- Shows relevant sight, distance, and line-of-sight conditions without changing
  game behavior.

## Who needs to install

Install BeeOverlay on the client that should see its diagnostic overlay.

## Documentation

For implementation details, see the
[repository documentation](https://github.com/aoirint/BeeOverlay/tree/main/docs).

## AI Disclosure

Some parts of this project were developed with AI tools based on large language
models (LLMs), including agent-based tools. The project maintainer reviews the
code. This disclosure is made in compliance with Thunderstore and community
policies.

[bepinexpack-package]: https://thunderstore.io/c/lethal-company/p/BepInEx/BepInExPack/
