# InterpolationMode

Last updated: 2026-09-24

- Declaration: `public enum InterpolationMode`
- Source: [Gradient.cs](../../src/Scene/Resources/Gradient.cs)
- Used by: [Gradient](Gradient.md)
- Component: [Gradients](../components/gradients.md)

## Description

Changing the property emits Changed then PropertyListChanged; equal assignment is silent.

## Values

| Name | Value | Meaning |
| --- | --- | --- |
| `Linear` | 0 | Linear adjacent-color interpolation. |
| `Constant` | 1 | Hold the preceding color until the next exact point; ignore color space. |
| `Cubic` | 2 | Catmull–Rom interpolation with duplicated endpoint neighbors; may overshoot. |

Undefined values throw ArgumentOutOfRangeException at the owning property boundary. [GradientTests](../../tests/Electron2D.Tests/GradientTests.cs) exercises every mode.
