# NativeShapedGlyph

Last updated: 2026-09-27

**Visibility:** internal · **Source:** [NativeFontPrecision.cs](../../src/Servers/Text/NativeFontPrecision.cs) · **Component:** [Text](../components/text.md)

## Responsibilities and invariants

Internal shaped glyph index, absolute scalar cluster, 26.6 advances/offsets and HarfBuzz safety flags. The layout layer owns conversion and rounding; native pointers do not escape.

## Integration and verification

Used through [Font](Font.md), [FontFile](FontFile.md) and canvas/Label consumers. These helpers do not add a public compatibility API. The [text component](../components/text.md#verification-boundaries) distinguishes source, managed, native pixel, allocation and platform evidence. [ADR 0046](../decisions/rendering.md#adr-0046) owns the backend and integration boundary.
