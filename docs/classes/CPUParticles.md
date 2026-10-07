# CPUParticles

Last updated: 2026-10-07

- Declaration: `public partial class CPUParticles : Entity`
- Sources: [configuration](../../src/Scene/2D/CPUParticles.cs), [simulation/drawing](../../src/Scene/2D/CPUParticles.Simulation.cs), [storage/lifetime](../../src/Scene/2D/CPUParticles.Storage.cs)
- Inherits: [Entity](Entity.md), [CanvasItem](CanvasItem.md), [Node](Node.md)
- Component: [CPU particles](../components/cpu-particles.md)

## Description

A scene-owned CPU emitter. The existing internal process lane advances simulation when visible and allowed by scene pause/process policy. A headless SceneTree executes the same simulation. Explicit RequestParticlesProcess works detached and independent of SpeedScale, visibility and pause; no graphics device is required for authoring or state simulation.

Particles retain copied transient pose, velocity, lifetime and seeded random samples. Configuration mutations serialize through the scene owner/capture guards. Texture, Curve, Gradient and Material remain borrowed; their owner disposes them. Array getters/setters copy. Particle storage has capacity Amount (1..65536), two reusable state arrays and one sort-order array. A numerical failure rejects an individual simulation step before publishing that step; earlier completed seek subdivisions remain committed. Time/resource changes and point-array replacement are setup work, while prepared simulation, sampling and canvas replay reuse storage.

LocalCoords=true retains local positions and ordinary inherited canvas transforms. The default false retains positions in the canvas world at birth; later moving, scaling or making the emitter singular does not move existing quads. The renderer applies the current viewport/CanvasLayer transform to these commands separately from the emitter's logical pose; children, clipping and Z/Y order keep ordinary scene rules. With scene physics interpolation enabled, world births use captured global emitter poses from the complete physics tick, even with the node's default interpolation Off. Reset and edits outside the tick discard stale follow history.

## Example

```csharp
var sparks = new CPUParticles
{
    Amount = 64,
    Lifetime = .8,
    OneShot = true,
    Explosiveness = 1,
    UseFixedSeed = true,
    Seed = 42,
    InitialVelocityMin = 80,
    InitialVelocityMax = 140,
    Spread = 180,
    Gravity = new Vector2(0, 220),
    Color = new Color(1, .6f, .15f),
    ScaleAmountMin = 3,
    ScaleAmountMax = 5
};
root.AddChild(sparks); // root is an ordinary scene parent.
sparks.Restart(true);
```

For sheet animation, borrow a CanvasItemMaterial with ParticlesAnimation=true and positive ParticlesAnimHFrames/ParticlesAnimVFrames. AnimOffset selects a normalized frame offset; AnimSpeed describes cycles over lifetime. Geometry stays the full texture size while its sampled UVs select the frame. The four material settings are read on replay, so retained quads respond during paused simulation. The common shader custom channel contains `(0, age / cycleLifetime, animationPhase, randomizedLifetime / cycleLifetime)`; custom ShaderMaterial output requires the GPU backend.

## Simulation contract

Birth phases follow index/Amount, optional deterministic cycle/index phase jitter and Explosiveness compression. FractDelta applies only the remaining birth-frame motion; newborn normalized age is zero for the first sample. Seven source distributions execute: point; disk with uniform radius; projected sphere surface; rectangle; copied points; directed points with matching normal bases; uniform-area ring. Missing/mismatched optional normals and colors are ignored. Zero points emit at the origin. Independent ring radii are validated together before simulation.

Initial velocity uses Direction's angle, Spread in degrees and its sampled range. Linear, radial and tangential acceleration add to Gravity; orbit is clockwise turns per second. Damping changes scalar speed; channel curves and random values affect actual forces. Angle/angular velocity use degrees; alignment instead points Y toward velocity. Scale clamps its resulting axes to at least 0.00001; SplitScale chooses independent X/Y curves. Lifetime and initial gradients, base/point colors and the hue matrix combine in the existing color path. All finite scalar ranges keep the source paired-min/max repair policy.

OneShot stops births after the first cycle and drains live particles. Finished commits idle state before delivery, fires once per completed active run and permits restart/removal/disposal. One-shot property-list and completion callbacks are both attempted; callback failure is not replayed. Hidden ordinary processing retains state; explicit seeking still runs. Restart clears particles, clock, remainder and cycle; UseFixedSeed or keepSeed preserves Seed. Otherwise restart chooses another random seed; construction chooses a random initial seed as the actual source constructor does.

FixedFPS=0 uses frame delta. Positive FixedFPS uses a retained wall-time remainder and caps an ordinary frame at 0.1 seconds. Preprocess and explicit seek ignore SpeedScale and use FixedFPS or 30 Hz subdivisions. Requests/preprocess are bounded to 3600 seconds and 262144 subdivisions; extremely small lifetimes or scaled long frames that exceed the cycle budget fail explicitly. Equal Amount assignment clears live state. Other edits generally affect existing subsequent samples or future births rather than silently restarting the emitter.

## API summary

### Constructors

| Signature | Contract |
| --- | --- |
| `public CPUParticles()` | [Inherited scene lifecycle override.](#ctor) |

### Properties

| Signature | Contract |
| --- | --- |
| `public System.Int32 Amount { get; set; }` | [Gets or changes capacity; assignment clears all live particles.](#amount) |
| `public Electron2D.Curve AngleCurve { get; set; }` | [Gets or sets the borrowed lifetime curve for degrees.](#anglecurve) |
| `public System.Single AngleMax { get; set; }` | [Gets or sets the maximum degrees; repairs the paired range.](#anglemax) |
| `public System.Single AngleMin { get; set; }` | [Gets or sets the minimum degrees; repairs the paired range.](#anglemin) |
| `public Electron2D.Curve AngularVelocityCurve { get; set; }` | [Gets or sets the borrowed lifetime curve for degrees per second.](#angularvelocitycurve) |
| `public System.Single AngularVelocityMax { get; set; }` | [Gets or sets the maximum degrees per second; repairs the paired range.](#angularvelocitymax) |
| `public System.Single AngularVelocityMin { get; set; }` | [Gets or sets the minimum degrees per second; repairs the paired range.](#angularvelocitymin) |
| `public Electron2D.Curve AnimOffsetCurve { get; set; }` | [Gets or sets the borrowed lifetime curve for normalized animation offset.](#animoffsetcurve) |
| `public System.Single AnimOffsetMax { get; set; }` | [Gets or sets the maximum normalized animation offset; repairs the paired range.](#animoffsetmax) |
| `public System.Single AnimOffsetMin { get; set; }` | [Gets or sets the minimum normalized animation offset; repairs the paired range.](#animoffsetmin) |
| `public Electron2D.Curve AnimSpeedCurve { get; set; }` | [Gets or sets the borrowed lifetime curve for animation cycles over particle lifetime.](#animspeedcurve) |
| `public System.Single AnimSpeedMax { get; set; }` | [Gets or sets the maximum animation cycles over particle lifetime; repairs the paired range.](#animspeedmax) |
| `public System.Single AnimSpeedMin { get; set; }` | [Gets or sets the minimum animation cycles over particle lifetime; repairs the paired range.](#animspeedmin) |
| `public Electron2D.Color Color { get; set; }` | [Set the base particle color.](#color) |
| `public Electron2D.Gradient ColorInitialRamp { get; set; }` | [Borrow the random initial color gradient.](#colorinitialramp) |
| `public Electron2D.Gradient ColorRamp { get; set; }` | [Borrow the normalized lifetime color gradient.](#colorramp) |
| `public Electron2D.Curve DampingCurve { get; set; }` | [Gets or sets the borrowed lifetime curve for pixels per second squared.](#dampingcurve) |
| `public System.Single DampingMax { get; set; }` | [Gets or sets the maximum pixels per second squared; repairs the paired range.](#dampingmax) |
| `public System.Single DampingMin { get; set; }` | [Gets or sets the minimum pixels per second squared; repairs the paired range.](#dampingmin) |
| `public Electron2D.Vector2 Direction { get; set; }` | [Set the initial direction, interpreted by its angle.](#direction) |
| `public Electron2D.CPUParticles.DrawOrderMode DrawOrder { get; set; }` | [Gets or sets quad ordering.](#draworder) |
| `public Electron2D.Color[] EmissionColors { get; set; }` | [Gets a copy or assigns copied finite emission colors, bounded to 65536.](#emissioncolors) |
| `public Electron2D.Vector2[] EmissionNormals { get; set; }` | [Gets a copy or assigns copied finite emission normal bases, bounded to 65536.](#emissionnormals) |
| `public Electron2D.Vector2[] EmissionPoints { get; set; }` | [Gets a copy or assigns copied finite emission points, bounded to 65536.](#emissionpoints) |
| `public Electron2D.Vector2 EmissionRectExtents { get; set; }` | [Set half extents of rectangle emission.](#emissionrectextents) |
| `public System.Single EmissionRingInnerRadius { get; set; }` | [Set the inner ring radius in pixels.](#emissionringinnerradius) |
| `public System.Single EmissionRingRadius { get; set; }` | [Set the outer ring radius in pixels.](#emissionringradius) |
| `public Electron2D.CPUParticles.EmissionShapeMode EmissionShape { get; set; }` | [Gets or sets the distribution for future births.](#emissionshape) |
| `public System.Single EmissionSphereRadius { get; set; }` | [Set circular emission radius in pixels.](#emissionsphereradius) |
| `public System.Boolean Emitting { get; set; }` | [Gets or sets emission; stopping allows already active particles to drain.](#emitting) |
| `public System.Single Explosiveness { get; set; }` | [Compress birth phases toward the cycle start.](#explosiveness) |
| `public System.Int32 FixedFPS { get; set; }` | [Gets or sets fixed simulation updates per second; zero uses frame delta.](#fixedfps) |
| `public System.Boolean FractDelta { get; set; }` | [Advance newborn particles for only the remaining birth-frame duration.](#fractdelta) |
| `public Electron2D.Vector2 Gravity { get; set; }` | [Set acceleration in canvas pixels per second squared.](#gravity) |
| `public Electron2D.Curve HueVariationCurve { get; set; }` | [Gets or sets the borrowed lifetime curve for hue turns.](#huevariationcurve) |
| `public System.Single HueVariationMax { get; set; }` | [Gets or sets the maximum hue turns; repairs the paired range.](#huevariationmax) |
| `public System.Single HueVariationMin { get; set; }` | [Gets or sets the minimum hue turns; repairs the paired range.](#huevariationmin) |
| `public System.Single InitialVelocityMax { get; set; }` | [Gets or sets the maximum pixels per second; repairs the paired range.](#initialvelocitymax) |
| `public System.Single InitialVelocityMin { get; set; }` | [Gets or sets the minimum pixels per second; repairs the paired range.](#initialvelocitymin) |
| `public System.Double Lifetime { get; set; }` | [Gets or sets positive emission-cycle duration in seconds.](#lifetime) |
| `public System.Single LifetimeRandomness { get; set; }` | [Reduce individual lifetime by a random fraction.](#lifetimerandomness) |
| `public Electron2D.Curve LinearAccelCurve { get; set; }` | [Gets or sets the borrowed lifetime curve for pixels per second squared.](#linearaccelcurve) |
| `public System.Single LinearAccelMax { get; set; }` | [Gets or sets the maximum pixels per second squared; repairs the paired range.](#linearaccelmax) |
| `public System.Single LinearAccelMin { get; set; }` | [Gets or sets the minimum pixels per second squared; repairs the paired range.](#linearaccelmin) |
| `public System.Boolean LocalCoords { get; set; }` | [Choose local positions instead of retained world positions.](#localcoords) |
| `public System.Boolean OneShot { get; set; }` | [Emit one cycle, then drain automatically.](#oneshot) |
| `public Electron2D.Curve OrbitVelocityCurve { get; set; }` | [Gets or sets the borrowed lifetime curve for clockwise turns per second.](#orbitvelocitycurve) |
| `public System.Single OrbitVelocityMax { get; set; }` | [Gets or sets the maximum clockwise turns per second; repairs the paired range.](#orbitvelocitymax) |
| `public System.Single OrbitVelocityMin { get; set; }` | [Gets or sets the minimum clockwise turns per second; repairs the paired range.](#orbitvelocitymin) |
| `public System.Boolean ParticleFlagAlignY { get; set; }` | [Align the particle Y basis with its velocity.](#particleflagaligny) |
| `public System.Double Preprocess { get; set; }` | [Set seconds of simulation before the first active update.](#preprocess) |
| `public Electron2D.Curve RadialAccelCurve { get; set; }` | [Gets or sets the borrowed lifetime curve for pixels per second squared.](#radialaccelcurve) |
| `public System.Single RadialAccelMax { get; set; }` | [Gets or sets the maximum pixels per second squared; repairs the paired range.](#radialaccelmax) |
| `public System.Single RadialAccelMin { get; set; }` | [Gets or sets the minimum pixels per second squared; repairs the paired range.](#radialaccelmin) |
| `public System.Single Randomness { get; set; }` | [Randomize birth phases within index intervals.](#randomness) |
| `public Electron2D.Curve ScaleAmountCurve { get; set; }` | [Gets or sets the borrowed lifetime curve for scale multiplier.](#scaleamountcurve) |
| `public System.Single ScaleAmountMax { get; set; }` | [Gets or sets the maximum scale multiplier; repairs the paired range.](#scaleamountmax) |
| `public System.Single ScaleAmountMin { get; set; }` | [Gets or sets the minimum scale multiplier; repairs the paired range.](#scaleamountmin) |
| `public Electron2D.Curve ScaleCurveX { get; set; }` | [Borrow the X scale curve used with SplitScale.](#scalecurvex) |
| `public Electron2D.Curve ScaleCurveY { get; set; }` | [Borrow the Y scale curve used with SplitScale.](#scalecurvey) |
| `public System.UInt32 Seed { get; set; }` | [Set the seed used for future births.](#seed) |
| `public System.Double SpeedScale { get; set; }` | [Scale ordinary simulation time; zero pauses time.](#speedscale) |
| `public System.Boolean SplitScale { get; set; }` | [Use independent X and Y scale curves.](#splitscale) |
| `public System.Single Spread { get; set; }` | [Set initial angular spread in degrees.](#spread) |
| `public Electron2D.Curve TangentialAccelCurve { get; set; }` | [Gets or sets the borrowed lifetime curve for pixels per second squared.](#tangentialaccelcurve) |
| `public System.Single TangentialAccelMax { get; set; }` | [Gets or sets the maximum pixels per second squared; repairs the paired range.](#tangentialaccelmax) |
| `public System.Single TangentialAccelMin { get; set; }` | [Gets or sets the minimum pixels per second squared; repairs the paired range.](#tangentialaccelmin) |
| `public Electron2D.Texture Texture { get; set; }` | [Gets or sets the borrowed particle image.](#texture) |
| `public System.Boolean UseFixedSeed { get; set; }` | [Preserve Seed across restart.](#usefixedseed) |

### Methods

| Signature | Contract |
| --- | --- |
| `protected override System.Func<Electron2D.Node> CreateSceneInstanceFactory()` | [Inherited scene lifecycle override.](#createsceneinstancefactory) |
| `protected override System.Void Dispose(System.Boolean disposing)` | [Inherited scene lifecycle override](#dispose) |
| `public override System.String[] GetConfigurationWarnings()` | [Inherited scene lifecycle override.](#getconfigurationwarnings) |
| `public Electron2D.Curve GetParamCurve(Electron2D.CPUParticles.Parameter parameter)` | [Returns the borrowed normalized lifetime curve of a channel.](#getparamcurve) |
| `public System.Single GetParamMax(Electron2D.CPUParticles.Parameter parameter)` | [Returns a channel's maximum.](#getparammax) |
| `public System.Single GetParamMin(Electron2D.CPUParticles.Parameter parameter)` | [Returns a channel's minimum.](#getparammin) |
| `public System.Boolean GetParticleFlag(Electron2D.CPUParticles.ParticleFlags particleFlag)` | [Returns the applicable alignment flag.](#getparticleflag) |
| `protected override System.Collections.Generic.IEnumerable<Electron2D.PropertyDescriptor> GetPropertyDescriptors()` | [Inherited scene lifecycle override.](#getpropertydescriptors) |
| `protected override System.Void OnDraw()` | [Inherited scene lifecycle override.](#ondraw) |
| `protected override System.Void OnNotification(System.Int32 what)` | [Inherited scene lifecycle override](#onnotification) |
| `public System.Void RequestParticlesProcess(System.Single processTime, System.Single processTimeResidual = 0f)` | [Processes explicit emitting and nonemitting intervals independently of SpeedScale.](#requestparticlesprocess) |
| `public System.Void Restart(System.Boolean keepSeed = false)` | [Clears particles and time, enables emission and optionally retains the current seed.](#restart) |
| `public System.Void SetParamCurve(Electron2D.CPUParticles.Parameter parameter, Electron2D.Curve curve)` | [Assigns a borrowed curve, initializing an empty curve to its channel's unit setup.](#setparamcurve) |
| `public System.Void SetParamMax(Electron2D.CPUParticles.Parameter parameter, System.Single value)` | [Sets a maximum and lowers the minimum if needed.](#setparammax) |
| `public System.Void SetParamMin(Electron2D.CPUParticles.Parameter parameter, System.Single value)` | [Sets a minimum and raises the maximum if needed.](#setparammin) |
| `public System.Void SetParticleFlag(Electron2D.CPUParticles.ParticleFlags particleFlag, System.Boolean enable)` | [Sets the applicable alignment flag.](#setparticleflag) |

### Events

| Signature | Contract |
| --- | --- |
| `public event System.Action Finished` | [Occurs once when an active emission drains, after committing the idle state.](#finished) |

## Enumerations

- [DrawOrderMode](CPUParticles.DrawOrderMode.md)
- [EmissionShapeMode](CPUParticles.EmissionShapeMode.md)
- [Parameter](CPUParticles.Parameter.md)
- [ParticleFlags](CPUParticles.ParticleFlags.md)

## Member descriptions

### .ctor

`public CPUParticles()`

Inherited scene lifecycle override.

### Finished

`public event System.Action Finished`

Occurs once when an active emission drains, after committing the idle state.

Handler failure propagates without replay. A handler may restart or dispose the emitter.

### CreateSceneInstanceFactory

`protected override System.Func<Electron2D.Node> CreateSceneInstanceFactory()`

Inherited scene lifecycle override.

### Dispose

`protected override System.Void Dispose(System.Boolean disposing)`



### GetConfigurationWarnings

`public override System.String[] GetConfigurationWarnings()`

Inherited scene lifecycle override.

### GetParamCurve

`public Electron2D.Curve GetParamCurve(Electron2D.CPUParticles.Parameter parameter)`

Returns the borrowed normalized lifetime curve of a channel.

The borrowed curve or null.

- `parameter`: Selectable channel.

### GetParamMax

`public System.Single GetParamMax(Electron2D.CPUParticles.Parameter parameter)`

Returns a channel's maximum.

Current maximum.

- `parameter`: Selectable channel.

### GetParamMin

`public System.Single GetParamMin(Electron2D.CPUParticles.Parameter parameter)`

Returns a channel's minimum.

Current minimum.

- `parameter`: Selectable channel.

### GetParticleFlag

`public System.Boolean GetParticleFlag(Electron2D.CPUParticles.ParticleFlags particleFlag)`

Returns the applicable alignment flag.

Current flag value.

- `particleFlag`: AlignYToVelocity; reserved 3D flags are not selectable.

### GetPropertyDescriptors

`protected override System.Collections.Generic.IEnumerable<Electron2D.PropertyDescriptor> GetPropertyDescriptors()`

Inherited scene lifecycle override.

### OnDraw

`protected override System.Void OnDraw()`

Inherited scene lifecycle override.

### OnNotification

`protected override System.Void OnNotification(System.Int32 what)`



### RequestParticlesProcess

`public System.Void RequestParticlesProcess(System.Single processTime, System.Single processTimeResidual = 0f)`

Processes explicit emitting and nonemitting intervals independently of SpeedScale.

Uses FixedFPS or 30 Hz subdivisions. Each request is bounded to 3600 seconds and 262144 steps. Explicit requests work detached and while speed is zero. Emitting reflects the last nonempty interval. Invalid inputs fail before simulation. Finished handlers run after the final state commits.

- `processTime`: Nonnegative seconds processed with emission enabled.
- `processTimeResidual`: Nonnegative seconds processed with emission disabled.

Throws `System.ArgumentOutOfRangeException`: An interval exceeds the bounds or is nonfinite/negative.

Throws `System.InvalidOperationException`: Reentrant simulation, capture, off-owner access or numeric overflow.

### Restart

`public System.Void Restart(System.Boolean keepSeed = false)`

Clears particles and time, enables emission and optionally retains the current seed.

Preparation occurs on the next update; detached restart needs no graphics backend.

- `keepSeed`: Preserve Seed even when UseFixedSeed is false.

### SetParamCurve

`public System.Void SetParamCurve(Electron2D.CPUParticles.Parameter parameter, Electron2D.Curve curve)`

Assigns a borrowed curve, initializing an empty curve to its channel's unit setup.

- `parameter`: Selectable channel.
- `curve`: Borrowed live curve or null.

### SetParamMax

`public System.Void SetParamMax(Electron2D.CPUParticles.Parameter parameter, System.Single value)`

Sets a maximum and lowers the minimum if needed.

- `parameter`: Selectable channel.
- `value`: Finite scalar in channel units.

### SetParamMin

`public System.Void SetParamMin(Electron2D.CPUParticles.Parameter parameter, System.Single value)`

Sets a minimum and raises the maximum if needed.

- `parameter`: Selectable channel.
- `value`: Finite scalar in channel units.

### SetParticleFlag

`public System.Void SetParticleFlag(Electron2D.CPUParticles.ParticleFlags particleFlag, System.Boolean enable)`

Sets the applicable alignment flag.

- `particleFlag`: AlignYToVelocity.
- `enable`: Whether to align the basis with velocity.

### Amount

`public System.Int32 Amount { get; set; }`

Gets or changes capacity; assignment clears all live particles.

Eight initially; valid range is 1..65536.

Throws `System.ArgumentOutOfRangeException`: Capacity is outside the bounded range.

Attached reads require the scene owner; setters reject capture/reentrant mutation and disposed resources before publication. Numeric inputs must satisfy the limits above; invalid assignment preserves the previous value. Resources remain borrowed; exported arrays are independent copies.

### AngleCurve

`public Electron2D.Curve AngleCurve { get; set; }`

Gets or sets the borrowed lifetime curve for degrees.

Null initially.

Attached reads require the scene owner; setters reject capture/reentrant mutation and disposed resources before publication. Numeric inputs must satisfy the limits above; invalid assignment preserves the previous value. Resources remain borrowed; exported arrays are independent copies.

### AngleMax

`public System.Single AngleMax { get; set; }`

Gets or sets the maximum degrees; repairs the paired range.

0 initially; finite values.

Attached reads require the scene owner; setters reject capture/reentrant mutation and disposed resources before publication. Numeric inputs must satisfy the limits above; invalid assignment preserves the previous value. Resources remain borrowed; exported arrays are independent copies.

### AngleMin

`public System.Single AngleMin { get; set; }`

Gets or sets the minimum degrees; repairs the paired range.

0 initially; finite values.

Attached reads require the scene owner; setters reject capture/reentrant mutation and disposed resources before publication. Numeric inputs must satisfy the limits above; invalid assignment preserves the previous value. Resources remain borrowed; exported arrays are independent copies.

### AngularVelocityCurve

`public Electron2D.Curve AngularVelocityCurve { get; set; }`

Gets or sets the borrowed lifetime curve for degrees per second.

Null initially.

Attached reads require the scene owner; setters reject capture/reentrant mutation and disposed resources before publication. Numeric inputs must satisfy the limits above; invalid assignment preserves the previous value. Resources remain borrowed; exported arrays are independent copies.

### AngularVelocityMax

`public System.Single AngularVelocityMax { get; set; }`

Gets or sets the maximum degrees per second; repairs the paired range.

0 initially; finite values.

Attached reads require the scene owner; setters reject capture/reentrant mutation and disposed resources before publication. Numeric inputs must satisfy the limits above; invalid assignment preserves the previous value. Resources remain borrowed; exported arrays are independent copies.

### AngularVelocityMin

`public System.Single AngularVelocityMin { get; set; }`

Gets or sets the minimum degrees per second; repairs the paired range.

0 initially; finite values.

Attached reads require the scene owner; setters reject capture/reentrant mutation and disposed resources before publication. Numeric inputs must satisfy the limits above; invalid assignment preserves the previous value. Resources remain borrowed; exported arrays are independent copies.

### AnimOffsetCurve

`public Electron2D.Curve AnimOffsetCurve { get; set; }`

Gets or sets the borrowed lifetime curve for normalized animation offset.

Null initially.

Attached reads require the scene owner; setters reject capture/reentrant mutation and disposed resources before publication. Numeric inputs must satisfy the limits above; invalid assignment preserves the previous value. Resources remain borrowed; exported arrays are independent copies.

### AnimOffsetMax

`public System.Single AnimOffsetMax { get; set; }`

Gets or sets the maximum normalized animation offset; repairs the paired range.

0 initially; finite values.

Attached reads require the scene owner; setters reject capture/reentrant mutation and disposed resources before publication. Numeric inputs must satisfy the limits above; invalid assignment preserves the previous value. Resources remain borrowed; exported arrays are independent copies.

### AnimOffsetMin

`public System.Single AnimOffsetMin { get; set; }`

Gets or sets the minimum normalized animation offset; repairs the paired range.

0 initially; finite values.

Attached reads require the scene owner; setters reject capture/reentrant mutation and disposed resources before publication. Numeric inputs must satisfy the limits above; invalid assignment preserves the previous value. Resources remain borrowed; exported arrays are independent copies.

### AnimSpeedCurve

`public Electron2D.Curve AnimSpeedCurve { get; set; }`

Gets or sets the borrowed lifetime curve for animation cycles over particle lifetime.

Null initially.

Attached reads require the scene owner; setters reject capture/reentrant mutation and disposed resources before publication. Numeric inputs must satisfy the limits above; invalid assignment preserves the previous value. Resources remain borrowed; exported arrays are independent copies.

### AnimSpeedMax

`public System.Single AnimSpeedMax { get; set; }`

Gets or sets the maximum animation cycles over particle lifetime; repairs the paired range.

0 initially; finite values.

Attached reads require the scene owner; setters reject capture/reentrant mutation and disposed resources before publication. Numeric inputs must satisfy the limits above; invalid assignment preserves the previous value. Resources remain borrowed; exported arrays are independent copies.

### AnimSpeedMin

`public System.Single AnimSpeedMin { get; set; }`

Gets or sets the minimum animation cycles over particle lifetime; repairs the paired range.

0 initially; finite values.

Attached reads require the scene owner; setters reject capture/reentrant mutation and disposed resources before publication. Numeric inputs must satisfy the limits above; invalid assignment preserves the previous value. Resources remain borrowed; exported arrays are independent copies.

### Color

`public Electron2D.Color Color { get; set; }`

Set the base particle color.

opaque white initially. Values are finite; resources remain caller-owned.

Attached reads require the scene owner; setters reject capture/reentrant mutation and disposed resources before publication. Numeric inputs must satisfy the limits above; invalid assignment preserves the previous value. Resources remain borrowed; exported arrays are independent copies.

### ColorInitialRamp

`public Electron2D.Gradient ColorInitialRamp { get; set; }`

Borrow the random initial color gradient.

null initially. Values are finite; resources remain caller-owned.

Attached reads require the scene owner; setters reject capture/reentrant mutation and disposed resources before publication. Numeric inputs must satisfy the limits above; invalid assignment preserves the previous value. Resources remain borrowed; exported arrays are independent copies.

### ColorRamp

`public Electron2D.Gradient ColorRamp { get; set; }`

Borrow the normalized lifetime color gradient.

null initially. Values are finite; resources remain caller-owned.

Attached reads require the scene owner; setters reject capture/reentrant mutation and disposed resources before publication. Numeric inputs must satisfy the limits above; invalid assignment preserves the previous value. Resources remain borrowed; exported arrays are independent copies.

### DampingCurve

`public Electron2D.Curve DampingCurve { get; set; }`

Gets or sets the borrowed lifetime curve for pixels per second squared.

Null initially.

Attached reads require the scene owner; setters reject capture/reentrant mutation and disposed resources before publication. Numeric inputs must satisfy the limits above; invalid assignment preserves the previous value. Resources remain borrowed; exported arrays are independent copies.

### DampingMax

`public System.Single DampingMax { get; set; }`

Gets or sets the maximum pixels per second squared; repairs the paired range.

0 initially; finite values.

Attached reads require the scene owner; setters reject capture/reentrant mutation and disposed resources before publication. Numeric inputs must satisfy the limits above; invalid assignment preserves the previous value. Resources remain borrowed; exported arrays are independent copies.

### DampingMin

`public System.Single DampingMin { get; set; }`

Gets or sets the minimum pixels per second squared; repairs the paired range.

0 initially; finite values.

Attached reads require the scene owner; setters reject capture/reentrant mutation and disposed resources before publication. Numeric inputs must satisfy the limits above; invalid assignment preserves the previous value. Resources remain borrowed; exported arrays are independent copies.

### Direction

`public Electron2D.Vector2 Direction { get; set; }`

Set the initial direction, interpreted by its angle.

(1,0) initially. Values are finite; resources remain caller-owned.

Attached reads require the scene owner; setters reject capture/reentrant mutation and disposed resources before publication. Numeric inputs must satisfy the limits above; invalid assignment preserves the previous value. Resources remain borrowed; exported arrays are independent copies.

### DrawOrder

`public Electron2D.CPUParticles.DrawOrderMode DrawOrder { get; set; }`

Gets or sets quad ordering.

Index initially.

Attached reads require the scene owner; setters reject capture/reentrant mutation and disposed resources before publication. Numeric inputs must satisfy the limits above; invalid assignment preserves the previous value. Resources remain borrowed; exported arrays are independent copies.

### EmissionColors

`public Electron2D.Color[] EmissionColors { get; set; }`

Gets a copy or assigns copied finite emission colors, bounded to 65536.

Attached reads require the scene owner; setters reject capture/reentrant mutation and disposed resources before publication. Numeric inputs must satisfy the limits above; invalid assignment preserves the previous value. Resources remain borrowed; exported arrays are independent copies.

### EmissionNormals

`public Electron2D.Vector2[] EmissionNormals { get; set; }`

Gets a copy or assigns copied finite emission normal bases, bounded to 65536.

Attached reads require the scene owner; setters reject capture/reentrant mutation and disposed resources before publication. Numeric inputs must satisfy the limits above; invalid assignment preserves the previous value. Resources remain borrowed; exported arrays are independent copies.

### EmissionPoints

`public Electron2D.Vector2[] EmissionPoints { get; set; }`

Gets a copy or assigns copied finite emission points, bounded to 65536.

Attached reads require the scene owner; setters reject capture/reentrant mutation and disposed resources before publication. Numeric inputs must satisfy the limits above; invalid assignment preserves the previous value. Resources remain borrowed; exported arrays are independent copies.

### EmissionRectExtents

`public Electron2D.Vector2 EmissionRectExtents { get; set; }`

Set half extents of rectangle emission.

(1,1) initially. Values are finite; resources remain caller-owned.

Attached reads require the scene owner; setters reject capture/reentrant mutation and disposed resources before publication. Numeric inputs must satisfy the limits above; invalid assignment preserves the previous value. Resources remain borrowed; exported arrays are independent copies.

### EmissionRingInnerRadius

`public System.Single EmissionRingInnerRadius { get; set; }`

Set the inner ring radius in pixels.

.8 initially. Values are finite; resources remain caller-owned.

Attached reads require the scene owner; setters reject capture/reentrant mutation and disposed resources before publication. Numeric inputs must satisfy the limits above; invalid assignment preserves the previous value. Resources remain borrowed; exported arrays are independent copies.

### EmissionRingRadius

`public System.Single EmissionRingRadius { get; set; }`

Set the outer ring radius in pixels.

1 initially. Values are finite; resources remain caller-owned.

Attached reads require the scene owner; setters reject capture/reentrant mutation and disposed resources before publication. Numeric inputs must satisfy the limits above; invalid assignment preserves the previous value. Resources remain borrowed; exported arrays are independent copies.

### EmissionShape

`public Electron2D.CPUParticles.EmissionShapeMode EmissionShape { get; set; }`

Gets or sets the distribution for future births.

Point initially. Mismatched point normals/colors are ignored.

Attached reads require the scene owner; setters reject capture/reentrant mutation and disposed resources before publication. Numeric inputs must satisfy the limits above; invalid assignment preserves the previous value. Resources remain borrowed; exported arrays are independent copies.

### EmissionSphereRadius

`public System.Single EmissionSphereRadius { get; set; }`

Set circular emission radius in pixels.

1 initially. Values are finite; resources remain caller-owned.

Attached reads require the scene owner; setters reject capture/reentrant mutation and disposed resources before publication. Numeric inputs must satisfy the limits above; invalid assignment preserves the previous value. Resources remain borrowed; exported arrays are independent copies.

### Emitting

`public System.Boolean Emitting { get; set; }`

Gets or sets emission; stopping allows already active particles to drain.

True initially. Starting an idle emitter enables its internal processing.

Attached reads require the scene owner; setters reject capture/reentrant mutation and disposed resources before publication. Numeric inputs must satisfy the limits above; invalid assignment preserves the previous value. Resources remain borrowed; exported arrays are independent copies.

### Explosiveness

`public System.Single Explosiveness { get; set; }`

Compress birth phases toward the cycle start.

0 initially. Values are finite; resources remain caller-owned.

Attached reads require the scene owner; setters reject capture/reentrant mutation and disposed resources before publication. Numeric inputs must satisfy the limits above; invalid assignment preserves the previous value. Resources remain borrowed; exported arrays are independent copies.

### FixedFPS

`public System.Int32 FixedFPS { get; set; }`

Gets or sets fixed simulation updates per second; zero uses frame delta.

Zero initially; valid range 0..1000. Ordinary fixed stepping caps an input frame at 0.1 seconds.

Attached reads require the scene owner; setters reject capture/reentrant mutation and disposed resources before publication. Numeric inputs must satisfy the limits above; invalid assignment preserves the previous value. Resources remain borrowed; exported arrays are independent copies.

### FractDelta

`public System.Boolean FractDelta { get; set; }`

Advance newborn particles for only the remaining birth-frame duration.

true initially. Values are finite; resources remain caller-owned.

Attached reads require the scene owner; setters reject capture/reentrant mutation and disposed resources before publication. Numeric inputs must satisfy the limits above; invalid assignment preserves the previous value. Resources remain borrowed; exported arrays are independent copies.

### Gravity

`public Electron2D.Vector2 Gravity { get; set; }`

Set acceleration in canvas pixels per second squared.

(0,980) initially. Values are finite; resources remain caller-owned.

Attached reads require the scene owner; setters reject capture/reentrant mutation and disposed resources before publication. Numeric inputs must satisfy the limits above; invalid assignment preserves the previous value. Resources remain borrowed; exported arrays are independent copies.

### HueVariationCurve

`public Electron2D.Curve HueVariationCurve { get; set; }`

Gets or sets the borrowed lifetime curve for hue turns.

Null initially.

Attached reads require the scene owner; setters reject capture/reentrant mutation and disposed resources before publication. Numeric inputs must satisfy the limits above; invalid assignment preserves the previous value. Resources remain borrowed; exported arrays are independent copies.

### HueVariationMax

`public System.Single HueVariationMax { get; set; }`

Gets or sets the maximum hue turns; repairs the paired range.

0 initially; finite values.

Attached reads require the scene owner; setters reject capture/reentrant mutation and disposed resources before publication. Numeric inputs must satisfy the limits above; invalid assignment preserves the previous value. Resources remain borrowed; exported arrays are independent copies.

### HueVariationMin

`public System.Single HueVariationMin { get; set; }`

Gets or sets the minimum hue turns; repairs the paired range.

0 initially; finite values.

Attached reads require the scene owner; setters reject capture/reentrant mutation and disposed resources before publication. Numeric inputs must satisfy the limits above; invalid assignment preserves the previous value. Resources remain borrowed; exported arrays are independent copies.

### InitialVelocityMax

`public System.Single InitialVelocityMax { get; set; }`

Gets or sets the maximum pixels per second; repairs the paired range.

0 initially; finite values.

Attached reads require the scene owner; setters reject capture/reentrant mutation and disposed resources before publication. Numeric inputs must satisfy the limits above; invalid assignment preserves the previous value. Resources remain borrowed; exported arrays are independent copies.

### InitialVelocityMin

`public System.Single InitialVelocityMin { get; set; }`

Gets or sets the minimum pixels per second; repairs the paired range.

0 initially; finite values.

Attached reads require the scene owner; setters reject capture/reentrant mutation and disposed resources before publication. Numeric inputs must satisfy the limits above; invalid assignment preserves the previous value. Resources remain borrowed; exported arrays are independent copies.

### Lifetime

`public System.Double Lifetime { get; set; }`

Gets or sets positive emission-cycle duration in seconds.

One second initially; particle lifetime randomness scales this duration.

Throws `System.ArgumentOutOfRangeException`: Duration is nonfinite or not positive.

Attached reads require the scene owner; setters reject capture/reentrant mutation and disposed resources before publication. Numeric inputs must satisfy the limits above; invalid assignment preserves the previous value. Resources remain borrowed; exported arrays are independent copies.

### LifetimeRandomness

`public System.Single LifetimeRandomness { get; set; }`

Reduce individual lifetime by a random fraction.

0 initially. Values are finite; resources remain caller-owned.

Attached reads require the scene owner; setters reject capture/reentrant mutation and disposed resources before publication. Numeric inputs must satisfy the limits above; invalid assignment preserves the previous value. Resources remain borrowed; exported arrays are independent copies.

### LinearAccelCurve

`public Electron2D.Curve LinearAccelCurve { get; set; }`

Gets or sets the borrowed lifetime curve for pixels per second squared.

Null initially.

Attached reads require the scene owner; setters reject capture/reentrant mutation and disposed resources before publication. Numeric inputs must satisfy the limits above; invalid assignment preserves the previous value. Resources remain borrowed; exported arrays are independent copies.

### LinearAccelMax

`public System.Single LinearAccelMax { get; set; }`

Gets or sets the maximum pixels per second squared; repairs the paired range.

0 initially; finite values.

Attached reads require the scene owner; setters reject capture/reentrant mutation and disposed resources before publication. Numeric inputs must satisfy the limits above; invalid assignment preserves the previous value. Resources remain borrowed; exported arrays are independent copies.

### LinearAccelMin

`public System.Single LinearAccelMin { get; set; }`

Gets or sets the minimum pixels per second squared; repairs the paired range.

0 initially; finite values.

Attached reads require the scene owner; setters reject capture/reentrant mutation and disposed resources before publication. Numeric inputs must satisfy the limits above; invalid assignment preserves the previous value. Resources remain borrowed; exported arrays are independent copies.

### LocalCoords

`public System.Boolean LocalCoords { get; set; }`

Choose local positions instead of retained world positions.

false initially. Values are finite; resources remain caller-owned.

Attached reads require the scene owner; setters reject capture/reentrant mutation and disposed resources before publication. Numeric inputs must satisfy the limits above; invalid assignment preserves the previous value. Resources remain borrowed; exported arrays are independent copies.

### OneShot

`public System.Boolean OneShot { get; set; }`

Emit one cycle, then drain automatically.

false initially. Values are finite; resources remain caller-owned.

Attached reads require the scene owner; setters reject capture/reentrant mutation and disposed resources before publication. Numeric inputs must satisfy the limits above; invalid assignment preserves the previous value. Resources remain borrowed; exported arrays are independent copies.

### OrbitVelocityCurve

`public Electron2D.Curve OrbitVelocityCurve { get; set; }`

Gets or sets the borrowed lifetime curve for clockwise turns per second.

Null initially.

Attached reads require the scene owner; setters reject capture/reentrant mutation and disposed resources before publication. Numeric inputs must satisfy the limits above; invalid assignment preserves the previous value. Resources remain borrowed; exported arrays are independent copies.

### OrbitVelocityMax

`public System.Single OrbitVelocityMax { get; set; }`

Gets or sets the maximum clockwise turns per second; repairs the paired range.

0 initially; finite values.

Attached reads require the scene owner; setters reject capture/reentrant mutation and disposed resources before publication. Numeric inputs must satisfy the limits above; invalid assignment preserves the previous value. Resources remain borrowed; exported arrays are independent copies.

### OrbitVelocityMin

`public System.Single OrbitVelocityMin { get; set; }`

Gets or sets the minimum clockwise turns per second; repairs the paired range.

0 initially; finite values.

Attached reads require the scene owner; setters reject capture/reentrant mutation and disposed resources before publication. Numeric inputs must satisfy the limits above; invalid assignment preserves the previous value. Resources remain borrowed; exported arrays are independent copies.

### ParticleFlagAlignY

`public System.Boolean ParticleFlagAlignY { get; set; }`

Align the particle Y basis with its velocity.

false initially. Values are finite; resources remain caller-owned.

Attached reads require the scene owner; setters reject capture/reentrant mutation and disposed resources before publication. Numeric inputs must satisfy the limits above; invalid assignment preserves the previous value. Resources remain borrowed; exported arrays are independent copies.

### Preprocess

`public System.Double Preprocess { get; set; }`

Set seconds of simulation before the first active update.

0 initially. Values are finite; resources remain caller-owned.

Attached reads require the scene owner; setters reject capture/reentrant mutation and disposed resources before publication. Numeric inputs must satisfy the limits above; invalid assignment preserves the previous value. Resources remain borrowed; exported arrays are independent copies.

### RadialAccelCurve

`public Electron2D.Curve RadialAccelCurve { get; set; }`

Gets or sets the borrowed lifetime curve for pixels per second squared.

Null initially.

Attached reads require the scene owner; setters reject capture/reentrant mutation and disposed resources before publication. Numeric inputs must satisfy the limits above; invalid assignment preserves the previous value. Resources remain borrowed; exported arrays are independent copies.

### RadialAccelMax

`public System.Single RadialAccelMax { get; set; }`

Gets or sets the maximum pixels per second squared; repairs the paired range.

0 initially; finite values.

Attached reads require the scene owner; setters reject capture/reentrant mutation and disposed resources before publication. Numeric inputs must satisfy the limits above; invalid assignment preserves the previous value. Resources remain borrowed; exported arrays are independent copies.

### RadialAccelMin

`public System.Single RadialAccelMin { get; set; }`

Gets or sets the minimum pixels per second squared; repairs the paired range.

0 initially; finite values.

Attached reads require the scene owner; setters reject capture/reentrant mutation and disposed resources before publication. Numeric inputs must satisfy the limits above; invalid assignment preserves the previous value. Resources remain borrowed; exported arrays are independent copies.

### Randomness

`public System.Single Randomness { get; set; }`

Randomize birth phases within index intervals.

0 initially. Values are finite; resources remain caller-owned.

Attached reads require the scene owner; setters reject capture/reentrant mutation and disposed resources before publication. Numeric inputs must satisfy the limits above; invalid assignment preserves the previous value. Resources remain borrowed; exported arrays are independent copies.

### ScaleAmountCurve

`public Electron2D.Curve ScaleAmountCurve { get; set; }`

Gets or sets the borrowed lifetime curve for scale multiplier.

Null initially.

Attached reads require the scene owner; setters reject capture/reentrant mutation and disposed resources before publication. Numeric inputs must satisfy the limits above; invalid assignment preserves the previous value. Resources remain borrowed; exported arrays are independent copies.

### ScaleAmountMax

`public System.Single ScaleAmountMax { get; set; }`

Gets or sets the maximum scale multiplier; repairs the paired range.

1 initially; finite values.

Attached reads require the scene owner; setters reject capture/reentrant mutation and disposed resources before publication. Numeric inputs must satisfy the limits above; invalid assignment preserves the previous value. Resources remain borrowed; exported arrays are independent copies.

### ScaleAmountMin

`public System.Single ScaleAmountMin { get; set; }`

Gets or sets the minimum scale multiplier; repairs the paired range.

1 initially; finite values.

Attached reads require the scene owner; setters reject capture/reentrant mutation and disposed resources before publication. Numeric inputs must satisfy the limits above; invalid assignment preserves the previous value. Resources remain borrowed; exported arrays are independent copies.

### ScaleCurveX

`public Electron2D.Curve ScaleCurveX { get; set; }`

Borrow the X scale curve used with SplitScale.

null initially. Values are finite; resources remain caller-owned.

Attached reads require the scene owner; setters reject capture/reentrant mutation and disposed resources before publication. Numeric inputs must satisfy the limits above; invalid assignment preserves the previous value. Resources remain borrowed; exported arrays are independent copies.

### ScaleCurveY

`public Electron2D.Curve ScaleCurveY { get; set; }`

Borrow the Y scale curve used with SplitScale.

null initially. Values are finite; resources remain caller-owned.

Attached reads require the scene owner; setters reject capture/reentrant mutation and disposed resources before publication. Numeric inputs must satisfy the limits above; invalid assignment preserves the previous value. Resources remain borrowed; exported arrays are independent copies.

### Seed

`public System.UInt32 Seed { get; set; }`

Set the seed used for future births.

A random initial seed; fixed replay uses the explicitly assigned seed.

Attached reads require the scene owner; setters reject capture/reentrant mutation and disposed resources before publication. Numeric inputs must satisfy the limits above; invalid assignment preserves the previous value. Resources remain borrowed; exported arrays are independent copies.

### SpeedScale

`public System.Double SpeedScale { get; set; }`

Scale ordinary simulation time; zero pauses time.

1 initially. Values are finite; resources remain caller-owned.

Attached reads require the scene owner; setters reject capture/reentrant mutation and disposed resources before publication. Numeric inputs must satisfy the limits above; invalid assignment preserves the previous value. Resources remain borrowed; exported arrays are independent copies.

### SplitScale

`public System.Boolean SplitScale { get; set; }`

Use independent X and Y scale curves.

false initially. Values are finite; resources remain caller-owned.

Attached reads require the scene owner; setters reject capture/reentrant mutation and disposed resources before publication. Numeric inputs must satisfy the limits above; invalid assignment preserves the previous value. Resources remain borrowed; exported arrays are independent copies.

### Spread

`public System.Single Spread { get; set; }`

Set initial angular spread in degrees.

45 initially. Values are finite; resources remain caller-owned.

Attached reads require the scene owner; setters reject capture/reentrant mutation and disposed resources before publication. Numeric inputs must satisfy the limits above; invalid assignment preserves the previous value. Resources remain borrowed; exported arrays are independent copies.

### TangentialAccelCurve

`public Electron2D.Curve TangentialAccelCurve { get; set; }`

Gets or sets the borrowed lifetime curve for pixels per second squared.

Null initially.

Attached reads require the scene owner; setters reject capture/reentrant mutation and disposed resources before publication. Numeric inputs must satisfy the limits above; invalid assignment preserves the previous value. Resources remain borrowed; exported arrays are independent copies.

### TangentialAccelMax

`public System.Single TangentialAccelMax { get; set; }`

Gets or sets the maximum pixels per second squared; repairs the paired range.

0 initially; finite values.

Attached reads require the scene owner; setters reject capture/reentrant mutation and disposed resources before publication. Numeric inputs must satisfy the limits above; invalid assignment preserves the previous value. Resources remain borrowed; exported arrays are independent copies.

### TangentialAccelMin

`public System.Single TangentialAccelMin { get; set; }`

Gets or sets the minimum pixels per second squared; repairs the paired range.

0 initially; finite values.

Attached reads require the scene owner; setters reject capture/reentrant mutation and disposed resources before publication. Numeric inputs must satisfy the limits above; invalid assignment preserves the previous value. Resources remain borrowed; exported arrays are independent copies.

### Texture

`public Electron2D.Texture Texture { get; set; }`

Gets or sets the borrowed particle image.

Null initially, drawing one-pixel centered quads. Sprite-sheet geometry is the full image size.

Attached reads require the scene owner; setters reject capture/reentrant mutation and disposed resources before publication. Numeric inputs must satisfy the limits above; invalid assignment preserves the previous value. Resources remain borrowed; exported arrays are independent copies.

### UseFixedSeed

`public System.Boolean UseFixedSeed { get; set; }`

Preserve Seed across restart.

false initially. Values are finite; resources remain caller-owned.

Attached reads require the scene owner; setters reject capture/reentrant mutation and disposed resources before publication. Numeric inputs must satisfy the limits above; invalid assignment preserves the previous value. Resources remain borrowed; exported arrays are independent copies.

## Storage, lifetime and verification

Built-in CPUParticles registration and stored typed descriptors persist configuration and borrowed resource graphs through PackedScene/.e2dscene. Live particles, clock, order and native identities are never stored. Min/max pairs, the generic initial-velocity curve, arrays, material settings and resource aliases survive restoration. Curve has a bounded versioned byte snapshot preserving domain/value limits, resolution and exact point/tangent/mode records; Gradient stores copied offsets/colors and interpolation policies. Resource loading creates independent owned encoded resources, while ordinary in-memory scene copying keeps its configured resource policy.

CPUParticlesTests checks defaults/guards, all distributions and forces, fractional and fixed stepping, preprocess/seek/seed, gradients/curves, failure commitment, pause/hidden/world/interpolated follow, actual UV/custom data, duplication and fresh-process storage. Prepared active simulation/record/replay covers 128 iterations; idle replay covers 64. Native Engine.Run hosts exercise current Linux GPU/compatibility pixels and 64 prepared render/simulation intervals; GPU additionally consumes the actual custom channel in a shader. Native/backend allocator activity, foreign targets, broad-emitter performance and owner acceptance remain unverified.

GPUParticles/ParticleProcessMaterial and conversion from their actual settings retain exact [coverage dependencies](../coverage/classes/CPUParticles2D.md). GPU simulation, process shaders/collisions/attractors, lights and editor inspector behavior are not supplied by this CPU emitter. Reference audit and bounded corrections are documented in the component under ADRs 0004/0008/0014/0028/0051/0090.
