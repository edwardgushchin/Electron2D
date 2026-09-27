# TextLineBreakFlags

Last updated: 2026-09-27

**Declaration:** `public enum TextLineBreakFlags` with `[Flags]` · **Source:** [TextEnums.cs](../../src/Servers/Text/TextEnums.cs) · **Component:** [Text](../components/text.md)

Controls mandatory and width-dependent text line breaks.

## Enumeration values

| Member | Value | Meaning |
| --- | --- | --- |
| [None](#none) | `0` | Does not break the text. |
| [Mandatory](#mandatory) | `1` | Breaks at mandatory Unicode separators. |
| [WordBound](#wordbound) | `2` | Allows Unicode word-boundary line breaks. |
| [GraphemeBound](#graphemebound) | `4` | Allows breaks at grapheme boundaries. |
| [Adaptive](#adaptive) | `8` | Falls back to grapheme boundaries when a word cannot fit. |
| [TrimEdgeSpaces](#trimedgespaces) | `16` | Trims both start and end spaces. |
| [TrimIndent](#trimindent) | `32` | Trims indentation after the initial line. |
| [TrimStartEdgeSpaces](#trimstartedgespaces) | `64` | Trims spaces at line starts. |
| [TrimEndEdgeSpaces](#trimendedgespaces) | `128` | Trims spaces at line ends. |

<a id="none"></a>
### None

`None = 0` — Does not break the text.

<a id="mandatory"></a>
### Mandatory

`Mandatory = 1` — Breaks at mandatory Unicode separators.

<a id="wordbound"></a>
### WordBound

`WordBound = 2` — Allows Unicode word-boundary line breaks.

<a id="graphemebound"></a>
### GraphemeBound

`GraphemeBound = 4` — Allows breaks at grapheme boundaries.

<a id="adaptive"></a>
### Adaptive

`Adaptive = 8` — Falls back to grapheme boundaries when a word cannot fit.

<a id="trimedgespaces"></a>
### TrimEdgeSpaces

`TrimEdgeSpaces = 16` — Trims both start and end spaces.

<a id="trimindent"></a>
### TrimIndent

`TrimIndent = 32` — Trims indentation after the initial line.

<a id="trimstartedgespaces"></a>
### TrimStartEdgeSpaces

`TrimStartEdgeSpaces = 64` — Trims spaces at line starts.

<a id="trimendedgespaces"></a>
### TrimEndEdgeSpaces

`TrimEndEdgeSpaces = 128` — Trims spaces at line ends.

## Use and validation

Consumed by [Font](Font.md) and canvas text entrypoints. Values and defaults are part of the typed text contract; each consumer documents whether unknown bits or values are retained or rejected. [FontTests](../../tests/Electron2D.Tests/FontTests.cs) and the native text gate cover the implemented consumer behavior. The declaration does not imply that public TextServer services are implemented. See [ADR 0046](../decisions/rendering.md#adr-0046).
