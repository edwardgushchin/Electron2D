# DisplayServer.WindowResizeEdge

**Inherits** `System.Enum`

**Source** [DisplayServer.WindowGestures.cs](../../src/Servers/Display/DisplayServer.WindowGestures.cs)

**Declaration** `public enum WindowResizeEdge` nested in [DisplayServer](DisplayServer.md)

## Description

Selects the edge or corner the pointer moves during an interactive resize. Use it with [WindowStartResize](DisplayServer.md#interactive-window-gestures) or [StartResize](Window.md#interactive-window-gestures).

| Value | Meaning |
| --- | --- |
| `TopLeft = 0` | Top-left corner. |
| `Top = 1` | Top edge. |
| `TopRight = 2` | Top-right corner. |
| `Left = 3` | Left edge. |
| `Right = 4` | Right edge. |
| `BottomLeft = 5` | Bottom-left corner. |
| `Bottom = 6` | Bottom edge. |
| `BottomRight = 7` | Bottom-right corner. |
| `Max = 8` | Number of selectable values. |

Passing `Max` or any other value outside the selectable range raises `ArgumentOutOfRangeException`.
