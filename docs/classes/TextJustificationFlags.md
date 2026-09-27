# TextJustificationFlags

Last updated: 2026-09-27

**Declaration:** `public enum TextJustificationFlags` with `[Flags]` · **Source:** [TextEnums.cs](../../src/Servers/Text/TextEnums.cs) · **Component:** [Text](../components/text.md)

Controls how a line expands to its requested width.

## Enumeration values

| Member | Value | Meaning |
| --- | --- | --- |
| [None](#none) | `0` | Does not expand glyph spacing. |
| [Kashida](#kashida) | `1` | Allows joining-script elongation. |
| [WordBound](#wordbound) | `2` | Allows expansion at word boundaries. |
| [TrimEdgeSpaces](#trimedgespaces) | `4` | Excludes spaces at line edges. |
| [AfterLastTab](#afterlasttab) | `8` | Expands only content following the last tab. |
| [ConstrainEllipsis](#constrainellipsis) | `16` | Preserves space occupied by an ellipsis. |
| [SkipLastLine](#skiplastline) | `32` | Does not justify the paragraph's final line. |
| [SkipLastLineWithVisibleChars](#skiplastlinewithvisiblechars) | `64` | Does not justify its last line containing visible content. |
| [DoNotSkipSingleLine](#donotskipsingleline) | `128` | Justifies a one-line paragraph even when final lines are otherwise skipped. |

<a id="none"></a>
### None

`None = 0` — Does not expand glyph spacing.

<a id="kashida"></a>
### Kashida

`Kashida = 1` — Allows joining-script elongation.

<a id="wordbound"></a>
### WordBound

`WordBound = 2` — Allows expansion at word boundaries.

<a id="trimedgespaces"></a>
### TrimEdgeSpaces

`TrimEdgeSpaces = 4` — Excludes spaces at line edges.

<a id="afterlasttab"></a>
### AfterLastTab

`AfterLastTab = 8` — Expands only content following the last tab.

<a id="constrainellipsis"></a>
### ConstrainEllipsis

`ConstrainEllipsis = 16` — Preserves space occupied by an ellipsis.

<a id="skiplastline"></a>
### SkipLastLine

`SkipLastLine = 32` — Does not justify the paragraph's final line.

<a id="skiplastlinewithvisiblechars"></a>
### SkipLastLineWithVisibleChars

`SkipLastLineWithVisibleChars = 64` — Does not justify its last line containing visible content.

<a id="donotskipsingleline"></a>
### DoNotSkipSingleLine

`DoNotSkipSingleLine = 128` — Justifies a one-line paragraph even when final lines are otherwise skipped.

## Use and validation

Consumed by [Font](Font.md) and canvas text entrypoints. Values and defaults are part of the typed text contract; each consumer documents whether unknown bits or values are retained or rejected. [FontTests](../../tests/Electron2D.Tests/FontTests.cs) and the native text gate cover the implemented consumer behavior. The declaration does not imply that public TextServer services are implemented. See [ADR 0046](../decisions/rendering.md#adr-0046).
