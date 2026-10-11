# CanvasTexture

Last updated: 2026-10-11

- Declaration: `public sealed class CanvasTexture : Texture`
- Source: [CanvasTexture.cs](../../src/Scene/Resources/CanvasTexture.cs)
- Component: [Canvas lights and occlusion](../components/canvas-lights.md)

## Description

Borrowed diffuse/normal/specular channels, reflection controls and resolved sampling overrides. See the component for defaults, ownership, sampling, masks, persistence and renderer limits. Public members follow the attached scene's owner-thread rules; resources reject disposed inputs and preserve borrowed ownership. GPU lighting is executable; compatibility rejects affected lighting passes explicitly.

## Verification

`LightingTests` and native lighting checks cover resource metadata, actual pixels and prepared frame reuse. Foreign-platform and owner acceptance remain separate.

Built-in lighting accepts float, raw signed and raw unsigned device-image channels in every combination. Integer channels require nearest filtering; point-light masks and alpha clipping execute on GPU with their matching scalar samplers. Atlas light masks preserve region coordinates and transparent margins. See [native integer lighting verification](../components/local-compute.md#integer-lighting-checkpoint).
