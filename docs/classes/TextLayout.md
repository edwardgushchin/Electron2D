# TextLayout

Last updated: 2026-10-04

**Visibility:** internal · **Source:** [TextLayout.cs](../../src/Servers/Text/TextLayout.cs) · **Component:** [Text](../components/text.md)

## Responsibilities and invariants

Reusable layout buffer owned by Font caches or a text consumer. Converts text to Unicode scalars, determines grapheme/word/line boundaries and scripts, resolves fallback faces, shapes contextual runs, applies BiDi order, line breaks, clipping/ellipsis and permitted justification. Character bounds use shaped scalar clusters rather than ink boxes. Font revisions invalidate cached layout. Build leases every source face, repeats at most 64 times when a callback or concurrent replacement changes dependencies, and isolates nested queries from active cache entries. Drawing revalidates the built generation before recording and holds source leases through the complete draw. Retained canvas commands preserve immutable glyph-image snapshots after source retirement.

## Integration and verification

Word and locale-specific line boundaries come from the private ICU dictionary iterators; grapheme boundaries and BiDi processing use the pinned managed algorithms. Decoding also caches exact nonprinting classification, so unsupported graphic characters retain hexadecimal fallback boxes while missing control or unassigned glyphs do not acquire a synthetic advance.

Used through [Font](Font.md), [FontFile](FontFile.md) and canvas/Label consumers. These helpers do not add a public compatibility API. The [text component](../components/text.md#verification-boundaries) distinguishes source, managed, native pixel, allocation and platform evidence. [ADR 0046](../decisions/rendering.md#adr-0046) owns the backend and integration boundary.

## Editing geometry

[TextLayout.Editing.cs](../../src/Servers/Text/TextLayout.Editing.cs) provides shaped-grapheme/word boundaries, dominant input direction, leading/trailing BiDi carets, nearest-column hit testing and visual selection intervals for [LineEdit](LineEdit.md). Cluster advance is subdivided over scalar columns; zero-width controls keep valid boundary positions. Carets are prepared once per build in retained buffers, so a pointer hit is linear instead of rescanning all glyphs per column. Optional PreserveControl emits visible hexadecimal placeholders; optional local clip rectangles crop glyph textures and missing-glyph rectangles before canvas recording, converting logical glyph coordinates to physical oversampled bitmap UVs. LineEditTests verifies these paths alongside native editing/display.
