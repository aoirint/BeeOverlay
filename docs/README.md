# Developer Documentation

## Documentation boundaries

- `domain/` contains versioned Lethal Company and reusable implementation
  knowledge. It must not prescribe this mod's product behaviour, model, or
  design choices.
- `architecture/` contains this mod's models, workflows, responsibilities,
  and design decisions. It links to the domain knowledge it relies on.
- `diagrams/` contains maintained diagram and icon sources with their authoring
  procedures. It links to the documents that own the facts shown by those
  artifacts.
- Keep a domain document focused on one game or technical concern. Add a new
  domain document when an architecture document needs knowledge not already
  covered there.
- Keep an architecture document focused on one mod concern. Do not copy
  base-game member declarations or behaviour analysis into it; link to the
  relevant domain document instead.

## Diagram sources

See [diagrams/README.md](diagrams/README.md) for the Bee AI Break diagram and
package-icon authoring procedures.

Start with [architecture/README.md](architecture/README.md) for BeeOverlay's
design, and [domain/README.md](domain/README.md) for supporting knowledge.
