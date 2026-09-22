# Viewport.DefaultCanvasItemTextureFilter

Last updated: 2026-09-23

- Declaration: `public enum Viewport.DefaultCanvasItemTextureFilter`
- Source: [Viewport.Sampling.cs](../../src/Scene/Main/Viewport.Sampling.cs)
- Owner: [Viewport](Viewport.md)
- Component: [Canvas rendering](../components/canvas-rendering.md#texture-sampling)

## Description

Typed sampling choices used by Viewport. Values are stored by PackedScene. Max is a sentinel rejected by setters, as are negative and undefined values. See the owning properties for defaults, inheritance, lifetime and threading.

## Values

| Name | Value | Contract |
| --- | --- | --- |
| [`Nearest`](#nearest) | 0 | Nearest base-level texels. |
| [`Linear`](#linear) | 1 | Linear base-level texels; the default. |
| [`LinearWithMipmaps`](#linearwithmipmaps) | 2 | Linear texels with mip levels. |
| [`NearestWithMipmaps`](#nearestwithmipmaps) | 3 | Nearest texels with mip levels. |
| [`ParentNode`](#parentnode) | 4 | Inherits the direct canvas or viewport parent, otherwise linear. |
| [`Max`](#max) | 5 | Sentinel; not a valid choice. |

## Value descriptions

### Nearest

`Viewport.DefaultCanvasItemTextureFilter.Nearest = 0`

Nearest base-level texels.

### Linear

`Viewport.DefaultCanvasItemTextureFilter.Linear = 1`

Linear base-level texels; the default.

### LinearWithMipmaps

`Viewport.DefaultCanvasItemTextureFilter.LinearWithMipmaps = 2`

Linear texels with mip levels.

### NearestWithMipmaps

`Viewport.DefaultCanvasItemTextureFilter.NearestWithMipmaps = 3`

Nearest texels with mip levels.

### ParentNode

`Viewport.DefaultCanvasItemTextureFilter.ParentNode = 4`

Inherits the direct canvas or viewport parent, otherwise linear.

### Max

`Viewport.DefaultCanvasItemTextureFilter.Max = 5`

Sentinel; not a valid choice.

## Verification and limits

[CanvasSamplingTests](../../tests/Electron2D.Tests/CanvasSamplingTests.cs) verifies defaults, invalid values, inheritance, stored values and ownership guards. [Native sampling checks](../../tests/Electron2D.Tests/CanvasSamplingRenderingTests.cs) verify actual filtering, addressing and backend rejection. GPU supports mipmaps, mirror and anisotropy. Hardware compatibility supports nearest/linear and clamp/repeat; software triangle rendering supports nearest only. Unsupported modes throw before drawing rather than silently substituting a mode.
