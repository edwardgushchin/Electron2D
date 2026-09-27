# FontSubpixelPositioning

Last updated: 2026-09-27

**Declaration:** `public enum FontSubpixelPositioning` · **Source:** [FontFile.cs](../../src/Scene/Resources/FontFile.cs)

Controls advance rounding and horizontal raster phases for [FontFile](FontFile.md).

| Member | Value | Behavior |
| --- | ---: | --- |
| Disabled | 0 | Whole-pixel positioning; successful file-load/reset default. |
| Auto | 1 | Quarter-pixel raster phases through size 16, half-pixel phases through size 20, whole pixels above 20; constructor default. |
| Half | 2 | Half-pixel phases. |
| Quarter | 3 | Quarter-pixel phases. |
| HalfMaxSize | 20 | Largest font size at which Auto uses half-pixel phases; a size threshold, not a positioning mode. |
| QuarterMaxSize | 16 | Largest font size at which Auto uses quarter-pixel phases; a size threshold, not a positioning mode. |

FontFile retains raw values, including the size thresholds and unrecognized values, and uses whole pixels for them. Equal assignments are silent. KeepRoundingRemainders controls the separate residual carried while rounding advances. Half/quarter glyphs are actually rasterized at shifted outline phases; translating a phase-zero texture is insufficient. Raster size, oversampling and final canvas placement are integrated by the font drawing path; see [NativeFontPrecision](NativeFontPrecision.md) and [ADR 0046](../decisions/rendering.md#adr-0046).
