# Camera.AnchorModeEnum

Last updated: 2026-09-23

- Declaration: `public enum Camera.AnchorModeEnum`
- Source: [Camera.cs](../../src/Scene/2D/Camera.cs)
- Owner: [Camera](Camera.md)

## Description

The Enum suffix avoids the C# name collision with Camera.AnchorMode.

## Values

| Name | Value | Meaning |
| --- | --- | --- |
| `FixedTopLeft` | 0 | Tracks the top-left anchor; drag margins are not used. |
| `DragCenter` | 1 | Centers the screen and supports drag margins and offsets. |

Undefined values throw ArgumentOutOfRangeException at the owning camera property. CameraTests verifies defaults and both valid modes.
