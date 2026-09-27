# TextSpacingType

Last updated: 2026-09-27

**Declaration:** `public enum TextSpacingType` · **Source:** [TextEnums.cs](../../src/Servers/Text/TextEnums.cs) · **Component:** [Text](../components/text.md)

Identifies additional spacing applied by a font.

## Enumeration values

| Member | Value | Meaning |
| --- | --- | --- |
| [Glyph](#glyph) | `0` | Spacing after ordinary glyphs. |
| [Space](#space) | `1` | Spacing after word spaces. |
| [Top](#top) | `2` | Spacing above the baseline's ascent. |
| [Bottom](#bottom) | `3` | Spacing below the baseline's descent. |
| [Max](#max) | `4` | Number of spacing categories. |

<a id="glyph"></a>
### Glyph

`Glyph = 0` — Spacing after ordinary glyphs.

<a id="space"></a>
### Space

`Space = 1` — Spacing after word spaces.

<a id="top"></a>
### Top

`Top = 2` — Spacing above the baseline's ascent.

<a id="bottom"></a>
### Bottom

`Bottom = 3` — Spacing below the baseline's descent.

<a id="max"></a>
### Max

`Max = 4` — Number of spacing categories.

## Use and validation

Consumed by [Font](Font.md) and canvas text entrypoints. Values and defaults are part of the typed text contract; each consumer documents whether unknown bits or values are retained or rejected. [FontTests](../../tests/Electron2D.Tests/FontTests.cs) and the native text gate cover the implemented consumer behavior. The declaration does not imply that public TextServer services are implemented. See [ADR 0046](../decisions/rendering.md#adr-0046).
