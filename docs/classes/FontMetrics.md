# FontMetrics

Last updated: 2026-09-27

**Visibility:** internal · **Source:** [FontData.cs](../../src/Servers/Text/FontData.cs) · **Component:** [Text](../components/text.md)

## Responsibilities and invariants

Immutable ascent, descent, total height, underline position and thickness snapshot in logical pixels. The font size belongs to the containing cache key. It is not a public native structure.

## Integration and verification

Used through [Font](Font.md), [FontFile](FontFile.md) and canvas/Label consumers. These helpers do not add a public compatibility API. The [text component](../components/text.md#verification-boundaries) distinguishes source, managed, native pixel, allocation and platform evidence. [ADR 0046](../decisions/rendering.md#adr-0046) owns the backend and integration boundary.
