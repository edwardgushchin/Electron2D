# FontHinting

Last updated: 2026-09-27

**Declaration:** `public enum FontHinting` · **Source:** [FontFile.cs](../../src/Scene/Resources/FontFile.cs)

Controls glyph raster fitting for [FontFile](FontFile.md). It is independent of the unhinted advance/offset policy used for shaping.

| Member | Value | Behavior |
| --- | ---: | --- |
| None | 0 | Disables outline hinting. |
| Light | 1 | Light vertical hinting; FontFile constructor and file-reset default. |
| Normal | 2 | Normal grid fitting. |

FontFile preserves unrecognized numeric values and renders them with normal fitting. Equal assignments are silent; changes invalidate font output. Native raster tests compare all three defined modes against the independent FreeType C oracle; see [NativeFontPrecision](NativeFontPrecision.md).
