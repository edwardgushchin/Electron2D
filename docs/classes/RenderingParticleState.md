# RenderingParticleState

Last updated: 2026-10-19

- Visibility: internal; unavailable to engine consumers.
- Source: [RenderingParticleState.cs](../../src/Servers/Rendering/RenderingParticleState.cs)
- Component: [GPU particles](../components/gpu-particles.md)

## Description

Retains renderer-session configuration, borrowed draw/process/subemitter edges, bounded copied manual emissions and CPU timing. Native particle records have no CPU mirror. Capacity changes retire GPU buffers and reset phase; restart defers record clearing until dispatch. Release clears borrowed references and native storage. Creation, mutations and release reject particle-preparation callback reentry.

## Internal usage

This partial runtime fragment requires the surrounding retained renderer owner, GPU backend/device and particle state; engine consumers use the public RenderingServer family.

```csharp
var state = new RenderingParticleState(owner, backend);
state.Allocate(64);
state.Requested = true;
```

## Verification

The connected ParticleServerTests profiles exercise real native simulation/output, identity and callback guards, finite/capacity failures, cold bounds, cancelled submission retry and warmed allocation behavior. Physical GPU, native allocator and foreign-platform acceptance remain separate.
