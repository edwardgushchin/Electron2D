namespace Electron2D;

/// <summary>Stores copied planar vertices and authored convex polygons used by navigation regions.</summary>
/// <remarks>Public arrays are independent copies. Mutations publish complete immutable versions and then raise Changed.
/// Source outlines, clearance baking and source-geometry parsing require their own executable baking pipeline.</remarks>
public sealed class NavigationPolygon : Resource
{
    private readonly object _gate = new();
    private NavigationPolygonData _data = new([], []);
    /// <summary>Creates empty authored navigation geometry.</summary>
    public NavigationPolygon() { }
    /// <summary>Returns an independent copy of the current local-space vertices.</summary>
    /// <returns>Returns an independent copy of the current local-space vertices.</returns>
    /// <exception cref="ObjectDisposedException">This instance is disposed.</exception>
    public Vector2[] GetVertices() { lock (_gate) { ThrowIfDisposed(); return (Vector2[])_data.Vertices.Clone(); } }
    /// <summary>Replaces vertices transactionally while retaining valid existing polygon indices.</summary>
    /// <param name="vertices">Finite local-space points copied by this resource.</param>
    /// <exception cref="ArgumentException">Points or retained polygon geometry are invalid.</exception>
    /// <exception cref="ObjectDisposedException">This instance is disposed.</exception>
    public void SetVertices(ReadOnlySpan<Vector2> vertices)
    {
        var copy = vertices.ToArray(); foreach (var p in copy) if (!p.IsFinite()) throw new ArgumentException("Navigation vertices must be finite.", nameof(vertices));
        lock (_gate) { ThrowIfDisposed(); foreach (var polygon in _data.Polygons) ValidatePolygon(copy, polygon); _data = new(copy, _data.Polygons); }
        EmitChanged();
    }
    /// <summary>Adds one copied convex polygon expressed as local vertex indices.</summary>
    /// <param name="polygon">At least three distinct valid indices, in either consistent winding.</param>
    /// <exception cref="ArgumentException">Indices, area or convexity are invalid.</exception>
    /// <exception cref="ObjectDisposedException">This instance is disposed.</exception>
    public void AddPolygon(ReadOnlySpan<int> polygon)
    {
        var copy = polygon.ToArray(); lock (_gate) { ThrowIfDisposed(); ValidatePolygon(_data.Vertices, copy); var all = new int[_data.Polygons.Length + 1][]; _data.Polygons.CopyTo(all, 0); all[^1] = copy; _data = new(_data.Vertices, all); }
        EmitChanged();
    }
    /// <summary>Returns an independent copy of a polygon's indices.</summary>
    /// <param name="index">Zero-based authored polygon index.</param>
    /// <exception cref="ArgumentOutOfRangeException">The index is invalid.</exception>
    /// <returns>Returns an independent copy of a polygon's indices.</returns>
    /// <exception cref="ObjectDisposedException">This instance is disposed.</exception>
    public int[] GetPolygon(int index) { lock (_gate) { ThrowIfDisposed(); if ((uint)index >= (uint)_data.Polygons.Length) throw new ArgumentOutOfRangeException(nameof(index)); return (int[])_data.Polygons[index].Clone(); } }
    /// <summary>Returns the number of authored polygons.</summary>
    /// <returns>Returns the number of authored polygons.</returns>
    /// <exception cref="ObjectDisposedException">This instance is disposed.</exception>
    public int GetPolygonCount() { lock (_gate) { ThrowIfDisposed(); return _data.Polygons.Length; } }
    /// <summary>Clears polygons while retaining vertices.</summary>
    /// <exception cref="ObjectDisposedException">This instance is disposed.</exception>
    public void ClearPolygons() { lock (_gate) { ThrowIfDisposed(); _data = new(_data.Vertices, []); } EmitChanged(); }
    /// <summary>Clears authored vertices and polygons.</summary>
    /// <exception cref="ObjectDisposedException">This instance is disposed.</exception>
    public void Clear() { lock (_gate) { ThrowIfDisposed(); _data = new([], []); } EmitChanged(); }
    internal NavigationPolygonData Snapshot() { lock (_gate) { ThrowIfDisposed(); return _data; } }
    internal static void ValidatePolygon(ReadOnlySpan<Vector2> vertices, ReadOnlySpan<int> indices)
    {
        if (indices.Length < 3) throw new ArgumentException("Navigation polygons require at least three vertices.");
        foreach (var i in indices) if ((uint)i >= (uint)vertices.Length) throw new ArgumentException("Navigation polygon index is outside the vertex array.");
        var sign = 0; double area = 0;
        for (var i = 0; i < indices.Length; i++)
        {
            var a = vertices[indices[i]]; var b = vertices[indices[(i + 1) % indices.Length]]; var c = vertices[indices[(i + 2) % indices.Length]];
            if (a == b) throw new ArgumentException("Navigation polygon edges must have length.");
            for (var j = 0; j < i; j++) if (indices[i] == indices[j]) throw new ArgumentException("Navigation polygon indices must be distinct.");
            var cross = ((double)b.X - a.X) * (c.Y - b.Y) - ((double)b.Y - a.Y) * (c.X - b.X);
            if (cross != 0) { var s = Math.Sign(cross); if (sign != 0 && s != sign) throw new ArgumentException("Authored navigation polygons must be convex."); sign = s; }
            area += (double)a.X * b.Y - (double)a.Y * b.X;
        }
        if (sign == 0 || area == 0 || !double.IsFinite(area)) throw new ArgumentException("Navigation polygon area must be finite and nonzero.");
        // Every point must lie in the same edge half-plane; local turns alone also admit self-intersecting stars.
        for (var i = 0; i < indices.Length; i++)
        {
            var a = vertices[indices[i]]; var b = vertices[indices[(i + 1) % indices.Length]];
            foreach (var index in indices)
            {
                var p = vertices[index]; var cross = ((double)b.X - a.X) * ((double)p.Y - a.Y) - ((double)b.Y - a.Y) * ((double)p.X - a.X);
                if (cross * sign < 0) throw new ArgumentException("Navigation polygon must be simple and convex.");
            }
        }
    }
    private byte[] SaveGeometry()
    {
        var data = Snapshot(); long budget = 12 + (long)data.Vertices.Length * 8;
        if (data.Vertices.Length > 1048576 || data.Polygons.Length > 1048576) throw new InvalidDataException("Navigation geometry exceeds its storage count budget.");
        foreach (var polygon in data.Polygons) budget += 4 + (long)polygon.Length * 4;
        if (budget > 64 * 1024 * 1024) throw new InvalidDataException("Navigation geometry exceeds its storage budget.");
        using var stream = new MemoryStream(); using var writer = new BinaryWriter(stream);
        writer.Write(1); writer.Write(data.Vertices.Length); foreach (var p in data.Vertices) { writer.Write(p.X); writer.Write(p.Y); }
        writer.Write(data.Polygons.Length); foreach (var polygon in data.Polygons) { writer.Write(polygon.Length); foreach (var i in polygon) writer.Write(i); }
        return stream.ToArray();
    }
    private void LoadGeometry(byte[] bytes)
    {
        ArgumentNullException.ThrowIfNull(bytes); if (bytes.Length == 0) { Clear(); return; }
        if (bytes.Length > 64 * 1024 * 1024) throw new InvalidDataException("Navigation geometry exceeds its storage budget.");
        using var stream = new MemoryStream(bytes, writable: false); using var reader = new BinaryReader(stream);
        try
        {
            if (reader.ReadInt32() != 1) throw new InvalidDataException("Unsupported navigation geometry version.");
            var count = reader.ReadInt32(); if (count is < 0 or > 1048576 || (long)count * 8 > stream.Length - stream.Position) throw new InvalidDataException("Invalid navigation vertex count.");
            var vertices = new Vector2[count]; for (var i = 0; i < count; i++) { vertices[i] = new(reader.ReadSingle(), reader.ReadSingle()); if (!vertices[i].IsFinite()) throw new InvalidDataException("Nonfinite navigation vertex."); }
            count = reader.ReadInt32(); if (count is < 0 or > 1048576 || (long)count * 4 > stream.Length - stream.Position) throw new InvalidDataException("Invalid navigation polygon count.");
            var polygons = new int[count][];
            for (var i = 0; i < count; i++) { var length = reader.ReadInt32(); if (length < 3 || length > vertices.Length || (long)length * 4 > stream.Length - stream.Position) throw new InvalidDataException("Invalid navigation polygon length."); polygons[i] = new int[length]; for (var j = 0; j < length; j++) polygons[i][j] = reader.ReadInt32(); try { ValidatePolygon(vertices, polygons[i]); } catch (ArgumentException error) { throw new InvalidDataException("Invalid stored navigation polygon geometry.", error); } }
            if (stream.Position != stream.Length) throw new InvalidDataException("Trailing navigation geometry payload.");
            lock (_gate) { ThrowIfDisposed(); _data = new(vertices, polygons); }
            EmitChanged();
        }
        catch (EndOfStreamException error) { throw new InvalidDataException("Truncated navigation geometry.", error); }
    }
    private static readonly PropertyDescriptor[] GeometryProperties = [new PropertyDescriptor<NavigationPolygon, byte[]>("_navigation_geometry", n => n.SaveGeometry(), (n, v) => n.LoadGeometry(v), _ => [], stored: true)];
    /// <inheritdoc />
    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors() => base.GetPropertyDescriptors().Concat(GeometryProperties);
    /// <inheritdoc />
    protected override Resource CreateDuplicateInstance() => new NavigationPolygon();
    /// <inheritdoc />
    protected override void CopyCustomStateTo(Resource target, bool deep, DeepDuplicateMode subresourceMode, Func<Resource?, Resource?> duplicateSubresource, Func<Resource?, Resource?> forceDuplicateSubresource)
    { var copy = (NavigationPolygon)target; var snapshot = Snapshot(); lock (copy._gate) copy._data = snapshot; }
}
internal sealed record NavigationPolygonData(Vector2[] Vertices, int[][] Polygons);
