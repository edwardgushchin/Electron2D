# CanvasEnvironment.ToneMapper

Select a tone operator on [CanvasEnvironment](CanvasEnvironment.md) or pass it to `RenderingServer.EnvironmentSetTonemap`.

| Value | Operator | Behavior |
| ---: | --- | --- |
| 0 | `Linear` | Applies exposure without highlight compression. |
| 1 | `Reinhard` | Uses the extended Reinhard curve and output-aware white point. |
| 2 | `Filmic` | Applies the filmic curve with an input white reference. |
| 3 | `ACES` | Applies fitted ACES color transforms and curve with an input white reference. |
| 4 | `AGX` | Applies AgX color transforms with a variable-range tone curve and independent contrast. |

See [canvas tone mapping](../components/canvas-tonemapping.md) for output and verification details.
