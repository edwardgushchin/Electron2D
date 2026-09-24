# AnchorMode

Last updated: 2026-09-24

- Declaration: `public enum AnchorMode`
- Source: [Camera.cs](../../src/Scene/2D/Camera.cs)
- Used by: [Camera](Camera.md)

## Description

The namespace-level AnchorMode type is selected by Camera.AnchorMode.

## Values

| Name | Value | Meaning |
| --- | --- | --- |
| `FixedTopLeft` | 0 | Tracks the top-left anchor; drag margins are not used. |
| `DragCenter` | 1 | Centers the screen and supports drag margins and offsets. |

Undefined values throw ArgumentOutOfRangeException at the owning camera property. CameraTests verifies defaults and both valid modes.
