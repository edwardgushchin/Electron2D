# AnimationNodeBlendSpace1D

Last updated: 2026-10-04

**Namespace:** `Electron2D` · **Declaration:** `public sealed class Electron2D.AnimationNodeBlendSpace1D` · **Source:** [AnimationNodeBlendSpace1D.cs](../../src/Scene/Animation/AnimationNodeBlendSpace1D.cs).

**Inherits:** [AnimationRootNode](AnimationRootNode.md).

## Description

A reusable 1D animation blend space with typed per-tree position and clock state.

Interpolated evaluation finds the greatest point coordinate at/below the blend position and the smallest coordinate above it. It mixes the two linearly, or holds the closest endpoint outside the axis. Equal coordinate ties keep the first matching point. One-point spaces pass through that timeline at ordinary rate (including cyclic settings after leaf validation), following the reference one-point path. The general carry selection begins once multiple points have been evaluated.

Points borrow AnimationRootNode resources. There are at most 64 points; insertion uses an index or -1 to append. Empty names use the first unused numeric name at/after the insertion index; slash/dot names reject, and conflicts gain an ordinal numeric suffix. FindBlendPointByName returns -1 when absent. Coordinates remain unclamped by MinSpace/MaxSpace/Snap, which are finite authoring metadata; crossing a bound moves the changed component one unit beyond the other bound. Snap and labels do not alter runtime blending. Reorder swaps whole entries, preserving names and child clocks. SetBlendPointNode preserves name/position while replacing the borrowed definition. Remove and disposal detach observer links without disposing points.

BlendPosition is an immutable typed key, accessed on AnimationTree by a graph path (empty for root). Every tree/path owns its selected-point and timeline cells. Selection follows point-entry identity across rename/reorder, and removed points cannot be carried. Shallow copies own independent point containers; deep copies preserve child-resource aliases through Resource duplication. Definitions may be shared sequentially across trees with different clip libraries. Owner-thread scene/parameter writes follow Node, resource authoring follows Resource; concurrent evaluation of a shared definition is unsupported.

BlendMode uses the shared AnimationBlendMode domain. Interpolated mode selects the largest contribution's timeline, resolving equal weights in later point/triangle order. Discrete mode chooses the first nearest point on ties and retains each point's independent clock. DiscreteCarry additionally tests the outgoing point after the incoming delta, seeks the newly selected point to that resulting timeline position and transfers PingPong direction for clip-leaf pairs. Carry is used only below cyclic sync modes; it does not fire the outgoing point's signals or commit its test-only clock changes. Names, coordinates and settings mutate before synchronous observer notification; failures propagate and can leave the committed edit for the caller to inspect.

SyncMode None freezes inactive points (contributions below 1e-5 are skipped), Independent advances every point, and cyclic modes require every point to be an AnimationNodeAnimation. CyclicMutable computes a weighted active node-timeline length; CyclicConstant uses CyclicLength. Each point receives delta * pointLength / targetLength; nonpositive/small target length freezes all deltas. Lengths are read from the current tree library/custom timeline on each evaluation, so shared resources and live clip edits do not reuse another tree's lengths. Sync is the inherited legacy projection: setting true selects Independent, false None, and reading returns true for any non-None mode. Absolute seeks remain unscaled positions, while the supplied delta is synchronized; seeks across unequal lengths can break phase alignment. Custom aligned timelines resolve that case.

Missing/disposed points, nonfinite inputs, malformed names, invalid indices, cycles and excess capacity reject with exceptions. A nonfinite Vector2 BlendPosition is rejected on evaluation; scalar position writes are validated by the tree. Cyclic use of a non-leaf point throws InvalidOperationException. Empty spaces fail explicitly. Warmed evaluation uses bounded stack weights/deltas and the existing prepared typed mixer. Structural edits, automatic triangulation, graph preparation and result arrays are cold allocation boundaries. No persistence factory or editor UI is implemented.

## Example

This snippet requires live AnimationNodeAnimation leaves and an AnimationTree with libraries/targets; the public consumer is exercised by [AnimationBlendSpaceTests](../../tests/Electron2D.Tests/AnimationBlendSpaceTests.cs).

```csharp
using var space = new AnimationNodeBlendSpace1D();
space.AddBlendPoint(idle, 0, name: "idle");
space.AddBlendPoint(run, 1, name: "run");
controller.TreeRoot = space; // Existing AnimationTree with named clip libraries.
controller.SetParameter("", AnimationNodeBlendSpace1D.BlendPosition, 0.4f);
controller.Advance(0);
```

## Enumeration domains

[AnimationBlendMode](AnimationBlendMode.md) and [AnimationSyncMode](AnimationSyncMode.md) share one semantic identity across both spaces under ADR 0051.

## Constructor summary

| Complete C# signature | Contract |
| --- | --- |
| `public AnimationNodeBlendSpace1D()` | Creates an empty blend space with independent point storage. |

## Constructor Descriptions

<a id="member-8047b4f200d7"></a>
### .ctor

`public AnimationNodeBlendSpace1D()`

Creates an empty blend space with independent point storage.

## Property summary

| Complete C# signature | Contract |
| --- | --- |
| `public Electron2D.AnimationBlendMode BlendMode { get; set; }` | Selects interpolated, discrete or carry playback. |
| `public System.Double CyclicLength { get; set; }` | Sets the finite constant cycle length in seconds; nonpositive values freeze cyclic clocks. |
| `public System.Single MaxSpace { get; set; }` | Sets finite authoring maximum; crossing the minimum adjusts each affected component to min plus one. |
| `public System.Single MinSpace { get; set; }` | Sets finite authoring minimum; crossing the maximum adjusts each affected component to max minus one. |
| `public System.Single Snap { get; set; }` | Sets the finite authoring snap increment; it does not quantize runtime blending. |
| `public System.Boolean Sync { get; set; }` | Projects SyncMode: true sets Independent, false sets None; reads true for every non-None mode. |
| `public Electron2D.AnimationSyncMode SyncMode { get; set; }` | Selects inactive/cyclic clock synchronization; cyclic modes require clip leaves. |
| `public System.String ValueLabel { get; set; }` | Sets the authoring axis label. |

## Property Descriptions

<a id="member-017e40d49551"></a>
### BlendMode

`public Electron2D.AnimationBlendMode BlendMode { get; set; }`

Selects interpolated, discrete or carry playback.

Value: The configured BlendMode value; initial value is AnimationBlendMode.Interpolated.

System.ObjectDisposedException: The resource has been disposed.

System.ArgumentOutOfRangeException: The enum/numeric value is invalid or nonfinite.

<a id="member-6931e6bfdb32"></a>
### CyclicLength

`public System.Double CyclicLength { get; set; }`

Sets the finite constant cycle length in seconds; nonpositive values freeze cyclic clocks.

Value: The configured CyclicLength value; initial value is 0.

System.ObjectDisposedException: The resource has been disposed.

System.ArgumentOutOfRangeException: The enum/numeric value is invalid or nonfinite.

<a id="member-128506e9ebf2"></a>
### MaxSpace

`public System.Single MaxSpace { get; set; }`

Sets finite authoring maximum; crossing the minimum adjusts each affected component to min plus one.

Value: The configured MaxSpace value; initial value is 1.

System.ObjectDisposedException: The resource has been disposed.

System.ArgumentOutOfRangeException: The enum/numeric value is invalid or nonfinite.

<a id="member-baf872ca99ed"></a>
### MinSpace

`public System.Single MinSpace { get; set; }`

Sets finite authoring minimum; crossing the maximum adjusts each affected component to max minus one.

Value: The configured MinSpace value; initial value is -1.

System.ObjectDisposedException: The resource has been disposed.

System.ArgumentOutOfRangeException: The enum/numeric value is invalid or nonfinite.

<a id="member-6370fd2407c8"></a>
### Snap

`public System.Single Snap { get; set; }`

Sets the finite authoring snap increment; it does not quantize runtime blending.

Value: The configured Snap value; initial value is .1f.

System.ObjectDisposedException: The resource has been disposed.

System.ArgumentOutOfRangeException: The enum/numeric value is invalid or nonfinite.

<a id="member-186cf70c3a4c"></a>
### Sync

`public System.Boolean Sync { get; set; }`

Projects SyncMode: true sets Independent, false sets None; reads true for every non-None mode.

Value: The configured Sync value; initial value is false.

System.ObjectDisposedException: The resource has been disposed.

<a id="member-798d6d60ef72"></a>
### SyncMode

`public Electron2D.AnimationSyncMode SyncMode { get; set; }`

Selects inactive/cyclic clock synchronization; cyclic modes require clip leaves.

Value: The configured SyncMode value; initial value is AnimationSyncMode.None.

System.ObjectDisposedException: The resource has been disposed.

System.ArgumentOutOfRangeException: The enum/numeric value is invalid or nonfinite.

<a id="member-3b3b63138a60"></a>
### ValueLabel

`public System.String ValueLabel { get; set; }`

Sets the authoring axis label.

Value: The configured ValueLabel value; initial value is value.

System.ObjectDisposedException: The resource has been disposed.

## Method summary

| Complete C# signature | Contract |
| --- | --- |
| `public System.Void AddBlendPoint(Electron2D.AnimationRootNode node, System.Single position, System.Int32 atIndex = -1, System.String name = "")` | Adds a borrowed point; empty names use the next unused safe numeric name, conflicts gain a numeric suffix. |
| `protected override System.Void CopyCustomStateTo(Electron2D.Resource target, System.Boolean deep, Electron2D.DeepDuplicateMode mode, System.Func<Electron2D.Resource, Electron2D.Resource> duplicate, System.Func<Electron2D.Resource, Electron2D.Resource> force)` | Copies derived stored state into a duplicate or copy target. |
| `protected override Electron2D.Resource CreateDuplicateInstance()` | Creates a fresh default instance used as the target of duplication. |
| `protected override System.Void Dispose(System.Boolean disposing)` | Implements the inherited resource lifecycle for this concrete type. |
| `public System.Int32 FindBlendPointByName(System.String name)` | Returns the index for an ordinal name, or minus one. |
| `public System.Int32 GetBlendPointCount()` | Returns the current point count. |
| `public System.String GetBlendPointName(System.Int32 point)` | Returns an exact point name. |
| `public Electron2D.AnimationRootNode GetBlendPointNode(System.Int32 point)` | Returns a borrowed point definition. |
| `public System.Single GetBlendPointPosition(System.Int32 point)` | Returns the point coordinates. |
| `protected override System.Collections.Generic.IEnumerable<Electron2D.PropertyDescriptor> GetPropertyDescriptors()` | Implements the inherited resource lifecycle for this concrete type. |
| `protected override System.String OnGetCaption()` | Supplies the graph resource's caption. |
| `protected override Electron2D.AnimationNode OnGetChildByName(System.String name)` | Finds a named child definition. |
| `protected override System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<System.String, Electron2D.AnimationNode>> OnGetChildNodes()` | Returns ordered named child definitions; no scene nodes are created. |
| `protected override System.Collections.Generic.IEnumerable<Electron2D.AnimationParameter> OnGetParameterList()` | Supplies immutable typed parameter definitions. |
| `protected override System.Double OnProcess(System.Double time, System.Boolean seek, System.Boolean isExternalSeeking, System.Boolean testOnly)` | Evaluates custom graph behavior; helpers can blend inputs, children and clips. |
| `public System.Void RemoveBlendPoint(System.Int32 point)` | Removes a point without disposing its resource; referring triangles are removed and later indices shift. |
| `public System.Void ReorderBlendPoint(System.Int32 fromIndex, System.Int32 toIndex)` | Swaps two point entries, preserving their names/clocks and remapping triangle indices. |
| `public System.Void SetBlendPointName(System.Int32 point, System.String name)` | Renames a point, generating a unique suffix for conflicts and preserving per-tree child state. |
| `public System.Void SetBlendPointNode(System.Int32 point, Electron2D.AnimationRootNode node)` | Replaces a borrowed point definition, preserving its name and coordinates. |
| `public System.Void SetBlendPointPosition(System.Int32 point, System.Single position)` | Changes finite point coordinates without clamping them to authoring bounds. |

## Method Descriptions

<a id="member-13610626642d"></a>
### AddBlendPoint

`public System.Void AddBlendPoint(Electron2D.AnimationRootNode node, System.Single position, System.Int32 atIndex = -1, System.String name = "")`

Adds a borrowed point; empty names use the next unused safe numeric name, conflicts gain a numeric suffix.

node: A live root resource without a containment cycle.

position: Finite point coordinates.

atIndex: Insertion index, or minus one to append.

name: Local name without slash/dot, or empty for a safe index.

System.ArgumentException: The name/node/position is invalid.

System.InvalidOperationException: Capacity or resource-cycle validation fails.

System.ObjectDisposedException: This resource has been disposed.

<a id="member-5517cc292f1d"></a>
### CopyCustomStateTo

`protected override System.Void CopyCustomStateTo(Electron2D.Resource target, System.Boolean deep, Electron2D.DeepDuplicateMode mode, System.Func<Electron2D.Resource, Electron2D.Resource> duplicate, System.Func<Electron2D.Resource, Electron2D.Resource> force)`

Copies derived stored state into a duplicate or copy target.

target: A live resource with the exact same runtime type.

deep: Whether typed collection containers should be cloned recursively.

subresourceMode: The nested-resource policy for this copy.

duplicateSubresource: A graph-preserving function that returns the correct shared or duplicated instance for a nested resource. Pass every nested resource through this function when deep is true.

forceDuplicateSubresource: A graph-preserving function that duplicates a nested resource even when the current policy would share it. Use it for typed properties whose contract requires duplication; assign the original reference directly for properties whose contract forbids duplication.

Remarks: The base implementation supports only an exact Electron2D.Resource instance. Derived implementations must copy all stored custom state and call the base implementation only when they intentionally want its validation. Assigning the original nested-resource reference directly expresses a never-duplicate property.

System.NotSupportedException: A derived resource has not explicitly implemented custom-state copying.

<a id="member-db01e9e08f81"></a>
### CreateDuplicateInstance

`protected override Electron2D.Resource CreateDuplicateInstance()`

Creates a fresh default instance used as the target of duplication.

Returns: A live resource of the exact same runtime type with empty path and scene ID.

Remarks: The base implementation supports only an exact Electron2D.Resource instance. Every derived class must override this method, even when it adds no state, so duplication support is explicit.

System.NotSupportedException: The runtime type derives from Electron2D.Resource and has not overridden this method.

<a id="member-fc2bef49ae5e"></a>
### Dispose

`protected override System.Void Dispose(System.Boolean disposing)`

Implements the inherited resource lifecycle for this concrete type.

Remarks: Unregisters the cache path and clears resource event subscribers before base cleanup.

<a id="member-ae3bad4ea342"></a>
### FindBlendPointByName

`public System.Int32 FindBlendPointByName(System.String name)`

Returns the index for an ordinal name, or minus one.

name: The non-null local name.

Returns: The index or minus one.

System.ObjectDisposedException: This resource has been disposed.

<a id="member-a8972c3d17ff"></a>
### GetBlendPointCount

`public System.Int32 GetBlendPointCount()`

Returns the current point count.

Returns: The count, from zero through 64.

System.ObjectDisposedException: This resource has been disposed.

<a id="member-8d04fe27d3d0"></a>
### GetBlendPointName

`public System.String GetBlendPointName(System.Int32 point)`

Returns an exact point name.

point: An existing index.

Returns: The nonempty local name.

System.ObjectDisposedException: This resource has been disposed.

System.ArgumentOutOfRangeException: An index or numeric value is outside its documented domain.

<a id="member-87e275b6989b"></a>
### GetBlendPointNode

`public Electron2D.AnimationRootNode GetBlendPointNode(System.Int32 point)`

Returns a borrowed point definition.

point: An existing zero-based index.

Returns: The borrowed root resource.

System.ObjectDisposedException: This resource has been disposed.

System.ArgumentOutOfRangeException: An index or numeric value is outside its documented domain.

<a id="member-27570953d876"></a>
### GetBlendPointPosition

`public System.Single GetBlendPointPosition(System.Int32 point)`

Returns the point coordinates.

point: An existing index.

Returns: The finite stored coordinates.

System.ObjectDisposedException: This resource has been disposed.

System.ArgumentOutOfRangeException: An index or numeric value is outside its documented domain.

<a id="member-d721a784e616"></a>
### GetPropertyDescriptors

`protected override System.Collections.Generic.IEnumerable<Electron2D.PropertyDescriptor> GetPropertyDescriptors()`

Implements the inherited resource lifecycle for this concrete type.

Remarks: Appends resource identity and scene-instancing configuration descriptors.

<a id="member-e00e490c1efe"></a>
### OnGetCaption

`protected override System.String OnGetCaption()`

Supplies the graph resource's caption.

Returns: The caption; defaults to Node.

System.ObjectDisposedException: This resource/controller or a required borrowed resource has been disposed.

<a id="member-0c00ab51645a"></a>
### OnGetChildByName

`protected override Electron2D.AnimationNode OnGetChildByName(System.String name)`

Finds a named child definition.

name: The exact local name.

Returns: The borrowed child or null.

System.ObjectDisposedException: This resource/controller or a required borrowed resource has been disposed.

<a id="member-2146f5437863"></a>
### OnGetChildNodes

`protected override System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<System.String, Electron2D.AnimationNode>> OnGetChildNodes()`

Returns ordered named child definitions; no scene nodes are created.

Returns: The graph's borrowed children.

System.ObjectDisposedException: This resource/controller or a required borrowed resource has been disposed.

<a id="member-9d1850b4ca44"></a>
### OnGetParameterList

`protected override System.Collections.Generic.IEnumerable<Electron2D.AnimationParameter> OnGetParameterList()`

Supplies immutable typed parameter definitions.

Returns: The node's parameter schema, including inherited timing slots.

System.ObjectDisposedException: This resource/controller or a required borrowed resource has been disposed.

<a id="member-b2797f397e67"></a>
### OnProcess

`protected override System.Double OnProcess(System.Double time, System.Boolean seek, System.Boolean isExternalSeeking, System.Boolean testOnly)`

Evaluates custom graph behavior; helpers can blend inputs, children and clips.

time: Relative delta seconds, or absolute time when seeking.

seek: Whether time is absolute.

isExternalSeeking: Whether the seek originated outside startup.

testOnly: Whether persistent changes and mixer writes are suppressed.

Returns: The selected remaining duration, or zero when no input is evaluated.

System.ObjectDisposedException: This resource/controller or a required borrowed resource has been disposed.

<a id="member-027a87cf0e81"></a>
### RemoveBlendPoint

`public System.Void RemoveBlendPoint(System.Int32 point)`

Removes a point without disposing its resource; referring triangles are removed and later indices shift.

point: An existing index.

System.ObjectDisposedException: This resource has been disposed.

System.ArgumentOutOfRangeException: An index or numeric value is outside its documented domain.

<a id="member-937e148ad375"></a>
### ReorderBlendPoint

`public System.Void ReorderBlendPoint(System.Int32 fromIndex, System.Int32 toIndex)`

Swaps two point entries, preserving their names/clocks and remapping triangle indices.

fromIndex: An existing source index.

toIndex: An existing destination index.

System.ObjectDisposedException: This resource has been disposed.

System.ArgumentOutOfRangeException: An index or numeric value is outside its documented domain.

<a id="member-acd78c6fc318"></a>
### SetBlendPointName

`public System.Void SetBlendPointName(System.Int32 point, System.String name)`

Renames a point, generating a unique suffix for conflicts and preserving per-tree child state.

point: An existing index.

name: A nonempty local name without slash/dot.

System.ObjectDisposedException: This resource has been disposed.

System.ArgumentOutOfRangeException: An index or numeric value is outside its documented domain.

<a id="member-68482f98415e"></a>
### SetBlendPointNode

`public System.Void SetBlendPointNode(System.Int32 point, Electron2D.AnimationRootNode node)`

Replaces a borrowed point definition, preserving its name and coordinates.

point: An existing index.

node: A live root resource without a containment cycle.

System.ObjectDisposedException: This resource has been disposed.

System.ArgumentOutOfRangeException: An index or numeric value is outside its documented domain.

<a id="member-4769e20483e4"></a>
### SetBlendPointPosition

`public System.Void SetBlendPointPosition(System.Int32 point, System.Single position)`

Changes finite point coordinates without clamping them to authoring bounds.

point: An existing index.

position: The finite coordinates.

System.ObjectDisposedException: This resource has been disposed.

System.ArgumentOutOfRangeException: An index or numeric value is outside its documented domain.

## Field summary

| Complete C# signature | Contract |
| --- | --- |
| `public static readonly Electron2D.AnimationParameter<System.Single> BlendPosition` | The finite per-tree blend position; defaults to zero. |

## Field Descriptions

<a id="member-b7dfa421e6cd"></a>
### BlendPosition

`public static readonly Electron2D.AnimationParameter<System.Single> BlendPosition`

The finite per-tree blend position; defaults to zero.

System.ObjectDisposedException: This resource has been disposed.

## Lifecycle, verification and dependencies

[ADR 0093](../decisions/scene-animation.md#adr-0093) and [blend spaces](../components/scene-animation.md#blend-spaces) own this executed profile. AnimationBlendSpaceTests verifies names/indices/capacity/cycles, endpoints/duplicate/degenerate/extreme geometry, automatic/manual topology, discrete/carry/PingPong, independent/cyclic clocks, shared libraries/live edits, callback failure/reentry/copies/borrowing and zero warmed managed bytes on 256 cyclic planar and 256 alternating carry passes. Two Wayland GPU and two compatibility Engine.Run cycles verify seven real pixel poses each and clean borrowed-resource shutdown. Cold preparation/triangulation, native/driver allocation, other-platform/human acceptance and scene/disk/editor round trips remain distinct. State-machine/transition/OneShot, expressions and non-property track schedulers remain separate coverage triggers.
