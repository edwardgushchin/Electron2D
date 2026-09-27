# StructuredTextParser

Last updated: 2026-09-27

**Declaration:** `public enum StructuredTextParser` · **Source:** [TextEnums.cs](../../src/Servers/Text/TextEnums.cs) · **Component:** [Text](../components/text.md)

Selects independent bidirectional contexts for structured strings.

## Enumeration values

| Member | Value | Meaning |
| --- | --- | --- |
| [Default](#default) | `0` | Uses one inherited paragraph context. |
| [URI](#uri) | `1` | Separates URI components and gives separators a left-to-right context. |
| [File](#file) | `2` | Separates file-path components. |
| [Email](#email) | `3` | Separates the address local part and dotted domain components. |
| [List](#list) | `4` | Separates list fields using one supplied string delimiter. |
| [Custom](#custom) | `6` | Invokes the control's typed custom parser. |

<a id="default"></a>
### Default

`Default = 0` — Uses one inherited paragraph context.

<a id="uri"></a>
### URI

`URI = 1` — Separates URI components and gives separators a left-to-right context.

<a id="file"></a>
### File

`File = 2` — Separates file-path components.

<a id="email"></a>
### Email

`Email = 3` — Separates the address local part and dotted domain components.

<a id="list"></a>
### List

`List = 4` — Separates list fields using one supplied string delimiter.

<a id="custom"></a>
### Custom

`Custom = 6` — Invokes the control's typed custom parser.

## Use and validation

Consumed by [Font](Font.md) and canvas text entrypoints. Values and defaults are part of the typed text contract; each consumer documents whether unknown bits or values are retained or rejected. [FontTests](../../tests/Electron2D.Tests/FontTests.cs) and the native text gate cover the implemented consumer behavior. The declaration does not imply that public TextServer services are implemented. See [ADR 0046](../decisions/rendering.md#adr-0046).
