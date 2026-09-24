# FillEnum

Last updated: 2026-09-24

- Declaration: `public enum FillEnum`
- Source: [GradientTexture.cs](../../src/Scene/Resources/GradientTexture.cs)
- Used by: [GradientTexture](GradientTexture.md)
- Component: [Gradients](../components/gradients.md)

## Description

UV axes normalize independently. Equal endpoints sample zero; a near-zero Linear line also samples zero. The namespace-level FillEnum type is selected by the Fill property.

## Values

| Name | Value | Meaning |
| --- | --- | --- |
| `Linear` | 0 | Signed projection onto the fill line. |
| `Radial` | 1 | Distance from FillFrom divided by fill radius. |
| `Square` | 2 | Maximum absolute axis distance divided by maximum axis span. |
| `Conic` | 3 | Wrapped signed angular turn from the fill direction. |

Undefined values throw ArgumentOutOfRangeException at the owning property boundary. [GradientTests](../../tests/Electron2D.Tests/GradientTests.cs) exercises every mode.
