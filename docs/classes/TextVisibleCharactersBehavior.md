# TextVisibleCharactersBehavior

Last updated: 2026-09-27

**Declaration:** `public enum TextVisibleCharactersBehavior` · **Source:** [TextEnums.cs](../../src/Servers/Text/TextEnums.cs) · **Component:** [Text](../components/text.md)

Selects whether partial text is limited before shaping or during glyph recording.

## Enumeration values

| Member | Value | Meaning |
| --- | --- | --- |
| [CharsBeforeShaping](#charsbeforeshaping) | `0` | Shapes only the visible logical characters. |
| [CharsAfterShaping](#charsaftershaping) | `1` | Shapes complete text, then hides clusters beyond the logical character limit. |
| [GlyphsAuto](#glyphsauto) | `2` | Shows a proportion of glyphs in the control's layout direction. |
| [GlyphsLTR](#glyphsltr) | `3` | Shows glyphs from left to right. |
| [GlyphsRTL](#glyphsrtl) | `4` | Shows glyphs from right to left. |

<a id="charsbeforeshaping"></a>
### CharsBeforeShaping

`CharsBeforeShaping = 0` — Shapes only the visible logical characters.

<a id="charsaftershaping"></a>
### CharsAfterShaping

`CharsAfterShaping = 1` — Shapes complete text, then hides clusters beyond the logical character limit.

<a id="glyphsauto"></a>
### GlyphsAuto

`GlyphsAuto = 2` — Shows a proportion of glyphs in the control's layout direction.

<a id="glyphsltr"></a>
### GlyphsLTR

`GlyphsLTR = 3` — Shows glyphs from left to right.

<a id="glyphsrtl"></a>
### GlyphsRTL

`GlyphsRTL = 4` — Shows glyphs from right to left.

## Use and validation

Consumed by [Font](Font.md) and canvas text entrypoints. Values and defaults are part of the typed text contract; each consumer documents whether unknown bits or values are retained or rejected. [FontTests](../../tests/Electron2D.Tests/FontTests.cs) and the native text gate cover the implemented consumer behavior. The declaration does not imply that public TextServer services are implemented. See [ADR 0046](../decisions/rendering.md#adr-0046).
