# RenderingServer.ParticlesTransformAlignAxis

Last updated: 2026-10-19

**Namespace:** Electron2D. **Declaration:** public enum Electron2D.RenderingServer.ParticlesTransformAlignAxis. **Source:** [RenderingServer.Particles.API.cs](../../src/Servers/Rendering/RenderingServer.Particles.API.cs). **Component:** [GPU particles](../components/gpu-particles.md).

## Description

Selects an executable planar particle draw policy. Unknown values reject; numeric gaps preserve distinct excluded spatial policies.

## Example

This fragment requires a live `particles` RID created on the renderer owner, such as inside `Window.Ready`.

```csharp
RenderingServer.ParticlesSetTransformAlignAxis(particles, RenderingServer.ParticlesTransformAlignAxis.X);
```

## API and member descriptions

| Member | Value | Behavior |
| --- | --- | --- |
| `X` | 0 | Selects the local X basis. |
| `Y` | 1 | Selects the local Y basis; the default. |
