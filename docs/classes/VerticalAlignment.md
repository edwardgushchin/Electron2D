# VerticalAlignment

Last updated: 2026-09-27

**Declaration:** `public enum VerticalAlignment` · **Source:** [TextEnums.cs](../../src/Servers/Text/TextEnums.cs) · **Component:** [Text](../components/text.md)

Aligns text vertically inside a control.

## Enumeration values

| Member | Value | Meaning |
| --- | --- | --- |
| [Top](#top) | `0` | Aligns the first line to the top. |
| [Center](#center) | `1` | Centers the visible lines. |
| [Bottom](#bottom) | `2` | Aligns the last visible line to the bottom. |
| [Fill](#fill) | `3` | Distributes remaining height between visible lines. |

<a id="top"></a>
### Top

`Top = 0` — Aligns the first line to the top.

<a id="center"></a>
### Center

`Center = 1` — Centers the visible lines.

<a id="bottom"></a>
### Bottom

`Bottom = 2` — Aligns the last visible line to the bottom.

<a id="fill"></a>
### Fill

`Fill = 3` — Distributes remaining height between visible lines.

## Use and validation

Consumed by [Font](Font.md) and canvas text entrypoints. Values and defaults are part of the typed text contract; each consumer documents whether unknown bits or values are retained or rejected. [FontTests](../../tests/Electron2D.Tests/FontTests.cs) and the native text gate cover the implemented consumer behavior. The declaration does not imply that public TextServer services are implemented. See [ADR 0046](../decisions/rendering.md#adr-0046).
