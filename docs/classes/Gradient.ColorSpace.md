# Gradient.ColorSpace

Last updated: 2026-09-23

- Declaration: `public enum Gradient.ColorSpace`
- Source: [Gradient.cs](../../src/Scene/Resources/Gradient.cs)
- Owner: [Gradient](Gradient.md)
- Component: [Gradients](../components/gradients.md)

## Description

Alpha is interpolated independently. Exact-point and held colors bypass conversion. Constant mode ignores this setting. Undefined values are rejected.

## Values

| Name | Value | Meaning |
| --- | --- | --- |
| `SRGB` | 0 | Interpolate stored nonlinear RGB. |
| `LinearSRGB` | 1 | Convert to linear RGB before interpolation and back afterward. |
| `OKLAB` | 2 | Convert through linear RGB to perceptual OKLAB and back. |

Undefined values throw ArgumentOutOfRangeException at the owning property boundary. [GradientTests](../../tests/Electron2D.Tests/GradientTests.cs) exercises every mode.
