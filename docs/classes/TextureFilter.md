# TextureFilter

Last updated: 2026-09-24

- Declaration: `public enum TextureFilter`
- Source: [CanvasItem.Sampling.cs](../../src/Scene/Main/CanvasItem.Sampling.cs)
- Used by: [CanvasItem](CanvasItem.md)
- Component: [Canvas rendering](../components/canvas-rendering.md#texture-sampling)

## Description

Typed sampling choices used by CanvasItem. Values are stored by PackedScene. Max is a sentinel rejected by setters, as are negative and undefined values. See the owning properties for defaults, inheritance, lifetime and threading.

## Values

| Name | Value | Contract |
| --- | --- | --- |
| [`ParentNode`](#parentnode) | 0 | Inherits the direct canvas parent, or the containing viewport default. |
| [`Nearest`](#nearest) | 1 | Samples the nearest base-level texel. |
| [`Linear`](#linear) | 2 | Interpolates neighboring base-level texels. |
| [`NearestWithMipmaps`](#nearestwithmipmaps) | 3 | Uses nearest texels with mip levels for minification. |
| [`LinearWithMipmaps`](#linearwithmipmaps) | 4 | Uses linear texel filtering with mip levels for minification. |
| [`NearestWithMipmapsAnisotropic`](#nearestwithmipmapsanisotropic) | 5 | Uses nearest texels, mip levels and viewport-controlled anisotropy. |
| [`LinearWithMipmapsAnisotropic`](#linearwithmipmapsanisotropic) | 6 | Uses linear texel filtering, mip levels and viewport-controlled anisotropy. |
| [`Max`](#max) | 7 | Sentinel; not a valid filtering choice. |

## Value descriptions

### ParentNode

`TextureFilter.ParentNode = 0`

Inherits the direct canvas parent, or the containing viewport default.

### Nearest

`TextureFilter.Nearest = 1`

Samples the nearest base-level texel.

### Linear

`TextureFilter.Linear = 2`

Interpolates neighboring base-level texels.

### NearestWithMipmaps

`TextureFilter.NearestWithMipmaps = 3`

Uses nearest texels with mip levels for minification.

### LinearWithMipmaps

`TextureFilter.LinearWithMipmaps = 4`

Uses linear texel filtering with mip levels for minification.

### NearestWithMipmapsAnisotropic

`TextureFilter.NearestWithMipmapsAnisotropic = 5`

Uses nearest texels, mip levels and viewport-controlled anisotropy.

### LinearWithMipmapsAnisotropic

`TextureFilter.LinearWithMipmapsAnisotropic = 6`

Uses linear texel filtering, mip levels and viewport-controlled anisotropy.

### Max

`TextureFilter.Max = 7`

Sentinel; not a valid filtering choice.

## Verification and limits

[CanvasSamplingTests](../../tests/Electron2D.Tests/CanvasSamplingTests.cs) verifies defaults, invalid values, inheritance, stored values and ownership guards. [Native sampling checks](../../tests/Electron2D.Tests/CanvasSamplingRenderingTests.cs) verify actual filtering, addressing and backend rejection. GPU supports mipmaps, mirror and anisotropy. Hardware compatibility supports nearest/linear and clamp/repeat; software triangle rendering supports nearest only. Unsupported modes throw before drawing rather than silently substituting a mode.
