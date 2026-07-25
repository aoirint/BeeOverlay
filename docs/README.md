# Developer Documentation

## Documentation boundaries

- `domain/` contains versioned Lethal Company and reusable implementation
  knowledge. It must not prescribe this mod's product behaviour, model, or
  design choices.
- `architecture/` contains this mod's models, workflows, responsibilities,
  and design decisions. It links to the domain knowledge it relies on.
- `release/` contains release-facing diagrams, icons, screenshots, and their
  asset-specific authoring procedures. It links to the documents that own the
  facts shown by those artifacts.
- `operations/` contains shared maintainer procedures, including visual-asset
  rendering and validation.
- Keep a domain document focused on one game or technical concern. Add a new
  domain document when an architecture document needs knowledge not already
  covered there.
- Keep an architecture document focused on one mod concern. Do not copy
  base-game member declarations or behaviour analysis into it; link to the
  relevant domain document instead.

## Release assets

See [release/README.md](release/README.md) for the Bee AI Break diagram,
package icon, and usage screenshots. See [operations/README.md](operations/README.md)
for the shared rendering and validation procedure.

Start with [architecture/README.md](architecture/README.md) for BeeOverlay's
design, and [domain/README.md](domain/README.md) for supporting knowledge.
