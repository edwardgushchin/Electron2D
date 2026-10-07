namespace Electron2D;

/// <summary>Draws a filled polygon from local vertices, optional contours, colors and texture coordinates.</summary>
/// <remarks>The texture is borrowed. Vertex and contour arrays are copied in both directions; edits request a retained-canvas redraw.</remarks>
public partial class Polygon : Entity
{
    private static readonly PropertyDescriptor[] PolygonProperties =
    [
        new PropertyDescriptor<Polygon, Vector2[]>(nameof(Vertices), n => n.Vertices, (n, v) => n.Vertices = v, _ => [], stored: true),
        new PropertyDescriptor<Polygon, int>(nameof(InternalVertexCount), n => n.InternalVertexCount, (n, v) => n.InternalVertexCount = v, _ => 0, stored: true),
        new PropertyDescriptor<Polygon, int[][]>(nameof(Polygons), n => n.Polygons, (n, v) => n.Polygons = v, _ => [], stored: true),
        new PropertyDescriptor<Polygon, Color>(nameof(Color), n => n.Color, (n, v) => n.Color = v, _ => Colors.White, stored: true),
        new PropertyDescriptor<Polygon, Color[]>(nameof(VertexColors), n => n.VertexColors, (n, v) => n.VertexColors = v, _ => [], stored: true),
        new PropertyDescriptor<Polygon, Texture?>(nameof(Texture), n => n.Texture, (n, v) => n.Texture = v, _ => null, stored: true),
        new PropertyDescriptor<Polygon, Vector2[]>(nameof(UV), n => n.UV, (n, v) => n.UV = v, _ => [], stored: true),
        new PropertyDescriptor<Polygon, Vector2>(nameof(Offset), n => n.Offset, (n, v) => n.Offset = v, _ => Vector2.Zero, stored: true),
        new PropertyDescriptor<Polygon, Vector2>(nameof(TextureOffset), n => n.TextureOffset, (n, v) => n.TextureOffset = v, _ => Vector2.Zero, stored: true),
        new PropertyDescriptor<Polygon, float>(nameof(TextureRotation), n => n.TextureRotation, (n, v) => n.TextureRotation = v, _ => 0f, stored: true),
        new PropertyDescriptor<Polygon, Vector2>(nameof(TextureScale), n => n.TextureScale, (n, v) => n.TextureScale = v, _ => Vector2.One, stored: true),
        new PropertyDescriptor<Polygon, bool>(nameof(InvertEnabled), n => n.InvertEnabled, (n, v) => n.InvertEnabled = v, _ => false, stored: true),
        new PropertyDescriptor<Polygon, float>(nameof(InvertBorder), n => n.InvertBorder, (n, v) => n.InvertBorder = v, _ => 100f, stored: true),
    ];

    private Vector2[] _vertices = [], _uv = [];
    private Vector2[] _drawPoints = [], _drawUV = [], _contourPoints = [], _contourUV = [];
    private Color[] _contourColors = [];
    private int[][] _polygons = [];
    private Color[] _vertexColors = [];
    private Color _color = Colors.White;
    private Texture? _texture;
    private Vector2 _offset, _textureOffset, _textureScale = Vector2.One;
    private float _textureRotation;
    private int _internalVertexCount;
    private bool _invertEnabled;
    private float _invertBorder = 100f;

    /// <summary>Creates an empty white polygon.</summary>
    public Polygon() { }

    /// <summary>Gets or replaces the copied local vertices. At least three are needed to draw.</summary>
    /// <value>An empty array by default.</value>
    /// <exception cref="ArgumentNullException">The assigned array is null.</exception>
    /// <exception cref="ArgumentException">A vertex is nonfinite.</exception>
    public Vector2[] Vertices
    {
        get { ThrowIfDisposed(); return (Vector2[])_vertices.Clone(); }
        set { EnsureMutable(); ValidateVectors(value); _vertices = (Vector2[])value.Clone(); InvalidateCanvas(); }
    }

    /// <summary>Gets or sets how many trailing vertices are omitted from the default contour.</summary>
    /// <value>Zero by default. Explicit contours may reference the trailing vertices.</value>
    /// <exception cref="ArgumentOutOfRangeException">The assigned count is negative.</exception>
    public int InternalVertexCount
    {
        get { ThrowIfDisposed(); return _internalVertexCount; }
        set { EnsureMutable(); ArgumentOutOfRangeException.ThrowIfNegative(value); _internalVertexCount = value; InvalidateCanvas(); }
    }

    /// <summary>Gets or replaces copied index contours into Vertices.</summary>
    /// <value>Empty by default, meaning one contour in vertex order. Contours with fewer than three indices are skipped.</value>
    /// <exception cref="ArgumentNullException">The outer array or a contour is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">A contour index is negative.</exception>
    public int[][] Polygons
    {
        get { ThrowIfDisposed(); return CopyContours(_polygons); }
        set
        {
            EnsureMutable(); ArgumentNullException.ThrowIfNull(value);
            foreach (var contour in value)
            {
                ArgumentNullException.ThrowIfNull(contour);
                foreach (var index in contour) ArgumentOutOfRangeException.ThrowIfNegative(index);
            }
            _polygons = CopyContours(value); InvalidateCanvas();
        }
    }

    /// <summary>Gets or sets the finite uniform fill color used when VertexColors has no complete vertex set.</summary>
    /// <value>Opaque white by default.</value>
    /// <exception cref="ArgumentException">The assigned color is nonfinite.</exception>
    public Color Color
    {
        get { ThrowIfDisposed(); return _color; }
        set { EnsureMutable(); ValidateColor(value); _color = value; InvalidateCanvas(); }
    }

    /// <summary>Gets or replaces copied per-vertex colors. An incomplete set uses Color for every vertex.</summary>
    /// <value>An empty array by default.</value>
    /// <exception cref="ArgumentNullException">The assigned array is null.</exception>
    /// <exception cref="ArgumentException">A color is nonfinite.</exception>
    public Color[] VertexColors
    {
        get { ThrowIfDisposed(); return (Color[])_vertexColors.Clone(); }
        set
        {
            EnsureMutable(); ArgumentNullException.ThrowIfNull(value);
            foreach (var color in value) ValidateColor(color);
            _vertexColors = (Color[])value.Clone(); InvalidateCanvas();
        }
    }

    /// <summary>Gets or sets the borrowed fill texture.</summary>
    /// <value>Null by default.</value>
    /// <exception cref="ObjectDisposedException">The assigned texture is disposed.</exception>
    public Texture? Texture
    {
        get { ThrowIfDisposed(); return _texture; }
        set
        {
            EnsureMutable();
            if (value is { IsDisposed: true }) throw new ObjectDisposedException(nameof(value));
            if (ReferenceEquals(_texture, value)) return;
            if (_texture is not null) _texture.Changed -= TextureChanged;
            _texture = value;
            if (value is not null) value.Changed += TextureChanged;
            InvalidateCanvas();
        }
    }

    /// <summary>Gets or replaces copied pixel-space texture coordinates. An incomplete set uses local vertices.</summary>
    /// <value>An empty array by default.</value>
    /// <exception cref="ArgumentNullException">The assigned array is null.</exception>
    /// <exception cref="ArgumentException">A coordinate is nonfinite.</exception>
    public Vector2[] UV
    {
        get { ThrowIfDisposed(); return (Vector2[])_uv.Clone(); }
        set { EnsureMutable(); ValidateVectors(value); _uv = (Vector2[])value.Clone(); InvalidateCanvas(); }
    }

    /// <summary>Gets or sets the finite local translation applied to every vertex before drawing.</summary>
    /// <value>Zero by default.</value>
    /// <exception cref="ArgumentException">The assigned offset is nonfinite.</exception>
    public Vector2 Offset
    {
        get { ThrowIfDisposed(); return _offset; }
        set { EnsureMutable(); ValidateVector(value); _offset = value; InvalidateCanvas(); }
    }

    /// <summary>Gets or sets the finite translation applied to pixel-space texture coordinates.</summary>
    /// <value>Zero by default.</value>
    /// <exception cref="ArgumentException">The assigned offset is nonfinite.</exception>
    public Vector2 TextureOffset
    {
        get { ThrowIfDisposed(); return _textureOffset; }
        set { EnsureMutable(); ValidateVector(value); _textureOffset = value; InvalidateCanvas(); }
    }

    /// <summary>Gets or sets the finite clockwise texture rotation in radians.</summary>
    /// <value>Zero by default.</value>
    /// <exception cref="ArgumentOutOfRangeException">The assigned rotation is nonfinite.</exception>
    public float TextureRotation
    {
        get { ThrowIfDisposed(); return _textureRotation; }
        set
        {
            EnsureMutable();
            if (!float.IsFinite(value)) throw new ArgumentOutOfRangeException(nameof(value));
            _textureRotation = value; InvalidateCanvas();
        }
    }

    /// <summary>Gets or sets the finite texture-coordinate scale before rotation.</summary>
    /// <value>One on each axis by default.</value>
    /// <exception cref="ArgumentException">The assigned scale is nonfinite.</exception>
    public Vector2 TextureScale
    {
        get { ThrowIfDisposed(); return _textureScale; }
        set { EnsureMutable(); ValidateVector(value); _textureScale = value; InvalidateCanvas(); }
    }

    /// <summary>Gets or sets whether the filled region extends outside the vertex contour to its padded bounds.</summary>
    /// <value>False by default. Explicit Polygons are ignored while inversion is enabled.</value>
    public bool InvertEnabled
    {
        get { ThrowIfDisposed(); return _invertEnabled; }
        set { EnsureMutable(); _invertEnabled = value; InvalidateCanvas(); }
    }

    /// <summary>Gets or sets the finite padding of the bounding rectangle used for inverted fill.</summary>
    /// <value>100 local units by default. A nonpositive value may fail triangulation.</value>
    /// <exception cref="ArgumentOutOfRangeException">The assigned border is nonfinite.</exception>
    public float InvertBorder
    {
        get { ThrowIfDisposed(); return _invertBorder; }
        set
        {
            EnsureMutable();
            if (!float.IsFinite(value)) throw new ArgumentOutOfRangeException(nameof(value));
            _invertBorder = value; InvalidateCanvas();
        }
    }

    /// <inheritdoc />
    protected override void OnDraw()
    {
        base.OnDraw();
        if (_vertices.Length < 3) return;
        var length = _invertEnabled || _polygons.Length == 0 ? Math.Max(0, _vertices.Length - _internalVertexCount) : _vertices.Length;
        if (length < 3) return;
        if (_drawPoints.Length < length) Array.Resize(ref _drawPoints, length);
        for (var i = 0; i < length; i++) _drawPoints[i] = _vertices[i] + _offset;
        ReadOnlySpan<Vector2> points = _drawPoints.AsSpan(0, length);
        if (_invertEnabled) points = InvertContour(points.ToArray(), _invertBorder);
        length = points.Length;
        Span<Color> uniform = stackalloc Color[1]; uniform[0] = _color;
        ReadOnlySpan<Color> colors = _vertexColors.Length == length ? _vertexColors : uniform;
        ReadOnlySpan<Vector2> uvs = default;
        if (_texture is { } texture)
        {
            var size = texture.GetSize();
            if (!size.IsFinite() || size.X <= 0 || size.Y <= 0) throw new InvalidOperationException("Polygon texture dimensions must be positive and finite.");
            if (_drawUV.Length < length) Array.Resize(ref _drawUV, length);
            var transform = new Transform(_textureRotation, _textureOffset);
            for (var i = 0; i < length; i++) { var source = _uv.Length == length ? _uv[i] : points[i]; _drawUV[i] = (transform * (source * _textureScale)) / size; }
            uvs = _drawUV.AsSpan(0, length);
        }
        if (_invertEnabled || _polygons.Length == 0)
        {
            DrawPolygon(points, colors, uvs, _texture); if (!_invertEnabled) AttachLastPolygonSkin(this, default); return;
        }
        foreach (var contour in _polygons)
        {
            if (contour.Length < 3) continue;
            if (_contourPoints.Length < contour.Length) Array.Resize(ref _contourPoints, contour.Length);
            if (_contourColors.Length < contour.Length) Array.Resize(ref _contourColors, contour.Length);
            if (_texture is not null && _contourUV.Length < contour.Length) Array.Resize(ref _contourUV, contour.Length);
            for (var i = 0; i < contour.Length; i++)
            {
                var index = contour[i]; if ((uint)index >= (uint)length) throw new ArgumentOutOfRangeException(nameof(Polygons), "A contour index is outside Vertices.");
                _contourPoints[i] = points[index]; _contourColors[i] = colors.Length == 1 ? colors[0] : colors[index]; if (_texture is not null) _contourUV[i] = uvs[index];
            }
            DrawPolygon(_contourPoints.AsSpan(0, contour.Length), _contourColors.AsSpan(0, contour.Length), _texture is null ? default : _contourUV.AsSpan(0, contour.Length), _texture);
            AttachLastPolygonSkin(this, contour);
        }
    }

    /// <inheritdoc />
    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors() => base.GetPropertyDescriptors().Concat(PolygonProperties).Concat(SkinProperties);

    /// <inheritdoc />
    protected override Func<Node> CreateSceneInstanceFactory() => GetType() == typeof(Polygon) ? CreatePolygon : base.CreateSceneInstanceFactory();

    private static Node CreatePolygon() => new Polygon();

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        if (disposing && _texture is not null) _texture.Changed -= TextureChanged;
        _texture = null; _skinTree = null; _skinSkeleton = null; _bones.Clear();
        base.Dispose(disposing);
    }

    private void TextureChanged(Resource _) => InvalidateCanvas();

    private static Vector2[] InvertContour(Vector2[] points, float border)
    {
        var minimum = points[0]; var maximum = points[0];
        var highestIndex = 0;
        double orientation = 0;
        for (var i = 0; i < points.Length; i++)
        {
            var point = points[i];
            minimum = new(Math.Min(minimum.X, point.X), Math.Min(minimum.Y, point.Y));
            maximum = new(Math.Max(maximum.X, point.X), Math.Max(maximum.Y, point.Y));
            if (point.Y > points[highestIndex].Y) highestIndex = i;
            var next = points[(i + 1) % points.Length];
            orientation += ((double)next.X - point.X) * (next.Y + point.Y);
        }

        minimum -= new Vector2(border, border);
        maximum += new Vector2(border, border);
        var highest = points[highestIndex];
        var bridgeX = highest.X - 0.00001f;
        if (bridgeX == highest.X) bridgeX = MathF.BitDecrement(highest.X);
        Vector2[] bridge =
        [
            new(highest.X, highest.Y + border),
            maximum,
            new(maximum.X, minimum.Y),
            minimum,
            new(minimum.X, maximum.Y),
            new(bridgeX, highest.Y + border),
            new(bridgeX, highest.Y),
        ];
        if (orientation > 0)
        {
            (bridge[1], bridge[4]) = (bridge[4], bridge[1]);
            (bridge[2], bridge[3]) = (bridge[3], bridge[2]);
            (bridge[5], bridge[0]) = (bridge[0], bridge[5]);
            (bridge[6], points[highestIndex]) = (points[highestIndex], bridge[6]);
        }
        var result = new Vector2[points.Length + bridge.Length];
        Array.Copy(points, 0, result, 0, highestIndex + 1);
        Array.Copy(bridge, 0, result, highestIndex + 1, bridge.Length);
        Array.Copy(points, highestIndex + 1, result, highestIndex + 1 + bridge.Length, points.Length - highestIndex - 1);
        return result;
    }

    private static int[][] CopyContours(int[][] contours) => contours.Select(contour => (int[])contour.Clone()).ToArray();

    private static void ValidateVectors(Vector2[] vectors)
    {
        ArgumentNullException.ThrowIfNull(vectors);
        foreach (var vector in vectors) ValidateVector(vector);
    }

    private static void ValidateVector(Vector2 vector)
    {
        if (!vector.IsFinite()) throw new ArgumentException("Polygon coordinates must be finite.");
    }

    private static void ValidateColor(Color color)
    {
        if (!color.IsFinite()) throw new ArgumentException("Polygon colors must be finite.");
    }
}
