# ScreenSpaceAAMode

Last updated: 2026-11-12

- Declaration: `public enum ScreenSpaceAAMode`
- Source: [Viewport.ScreenSpaceAA.cs](../../src/Scene/Main/Viewport.ScreenSpaceAA.cs)
- Component: [Canvas screen-space antialiasing](../components/canvas-rendering.md#screen-space-antialiasing)

| Value | Number | Behavior |
| --- | ---: | --- |
| `Disabled` | 0 | Publishes the canvas without a screen-space filter. |
| `FXAA` | 1 | Filters high-contrast edges of the completed GPU image. |
| `SMAA` | 2 | Detects edges, calculates area weights and blends neighboring pixels on the GPU. |
| `Max` | 3 | Exclusive bound. Selection rejects this value. |

`Viewport.ScreenSpaceAA`, `RenderingServer.ViewportSetScreenSpaceAA` and
`ProjectSettings.ScreenSpaceAA` share this mode contract.
