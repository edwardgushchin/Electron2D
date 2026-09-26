# StyleBoxTexture.AxisStretchMode

Last updated: 2026-09-27

**Declaration:** `public enum StyleBoxTexture.AxisStretchMode` · **Source:** [StyleBoxTexture.cs](../../src/Scene/Resources/StyleBoxTexture.cs) · **Owner:** [StyleBoxTexture](StyleBoxTexture.md)

Controls how each axis fills the middle region between fixed texture borders. Horizontal and vertical policies are independent and default to Stretch.

| Value | Numeric identity | Behavior |
| --- | --- | --- |
| `Stretch` | `0` | Stretches the source middle region to fit. |
| `Tile` | `1` | Repeats at source scale; clips the final partial tile. |
| `TileFit` | `2` | Repeats a whole number of tiles with adjusted tile scale. |

```csharp
using var style = new StyleBoxTexture
{
    AxisStretchHorizontal = StyleBoxTexture.AxisStretchMode.Tile,
    AxisStretchVertical = StyleBoxTexture.AxisStretchMode.TileFit
};
```

<a id="stretch"></a><a id="tile"></a><a id="tilefit"></a>
Undefined enum values are rejected by style setters before mutation. The modes reuse the retained nine-patch geometry contract; an empty middle region, tight destination and atlas source handling are owned by that geometry rather than introducing another texture renderer. DrawCenter controls only the center cell, independent of these axis policies. See [ADR 0082](../decisions/rendering.md#adr-0082).
