# TextMissingGlyph

Last updated: 2026-09-27

**Visibility:** internal · **Source:** [TextMissingGlyph.cs](../../src/Servers/Text/TextMissingGlyph.cs) · **Component:** [Text](../components/text.md)

## Responsibilities and invariants

Computes and records hexadecimal missing-character boxes through ordinary canvas rectangles. This preserves codepoint identity and source box metrics when no fallback font covers a scalar, instead of rendering an unrelated glyph-zero shape. Retained geometry and warmed recording are checked in TextHelperTests.

## Integration and verification

Used through [Font](Font.md), [FontFile](FontFile.md) and canvas/Label consumers. These helpers do not add a public compatibility API. The [text component](../components/text.md#verification-boundaries) distinguishes source, managed, native pixel, allocation and platform evidence. [ADR 0046](../decisions/rendering.md#adr-0046) owns the backend and integration boundary.
