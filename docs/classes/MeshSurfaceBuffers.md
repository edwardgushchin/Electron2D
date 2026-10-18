# MeshSurfaceBuffers

Last updated: 2026-10-18

- Source: [MeshSurfaceBuffers.cs](../../src/Scene/Resources/MeshSurfaceBuffers.cs)
- Declaration: public sealed class MeshSurfaceBuffers
- Component: [Packed mesh surfaces](../components/meshes.md#packed-server-surfaces)

## Description

Typed packed surface input/output for RenderingServer.MeshCreateFromSurfaces, MeshAddSurface and MeshGetSurface. Decoded MeshSurfaceData remains the array authoring role. All arrays are copied on import/export; Material stays borrowed. Coordinate concurrent mutations of descriptions and arrays across an import call. This value is runtime authoring data, with no native handle or Resource identity.

## Complete API

| Signature | Contract |
| --- | --- |
| `public MeshSurfaceBuffers()` | Empty description with triangle topology and planar position flags. |
| `public Mesh.PrimitiveType Primitive { get; set; }` | All five existing topologies; complete primitive records required. |
| `public Mesh.ArrayFormat Format { get; set; }` | Exact supported channels and policies; requires explicit planar positions. |
| `public byte[] VertexData { get; set; }` | VertexCount contiguous float32 XY pairs, followed by interleaved packed normal/tangent records. |
| `public int VertexCount { get; set; }` | Positive count on import. |
| `public byte[] AttributeData { get; set; }` | One interleaved color, UV, UV2, custom0–3 record per vertex. |
| `public byte[] SkinData { get; set; }` | Per vertex: four/eight uint16 bone indices, then four/eight UNORM16 weights. Both channels required. |
| `public byte[] IndexData { get; set; }` | UInt16 through 65536 vertices, UInt32 thereafter; each index must resolve. |
| `public int IndexCount { get; set; }` | Zero without the index flag, positive with it. |
| `public Rect2 AABB { get; set; }` | Finite nonnegative authored visibility rectangle with finite end. |
| `public Vector4 UVScale { get; set; }` | Finite nonnegative XY/ZW compressed UV scales; zero selects direct UNORM16. |
| `public IReadOnlyDictionary<float, byte[]> LODs { get; set; }` | Positive finite local errors mapped to shorter complete index buffers of the base index width. |
| `public Rect2?[] BoneAABBs { get; set; }` | Optional finite mesh-local per-bone rectangles; null means unused. Attached palettes transform them for canvas bounds. |
| `public Mesh.ArrayFormat BlendShapeFormat { get; set; }` | None selects base position/direction channels. An explicit Vertex/Normal/Tangent mask preserves an independent existing target schema. Export uses None for the full base schema. |
| `public byte[] BlendShapeData { get; set; }` | Concatenated position/direction blocks in mesh shape-index order, using the effective shape schema. |
| `public Material? Material { get; set; }` | Borrowed material; null inherits the drawing material. |

All buffers are little-endian. Ordinary positions use eight bytes per vertex; direction words use four bytes per selected direction. CompressAttributes retains float32 XY, uses six-byte joint direction frames and four-byte UNORM16 UV pairs. Four custom channels retain their existing independent eight-format layouts. Compressed base normals imply tangents. Shape masks may omit tangents retained in the base after generation; no target channel is fabricated. Actual buffer lengths must match the complete counts/schema before allocation/publication.

Imported bounds remain authored after position/skin buffer updates; callers maintain their coverage. Ordinary resource exports derive bounds and nullable bone rectangles from current geometry. These descriptions and server-owned RIDs are transient; material/Resource graph serialization is separate. A material-free description can be transported by a caller-owned typed JSON representation, as exercised in a fresh process; this does not register a resource-file schema or ConfigFile engine-type conversion.

## Example

Partial consumer inside an active Engine.Run host with an existing ArrayMesh `source` and canvas item `item`:

```csharp
MeshSurfaceBuffers surface = RenderingServer.MeshGetSurface(source.GetRID(), 0);
RID copy = RenderingServer.MeshCreateFromSurfaces([surface], source.GetBlendShapeCount());
RenderingServer.MeshSetBlendShapeMode(copy, source.BlendShapeMode);
RenderingServer.CanvasItemAddMesh(item.GetCanvasItem(), copy);
// Clear/remove commands before freeing copy; borrowed source remains caller-owned.
```

## Verification

MeshPackedTests exercises all topologies/eight custom formats, ordinary/compressed packing, both skin widths, both index-width boundaries, two shapes/LODs, independent shape schemas after tangent generation, both live morph modes, copies, bounds, invalid records and fresh material-free JSON authoring. MeshPackedNativeTests uses the public static family on real Linux Wayland GPU/GLSL, GPU/HLSL and Compatibility hosts; it verifies channel/material/LOD/skin pixels, palette-aware bounds, shape counts, atomic failure, custom callback recovery and 64 active/64 idle prepared owner frames with zero managed bytes. Software-driver evidence does not establish target hardware, foreign execution, native allocator totals or owner acceptance. Procedural surfaces without positions remain a separate shader producer.
