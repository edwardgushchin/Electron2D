# TextLayoutKey

Last updated: 2026-09-27

**Visibility:** internal · **Source:** [TextLayout.cs](../../src/Servers/Text/TextLayout.cs) · **Component:** [Text](../components/text.md)

## Responsibilities and invariants

Immutable cache identity for text, size, width, alignment, line limit, break/justification flags, direction, orientation and single-line versus paragraph mode. It contains no native handles.

## Integration and verification

Used through [Font](Font.md), [FontFile](FontFile.md) and canvas/Label consumers. These helpers do not add a public compatibility API. The [text component](../components/text.md#verification-boundaries) distinguishes source, managed, native pixel, allocation and platform evidence. [ADR 0046](../decisions/rendering.md#adr-0046) owns the backend and integration boundary.
