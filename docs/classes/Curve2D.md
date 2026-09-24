# Curve2D

Last updated: 2026-09-24

**Inherits:** [Resource](Resource.md), [ElectronObject](ElectronObject.md)

- **Declaration:** `public sealed class Curve2D : Resource`
- **Source:** [Curve2D.cs](../../src/Scene/Resources/Curve2D.cs)
- **Component:** [Curves](../components/curves.md), [Resources domain](../domains/resources.md)

## Description

A spatial cubic Bézier curve. Each ordered vertex has a local position and relative incoming/outgoing handles. Scalar [Curve](Curve.md) keeps its distinct y(x) role; neither resource derives from the other. Coordinates and distances are local, normally pixels. Empty by default, with BakeInterval 5.

Sample evaluates one segment and allows finite extrapolation parameters. Samplef combines segment index and fraction. Baked queries use a distance-indexed polyline; cubic sampling interpolates neighboring cached positions. SampleBakedWithRotation uses interpolated normalized analytic tangents, independent of the position interpolation mode. A one-point or fully degenerate curve has identity orientation. Closest queries project onto cached segments, retain degenerate point candidates and choose the earliest segment on ties.

Baking uses at most ten subdivisions per segment. BakeInterval is a density target, not a guaranteed upper bound. Angular tessellation evaluates midpoints at depths 0 through maxStages; its per-segment maximum is 2^(maxStages+1)+1 points. Length tessellation subdivides to maxStages and has at most 2^maxStages+1 points. Public subdivision depth is 0..20; length tolerances are finite positive and angular tolerance is finite nonnegative. Fewer than two points give an empty even-length tessellation; angular tessellation retains a single point. Empty positional queries throw InvalidOperationException, while empty length and array queries return zero/empty.

All input vectors are finite. Derived nonfinite control geometry, samples or lengths fail explicitly. A failed bake never publishes a partial cache; later valid edits can retry. Returned point arrays are independent copies. Warm cached queries allocate no managed storage, but baking, tessellation and array-returning APIs allocate.

Each operation serializes access to the resource state. Events run synchronously outside the state lock after commitment; a throwing observer retains completed edits and prevents later notifications in that operation. PointCount growth emits per-point changes and can therefore be interrupted or reentered. Disposal in a callback stops later growth. Multi-call edits and notifications from different threads are not one transaction; Resource copying retains its inherited caller-coordination contract.

Typed property descriptors expose cache policy and indexed points; they address the current index, so structural edits can invalidate their meaning. The first point hides its incoming handle and the last hides its outgoing handle; a single point retains its outgoing descriptor. Private packed Variant data is not public API. Custom Resource hooks copy exact stored values without setter reclamping. Both shallow and deep copies own point containers; there are no nested resource edges. Caches rebuild lazily, ResetState clears only caches, and disposal releases containers. ResourceLocalToScene participates in PackedScene duplication and root-owned cleanup.

## Example

Standalone managed resource usage with `using Electron2D;`.

```csharp
using var path = new Curve2D();
path.AddPoint(Vector2.Zero, outHandle: new(10, 0));
path.AddPoint(new(30, 0), inHandle: new(-10, 0));
Vector2 position = path.SampleBaked(15); // (15, 0)
Transform pose = path.SampleBakedWithRotation(15);
```

## Constructors

| Declaration | Contract |
| --- | --- |
| [`public Curve2D()`](#curve2d) | Creates an empty spatial curve with a bake interval of five local units. |

## Properties

| Declaration | Contract |
| --- | --- |
| [`public int PointCount { get; set; }`](#pointcount) | Gets or sets the number of vertices. |
| [`public float BakeInterval { get; set; }`](#bakeinterval) | Gets or sets the target distance between cached points. |

## Methods

| Declaration | Contract |
| --- | --- |
| [`public void AddPoint(Vector2 position, Vector2 inHandle = default, Vector2 outHandle = default, int index = -1)`](#addpoint) | Inserts a vertex with relative incoming and outgoing handles. |
| [`public Vector2 GetPointPosition(int index)`](#getpointposition) | Returns a vertex's local position. |
| [`public Vector2 GetPointIn(int index)`](#getpointin) | Returns a vertex's relative incoming handle. |
| [`public Vector2 GetPointOut(int index)`](#getpointout) | Returns a vertex's relative outgoing handle. |
| [`public void SetPointPosition(int index, Vector2 position)`](#setpointposition) | Sets a vertex position without changing its relative handles and emits Changed. |
| [`public void SetPointIn(int index, Vector2 position)`](#setpointin) | Sets a relative incoming handle and emits Changed. |
| [`public void SetPointOut(int index, Vector2 position)`](#setpointout) | Sets a relative outgoing handle and emits Changed. |
| [`public void RemovePoint(int index)`](#removepoint) | Removes an existing vertex and emits Changed then PropertyListChanged. |
| [`public void ClearPoints()`](#clearpoints) | Removes all vertices, emitting Changed then PropertyListChanged; empty curves are unchanged. |
| [`public Vector2 Sample(int index, float t)`](#sample) | Samples one cubic segment at an unbounded finite parameter. |
| [`public Vector2 Samplef(float offset)`](#samplef) | Samples by combined segment index and fractional parameter. |
| [`public float GetBakedLength()`](#getbakedlength) | Returns the cumulative chord length of the baked polyline. |
| [`public Vector2[] GetBakedPoints()`](#getbakedpoints) | Returns an independent copy of the baked positions, baking first if dirty. |
| [`public Vector2 SampleBaked(float offset = 0, bool cubic = false)`](#samplebaked) | Samples the baked polyline by distance. |
| [`public Transform SampleBakedWithRotation(float offset = 0, bool cubic = false)`](#samplebakedwithrotation) | Samples local position and an interpolated unit tangent frame by baked distance. |
| [`public Vector2 GetClosestPoint(Vector2 toPoint)`](#getclosestpoint) | Returns the closest point on the baked polyline in local coordinates. |
| [`public float GetClosestOffset(Vector2 toPoint)`](#getclosestoffset) | Returns the local distance along the baked polyline to its closest point. |
| [`public Vector2[] Tessellate(int maxStages = 5, float toleranceDegrees = 4)`](#tessellate) | Returns a curvature-controlled polyline, including both endpoints. |
| [`public Vector2[] TessellateEvenLength(int maxStages = 5, float toleranceLength = 20)`](#tessellateevenlength) | Returns a polyline subdivided by chord length, preserving nonconstant closed segments. |

## Protected hooks

| Declaration | Contract |
| --- | --- |
| [`protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors()`](#getpropertydescriptors) | Appends typed descriptors for cache policy, limits where applicable and current indexed points. Copying uses the explicit custom-state hook, not descriptor enumeration. |
| [`protected override Resource CreateDuplicateInstance()`](#createduplicateinstance) | Constructs a new exact-type resource for the existing Resource duplication session. |
| [`protected override void CopyCustomStateTo(Resource target, bool deep, DeepDuplicateMode subresourceMode, Func<Resource?, Resource?> duplicateSubresource, Func<Resource?, Resource?> forceDuplicateSubresource)`](#copycustomstateto) | Copies independent point containers and exact stored settings to the target; deep/subresource arguments do not alter scalar/vector payloads. The source and target locks are taken separately. Caches are reset. No referenced resources require either duplication delegate. |
| [`protected override void OnResetState()`](#onresetstate) | Drops cached samples, distances and tangents as applicable, preserving all stored points and settings; no change notification. |
| [`protected override void Dispose(bool disposing)`](#dispose) | When disposing is true, clears owned containers and declared event handlers, then invokes inherited cleanup. No native resources or borrowed children exist. |

## Constructors descriptions

### Curve2D

`public Curve2D()`

Creates an empty spatial curve with a bake interval of five local units.

## Properties descriptions

### PointCount

`public int PointCount { get; set; }`

Gets or sets the number of vertices.

Value: Zero initially; must be nonnegative.

Contract: Growing appends zero-position, zero-handle points and emits Changed once per point. Shrinking removes the tail and emits Changed once. Both then emit PropertyListChanged; equal assignment is silent.

`ArgumentOutOfRangeException`: The count is negative.

`ObjectDisposedException`: The resource is disposed.

### BakeInterval

`public float BakeInterval { get; set; }`

Gets or sets the target distance between cached points.

Value: Five initially; finite and positive.

Contract: Every assignment invalidates the cache and emits Changed. Baking uses at most ten subdivision stages per segment, so this is a target rather than an unconditional maximum distance.

`ArgumentOutOfRangeException`: The interval is nonpositive or nonfinite.

`ObjectDisposedException`: The resource is disposed.

## Methods descriptions

### AddPoint

`public void AddPoint(Vector2 position, Vector2 inHandle = default, Vector2 outHandle = default, int index = -1)`

Inserts a vertex with relative incoming and outgoing handles.

Parameter `position`: Finite local position.

Parameter `inHandle`: Finite relative incoming handle; zero by default.

Parameter `outHandle`: Finite relative outgoing handle; zero by default.

Parameter `index`: Insert before an existing index; any other value appends.

Contract: Emits Changed then PropertyListChanged, with state committed before callbacks.

`ArgumentException`: A vector is nonfinite.

`ObjectDisposedException`: The resource is disposed.

### GetPointPosition

`public Vector2 GetPointPosition(int index)`

Returns a vertex's local position.

Parameter `index`: An existing index.

Returns: The stored position.

`ArgumentOutOfRangeException`: The index is invalid.

`ObjectDisposedException`: The resource is disposed.

### GetPointIn

`public Vector2 GetPointIn(int index)`

Returns a vertex's relative incoming handle.

Parameter `index`: An existing index.

Returns: The stored relative offset.

`ArgumentOutOfRangeException`: The index is invalid.

`ObjectDisposedException`: The resource is disposed.

### GetPointOut

`public Vector2 GetPointOut(int index)`

Returns a vertex's relative outgoing handle.

Parameter `index`: An existing index.

Returns: The stored relative offset.

`ArgumentOutOfRangeException`: The index is invalid.

`ObjectDisposedException`: The resource is disposed.

### SetPointPosition

`public void SetPointPosition(int index, Vector2 position)`

Sets a vertex position without changing its relative handles and emits Changed.

Parameter `index`: An existing index.

Parameter `position`: Finite local position.

`ArgumentException`: The vector is nonfinite.

`ArgumentOutOfRangeException`: The index is invalid.

`ObjectDisposedException`: The resource is disposed.

### SetPointIn

`public void SetPointIn(int index, Vector2 position)`

Sets a relative incoming handle and emits Changed.

Parameter `index`: An existing index.

Parameter `position`: Finite relative offset.

`ArgumentException`: The vector is nonfinite.

`ArgumentOutOfRangeException`: The index is invalid.

`ObjectDisposedException`: The resource is disposed.

### SetPointOut

`public void SetPointOut(int index, Vector2 position)`

Sets a relative outgoing handle and emits Changed.

Parameter `index`: An existing index.

Parameter `position`: Finite relative offset.

`ArgumentException`: The vector is nonfinite.

`ArgumentOutOfRangeException`: The index is invalid.

`ObjectDisposedException`: The resource is disposed.

### RemovePoint

`public void RemovePoint(int index)`

Removes an existing vertex and emits Changed then PropertyListChanged.

Parameter `index`: An existing index.

`ArgumentOutOfRangeException`: The index is invalid.

`ObjectDisposedException`: The resource is disposed.

### ClearPoints

`public void ClearPoints()`

Removes all vertices, emitting Changed then PropertyListChanged; empty curves are unchanged.

`ObjectDisposedException`: The resource is disposed.

### Sample

`public Vector2 Sample(int index, float t)`

Samples one cubic segment at an unbounded finite parameter.

Parameter `index`: Segment index; negative selects the first vertex and index at/past the last selects the last.

Parameter `t`: Zero starts a valid segment, one ends it; other finite values extrapolate.

Returns: A local position.

`ArgumentOutOfRangeException`: t is nonfinite.

`InvalidOperationException`: The curve is empty or derived geometry is nonfinite.

`ObjectDisposedException`: The resource is disposed.

### Samplef

`public Vector2 Samplef(float offset)`

Samples by combined segment index and fractional parameter.

Parameter `offset`: Finite index plus fraction, clamped between zero and the point count.

Returns: A local position using Sample's endpoint behavior.

`ArgumentOutOfRangeException`: The offset is nonfinite.

`InvalidOperationException`: The curve is empty or derived geometry is nonfinite.

`ObjectDisposedException`: The resource is disposed.

### GetBakedLength

`public float GetBakedLength()`

Returns the cumulative chord length of the baked polyline.

Returns: Zero for fewer than two distinct baked positions.

`InvalidOperationException`: Derived geometry or cumulative length is nonfinite.

`ObjectDisposedException`: The resource is disposed.

### GetBakedPoints

`public Vector2[] GetBakedPoints()`

Returns an independent copy of the baked positions, baking first if dirty.

Returns: A new array, possibly empty.

`InvalidOperationException`: Derived geometry is nonfinite.

`ObjectDisposedException`: The resource is disposed.

### SampleBaked

`public Vector2 SampleBaked(float offset = 0, bool cubic = false)`

Samples the baked polyline by distance.

Parameter `offset`: Finite local distance, clamped to the baked length.

Parameter `cubic`: Use cubic interpolation of neighboring baked positions instead of linear interpolation.

Returns: The local sample; a one-point curve returns that point.

`ArgumentOutOfRangeException`: The distance is nonfinite.

`InvalidOperationException`: The curve is empty or derived geometry is nonfinite.

`ObjectDisposedException`: The resource is disposed.

### SampleBakedWithRotation

`public Transform SampleBakedWithRotation(float offset = 0, bool cubic = false)`

Samples local position and an interpolated unit tangent frame by baked distance.

Parameter `offset`: Finite local distance, clamped to the baked length.

Parameter `cubic`: Whether position uses cubic cached interpolation; tangent interpolation remains spherical.

Returns: A transform with tangent X, perpendicular Y and sampled Origin. A one-point or fully degenerate curve uses identity orientation.

`ArgumentOutOfRangeException`: The distance is nonfinite.

`InvalidOperationException`: The curve is empty or derived geometry is nonfinite.

`ObjectDisposedException`: The resource is disposed.

### GetClosestPoint

`public Vector2 GetClosestPoint(Vector2 toPoint)`

Returns the closest point on the baked polyline in local coordinates.

Parameter `toPoint`: Finite local query position.

Returns: The earliest segment's nearest point on ties; degenerate segments remain valid point candidates.

`ArgumentException`: The query is nonfinite.

`InvalidOperationException`: The curve is empty or derived geometry is nonfinite.

`ObjectDisposedException`: The resource is disposed.

### GetClosestOffset

`public float GetClosestOffset(Vector2 toPoint)`

Returns the local distance along the baked polyline to its closest point.

Parameter `toPoint`: Finite local query position.

Returns: The earliest closest distance on ties; zero for a one-point curve.

`ArgumentException`: The query is nonfinite.

`InvalidOperationException`: The curve is empty or derived geometry is nonfinite.

`ObjectDisposedException`: The resource is disposed.

### Tessellate

`public Vector2[] Tessellate(int maxStages = 5, float toleranceDegrees = 4)`

Returns a curvature-controlled polyline, including both endpoints.

Parameter `maxStages`: Subdivision depth, zero through twenty; default five.

Parameter `toleranceDegrees`: Finite nonnegative turning-angle threshold in degrees; default four.

Returns: An independent array; one vertex returns one point, no vertices returns empty.

Contract: Midpoints are evaluated at depths zero through maxStages, so a segment can produce up to 2^(maxStages+1)+1 points.

`ArgumentOutOfRangeException`: A parameter is outside its finite range.

`InvalidOperationException`: Derived geometry is nonfinite.

`ObjectDisposedException`: The resource is disposed.

### TessellateEvenLength

`public Vector2[] TessellateEvenLength(int maxStages = 5, float toleranceLength = 20)`

Returns a polyline subdivided by chord length, preserving nonconstant closed segments.

Parameter `maxStages`: Subdivision depth, zero through twenty; default five.

Parameter `toleranceLength`: Finite positive target chord length; default twenty local units.

Returns: An independent array; fewer than two vertices returns empty. At most 2^maxStages+1 points per segment.

`ArgumentOutOfRangeException`: A parameter is outside its finite range.

`InvalidOperationException`: Derived geometry is nonfinite.

`ObjectDisposedException`: The resource is disposed.

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

The pinned Godot 4.7.2 [implementation](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/scene/resources/curve.cpp) is audited alongside its XML. [ADR 0013](../decisions/resources.md#adr-0013) records the typed mappings and proven correctness fixes under ADR 0034: duplicate cleanup and surviving linear tangents, finite-domain cache arithmetic, nonconstant closed segments, degenerate nearest-point queries and identity orientation. The inherited Resource API is documented on its own class page. [Path/PathFollow](../components/scene-paths.md) now consume spatial curves. Curve textures, editor widgets and disk serialization remain separate unimplemented consumers with explicit [dependency triggers](../components/curves.md#dependent-slices).
