# DirectionalLight

Last updated: 2026-11-06

- Declaration: `public class DirectionalLight : Light`
- Source: [DirectionalLight.cs](../../src/Scene/2D/DirectionalLight.cs)
- Component: [Canvas lights and occlusion](../components/canvas-lights.md)

## Description

Directional canvas illumination, tilt and upstream contour inclusion distance; receiving masks are ignored. See the component for defaults, ownership, sampling, masks, persistence and renderer limits. Public members follow the attached scene's owner-thread rules; resources reject disposed inputs and preserve borrowed ownership. GPU lighting is executable; compatibility rejects affected lighting passes explicitly.

## Verification

`LightingTests` and native lighting checks cover resource metadata, actual pixels and prepared frame reuse. Foreign-platform and owner acceptance remain separate.

## MaxDistance

`public float MaxDistance { get; set; }` extends the upstream depth range beyond the rendered viewport in framebuffer pixels. The default is 10000. Nonpositive values add no extension; contours within the viewport still cast shadows. Increasing receiver distance does not shorten the shadow. Rotation determines the ray direction; position is ignored. Values must be finite. See [projection, filtering and verification](../components/canvas-shadow-atlas.md).
