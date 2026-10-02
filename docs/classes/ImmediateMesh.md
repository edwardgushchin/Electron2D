# ImmediateMesh

Last updated: 2026-10-02

**Namespace:** `Electron2D` · **Declaration:** `public sealed class ImmediateMesh : Mesh` · **Source:** [ImmediateMesh.cs](../../src/Scene/Resources/ImmediateMesh.cs).

**Inherits:** [Mesh](Mesh.md) → [Resource](Resource.md).

## Description

Builds sequential 2D surfaces through one invisible draft. Begin selects topology and a borrowed material. SetColor/SetUV capture attributes for subsequent vertices; the first setter for a channel also fills all previous draft vertices with that value. End validates and publishes a copied surface into private ArrayMesh storage. A draft never contributes to queries, faces or drawing. All five primitive topologies execute through the existing retained mesh renderer. Points and lines retain its one-framebuffer-pixel policy.

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
| `public void SurfaceEnd()` | Validates, commits and ends the draft, then emits Changed. |
| `public void ClearSurfaces()` | Removes completed surfaces and cancels a draft, then emits Changed. |

## Method Descriptions

### SurfaceBegin

`public void SurfaceBegin(Mesh.PrimitiveType primitive, Material? material = null)`

Accepts Points, Lines, LineStrip, Triangles or TriangleStrip. A second Begin while active throws InvalidOperationException and retains the first draft. Invalid topology throws ArgumentOutOfRangeException; a disposed material or mesh throws ObjectDisposedException. Material is captured as a borrowed reference. Initially both optional channels are absent, so rendering supplies white color and zero UV.

### SurfaceAddVertex2D

`public void SurfaceAddVertex2D(Vector2 vertex)`

Appends a finite local position, plus the currently enabled color and UV. Nonfinite input throws ArgumentException before appending. Missing Begin throws InvalidOperationException. Vertices preserve append order with no explicit indices.

### SurfaceSetColor

`public void SurfaceSetColor(Color color)`

The first call enables Colors and fills all preceding vertices with `color`. Subsequent calls only change the value captured by future vertices. Finite components outside 0..1 are accepted, then clamped/truncated to RGBA8 UNORM on commit. Nonfinite values throw ArgumentException; missing Begin throws InvalidOperationException. Attribute edits are silent until End.

### SurfaceSetUV

`public void SurfaceSetUV(Vector2 uv)`

The first call enables primary UV and fills all preceding vertices with `uv`; subsequent calls only affect new vertices. Finite coordinates remain float pairs. Values outside 0..1 use the drawing item's sampler policy. Invalid input and inactive-builder errors follow SurfaceSetColor. Secondary UV is absent pending its actual shader attribute consumer, as recorded in [coverage](../coverage/classes/ImmediateMesh.md).

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

Returns inferred Vertex/Use2DVertices with Color and/or TexUV only when enabled in that surface. New drafts reset activation, independent of prior current attribute values.

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

Draft growth/structural commit are explicit authoring allocations, not a zero-allocation frame replacement API. UV2 awaits typed storage/packing and a real shader attribute consumer. 3D vertex/normal/tangent operations remain excluded. Inherited advanced mesh members keep their individual dependencies. Native allocator totals, wider platforms, broad performance and owner acceptance remain unverified.
