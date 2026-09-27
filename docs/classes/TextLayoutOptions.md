# TextLayoutOptions

Last updated: 2026-09-27

**Visibility:** internal · **Source:** [TextLayout.cs](../../src/Servers/Text/TextLayout.cs) · **Component:** [Text](../components/text.md)

## Responsibilities and invariants

Internal consumer-specific layout inputs such as spacing, language, tab stops, overrun, visibility and structured BiDi context. Arrays are retained only within the owning consumer/cache lifetime. It is not a second public TextParagraph API.

## Integration and verification

Used through [Font](Font.md), [FontFile](FontFile.md) and canvas/Label consumers. These helpers do not add a public compatibility API. The [text component](../components/text.md#verification-boundaries) distinguishes source, managed, native pixel, allocation and platform evidence. [ADR 0046](../decisions/rendering.md#adr-0046) owns the backend and integration boundary.
