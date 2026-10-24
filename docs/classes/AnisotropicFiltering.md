# AnisotropicFiltering

Last updated: 2026-10-24

- Declaration: `public enum AnisotropicFiltering`
- Source: [AnisotropicFiltering.cs](../../src/Servers/Rendering/AnisotropicFiltering.cs)
- Namespace: `Electron2D`
- Component: [Canvas rendering](../components/canvas-rendering.md#texture-sampling)

## Description

Shared sampling choices used by Viewport and RenderingServer.ViewportSetAnisotropicFilteringLevel. Values are stored by PackedScene. Max is a sentinel rejected by setters, as are negative and undefined values. See the owning properties for defaults, inheritance, lifetime and threading.

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

`AnisotropicFiltering.Disabled = 0`

Disables anisotropy.

### Anisotropy2X

`AnisotropicFiltering.Anisotropy2X = 1`

At most two samples.

### Anisotropy4X

`AnisotropicFiltering.Anisotropy4X = 2`

At most four samples; the default.

### Anisotropy8X

`AnisotropicFiltering.Anisotropy8X = 3`

At most eight samples.

### Anisotropy16X

`AnisotropicFiltering.Anisotropy16X = 4`

At most sixteen samples.

### Max

`AnisotropicFiltering.Max = 5`

Sentinel; not a valid choice.

## Verification and limits

[CanvasSamplingTests](../../tests/Electron2D.Tests/CanvasSamplingTests.cs) verifies defaults, invalid values, inheritance, stored values and ownership guards. [Native sampling checks](../../tests/Electron2D.Tests/CanvasSamplingRenderingTests.cs) verify actual filtering, addressing and backend rejection. GPU supports mipmaps, mirror and anisotropy. Hardware compatibility supports nearest/linear and clamp/repeat; software triangle rendering supports nearest only. Unsupported modes throw before drawing rather than silently substituting a mode.
