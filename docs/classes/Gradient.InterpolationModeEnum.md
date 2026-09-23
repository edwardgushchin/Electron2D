# Gradient.InterpolationModeEnum

Last updated: 2026-09-23

- Declaration: `public enum Gradient.InterpolationModeEnum`
- Source: [Gradient.cs](../../src/Scene/Resources/Gradient.cs)
- Owner: [Gradient](Gradient.md)
- Component: [Gradients](../components/gradients.md)

## Description

The Enum suffix avoids a C# collision with InterpolationMode. Changing the property emits Changed then PropertyListChanged; equal assignment is silent.

## Values

| Name | Value | Meaning |
| --- | --- | --- |
| `Linear` | 0 | Linear adjacent-color interpolation. |
| `Constant` | 1 | Hold the preceding color until the next exact point; ignore color space. |
| `Cubic` | 2 | Catmull–Rom interpolation with duplicated endpoint neighbors; may overshoot. |

Undefined values throw ArgumentOutOfRangeException at the owning property boundary. [GradientTests](../../tests/Electron2D.Tests/GradientTests.cs) exercises every mode.
