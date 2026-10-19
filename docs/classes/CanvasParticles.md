# CanvasParticles

Last updated: 2026-10-19

- Visibility: internal; unavailable to engine consumers.
- Source: [CanvasParticles.cs](../../src/Servers/Rendering/CanvasParticles.cs)
- Component: [GPU particles](../components/gpu-particles.md)

## Description

Retains the particle draw command and per-pass mesh templates. It requests once-per-frame simulation, captures canvas/world composition and sprite-sheet policy, and appends normal canvas batches with GPU conversion metadata. Released sources emit no geometry. Mesh callbacks must preserve particle ownership. The command borrows source resources; native allocations belong to GPUCanvasBackend.

## Internal usage

This partial runtime fragment requires the surrounding retained renderer owner, GPU backend/device and particle state; engine consumers use the public RenderingServer family.

```csharp
var command = new CanvasParticles(state);
```

## Verification

The connected ParticleServerTests profiles exercise real native simulation/output, identity and callback guards, finite/capacity failures, cold bounds, cancelled submission retry and warmed allocation behavior. Physical GPU, native allocator and foreign-platform acceptance remain separate.

## ParticleGeometry

Internal surface cache in the same source. Copies up to eight bone indices/weights per original mesh vertex on a new mesh revision and lazily uploads them into a renderer-owned native buffer. Cancelled commands mark the upload dirty; unused geometry retires at frame end. The buffer is not a resource owned by the caller.

## ParticleGPUDraw

Internal immutable batch metadata: source storage, geometry cache, captured canvas transform, modulation and sprite-sheet lanes. It feeds native vertex expansion without copying particle state to the CPU.
