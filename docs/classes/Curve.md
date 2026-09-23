# Curve

Last updated: 2026-09-23

**Inherits:** [Resource](Resource.md), [ElectronObject](ElectronObject.md)

- **Declaration:** `public sealed class Curve : Resource`
- **Source:** [Curve.cs](../../src/Scene/Resources/Curve.cs)
- **Component:** [Curves](../components/curves.md), [Resources domain](../domains/resources.md)

## Description

A scalar cubic function y(x), with points ordered by horizontal offset, independent incoming/outgoing tangent slopes, and an evenly spaced sample cache. This is distinct from [PathCurve](PathCurve.md), which describes spatial paths. Tangents are dy/dx slopes, not angles. Defaults are no points, domain/value limits [0,1], and BakeResolution 100.

Insertion clamps both coordinates. SetPointValue allows values outside insertion limits; moving a point horizontally reinserts it and clamps its current value again. Sampling extrapolates constantly outside the endpoint offsets, uses cubic Bézier interpolation inside, and does not clamp overshoot. Near-zero spans use the next value. Linear tangents follow adjacent points; duplicate offsets may produce IEEE nonfinite slopes. Extreme slope/value arithmetic follows the existing float math contract and may overflow. Explicit input coordinates and slopes must be finite.

Value-limit setters emit only RangeChanged; domain setters emit Changed then DomainChanged. Limits include existing points and keep a finite positive span. BakeResolution invalidates without Changed; resolution one returns the last control value everywhere. Bake uses the full domain, even when control points occupy less of it. Cached sampling linearly interpolates the cached samples.

Each operation serializes access to the resource state. Events run synchronously outside the state lock after commitment; a throwing observer retains completed edits and prevents later notifications in that operation. PointCount growth emits per-point changes and can therefore be interrupted or reentered. Disposal in a callback stops later growth. Multi-call edits and notifications from different threads are not one transaction; Resource copying retains its inherited caller-coordination contract.

Typed property descriptors expose cache policy and indexed points; they address the current index, so structural edits can invalidate their meaning. The first point hides its incoming handle and the last hides its outgoing handle; a single point retains its outgoing descriptor. Private packed Variant data is not public API. Custom Resource hooks copy exact stored values without setter reclamping. Both shallow and deep copies own point containers; there are no nested resource edges. Caches rebuild lazily, ResetState clears only caches, and disposal releases containers. ResourceLocalToScene participates in PackedScene duplication and root-owned cleanup.

## Example

Standalone managed resource usage with `using Electron2D;`.

```csharp
using var curve = new Curve();
curve.AddPoint(new(0, 0), rightMode: Curve.TangentMode.Linear);
curve.AddPoint(new(1, 1), leftMode: Curve.TangentMode.Linear);
float value = curve.Sample(.25f); // 0.25
```

## Enumerations

[TangentMode](Curve.TangentMode.md): Free = 0, Linear = 1, Count = 2 (invalid as a point mode).

## Constructors

| Declaration | Contract |
| --- | --- |
| [`public Curve()`](#curve) | Creates an empty curve with unit domain/value limits and bake resolution 100. |

## Properties

| Declaration | Contract |
| --- | --- |
| [`public int PointCount { get; set; }`](#pointcount) | Gets or sets the number of control points. |
| [`public int BakeResolution { get; set; }`](#bakeresolution) | Gets or sets the number of evenly spaced cached samples. |
| [`public float MinDomain { get; set; }`](#mindomain) | Gets or sets the lower horizontal bound. |
| [`public float MaxDomain { get; set; }`](#maxdomain) | Gets or sets the upper horizontal bound. |
| [`public float MinValue { get; set; }`](#minvalue) | Gets or sets the lower insertion-value bound. |
| [`public float MaxValue { get; set; }`](#maxvalue) | Gets or sets the upper insertion-value bound. |

## Methods

| Declaration | Contract |
| --- | --- |
| [`public float GetDomainRange()`](#getdomainrange) | Returns MaxDomain minus MinDomain. |
| [`public float GetValueRange()`](#getvaluerange) | Returns MaxValue minus MinValue. |
| [`public int AddPoint(Vector2 position, float leftTangent = 0, float rightTangent = 0, TangentMode leftMode = TangentMode.Free, TangentMode rightMode = TangentMode.Free)`](#addpoint) | Inserts a point in horizontal order and updates neighboring automatic tangents. |
| [`public Vector2 GetPointPosition(int index)`](#getpointposition) | Returns a point's stored coordinates. |
| [`public void SetPointValue(int index, float y)`](#setpointvalue) | Sets a point's value without clamping to value limits, updates automatic tangents and emits Changed. |
| [`public int SetPointOffset(int index, float offset)`](#setpointoffset) | Moves a point horizontally, reinserts it in order, recalculates tangents and emits Changed. |
| [`public float GetPointLeftTangent(int index)`](#getpointlefttangent) | Returns a point's left tangent slope. |
| [`public float GetPointRightTangent(int index)`](#getpointrighttangent) | Returns a point's right tangent slope. |
| [`public TangentMode GetPointLeftMode(int index)`](#getpointleftmode) | Returns a point's left tangent mode. |
| [`public TangentMode GetPointRightMode(int index)`](#getpointrightmode) | Returns a point's right tangent mode. |
| [`public void SetPointLeftTangent(int index, float tangent)`](#setpointlefttangent) | Sets a finite left slope, switches that side to Free, and emits Changed. |
| [`public void SetPointRightTangent(int index, float tangent)`](#setpointrighttangent) | Sets a finite right slope, switches that side to Free, and emits Changed. |
| [`public void SetPointLeftMode(int index, TangentMode mode)`](#setpointleftmode) | Sets the left mode, recalculates its slope when Linear has a neighbor, and emits Changed. |
| [`public void SetPointRightMode(int index, TangentMode mode)`](#setpointrightmode) | Sets the right mode, recalculates its slope when Linear has a neighbor, and emits Changed. |
| [`public void RemovePoint(int index)`](#removepoint) | Removes a point, refreshes surviving automatic tangents and emits Changed then PropertyListChanged. |
| [`public void ClearPoints()`](#clearpoints) | Removes all points and emits Changed then PropertyListChanged; an empty curve is a no-op. |
| [`public void CleanDupes()`](#cleandupes) | Removes later neighbors within Mathf.Epsilon in horizontal offset and refreshes automatic tangents. |
| [`public float Sample(float offset)`](#sample) | Samples the cubic curve at a horizontal coordinate. |
| [`public void Bake()`](#bake) | Recomputes the evenly spaced sample cache without emitting an event. |
| [`public float SampleBaked(float offset)`](#samplebaked) | Samples the cached curve, baking lazily when dirty. |

## Events

| Declaration | Contract |
| --- | --- |
| [`public event Action? DomainChanged`](#domainchanged) | Occurs after a domain setter's Changed event, including equal assignments. |
| [`public event Action? RangeChanged`](#rangechanged) | Occurs after a value-limit setter; that setter does not emit Changed. |

## Protected hooks

| Declaration | Contract |
| --- | --- |
| [`protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors()`](#getpropertydescriptors) | Appends typed descriptors for cache policy, limits where applicable and current indexed points. Copying uses the explicit custom-state hook, not descriptor enumeration. |
| [`protected override Resource CreateDuplicateInstance()`](#createduplicateinstance) | Constructs a new exact-type resource for the existing Resource duplication session. |
| [`protected override void CopyCustomStateTo(Resource target, bool deep, DeepDuplicateMode subresourceMode, Func<Resource?, Resource?> duplicateSubresource, Func<Resource?, Resource?> forceDuplicateSubresource)`](#copycustomstateto) | Copies independent point containers and exact stored settings to the target; deep/subresource arguments do not alter scalar/vector payloads. The source and target locks are taken separately. Caches are reset. No referenced resources require either duplication delegate. |
| [`protected override void OnResetState()`](#onresetstate) | Drops cached samples, distances and tangents as applicable, preserving all stored points and settings; no change notification. |
| [`protected override void Dispose(bool disposing)`](#dispose) | When disposing is true, clears owned containers and declared event handlers, then invokes inherited cleanup. No native resources or borrowed children exist. |

## Constructors descriptions

### Curve

`public Curve()`

Creates an empty curve with unit domain/value limits and bake resolution 100.

## Properties descriptions

### PointCount

`public int PointCount { get; set; }`

Gets or sets the number of control points.

Value: Zero initially; must be nonnegative.

Contract: Shrinking removes the tail. Growing inserts default points at clamped zero when previously empty, otherwise at MaxDomain; values use clamped zero. Growth emits Changed for each added point, shrinking once, followed by PropertyListChanged. Equal assignment is silent. Callback failure retains committed edits.

`ArgumentOutOfRangeException`: The count is negative.

`ObjectDisposedException`: The resource is disposed.

### BakeResolution

`public int BakeResolution { get; set; }`

Gets or sets the number of evenly spaced cached samples.

Value: 100 initially; allowed range 1 through 1000.

Contract: Invalidates the cache without emitting Changed. Resolution one samples the last control value.

`ArgumentOutOfRangeException`: The resolution is outside the allowed range.

`ObjectDisposedException`: The resource is disposed.

### MinDomain

`public float MinDomain { get; set; }`

Gets or sets the lower horizontal bound.

Value: Zero initially.

Contract: Limited to at most MaxDomain minus 0.01 and the first point's offset. Every assignment dirties the cache and emits Changed followed by DomainChanged, including equal values. Does not move points.

`ArgumentOutOfRangeException`: The value or resulting range is not finite and positive.

`ObjectDisposedException`: The resource is disposed.

### MaxDomain

`public float MaxDomain { get; set; }`

Gets or sets the upper horizontal bound.

Value: One initially.

Contract: Limited to at least MinDomain plus 0.01 and the last point's offset. Emits Changed then DomainChanged.

`ArgumentOutOfRangeException`: The value or resulting range is not finite and positive.

`ObjectDisposedException`: The resource is disposed.

### MinValue

`public float MinValue { get; set; }`

Gets or sets the lower insertion-value bound.

Value: Zero initially.

Contract: Limited to at most MaxValue minus 0.01 and every existing point's value. Emits RangeChanged even on equal assignment, without Changed or cache invalidation. Does not clamp sampled values or existing points.

`ArgumentOutOfRangeException`: The value or resulting range is not finite and positive.

`ObjectDisposedException`: The resource is disposed.

### MaxValue

`public float MaxValue { get; set; }`

Gets or sets the upper insertion-value bound.

Value: One initially.

Contract: Limited to at least MinValue plus 0.01 and every existing point's value. Emits only RangeChanged.

`ArgumentOutOfRangeException`: The value or resulting range is not finite and positive.

`ObjectDisposedException`: The resource is disposed.

## Methods descriptions

### GetDomainRange

`public float GetDomainRange()`

Returns MaxDomain minus MinDomain.

Returns: The positive finite horizontal range.

`ObjectDisposedException`: The resource is disposed.

### GetValueRange

`public float GetValueRange()`

Returns MaxValue minus MinValue.

Returns: The positive finite insertion-value range.

`ObjectDisposedException`: The resource is disposed.

### AddPoint

`public int AddPoint(Vector2 position, float leftTangent = 0, float rightTangent = 0, TangentMode leftMode = TangentMode.Free, TangentMode rightMode = TangentMode.Free)`

Inserts a point in horizontal order and updates neighboring automatic tangents.

Parameter `position`: Finite coordinates, clamped to the domain/value limits.

Parameter `leftTangent`: Finite left slope, used in Free mode or when no left neighbor exists.

Parameter `rightTangent`: Finite right slope, used in Free mode or when no right neighbor exists.

Parameter `leftMode`: Free or Linear.

Parameter `rightMode`: Free or Linear.

Returns: The inserted index; equal offsets are retained.

Contract: Emits Changed then PropertyListChanged. Equal-offset insertion uses the interval search's tie order.

`ArgumentOutOfRangeException`: A coordinate/tangent is nonfinite or a mode is invalid.

`ObjectDisposedException`: The resource is disposed.

### GetPointPosition

`public Vector2 GetPointPosition(int index)`

Returns a point's stored coordinates.

Parameter `index`: An existing index.

Returns: The ordered offset and value.

`ArgumentOutOfRangeException`: The index is invalid.

`ObjectDisposedException`: The resource is disposed.

### SetPointValue

`public void SetPointValue(int index, float y)`

Sets a point's value without clamping to value limits, updates automatic tangents and emits Changed.

Parameter `index`: An existing index.

Parameter `y`: A finite vertical value.

`ArgumentOutOfRangeException`: The index is invalid or y is nonfinite.

`ObjectDisposedException`: The resource is disposed.

### SetPointOffset

`public int SetPointOffset(int index, float offset)`

Moves a point horizontally, reinserts it in order, recalculates tangents and emits Changed.

Parameter `index`: An existing index.

Parameter `offset`: Finite horizontal position, clamped to domain limits.

Returns: The new index. Reinsertion also clamps the point's value to current value limits.

Contract: Does not emit PropertyListChanged, even when the index changes.

`ArgumentOutOfRangeException`: The index is invalid or offset is nonfinite.

`ObjectDisposedException`: The resource is disposed.

### GetPointLeftTangent

`public float GetPointLeftTangent(int index)`

Returns a point's left tangent slope.

Parameter `index`: An existing index.

Returns: The stored slope; automatic duplicate-offset slopes may be nonfinite.

`ArgumentOutOfRangeException`: The index is invalid.

`ObjectDisposedException`: The resource is disposed.

### GetPointRightTangent

`public float GetPointRightTangent(int index)`

Returns a point's right tangent slope.

Parameter `index`: An existing index.

Returns: The stored slope; automatic duplicate-offset slopes may be nonfinite.

`ArgumentOutOfRangeException`: The index is invalid.

`ObjectDisposedException`: The resource is disposed.

### GetPointLeftMode

`public TangentMode GetPointLeftMode(int index)`

Returns a point's left tangent mode.

Parameter `index`: An existing index.

Returns: Free or Linear.

`ArgumentOutOfRangeException`: The index is invalid.

`ObjectDisposedException`: The resource is disposed.

### GetPointRightMode

`public TangentMode GetPointRightMode(int index)`

Returns a point's right tangent mode.

Parameter `index`: An existing index.

Returns: Free or Linear.

`ArgumentOutOfRangeException`: The index is invalid.

`ObjectDisposedException`: The resource is disposed.

### SetPointLeftTangent

`public void SetPointLeftTangent(int index, float tangent)`

Sets a finite left slope, switches that side to Free, and emits Changed.

Parameter `index`: An existing index.

Parameter `tangent`: Finite slope, not an angle.

`ArgumentOutOfRangeException`: The index is invalid or tangent is nonfinite.

`ObjectDisposedException`: The resource is disposed.

### SetPointRightTangent

`public void SetPointRightTangent(int index, float tangent)`

Sets a finite right slope, switches that side to Free, and emits Changed.

Parameter `index`: An existing index.

Parameter `tangent`: Finite slope, not an angle.

`ArgumentOutOfRangeException`: The index is invalid or tangent is nonfinite.

`ObjectDisposedException`: The resource is disposed.

### SetPointLeftMode

`public void SetPointLeftMode(int index, TangentMode mode)`

Sets the left mode, recalculates its slope when Linear has a neighbor, and emits Changed.

Parameter `index`: An existing index.

Parameter `mode`: Free or Linear.

`ArgumentOutOfRangeException`: The index or mode is invalid.

`ObjectDisposedException`: The resource is disposed.

### SetPointRightMode

`public void SetPointRightMode(int index, TangentMode mode)`

Sets the right mode, recalculates its slope when Linear has a neighbor, and emits Changed.

Parameter `index`: An existing index.

Parameter `mode`: Free or Linear.

`ArgumentOutOfRangeException`: The index or mode is invalid.

`ObjectDisposedException`: The resource is disposed.

### RemovePoint

`public void RemovePoint(int index)`

Removes a point, refreshes surviving automatic tangents and emits Changed then PropertyListChanged.

Parameter `index`: An existing index.

`ArgumentOutOfRangeException`: The index is invalid.

`ObjectDisposedException`: The resource is disposed.

### ClearPoints

`public void ClearPoints()`

Removes all points and emits Changed then PropertyListChanged; an empty curve is a no-op.

`ObjectDisposedException`: The resource is disposed.

### CleanDupes

`public void CleanDupes()`

Removes later neighbors within Mathf.Epsilon in horizontal offset and refreshes automatic tangents.

Contract: Preserves the first of each near-duplicate run. Emits Changed only when points were removed; does not emit PropertyListChanged. Distinct ordered points are never removed just because their difference is signed.

`ObjectDisposedException`: The resource is disposed.

### Sample

`public float Sample(float offset)`

Samples the cubic curve at a horizontal coordinate.

Parameter `offset`: Finite horizontal position.

Returns: Zero for no points; otherwise a cubic sample with constant extrapolation past endpoints.

Contract: Near-zero horizontal spans use the next point's value. Tangent overshoot is not clamped.

`ArgumentOutOfRangeException`: The offset is nonfinite.

`ObjectDisposedException`: The resource is disposed.

### Bake

`public void Bake()`

Recomputes the evenly spaced sample cache without emitting an event.

`ObjectDisposedException`: The resource is disposed.

### SampleBaked

`public float SampleBaked(float offset)`

Samples the cached curve, baking lazily when dirty.

Parameter `offset`: Finite horizontal position, clamped to the cache endpoints.

Returns: Linearly interpolated cached values, or zero for an empty curve.

`ArgumentOutOfRangeException`: The offset is nonfinite.

`ObjectDisposedException`: The resource is disposed.

## Events descriptions

### DomainChanged

`public event Action? DomainChanged`

Occurs after a domain setter's Changed event, including equal assignments.

### RangeChanged

`public event Action? RangeChanged`

Occurs after a value-limit setter; that setter does not emit Changed.

## Protected hooks descriptions

### GetPropertyDescriptors

`protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors()`

Appends typed descriptors for cache policy, limits where applicable and current indexed points. Copying uses the explicit custom-state hook, not descriptor enumeration.

Contract: Tooling receives typed descriptors for limits/cache policy and indexed points. Indexed descriptors address the current index and can become invalid after structural edits; copying uses the custom resource hook.

### CreateDuplicateInstance

`protected override Resource CreateDuplicateInstance()`

Constructs a new exact-type resource for the existing Resource duplication session.

### CopyCustomStateTo

`protected override void CopyCustomStateTo(Resource target, bool deep, DeepDuplicateMode subresourceMode, Func<Resource?, Resource?> duplicateSubresource, Func<Resource?, Resource?> forceDuplicateSubresource)`

Copies independent point containers and exact stored settings to the target; deep/subresource arguments do not alter scalar/vector payloads. The source and target locks are taken separately. Caches are reset. No referenced resources require either duplication delegate.

### OnResetState

`protected override void OnResetState()`

Drops cached samples, distances and tangents as applicable, preserving all stored points and settings; no change notification.

### Dispose

`protected override void Dispose(bool disposing)`

When disposing is true, clears owned containers and declared event handlers, then invokes inherited cleanup. No native resources or borrowed children exist.

## Audit, dependencies and verification

Uses the existing Resource, Vector2, Mathf and Transform contracts and standard managed collections; there is no renderer, native library, importer or editor dependency. [CurveTests](../../tests/Electron2D.Tests/CurveTests.cs) verifies analytic samples, defaults, event timing, clamping, ordering, automatic tangents, degenerate curves, cache invalidation, array/copy isolation, typed descriptors, PackedScene ownership and an executing Entity consumer, callback failures/disposal, concurrent edits and reads, and allocation-free warm scalar/spatial queries. These are Linux managed checks; no native, owner visual or cross-platform acceptance is claimed for this slice.

The pinned Godot 4.7.2 [implementation](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/scene/resources/curve.cpp) is audited alongside its XML. [ADR 0013](../decisions/resources.md#adr-0013) records the typed mappings and proven correctness fixes under ADR 0034: duplicate cleanup and surviving linear tangents, finite-domain cache arithmetic, nonconstant closed segments, degenerate nearest-point queries and identity orientation. The inherited Resource API is documented on its own class page. Paths/followers, curve textures, editor widgets and disk serialization remain separate unimplemented consumers with explicit [dependency triggers](../components/curves.md#dependent-slices).
