# MeshDataTool

Last updated: 2026-10-12

MeshDataTool : ElectronObject edits copied two-dimensional triangle surfaces under [ADR 0092](../decisions/mesh.md#adr-0092). It borrows source/target meshes, material and runtime metadata values. Every operation rejects a disposed editor; invalid element/corner indices throw ArgumentOutOfRangeException. Individual operations serialize; callers synchronize whole edit sequences across threads.

CreateFromSurface captures all source channels, format and material under one storage gate and builds the full graph before replacing state. Only complete indexed or sequential Triangles are accepted. Source data is never modified or retained. Unique sorted endpoint pairs define edges in discovery order; each face connects its corner to the next corner. Incidence arrays preserve traversal and multiplicity for self-loops, repeated/degenerate faces and nonmanifold edges. Unreferenced vertices are retained. Relation and channel-array getters return independent copies.

Vertex edits preserve connectivity; GetFaceNormal calculates clockwise-front numeric unit Z/zero from current XY winding with widened arithmetic. Missing color/UV defaults are white/zero. New channels initialize all untouched vertices to those actual drawing defaults. Four/eight bone/weight width follows imported storage; missing skin uses four unless the source explicitly selects eight. Each channel activates independently, so incomplete pairs cannot commit. Current finite values remain authored until target RGBA8/UNORM16 precision. Unknown compression/advanced flags reject.

CommitToSurface appends a copied explicit indexed surface and its borrowed material together before one Changed; it does not replace an existing surface or clear this editor. Invalid data preserves target storage and observer errors preserve complete publication. Clear releases topology/material/annotations without disposing borrowed values. Source/target resource RID and native bindings remain outside this helper.

Generic metadata methods retain exact stored types. Get throws KeyNotFoundException for missing/wrong type; TryGet distinguishes missing metadata from a stored null. Same-type replacement reuses its holder. Runtime annotations are reset on import/Clear/disposal and are omitted from committed mesh archives. They are not a public object store or universal engine value. Vertex normal/tangent readers and writers execute through the shared typed storage/shader consumer.

Structural graph import, first channel/type activation, copied relation/channel queries, commit and files may allocate. Scalar edits/queries, same-type metadata replacement and prepared committed playback reuse storage. [MeshDataToolTests](../../tests/Electron2D.Tests/MeshDataToolTests.cs) checks exact topology/order, degenerate/nonmanifold data, finite/width boundaries, rollback, copies, metadata, observer failures, fresh-process edited output and warmed operation scopes. [MeshDataToolNativeTests](../../tests/Electron2D.Tests/MeshDataToolNativeTests.cs) checks real indexed GLSL/HLSL edited positions/colors and retained UV2/eighth influence/instance pixels on GPU and compatibility. Devices measure 64 replay/readback frames; retained hosts measure 64 active and 64 idle frames after warmup with zero managed bytes on the owner thread. Wider platforms, native allocator totals, broad performance and owner acceptance remain unverified.


## Complete API

| Complete signature | Contract |
| --- | --- |
| `public MeshDataTool()` | Creates an empty editor with no topology or material. |
| `public System.Void Clear()` | Clears copied geometry, topology, metadata and the borrowed material. |
| `public System.Void CommitToSurface(Electron2D.ArrayMesh mesh, Electron2D.Mesh.ArrayFormat compressionFlags = None)` | Appends a validated copied triangle surface with the current borrowed material. |
| `public System.Void CreateFromSurface(Electron2D.ArrayMesh mesh, System.Int32 surface)` | Copies and builds connectivity for an existing complete triangle surface. |
| `protected override System.Void Dispose(System.Boolean disposing)` |  |
| `public System.Int32 GetEdgeCount()` | Gets the number of unique sorted vertex-pair edges. |
| `public System.Int32[] GetEdgeFaces(System.Int32 index)` | Gets copied incident face IDs for a boundary, shared, nonmanifold or repeated edge. |
| `public T GetEdgeMeta<T>(System.Int32 index)` | Gets edge metadata using its exact stored type. |
| `public System.Int32 GetEdgeVertex(System.Int32 index, System.Int32 vertex)` | Gets one of an edge's sorted vertex endpoints. |
| `public System.Int32 GetFaceCount()` | Gets the number of ordered triangle faces. |
| `public System.Int32 GetFaceEdge(System.Int32 index, System.Int32 edge)` | Gets the edge between a face corner and its next corner. |
| `public T GetFaceMeta<T>(System.Int32 index)` | Gets face metadata using its exact stored type. |
| `public Electron2D.Vector3 GetFaceNormal(System.Int32 index)` | Computes a face's current numeric winding normal in the XY plane. |
| `public System.Int32 GetFaceVertex(System.Int32 index, System.Int32 vertex)` | Gets a face's vertex ID in authored winding order. |
| `public Electron2D.Mesh.ArrayFormat GetFormat()` | Gets the copied channel/policy mask, including newly enabled channels. |
| `public Electron2D.Material GetMaterial()` | Gets the current borrowed surface material. |
| `public Electron2D.Vector2 GetVertex(System.Int32 index)` | Gets a copied local vertex position. |
| `public System.Int32[] GetVertexBones(System.Int32 index)` | Gets an independent four/eight influence index array, empty without the channel. |
| `public Electron2D.Color GetVertexColor(System.Int32 index)` | Gets the authored color or white when no color channel exists. |
| `public System.Int32 GetVertexCount()` | Gets the number of copied vertices, including unreferenced vertices. |
| `public System.Int32[] GetVertexEdges(System.Int32 index)` | Gets independently copied incident edge IDs in discovery order. |
| `public System.Int32[] GetVertexFaces(System.Int32 index)` | Gets independently copied incident face IDs in corner traversal order. |
| `public T GetVertexMeta<T>(System.Int32 index)` | Gets vertex metadata using its exact stored type. |
| `public Electron2D.Vector3 GetVertexNormal(System.Int32 index)` | Returns the copied mesh-local normal value. |
| `public Electron2D.Vector4 GetVertexTangent(System.Int32 index)` | Returns the copied tangent direction and orientation. |
| `public Electron2D.Vector2 GetVertexUV(System.Int32 index)` | Gets the primary UV or zero if that channel is absent. |
| `public Electron2D.Vector2 GetVertexUV2(System.Int32 index)` | Gets secondary UV or zero if that channel is absent. |
| `public System.Single[] GetVertexWeights(System.Int32 index)` | Gets independent current four/eight weights, empty without the channel. |
| `public System.Void SetEdgeMeta<T>(System.Int32 index, T value)` | Stores a runtime-only exactly typed edge annotation. |
| `public System.Void SetFaceMeta<T>(System.Int32 index, T value)` | Stores a runtime-only exactly typed face annotation. |
| `public System.Void SetMaterial(Electron2D.Material material)` | Assigns a borrowed material without modifying source or target meshes. |
| `public System.Void SetVertex(System.Int32 index, Electron2D.Vector2 vertex)` | Changes a finite local vertex position without altering connectivity. |
| `public System.Void SetVertexBones(System.Int32 index, System.ReadOnlySpan<System.Int32> bones)` | Changes exactly four/eight stored bone indices for one vertex. |
| `public System.Void SetVertexColor(System.Int32 index, Electron2D.Color color)` | Changes a finite color, preserving white on other vertices when enabling the channel. |
| `public System.Void SetVertexMeta<T>(System.Int32 index, T value)` | Stores a runtime-only exactly typed vertex annotation. |
| `public System.Void SetVertexNormal(System.Int32 index, Electron2D.Vector3 normal)` | Assigns a finite mesh-local numerical normal vector. |
| `public System.Void SetVertexTangent(System.Int32 index, Electron2D.Vector4 tangent)` | Assigns a finite tangent direction and orientation. |
| `public System.Void SetVertexUV(System.Int32 index, Electron2D.Vector2 uv)` | Changes primary UV, enabling zero values on other vertices if needed. |
| `public System.Void SetVertexUV2(System.Int32 index, Electron2D.Vector2 uv2)` | Changes secondary UV, enabling zero values on other vertices if needed. |
| `public System.Void SetVertexWeights(System.Int32 index, System.ReadOnlySpan<System.Single> weights)` | Changes exactly four/eight finite weights for one vertex without normalizing their sum. |
| `public System.Boolean TryGetEdgeMeta<T>(System.Int32 index, out T value)` | Tries to read an edge's exactly typed runtime annotation. |
| `public System.Boolean TryGetFaceMeta<T>(System.Int32 index, out T value)` | Tries to read a face's exactly typed runtime annotation. |
| `public System.Boolean TryGetVertexMeta<T>(System.Int32 index, out T value)` | Tries to read vertex metadata, distinguishing absent data from an explicitly stored null. |

## Member descriptions

### MeshDataTool

`public MeshDataTool()`

summary: Creates an empty editor with no topology or material.

### Clear

`public System.Void Clear()`

summary: Clears copied geometry, topology, metadata and the borrowed material.

### CommitToSurface

`public System.Void CommitToSurface(Electron2D.ArrayMesh mesh, Electron2D.Mesh.ArrayFormat compressionFlags = None)`

summary: Appends a validated copied triangle surface with the current borrowed material.

mesh: Live target mesh.

compressionFlags: Supported storage policies and matching channel flags.

remarks: Current face order is committed as explicit indices. Source/editor state is preserved. Geometry and material publish together before one Changed; observer failure retains the committed surface.

System.ArgumentException: Skin channels are incomplete or data is invalid.

System.InvalidOperationException: The editor is empty.

System.NotSupportedException: Flags require an absent encoder or attribute consumer.

### CreateFromSurface

`public System.Void CreateFromSurface(Electron2D.ArrayMesh mesh, System.Int32 surface)`

summary: Copies and builds connectivity for an existing complete triangle surface.

mesh: Live source mesh; it is neither modified nor retained.

surface: Existing zero-based triangle surface index.

remarks: Indexed and sequential triangles are accepted; source indices become explicit face order. All vertex channels and material are captured under one source storage gate. Failure preserves prior state.

System.ArgumentException: The topology is not triangles or the stored data is invalid.

System.ArgumentOutOfRangeException: The surface index is invalid.

### Dispose

`protected override System.Void Dispose(System.Boolean disposing)`

### GetEdgeCount

`public System.Int32 GetEdgeCount()`

summary: Gets the number of unique sorted vertex-pair edges.

returns: A nonnegative count; self-loop edges are retained.

### GetEdgeFaces

`public System.Int32[] GetEdgeFaces(System.Int32 index)`

summary: Gets copied incident face IDs for a boundary, shared, nonmanifold or repeated edge.

index: Existing edge index.

returns: Caller-owned IDs in face/corner discovery order.

### GetEdgeMeta

`public T GetEdgeMeta<T>(System.Int32 index)`

summary: Gets edge metadata using its exact stored type.

T: The exact stored type.

index: Existing edge index.

returns: The stored value.

System.Collections.Generic.KeyNotFoundException: Metadata is absent or has another type.

### GetEdgeVertex

`public System.Int32 GetEdgeVertex(System.Int32 index, System.Int32 vertex)`

summary: Gets one of an edge's sorted vertex endpoints.

index: Existing edge index.

vertex: Zero or one.

returns: The vertex ID; endpoints may be equal for a self-loop.

### GetFaceCount

`public System.Int32 GetFaceCount()`

summary: Gets the number of ordered triangle faces.

returns: A nonnegative count, including degenerate/repeated faces.

### GetFaceEdge

`public System.Int32 GetFaceEdge(System.Int32 index, System.Int32 edge)`

summary: Gets the edge between a face corner and its next corner.

index: Existing face index.

edge: Corner zero through two.

returns: The edge ID in authored corner order.

### GetFaceMeta

`public T GetFaceMeta<T>(System.Int32 index)`

summary: Gets face metadata using its exact stored type.

T: The exact stored type.

index: Existing face index.

returns: The stored value.

System.Collections.Generic.KeyNotFoundException: Metadata is absent or has another type.

### GetFaceNormal

`public Electron2D.Vector3 GetFaceNormal(System.Int32 index)`

summary: Computes a face's current numeric winding normal in the XY plane.

index: Existing face index.

returns: Positive/negative unit Z for counterclockwise/clockwise XY order, zero for a degenerate face.

remarks: Uses widened differences/products and actual edited positions; this does not enable a vertex normal channel.

### GetFaceVertex

`public System.Int32 GetFaceVertex(System.Int32 index, System.Int32 vertex)`

summary: Gets a face's vertex ID in authored winding order.

index: Existing face index.

vertex: Corner zero through two.

returns: The ordered vertex ID.

### GetFormat

`public Electron2D.Mesh.ArrayFormat GetFormat()`

summary: Gets the copied channel/policy mask, including newly enabled channels.

returns: Zero after construction/Clear; indices may be implicit in the original import mask.

### GetMaterial

`public Electron2D.Material GetMaterial()`

summary: Gets the current borrowed surface material.

returns: Material, or null for the drawing item's material.

### GetVertex

`public Electron2D.Vector2 GetVertex(System.Int32 index)`

summary: Gets a copied local vertex position.

index: Existing vertex index.

returns: Finite two-dimensional position.

### GetVertexBones

`public System.Int32[] GetVertexBones(System.Int32 index)`

summary: Gets an independent four/eight influence index array, empty without the channel.

index: Existing vertex index.

returns: Caller-owned indices.

### GetVertexColor

`public Electron2D.Color GetVertexColor(System.Int32 index)`

summary: Gets the authored color or white when no color channel exists.

index: Existing vertex index.

returns: The current finite color, before target mesh quantization.

### GetVertexCount

`public System.Int32 GetVertexCount()`

summary: Gets the number of copied vertices, including unreferenced vertices.

returns: A nonnegative count.

### GetVertexEdges

`public System.Int32[] GetVertexEdges(System.Int32 index)`

summary: Gets independently copied incident edge IDs in discovery order.

index: Existing vertex index.

returns: Caller-owned IDs; a self-loop contributes its two endpoint incidences.

### GetVertexFaces

`public System.Int32[] GetVertexFaces(System.Int32 index)`

summary: Gets independently copied incident face IDs in corner traversal order.

index: Existing vertex index.

returns: Caller-owned IDs; repeated triangle corners retain multiplicity.

### GetVertexMeta

`public T GetVertexMeta<T>(System.Int32 index)`

summary: Gets vertex metadata using its exact stored type.

T: The exact stored type.

index: Existing vertex index.

returns: Stored value, including null if explicitly stored.

System.Collections.Generic.KeyNotFoundException: Metadata is absent or has another type.

### GetVertexNormal

`public Electron2D.Vector3 GetVertexNormal(System.Int32 index)`

summary: Returns the copied mesh-local normal value.

index: Existing vertex index.

returns: The normal, or zero without the channel.

### GetVertexTangent

`public Electron2D.Vector4 GetVertexTangent(System.Int32 index)`

summary: Returns the copied tangent direction and orientation.

index: Existing vertex index.

returns: XYZ direction and W orientation, or zero without the channel.

### GetVertexUV

`public Electron2D.Vector2 GetVertexUV(System.Int32 index)`

summary: Gets the primary UV or zero if that channel is absent.

index: Existing vertex index.

returns: Finite primary coordinate.

### GetVertexUV2

`public Electron2D.Vector2 GetVertexUV2(System.Int32 index)`

summary: Gets secondary UV or zero if that channel is absent.

index: Existing vertex index.

returns: Finite secondary coordinate.

### GetVertexWeights

`public System.Single[] GetVertexWeights(System.Int32 index)`

summary: Gets independent current four/eight weights, empty without the channel.

index: Existing vertex index.

returns: Caller-owned finite weights, before target UNORM16 packing.

### SetEdgeMeta

`public System.Void SetEdgeMeta<T>(System.Int32 index, T value)`

summary: Stores a runtime-only exactly typed edge annotation.

T: The exact type used again when reading.

index: Existing edge index.

value: Borrowed value, not copied or disposed.

### SetFaceMeta

`public System.Void SetFaceMeta<T>(System.Int32 index, T value)`

summary: Stores a runtime-only exactly typed face annotation.

T: The exact type used again when reading.

index: Existing face index.

value: Borrowed value, not copied or disposed.

### SetMaterial

`public System.Void SetMaterial(Electron2D.Material material)`

summary: Assigns a borrowed material without modifying source or target meshes.

material: Live material, or null for inherited drawing.

### SetVertex

`public System.Void SetVertex(System.Int32 index, Electron2D.Vector2 vertex)`

summary: Changes a finite local vertex position without altering connectivity.

index: Existing vertex index.

vertex: Finite local position.

System.ArgumentException: A component is nonfinite.

### SetVertexBones

`public System.Void SetVertexBones(System.Int32 index, System.ReadOnlySpan<System.Int32> bones)`

summary: Changes exactly four/eight stored bone indices for one vertex.

index: Existing vertex index.

bones: The imported slot count, or four for an unskinned surface; indices zero through 65535.

remarks: Enabling one skin channel leaves the other absent until authored. Commit rejects incomplete pairs.

### SetVertexColor

`public System.Void SetVertexColor(System.Int32 index, Electron2D.Color color)`

summary: Changes a finite color, preserving white on other vertices when enabling the channel.

index: Existing vertex index.

color: Finite color; target commit applies RGBA8 precision.

### SetVertexMeta

`public System.Void SetVertexMeta<T>(System.Int32 index, T value)`

summary: Stores a runtime-only exactly typed vertex annotation.

T: The type used again when reading; same-type replacements reuse their holder.

index: Existing vertex index.

value: Borrowed value, including an explicitly stored null.

### SetVertexNormal

`public System.Void SetVertexNormal(System.Int32 index, Electron2D.Vector3 normal)`

summary: Assigns a finite mesh-local numerical normal vector.

index: Existing vertex index.

normal: Finite direction; target commit quantizes it.

### SetVertexTangent

`public System.Void SetVertexTangent(System.Int32 index, Electron2D.Vector4 tangent)`

summary: Assigns a finite tangent direction and orientation.

index: Existing vertex index.

tangent: Finite XYZ direction and W orientation; target commit quantizes them.

### SetVertexUV

`public System.Void SetVertexUV(System.Int32 index, Electron2D.Vector2 uv)`

summary: Changes primary UV, enabling zero values on other vertices if needed.

index: Existing vertex index.

uv: Finite coordinate.

### SetVertexUV2

`public System.Void SetVertexUV2(System.Int32 index, Electron2D.Vector2 uv2)`

summary: Changes secondary UV, enabling zero values on other vertices if needed.

index: Existing vertex index.

uv2: Finite coordinate.

### SetVertexWeights

`public System.Void SetVertexWeights(System.Int32 index, System.ReadOnlySpan<System.Single> weights)`

summary: Changes exactly four/eight finite weights for one vertex without normalizing their sum.

index: Existing vertex index.

weights: The imported slot count, or four for an unskinned surface.

remarks: Commit clamps/packs to UNORM16 and requires matching bones on all vertices.

### TryGetEdgeMeta

`public System.Boolean TryGetEdgeMeta<T>(System.Int32 index, out T value)`

summary: Tries to read an edge's exactly typed runtime annotation.

T: The exact stored type.

index: Existing edge index.

value: Stored value, or default on failure.

returns: Whether this type was stored, including a null value.

### TryGetFaceMeta

`public System.Boolean TryGetFaceMeta<T>(System.Int32 index, out T value)`

summary: Tries to read a face's exactly typed runtime annotation.

T: The exact stored type.

index: Existing face index.

value: Stored value, or default on failure.

returns: Whether this type was stored, including a null value.

### TryGetVertexMeta

`public System.Boolean TryGetVertexMeta<T>(System.Int32 index, out T value)`

summary: Tries to read vertex metadata, distinguishing absent data from an explicitly stored null.

T: The exact stored type.

index: Existing vertex index.

value: Stored value, or default on failure.

returns: Whether an annotation of this exact type is stored.


Compressed ArrayMesh surfaces import decoded numerical channels and retain their format mask. CommitToSurface accepts CompressAttributes and applies the documented quantization to edited values through the shared [compressed attribute layout](../components/meshes.md#compressed-attributes).
