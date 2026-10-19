# Shader.Mode

Last updated: 2026-10-19

- Declaration: `public enum Shader.Mode`
- Source: [Shader.cs](../../src/Scene/Resources/Shader.cs)
- Owner: [Shader](Shader.md)

## Values

| Value | Integer | Meaning |
| --- | --- | --- |
| `CanvasItem` | 1 | Canvas fragments and optional authored vertices. |
| `Particles` | 2 | Particle compute program with the portable state/frame ABI. |
| `TextureBlit` | 5 | Fragments drawing into writable textures. |

`GetMode()` returns the validated executing domain. [Particle shaders](../components/particle-shaders.md) execute through public RenderingDevice dispatch and the [retained GPU particle server](../components/gpu-particles.md). [Writable textures](../components/drawable-textures.md) document blit execution. Spatial, sky and fog scene domains are outside the planar product.
