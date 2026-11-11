# CanvasEnvironment.GlowBlendMode

`GlowBlendMode` selects how a [CanvasEnvironment](CanvasEnvironment.md) combines the blurred RGB values with the original HDR canvas.

| Value | Name | Operation |
| ---: | --- | --- |
| 0 | `Additive` | Adds glow to the source color. |
| 1 | `Screen` | Blends by multiplying the inverse source and glow colors. |
| 2 | `SoftLight` | Applies a soft-light curve to the source color. |
| 3 | `Replace` | Uses glow RGB and preserves source alpha. |
| 4 | `Mix` | Interpolates between source RGB and glow RGB using `GlowMix`. |
