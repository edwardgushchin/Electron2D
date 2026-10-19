# Particle shaders

Last updated: 2026-10-19

## Executable scope

`Shader.Mode.Particles` (2) is a compiled compute profile for planar particle state. The HLSL/GLSL importers, project build items, resource reflection, typed `ShaderMaterial` values, duplication and file graphs execute this profile. Export the compute module to the public `RenderingDevice` API to create a compute pipeline, bind buffers and dispatch it. The device owns the native state; reading it is an explicit synchronization/readback operation.

The [retained GPU particle server](gpu-particles.md) now schedules these programs and submits canvas mesh geometry. [ParticleProcessMaterial](../classes/ParticleProcessMaterial.md) now supplies built-in motion and appearance compute. The GPUParticles node and CPU conversion remain their next connected integration. A process material cannot be submitted to canvas or writable-texture drawing. No high-level particle placeholder types are exposed.

## Authoring and loading

Use `#include "electron2d/particles.hlslinc"` or `#include "electron2d/particles.glslinc"`. GLSL needs version 450 and `GL_GOOGLE_include_directive`; HLSL uses `SV_DispatchThreadID`. Define a `main` compute entry point with exactly 64 by 1 by 1 threads. Bounds-check its linear particle index against `Counts.x` before touching a record. Specialization of workgroup dimensions is rejected.

```bash
dotnet run --project tools/shaders/ShaderImport.csproj -c Release -- process.comp.glsl compute process.spv --particles
```

A project using `Electron2D.Shaders.targets` can declare:

```xml
<Electron2DShader Include="process.comp.hlsl" Stage="compute" Profile="particles" TargetPath="Shaders/process.spv" />
```

```csharp
using var process = Shader.CreateFromSPIRV(File.ReadAllBytes("process.spv"), Shader.Mode.Particles);
using var module = new RDShaderSPIRV
{
    BytecodeCompute = process.GetSPIRV(RenderingDevice.ShaderStage.Compute)
};
using var device = RenderingServer.CreateLocalRenderingDevice();
var shader = device.ShaderCreateFromSPIRV(module);
var pipeline = device.ComputePipelineCreate(shader);
```

The caller binds storage at set one and frame/user uniform buffers at set two through `RDUniform`/`UniformSetCreate`. Sampled images use set zero. Ordinary device submission, synchronization, thread affinity, lifetime and resource limits apply. Runtime loading consumes compiled bytes and does not invoke the source importer. Both bytecode getters return independent authored compute bytes. The Vertex/Fragment overloads reject this mode. `SetSPIRV(bytes, mode)` validates the replacement before publication; failure retains the prior program and values.

## Buffer ABI

Each `Electron2D_Particle` is a 96-byte record, with six aligned 16-byte lanes:

| Offset | Field | Meaning |
| --- | --- | --- |
| 0 | `float4 TransformX` | Planar transform row `(X.x, Y.x, spare, origin.x)`. |
| 16 | `float4 TransformY` | Planar transform row `(X.y, Y.y, spare, origin.y)`. |
| 32 | `float4 Color` | Particle color. |
| 48 | `float4 Custom` | Four numeric user channels. |
| 64 | `float4 VelocityAge` | XY velocity, elapsed age, fourth numeric channel. |
| 80 | `uint4 State` | Flags, birth cycle, seed and local delta float bits. |

The writable `PARTICLES` runtime array is descriptor set one/binding zero. Optional `SUBPARTICLES` is the same record layout at binding one. Storage descriptors cannot be arrays; other storage/image/push-constant resources are rejected by this profile.

`ParticleFrame` is a required 192-byte uniform block at set two/binding zero. Its twelve aligned lanes, in order, are `uint4 Counts`, then float4 `Time`, `Cycle`, `EmissionX`, `EmissionY`, `PreviousEmissionX`, `PreviousEmissionY`, `EmitterVelocity`, `SDFRect`, `SDFTransformX`, `SDFTransformY`, and `uint4 Options`. It stays outside material properties. Explicit device callers supply all values and their simulation policy. The retained renderer supplies the executing frame semantics described in [GPU particles](gpu-particles.md); explicit device callers choose their own policy.

At most three further uniform blocks use contiguous bindings one through three. Existing std140 scalar/vector/matrix/fixed-array types, logical boolean metadata, typed sampled images and their fixed descriptor arrays retain the shared material contract. Up to sixteen sampled bindings occupy set zero. Instance blocks, screen/command textures and screen pixel dimensions are rejected. Global/TIME/SDF inputs, when declared, require explicit caller binding in this device workflow; there is no automatic viewport source.

## Bounded subemission

Define `ELECTRON2D_PARTICLE_SUBEMISSION` before the include to declare the optional queue and `Electron2D_EmitSubparticle(record)`. Queue record zero holds an atomic header in `State`: count, capacity, overflow flag and a reserved word. Payload records start at index one. Initialize the header and allocate its storage before dispatch; do not reset it concurrently with writers.

The helper clamps capacity to the physical array length minus its header. A compare/exchange loop reserves one unique payload slot. A full, zero-capacity or invalid oversized count returns false, sets overflow bit one and leaves the count/payload bounded. It never wraps an overflowing atomic increment. A headerless array returns false without a write. Payload visibility is established by ending the dispatch/pass before a consuming dispatch or readback; count is a reservation counter during concurrent writes.

## Verification and limits

`ParticleShaderTests` checks both imported languages before renderer startup, logical booleans, typed migration, atomic replacement, malformed workgroup/storage/frame layouts, independent exports, resource graph archives and duplication. The native path passes each exported public Shader module to RenderingDevice, advances GPU state repeatedly, verifies a partial final workgroup, exercises bounded concurrent queue writes and disables processing through its logical boolean. Headerless, zero-capacity, oversized-capacity and UINT_MAX-count queues, plus a state/frame-only program without a queue, execute. Thirty-two warmed submission/synchronization steps per language allocate zero managed owner bytes.

Current native evidence uses Linux Wayland and software Vulkan (lavapipe). This establishes compute execution and readback, with the retained canvas evidence documented separately in [GPU particles](gpu-particles.md). Hardware performance, native allocator measurements, other platforms and owner acceptance remain unverified.
