# NativeFontFeature

Last updated: 2026-09-27

**Visibility:** internal · **Source:** [NativeFontPrecision.cs](../../src/Servers/Text/NativeFontPrecision.cs) · **Component:** [Text](../components/text.md)

## Responsibilities and invariants

Internal OpenType tag, value and scalar-range arguments for shaping. The fixed layout matches the public HarfBuzz C structure; FontFile compiles typed feature dictionaries into a cached array.

## Integration and verification

Used through [Font](Font.md), [FontFile](FontFile.md) and canvas/Label consumers. These helpers do not add a public compatibility API. The [text component](../components/text.md#verification-boundaries) distinguishes source, managed, native pixel, allocation and platform evidence. [ADR 0046](../decisions/rendering.md#adr-0046) owns the backend and integration boundary.
