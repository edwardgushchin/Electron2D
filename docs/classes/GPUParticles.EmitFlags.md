# GPUParticles.EmitFlags

Last updated: 2026-10-19

**Namespace:** Electron2D. **Declaration:** `[Flags] public enum GPUParticles.EmitFlags`. **Source:** [GPUParticles.cs](../../src/Scene/2D/GPUParticles.cs). **Component:** [GPU particles](../components/gpu-particles.md).

## Description

Selects copied fields for GPUParticles.EmitParticle. Combine flags with bitwise OR; zero lets the process material initialize all fields. Unknown bits are ignored by the retained server.

## Example

```csharp
var supplied = GPUParticles.EmitFlags.Position | GPUParticles.EmitFlags.Velocity;
```

## API and member descriptions

| Member | Value | Supplied field |
| --- | --- | --- |
| `Position` | 1 | Particle translation. |
| `RotationScale` | 2 | Particle basis. |
| `Velocity` | 4 | Physical velocity. |
| `Color` | 8 | Initial color. |
| `Custom` | 16 | Four numeric custom channels. |
