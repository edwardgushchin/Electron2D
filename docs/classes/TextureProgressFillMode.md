# TextureProgressFillMode

Last updated: 2026-09-26

**Source:** [TextureProgressBar.cs](../../src/Scene/GUI/TextureProgressBar.cs) · **Owner property:** [TextureProgressBar.FillMode](TextureProgressBar.md#fillmode)

| Value | Integer | Progress direction |
| --- | ---: | --- |
| `LeftToRight` | 0 | Left edge inward. |
| `RightToLeft` | 1 | Right edge inward. |
| `TopToBottom` | 2 | Top edge inward. |
| `BottomToTop` | 3 | Bottom edge inward. |
| `Clockwise` | 4 | Clockwise radial from initial angle. |
| `CounterClockwise` | 5 | Counterclockwise radial. |
| `BilinearLeftAndRight` | 6 | Centered horizontal expansion. |
| `BilinearTopAndBottom` | 7 | Centered vertical expansion. |
| `ClockwiseAndCounterClockwise` | 8 | Equal radial expansion in both directions. |

Default is LeftToRight. The namespace enum avoids a C# property/nested-type naming collision. Every policy has native fill-mask checks in [TextureProgressRenderingTests](../../tests/Electron2D.Tests/TextureProgressRenderingTests.cs), under [ADR 0080](../decisions/rendering.md#adr-0080).
