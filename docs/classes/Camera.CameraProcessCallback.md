# Camera.CameraProcessCallback

Last updated: 2026-09-23

- Declaration: `public enum Camera.CameraProcessCallback`
- Source: [Camera.cs](../../src/Scene/2D/Camera.cs)
- Owner: [Camera](Camera.md)

## Description

Inherited transform notifications can also refresh unsmoothed cameras between frames. Physics interpolation is not implemented.

## Values

| Name | Value | Meaning |
| --- | --- | --- |
| `Physics` | 0 | Use the internal physics notification lane. |
| `Idle` | 1 | Use the internal process notification lane. |

Undefined values throw ArgumentOutOfRangeException at the owning camera property. CameraTests verifies defaults and both valid modes.
