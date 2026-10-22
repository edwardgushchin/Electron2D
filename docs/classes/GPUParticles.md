# GPUParticles

Last updated: 2026-10-19

- Declaration: `public partial class GPUParticles : Entity`
- Sources: [configuration](../../src/Scene/2D/GPUParticles.cs), [properties](../../src/Scene/2D/GPUParticles.Properties.cs), [rendering](../../src/Scene/2D/GPUParticles.Rendering.cs), [lifecycle](../../src/Scene/2D/GPUParticles.Lifecycle.cs), [storage](../../src/Scene/2D/GPUParticles.Storage.cs), [conversion](../../src/Scene/2D/GPUParticles.Conversion.cs)
- Inherits: [Entity](Entity.md), [CanvasItem](CanvasItem.md), [Node](Node.md)
- Component: [GPU particles](../components/gpu-particles.md)

## Description

An ordinary scene emitter backed by retained GPU simulation. Detached construction, configuration, scene archives and one-shot timing need no graphics session. Drawing, manual emission and explicit simulation lazily allocate native state in an active GPU rendering session. CaptureRect returns empty before native initialization and after session retirement. The compatibility backend cannot execute this node's compute simulation.

The node owns its mesh, random generator and renderer-session identity. Assigned Texture, ProcessMaterial and ordinary CanvasItem Material remain borrowed. CPU conversion owns its generated process resources while borrowing source curves, gradients and draw resources. A detached node can survive renderer teardown and rebuild its native state in another session. Archives store settings and resource graphs, excluding live particles and handles.

LocalCoords retains local particles; world particles remain at their birth pose as the emitter moves. Physics ticks derive emitter velocity. Scene pause and process policy stop automatic simulation; explicit RequestParticlesProcess uses the supplied time. Finished follows the scene's process clock independently of SpeedScale. Restart starts a new one-shot completion cycle; overlapping Emitting=true assignments cancel that cycle's completion signal. State commits before callbacks, so a Finished handler may restart or dispose the emitter.

Preparation rejects emitter or ancestor scene mutation/disposal from borrowed texture callbacks before changing scene state. Failed preparation retains pending native requests for retry. Relative SubEmitter paths resolve to another GPUParticles; missing/self paths stay unbound and produce configuration warnings, and native cycles reject.

## Example

```csharp
var process = new ParticleProcessMaterial
{
    Direction = new Vector2(0, -1),
    InitialVelocityMin = 60,
    InitialVelocityMax = 100,
    Gravity = new Vector2(0, 180)
};
var sparks = new GPUParticles
{
    Amount = 64,
    Lifetime = .8,
    ProcessMaterial = process,
    OneShot = true,
    Explosiveness = 1
};
root.AddChild(sparks);
sparks.Restart();
// Dispose the borrowed process material after the scene stops using it.
```

## Properties

All settings are stored through typed descriptors. Finite values are required for numerical inputs. Time/scales are limited to finite float storage in the native backend.

| Property | Initial value | Behavior |
| --- | --- | --- |
| `int Amount` | 8 | Positive capacity; rejects native byte-size overflow. Changing capacity resets storage. |
| `float AmountRatio` | 1 | Automatic-birth capacity fraction, clamped to 0..1. |
| `float CollisionBaseSize` | 1 | Nonnegative radius multiplier times the texture's mean half size; no texture uses the multiplier directly. |
| `ParticlesDrawOrder DrawOrder` | Lifetime | Shared Index, Lifetime or ReverseLifetime order. |
| `bool Emitting` | true | Controls automatic births; existing records drain when stopped. |
| `float Explosiveness` | 0 | Phase compression clamped to 0..1. |
| `int FixedFPS` | 30 | Positive fixed frequency; nonpositive selects variable steps. |
| `bool FractDelta` | true | Integrates newborn fractional time. |
| `float InterpToEnd` | 0 | Process end interpolation ratio clamped to 0..1. |
| `bool Interpolate` | true | Interpolates fixed-step presentation. |
| `double Lifetime` | 1 | Positive cycle duration in seconds, representable above float zero. |
| `bool LocalCoords` | false | Local versus world simulation coordinates. |
| `bool OneShot` | false | Stops births after one cycle and tracks completion. |
| `double Preprocess` | 0 | Nonnegative restart preprocessing seconds. |
| `Material? ProcessMaterial` | null | Built-in ParticleProcessMaterial or particle ShaderMaterial; null uses the default native process. |
| `float Randomness` | 0 | Emission-time randomness clamped to 0..1. |
| `uint Seed` | Random | Seed for future births. |
| `double SpeedScale` | 1 | Nonnegative automatic simulation scale; zero freezes simulation. |
| `string SubEmitter` | Empty | Relative path to a GPU child emitter, including sibling routing. |
| `Texture? Texture` | null | Logical texture size determines quad dimensions; null gives a one-pixel white quad. Nested atlas regions preserve sampled UVs. |
| `bool TrailEnabled` | false | Weighted native history and velocity alignment. |
| `double TrailLifetime` | .3 | At least .01 seconds; native history caps at ten seconds. |
| `int TrailSections` | 8 | 2..128 history sections. |
| `int TrailSectionSubdivisions` | 4 | 1..1024 segments per section. |
| `bool UseFixedSeed` | false | Preserves Seed across normal restarts. |
| `Rect2 VisibilityRect` | (-100,-100,200,200) | Finite emitter-local visibility bounds, independent of the cold live bounds query. |

## Methods and events

`GPUParticles()` configures a detached emitter without allocating a native session handle.

`Rect2 CaptureRect()` synchronously reads current native geometry bounds. It returns default before initialization; this is a cold query, not an automatic frame readback.

`void EmitParticle(Transform transform, Vector2 velocity, Color color, Color custom, ParticlesEmitFlags flags)` queues one particle. [EmitFlags](ParticlesEmitFlags.md) selects supplied fields; remaining fields use the process material. Inputs follow retained server validation. Rendering requires an active GPU session.

`void RequestParticlesProcess(float processTime, float processTimeResidual = 0)` requests nonnegative explicit simulation time independently of visibility, pause and SpeedScale. The residual is a final variable step.

`void Restart(bool keepSeed = false)` clears native state when initialized, starts emission and resets completion tracking. keepSeed preserves Seed even when UseFixedSeed is false.

`void ConvertFromParticles(Node particles)` requires CPUParticles, copies shared authored settings and constructs a built-in process material. It preserves point/normal/color arrays as float textures and HDR gradients. Initial velocity lifetime and scalar-times-split scale curves execute through private stored compute inputs. It copies no live particle state or source transform. Conversion on an attached node starts a copied emitting one-shot cycle.

`string[] GetConfigurationWarnings()` includes inherited warnings, missing process material and invalid/self sub-emitter routes.

`event Action? Finished` fires once at uncancelled one-shot scene completion. Handler errors do not replay the event.

Inherited protected overrides integrate drawing, notifications, typed property descriptors, scene factories and disposal; they add no separate authoring protocol.

## Verification limits

GPUParticlesNodeTests covers managed authoring/ranges/one-shot behavior, conversions, actual software-Vulkan pixels/bounds, atlas updates, relative subemitters, world/pause behavior, fresh-process files, private conversion curve execution, callback retry, session reuse and prepared allocation. Physical GPU, native allocator accounting, other platforms, independent publish/AOT and owner acceptance require separate evidence.

The manual emission mask is shared [ParticlesEmitFlags](ParticlesEmitFlags.md), also used by compiled graph subemission. The former nested enum is removed under ADR 0051.
