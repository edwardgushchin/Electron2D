# GradientTexture.RepeatEnum

Last updated: 2026-09-23

- Declaration: `public enum GradientTexture.RepeatEnum`
- Source: [GradientTexture.cs](../../src/Scene/Resources/GradientTexture.cs)
- Owner: [GradientTexture](GradientTexture.md)
- Component: [Gradients](../components/gradients.md)

## Description

Operates on generated offsets, independently of the canvas sampler. The Enum suffix avoids collision with the Repeat property.

## Values

| Name | Value | Meaning |
| --- | --- | --- |
| `None` | 0 | Clamp to the unit interval. |
| `Repeat` | 1 | Modulo-one repetition, including negative offsets. |
| `Mirror` | 2 | Absolute modulo-two repetition, reflecting the second half. |

Undefined values throw ArgumentOutOfRangeException at the owning property boundary. [GradientTests](../../tests/Electron2D.Tests/GradientTests.cs) exercises every mode.
