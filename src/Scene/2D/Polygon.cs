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
        new PropertyDescriptor<Polygon, bool>(nameof(Antialiased), n => n.Antialiased, (n, v) => n.Antialiased = v, _ => false, stored: true),
        new PropertyDescriptor<Polygon, bool>(nameof(InvertEnabled), n => n.InvertEnabled, (n, v) => n.InvertEnabled = v, _ => false, stored: true),
        new PropertyDescriptor<Polygon, float>(nameof(InvertBorder), n => n.InvertBorder, (n, v) => n.InvertBorder = v, _ => 100f, stored: true),
    ];

    private Vector2[] _vertices = [], _uv = [];
    private Vector2[] _drawPoints = [], _drawUV = [], _contourPoints = [], _contourUV = [];
    private Vector2[] _invertedPoints = [];
    private Color[] _contourColors = [];
    private int[][] _polygons = [];
    private Color[] _vertexColors = [];
    private Color _color = Colors.White;
    private Texture? _texture;
    private Vector2 _offset, _textureOffset, _textureScale = Vector2.One;
    private float _textureRotation;
    private int _internalVertexCount;
    private bool _invertEnabled, _antialiased;
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
    /// <value>Zero by default. Inversion also omits these vertices; explicit contours may reference them.</value>
    /// <exception cref="ArgumentOutOfRangeException">The assigned count is negative.</exception>
    public int InternalVertexCount
    {
        get { ThrowIfDisposed(); return _internalVertexCount; }
        set { EnsureMutable(); ArgumentOutOfRangeException.ThrowIfNegative(value); _internalVertexCount = value; InvalidateCanvas(); }
    }

    /// <summary>Gets or replaces copied index contours into Vertices.</summary>
    /// <value>Empty by default, meaning one contour in vertex order. Contours with fewer than three indices are skipped.
    /// An index outside the current <see cref="Vertices"/> fails on redraw.</value>
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

    /// <summary>Gets or sets the borrowed fill texture. Texture changes request a redraw.</summary>
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

    /// <summary>Gets or replaces copied pixel-space texture coordinates. A count mismatch uses the drawn local positions.</summary>
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

    /// <summary>Gets or sets the finite offset applied after rotation and before per-axis scaling of pixel-space texture coordinates.</summary>
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

    /// <summary>Gets or sets the finite per-axis scale applied after texture-coordinate rotation and offset.</summary>
    /// <value>One on each axis by default.</value>
    /// <exception cref="ArgumentException">The assigned scale is nonfinite.</exception>
    public Vector2 TextureScale
    {
        get { ThrowIfDisposed(); return _textureScale; }
        set { EnsureMutable(); ValidateVector(value); _textureScale = value; InvalidateCanvas(); }
    }

    /// <summary>Gets or sets a one-framebuffer-pixel transparent fringe along filled polygon boundaries.</summary>
    /// <value>False initially.</value>
    /// <remarks>Runs after skinning, transforms and pixel snapping on both canvas backends. Interior triangles and
    /// shared contour edges retain their original fill. Texture coordinates and colors extend from each boundary;
    /// authored vertex shaders may alter the final width, and custom fragments must preserve interpolated color alpha. Joins are bounded to four framebuffer pixels.
    /// Rendering rejects multiply blending, which do not consume coverage alpha.</remarks>
    public bool Antialiased
    {
        get { ThrowIfDisposed(); return _antialiased; }
        set { EnsureMutable(); if (_antialiased == value) return; _antialiased = value; InvalidateCanvas(); }
    }

    /// <summary>Gets or sets whether the filled region extends outside the vertex contour to its padded bounds.</summary>
    /// <value>False by default. Explicit Polygons are ignored while inversion is enabled.</value>
    public bool InvertEnabled
    {
        get { ThrowIfDisposed(); return _invertEnabled; }
        set { EnsureMutable(); _invertEnabled = value; InvalidateCanvas(); }
    }

    /// <summary>Gets or sets the finite padding of the bounding rectangle used for inverted fill.</summary>
    /// <value>100 local units by default. Inverted drawing requires padding that expands the contour bounds.</value>
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
        var length = SkinInputLength;
        if (length < 3) return;
        var texture = _texture;
        Vector2 textureSize = default;
        if (texture is not null)
        {
            if (texture.IsDisposed) throw new ObjectDisposedException(nameof(Texture));
            textureSize = texture.GetSize();
            EnsureMutable();
            if (!ReferenceEquals(texture, _texture)) return;
            if (!textureSize.IsFinite() || textureSize.X <= 0 || textureSize.Y <= 0)
                throw new InvalidOperationException("Polygon texture dimensions must be positive and finite.");
            length = SkinInputLength;
            if (length < 3) return;
        }
        if (_drawPoints.Length < length) Array.Resize(ref _drawPoints, length);
        for (var i = 0; i < length; i++) _drawPoints[i] = _vertices[i] + _offset;
        var sourcePoints = _drawPoints.AsSpan(0, length);
        var inverted = _invertEnabled;
        var contours = _polygons;
        ReadOnlySpan<Vector2> points = inverted ? InvertContour(sourcePoints) : sourcePoints;
        length = points.Length;
        Span<Color> uniform = stackalloc Color[1]; uniform[0] = _color;
        ReadOnlySpan<Color> colors = _vertexColors.Length == length ? _vertexColors : uniform;
        ReadOnlySpan<Vector2> uvs = default;
        if (texture is not null)
        {
            if (_drawUV.Length < length) Array.Resize(ref _drawUV, length);
            var transform = new Transform(_textureRotation, _textureOffset).Scaled(_textureScale);
            var sourceUV = _uv.Length == length ? _uv.AsSpan() : points;
            for (var i = 0; i < length; i++) _drawUV[i] = (transform * sourceUV[i]) / textureSize;
            uvs = _drawUV.AsSpan(0, length);
        }
        // The node samples the backing image using UVs normalized by the authored texture's logical size.
        var drawTexture = texture is AtlasTexture atlas ? atlas.ResolvePolygonTexture(false, out _) : texture;
        if (_antialiased)
        {
            if (!inverted && contours.Length != 0)
            {
                var found = false; foreach (var contour in contours) found |= contour.Length >= 3;
                if (!found) return;
            }
            RecordPolygon(points, colors, uvs, drawTexture, primitive: false, inverted ? null : contours);
            EnableLastPolygonAntialias(inverted ? sourcePoints : default);
            if (!inverted) AttachLastPolygonSkin(this, default, length);
            return;
        }
        if (inverted || contours.Length == 0)
        {
            DrawPolygon(points, colors, uvs, drawTexture); if (!inverted) AttachLastPolygonSkin(this, default, length); return;
        }
        foreach (var contour in contours)
        {
            if (contour.Length < 3) continue;
            if (_contourPoints.Length < contour.Length) Array.Resize(ref _contourPoints, contour.Length);
            if (_contourColors.Length < contour.Length) Array.Resize(ref _contourColors, contour.Length);
            if (texture is not null && _contourUV.Length < contour.Length) Array.Resize(ref _contourUV, contour.Length);
            for (var i = 0; i < contour.Length; i++)
            {
                var index = contour[i]; if ((uint)index >= (uint)length) throw new ArgumentOutOfRangeException(nameof(Polygons), "A contour index is outside Vertices.");
                _contourPoints[i] = points[index]; _contourColors[i] = colors.Length == 1 ? colors[0] : colors[index]; if (texture is not null) _contourUV[i] = uvs[index];
            }
            DrawPolygon(_contourPoints.AsSpan(0, contour.Length), _contourColors.AsSpan(0, contour.Length), texture is null ? default : _contourUV.AsSpan(0, contour.Length), drawTexture);
            AttachLastPolygonSkin(this, contour, length);
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

    private ReadOnlySpan<Vector2> InvertContour(ReadOnlySpan<Vector2> points)
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
            orientation += ((double)next.X - point.X) * ((double)next.Y + point.Y);
        }

        if (orientation == 0) throw new ArgumentException("An inverted contour must enclose a nonzero area.", nameof(Vertices));
        var padding = new Vector2(_invertBorder, _invertBorder);
        var outerMin = minimum - padding;
        var outerMax = maximum + padding;
        if (outerMin.X >= minimum.X || outerMin.Y >= minimum.Y || outerMax.X <= maximum.X || outerMax.Y <= maximum.Y)
            throw new ArgumentException("The inversion border must expand the contour bounds.", nameof(InvertBorder));

        var highest = points[highestIndex];
        var bridgeX = highest.X - 0.00001f;
        if (bridgeX == highest.X) bridgeX = MathF.BitDecrement(highest.X);
        Span<Vector2> bridge =
        [
            new(highest.X, outerMax.Y),
            outerMax,
            new(outerMax.X, outerMin.Y),
            outerMin,
            new(outerMin.X, outerMax.Y),
            new(bridgeX, outerMax.Y),
            new(bridgeX, highest.Y),
        ];
        var length = checked(points.Length + bridge.Length);
        if (_invertedPoints.Length < length) Array.Resize(ref _invertedPoints, length);
        var result = _invertedPoints.AsSpan(0, length);
        points[..(highestIndex + 1)].CopyTo(result);
        if (orientation > 0)
        {
            (bridge[1], bridge[4]) = (bridge[4], bridge[1]);
            (bridge[2], bridge[3]) = (bridge[3], bridge[2]);
            (bridge[5], bridge[0]) = (bridge[0], bridge[5]);
            (bridge[6], result[highestIndex]) = (result[highestIndex], bridge[6]);
        }
        bridge.CopyTo(result[(highestIndex + 1)..]);
        points[(highestIndex + 1)..].CopyTo(result[(highestIndex + 1 + bridge.Length)..]);
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
