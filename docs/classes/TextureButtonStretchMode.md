# TextureButtonStretchMode

Last updated: 2026-09-27

**Kind:** Enum · **Source:** [TextureButton.cs](../../src/Scene/GUI/TextureButton.cs) · **Consumer:** [TextureButton](TextureButton.md)

Controls how a texture button fits its selected image to the control rectangle.

| Value | Number | Placement |
| --- | --- | --- |
| Scale | 0 | Stretches to the control rectangle. |
| Tile | 1 | Repeats at natural pixel size. |
| Keep | 2 | Keeps natural size at the top-left corner; the default. |
| KeepCentered | 3 | Centers the natural-size image. |
| KeepAspect | 4 | Fits without changing aspect ratio, aligned to the top-left. |
| KeepAspectCentered | 5 | Fits without changing aspect ratio and centers the result. |
| KeepAspectCovered | 6 | Covers the control with a centered source crop preserving aspect ratio. |

Undefined integer identities remain stored by TextureButton and use natural top-left placement. The focus overlay uses the full focused image stretched over the selected destination. TextureButton's bitmap mapping and visual-only flips are documented on its class page.
