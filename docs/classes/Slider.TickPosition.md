# Slider.TickPosition

Last updated: 2026-09-27

**Declaration:** `public enum Slider.TickPosition` · **Source:** [Slider.cs](../../src/Scene/GUI/Slider.cs) · **Owner:** [Slider](Slider.md)

Places tick icons relative to the themed track; it does not change the range value or minimum size.

| Value | Number | HSlider | VSlider |
| --- | --- | --- | --- |
| `BottomRight` | `0` | Below the track. | Right of the track. |
| `TopLeft` | `1` | Above the track, vertically mirrored. | Left of the track, horizontally mirrored. |
| `Both` | `2` | Both sides. | Both sides. |
| `Center` | `3` | Across the center. | Across the center. |

```csharp
using var slider = new HSlider { TickCount = 5, TicksPosition = Slider.TickPosition.Both };
```

<a id="bottomright"></a><a id="topleft"></a><a id="both"></a><a id="center"></a>
The enum is not a bitmask. Undefined integer casts are retained by TicksPosition and select no tick-drawing branch, following the accepted source behavior. Counts at most one also draw no ticks. TicksOnBorders determines whether the first and last positions are included; the theme's signed tick_offset shifts each side in its corresponding direction. See [ADR 0080](../decisions/rendering.md#adr-0080).
