# TextOverrunFlags

Last updated: 2026-10-20

**Declaration:** `public enum TextOverrunFlags` with `[Flags]` · **Source:** [TextOverrunFlags.cs](../../src/Servers/Text/TextOverrunFlags.cs) · **Component:** [Text buffers](../components/text-server.md)

Controls explicit shaped-buffer trimming. [TextServer.ShapedTextOverrunTrimToWidth](TextServer.md#shapedtextoverruntrimtowidth) accepts combinations of the following bits and rejects undefined bits. NoTrim is the default.

## Enumeration values

| Member | Value | Meaning |
| --- | --- | --- |
| [NoTrim](#notrim) | `0` | Resets the current display to untrimmed raw geometry. |
| [Trim](#trim) | `1` | Enables width-dependent trimming. |
| [TrimWordOnly](#trimwordonly) | `2` | Prefers word opportunities, with cluster fallback below six retained records. |
| [AddEllipsis](#addellipsis) | `4` | Adds an ellipsis when the remaining length permits. |
| [EnforceEllipsis](#enforceellipsis) | `8` | Allows an ellipsis even when the line fits; combine with Trim and AddEllipsis. |
| [JustificationAware](#justificationaware) | `16` | Trims only after fitting cannot meet the target width. |
| [ShortStringEllipsis](#shortstringellipsis) | `32` | Allows ellipsis below six retained records. |

<a id="notrim"></a>
### NoTrim

`NoTrim = 0` — Resets the current display to untrimmed raw geometry.

<a id="trim"></a>
### Trim

`Trim = 1` — Enables width-dependent trimming.

<a id="trimwordonly"></a>
### TrimWordOnly

`TrimWordOnly = 2` — Prefers word opportunities, with cluster fallback below six retained records.

<a id="addellipsis"></a>
### AddEllipsis

`AddEllipsis = 4` — Adds an ellipsis when the remaining length permits.

<a id="enforceellipsis"></a>
### EnforceEllipsis

`EnforceEllipsis = 8` — Allows an ellipsis even when the line fits; combine with Trim and AddEllipsis.

<a id="justificationaware"></a>
### JustificationAware

`JustificationAware = 16` — Trims only after fitting cannot meet the target width.

<a id="shortstringellipsis"></a>
### ShortStringEllipsis

`ShortStringEllipsis = 32` — Allows ellipsis below six retained records.

## Use and validation

Trimming preserves original clusters and source/selection geometry. Widths are finite; nonpositive values disable trimming. The complete [formatting contract](../components/text-server.md#fitting-spacing-and-overrun) specifies minimum-width, short-result and missing-ellipsis behavior. TextServerFormattingTests and the four software-rendered formatting profiles verify actual consumers; hardware, native allocator and foreign-platform acceptance remain separate.
