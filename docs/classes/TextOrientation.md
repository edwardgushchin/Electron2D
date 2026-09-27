# TextOrientation

Last updated: 2026-09-27

**Declaration:** `public enum TextOrientation` · **Source:** [TextEnums.cs](../../src/Servers/Text/TextEnums.cs) · **Component:** [Text](../components/text.md)

Selects the axis along which glyphs advance.

## Enumeration values

| Member | Value | Meaning |
| --- | --- | --- |
| [Horizontal](#horizontal) | `0` | Glyphs advance horizontally. |
| [Vertical](#vertical) | `1` | Glyphs advance vertically using vertical font metrics and substitutions. |

<a id="horizontal"></a>
### Horizontal

`Horizontal = 0` — Glyphs advance horizontally.

<a id="vertical"></a>
### Vertical

`Vertical = 1` — Glyphs advance vertically using vertical font metrics and substitutions.

## Use and validation

Consumed by [Font](Font.md) and canvas text entrypoints. Values and defaults are part of the typed text contract; each consumer documents whether unknown bits or values are retained or rejected. [FontTests](../../tests/Electron2D.Tests/FontTests.cs) and the native text gate cover the implemented consumer behavior. The declaration does not imply that public TextServer services are implemented. See [ADR 0046](../decisions/rendering.md#adr-0046).
