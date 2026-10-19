# ParticleProcessMaterial

Last updated: 2026-10-19

- Declaration: `public sealed partial class ParticleProcessMaterial : Material`
- Sources: [configuration](../../src/Scene/Resources/ParticleProcessMaterial.cs), [properties](../../src/Scene/Resources/ParticleProcessMaterial.Properties.cs), [native preparation](../../src/Scene/Resources/ParticleProcessMaterial.Rendering.cs), [storage](../../src/Scene/Resources/ParticleProcessMaterial.Storage.cs)
- Component: [GPU particles](../components/gpu-particles.md#built-in-process-material)

## Description

Authors built-in planar particle motion and appearance. Construction, typed property edits, duplication and file graphs need no renderer. Native execution uses the GPU backend through RenderingServer particle RIDs. The material and its textures remain caller-owned; consumers borrow them. Parameter edits apply at the next process step, while initial velocity/distribution choices apply at birth. Coordinate compound edits with rendering. Disposed texture assignments reject before replacing the prior value; disposing a borrowed input rejects preparation.

## Example

```csharp
using var material = new ParticleProcessMaterial
{
    Direction = new(1, 0),
    Spread = 15,
    InitialVelocityMin = 40,
    InitialVelocityMax = 80,
    Gravity = new(0, 120),
    Color = new(1, .6f, .1f)
};
// During an active GPU rendering session:
var particles = RenderingServer.ParticlesCreate();
RenderingServer.ParticlesSetAmount(particles, 64);
RenderingServer.ParticlesSetProcessMaterial(particles, material.GetRID());
RenderingServer.ParticlesSetEmitting(particles, true);
// Assign draw-pass meshes and submit with CanvasItemAddParticles.
// Free the particle RID before disposing borrowed resources.
```

## API summary

| Family | Contract |
| --- | --- |
| `ParticleProcessMaterial()` | Rightward direction, spread 45 degrees, gravity (0,-9.8), white color, unit scale/directional multiplier, turbulence influence .1, subemission frequency 4. |
| `GetParam`, `GetParamMin`, `GetParamMax` | Read an applicable scalar range as Vector2 or a finite scalar. |
| `SetParam`, `SetParamMin`, `SetParamMax` | Keep pairs ordered; SetParam applies minimum followed by maximum. |
| `GetParamTexture`, `SetParamTexture` | Borrow a channel texture; initialize only absent CurveTexture/CurveXYZTexture curves to unit samples. |
| `GetParticleFlag`, `SetParticleFlag` | Y alignment, friction damping or inherited emitter scale. |
| Direction, Spread, Gravity, InheritVelocityRatio, LifetimeRandomness | Birth direction, acceleration, inherited velocity and randomized lifetime. |
| EmissionShape, EmissionShapeOffset/Scale, EmissionRectExtents, EmissionSphereRadius, EmissionRingRadius/InnerRadius | Seven shared planar distributions and numeric dimensions. |
| EmissionPointCount, EmissionPointTexture, EmissionNormalTexture, EmissionColorTexture | Position/normal RG texels and optional color texels. |
| InitialVelocity; AngularVelocity; OrbitVelocity; LinearAccel; RadialAccel; TangentialAccel; Damping; Angle; Scale; HueVariation; AnimSpeed; AnimOffset | Named Min/Max properties; every family except InitialVelocity also has a Curve texture. |
| RadialVelocity, DirectionalVelocity, ScaleOverVelocity | Min/Max and Curve properties for controlled velocity and final-speed scaling. |
| Color, ColorRamp, ColorInitialRamp, AlphaCurve, EmissionCurve, VelocityLimitCurve | Base color, life/initial ramps, alpha/emission multiplication and final velocity limit. |
| VelocityPivot | Simulation-space pivot for orbit/radial channels. |
| TurbulenceEnabled, TurbulenceNoiseStrength/Scale/Speed/SpeedRandom | Numeric curl-noise controls; speed is Vector3 noise-coordinate data. |
| TurbulenceInfluenceMin/Max, TurbulenceInfluenceOverLife, TurbulenceInitialDisplacementMin/Max | Velocity mixing and generated-birth displacement. |
| ParticleFlagAlignY, ParticleFlagDampingAsFriction, ParticleFlagInheritEmitterScale | Named access to the applicable flag domain. |
| CollisionMode, CollisionFriction, CollisionBounce, CollisionUseScale | Canvas SDF disabled/rigid/hide response and numeric contact controls. |
| SubEmitterMode, SubEmitterFrequency, SubEmitterAmountAtStart/End/Collision, SubEmitterKeepVelocity | Bounded linked-child emission at constant intervals, birth, end or contact. |

See [Parameter](ParticleProcessMaterial.Parameter.md), [ParticleFlags](ParticleProcessMaterial.ParticleFlags.md), [ParticlesEmissionShape](ParticlesEmissionShape.md), [CollisionModeEnum](ParticleProcessMaterial.CollisionModeEnum.md) and [SubEmitterModeEnum](ParticleProcessMaterial.SubEmitterModeEnum.md) for valid values. Max and spatial flag holes reject.

## Behavior and storage

The [component contract](../components/gpu-particles.md#built-in-process-material) records units, sampling coordinates, stage order, supplied emission fields, collisions and subemission lifetime. Scalar TurbInfluenceOverLife and texture InitialLinearVelocity/TurbVelocityInfluence/TurbInitialDisplacement roles reject rather than store ineffective settings. Numeric inputs are finite; invalid domains and negative radii/counts reject. Unit-ratio controls clamp. Inverted independent ring radii reject preparation. Extreme derived nonfinite state cannot remain active.

Curve and gradient textures are borrowed and shared by shallow duplication. Deep duplication preserves internal aliases while copying the subresource graph. Typed stored descriptors and registered resource factories persist configuration, curves, initialization state and detached gradient pixels. Current stored enum schemas use ParticlesEmissionShape. Editor-only shape notifications depend on an executable editor consumer. There are no spatial rotation/scale, attractor, cylinder or fixed DisableZ controls.

GetPropertyDescriptors, CreateDuplicateInstance and CopyCustomStateTo implement the inherited Resource contract. No renderer session or runtime source compilation is needed for these hooks. Native shader/pipeline and GPU texture ownership belongs to the renderer; this resource owns only its settings.

## Verification

ParticleServerTests material profiles exercise authoring/defaults/range roles, shared graph duplication, same/fresh-process files, malformed frozen-pixel rejection, native distribution/channel/noise/finite edge cases, world/local emission basis, SDF contact, bounded/timed subemission, live viewport texture dependencies and callback failure/retry. Thirty-two warmed built-in process/sort/render frames allocate zero managed owner bytes on Linux Wayland software Vulkan. Physical GPU performance, native allocator accounting, foreign platforms, editor UI and owner acceptance remain separate gates.

CPU emitter conversion also retains private stored initial-speed lifetime and scalar-times-split scale curves. Dedicated built-in compute variants execute them after file loading; they do not add public Parameter roles.
