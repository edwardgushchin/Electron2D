# ImporterMesh

Last updated: 2026-10-18

**Namespace:** `Electron2D` · **Declaration:** `public sealed class Electron2D.ImporterMesh` · **Source:** [ImporterMesh.cs](../../src/Scene/Resources/ImporterMesh.cs).

**Inherits:** [Resource](Resource.md).

## Description

Copied raw mesh authoring and a real producer of drawable ArrayMesh snapshots and generated LODs. Raw channels retain supplied floating precision and exact custom records; compilation applies ordinary color/frame/UV/skin quantization. Flags remain exactly as supplied in GetSurfaceFormat, including zero rather than inferred channel bits. Surfaces require explicit finite positions and complete topology, matching masks/skin width and one raw target per definition. Normalized is the default target mode. AddBlendShape replaces colons with underscores and preserves duplicate names; compilation applies the existing ArrayMesh name-collision policy.

Materials are borrowed. Returned compiled meshes are caller owned snapshots; the producer retains a cached reference until authoring/revision changes, and never disposes already returned snapshots. Disposed cached output is recompiled. ResourceName and mode edits invalidate via revision. Clear removes definitions/surfaces while retaining mode; ResetState drops only transient cache. Copies preserve raw data, definitions, levels and material graph policy, with no compiled cache/native identity. ResourceLocalToScene creates independent local producers while preserving aliases within each instance.

GenerateLODs invokes pinned attribute-aware native simplification only for indexed triangles above 24 indices, halving targets to at least 12. It locks geometric borders, respects coincident UV/UV2 and handedness seams, retains normal/color error and uses regularized tessellation for shaped/skinned meshes. Custom/skin/target differences also prevent lossy coincident welding. Optional 2D bone poses affect error geometry only. Original channels/targets remain raw. Nontriangle levels remain; small/unindexed triangles have no generated levels. Thresholds are strictly increasing positive local edge errors, with 1e-10 floor and the next representable value for repeated zero-error levels. GenerateLODs publishes all surface replacements atomically after preparation. Native allocation failure raises OutOfMemoryException after retiring scratch storage, preserves prior levels and compiled-cache identity, and emits no Changed event. A native bundle without allocator ownership support raises PlatformNotSupportedException before simplification.

MergeImporterMeshes copies/transforms raw geometry, discards targets/levels and defaults to normalized mode. Compatible unskinned equal names/flags/channel schemas join, retaining the first material. Incompatible names use `_2`, `_3` and later free suffixes. Unnamed surfaces use `surface_N`. Missing-channel/schema mismatches and line strips remain separate. Mixed indexing preserves sequential geometry; reflected triangles swap every associated record, including skin/custom channels; reflected and joined triangle strips use degenerate index joins to preserve geometry and winding. Numeric normals/tangents use the source basis-normalization role, retaining tangent W. Source resources remain independent and require stable live revisions during import/merge.

Cold authoring, generation, archives, copies and first compilation may allocate. Cached GetMesh and warmed render replay reuse storage. Individual operations serialize; complete multithreaded workflows require caller synchronization. Listener failure follows committed-state Resource semantics. Compiling into an existing target appends unshaped data atomically; target shape definitions must be empty, and shaped input requires an empty target. Current cache ignores a later baseMesh argument. A target Changed listener sees complete cached geometry and cannot undo the publication.

## Example

```csharp
using var input = ImporterMesh.FromMesh(existingArrayMesh);
input.GenerateLODs(60);
using var drawable = input.GetMesh();
var instance = new MeshInstance { Mesh = drawable };
// Attach the instance to a Window; retain drawable until scene disposal.
```

The snippet requires a live `existingArrayMesh` with complete typed geometry and a matching native platform package. Generation needs the engine-owned mesh library; ordinary package/source consumers do not invoke native compilers. Source maintainers prepare native assets with the explicit native producer, and the public package workflow is checked with native tools blocked.

## Complete API

| Complete signature | Contract |
| --- | --- |
| `public ImporterMesh()` | Creates an empty normalized-mode producer. |
| `public System.Void AddBlendShape(System.String name)` | Adds a raw target name before surfaces exist; duplicate names retain their authored order. |
| `public System.Void AddSurface(Electron2D.Mesh.PrimitiveType primitive, Electron2D.MeshSurfaceData arrays, System.Collections.Generic.IReadOnlyList<Electron2D.MeshSurfaceData> blendShapes = null, System.Collections.Generic.IReadOnlyDictionary<System.Single, System.Int32[]> lods = null, Electron2D.Material material = null, System.String name = "", Electron2D.Mesh.ArrayFormat flags = None)` | Copies one complete raw surface without rendering quantization. |
| `public System.Void Clear()` | Removes all raw surfaces, definitions and compiled cache; mode is retained. |
| `protected override System.Void CopyCustomStateTo(Electron2D.Resource target, System.Boolean deep, Electron2D.DeepDuplicateMode subresourceMode, System.Func<Electron2D.Resource, Electron2D.Resource> duplicateSubresource, System.Func<Electron2D.Resource, Electron2D.Resource> forceDuplicateSubresource)` | Inherited Resource lifecycle implementation. |
| `protected override Electron2D.Resource CreateDuplicateInstance()` | Inherited Resource lifecycle implementation. |
| `protected override System.Void Dispose(System.Boolean disposing)` | Inherited Resource lifecycle implementation. |
| `public static Electron2D.ImporterMesh FromMesh(Electron2D.Mesh mesh)` | Copies applicable mesh state into a raw producer. |
| `public System.Void GenerateLODs(System.Single normalMergeAngle, System.Collections.Generic.IReadOnlyList<Electron2D.Transform> boneTransformArray = null)` | Replaces indexed triangle levels with actual attribute-aware simplification. |
| `public System.Int32 GetBlendShapeCount()` | Gets the number of raw target definitions. |
| `public System.String GetBlendShapeName(System.Int32 index)` | Gets an exact raw target name. |
| `public Electron2D.ArrayMesh GetMesh(Electron2D.ArrayMesh baseMesh = null)` | Compiles a caller-owned drawable snapshot, cached until authoring changes. |
| `protected override System.Collections.Generic.IEnumerable<Electron2D.PropertyDescriptor> GetPropertyDescriptors()` | Inherited Resource lifecycle implementation. |
| `public Electron2D.MeshSurfaceData GetSurfaceArrays(System.Int32 surface)` | Gets independent copies of raw surface channels. |
| `public Electron2D.MeshSurfaceData GetSurfaceBlendShapeArrays(System.Int32 surface, System.Int32 shape)` | Gets a copied raw target payload. |
| `public System.Int32 GetSurfaceCount()` | Gets the number of authored surfaces. |
| `public Electron2D.Mesh.ArrayFormat GetSurfaceFormat(System.Int32 surface)` | Gets the exact authored storage flags rather than inferred channels. |
| `public System.Int32 GetSurfaceLODCount(System.Int32 surface)` | Gets the number of stored sorted levels. |
| `public System.Int32[] GetSurfaceLODIndices(System.Int32 surface, System.Int32 lod)` | Gets independent copies of one level's indices. |
| `public System.Single GetSurfaceLODSize(System.Int32 surface, System.Int32 lod)` | Gets one level's local edge-error threshold. |
| `public Electron2D.Material GetSurfaceMaterial(System.Int32 surface)` | Gets the borrowed surface material. |
| `public System.String GetSurfaceName(System.Int32 surface)` | Gets a stored surface name. |
| `public Electron2D.Mesh.PrimitiveType GetSurfacePrimitiveType(System.Int32 surface)` | Gets a surface topology. |
| `public static Electron2D.ImporterMesh MergeImporterMeshes(System.Collections.Generic.IReadOnlyList<Electron2D.ImporterMesh> importerMeshes, System.Collections.Generic.IReadOnlyList<Electron2D.Transform> relativeTransforms, System.Boolean deduplicateSurfaces = true)` | Creates transformed raw surfaces, optionally joining compatible unskinned names. |
| `protected override System.Void OnResetState()` | Inherited Resource lifecycle implementation. |
| `public System.Void SetSurfaceMaterial(System.Int32 surface, Electron2D.Material material)` | Sets a borrowed material and invalidates the compiled snapshot. |
| `public System.Void SetSurfaceName(System.Int32 surface, System.String name)` | Sets a surface name and invalidates the compiled snapshot. |
| `public Electron2D.BlendShapeMode BlendShapeMode { get; set; }` | Gets or selects the target mode used by subsequently compiled meshes. |

## Member descriptions

### .ctor

`public ImporterMesh()`

summary: Creates an empty normalized-mode producer.

### AddBlendShape

`public System.Void AddBlendShape(System.String name)`

summary: Adds a raw target name before surfaces exist; duplicate names retain their authored order.

param name: Non-null name; colons become underscores and compilation applies ordinary mesh collisions.

### AddSurface

`public System.Void AddSurface(Electron2D.Mesh.PrimitiveType primitive, Electron2D.MeshSurfaceData arrays, System.Collections.Generic.IReadOnlyList<Electron2D.MeshSurfaceData> blendShapes = null, System.Collections.Generic.IReadOnlyDictionary<System.Single, System.Int32[]> lods = null, Electron2D.Material material = null, System.String name = "", Electron2D.Mesh.ArrayFormat flags = None)`

summary: Copies one complete raw surface without rendering quantization.

param primitive: One of the five supported topologies.

param arrays: Finite typed channels, copied before publication.

param blendShapes: One position/normal/tangent payload per raw name.

param lods: Positive thresholds with shorter complete index sets over an indexed base.

param material: Borrowed live material or null.

param name: Non-null surface name.

param flags: Supported raw channel/storage flags; kept exactly for GetSurfaceFormat.

remarks: Channel/skin/schema failures leave prior surfaces intact. Changed runs after commit.

### Clear

`public System.Void Clear()`

summary: Removes all raw surfaces, definitions and compiled cache; mode is retained.

### CopyCustomStateTo

`protected override System.Void CopyCustomStateTo(Electron2D.Resource target, System.Boolean deep, Electron2D.DeepDuplicateMode subresourceMode, System.Func<Electron2D.Resource, Electron2D.Resource> duplicateSubresource, System.Func<Electron2D.Resource, Electron2D.Resource> forceDuplicateSubresource)`

### CreateDuplicateInstance

`protected override Electron2D.Resource CreateDuplicateInstance()`

### Dispose

`protected override System.Void Dispose(System.Boolean disposing)`

### FromMesh

`public static Electron2D.ImporterMesh FromMesh(Electron2D.Mesh mesh)`

summary: Copies applicable mesh state into a raw producer.

param mesh: Live source or null for an empty producer.

returns: A new caller-owned ImporterMesh.

### GenerateLODs

`public System.Void GenerateLODs(System.Single normalMergeAngle, System.Collections.Generic.IReadOnlyList<Electron2D.Transform> boneTransformArray = null)`

summary: Replaces indexed triangle levels with actual attribute-aware simplification.

param normalMergeAngle: Finite angular threshold in degrees for welding coincident normal-compatible vertices.

param boneTransformArray: Optional finite two-dimensional skin pose used to measure errors; original channels remain raw.

remarks: Nontriangle levels remain intact; unindexed/small triangles produce no generated levels. All surfaces preflight before publication. Generation may allocate managed/native temporary storage and needs the matching native mesh library.

exception T:System.ArgumentException: Coordinates, pose, thresholds or generated data are invalid.

### GetBlendShapeCount

`public System.Int32 GetBlendShapeCount()`

summary: Gets the number of raw target definitions.

returns: Nonnegative definition count.

### GetBlendShapeName

`public System.String GetBlendShapeName(System.Int32 index)`

summary: Gets an exact raw target name.

param index: Existing definition index.

returns: The stored name.

### GetMesh

`public Electron2D.ArrayMesh GetMesh(Electron2D.ArrayMesh baseMesh = null)`

summary: Compiles a caller-owned drawable snapshot, cached until authoring changes.

param baseMesh: Optional live target; unshaped existing surfaces are appended atomically. Ignored when the current cache exists.

returns: Cached ArrayMesh, or null when no surfaces exist.

remarks: Quantization occurs here. Earlier returned snapshots remain alive after edits/disposal of the producer. A target Changed listener failure retains committed geometry and the producer's cache.

### GetPropertyDescriptors

`protected override System.Collections.Generic.IEnumerable<Electron2D.PropertyDescriptor> GetPropertyDescriptors()`

### GetSurfaceArrays

`public Electron2D.MeshSurfaceData GetSurfaceArrays(System.Int32 surface)`

summary: Gets independent copies of raw surface channels.

param surface: Existing surface index.

returns: Copied arrays with authoring precision.

### GetSurfaceBlendShapeArrays

`public Electron2D.MeshSurfaceData GetSurfaceBlendShapeArrays(System.Int32 surface, System.Int32 shape)`

summary: Gets a copied raw target payload.

param surface: Existing surface index.

param shape: Existing definition index.

returns: Copied position/normal/tangent arrays.

### GetSurfaceCount

`public System.Int32 GetSurfaceCount()`

summary: Gets the number of authored surfaces.

returns: Nonnegative surface count.

### GetSurfaceFormat

`public Electron2D.Mesh.ArrayFormat GetSurfaceFormat(System.Int32 surface)`

summary: Gets the exact authored storage flags rather than inferred channels.

param surface: Existing surface index.

returns: The AddSurface flags value.

### GetSurfaceLODCount

`public System.Int32 GetSurfaceLODCount(System.Int32 surface)`

summary: Gets the number of stored sorted levels.

param surface: Existing surface index.

returns: Nonnegative LOD count.

### GetSurfaceLODIndices

`public System.Int32[] GetSurfaceLODIndices(System.Int32 surface, System.Int32 lod)`

summary: Gets independent copies of one level's indices.

param surface: Existing surface index.

param lod: Existing level in ascending-threshold order.

returns: Copied indices into the raw base channels.

### GetSurfaceLODSize

`public System.Single GetSurfaceLODSize(System.Int32 surface, System.Int32 lod)`

summary: Gets one level's local edge-error threshold.

param surface: Existing surface index.

param lod: Existing level in ascending-threshold order.

returns: Positive finite local error.

### GetSurfaceMaterial

`public Electron2D.Material GetSurfaceMaterial(System.Int32 surface)`

summary: Gets the borrowed surface material.

param surface: Existing surface index.

returns: The authored material or null.

### GetSurfaceName

`public System.String GetSurfaceName(System.Int32 surface)`

summary: Gets a stored surface name.

param surface: Existing surface index.

returns: Exact ordinal name, possibly empty.

### GetSurfacePrimitiveType

`public Electron2D.Mesh.PrimitiveType GetSurfacePrimitiveType(System.Int32 surface)`

summary: Gets a surface topology.

param surface: Existing surface index.

returns: The authored topology.

### MergeImporterMeshes

`public static Electron2D.ImporterMesh MergeImporterMeshes(System.Collections.Generic.IReadOnlyList<Electron2D.ImporterMesh> importerMeshes, System.Collections.Generic.IReadOnlyList<Electron2D.Transform> relativeTransforms, System.Boolean deduplicateSurfaces = true)`

summary: Creates transformed raw surfaces, optionally joining compatible unskinned names.

param importerMeshes: One or more live source producers.

param relativeTransforms: One finite local two-dimensional transform per producer.

param deduplicateSurfaces: Joins equal names, flags, channel schemas and topologies except line strips; skin surfaces remain separate.

returns: A new caller-owned producer in normalized mode.

remarks: Named targets and authored/generated levels are discarded. Joined surfaces retain the first material. Unnamed input surfaces use surface_N; incompatible collisions use the first free underscore suffix starting at two. Reflections retain complete geometry/winding, including strips, mixed indexing and all four/eight skin records. Schema mismatches and independent line strips stay separate rather than producing malformed channels or connecting segments.

### OnResetState

`protected override System.Void OnResetState()`

### SetSurfaceMaterial

`public System.Void SetSurfaceMaterial(System.Int32 surface, Electron2D.Material material)`

summary: Sets a borrowed material and invalidates the compiled snapshot.

param surface: Existing surface index.

param material: Live material or null.

### SetSurfaceName

`public System.Void SetSurfaceName(System.Int32 surface, System.String name)`

summary: Sets a surface name and invalidates the compiled snapshot.

param surface: Existing surface index.

param name: Non-null name.

### BlendShapeMode

`public Electron2D.BlendShapeMode BlendShapeMode { get; set; }`

summary: Gets or selects the target mode used by subsequently compiled meshes.

value: Normalized initially; edits invalidate the compiled snapshot.

## Storage, errors and verification

The typed IMP2 raw archive (reading IMP1) stores 4096 surfaces/definitions/levels, at most 65536 UTF-16 units per name and 64 MiB total. Counts/schemas/truncation/finite data/topology/flags/levels validate before replacement. Mode is a stored property; borrowed materials are a separate typed array preserving resource aliases. Compiled cache/native identities are transient. The loader has exact built-in registration; fresh .e2dscene processes restore and execute native generation plus actual canvas geometry.

[ImporterMeshTests](../../tests/Electron2D.Tests/ImporterMeshTests.cs) covers raw precision, copies/modes/names, cached identity, target callback failure, independent snapshots/disposal, real decreasing levels, monotonic errors, six independent pinned C++ index corpora, eight-slot poses and late-surface rollback, UV seams, reflected/mixed topology, material aliases, malformed archives and fresh/local scenes. [Native tests](../../tests/Electron2D.Tests/ImporterMeshNativeTests.cs) exercise GLSL/HLSL numeric attributes, reflected tangent signs, generated levels, silhouette/gaps, morph, eighth-slot skin, groups, instances and owned drawing. Each measures 24 warmup, 64 active and 64 idle complete frames at zero managed allocation. Full native allocations, foreign platforms and human acceptance remain separate.

The shared empty-position flag executes through procedural vertex geometry and independent non-position storage. Lightmap atlas hints retain the 3D lighting exclusion. See [ADR 0092](../decisions/mesh.md#adr-0092), [the mesh component](../components/meshes.md#importer-mesh) and [coverage](../coverage/classes/ImporterMesh.md).

## Procedural mesh integration

UseEmptyVertexArray retains independent counts, absent positions, direction targets and authored levels through raw IMP2 (reading IMP1), copies and compilation. GenerateLODs preserves authored procedural levels. Identity merges retain/remap channels; nonidentity merge transforms reject unavailable positions. See [the connected contract](../components/meshes.md#procedural-surfaces-without-positions).
