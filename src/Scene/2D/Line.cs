using System.Runtime.InteropServices;

namespace Electron2D;

/// <summary>Selects the shape of an open line's endpoint.</summary>
public enum LineCapMode
{
    /// <summary>No cap.</summary>
    None = 0,
    /// <summary>A square cap.</summary>
    Box = 1,
    /// <summary>A semicircular cap.</summary>
    Round = 2,
}

/// <summary>Selects the shape of a line joint.</summary>
public enum LineJointMode
{
    /// <summary>A miter joint, limited by SharpLimit.</summary>
    Sharp = 0,
    /// <summary>A beveled joint.</summary>
    Bevel = 1,
    /// <summary>A rounded joint.</summary>
    Round = 2,
}

/// <summary>Selects how a line texture is placed along its length.</summary>
public enum LineTextureMode
{
    /// <summary>Supplies zero texture coordinates across the line.</summary>
    None = 0,
    /// <summary>Tiles the texture along the line when repeat sampling is enabled.</summary>
    Tile = 1,
    /// <summary>Stretches the texture along the full length.</summary>
    Stretch = 2,
}

/// <summary>Draws a thick, optionally colored and textured polyline in local canvas coordinates.</summary>
/// <remarks>Points are caller-owned values. Gradient, width curve and texture are borrowed resources; changes
/// invalidate retained drawing. The node owns only its event subscriptions, not those resources.</remarks>
public partial class Line : Entity
{
    private static readonly PropertyDescriptor[] LineProperties =
    [
        new PropertyDescriptor<Line, Vector2[]>(nameof(Points), n => n.Points, (n, v) => n.Points = v, _ => [], stored: true),
        new PropertyDescriptor<Line, bool>(nameof(Closed), n => n.Closed, (n, v) => n.Closed = v, _ => false, stored: true),
        new PropertyDescriptor<Line, float>(nameof(Width), n => n.Width, (n, v) => n.Width = v, _ => 10f, stored: true),
        new PropertyDescriptor<Line, Curve?>(nameof(WidthCurve), n => n.WidthCurve, (n, v) => n.WidthCurve = v, _ => null, stored: true),
        new PropertyDescriptor<Line, Color>(nameof(DefaultColor), n => n.DefaultColor, (n, v) => n.DefaultColor = v, _ => Colors.White, stored: true),
        new PropertyDescriptor<Line, Gradient?>(nameof(Gradient), n => n.Gradient, (n, v) => n.Gradient = v, _ => null, stored: true),
        new PropertyDescriptor<Line, Texture?>(nameof(Texture), n => n.Texture, (n, v) => n.Texture = v, _ => null, stored: true),
        new PropertyDescriptor<Line, LineTextureMode>(nameof(TextureMode), n => n.TextureMode, (n, v) => n.TextureMode = v, _ => LineTextureMode.None, stored: true),
        new PropertyDescriptor<Line, LineJointMode>(nameof(JointMode), n => n.JointMode, (n, v) => n.JointMode = v, _ => LineJointMode.Sharp, stored: true),
        new PropertyDescriptor<Line, LineCapMode>(nameof(BeginCapMode), n => n.BeginCapMode, (n, v) => n.BeginCapMode = v, _ => LineCapMode.None, stored: true),
        new PropertyDescriptor<Line, LineCapMode>(nameof(EndCapMode), n => n.EndCapMode, (n, v) => n.EndCapMode = v, _ => LineCapMode.None, stored: true),
        new PropertyDescriptor<Line, float>(nameof(SharpLimit), n => n.SharpLimit, (n, v) => n.SharpLimit = v, _ => 2f, stored: true),
        new PropertyDescriptor<Line, int>(nameof(RoundPrecision), n => n.RoundPrecision, (n, v) => n.RoundPrecision = v, _ => 8, stored: true),
        new PropertyDescriptor<Line, bool>(nameof(Antialiased), n => n.Antialiased, (n, v) => n.Antialiased = v, _ => false, stored: true),
    ];

    private Vector2[] _points = [];
    private bool _closed;
    private float _width = 10f;
    private Curve? _widthCurve;
    private Color _defaultColor = Colors.White;
    private Gradient? _gradient;
    private Texture? _texture;
    private LineTextureMode _textureMode;
    private LineJointMode _jointMode;
    private LineCapMode _beginCapMode, _endCapMode;
    private float _sharpLimit = 2f;
    private int _roundPrecision = 8;
    private bool _antialiased;
    private readonly List<CanvasVertex> _mesh = [];
    private readonly List<int> _lineIndices = [];
    private int _stripUp, _stripDown;
    private readonly List<Vector2> _effectivePoints = [];

    /// <summary>Creates an empty line with width ten and white color.</summary>
    public Line() { }

    /// <summary>Gets or replaces the local points. The returned array is a copy.</summary>
    /// <value>Copied finite local positions, empty initially.</value>
    /// <exception cref="ArgumentException">An assigned point is nonfinite.</exception>
    public Vector2[] Points
    {
        get { ThrowIfDisposed(); return (Vector2[])_points.Clone(); }
        set
        {
            EnsureMutable(); ArgumentNullException.ThrowIfNull(value);
            foreach (var point in value) if (!point.IsFinite()) throw new ArgumentException("Line points must be finite.", nameof(value));
            _points = (Vector2[])value.Clone(); InvalidateCanvas();
        }
    }

    /// <summary>Connects the last point to the first when at least three distinct points exist.</summary>
    /// <remarks>Caps are ignored. The closing joint samples the start of width and color resources; variable width need not be seamless.</remarks>
    public bool Closed { get { ThrowIfDisposed(); return _closed; } set { EnsureMutable(); _closed = value; InvalidateCanvas(); } }

    /// <summary>Gets or sets the base width in local units; negative values clamp to zero.</summary>
    /// <value>Ten initially.</value>
    /// <exception cref="ArgumentOutOfRangeException">The assigned width is nonfinite.</exception>
    public float Width
    {
        get { ThrowIfDisposed(); return _width; }
        set { EnsureMutable(); Finite(value, nameof(value)); _width = Math.Max(0, value); InvalidateCanvas(); }
    }

    /// <summary>Gets or sets a borrowed normalized width curve.</summary>
    /// <remarks>Open-line cap extensions contribute to the normalized distance at interior points.</remarks>
    public Curve? WidthCurve
    {
        get { ThrowIfDisposed(); return _widthCurve; }
        set
        {
            EnsureMutable(); Live(value);
            if (_widthCurve is not null) _widthCurve.Changed -= ResourceChanged;
            _widthCurve = value;
            if (value is not null) value.Changed += ResourceChanged;
            InvalidateCanvas();
        }
    }

    /// <summary>Gets or sets the uniform color used when Gradient is null.</summary>
    /// <value>Opaque white initially.</value>
    /// <exception cref="ArgumentException">The assigned color is nonfinite.</exception>
    public Color DefaultColor
    {
        get { ThrowIfDisposed(); return _defaultColor; }
        set { EnsureMutable(); if (!value.IsFinite()) throw new ArgumentException("Line color must be finite.", nameof(value)); _defaultColor = value; InvalidateCanvas(); }
    }

    /// <summary>Gets or sets a borrowed gradient across the full line.</summary>
    /// <remarks>Open-line cap extensions contribute to the normalized distance at interior points.</remarks>
    public Gradient? Gradient
    {
        get { ThrowIfDisposed(); return _gradient; }
        set
        {
            EnsureMutable(); Live(value);
            if (_gradient is not null) _gradient.Changed -= ResourceChanged;
            _gradient = value;
            if (value is not null) value.Changed += ResourceChanged;
            InvalidateCanvas();
        }
    }

    /// <summary>Gets or sets a borrowed line texture.</summary>
    public Texture? Texture
    {
        get { ThrowIfDisposed(); return _texture; }
        set
        {
            EnsureMutable(); Live(value);
            if (_texture is not null) _texture.Changed -= ResourceChanged;
            _texture = value;
            if (value is not null) value.Changed += ResourceChanged;
            InvalidateCanvas();
        }
    }

    /// <summary>Gets or sets the texture's longitudinal coordinate policy.</summary>
    /// <remarks>None supplies zero UVs. Round caps map their arc before or after the body; Tile requires repeat sampling.</remarks>
    public LineTextureMode TextureMode
    {
        get { ThrowIfDisposed(); return _textureMode; }
        set { EnsureMutable(); Mode(value); _textureMode = value; InvalidateCanvas(); }
    }

    /// <summary>Gets or sets the shape of every bend.</summary>
    public LineJointMode JointMode
    {
        get { ThrowIfDisposed(); return _jointMode; }
        set { EnsureMutable(); Mode(value); _jointMode = value; InvalidateCanvas(); }
    }

    /// <summary>Gets or sets the first endpoint cap when open.</summary>
    public LineCapMode BeginCapMode
    {
        get { ThrowIfDisposed(); return _beginCapMode; }
        set { EnsureMutable(); Mode(value); _beginCapMode = value; InvalidateCanvas(); }
    }

    /// <summary>Gets or sets the final endpoint cap when open.</summary>
    public LineCapMode EndCapMode
    {
        get { ThrowIfDisposed(); return _endCapMode; }
        set { EnsureMutable(); Mode(value); _endCapMode = value; InvalidateCanvas(); }
    }

    /// <summary>Gets or sets the maximum miter length in half-widths; negative values clamp to zero.</summary>
    public float SharpLimit
    {
        get { ThrowIfDisposed(); return _sharpLimit; }
        set { EnsureMutable(); Finite(value, nameof(value)); _sharpLimit = Math.Max(0, value); InvalidateCanvas(); }
    }

    /// <summary>Gets or sets the number of subdivisions per semicircular cap; values below one clamp to one.</summary>
    public int RoundPrecision
    {
        get { ThrowIfDisposed(); return _roundPrecision; }
        set { EnsureMutable(); _roundPrecision = Math.Max(1, value); InvalidateCanvas(); }
    }

    /// <summary>Gets or sets whether sides, caps and joints receive a one-framebuffer-pixel coverage fringe.</summary>
    public bool Antialiased { get { ThrowIfDisposed(); return _antialiased; } set { EnsureMutable(); _antialiased = value; InvalidateCanvas(); } }

    /// <summary>Adds a local point, inserting before index or appending when index is negative or past the end.</summary>
    /// <param name="position">Finite local position.</param>
    /// <param name="index">Insertion index, or a negative/out-of-range value to append.</param>
    /// <exception cref="ArgumentException">The position is nonfinite.</exception>
    public void AddPoint(Vector2 position, int index = -1)
    {
        EnsureMutable(); if (!position.IsFinite()) throw new ArgumentException("Line points must be finite.", nameof(position));
        var at = index < 0 || index > _points.Length ? _points.Length : index;
        var updated = new Vector2[_points.Length + 1];
        Array.Copy(_points, 0, updated, 0, at); updated[at] = position;
        Array.Copy(_points, at, updated, at + 1, _points.Length - at);
        _points = updated; InvalidateCanvas();
    }

    /// <summary>Removes every point.</summary>
    public void ClearPoints() { EnsureMutable(); if (_points.Length == 0) return; _points = []; InvalidateCanvas(); }
    /// <summary>Returns the number of stored points.</summary>
    /// <returns>The count, including duplicate points.</returns>
    public int GetPointCount() { ThrowIfDisposed(); return _points.Length; }
    /// <summary>Returns a point at the specified index.</summary>
    /// <param name="index">Zero-based stored point index.</param>
    /// <returns>The stored local position.</returns>
    /// <exception cref="IndexOutOfRangeException">The index is invalid.</exception>
    public Vector2 GetPointPosition(int index) { ThrowIfDisposed(); return _points[index]; }
    /// <summary>Removes a point at the specified index.</summary>
    /// <param name="index">Zero-based stored point index.</param>
    /// <exception cref="ArgumentOutOfRangeException">The index is invalid.</exception>
    public void RemovePoint(int index)
    {
        EnsureMutable(); ArgumentOutOfRangeException.ThrowIfNegative(index); ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(index, _points.Length);
        var updated = new Vector2[_points.Length - 1];
        Array.Copy(_points, 0, updated, 0, index); Array.Copy(_points, index + 1, updated, index, updated.Length - index);
        _points = updated; InvalidateCanvas();
    }
    /// <summary>Changes a point's local position.</summary>
    /// <param name="index">Zero-based stored point index.</param>
    /// <param name="position">Finite replacement position.</param>
    /// <exception cref="ArgumentOutOfRangeException">The index is invalid.</exception>
    /// <exception cref="ArgumentException">The position is nonfinite.</exception>
    public void SetPointPosition(int index, Vector2 position)
    {
        EnsureMutable(); ArgumentOutOfRangeException.ThrowIfNegative(index); ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(index, _points.Length);
        if (!position.IsFinite()) throw new ArgumentException("Line points must be finite.", nameof(position));
        _points[index] = position; InvalidateCanvas();
    }

    /// <inheritdoc />
    protected override void OnDraw()
    {
        base.OnDraw();
        if (_width == 0 || _points.Length < 2) return;
        var texture = _texture; BuildMesh(texture);
        if (_lineIndices.Count > 0) DrawTriangleArray(CollectionsMarshal.AsSpan(_mesh), CollectionsMarshal.AsSpan(_lineIndices), texture, _antialiased);
    }

    /// <inheritdoc />
    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors() => base.GetPropertyDescriptors().Concat(LineProperties);
    /// <inheritdoc />
    protected override Func<Node> CreateSceneInstanceFactory() => GetType() == typeof(Line) ? CreateLine : base.CreateSceneInstanceFactory();
    private static Node CreateLine() => new Line();
    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            if (_widthCurve is not null) _widthCurve.Changed -= ResourceChanged;
            if (_gradient is not null) _gradient.Changed -= ResourceChanged;
            if (_texture is not null) _texture.Changed -= ResourceChanged;
            _widthCurve = null; _gradient = null; _texture = null;
        }
        base.Dispose(disposing);
    }

    private void ResourceChanged(Resource _) => InvalidateCanvas();
    private static void Live(Resource? value) { if (value?.IsDisposed == true) throw new ObjectDisposedException(nameof(value)); }
    private static void Finite(float value, string name) { if (!float.IsFinite(value)) throw new ArgumentOutOfRangeException(name); }
    private static void Mode<T>(T value) where T : struct, Enum { if (!Enum.IsDefined(value)) throw new ArgumentOutOfRangeException(nameof(value)); }

}
