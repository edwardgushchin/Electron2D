# TextOverrunBehavior

Last updated: 2026-09-27

**Declaration:** `public enum TextOverrunBehavior` · **Source:** [TextEnums.cs](../../src/Servers/Text/TextEnums.cs) · **Component:** [Text](../components/text.md)

Controls trimming of text that exceeds its available width.

## Enumeration values

| Member | Value | Meaning |
| --- | --- | --- |
| [NoTrimming](#notrimming) | `0` | Preserves the complete text. |
| [TrimChar](#trimchar) | `1` | Removes complete character clusters. |
| [TrimWord](#trimword) | `2` | Removes complete words. |
| [TrimEllipsis](#trimellipsis) | `3` | Trims clusters and adds an ellipsis when at least six glyphs remain. |
| [TrimWordEllipsis](#trimwordellipsis) | `4` | Trims words and adds an ellipsis when at least six glyphs remain. |
| [TrimEllipsisForce](#trimellipsisforce) | `5` | Always includes the ellipsis while trimming clusters. |
| [TrimWordEllipsisForce](#trimwordellipsisforce) | `6` | Always includes the ellipsis while trimming words. |

<a id="notrimming"></a>
### NoTrimming

`NoTrimming = 0` — Preserves the complete text.

<a id="trimchar"></a>
### TrimChar

`TrimChar = 1` — Removes complete character clusters.

<a id="trimword"></a>
### TrimWord

`TrimWord = 2` — Removes complete words.

<a id="trimellipsis"></a>
### TrimEllipsis

`TrimEllipsis = 3` — Trims clusters and adds an ellipsis when at least six glyphs remain.

<a id="trimwordellipsis"></a>
### TrimWordEllipsis

`TrimWordEllipsis = 4` — Trims words and adds an ellipsis when at least six glyphs remain.

<a id="trimellipsisforce"></a>
### TrimEllipsisForce

`TrimEllipsisForce = 5` — Always includes the ellipsis while trimming clusters.

<a id="trimwordellipsisforce"></a>
### TrimWordEllipsisForce

`TrimWordEllipsisForce = 6` — Always includes the ellipsis while trimming words.

## Use and validation

Consumed by [Font](Font.md) and canvas text entrypoints. Values and defaults are part of the typed text contract; each consumer documents whether unknown bits or values are retained or rejected. [FontTests](../../tests/Electron2D.Tests/FontTests.cs) and the native text gate cover the implemented consumer behavior. The declaration does not imply that public TextServer services are implemented. See [ADR 0046](../decisions/rendering.md#adr-0046).
