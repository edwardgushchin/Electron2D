# FontGlyph

Last updated: 2026-09-27

**Visibility:** internal · **Source:** [FontData.cs](../../src/Servers/Text/FontData.cs) · **Component:** [Text](../components/text.md)

## Responsibilities and invariants

Borrowed internal reference to a font-owned texture with baseline-relative offset, logical size and intrinsic-color marker. Empty glyphs have no texture. Raster bearings and scale determine offset/size; color glyphs preserve intrinsic RGB unless the applicable draw policy permits modulation.

## Integration and verification

Used through [Font](Font.md), [FontFile](FontFile.md) and canvas/Label consumers. These helpers do not add a public compatibility API. The [text component](../components/text.md#verification-boundaries) distinguishes source, managed, native pixel, allocation and platform evidence. [ADR 0046](../decisions/rendering.md#adr-0046) owns the backend and integration boundary.
