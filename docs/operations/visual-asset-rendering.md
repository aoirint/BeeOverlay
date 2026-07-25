# Visual Asset Rendering

Use this procedure when updating a release asset that has an editable source
and a rendered derivative. Asset-specific constraints remain in the adjacent
guide under `../release/`.

## Shared rules

- Treat the editable SVG as the only source of visual truth. Do not edit a
  generated PNG or WebP by hand.
- Render and inspect the source after a visual change; source syntax alone does
  not establish readable layout.
- Keep temporary renders outside the repository. Commit a derivative only when
  a declared consumer needs a retained file.
- Validate each consumer separately: the root README may use the SVG, while
  Thunderstore's package README uses a WebP fallback or the required package
  asset.

## Update sequence

1. Read the asset-specific guide and the document that owns any facts shown.
2. Update the canonical source, then render and inspect it at its intended
   size.
3. Regenerate a retained derivative only when the source or its consumer-facing
   content changed.
4. Check the repository and package README references, including their target
   renderer behavior after merge to `main`.
5. Run the asset-specific syntax and dimension checks, then `git diff --check`.
