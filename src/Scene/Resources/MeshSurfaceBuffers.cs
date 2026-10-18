namespace Electron2D;

/// <summary>Describes one packed planar mesh surface for RenderingServer import and export.</summary>
/// <remarks>Buffers use the engine's little-endian channel layout and are copied on import. Exported arrays
/// belong to the caller; Material remains borrowed. This is runtime surface data, independent of native handles.</remarks>
public sealed class MeshSurfaceBuffers
{
    /// <summary>Creates an empty packed surface description.</summary>
    public MeshSurfaceBuffers() { }
    /// <summary>Gets or sets primitive topology.</summary>
    /// <value>Triangles initially.</value>
    public Mesh.PrimitiveType Primitive { get; set; } = Mesh.PrimitiveType.Triangles;
    /// <summary>Gets or sets the exact channel and packing flags.</summary>
    /// <value>Planar positions initially. UseEmptyVertexArray permits a missing position block.</value>
    public Mesh.ArrayFormat Format { get; set; } = Mesh.ArrayFormat.Vertex | Mesh.ArrayFormat.Use2DVertices;
    /// <summary>Gets or sets the position block followed by interleaved normal/tangent records.</summary>
    /// <value>Empty initially; requires VertexCount times the combined vertex and direction strides.</value>
    public byte[] VertexData { get; set; } = [];
    /// <summary>Gets or sets the number of vertex records.</summary>
    /// <value>Zero initially; positive counts size all selected channels. Zero is valid for uint16 index-only
    /// procedural geometry, with empty channel buffers and a nonempty index buffer.</value>
    public int VertexCount { get; set; }
    /// <summary>Gets or sets interleaved color, primary/secondary UV and custom records.</summary>
    /// <value>Empty without attribute channels; otherwise exactly one format-sized record per vertex.</value>
    public byte[] AttributeData { get; set; } = [];
    /// <summary>Gets or sets four- or eight-slot uint16 bone indices and UNORM16 weights per vertex.</summary>
    /// <value>Empty without skin; skin requires both channels.</value>
    public byte[] SkinData { get; set; } = [];
    /// <summary>Gets or sets packed unsigned vertex indices.</summary>
    /// <value>Empty without indices; two bytes per index through 65536 vertices, otherwise four.</value>
    public byte[] IndexData { get; set; } = [];
    /// <summary>Gets or sets the number of index records.</summary>
    /// <value>Zero for sequential topology.</value>
    public int IndexCount { get; set; }
    /// <summary>Gets or sets the authored local visibility rectangle.</summary>
    /// <value>Empty initially; must be finite with nonnegative size and finite end.</value>
    public Rect2 AABB { get; set; }
    /// <summary>Gets or sets the primary XY and secondary ZW compressed UV scales.</summary>
    /// <value>Zero selects direct UNORM16 coordinates; otherwise each component is finite and nonnegative.</value>
    public Vector4 UVScale { get; set; }
    /// <summary>Gets or sets shorter packed index buffers by positive finite local error threshold.</summary>
    /// <value>Empty initially; buffers use the base index width and import in increasing threshold order.</value>
    public IReadOnlyDictionary<float, byte[]> LODs { get; set; } = new Dictionary<float, byte[]>();
    /// <summary>Gets or sets mesh-local per-bone visibility rectangles.</summary>
    /// <value>Empty without authored bone bounds; null entries identify unused bones.</value>
    /// <remarks>Attached palettes transform these rectangles for canvas visibility. Without a palette the surface AABB applies.</remarks>
    public Rect2?[] BoneAABBs { get; set; } = [];
    /// <summary>Gets or sets the optional independent shape position/direction schema.</summary>
    /// <value>None uses the base position/direction channels. An explicit mask retains the base Vertex bit
    /// and may select present Normal/Tangent channels. Exports use None for the complete base schema.</value>
    /// <remarks>Preserves existing shape payloads when base tangents were generated after shape authoring.</remarks>
    public Mesh.ArrayFormat BlendShapeFormat { get; set; }
    /// <summary>Gets or sets concatenated shape position/direction blocks.</summary>
    /// <value>One block per mesh blend shape using BlendShapeFormat; no attribute, skin or index records.</value>
    public byte[] BlendShapeData { get; set; } = [];
    /// <summary>Gets or sets the borrowed surface material.</summary>
    /// <value>Null inherits the drawing item's material.</value>
    public Material? Material { get; set; }
}
