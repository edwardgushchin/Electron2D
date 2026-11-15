# PointLight

Last updated: 2026-11-15

- Declaration: `public class PointLight : Light`
- Source: [PointLight.cs](../../src/Scene/2D/PointLight.cs)
- Component: [Canvas lights and occlusion](../components/canvas-lights.md)

## Description

Transformed borrowed light mask, local center offset, texture scale and normal-mapping height. See the component for defaults, ownership, sampling, masks, persistence and renderer limits. Public members follow the attached scene's owner-thread rules; resources reject disposed inputs and preserve borrowed ownership. GPU lighting is executable; compatibility rejects affected lighting passes explicitly.

An attached child [Control](Control.md) uses the texture size multiplied by TextureScale, with the rectangle centered at Offset. A missing texture supplies an empty area. Offset and TextureScale edits raise ItemRectChanged and reflow the child.

## Verification

`LightingTests` and native lighting checks cover resource metadata, actual pixels and prepared frame reuse. Foreign-platform and owner acceptance remain separate.

Built-in lighting accepts float, raw signed and raw unsigned device-image channels in every combination. Integer channels require nearest filtering; point-light masks and alpha clipping execute on GPU with their matching scalar samplers. Atlas light masks preserve region coordinates and transparent margins. See [native integer lighting verification](../components/local-compute.md#integer-lighting-checkpoint).
