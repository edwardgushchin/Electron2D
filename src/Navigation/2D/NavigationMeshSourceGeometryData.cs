namespace Electron2D;

/// <summary>Owns copied planar source contours used to bake navigation geometry.</summary>
/// <remarks>Snapshots and exports do not share caller arrays. State is synchronized; callbacks run after mutation.
/// Bounds include all source families. HasData reports traversable contours, independently of obstructions.</remarks>
public sealed class NavigationMeshSourceGeometryData : Resource
{
    private readonly object _gate = new();
    private NavigationSourceGeometrySnapshot _data = new([], [], []);
    /// <summary>Creates empty source geometry.</summary>
    public NavigationMeshSourceGeometryData() { }
    /// <summary>Reports whether traversable contours are present.</summary><returns>False for obstruction-only geometry.</returns>
    public bool HasData() { lock (_gate) { ThrowIfDisposed(); return _data.Traversable.Length != 0; } }
    /// <summary>Clears all source contours and projected obstructions.</summary>
    public void Clear() { lock (_gate) { ThrowIfDisposed(); _data = new([], [], []); } EmitChanged(); }
    /// <summary>Clears only projected obstructions.</summary>
    public void ClearProjectedObstructions() { lock (_gate) { ThrowIfDisposed(); _data = _data with { Projected = [] }; } EmitChanged(); }
    /// <summary>Returns copied traversable contours.</summary><returns>Independent arrays.</returns>
    public Vector2[][] GetTraversableOutlines() => NavigationGeometryStorage.CopyOutlines(Snapshot().Traversable);
    /// <summary>Returns copied obstruction contours.</summary><returns>Independent arrays.</returns>
    public Vector2[][] GetObstructionOutlines() => NavigationGeometryStorage.CopyOutlines(Snapshot().Obstruction);
    /// <summary>Returns immutable projected entries with copied vertex exports.</summary><returns>Independent entry array.</returns>
    public NavigationProjectedObstruction[] GetProjectedObstructions() => (NavigationProjectedObstruction[])Snapshot().Projected.Clone();
    /// <summary>Replaces traversable contours after copying and validation.</summary><param name="outlines">Finite point arrays.</param>
    public void SetTraversableOutlines(Vector2[][] outlines) => SetOutlines(outlines, true, false);
    /// <summary>Replaces obstruction contours after copying and validation.</summary><param name="outlines">Finite point arrays.</param>
    public void SetObstructionOutlines(Vector2[][] outlines) => SetOutlines(outlines, false, false);
    /// <summary>Appends copied traversable contours.</summary><param name="outlines">Finite point arrays.</param>
    public void AppendTraversableOutlines(Vector2[][] outlines) => SetOutlines(outlines, true, true);
    /// <summary>Appends copied obstruction contours.</summary><param name="outlines">Finite point arrays.</param>
    public void AppendObstructionOutlines(Vector2[][] outlines) => SetOutlines(outlines, false, true);
    private void SetOutlines(Vector2[][] outlines, bool traversable, bool append)
    {
        var copy = NavigationGeometryStorage.CopyOutlines(outlines);
        lock (_gate)
        {
            ThrowIfDisposed(); var previous = traversable ? _data.Traversable : _data.Obstruction;
            if (append) copy = [.. previous, .. copy];
            var next = traversable ? _data with { Traversable = copy } : _data with { Obstruction = copy };
            NavigationGeometryStorage.ValidateBudget(next); _data = next;
        }
        EmitChanged();
    }
    /// <summary>Appends a copied traversable contour when it has at least two points.</summary><param name="outline">Finite root-local points.</param>
    public void AddTraversableOutline(ReadOnlySpan<Vector2> outline) { ThrowIfDisposed(); var copy = NavigationGeometryStorage.CopyOutline(outline); if (copy.Length > 1) SetOutlines([copy], true, true); }
    /// <summary>Appends a copied obstruction contour when it has at least two points.</summary><param name="outline">Finite root-local points.</param>
    public void AddObstructionOutline(ReadOnlySpan<Vector2> outline) { ThrowIfDisposed(); var copy = NavigationGeometryStorage.CopyOutline(outline); if (copy.Length > 1) SetOutlines([copy], false, true); }
    /// <summary>Appends a projected obstruction.</summary><param name="vertices">At least two finite points.</param><param name="carve">Applies after clearance erosion when true.</param>
    public void AddProjectedObstruction(ReadOnlySpan<Vector2> vertices, bool carve)
    { var value = new NavigationProjectedObstruction(vertices, carve); lock (_gate) { ThrowIfDisposed(); var next = _data with { Projected = [.. _data.Projected, value] }; NavigationGeometryStorage.ValidateBudget(next); _data = next; } EmitChanged(); }
    /// <summary>Replaces projected obstructions with immutable entries.</summary><param name="obstructions">Entries with independent immutable vertex state.</param>
    public void SetProjectedObstructions(ReadOnlySpan<NavigationProjectedObstruction> obstructions)
    {
        var copy = obstructions.ToArray(); foreach (var item in copy) ArgumentNullException.ThrowIfNull(item);
        lock (_gate) { ThrowIfDisposed(); var next = _data with { Projected = copy }; NavigationGeometryStorage.ValidateBudget(next); _data = next; }
        EmitChanged();
    }
    /// <summary>Appends one coherent snapshot of another source, including self-merge.</summary><param name="otherGeometry">Live source resource.</param>
    public void Merge(NavigationMeshSourceGeometryData otherGeometry)
    {
        ArgumentNullException.ThrowIfNull(otherGeometry); var other = otherGeometry.Snapshot();
        lock (_gate) { ThrowIfDisposed(); var next = new NavigationSourceGeometrySnapshot([.. _data.Traversable, .. other.Traversable], [.. _data.Obstruction, .. other.Obstruction], [.. _data.Projected, .. other.Projected]); NavigationGeometryStorage.ValidateBudget(next); _data = next; }
        EmitChanged();
    }
    /// <summary>Returns the enclosing bounds of every source point.</summary><returns>Zero for empty input.</returns>
    /// <exception cref="OverflowException">The finite coordinate span cannot be represented by Rect2.</exception>
    public Rect2 GetBounds()
    {
        var snapshot = Snapshot(); double minX = double.PositiveInfinity, minY = minX, maxX = double.NegativeInfinity, maxY = maxX;
        void Include(ReadOnlySpan<Vector2> points) { foreach (var p in points) { minX = Math.Min(minX, p.X); minY = Math.Min(minY, p.Y); maxX = Math.Max(maxX, p.X); maxY = Math.Max(maxY, p.Y); } }
        foreach (var outline in snapshot.Traversable) Include(outline); foreach (var outline in snapshot.Obstruction) Include(outline); foreach (var projected in snapshot.Projected) Include(projected.Points);
        if (double.IsPositiveInfinity(minX)) return default;
        if (maxX - minX > float.MaxValue || maxY - minY > float.MaxValue) throw new OverflowException("Source bounds exceed finite Rect2 dimensions.");
        return new((float)minX, (float)minY, (float)(maxX - minX), (float)(maxY - minY));
    }
    internal NavigationSourceGeometrySnapshot Snapshot() { lock (_gate) { ThrowIfDisposed(); return _data; } }
    private byte[] SaveSource()
    {
        var data = Snapshot(); using var stream = new MemoryStream(); using var writer = new BinaryWriter(stream);
        writer.Write(1); NavigationGeometryStorage.WriteOutlines(writer, data.Traversable); NavigationGeometryStorage.WriteOutlines(writer, data.Obstruction);
        writer.Write(data.Projected.Length); foreach (var item in data.Projected) { writer.Write(item.Carve); NavigationGeometryStorage.WriteOutline(writer, item.Points); }
        return stream.ToArray();
    }
    private void LoadSource(byte[] bytes)
    {
        ArgumentNullException.ThrowIfNull(bytes); if (bytes.Length == 0) { Clear(); return; }
        using var stream = new MemoryStream(bytes, false); using var reader = new BinaryReader(stream);
        try
        {
            if (reader.ReadInt32() != 1) throw new InvalidDataException("Unsupported navigation source version.");
            var traversable = NavigationGeometryStorage.ReadOutlines(reader); var obstruction = NavigationGeometryStorage.ReadOutlines(reader);
            var count = NavigationGeometryStorage.ReadCount(reader); var projected = new NavigationProjectedObstruction[count];
            for (var i = 0; i < count; i++) { var carve = reader.ReadBoolean(); projected[i] = new(NavigationGeometryStorage.ReadOutline(reader), carve); }
            if (stream.Position != stream.Length) throw new InvalidDataException("Trailing navigation source data.");
            var data = new NavigationSourceGeometrySnapshot(traversable, obstruction, projected); NavigationGeometryStorage.ValidateBudget(data);
            lock (_gate) { ThrowIfDisposed(); _data = data; }
            EmitChanged();
        }
        catch (EndOfStreamException error) { throw new InvalidDataException("Truncated navigation source data.", error); }
    }
    private static readonly PropertyDescriptor[] SourceProperties = [new PropertyDescriptor<NavigationMeshSourceGeometryData, byte[]>("_navigation_source", r => r.SaveSource(), (r, v) => r.LoadSource(v), _ => [], stored: true)];
    /// <inheritdoc />
    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors() => base.GetPropertyDescriptors().Concat(SourceProperties);
    /// <inheritdoc />
    protected override Resource CreateDuplicateInstance() => new NavigationMeshSourceGeometryData();
    /// <inheritdoc />
    protected override void CopyCustomStateTo(Resource target, bool deep, DeepDuplicateMode subresourceMode, Func<Resource?, Resource?> duplicateSubresource, Func<Resource?, Resource?> forceDuplicateSubresource)
    { var data = Snapshot(); var copy = (NavigationMeshSourceGeometryData)target; lock (copy._gate) copy._data = data; }
}

/// <summary>Stores one immutable projected contour and its clearance policy.</summary>
public sealed class NavigationProjectedObstruction
{
    private readonly Vector2[] _vertices;
    /// <summary>Copies at least two finite points.</summary><param name="vertices">Root-local contour.</param><param name="carve">True excludes the contour after clearance erosion.</param>
    public NavigationProjectedObstruction(ReadOnlySpan<Vector2> vertices, bool carve)
    { if (vertices.Length < 2) throw new ArgumentException("A projected obstruction requires at least two points.", nameof(vertices)); _vertices = NavigationGeometryStorage.CopyOutline(vertices); Carve = carve; }
    /// <summary>Gets whether this contour bypasses agent-radius expansion.</summary><value>Immutable policy.</value>
    public bool Carve { get; }
    /// <summary>Returns an independent copy of the contour.</summary><returns>Finite root-local vertices.</returns>
    public Vector2[] GetVertices() => (Vector2[])_vertices.Clone();
    internal ReadOnlySpan<Vector2> Points => _vertices;
}
internal sealed record NavigationSourceGeometrySnapshot(Vector2[][] Traversable, Vector2[][] Obstruction, NavigationProjectedObstruction[] Projected);

internal static class NavigationGeometryStorage
{
    internal const int Limit = 1048576;
    internal static Vector2[] CopyOutline(ReadOnlySpan<Vector2> outline)
    { if (outline.Length > Limit) throw new ArgumentException("Navigation point count exceeds its preparation budget."); foreach (var p in outline) if (!p.IsFinite()) throw new ArgumentException("Navigation source points must be finite."); return outline.ToArray(); }
    internal static Vector2[][] CopyOutlines(Vector2[][] outlines)
    { ArgumentNullException.ThrowIfNull(outlines); if (outlines.Length > Limit) throw new ArgumentException("Navigation contour count exceeds its preparation budget."); long count = 0; var result = new Vector2[outlines.Length][]; for (var i = 0; i < result.Length; i++) { ArgumentNullException.ThrowIfNull(outlines[i]); count += outlines[i].Length; if (count > Limit) throw new ArgumentException("Navigation point count exceeds its preparation budget."); result[i] = CopyOutline(outlines[i]); } return result; }
    internal static void ValidateBudget(NavigationSourceGeometrySnapshot data)
    { long points = 0, contours = (long)data.Traversable.Length + data.Obstruction.Length + data.Projected.Length; foreach (var p in data.Traversable) points += p.Length; foreach (var p in data.Obstruction) points += p.Length; foreach (var p in data.Projected) points += p.Points.Length; if (points > Limit || contours > Limit) throw new ArgumentException("Navigation source exceeds its preparation budget."); }
    internal static void WriteOutlines(BinaryWriter writer, Vector2[][] outlines) { writer.Write(outlines.Length); foreach (var item in outlines) WriteOutline(writer, item); }
    internal static void WriteOutline(BinaryWriter writer, ReadOnlySpan<Vector2> outline) { writer.Write(outline.Length); foreach (var p in outline) { writer.Write(p.X); writer.Write(p.Y); } }
    internal static int ReadCount(BinaryReader reader) { var count = reader.ReadInt32(); if (count < 0 || count > Limit || (long)count * 4 > reader.BaseStream.Length - reader.BaseStream.Position) throw new InvalidDataException("Invalid navigation contour count."); return count; }
    internal static Vector2[][] ReadOutlines(BinaryReader reader) { var count = ReadCount(reader); var data = new Vector2[count][]; long points = 0; for (var i = 0; i < count; i++) { data[i] = ReadOutline(reader); points += data[i].Length; if (points > Limit) throw new InvalidDataException("Navigation point count exceeds its storage budget."); } return data; }
    internal static Vector2[] ReadOutline(BinaryReader reader) { var count = reader.ReadInt32(); if (count < 0 || count > Limit || (long)count * 8 > reader.BaseStream.Length - reader.BaseStream.Position) throw new InvalidDataException("Invalid navigation point count."); var data = new Vector2[count]; for (var i = 0; i < count; i++) { data[i] = new(reader.ReadSingle(), reader.ReadSingle()); if (!data[i].IsFinite()) throw new InvalidDataException("Nonfinite navigation source point."); } return data; }
}
