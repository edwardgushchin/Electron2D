namespace Electron2D;

public sealed partial class ArrayMesh
{
    private readonly List<string> _blendShapeNames = [];
    private BlendShapeMode _blendShapeMode = BlendShapeMode.Relative;
    /// <summary>Gets or selects normalized absolute targets or additive relative offsets.</summary>
    /// <value>Relative initially; changing the mode preserves stored payloads and updates drawing.</value>
    public BlendShapeMode BlendShapeMode
    {
        get { lock (Gate) { ThrowIfDisposed(); return _blendShapeMode; } }
        set { if (value is not (BlendShapeMode.Normalized or BlendShapeMode.Relative)) throw new ArgumentOutOfRangeException(nameof(value)); lock (Gate) { ThrowIfDisposed(); _blendShapeMode = value; } EmitChanged(); }
    }
    /// <summary>Adds a named shape definition before any surfaces exist.</summary>
    /// <param name="name">Non-null name; collisions gain the first free ordinal suffix starting at two.</param>
    /// <remarks>All subsequently added surfaces require one shape payload per definition.</remarks>
    public void AddBlendShape(string name)
    {
        ArgumentNullException.ThrowIfNull(name); lock (Gate) { ThrowIfDisposed(); if (_blendShapeNames.Count >= 4096) throw new InvalidOperationException("Blend shape definition budget exceeded."); if (_surfaces.Count != 0) throw new InvalidOperationException("Define blend shapes before adding surfaces."); _blendShapeNames.Add(UniqueBlendShapeName(name, -1)); }
        EmitChanged();
    }
    /// <summary>Clears shape definitions before any surfaces exist.</summary>
    /// <remarks>ClearSurfaces removes geometry while retaining these definitions.</remarks>
    public void ClearBlendShapes()
    {
        lock (Gate) { ThrowIfDisposed(); if (_surfaces.Count != 0) throw new InvalidOperationException("Remove surfaces before clearing blend shapes."); _blendShapeNames.Clear(); }
        EmitChanged();
    }
    private string UniqueBlendShapeName(string name, int except)
    {
        if (name.Length > 65536) throw new ArgumentException("Blend shape name exceeds its archive budget.", nameof(name));
        bool Taken(string value) { for (var i = 0; i < _blendShapeNames.Count; i++) if (i != except && string.Equals(_blendShapeNames[i], value, StringComparison.Ordinal)) return true; return false; }
        var unique = name; for (var suffix = 2; Taken(unique); suffix++) unique = name + " " + suffix.ToString(System.Globalization.CultureInfo.InvariantCulture);
        if (unique.Length > 65536) throw new ArgumentException("Unique blend shape name exceeds its archive budget.", nameof(name)); return unique;
    }
    /// <inheritdoc />
    protected override int OnGetBlendShapeCount() { lock (Gate) { ThrowIfDisposed(); return _blendShapeNames.Count; } }
    /// <inheritdoc />
    protected override string OnGetBlendShapeName(int index) { lock (Gate) return _blendShapeNames[index]; }
    /// <inheritdoc />
    protected override void OnSetBlendShapeName(int index, string name) { lock (Gate) { ThrowIfDisposed(); _blendShapeNames[index] = UniqueBlendShapeName(name, index); } EmitChanged(); }
    /// <inheritdoc />
    protected override MeshSurfaceData[] OnSurfaceGetBlendShapeArrays(int surfaceIndex) { lock (Gate) return Get(surfaceIndex).BlendShapes.Select(shape => shape.Copy()).ToArray(); }
    internal static ArrayFormat BlendMaskFor(MeshSurfaceData data, IReadOnlyList<MeshSurfaceData>? shapes, ArrayFormat format = 0)
    {
        var mask = (data.VertexCount == 0 ? format & ArrayFormat.BlendShapeMask : data.IsProcedural ? ArrayFormat.None : ArrayFormat.Vertex) | (data.Normals.Length != 0 ? ArrayFormat.Normal : 0) | (data.Tangents.Length != 0 ? ArrayFormat.Tangent : 0);
        if ((mask & ArrayFormat.Normal) != 0 && shapes is { Count: > 0 } && shapes[0] is { Tangents.Length: 0 }) mask &= ~ArrayFormat.Tangent;
        return mask;
    }
    internal static void ValidateBlendShape(MeshSurfaceData shape, MeshSurfaceData basis, ArrayFormat mask)
    {
        ArgumentNullException.ThrowIfNull(shape); ArgumentNullException.ThrowIfNull(shape.Vertices); ArgumentNullException.ThrowIfNull(shape.Normals); ArgumentNullException.ThrowIfNull(shape.Tangents);
        if (shape.VertexCount != basis.VertexCount || shape.Vertices.Length != ((mask & ArrayFormat.Vertex) != 0 ? basis.VertexCount : 0) || shape.Normals.Length != ((mask & ArrayFormat.Normal) != 0 ? basis.VertexCount : 0) || shape.Tangents.Length != ((mask & ArrayFormat.Tangent) != 0 ? basis.VertexCount : 0)) throw new ArgumentException("Blend shape counts must match the base position/normal/tangent schema.");
        if (shape.Colors.Length != 0 || shape.UVs.Length != 0 || shape.UV2s.Length != 0 || shape.Indices.Length != 0 || shape.Bones.Length != 0 || shape.Weights.Length != 0 || shape.Custom0 is not null || shape.Custom1 is not null || shape.Custom2 is not null || shape.Custom3 is not null) throw new ArgumentException("Blend shapes contain only positions, normals and tangents.");
        foreach (var value in shape.Vertices) if (!value.IsFinite()) throw new ArgumentException("Blend positions must be finite.");
        foreach (var value in shape.Normals) if (!value.IsFinite()) throw new ArgumentException("Blend normals must be finite.");
        foreach (var value in shape.Tangents) if (!value.IsFinite()) throw new ArgumentException("Blend tangents must be finite.");
    }
}
