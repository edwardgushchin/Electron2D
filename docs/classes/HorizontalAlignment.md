# HorizontalAlignment

Last updated: 2026-09-27

**Declaration:** `public enum HorizontalAlignment` · **Source:** [TextEnums.cs](../../src/Servers/Text/TextEnums.cs) · **Component:** [Text](../components/text.md)

Aligns content along a horizontal or text-advance axis.

## Enumeration values

| Member | Value | Meaning |
| --- | --- | --- |
| [Left](#left) | `0` | Aligns to the start of the available axis. |
| [Center](#center) | `1` | Centers in the available axis. |
| [Right](#right) | `2` | Aligns to the end of the available axis. |
| [Fill](#fill) | `3` | Expands eligible spacing to fill the available axis. |

<a id="left"></a>
### Left

`Left = 0` — Aligns to the start of the available axis.

<a id="center"></a>
### Center

`Center = 1` — Centers in the available axis.

<a id="right"></a>
### Right

`Right = 2` — Aligns to the end of the available axis.

<a id="fill"></a>
### Fill

`Fill = 3` — Expands eligible spacing to fill the available axis.

## Use and validation

Consumed by [Font](Font.md) and canvas text entrypoints. Values and defaults are part of the typed text contract; each consumer documents whether unknown bits or values are retained or rejected. [FontTests](../../tests/Electron2D.Tests/FontTests.cs) and the native text gate cover the implemented consumer behavior. The declaration does not imply that public TextServer services are implemented. See [ADR 0046](../decisions/rendering.md#adr-0046).
