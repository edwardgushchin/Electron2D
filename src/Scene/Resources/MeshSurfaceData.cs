namespace Electron2D;

/// <summary>Supplies typed vertex, numerical normal/tangent, color, texture-coordinate, packed custom and index arrays for a two-dimensional mesh surface.</summary>
/// <remarks>Arrays are borrowed while passing this value to a mesh. Adding a surface copies every array;
/// query results own independent copies. Missing colors are white and missing texture coordinates are zero.</remarks>
public sealed class MeshSurfaceData
{
    /// <summary>Creates an empty set of typed surface arrays.</summary>
    public MeshSurfaceData() { }
    /// <summary>Gets or sets local two-dimensional vertex positions.</summary>
    /// <value>Empty initially; vertices must be finite when consumed.</value>
    public Vector2[] Vertices { get; set; } = [];
    /// <summary>Gets or sets optional mesh-local numerical normal vectors.</summary>
    /// <value>Empty or one finite Vector3 per vertex; consumed surfaces pack directions to octahedral UNORM16 or a compressed joint axis/angle frame.</value>
    public Vector3[] Normals { get; set; } = [];
    /// <summary>Gets or sets optional mesh-local tangent vectors and handedness.</summary>
    /// <value>Empty or one finite Vector4 per vertex; XYZ is the direction and W selects negative or positive orientation.</value>
    public Vector4[] Tangents { get; set; } = [];
    /// <summary>Gets or sets optional vertex colors.</summary>
    /// <value>Empty or one finite color per vertex.</value>
    public Color[] Colors { get; set; } = [];
    /// <summary>Gets or sets optional primary texture coordinates.</summary>
    /// <value>Empty or one finite normalized-space coordinate per vertex; values outside zero through one retain sampler behavior.</value>
    public Vector2[] UVs { get; set; } = [];
    /// <summary>Gets or sets optional secondary texture coordinates.</summary>
    /// <value>Empty or one finite coordinate per vertex; independent of primary UVs and instance shader data.</value>
    public Vector2[] UV2s { get; set; } = [];
    /// <summary>Gets or sets optional indices defining primitive vertex order.</summary>
    /// <value>Empty initially; each index must address Vertices when consumed.</value>
    public int[] Indices { get; set; } = [];

    /// <summary>Gets or sets optional flattened four- or eight-slot bone indices per vertex.</summary>
    /// <value>Empty without skin; every stored index is zero through 65535 and must resolve when drawn with a palette.</value>
    public int[] Bones { get; set; } = [];
    /// <summary>Gets or sets flattened skin weights matching Bones.</summary>
    /// <value>Finite values; imports clamp and truncate to unsigned normalized 16-bit weights without normalizing the sum.</value>
    public float[] Weights { get; set; } = [];
    /// <summary>Gets or sets the optional first packed custom channel.</summary>
    /// <value>Null without the channel; one finite packed record per vertex when present.</value>
    public MeshCustomData? Custom0 { get; set; }
    /// <summary>Gets or sets the optional second packed custom channel.</summary>
    /// <value>Null without the channel; independent of Custom0.</value>
    public MeshCustomData? Custom1 { get; set; }
    /// <summary>Gets or sets the optional third packed custom channel.</summary>
    /// <value>Null without the channel; independent of other custom channels.</value>
    public MeshCustomData? Custom2 { get; set; }
    /// <summary>Gets or sets the optional fourth packed custom channel.</summary>
    /// <value>Null without the channel; independent of instance shader data.</value>
    public MeshCustomData? Custom3 { get; set; }
    internal MeshCustomData? Custom(int channel) => channel switch { 0 => Custom0, 1 => Custom1, 2 => Custom2, 3 => Custom3, _ => throw new ArgumentOutOfRangeException(nameof(channel)) };
    internal void SetCustom(int channel, MeshCustomData? data) { switch (channel) { case 0: Custom0 = data; break; case 1: Custom1 = data; break; case 2: Custom2 = data; break; case 3: Custom3 = data; break; default: throw new ArgumentOutOfRangeException(nameof(channel)); } }
    internal void PrepareCustoms() { for (var i = 0; i < 4; i++) Custom(i)?.Prepare(Vertices.Length); }
    internal int SkinSlots => Bones.Length == 0 ? 0 : Bones.Length / Vertices.Length;
    internal void QuantizeSkin() { for (var i = 0; i < Weights.Length; i++) Weights[i] = (ushort)Math.Clamp(Weights[i] * 65535f, 0, 65535) / 65535f; }
    internal MeshSurfaceData Copy()
    {
        ArgumentNullException.ThrowIfNull(Vertices); ArgumentNullException.ThrowIfNull(Colors); ArgumentNullException.ThrowIfNull(Normals); ArgumentNullException.ThrowIfNull(Tangents);
        ArgumentNullException.ThrowIfNull(UVs); ArgumentNullException.ThrowIfNull(UV2s); ArgumentNullException.ThrowIfNull(Indices); ArgumentNullException.ThrowIfNull(Bones); ArgumentNullException.ThrowIfNull(Weights);
        return new() { Vertices = (Vector2[])Vertices.Clone(), Colors = (Color[])Colors.Clone(), Normals = (Vector3[])Normals.Clone(), Tangents = (Vector4[])Tangents.Clone(), UVs = (Vector2[])UVs.Clone(), UV2s = (Vector2[])UV2s.Clone(), Indices = (int[])Indices.Clone(), Bones = (int[])Bones.Clone(), Weights = (float[])Weights.Clone(), Custom0 = Custom0?.Copy(), Custom1 = Custom1?.Copy(), Custom2 = Custom2?.Copy(), Custom3 = Custom3?.Copy() };
    }
    internal void Validate(Mesh.PrimitiveType primitive)
    {
        ArgumentNullException.ThrowIfNull(Vertices); ArgumentNullException.ThrowIfNull(Colors); ArgumentNullException.ThrowIfNull(Normals); ArgumentNullException.ThrowIfNull(Tangents);
        ArgumentNullException.ThrowIfNull(UVs); ArgumentNullException.ThrowIfNull(UV2s); ArgumentNullException.ThrowIfNull(Indices); ArgumentNullException.ThrowIfNull(Bones); ArgumentNullException.ThrowIfNull(Weights);
        if (primitive is < Mesh.PrimitiveType.Points or > Mesh.PrimitiveType.TriangleStrip) throw new ArgumentOutOfRangeException(nameof(primitive));
        if (Vertices.Length == 0) throw new ArgumentException("A surface requires vertex positions.");
        if (Normals.Length != 0 && Normals.Length != Vertices.Length || Tangents.Length != 0 && Tangents.Length != Vertices.Length || Colors.Length != 0 && Colors.Length != Vertices.Length || UVs.Length != 0 && UVs.Length != Vertices.Length || UV2s.Length != 0 && UV2s.Length != Vertices.Length) throw new ArgumentException("Attribute counts must match the vertex count.");
        if (Bones.Length != Weights.Length || Bones.Length != 0 && (Bones.LongLength != Vertices.LongLength * 4 && Bones.LongLength != Vertices.LongLength * 8)) throw new ArgumentException("Skin channels require matching four or eight slots per vertex.");
        for (var channel = 0; channel < 4; channel++) Custom(channel)?.Validate(Vertices.Length);
        foreach (var bone in Bones) if ((uint)bone > 65535) throw new ArgumentOutOfRangeException(nameof(Bones));
        foreach (var weight in Weights) if (!float.IsFinite(weight)) throw new ArgumentException("Skin weights must be finite.");
        foreach (var vertex in Vertices) if (!vertex.IsFinite()) throw new ArgumentException("Mesh positions must be finite.");
        foreach (var normal in Normals) if (!normal.IsFinite()) throw new ArgumentException("Mesh normals must be finite.");
        foreach (var tangent in Tangents) if (!tangent.IsFinite()) throw new ArgumentException("Mesh tangents must be finite.");
        foreach (var color in Colors) if (!color.IsFinite()) throw new ArgumentException("Mesh colors must be finite.");
        foreach (var uv in UVs) if (!uv.IsFinite()) throw new ArgumentException("Mesh coordinates must be finite.");
        foreach (var uv in UV2s) if (!uv.IsFinite()) throw new ArgumentException("Secondary mesh coordinates must be finite.");
        foreach (var index in Indices) if ((uint)index >= (uint)Vertices.Length) throw new ArgumentOutOfRangeException(nameof(Indices));
        var count = Indices.Length == 0 ? Vertices.Length : Indices.Length;
        if (primitive == Mesh.PrimitiveType.Lines && count % 2 != 0 || primitive == Mesh.PrimitiveType.Triangles && count % 3 != 0 || primitive == Mesh.PrimitiveType.LineStrip && count < 2 || primitive == Mesh.PrimitiveType.TriangleStrip && count < 3)
            throw new ArgumentException("The vertex order does not form complete primitives.");
    }
}
