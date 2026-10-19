# RenderingServer.ParticlesTransformAlign

Last updated: 2026-10-19

**Namespace:** Electron2D. **Declaration:** public enum Electron2D.RenderingServer.ParticlesTransformAlign. **Source:** [RenderingServer.Particles.API.cs](../../src/Servers/Rendering/RenderingServer.Particles.API.cs). **Component:** [GPU particles](../components/gpu-particles.md).

## Description

Selects an executable planar particle draw policy. Unknown values reject; numeric gaps preserve distinct excluded spatial policies.

## Example

This fragment requires a live `particles` RID created on the renderer owner, such as inside `Window.Ready`.

```csharp
RenderingServer.ParticlesSetTransformAlign(particles, RenderingServer.ParticlesTransformAlign.YToVelocity);
```

## API and member descriptions

| Member | Value | Behavior |
| --- | --- | --- |
| `Disabled` | 0 | Preserves the process basis. |
| `YToVelocity` | 2 | Aligns the selected planar basis with velocity. |
