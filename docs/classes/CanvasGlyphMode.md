# CanvasGlyphMode

Last updated: 2026-10-14

- Declaration: `internal enum CanvasGlyphMode`
- Source: [CanvasGlyph.cs](../../src/Servers/Rendering/CanvasGlyph.cs)
- Component: [canvas rendering](../components/canvas-rendering.md#lcd-and-msdf-texture-regions)
- Visibility: internal.

| Value | Meaning |
| --- | --- |
| None = 0 | Ordinary geometry/material sampling. |
| MSDF = 2 | Median RGB distances, derivative range and optional alpha-min outline. |
| LCD = 3 | Independent RGB coverage and exact sampled-alpha gate. |

Values are private vertex/shader encodings. They are not public enums or stored scene properties. CanvasGlyphTests checks the connected recording, batch and native consumer paths.
