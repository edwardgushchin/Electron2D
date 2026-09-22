# Viewport.DefaultCanvasItemTextureRepeat

Last updated: 2026-09-23

- Declaration: `public enum Viewport.DefaultCanvasItemTextureRepeat`
- Source: [Viewport.Sampling.cs](../../src/Scene/Main/Viewport.Sampling.cs)
- Owner: [Viewport](Viewport.md)
- Component: [Canvas rendering](../components/canvas-rendering.md#texture-sampling)

## Description

Typed sampling choices used by Viewport. Values are stored by PackedScene. Max is a sentinel rejected by setters, as are negative and undefined values. See the owning properties for defaults, inheritance, lifetime and threading.

## Values

| Name | Value | Contract |
| --- | --- | --- |
| [`Disabled`](#disabled) | 0 | Clamps to texture edges; the default. |
| [`Enabled`](#enabled) | 1 | Repeats the texture. |
| [`Mirror`](#mirror) | 2 | Reflects alternate repeated tiles. |
| [`ParentNode`](#parentnode) | 3 | Inherits the direct canvas or viewport parent, otherwise disabled. |
| [`Max`](#max) | 4 | Sentinel; not a valid choice. |

## Value descriptions

### Disabled

`Viewport.DefaultCanvasItemTextureRepeat.Disabled = 0`

Clamps to texture edges; the default.

### Enabled

`Viewport.DefaultCanvasItemTextureRepeat.Enabled = 1`

Repeats the texture.

### Mirror

`Viewport.DefaultCanvasItemTextureRepeat.Mirror = 2`

Reflects alternate repeated tiles.

### ParentNode

`Viewport.DefaultCanvasItemTextureRepeat.ParentNode = 3`

Inherits the direct canvas or viewport parent, otherwise disabled.

### Max

`Viewport.DefaultCanvasItemTextureRepeat.Max = 4`

Sentinel; not a valid choice.

## Verification and limits

[CanvasSamplingTests](../../tests/Electron2D.Tests/CanvasSamplingTests.cs) verifies defaults, invalid values, inheritance, stored values and ownership guards. [Native sampling checks](../../tests/Electron2D.Tests/CanvasSamplingRenderingTests.cs) verify actual filtering, addressing and backend rejection. GPU supports mipmaps, mirror and anisotropy. Hardware compatibility supports nearest/linear and clamp/repeat; software triangle rendering supports nearest only. Unsupported modes throw before drawing rather than silently substituting a mode.
