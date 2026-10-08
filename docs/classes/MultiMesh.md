# MultiMesh

Last updated: 2026-10-09

**Namespace:** `Electron2D` · **Declaration:** `public sealed class Electron2D.MultiMesh` · **Source:** [MultiMesh.cs](../../src/Scene/Resources/MultiMesh.cs).

**Inherits:** [Resource](Resource.md).

## Executable mesh skin integration

An attached canvas palette can now deform its borrowed mesh during actual MultiMesh replay. Per-instance transforms/colors/custom data remain applied afterward; GetAABB/presentation bounds retain authored geometry.

## Description

Fixed two-dimensional instance resource borrowing a Mesh and owning copied packed current/previous records. Configure UseColors/UseCustomData at zero count, then allocate and author every transform; initial records are zero, including colors. VisibleInstanceCount selects a prefix without reallocating. Queries return current logical data. Empty legacy assignments are no-ops; finite full-sized edits preflight before commit. Changes commit before Changed listeners and listener failure retains data. Resource methods serialize individual operations; callers synchronize compound configuration/authoring sequences.

GetAABB returns Rect2 bounds for the visible prefix or manual CustomAABB; zero override enables computation. Culling uses presentation poses and finite rectangles. Fast interpolates components; High preserves rotation/scale/skew for compatible nonsingular bases and falls back otherwise. Tick capture/reset/explicit-pair behavior is described by each member. Scene consumers refresh the shared resource policy; last assigned effective policy applies to shared storage. Ordinary writes outside ticks reset edited previous values.

Shallow copies borrow Mesh, deep and scene-local copies follow inherited resource graph rules and own independent records. New copies reset transient previous state and logical identities. Disposal invalidates this resource RID and storage without disposing Mesh. Renderer-owned allocation exposes the same ordinary behavior; indirect/device-buffer storage is absent. GPU draws use a retained hardware stream for eligible unskinned ArrayMesh/ImmediateMesh triangles; other cases retain CPU expansion. Both preserve surface/instance order and finite presentation validation. Raw custom fragment data reaches GPU shaders; compatibility rejects custom shaders.

## Example

```csharp
using var mesh = new ArrayMesh();
mesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, new MeshSurfaceData
{ Vertices = [new(0, 0), new(12, 0), new(0, 12)] });
using var instances = new MultiMesh { Mesh = mesh, UseColors = true, InstanceCount = 2 };
for (int i = 0; i < 2; i++)
{
    instances.SetInstanceTransform2D(i, new Transform(0, new(i * 20, 0)));
    instances.SetInstanceColor(i, Colors.White);
}
var node = new MultiMeshInstance { MultiMesh = instances };
// Add node to a Window and run Engine.Run(window).
```

## Constructors

| Complete signature | Contract |
| --- | --- |
| `public MultiMesh()` | Creates an empty collection with no optional channels and all instances visible. |

## Constructors descriptions

<a id="member-940e06f4c708"></a>
### .ctor

`public MultiMesh()`

Creates an empty collection with no optional channels and all instances visible.

## Properties

| Complete signature | Contract |
| --- | --- |
| `public System.Single[] Buffer { get; set; }` | Gets a copied packed buffer or assigns a whole copied finite buffer. |
| `public Electron2D.Color[] ColorArray { get; set; }` | Gets or sets copied legacy instance color values. |
| `public Electron2D.Rect2 CustomAABB { get; set; }` | Gets or sets a manual local visibility rectangle for the whole instance resource. |
| `public Electron2D.Color[] CustomDataArray { get; set; }` | Gets or sets copied legacy four-component shader values. |
| `public System.Int32 InstanceCount { get; set; }` | Gets or sets the allocated instance count. |
| `public Electron2D.Mesh Mesh { get; set; }` | Gets or sets the borrowed mesh drawn by every visible instance. |
| `public Electron2D.MultiMesh.PhysicsInterpolationQuality PhysicsInterpolationQualityMode { get; set; }` | Gets or sets the two-dimensional basis interpolation policy. |
| `public Electron2D.Vector2[] Transform2DArray { get; set; }` | Gets or sets copied legacy packed transform columns. |
| `public System.Boolean UseColors { get; set; }` | Gets or sets whether packed records include instance color multipliers. |
| `public System.Boolean UseCustomData { get; set; }` | Gets or sets whether records include four raw floating shader components. |
| `public System.Int32 VisibleInstanceCount { get; set; }` | Gets or sets the visible prefix without reallocating packed storage. |

## Properties descriptions

<a id="member-97039ca53646"></a>
### Buffer

`public System.Single[] Buffer { get; set; }`

Gets a copied packed buffer or assigns a whole copied finite buffer.

Value: Records store X.X, Y.X, zero padding, Origin.X, X.Y, Y.Y, zero padding, Origin.Y, then optional RGBA channels.

Remarks: Caller-supplied padding is retained. Outside a physics tick, ordinary writes reset their previous values. Use SetBufferInterpolated to supply an explicit presentation pair.

System.ArgumentException: The size differs from capacity times stride or any float is nonfinite.

<a id="member-0741d0a9d8b4"></a>
### ColorArray

`public Electron2D.Color[] ColorArray { get; set; }`

Gets or sets copied legacy instance color values.

Value: An empty array when the channel is disabled. Empty assignments are no-ops.

<a id="member-60212350db62"></a>
### CustomAABB

`public Electron2D.Rect2 CustomAABB { get; set; }`

Gets or sets a manual local visibility rectangle for the whole instance resource.

Value: The zero rectangle selects automatic bounds; other finite nonnegative rectangles override computation.

Remarks: Bounds are expressed in this resource's local coordinates and participate in native canvas culling.

System.ArgumentException: Bounds are nonfinite, negative-sized or have an unrepresentable end.

<a id="member-dde46a01cdac"></a>
### CustomDataArray

`public Electron2D.Color[] CustomDataArray { get; set; }`

Gets or sets copied legacy four-component shader values.

Value: An empty array when the channel is disabled. Empty assignments are no-ops.

<a id="member-4633f3a94ee1"></a>
### InstanceCount

`public System.Int32 InstanceCount { get; set; }`

Gets or sets the allocated instance count.

Value: Zero initially. A different count clears both packed snapshots; equal writes preserve data.

Remarks: Allocation is explicit cold work. Shrinking clamps an authored visible count to the new capacity.

System.ArgumentOutOfRangeException: The count is negative or packed storage exceeds managed array limits.

<a id="member-896792d8086f"></a>
### Mesh

`public Electron2D.Mesh Mesh { get; set; }`

Gets or sets the borrowed mesh drawn by every visible instance.

Value: Null initially; changing the reference does not reset instance data.

System.ObjectDisposedException: This resource or the assigned mesh is disposed.

<a id="member-31243dd30cd0"></a>
### PhysicsInterpolationQualityMode

`public Electron2D.MultiMesh.PhysicsInterpolationQuality PhysicsInterpolationQualityMode { get; set; }`

Gets or sets the two-dimensional basis interpolation policy.

Value: Fast initially; High retains angular motion where finite compatible bases allow decomposition.

System.ArgumentOutOfRangeException: The selector is undefined.

<a id="member-4fc6c1f23c77"></a>
### Transform2DArray

`public Electron2D.Vector2[] Transform2DArray { get; set; }`

Gets or sets copied legacy packed transform columns.

Value: Three Vector2 columns per instance: X, Y and Origin. An empty assignment is a no-op.

System.ArgumentException: A nonempty array has a different count or nonfinite column.

<a id="member-a6c8fa568ae7"></a>
### UseColors

`public System.Boolean UseColors { get; set; }`

Gets or sets whether packed records include instance color multipliers.

Value: False initially; configure before allocating instances.

System.InvalidOperationException: Instances are allocated.

<a id="member-bce2c2785fea"></a>
### UseCustomData

`public System.Boolean UseCustomData { get; set; }`

Gets or sets whether records include four raw floating shader components.

Value: False initially. Shader data is independent of color multiplication.

System.InvalidOperationException: Instances are allocated.

<a id="member-94d8c54c1743"></a>
### VisibleInstanceCount

`public System.Int32 VisibleInstanceCount { get; set; }`

Gets or sets the visible prefix without reallocating packed storage.

Value: Minus one draws every instance; otherwise zero through InstanceCount.

System.ArgumentOutOfRangeException: The count is less than minus one or greater than capacity.

## Methods and protected extension points

| Complete signature | Contract |
| --- | --- |
| `protected override System.Void CopyCustomStateTo(Electron2D.Resource target, System.Boolean deep, Electron2D.DeepDuplicateMode subresourceMode, System.Func<Electron2D.Resource, Electron2D.Resource> duplicateSubresource, System.Func<Electron2D.Resource, Electron2D.Resource> forceDuplicateSubresource)` | Inherited resource/node hook; see Description for ownership and lifecycle. |
| `protected override Electron2D.Resource CreateDuplicateInstance()` | Inherited resource/node hook; see Description for ownership and lifecycle. |
| `protected override System.Void Dispose(System.Boolean disposing)` | Inherited resource/node hook; see Description for ownership and lifecycle. |
| `public Electron2D.Rect2 GetAABB()` | Gets the local visibility rectangle of the visible instance prefix. |
| `public Electron2D.Color GetInstanceColor(System.Int32 instance)` | Gets one stored instance color multiplier. |
| `public Electron2D.Color GetInstanceCustomData(System.Int32 instance)` | Gets one stored four-component shader value. |
| `public Electron2D.Transform GetInstanceTransform2D(System.Int32 instance)` | Returns the current stored local transform for one instance. |
| `public override Electron2D.RID GetRID()` | Gets the stable borrowed resource identity. |
| `public System.Void ResetInstancePhysicsInterpolation(System.Int32 instance)` | Prevents interpolation for one instance by copying its current record to the previous snapshot. |
| `public System.Void ResetInstancesPhysicsInterpolation()` | Resets all presentation records to current values without editing logical data. |
| `public System.Void SetBufferInterpolated(System.ReadOnlySpan<System.Single> bufferCurrent, System.ReadOnlySpan<System.Single> bufferPrevious)` | Copies coherent current and previous packed records for interpolated rendering. |
| `public System.Void SetInstanceColor(System.Int32 instance, Electron2D.Color color)` | Sets one finite instance color multiplier. |
| `public System.Void SetInstanceCustomData(System.Int32 instance, Electron2D.Color customData)` | Sets one finite four-component shader value. |
| `public System.Void SetInstanceTransform2D(System.Int32 instance, Electron2D.Transform transform)` | Commits a finite local transform for one instance. |

## Methods and protected extension points descriptions

<a id="member-6a74c0af3862"></a>
### CopyCustomStateTo

`protected override System.Void CopyCustomStateTo(Electron2D.Resource target, System.Boolean deep, Electron2D.DeepDuplicateMode subresourceMode, System.Func<Electron2D.Resource, Electron2D.Resource> duplicateSubresource, System.Func<Electron2D.Resource, Electron2D.Resource> forceDuplicateSubresource)`

Inherited resource/node hook; see Description for ownership and lifecycle.

<a id="member-1c6ef14f5a14"></a>
### CreateDuplicateInstance

`protected override Electron2D.Resource CreateDuplicateInstance()`

Inherited resource/node hook; see Description for ownership and lifecycle.

<a id="member-1dbdbc56f4c9"></a>
### Dispose

`protected override System.Void Dispose(System.Boolean disposing)`

Inherited resource/node hook; see Description for ownership and lifecycle.

<a id="member-3215a6e03183"></a>
### GetAABB

`public Electron2D.Rect2 GetAABB()`

Gets the local visibility rectangle of the visible instance prefix.

Returns: The manual rectangle or merged transformed mesh bounds, empty without visible geometry.

<a id="member-fc007d6d7a73"></a>
### GetInstanceColor

`public Electron2D.Color GetInstanceColor(System.Int32 instance)`

Gets one stored instance color multiplier.

instance: Existing zero-based index.

Returns: Four raw finite color components.

System.InvalidOperationException: The color channel is disabled.

<a id="member-e5e67e2080dc"></a>
### GetInstanceCustomData

`public Electron2D.Color GetInstanceCustomData(System.Int32 instance)`

Gets one stored four-component shader value.

instance: Existing zero-based index.

Returns: Raw shader data represented by Color without color conversion.

System.InvalidOperationException: The custom channel is disabled.

<a id="member-302dcf8727ba"></a>
### GetInstanceTransform2D

`public Electron2D.Transform GetInstanceTransform2D(System.Int32 instance)`

Returns the current stored local transform for one instance.

instance: Existing zero-based instance index.

Returns: The un-interpolated two-dimensional transform.

System.ArgumentOutOfRangeException: The index is outside allocated storage.

<a id="member-4b2d81ab1798"></a>
### GetRID

`public override Electron2D.RID GetRID()`

Gets the stable borrowed resource identity.

Returns: A weak logical RID valid until resource disposal, independently of a renderer.

<a id="member-38fbbbf575ff"></a>
### ResetInstancePhysicsInterpolation

`public System.Void ResetInstancePhysicsInterpolation(System.Int32 instance)`

Prevents interpolation for one instance by copying its current record to the previous snapshot.

instance: Existing zero-based index.

<a id="member-d2663fe8bc1d"></a>
### ResetInstancesPhysicsInterpolation

`public System.Void ResetInstancesPhysicsInterpolation()`

Resets all presentation records to current values without editing logical data.

<a id="member-8fdce1b18aa7"></a>
### SetBufferInterpolated

`public System.Void SetBufferInterpolated(System.ReadOnlySpan<System.Single> bufferCurrent, System.ReadOnlySpan<System.Single> bufferPrevious)`

Copies coherent current and previous packed records for interpolated rendering.

bufferCurrent: Whole finite current buffer.

bufferPrevious: Whole finite previous buffer of the same schema.

System.ArgumentException: A buffer is malformed; both snapshots remain unchanged.

<a id="member-894a0a166491"></a>
### SetInstanceColor

`public System.Void SetInstanceColor(System.Int32 instance, Electron2D.Color color)`

Sets one finite instance color multiplier.

instance: Existing zero-based index.

color: Raw multiplier; components are not clamped in CPU storage.

System.InvalidOperationException: The color channel is disabled.

System.ArgumentException: A component is nonfinite.

<a id="member-84a383f4b372"></a>
### SetInstanceCustomData

`public System.Void SetInstanceCustomData(System.Int32 instance, Electron2D.Color customData)`

Sets one finite four-component shader value.

instance: Existing zero-based index.

customData: Raw components, independent of the color multiplier.

System.InvalidOperationException: The custom channel is disabled.

System.ArgumentException: A component is nonfinite.

<a id="member-5918fedca9cd"></a>
### SetInstanceTransform2D

`public System.Void SetInstanceTransform2D(System.Int32 instance, Electron2D.Transform transform)`

Commits a finite local transform for one instance.

instance: Existing zero-based index.

transform: Finite basis and translation; singular and reflected bases are accepted.

System.ArgumentException: The transform is nonfinite.

System.ArgumentOutOfRangeException: The index is outside storage.

## Nested enumeration

[MultiMesh.PhysicsInterpolationQuality](MultiMesh.PhysicsInterpolationQuality.md) defines the shared Fast/High interpolation policy.

## Dependencies, errors and verification

[ADR 0092](../decisions/mesh.md#adr-0092) and [mesh component](../components/meshes.md#repeated-instance-resources) define ownership, typed bounds, callbacks and backend limits. MultiMeshTests checks resource/scene failures and warmed replay; MultiMeshRenderingTests checks native pixels, actual intermediate poses, owned identities and 256-instance active/idle frames. Native allocator totals, structural allocation, dense-scene cost, other targets and owner acceptance remain unverified. Scene-file persistence remains a separate dependency.
