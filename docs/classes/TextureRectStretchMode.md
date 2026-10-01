# TextureRectStretchMode

Last updated: 2026-10-01

**Declaration:** `public enum TextureRectStretchMode` · **Source:** [TextureRect.cs](../../src/Scene/GUI/TextureRect.cs). Consumed by [TextureRect.StretchMode](TextureRect.md#stretchmode).

## Enumeration descriptions

| Value | Placement |
| --- | --- |
| `Scale = 0` | Stretch to the full control rectangle; constructor default. |
| `Tile = 1` | Repeat natural logical pixels; nested atlases reuse zero-border nine-patch geometry. Nonzero atlas margins are unsupported and warn. |
| `Keep = 2` | Natural size at the top-left corner. |
| `KeepCentered = 3` | Natural size centered inside the control. |
| `KeepAspect = 4` | Integer-truncated aspect fit at the top-left corner. |
| `KeepAspectCentered = 5` | The same fit with potentially fractional centering offsets. |
| `KeepAspectCovered = 6` | Cover the control through a centered source crop, preserving aspect. |

Undefined values reject before assignment. Empty natural dimensions draw nothing. FlipH/FlipV reflect inside the resulting destination; inherited clipping applies. Keep modes may exceed the control's size. See the [control verification and limits](TextureRect.md#verification-and-limits).
