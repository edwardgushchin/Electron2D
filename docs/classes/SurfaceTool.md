# Typed surface authoring

Last updated: 2026-10-12

SurfaceTool : ElectronObject builds and edits supported two-dimensional MeshSurfaceData under [ADR 0092](../decisions/mesh.md#adr-0092). It has deterministic lifetime and borrows source/target meshes and materials. Every public call rejects a disposed builder. Mutations/disposal during source capture throw InvalidOperationException. Draft-only setters reject inactive drafts and late channel activation. Completed ArrayMesh resources use the existing canvas and indexed RenderingDevice consumers.

Begin replaces prior geometry/attributes/material while retaining the selected four/eight influence width. Activate color, primary/secondary UV and paired bone/weight channels before the first vertex. Later values affect subsequent vertices. Clear resets all defaults, including four influences and Lines. SetSkinWeightCount accepts the enum mode and works before Begin or before the first vertex; imported data infers actual width. Short influences pad zero; overfull influences stably select the strongest values and normalize with a finite double sum. Ordinary four/eight records preserve authored weights until mesh UNORM16 packing.

CommitToArrays returns independent current arrays, permitting incomplete authoring. Commit validates final topology/index bounds, appends copied geometry and the borrowed material together before one Changed, and preserves the builder. Empty authoring returns the existing/new empty mesh. Invalid data preserves target storage; observer failure retains the committed surface. Structural authoring and snapshots allocate; repeated committed playback uses prepared buffers.

CreateFrom and CreateFromArrays replace state only after validation. Source callbacks must retain stable revisions and cannot mutate/dispose the receiving builder. Imported geometry supports structural editing/append/commit; Begin starts a new incremental draft. AppendFrom uses a finite Transform, requires compatible nonempty topology/skin width, transforms copied positions and fills missing color/UV with white/zero. Mixed indexed/sequential sources retain both prefixes. AddTriangleFan appends wound triangles and extends existing indices; absent optional entries retain current values during fan traversal. All five primitive types execute. GetAABB returns finite local Rect2 bounds including unused vertices.

Index welds exact complete records, including captured colors/UVs/skin/groups. Existing indices are unchanged. Deindex validates every reference before replacing geometry. OptimizeIndicesForCache traverses bounded adjacency and accepts a reordered triangle list only when a sixteen-entry LRU model improves; winding and attributes are preserved, global triangle order can change. The retained canvas expands indices; RenderingDevice accepts actual index buffers. Model improvement does not prove physical GPU speed.

Normal/tangent numerical data, generation and fan arguments execute through the shared pipeline. Custom codecs/shader consumers and morph pose extraction retain individual coverage triggers. No inert setter or unavailable format is exposed. Deprecated LOD generation is excluded using pinned metadata; its current replacement belongs to the future importer/LOD consumer slice.

[SurfaceToolTests](../../tests/Electron2D.Tests/SurfaceToolTests.cs) checks finite/state/channel boundaries, retries, skin selection/overflow, atomic material publication, callback failure/reentry, transformed/mixed append, all topologies, exact index/deindex, bounded cache reorder on 256x256 geometry, fresh-process saved output and warmed prepared replay. [SurfaceToolNativeTests](../../tests/Electron2D.Tests/SurfaceToolNativeTests.cs) verifies identical pre/post cache pixels from real GLSL/HLSL indexed device drawing, UV2/eighth influence/hardware instance output and five canvas topologies on GPU/compatibility. Each device measures 64 alternating replay/readback frames; each retained host measures 64 active and 64 idle frames after warmup, with zero managed bytes on the owner thread. Wider platforms, native allocation totals, physical cache performance and human acceptance remain unverified.


## Complete API

| Complete signature | Contract |
| --- | --- |
| `public SurfaceTool()` | Creates an empty builder with line topology and four influence slots. |
| `public System.Void AddIndex(System.Int32 index)` | Appends a nonnegative index, permitting references to vertices authored later. |
| `public System.Void AddTriangleFan(System.ReadOnlySpan<Electron2D.Vector2> vertices, System.ReadOnlySpan<Electron2D.Vector2> uvs = default, System.ReadOnlySpan<Electron2D.Color> colors = default, System.ReadOnlySpan<Electron2D.Vector2> uv2s = default, System.ReadOnlySpan<Electron2D.Vector3> normals = default, System.ReadOnlySpan<Electron2D.Vector4> tangents = default)` | Appends sequential triangles for a fan using the current supported attributes. |
| `public System.Void AddVertex(Electron2D.Vector2 vertex)` | Captures a finite local vertex with the current enabled attributes. |
| `public System.Void AppendFrom(Electron2D.Mesh existing, System.Int32 surface, Electron2D.Transform transform)` | Appends copied transformed geometry while preserving existing draw order and attributes. |
| `public System.Void Begin(Electron2D.Mesh.PrimitiveType primitive)` | Clears prior authoring and starts a surface. |
| `public System.Void Clear()` | Clears geometry, attributes and borrowed material and restores four-slot line defaults. |
| `public Electron2D.ArrayMesh Commit(Electron2D.ArrayMesh existing = null, Electron2D.Mesh.ArrayFormat flags = None)` | Publishes one validated copied surface with its borrowed material and preserves the builder. |
| `public Electron2D.MeshSurfaceData CommitToArrays()` | Returns independent typed arrays for the current authoring state. |
| `public System.Void CreateFrom(Electron2D.Mesh existing, System.Int32 surface)` | Replaces geometry with a validated copied surface and its borrowed material. |
| `public System.Void CreateFromArrays(Electron2D.MeshSurfaceData arrays, Electron2D.Mesh.PrimitiveType primitiveType = Triangles)` | Replaces geometry with validated copied typed arrays. |
| `public System.Void Deindex()` | Expands current indices into ordered complete vertex records and removes the index channel. |
| `protected override System.Void Dispose(System.Boolean disposing)` |  |
| `public System.Void GenerateNormals(System.Boolean flip = false)` | Generates planar triangle normals, respecting captured smooth groups. |
| `public System.Void GenerateTangents()` | Generates tangent directions from normal/UV channels using MikkTSpace. |
| `public Electron2D.Rect2 GetAABB()` | Gets the local rectangle enclosing every stored vertex, including unreferenced vertices. |
| `public Electron2D.Mesh.PrimitiveType GetPrimitiveType()` | Gets the selected primitive topology, including while empty. |
| `public Electron2D.SurfaceTool.SkinWeightCount GetSkinWeightCount()` | Gets the selected influence-width mode. |
| `public System.Void Index()` | Replaces sequential vertices with exact unique records and their original ordered indices. |
| `public System.Void OptimizeIndicesForCache()` | Reorders indexed triangles for vertex-cache locality without changing their winding or attributes. |
| `public System.Void SetBones(System.ReadOnlySpan<System.Int32> bones)` | Copies the current bone indices before capturing a vertex. |
| `public System.Void SetColor(Electron2D.Color color)` | Enables vertex color before the first vertex, or changes its subsequent captured value. |
| `public System.Void SetMaterial(Electron2D.Material material)` | Assigns a live borrowed surface material, including on imported geometry. |
| `public System.Void SetNormal(Electron2D.Vector3 normal)` | Assigns the normal captured by subsequent vertices. |
| `public System.Void SetSkinWeightCount(Electron2D.SurfaceTool.SkinWeightCount count)` | Selects four or eight influence slots before any vertex is captured. |
| `public System.Void SetSmoothGroup(System.Int32 index)` | Sets the group identity captured by future vertices and retained by exact indexing. |
| `public System.Void SetTangent(Electron2D.Vector4 tangent)` | Assigns the tangent captured by subsequent vertices. |
| `public System.Void SetUV(Electron2D.Vector2 uv)` | Enables primary UV before the first vertex, or changes its subsequent captured value. |
| `public System.Void SetUV2(Electron2D.Vector2 uv2)` | Enables secondary UV before the first vertex, or changes its subsequent captured value. |
| `public System.Void SetWeights(System.ReadOnlySpan<System.Single> weights)` | Copies current finite influence weights before capturing a vertex. |
| `protected override System.Void ValidateDisposal()` |  |

## Member descriptions

### SurfaceTool

`public SurfaceTool()`

summary: Creates an empty builder with line topology and four influence slots.

### AddIndex

`public System.Void AddIndex(System.Int32 index)`

summary: Appends a nonnegative index, permitting references to vertices authored later.

index: Zero-based vertex index; Commit validates its final bounds.

System.ArgumentOutOfRangeException: The index is negative.

### AddTriangleFan

`public System.Void AddTriangleFan(System.ReadOnlySpan<Electron2D.Vector2> vertices, System.ReadOnlySpan<Electron2D.Vector2> uvs = default, System.ReadOnlySpan<Electron2D.Color> colors = default, System.ReadOnlySpan<Electron2D.Vector2> uv2s = default, System.ReadOnlySpan<Electron2D.Vector3> normals = default, System.ReadOnlySpan<Electron2D.Vector4> tangents = default)`

summary: Appends sequential triangles for a fan using the current supported attributes.

vertices: Finite fan positions; fewer than three produce no triangles.

uvs: Optional finite primary coordinates, at most one per fan position.

colors: Optional finite colors, at most one per fan position.

uv2s: Optional finite secondary coordinates, at most one per fan position.

normals: Optional finite numerical normal vectors.

tangents: Optional finite tangent XYZ vectors and W orientation.

remarks: Missing entries retain the current captured attribute at that point in fan traversal. Channels must be enabled before prior vertices. Existing indexed geometry retains its indices and gains indices for the new triangles. Invalid input leaves geometry and current values unchanged.

System.InvalidOperationException: No triangle draft exists or a new channel follows prior vertices.

System.ArgumentException: Optional counts or values are invalid.

### AddVertex

`public System.Void AddVertex(Electron2D.Vector2 vertex)`

summary: Captures a finite local vertex with the current enabled attributes.

vertex: Two-dimensional local position.

System.ArgumentException: The position is nonfinite or influence counts do not match.

System.InvalidOperationException: No draft exists or only one skin channel is enabled.

### AppendFrom

`public System.Void AppendFrom(Electron2D.Mesh existing, System.Int32 surface, Electron2D.Transform transform)`

summary: Appends copied transformed geometry while preserving existing draw order and attributes.

existing: Live source mesh.

surface: Existing zero-based source surface.

transform: Finite local two-dimensional transform.

remarks: Empty geometry adopts source topology/skin width. Nonempty inputs must share topology and skin presence/width. Missing color/UV/frame channels use white/zero. Normal data uses inverse-transpose XY basis; tangent data uses the basis and reflected orientation. Singular normal-bearing append rejects. Mixed indexing expands sequential order to indices.

System.ArgumentException: Topology, skin width, indices or transformed positions are incompatible.

### Begin

`public System.Void Begin(Electron2D.Mesh.PrimitiveType primitive)`

summary: Clears prior authoring and starts a surface.

primitive: One of the five supported mesh topologies.

remarks: Retains the selected skin width. An existing draft is replaced.

System.ArgumentOutOfRangeException: The topology is invalid.

### Clear

`public System.Void Clear()`

summary: Clears geometry, attributes and borrowed material and restores four-slot line defaults.

### Commit

`public Electron2D.ArrayMesh Commit(Electron2D.ArrayMesh existing = null, Electron2D.Mesh.ArrayFormat flags = None)`

summary: Publishes one validated copied surface with its borrowed material and preserves the builder.

existing: Target mesh, or null to create a caller-owned mesh.

flags: Supported mesh policies and matching channel flags.

returns: The same target, or a newly owned mesh. Empty authoring adds no surface.

remarks: One Changed reports complete geometry/material publication. Listener errors retain the surface.

System.ArgumentException: Topology, final index bounds or explicit channels are invalid.

System.NotSupportedException: Flags require an absent storage consumer.

### CommitToArrays

`public Electron2D.MeshSurfaceData CommitToArrays()`

summary: Returns independent typed arrays for the current authoring state.

returns: Caller-owned channels; empty without vertices. Pending topology/indices may be incomplete.

### CreateFrom

`public System.Void CreateFrom(Electron2D.Mesh existing, System.Int32 surface)`

summary: Replaces geometry with a validated copied surface and its borrowed material.

existing: Live source mesh.

surface: Existing zero-based surface index.

remarks: Imported geometry can be indexed, expanded, appended and committed; Begin starts a new draft.

System.InvalidOperationException: Source callbacks mutate their mesh or the receiving builder.

### CreateFromArrays

`public System.Void CreateFromArrays(Electron2D.MeshSurfaceData arrays, Electron2D.Mesh.PrimitiveType primitiveType = Triangles)`

summary: Replaces geometry with validated copied typed arrays.

arrays: Finite supported channel arrays and complete topology.

primitiveType: Topology, triangles by default.

System.ArgumentException: The arrays, counts or topology are invalid.

### Deindex

`public System.Void Deindex()`

summary: Expands current indices into ordered complete vertex records and removes the index channel.

System.ArgumentException: An index does not address the current geometry; state is preserved.

### Dispose

`protected override System.Void Dispose(System.Boolean disposing)`

### GenerateNormals

`public System.Void GenerateNormals(System.Boolean flip = false)`

summary: Generates planar triangle normals, respecting captured smooth groups.

flip: Reverses the clockwise-front convention when true.

remarks: Flat group minus one retains each face's normal; other groups share equal positions. Indexed input expands before generation and is indexed again afterward, retaining attribute seams. Degenerate triangles contribute zero. Structural generation may allocate.

System.InvalidOperationException: The surface does not use triangle topology.

System.ArgumentException: Triangle order is incomplete or contains invalid indices.

### GenerateTangents

`public System.Void GenerateTangents()`

summary: Generates tangent directions from normal/UV channels using MikkTSpace.

remarks: Faces use successive triples of the current index or vertex order. Shared indexed vertices receive the last corresponding face-corner result; author duplicated seam vertices where needed. Generation preserves the input when validation or generation fails. Temporary native blocks are owned by the generator call and released after errors. Structural generation may allocate.

System.InvalidOperationException: Normals/primary UVs are missing or no face can be generated.

### GetAABB

`public Electron2D.Rect2 GetAABB()`

summary: Gets the local rectangle enclosing every stored vertex, including unreferenced vertices.

returns: Empty without geometry; otherwise finite nonnegative bounds.

System.ArgumentException: The extent cannot be represented by finite floats.

### GetPrimitiveType

`public Electron2D.Mesh.PrimitiveType GetPrimitiveType()`

summary: Gets the selected primitive topology, including while empty.

returns: Lines initially and after Clear.

### GetSkinWeightCount

`public Electron2D.SurfaceTool.SkinWeightCount GetSkinWeightCount()`

summary: Gets the selected influence-width mode.

returns: Four initially; imported skin selects its actual width.

### Index

`public System.Void Index()`

summary: Replaces sequential vertices with exact unique records and their original ordered indices.

remarks: Includes enabled attributes, skin values and smooth groups. Existing indices are retained unchanged.

### OptimizeIndicesForCache

`public System.Void OptimizeIndicesForCache()`

summary: Reorders indexed triangles for vertex-cache locality without changing their winding or attributes.

remarks: Uses bounded adjacency traversal and accepts a candidate only when a sixteen-entry LRU model improves. Global triangle order may change. Retained canvas drawing expands indices; native indexed device consumers can use the copied result. Actual GPU cache sizes and performance are not inferred from this model.

System.InvalidOperationException: The surface is not an indexed complete triangle list.

System.ArgumentException: An index does not address captured geometry.

### SetBones

`public System.Void SetBones(System.ReadOnlySpan<System.Int32> bones)`

summary: Copies the current bone indices before capturing a vertex.

bones: Indices zero through 65535, paired with current weights.

remarks: Short lists are padded; overfull lists retain the strongest four/eight influences.

System.ArgumentOutOfRangeException: An index is outside the stored range.

### SetColor

`public System.Void SetColor(Electron2D.Color color)`

summary: Enables vertex color before the first vertex, or changes its subsequent captured value.

color: Finite color; mesh commit applies RGBA8 storage precision.

System.ArgumentException: A component is nonfinite.

System.InvalidOperationException: No draft exists or a new channel is enabled after a vertex.

### SetMaterial

`public System.Void SetMaterial(Electron2D.Material material)`

summary: Assigns a live borrowed surface material, including on imported geometry.

material: Material or null for the drawing item's inherited material.

System.ObjectDisposedException: The material or builder is disposed.

### SetNormal

`public System.Void SetNormal(Electron2D.Vector3 normal)`

summary: Assigns the normal captured by subsequent vertices.

normal: Finite mesh-local numerical direction.

remarks: Activate this channel before capturing the first vertex.

### SetSkinWeightCount

`public System.Void SetSkinWeightCount(Electron2D.SurfaceTool.SkinWeightCount count)`

summary: Selects four or eight influence slots before any vertex is captured.

count: An enum value, not the literal slot count.

remarks: Works before Begin or before its first vertex; Begin retains this choice.

System.ArgumentOutOfRangeException: The mode is invalid.

System.InvalidOperationException: Geometry has already been captured.

### SetSmoothGroup

`public System.Void SetSmoothGroup(System.Int32 index)`

summary: Sets the group identity captured by future vertices and retained by exact indexing.

index: A 32-bit group identity; minus one denotes the flat group.

### SetTangent

`public System.Void SetTangent(Electron2D.Vector4 tangent)`

summary: Assigns the tangent captured by subsequent vertices.

tangent: Finite XYZ direction and W orientation.

remarks: Activate this channel before capturing the first vertex.

### SetUV

`public System.Void SetUV(Electron2D.Vector2 uv)`

summary: Enables primary UV before the first vertex, or changes its subsequent captured value.

uv: Finite primary coordinate.

System.ArgumentException: A component is nonfinite.

### SetUV2

`public System.Void SetUV2(Electron2D.Vector2 uv2)`

summary: Enables secondary UV before the first vertex, or changes its subsequent captured value.

uv2: Finite coordinate independent of primary sampling and instance data.

System.ArgumentException: A component is nonfinite.

### SetWeights

`public System.Void SetWeights(System.ReadOnlySpan<System.Single> weights)`

summary: Copies current finite influence weights before capturing a vertex.

weights: Values paired with current bones; commit clamps to UNORM16.

remarks: Only overfull lists normalize their retained strongest influences; shorter lists pad zero.

System.ArgumentException: A value is nonfinite.

### ValidateDisposal

`protected override System.Void ValidateDisposal()`
