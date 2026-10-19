# ParticleGPUState

Last updated: 2026-10-19

- Visibility: internal; unavailable to engine consumers.
- Source: [ParticleGPUState.cs](../../src/Servers/Rendering/ParticleGPUState.cs)
- Component: [GPU particles](../components/gpu-particles.md)

## Description

Owns current/previous state, bounded incoming/outgoing queues, age ordering, history, copied bind-pose rows and upload/readback transfers on the renderer device. Record and byte budgets are checked before allocation. Prepare uploads changed poses and pending queue headers; cancelled commands invalidate upload caches. CaptureHistory advances the native ring after a step. Read synchronizes and returns a cold copied snapshot. Dispose retires every native handle.

## Internal usage

This partial runtime fragment requires the surrounding retained renderer owner, GPU backend/device and particle state; engine consumers use the public RenderingServer family.

```csharp
state.GPU ??= new ParticleGPUState(state, device);
```

## Verification

The connected ParticleServerTests profiles exercise real native simulation/output, identity and callback guards, finite/capacity failures, cold bounds, cancelled submission retry and warmed allocation behavior. Physical GPU, native allocator and foreign-platform acceptance remain separate.
