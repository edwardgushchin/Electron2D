# CanvasItem.TextureRepeatEnum

Last updated: 2026-09-23

- Declaration: `public enum CanvasItem.TextureRepeatEnum`
- Source: [CanvasItem.Sampling.cs](../../src/Scene/Main/CanvasItem.Sampling.cs)
- Owner: [CanvasItem](CanvasItem.md)
- Component: [Canvas rendering](../components/canvas-rendering.md#texture-sampling)

## Description

Typed sampling choices used by CanvasItem. Values are stored by PackedScene. Max is a sentinel rejected by setters, as are negative and undefined values. See the owning properties for defaults, inheritance, lifetime and threading.

## Values

| Name | Value | Contract |
| --- | --- | --- |
| [`ParentNode`](#parentnode) | 0 | Inherits the direct canvas parent, or the containing viewport default. |
| [`Disabled`](#disabled) | 1 | Clamps sampling to the texture edge. |
| [`Enabled`](#enabled) | 2 | Repeats the texture. |
| [`Mirror`](#mirror) | 3 | Repeats, reflecting alternate tiles. |
| [`Max`](#max) | 4 | Sentinel; not a valid repeat choice. |

## Value descriptions

### ParentNode

`CanvasItem.TextureRepeatEnum.ParentNode = 0`

Inherits the direct canvas parent, or the containing viewport default.

### Disabled

`CanvasItem.TextureRepeatEnum.Disabled = 1`

Clamps sampling to the texture edge.

### Enabled

`CanvasItem.TextureRepeatEnum.Enabled = 2`

Repeats the texture.

### Mirror

`CanvasItem.TextureRepeatEnum.Mirror = 3`

Repeats, reflecting alternate tiles.

### Max

`CanvasItem.TextureRepeatEnum.Max = 4`

Sentinel; not a valid repeat choice.

## Verification and limits

[CanvasSamplingTests](../../tests/Electron2D.Tests/CanvasSamplingTests.cs) verifies defaults, invalid values, inheritance, stored values and ownership guards. [Native sampling checks](../../tests/Electron2D.Tests/CanvasSamplingRenderingTests.cs) verify actual filtering, addressing and backend rejection. GPU supports mipmaps, mirror and anisotropy. Hardware compatibility supports nearest/linear and clamp/repeat; software triangle rendering supports nearest only. Unsupported modes throw before drawing rather than silently substituting a mode.
