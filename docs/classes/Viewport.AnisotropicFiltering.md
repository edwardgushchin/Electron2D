# Viewport.AnisotropicFiltering

Last updated: 2026-09-23

- Declaration: `public enum Viewport.AnisotropicFiltering`
- Source: [Viewport.Sampling.cs](../../src/Scene/Main/Viewport.Sampling.cs)
- Owner: [Viewport](Viewport.md)
- Component: [Canvas rendering](../components/canvas-rendering.md#texture-sampling)

## Description

Typed sampling choices used by Viewport. Values are stored by PackedScene. Max is a sentinel rejected by setters, as are negative and undefined values. See the owning properties for defaults, inheritance, lifetime and threading.

## Values

| Name | Value | Contract |
| --- | --- | --- |
| [`Disabled`](#disabled) | 0 | Disables anisotropy. |
| [`Anisotropy2X`](#anisotropy2x) | 1 | At most two samples. |
| [`Anisotropy4X`](#anisotropy4x) | 2 | At most four samples; the default. |
| [`Anisotropy8X`](#anisotropy8x) | 3 | At most eight samples. |
| [`Anisotropy16X`](#anisotropy16x) | 4 | At most sixteen samples. |
| [`Max`](#max) | 5 | Sentinel; not a valid choice. |

## Value descriptions

### Disabled

`Viewport.AnisotropicFiltering.Disabled = 0`

Disables anisotropy.

### Anisotropy2X

`Viewport.AnisotropicFiltering.Anisotropy2X = 1`

At most two samples.

### Anisotropy4X

`Viewport.AnisotropicFiltering.Anisotropy4X = 2`

At most four samples; the default.

### Anisotropy8X

`Viewport.AnisotropicFiltering.Anisotropy8X = 3`

At most eight samples.

### Anisotropy16X

`Viewport.AnisotropicFiltering.Anisotropy16X = 4`

At most sixteen samples.

### Max

`Viewport.AnisotropicFiltering.Max = 5`

Sentinel; not a valid choice.

## Verification and limits

[CanvasSamplingTests](../../tests/Electron2D.Tests/CanvasSamplingTests.cs) verifies defaults, invalid values, inheritance, stored values and ownership guards. [Native sampling checks](../../tests/Electron2D.Tests/CanvasSamplingRenderingTests.cs) verify actual filtering, addressing and backend rejection. GPU supports mipmaps, mirror and anisotropy. Hardware compatibility supports nearest/linear and clamp/repeat; software triangle rendering supports nearest only. Unsupported modes throw before drawing rather than silently substituting a mode.
