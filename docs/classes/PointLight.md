# PointLight

Last updated: 2026-10-11

- Declaration: `public class PointLight : Light`
- Source: [PointLight.cs](../../src/Scene/2D/PointLight.cs)
- Component: [Canvas lights and occlusion](../components/canvas-lights.md)

## Description

Transformed borrowed light mask, local center offset, texture scale and normal-mapping height. See the component for defaults, ownership, sampling, masks, persistence and renderer limits. Public members follow the attached scene's owner-thread rules; resources reject disposed inputs and preserve borrowed ownership. GPU lighting is executable; compatibility rejects affected lighting passes explicitly.

## Verification

`LightingTests` and native lighting checks cover resource metadata, actual pixels and prepared frame reuse. Foreign-platform and owner acceptance remain separate.
