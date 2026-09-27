# TextDirection

Last updated: 2026-09-27

**Declaration:** `public enum TextDirection` · **Source:** [TextEnums.cs](../../src/Servers/Text/TextEnums.cs) · **Component:** [Text](../components/text.md)

Selects a paragraph's base writing direction.

## Enumeration values

| Member | Value | Meaning |
| --- | --- | --- |
| [Auto](#auto) | `0` | Uses the first strong directional character, falling back to the text consumer's language policy when none is present. |
| [LTR](#ltr) | `1` | Uses left-to-right paragraph order. |
| [RTL](#rtl) | `2` | Uses right-to-left paragraph order. |
| [Inherited](#inherited) | `3` | Uses the consumer's inherited direction, or automatic direction without a consumer. |

<a id="auto"></a>
### Auto

`Auto = 0` — Uses the first strong directional character, falling back to the text consumer's language policy when none is present.

<a id="ltr"></a>
### LTR

`LTR = 1` — Uses left-to-right paragraph order.

<a id="rtl"></a>
### RTL

`RTL = 2` — Uses right-to-left paragraph order.

<a id="inherited"></a>
### Inherited

`Inherited = 3` — Uses the consumer's inherited direction, or automatic direction without a consumer.

## Use and validation

Consumed by [Font](Font.md) and canvas text entrypoints. Values and defaults are part of the typed text contract; each consumer documents whether unknown bits or values are retained or rejected. [FontTests](../../tests/Electron2D.Tests/FontTests.cs) and the native text gate cover the implemented consumer behavior. The declaration does not imply that public TextServer services are implemented. See [ADR 0046](../decisions/rendering.md#adr-0046).
