# OccluderPolygon

Last updated: 2026-10-11

- Declaration: `public sealed class OccluderPolygon : Resource`
- Source: [OccluderPolygon.cs](../../src/Scene/Resources/OccluderPolygon.cs)
- Component: [Canvas lights and occlusion](../components/canvas-lights.md)

## Description

Copied finite open/closed contours, directed-edge culling and stable borrowed contour identity. See the component for defaults, ownership, sampling, masks, persistence and renderer limits. Public members follow the attached scene's owner-thread rules; resources reject disposed inputs and preserve borrowed ownership. GPU lighting is executable; compatibility rejects affected lighting passes explicitly.

## Verification

`LightingTests` and native lighting checks cover resource metadata, actual pixels and prepared frame reuse. Foreign-platform and owner acceptance remain separate.
