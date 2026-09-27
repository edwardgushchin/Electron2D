# TextLayout

Last updated: 2026-09-27

**Visibility:** internal · **Source:** [TextLayout.cs](../../src/Servers/Text/TextLayout.cs) · **Component:** [Text](../components/text.md)

## Responsibilities and invariants

Reusable layout buffer owned by Font caches or a text consumer. Converts text to Unicode scalars, determines grapheme/word/line boundaries and scripts, resolves fallback faces, shapes contextual runs, applies BiDi order, line breaks, clipping/ellipsis and permitted justification. Character bounds use shaped scalar clusters rather than ink boxes. Font revisions invalidate cached layout. Build leases every source face, repeats at most 64 times when a callback or concurrent replacement changes dependencies, and isolates nested queries from active cache entries. Drawing revalidates the built generation before recording and holds source leases through the complete draw. Retained canvas commands preserve immutable glyph-image snapshots after source retirement.

## Integration and verification

Word and locale-specific line boundaries come from the private ICU dictionary iterators; grapheme boundaries and BiDi processing use the pinned managed algorithms. Decoding also caches exact nonprinting classification, so unsupported graphic characters retain hexadecimal fallback boxes while missing control or unassigned glyphs do not acquire a synthetic advance.

Used through [Font](Font.md), [FontFile](FontFile.md) and canvas/Label consumers. These helpers do not add a public compatibility API. The [text component](../components/text.md#verification-boundaries) distinguishes source, managed, native pixel, allocation and platform evidence. [ADR 0046](../decisions/rendering.md#adr-0046) owns the backend and integration boundary.
