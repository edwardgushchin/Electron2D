# ImmediateMesh

Last updated: 2026-10-12

**Namespace:** `Electron2D` · **Declaration:** `public sealed class ImmediateMesh : Mesh` · **Source:** [ImmediateMesh.cs](../../src/Scene/Resources/ImmediateMesh.cs).

**Inherits:** [Mesh](Mesh.md) → [Resource](Resource.md).

## Description

Builds sequential 2D surfaces through one invisible draft. Begin selects topology and a borrowed material. SetColor/SetUV/SetUV2 capture attributes for subsequent vertices; the first setter for a channel also fills all previous draft vertices with that value. End validates and publishes a copied surface into private ArrayMesh storage. A draft never contributes to queries, faces or drawing. All five primitive topologies execute through the existing retained mesh renderer. Points and lines retain its one-framebuffer-pixel policy.

Methods serialize individual reads/edits. A caller sharing the resource must synchronize the whole begin/edit/end sequence; locking one call does not give a thread exclusive ownership of a draft. Material resources remain borrowed. Completed surface arrays are independent of caller query copies. Structural construction and duplication may allocate. Prepared replay and repeated material/transform edits reuse storage.

Completed edits emit Changed after commitment outside the storage lock, including clear and inherited material writes. A throwing listener leaves the committed state intact. Clear removes completed surfaces and cancels a draft. Duplication copies completed geometry and applies the inherited shallow/deep/local resource graph policy to materials; transient draft state is omitted. Dispose releases private storage and invalidates the outer mesh RID without disposing borrowed materials.

## Example

```csharp
using var mesh = new ImmediateMesh();
mesh.SurfaceBegin(Mesh.PrimitiveType.Triangles);
mesh.SurfaceSetColor(Colors.Red);
mesh.SurfaceAddVertex2D(new(0, 0));
mesh.SurfaceAddVertex2D(new(20, 0));
mesh.SurfaceAddVertex2D(new(0, 20));
mesh.SurfaceEnd();
var node = new MeshInstance { Mesh = mesh };
// Attach node to a Window and retain mesh until the scene is disposed.
```

The executable tests compile and exercise this public construction/drawing path through Engine.Run.

## Constructors

| Signature | Contract |
| --- | --- |
| `public ImmediateMesh()` | Empty completed storage and no draft. |

### .ctor

Construction is backend-independent. GetRID creates a stable borrowed resource identity on demand; no native renderer must be open.

## Methods

| Signature | Contract |
| --- | --- |
| `public void SurfaceBegin(Mesh.PrimitiveType primitive, Material? material = null)` | Starts an invisible draft. |
| `public void SurfaceAddVertex2D(Vector2 vertex)` | Captures the current attributes at a finite local position. |
| `public void SurfaceSetColor(Color color)` | Activates/backfills or changes current color. |
| `public void SurfaceSetUV(Vector2 uv)` | Activates/backfills or changes primary UV. |
| `public void SurfaceSetUV2(Vector2 uv2)` | Activates/backfills or changes independent secondary UV. |
| `public void SurfaceEnd()` | Validates, commits and ends the draft, then emits Changed. |
| `public void ClearSurfaces()` | Removes completed surfaces and cancels a draft, then emits Changed. |

## Method Descriptions

### SurfaceBegin

`public void SurfaceBegin(Mesh.PrimitiveType primitive, Material? material = null)`

Accepts Points, Lines, LineStrip, Triangles or TriangleStrip. A second Begin while active throws InvalidOperationException and retains the first draft. Invalid topology throws ArgumentOutOfRangeException; a disposed material or mesh throws ObjectDisposedException. Material is captured as a borrowed reference. Initially all optional channels are absent, so rendering supplies white color and zero UV.

### SurfaceAddVertex2D

`public void SurfaceAddVertex2D(Vector2 vertex)`

Appends a finite local position, plus the currently enabled color and primary/secondary UV. Nonfinite input throws ArgumentException before appending. Missing Begin throws InvalidOperationException. Vertices preserve append order with no explicit indices.

### SurfaceSetColor

`public void SurfaceSetColor(Color color)`

The first call enables Colors and fills all preceding vertices with `color`. Subsequent calls only change the value captured by future vertices. Finite components outside 0..1 are accepted, then clamped/truncated to RGBA8 UNORM on commit. Nonfinite values throw ArgumentException; missing Begin throws InvalidOperationException. Attribute edits are silent until End.

### SurfaceSetUV

`public void SurfaceSetUV(Vector2 uv)`

The first call enables primary UV and fills all preceding vertices with `uv`; subsequent calls only affect new vertices. Finite coordinates remain float pairs. Values outside 0..1 use the drawing item's sampler policy. Invalid input and inactive-builder errors follow SurfaceSetColor.

### SurfaceSetUV2

`public void SurfaceSetUV2(Vector2 uv2)`

Activates the independent secondary channel, backfilling all preceding draft vertices on its first assignment. Subsequent assignments affect future vertices. Nonfinite input, inactive draft and disposed resource checks match SurfaceSetUV and preserve draft state. Commit stores copied finite float pairs; new drafts reset activation. GPU shader fragments receive float2 at location four. Primary sampling and instance custom values remain independent.

### SurfaceEnd

`public void SurfaceEnd()`

Validates nonempty, complete topology and live draft material before publication. Lines require an even vertex count, LineStrip at least two, Triangles a multiple of three and TriangleStrip at least three; Points requires at least one. ArgumentException retains an incomplete draft for additional vertices and retry. Material disposal throws ObjectDisposedException without consuming the draft. Inactive End throws InvalidOperationException. Successful commit appends one surface, resets channel activation and clears draft/material references before Changed. Listener failure propagates after commit; reentrant listeners see an idle builder and may begin or clear it.

### ClearSurfaces

`public void ClearSurfaces()`

Cancels the active draft, clears its attributes/material, removes every completed surface and emits one Changed even when already empty. Borrowed materials remain usable. Retained drawing observes the empty storage directly; no custom-mesh snapshot rebuild is needed. Listener failure preserves the empty state. Disposed mesh access throws ObjectDisposedException.

## Protected overrides

| Signature | Contract |
| --- | --- |
| `protected override int OnGetSurfaceCount()` | Completed surface count. |
| `protected override MeshSurfaceData OnSurfaceGetArrays(int surfaceIndex)` | Independent channel copies. |
| `protected override int OnSurfaceGetArrayLen(int surfaceIndex)` | Sequential vertex count. |
| `protected override int OnSurfaceGetArrayIndexLen(int surfaceIndex)` | Zero for each valid surface. |
| `protected override Mesh.PrimitiveType OnSurfaceGetPrimitiveType(int surfaceIndex)` | Captured topology. |
| `protected override Mesh.ArrayFormat OnSurfaceGetFormat(int surfaceIndex)` | Vertex/Use2DVertices and activated Color/TexUV bits. |
| `protected override Material? OnSurfaceGetMaterial(int surfaceIndex)` | Borrowed surface material. |
| `protected override void OnSurfaceSetMaterial(int surfaceIndex, Material? material)` | Commit replacement before Changed. |
| `protected override Resource CreateDuplicateInstance()` | New exact ImmediateMesh. |
| `protected override void CopyCustomStateTo(Resource target, bool deep, DeepDuplicateMode subresourceMode, Func<Resource?, Resource?> duplicateSubresource, Func<Resource?, Resource?> forceDuplicateSubresource)` | Completed geometry and context-aware material graph copying. |
| `protected override void Dispose(bool disposing)` | Draft/private storage/outer identity cleanup. |

### OnGetSurfaceCount

Returns only completed surfaces through the inherited GetSurfaceCount wrapper. A draft does not increase this count.

### OnSurfaceGetArrays

Copies completed Vertices/Colors/UVs and the empty index array. Returned mutation cannot affect storage. Invalid indices throw ArgumentOutOfRangeException; disposal throws ObjectDisposedException.

### OnSurfaceGetArrayLen

Reads completed vertex count with the same index/lifetime validation.

### OnSurfaceGetArrayIndexLen

Returns zero after validating the index/lifetime; ImmediateMesh surfaces are sequential.

### OnSurfaceGetPrimitiveType

Returns the draft's captured topology after validation.

### OnSurfaceGetFormat

Returns inferred Vertex/Use2DVertices with Normal/Tangent, Color, TexUV and/or TexUV2 when enabled in that surface. New drafts reset activation, independent of prior current attribute values.

### OnSurfaceGetMaterial

Returns the borrowed completed material, or null for item material fallback. No material is acquired or cloned by this query.

### OnSurfaceSetMaterial

Commits the borrowed live replacement and emits Changed outside the lock, even on an equal write. Invalid index/disposed state rejects. Listener failure does not restore the old material. Drawing reads the current completed material during replay.

### CreateDuplicateInstance

Creates an empty exact resource for the inherited duplicate operation. No draft or logical RID is shared.

### CopyCustomStateTo

Snapshots completed geometry/material references while locked, then applies the supplied resource-copy function outside the source lock. Shallow copies borrow materials; deep copies preserve graph aliases through the inherited duplication context. Scene-local resources receive independent completed geometry. Pending draft state is not resource storage and is omitted. Inherited failure cleanup owns a failed duplicate.

### Dispose

Cancels the draft, disposes its owned private ArrayMesh and delegates outer resource/RID cleanup to Mesh. Materials remain borrowed. Subsequent public builder/query operations reject disposal.

## Dependencies, verification and limits

[ADR 0092](../decisions/mesh.md#adr-0092) owns the projection and notification adaptation. [Mesh](Mesh.md) documents inherited queries, material methods, GetFaces and identity. [Resource](Resource.md) documents graph/lifetime behavior. [Mesh component](../components/meshes.md) records backend integration.

[ImmediateMeshTests](../../tests/Electron2D.Tests/ImmediateMeshTests.cs) verifies state/finite/topology boundaries, late attributes/quantization, correction after failed End, observer failure/reentry, shallow/deep alias and local scene copies, query isolation, lifetime and already recorded command updates. Twenty warmup cycles precede 64 active material/transform/record/replay cycles and 64 idle replays; both measure zero managed bytes. [ImmediateMeshRenderingTests](../../tests/Electron2D.Tests/ImmediateMeshRenderingTests.cs) checks seven native pixel phases, resource RID drawing/server queries and all five topologies on Linux Wayland GPU/compatibility. Each backend also measures 64 active frames alternating all five built-in blends with null material and 64 idle frames after 20 warmup frames, from ProcessFrameStarted through FramePostDraw, with zero managed bytes.

Draft growth/structural commit are explicit authoring allocations, not a zero-allocation frame replacement API. Spatial vertex operations remain excluded; numerical normal/tangent capture executes. Inherited advanced mesh members keep their individual dependencies. Native allocator totals, wider platforms, broad performance and owner acceptance remain unverified.

Inherited Mesh.GetAABB returns local Rect2 visibility bounds for every stored vertex, including unreferenced vertices and point/line topologies. Prepared bounds/replay do not copy live ArrayMesh data. MeshInstance resource-change listeners use worker-safe invalidation; attached node setters retain owner/capture guards.

## Secondary mesh UV channel

[MeshSurfaceData.UV2s](MeshSurfaceData.md#uv2s) supplies copied finite float pairs independent of primary UV and instance data. [The mesh component](../components/meshes.md#secondary-uv-storage-and-shader-consumer) records packing, archives, shader locations and executable verification.

ImmediateMesh now registers its file factory and forwards completed surface bytes/material references through the same bounded ArrayMesh archive schema. Active drafts are excluded from saving; successful storage restoration cancels a draft, while malformed bytes preserve completed state and the draft. Fresh-process file restoration is checked in MeshUV2Tests.

### GetPropertyDescriptors

`protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors()`

Returns the inherited resource descriptors plus stored completed surface bytes and borrowed material references. Draft data is omitted. The file factory is available in fresh processes.

## Complete API

| Complete signature | Contract |
| --- | --- |
| `public ImmediateMesh()` | Creates an empty mesh without an active draft. |
| `public System.Void ClearSurfaces()` | Removes completed surfaces and cancels any draft, then emits Changed. |
| `protected override System.Void CopyCustomStateTo(Electron2D.Resource target, System.Boolean deep, Electron2D.DeepDuplicateMode subresourceMode, System.Func<Electron2D.Resource, Electron2D.Resource> duplicateSubresource, System.Func<Electron2D.Resource, Electron2D.Resource> forceDuplicateSubresource)` |  |
| `protected override Electron2D.Resource CreateDuplicateInstance()` |  |
| `protected override System.Void Dispose(System.Boolean disposing)` |  |
| `protected override System.Collections.Generic.IEnumerable<Electron2D.PropertyDescriptor> GetPropertyDescriptors()` |  |
| `protected override System.Int32 OnGetSurfaceCount()` |  |
| `protected override System.Int32 OnSurfaceGetArrayIndexLen(System.Int32 surfaceIndex)` |  |
| `protected override System.Int32 OnSurfaceGetArrayLen(System.Int32 surfaceIndex)` |  |
| `protected override Electron2D.MeshSurfaceData OnSurfaceGetArrays(System.Int32 surfaceIndex)` |  |
| `protected override Electron2D.Mesh.ArrayFormat OnSurfaceGetFormat(System.Int32 surfaceIndex)` |  |
| `protected override Electron2D.Material OnSurfaceGetMaterial(System.Int32 surfaceIndex)` |  |
| `protected override Electron2D.Mesh.PrimitiveType OnSurfaceGetPrimitiveType(System.Int32 surfaceIndex)` |  |
| `protected override System.Void OnSurfaceSetMaterial(System.Int32 surfaceIndex, Electron2D.Material material)` |  |
| `public System.Void SurfaceAddVertex2D(Electron2D.Vector2 vertex)` | Appends a finite local vertex using the current color and texture-coordinate channels. |
| `public System.Void SurfaceBegin(Electron2D.Mesh.PrimitiveType primitive, Electron2D.Material material = null)` | Begins an invisible surface using the selected topology and borrowed material. |
| `public System.Void SurfaceEnd()` | Validates and commits the draft, then emits Changed outside the storage lock. |
| `public System.Void SurfaceSetColor(Electron2D.Color color)` | Sets the current color, enabling the color channel on its first call. |
| `public System.Void SurfaceSetNormal(Electron2D.Vector3 normal)` | Assigns the current mesh-local normal and backfills the draft on first activation. |
| `public System.Void SurfaceSetTangent(Electron2D.Vector4 tangent)` | Assigns the current tangent and backfills the draft on first activation. |
| `public System.Void SurfaceSetUV(Electron2D.Vector2 uv)` | Sets the current primary UV, enabling that channel on its first call. |
| `public System.Void SurfaceSetUV2(Electron2D.Vector2 uv2)` | Sets the current secondary UV, enabling that channel on its first call. |

## Member descriptions

### ImmediateMesh

`public ImmediateMesh()`

summary: Creates an empty mesh without an active draft.

### ClearSurfaces

`public System.Void ClearSurfaces()`

summary: Removes completed surfaces and cancels any draft, then emits Changed.

remarks: Borrowed materials remain live. Retained drawing observes the emptied storage.

System.ObjectDisposedException: The mesh is disposed.

### CopyCustomStateTo

`protected override System.Void CopyCustomStateTo(Electron2D.Resource target, System.Boolean deep, Electron2D.DeepDuplicateMode subresourceMode, System.Func<Electron2D.Resource, Electron2D.Resource> duplicateSubresource, System.Func<Electron2D.Resource, Electron2D.Resource> forceDuplicateSubresource)`

### CreateDuplicateInstance

`protected override Electron2D.Resource CreateDuplicateInstance()`

### Dispose

`protected override System.Void Dispose(System.Boolean disposing)`

### GetPropertyDescriptors

`protected override System.Collections.Generic.IEnumerable<Electron2D.PropertyDescriptor> GetPropertyDescriptors()`

### OnGetSurfaceCount

`protected override System.Int32 OnGetSurfaceCount()`

### OnSurfaceGetArrayIndexLen

`protected override System.Int32 OnSurfaceGetArrayIndexLen(System.Int32 surfaceIndex)`

### OnSurfaceGetArrayLen

`protected override System.Int32 OnSurfaceGetArrayLen(System.Int32 surfaceIndex)`

### OnSurfaceGetArrays

`protected override Electron2D.MeshSurfaceData OnSurfaceGetArrays(System.Int32 surfaceIndex)`

### OnSurfaceGetFormat

`protected override Electron2D.Mesh.ArrayFormat OnSurfaceGetFormat(System.Int32 surfaceIndex)`

### OnSurfaceGetMaterial

`protected override Electron2D.Material OnSurfaceGetMaterial(System.Int32 surfaceIndex)`

### OnSurfaceGetPrimitiveType

`protected override Electron2D.Mesh.PrimitiveType OnSurfaceGetPrimitiveType(System.Int32 surfaceIndex)`

### OnSurfaceSetMaterial

`protected override System.Void OnSurfaceSetMaterial(System.Int32 surfaceIndex, Electron2D.Material material)`

### SurfaceAddVertex2D

`public System.Void SurfaceAddVertex2D(Electron2D.Vector2 vertex)`

summary: Appends a finite local vertex using the current color and texture-coordinate channels.

vertex: Local two-dimensional position.

System.InvalidOperationException: No surface is being built.

System.ArgumentException: The position is not finite.

System.ObjectDisposedException: The mesh is disposed.

### SurfaceBegin

`public System.Void SurfaceBegin(Electron2D.Mesh.PrimitiveType primitive, Electron2D.Material material = null)`

summary: Begins an invisible surface using the selected topology and borrowed material.

primitive: One of the five two-dimensional primitive topologies.

material: Live borrowed material, or null to use the drawing item's material.

System.InvalidOperationException: A surface is already being built.

System.ArgumentOutOfRangeException: The topology is invalid.

System.ObjectDisposedException: The mesh or material is disposed.

### SurfaceEnd

`public System.Void SurfaceEnd()`

summary: Validates and commits the draft, then emits Changed outside the storage lock.

remarks: An empty or incomplete primitive rejects without losing the draft; append the missing vertices and retry, or cancel with ClearSurfaces. A throwing Changed listener leaves the surface committed and the builder idle. Material disposal before commit rejects without consuming the draft.

System.InvalidOperationException: No surface is being built.

System.ArgumentException: The draft is empty or its topology is incomplete.

System.ObjectDisposedException: The mesh or draft material is disposed.

### SurfaceSetColor

`public System.Void SurfaceSetColor(Electron2D.Color color)`

summary: Sets the current color, enabling the color channel on its first call.

color: Finite color; clamped and truncated to RGBA8 when committed.

remarks: The first call also fills previously added vertices with this color. Later calls affect only subsequently appended vertices. A new surface starts without a color channel.

System.InvalidOperationException: No surface is being built.

System.ArgumentException: The color is not finite.

System.ObjectDisposedException: The mesh is disposed.

### SurfaceSetNormal

`public System.Void SurfaceSetNormal(Electron2D.Vector3 normal)`

summary: Assigns the current mesh-local normal and backfills the draft on first activation.

normal: Finite numerical direction.

### SurfaceSetTangent

`public System.Void SurfaceSetTangent(Electron2D.Vector4 tangent)`

summary: Assigns the current tangent and backfills the draft on first activation.

tangent: Finite XYZ direction and W orientation.

### SurfaceSetUV

`public System.Void SurfaceSetUV(Electron2D.Vector2 uv)`

summary: Sets the current primary UV, enabling that channel on its first call.

uv: Finite texture coordinate; values outside zero through one use the sampler policy.

remarks: The first call fills all previous vertices; subsequent calls affect only new vertices. New surfaces start without a UV channel.

System.InvalidOperationException: No surface is being built.

System.ArgumentException: The coordinate is not finite.

System.ObjectDisposedException: The mesh is disposed.

### SurfaceSetUV2

`public System.Void SurfaceSetUV2(Electron2D.Vector2 uv2)`

summary: Sets the current secondary UV, enabling that channel on its first call.

uv2: Finite coordinate independent of primary UVs.

remarks: The first call backfills previous vertices; later calls affect only new vertices. Each new surface begins without a secondary channel.

System.InvalidOperationException: No surface is being built.

System.ArgumentException: The coordinate is not finite.

System.ObjectDisposedException: The mesh is disposed.
