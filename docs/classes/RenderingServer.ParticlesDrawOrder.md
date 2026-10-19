# RenderingServer.ParticlesDrawOrder

Last updated: 2026-10-19

**Namespace:** Electron2D. **Declaration:** public enum Electron2D.RenderingServer.ParticlesDrawOrder. **Source:** [RenderingServer.Particles.API.cs](../../src/Servers/Rendering/RenderingServer.Particles.API.cs). **Component:** [GPU particles](../components/gpu-particles.md).

## Description

Selects an executable planar particle draw policy. Unknown values reject; numeric gaps preserve distinct excluded spatial policies.

## Example

This fragment requires a live `particles` RID created on the renderer owner, such as inside `Window.Ready`.

```csharp
RenderingServer.ParticlesSetDrawOrder(particles, RenderingServer.ParticlesDrawOrder.Lifetime);
```

## API and member descriptions

| Member | Value | Behavior |
| --- | --- | --- |
| `Index` | 0 | Preserves original record order. |
| `Lifetime` | 1 | Oldest particles draw first. |
| `ReverseLifetime` | 2 | Youngest particles draw first. |
