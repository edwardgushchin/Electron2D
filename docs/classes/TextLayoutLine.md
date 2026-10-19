# TextLayoutLine

Last updated: 2026-10-19

**Visibility:** internal · **Source:** [TextLayout.cs](../../src/Servers/Text/TextLayout.cs) · **Component:** [Text](../components/text.md)

## Responsibilities and invariants

Immutable scalar and glyph ranges plus advance width, ascent/descent and placement information for one formatted line. All ranges address the reusable owning TextLayout buffers; they do not transfer their ownership.

## Integration and verification

Used through [Font](Font.md), [FontFile](FontFile.md) and canvas/Label consumers. These helpers do not add a public compatibility API. The [text component](../components/text.md#verification-boundaries) distinguishes source, managed, native pixel, allocation and platform evidence. [ADR 0046](../decisions/rendering.md#adr-0046) owns the backend and integration boundary.

Underline offset/thickness snapshots are retained with baseline metrics during shaping under active font read scopes. Prepared text-buffer queries read these values without invoking fonts after face retirement.
