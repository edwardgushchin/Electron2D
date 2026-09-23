namespace Electron2D;

public abstract partial class CanvasItem
{
    private List<CanvasPolygon>? _polygons;
    private int _polygonCount;

    /// <summary>Records a filled convex or concave polygon with one color during canvas recording.</summary>
    /// <param name="points">At least three finite local contour vertices, in either winding order.</param>
    /// <param name="color">The finite color multiplied by texture sampling and canvas modulation.</param>
    /// <param name="uvs">Normalized texture coordinates for every vertex, or empty for zero coordinates.</param>
    /// <param name="texture">An optional borrowed live texture.</param>
    /// <remarks>Copies the input data and triangulates when recording. Later input edits need a redraw. Atlas regions
    /// remap supplied UVs; margins and FilterClip do not affect polygons. Self-intersecting contours are unsupported.</remarks>
    /// <exception cref="ArgumentException">Data is nonfinite, counts are invalid, or triangulation fails.</exception>
    /// <exception cref="InvalidOperationException">Called outside this item's recording scope or off its owner thread.</exception>
    /// <exception cref="ObjectDisposedException">The item or texture is disposed.</exception>
    public void DrawColoredPolygon(ReadOnlySpan<Vector2> points, Color color, ReadOnlySpan<Vector2> uvs = default, Texture? texture = null) =>
        DrawPolygon(points, [color], uvs, texture);

    /// <summary>Records a filled convex or concave polygon with interpolated vertex colors.</summary>
    /// <param name="points">At least three finite local contour vertices, in either winding order.</param>
    /// <param name="colors">Empty for white, one uniform color, or one finite color per vertex.</param>
    /// <param name="uvs">Empty for zero coordinates, or one finite normalized coordinate per vertex.</param>
    /// <param name="texture">An optional borrowed live texture.</param>
    /// <remarks>Input arrays are copied. Retained triangles obey transforms, ordering, material, modulation and sampling
    /// policies. Triangulation runs only when recording. Self-intersecting contours and holes are unsupported.
    /// Atlas UV remapping captures the immediate atlas region at recording; pixel updates remain live.</remarks>
    /// <exception cref="ArgumentException">Data is nonfinite, counts are invalid, or triangulation fails.</exception>
    /// <exception cref="InvalidOperationException">Called outside this item's recording scope or off its owner thread.</exception>
    /// <exception cref="ObjectDisposedException">The item or texture is disposed.</exception>
    public void DrawPolygon(ReadOnlySpan<Vector2> points, ReadOnlySpan<Color> colors, ReadOnlySpan<Vector2> uvs = default, Texture? texture = null) =>
        RecordPolygon(points, colors, uvs, texture, primitive: false);

    /// <summary>Records a point, one-pixel line, triangle or quad from one through four vertices.</summary>
    /// <param name="points">One through four finite local vertices. Quads use the diagonal from vertex zero to two.</param>
    /// <param name="colors">Finite vertex colors. Missing entries use the first color, or white when empty; extra entries are ignored.</param>
    /// <param name="uvs">Finite normalized vertex UVs. Missing entries are zero; extra entries are ignored.</param>
    /// <param name="texture">An optional borrowed live texture; atlas primitives sample the underlying full image.</param>
    /// <remarks>Copies supplied data. Points and lines remain one framebuffer pixel wide under scaling and have no
    /// antialias fringe. A coincident two-point line draws nothing. Primitive UVs do not remap atlas regions.</remarks>
    /// <exception cref="ArgumentException">Used data is nonfinite or the vertex count is outside one through four.</exception>
    /// <exception cref="InvalidOperationException">Called outside this item's recording scope or off its owner thread.</exception>
    /// <exception cref="ObjectDisposedException">The item or texture is disposed.</exception>
    public void DrawPrimitive(ReadOnlySpan<Vector2> points, ReadOnlySpan<Color> colors, ReadOnlySpan<Vector2> uvs, Texture? texture = null) =>
        RecordPolygon(points, colors, uvs, texture, primitive: true);

    private void RecordPolygon(ReadOnlySpan<Vector2> points, ReadOnlySpan<Color> colors, ReadOnlySpan<Vector2> uvs, Texture? texture, bool primitive)
    {
        EnsureDrawing();
        Rect? uvMapping = null;
        if (texture is AtlasTexture view) texture = view.ResolvePolygonTexture(!primitive && !uvs.IsEmpty, out uvMapping);
        // A custom texture size query can invoke scene code. Validate after that callback before choosing pooled storage.
        EnsureDrawing();
        if (primitive ? points.Length is < 1 or > 4 : points.Length < 3) throw new ArgumentException("Invalid polygon vertex count.", nameof(points));
        if (!primitive && colors.Length != 0 && colors.Length != 1 && colors.Length != points.Length)
            throw new ArgumentException("Colors must be empty, uniform, or match the vertex count.", nameof(colors));
        if (!primitive && uvs.Length != 0 && uvs.Length != points.Length)
            throw new ArgumentException("UVs must be empty or match the vertex count.", nameof(uvs));
        foreach (var point in points) if (!point.IsFinite()) throw new ArgumentException("Vertices must be finite.", nameof(points));
        foreach (var color in colors[..Math.Min(colors.Length, points.Length)]) ValidateCanvasColor(color);
        foreach (var uv in uvs[..Math.Min(uvs.Length, points.Length)]) if (!uv.IsFinite()) throw new ArgumentException("UVs must be finite.", nameof(uvs));
        if (texture is { IsDisposed: true }) throw new ObjectDisposedException(nameof(texture));
        _polygons ??= [];
        if (_polygonCount == _polygons.Count) _polygons.Add(new());
        var polygon = _polygons[_polygonCount];
        polygon.Set(points, colors, uvs, primitive);
        if (uvMapping is { } mapping) polygon.RemapUV(mapping);
        (_canvasCommands ??= []).Add(new CanvasCommand(false, default, default, Colors.White, 0, false, Transform.Identity, texture, Polygon: polygon));
        _polygonCount++;
    }
}
