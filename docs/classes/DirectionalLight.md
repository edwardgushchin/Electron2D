# DirectionalLight

Last updated: 2026-10-11

- Declaration: `public class DirectionalLight : Light`
- Source: [DirectionalLight.cs](../../src/Scene/2D/DirectionalLight.cs)
- Component: [Canvas lights and occlusion](../components/canvas-lights.md)

## Description

Directional canvas illumination, tilt and bounded shadow distance; receiving masks are ignored. See the component for defaults, ownership, sampling, masks, persistence and renderer limits. Public members follow the attached scene's owner-thread rules; resources reject disposed inputs and preserve borrowed ownership. GPU lighting is executable; compatibility rejects affected lighting passes explicitly.

## Verification

`LightingTests` and native lighting checks cover resource metadata, actual pixels and prepared frame reuse. Foreign-platform and owner acceptance remain separate.
