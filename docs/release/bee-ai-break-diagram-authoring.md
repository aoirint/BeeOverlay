# Bee AI Break Diagram Authoring

`bee-ai-break-diagram.svg` is the canonical source for the Bee AI Break
conditions diagram. `bee-ai-break-diagram.webp` is its Thunderstore-facing
derivative. The root README embeds the SVG; the package README embeds the
WebP. This guide answers how to update those assets without turning either
into a separate source of game-mechanics truth.

## Sources and update triggers

The diagram summarizes the two state-0 transition paths documented in
[Bee AI Break](../domain/bee-ai-break.md). Update the diagram when any of the
following changes:

- the documented hive-defense or missing-hive conditions;
- the relevant BeeOverlay visual colors or line-state conventions in
  [`Overlay.cs`](../../BeeOverlay/Interop/Overlay.cs) or
  [`Overlay.BeeView.cs`](../../BeeOverlay/Interop/Rendering/Overlay.BeeView.cs);
- the root or package README embedding paths and surrounding explanation.

Do not infer or revise game behavior from the drawing. Update the domain
document first when the underlying analysis changes, then adapt the diagram to
that documented result.

## Diagram conventions

- Keep the two cards parallel: **Hive defense** describes the player and hive;
  **Missing hive** describes the bee and known-hive position.
- Use `B`, `H`, `P`, and `K` for bee, hive, player, and known-hive position.
  Repeated markers show alternative spatial cases, not additional entities.
- Match marker and range colors to the overlay: bee yellow, hive green, player
  red, known-hive position and its probe blue. Keep `P` and `K` letters white
  on their dark markers.
- Use the inactive-line opacity from the overlay for an inactive diagram line.
  A solid-cover block is a schematic obstacle, not an inactive line, and may
  remain darker.
- Keep repeated ranges the same illustrated size within a card. In particular,
  both hive defense circles use the same radius.
- List ranges from narrower to wider, both in legends and nearby explanatory
  text.
- Use dashed arrows for check relationships so a future movement path can use
  a distinct line treatment. A dashed arrow does not, by itself, mean a check
  is blocked.
- Draw range and marker labels after the geometry they label so the text stays
  readable when it overlaps a line. The labeled distances are the mechanics;
  circle and capsule proportions are intentionally schematic for readability.

## Thunderstore WebP derivative

Thunderstore renders the package README as HTML Markdown, so use
`bee-ai-break-diagram.webp` there instead of the SVG source.

Generate the WebP only after the user-facing diagram is settled for a pull
request that updates the package README or diagram content. Do not regenerate
it for every intermediate layout adjustment, documentation-only edit, or
review iteration: each committed binary revision remains in Git history.

When generation is warranted:

1. Render the SVG to a 2× PNG with the Edge headless procedure used by
   [Icon authoring](icon-authoring.md).
2. Resize that PNG with high-quality bicubic interpolation to the SVG's
   intrinsic 1500×670 dimensions, then encode it as WebP.
3. Replace `bee-ai-break-diagram.webp`, inspect it at native size, and
   confirm it represents the final SVG revision.

Keep the temporary PNG outside the repository. The WebP is the only committed
derivative, and only when the timing rule above is met.

## Editing procedure

1. Read [Bee AI Break](../domain/bee-ai-break.md) and inspect the current
   overlay colors and line behavior before changing a mechanic, color, or
   label.
2. Edit `docs/release/bee-ai-break-diagram.svg` directly. Keep marker,
   range, arrow, cover, and label changes together when one case moves.
3. Preserve the two README embeds unless the asset path changes:
   - `README.md` uses the repository-relative SVG.
   - `assets/README.md` uses the `main`-branch raw GitHub WebP URL.
4. Parse the SVG and check whitespace before committing:

   ```powershell
   [xml](Get-Content docs/release/bee-ai-break-diagram.svg -Raw) | Out-Null
   git diff --check
   ```

5. Render the committed SVG from its branch or pull request and inspect both
   cards. Check that labels are legible, intended alternative cases are clear,
   and no text overlaps another label unintentionally.
6. Review the root and package README renderings after the PR targets `main`.
   The package README's image URL intentionally follows `main`, so it updates
   after merge rather than previewing the branch asset.

## Pull request checklist

- [ ] The domain document remains the canonical source of the shown mechanics.
- [ ] Colors, line opacity, marker letters, and range ordering follow the
      conventions above.
- [ ] The SVG parses and `git diff --check` passes.
- [ ] A rendered review confirms clear labels and balanced card layout.
- [ ] The root README resolves the SVG and the package README resolves the
      final WebP derivative.
