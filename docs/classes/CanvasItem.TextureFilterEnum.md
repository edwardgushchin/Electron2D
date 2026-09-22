# CanvasItem.TextureFilterEnum

Last updated: 2026-09-23

- Declaration: `public enum CanvasItem.TextureFilterEnum`
- Source: [CanvasItem.Sampling.cs](../../src/Scene/Main/CanvasItem.Sampling.cs)
- Owner: [CanvasItem](CanvasItem.md)
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

`CanvasItem.TextureFilterEnum.ParentNode = 0`

Inherits the direct canvas parent, or the containing viewport default.

### Nearest

`CanvasItem.TextureFilterEnum.Nearest = 1`

Samples the nearest base-level texel.

### Linear

`CanvasItem.TextureFilterEnum.Linear = 2`

Interpolates neighboring base-level texels.

### NearestWithMipmaps

`CanvasItem.TextureFilterEnum.NearestWithMipmaps = 3`

Uses nearest texels with mip levels for minification.

### LinearWithMipmaps

`CanvasItem.TextureFilterEnum.LinearWithMipmaps = 4`

Uses linear texel filtering with mip levels for minification.

### NearestWithMipmapsAnisotropic

`CanvasItem.TextureFilterEnum.NearestWithMipmapsAnisotropic = 5`

Uses nearest texels, mip levels and viewport-controlled anisotropy.

### LinearWithMipmapsAnisotropic

`CanvasItem.TextureFilterEnum.LinearWithMipmapsAnisotropic = 6`

Uses linear texel filtering, mip levels and viewport-controlled anisotropy.

### Max

`CanvasItem.TextureFilterEnum.Max = 7`

Sentinel; not a valid filtering choice.

## Verification and limits

[CanvasSamplingTests](../../tests/Electron2D.Tests/CanvasSamplingTests.cs) verifies defaults, invalid values, inheritance, stored values and ownership guards. [Native sampling checks](../../tests/Electron2D.Tests/CanvasSamplingRenderingTests.cs) verify actual filtering, addressing and backend rejection. GPU supports mipmaps, mirror and anisotropy. Hardware compatibility supports nearest/linear and clamp/repeat; software triangle rendering supports nearest only. Unsupported modes throw before drawing rather than silently substituting a mode.
