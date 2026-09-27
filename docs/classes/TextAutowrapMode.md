# TextAutowrapMode

Last updated: 2026-09-27

**Declaration:** `public enum TextAutowrapMode` · **Source:** [TextEnums.cs](../../src/Servers/Text/TextEnums.cs) · **Component:** [Text](../components/text.md)

Selects width-dependent label wrapping.

## Enumeration values

| Member | Value | Meaning |
| --- | --- | --- |
| [Off](#off) | `0` | Uses only explicit line separators. |
| [Arbitrary](#arbitrary) | `1` | Allows breaks at grapheme boundaries. |
| [Word](#word) | `2` | Allows Unicode word-boundary line breaks. |
| [WordSmart](#wordsmart) | `3` | Uses word boundaries and falls back to grapheme boundaries for oversized words. |

<a id="off"></a>
### Off

`Off = 0` — Uses only explicit line separators.

<a id="arbitrary"></a>
### Arbitrary

`Arbitrary = 1` — Allows breaks at grapheme boundaries.

<a id="word"></a>
### Word

`Word = 2` — Allows Unicode word-boundary line breaks.

<a id="wordsmart"></a>
### WordSmart

`WordSmart = 3` — Uses word boundaries and falls back to grapheme boundaries for oversized words.

## Use and validation

Consumed by [Font](Font.md) and canvas text entrypoints. Values and defaults are part of the typed text contract; each consumer documents whether unknown bits or values are retained or rejected. [FontTests](../../tests/Electron2D.Tests/FontTests.cs) and the native text gate cover the implemented consumer behavior. The declaration does not imply that public TextServer services are implemented. See [ADR 0046](../decisions/rendering.md#adr-0046).
