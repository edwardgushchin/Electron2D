# FontStyle

Last updated: 2026-09-27

**Declaration:** `public enum FontStyle` with `[Flags]` · **Source:** [TextEnums.cs](../../src/Servers/Text/TextEnums.cs) · **Component:** [Text](../components/text.md)

Describes a font face's intrinsic style.

## Enumeration values

| Member | Value | Meaning |
| --- | --- | --- |
| [None](#none) | `0` | A regular proportional face. |
| [Bold](#bold) | `1` | A bold face. |
| [Italic](#italic) | `2` | An italic or oblique face. |
| [FixedWidth](#fixedwidth) | `4` | A monospaced face. |

<a id="none"></a>
### None

`None = 0` — A regular proportional face.

<a id="bold"></a>
### Bold

`Bold = 1` — A bold face.

<a id="italic"></a>
### Italic

`Italic = 2` — An italic or oblique face.

<a id="fixedwidth"></a>
### FixedWidth

`FixedWidth = 4` — A monospaced face.

## Use and validation

Consumed by [Font](Font.md) and canvas text entrypoints. Values and defaults are part of the typed text contract; each consumer documents whether unknown bits or values are retained or rejected. [FontTests](../../tests/Electron2D.Tests/FontTests.cs) and the native text gate cover the implemented consumer behavior. The declaration does not imply that public TextServer services are implemented. See [ADR 0046](../decisions/rendering.md#adr-0046).
