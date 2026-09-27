# NativeTextDirection

Last updated: 2026-09-27

**Visibility:** internal · **Source:** [NativeFontPrecision.cs](../../src/Servers/Text/NativeFontPrecision.cs) · **Component:** [Text](../components/text.md)

## Responsibilities and invariants

Internal HarfBuzz direction identifiers used by the C ABI bridge. They never appear in public signatures; public TextDirection and TextOrientation select the appropriate native direction.

## Integration and verification

Used through [Font](Font.md), [FontFile](FontFile.md) and canvas/Label consumers. These helpers do not add a public compatibility API. The [text component](../components/text.md#verification-boundaries) distinguishes source, managed, native pixel, allocation and platform evidence. [ADR 0046](../decisions/rendering.md#adr-0046) owns the backend and integration boundary.
