using System.Runtime.InteropServices;

namespace Electron2D;

/// <summary>Draws a thick, optionally colored and textured polyline in local canvas coordinates.</summary>
/// <remarks>Points are caller-owned values. Gradient, width curve and texture are borrowed resources; changes
/// invalidate retained drawing. The node owns only its event subscriptions, not those resources.</remarks>
public class Line : Entity
{
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
        /// <summary>Samples the first texture column along the line.</summary>
        None = 0,
        /// <summary>Tiles the texture along the line when repeat sampling is enabled.</summary>
        Tile = 1,
        /// <summary>Stretches the texture along the full length.</summary>
        Stretch = 2,
    }

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
    private readonly List<CanvasVertex> _fringe = [];
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

    /// <summary>Gets or sets whether the line has a one-unit feather along its edges.</summary>
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
        BuildMesh();
        if (_fringe.Count > 0) DrawTriangleArray(CollectionsMarshal.AsSpan(_fringe), _texture);
        if (_mesh.Count > 0) DrawTriangleArray(CollectionsMarshal.AsSpan(_mesh), _texture);
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

    private void BuildMesh()
    {
        _mesh.Clear(); _fringe.Clear(); _effectivePoints.Clear();
        foreach (var point in _points)
            if (_effectivePoints.Count == 0 || point != _effectivePoints[^1]) _effectivePoints.Add(point);
        if (_closed && _effectivePoints.Count > 2 && _effectivePoints[0] == _effectivePoints[^1]) _effectivePoints.RemoveAt(_effectivePoints.Count - 1);
        var count = _effectivePoints.Count;
        var closed = _closed && count > 2;
        if (count < 2) return;
        var segmentCount = closed ? count : count - 1;
        Span<double> distances = count <= 256 ? stackalloc double[count] : new double[count];
        for (var i = 1; i < count; i++) distances[i] = distances[i - 1] + (_effectivePoints[i] - _effectivePoints[i - 1]).Length();
        var total = distances[^1] + (closed ? (_effectivePoints[0] - _effectivePoints[^1]).Length() : 0);
        if (!(total > 0) || !double.IsFinite(total)) return;
        var aspect = 1f;
        if (_texture is not null)
        {
            var height = _texture.GetHeight();
            if (height <= 0 || _texture.GetWidth() <= 0) return;
            aspect = _texture.GetWidth() / (float)height;
        }
        var segments = new Segment[segmentCount];
        for (var i = 0; i < segmentCount; i++)
        {
            var next = (i + 1) % count;
            var start = _effectivePoints[i]; var end = _effectivePoints[next];
            var delta = end - start; var length = delta.Length();
            if (!(length > 0)) continue;
            var direction = delta / length; var normal = new Vector2(-direction.Y, direction.X);
            var t0 = (float)(distances[i] / total); var t1 = next == 0 ? 1f : (float)(distances[next] / total);
            var h0 = HalfWidth(t0); var h1 = HalfWidth(t1);
            segments[i] = new Segment(start, end, direction, normal, h0, h1, t0, t1,
                start + normal * h0, start - normal * h0, end + normal * h1, end - normal * h1);
        }
        if (!closed)
        {
            if (_beginCapMode == LineCapMode.Box) segments[0].ExtendStart();
            if (_endCapMode == LineCapMode.Box) segments[^1].ExtendEnd();
        }
        for (var i = closed ? 0 : 1; i < (closed ? count : count - 1); i++)
        {
            var prevIndex = (i + segmentCount - 1) % segmentCount;
            var nextIndex = i % segmentCount;
            ref var prev = ref segments[prevIndex]; ref var next = ref segments[nextIndex];
            Join(ref prev, ref next, _effectivePoints[i], (float)(distances[i] / total), total, aspect);
        }
        for (var i = 0; i < segmentCount; i++)
        {
            var segment = segments[i];
            var c0 = ColorAt(segment.StartT); var c1 = ColorAt(segment.EndT);
            var u0 = UAt(segment.StartT, total, aspect); var u1 = UAt(segment.EndT, total, aspect);
            Quad(segment.StartLeft, segment.StartRight, segment.EndRight, segment.EndLeft, c0, c1, u0, u1);
            if (_antialiased) Fringe(segment, c0, c1, u0, u1);
        }
        if (!closed)
        {
            if (_beginCapMode == LineCapMode.Round) Cap(segments[0], true, total, aspect);
            if (_endCapMode == LineCapMode.Round) Cap(segments[^1], false, total, aspect);
        }
    }

    private float HalfWidth(float t)
    {
        var factor = _widthCurve?.SampleBaked(t) ?? 1f;
        if (!float.IsFinite(factor)) throw new InvalidOperationException("Line width curve produced a nonfinite width.");
        return _width * factor * 0.5f;
    }
    private Color ColorAt(float t) => _gradient?.Sample(t) ?? _defaultColor;
    private float UAt(float t, double total, float aspect) => _textureMode switch
    {
        LineTextureMode.Tile => (float)(t * total / (_width * aspect)),
        LineTextureMode.Stretch => t,
        _ => 0f,
    };

    private void Join(ref Segment prev, ref Segment next, Vector2 point, float t, double total, float aspect)
    {
        var cross = prev.Direction.Cross(next.Direction);
        var denominator = 1f + prev.Direction.Dot(next.Direction);
        if (Math.Abs(cross) < 0.00001f || denominator <= 0.00001f) return;
        var half = next.StartHalfWidth;
        var miter = (prev.Normal + next.Normal) * (half / denominator);
        var sharp = _jointMode == LineJointMode.Sharp && miter.Length() <= _sharpLimit * Math.Abs(half);
        if (sharp)
        {
            prev.EndLeft = next.StartLeft = point + miter;
            prev.EndRight = next.StartRight = point - miter;
            return;
        }
        var outerLeft = cross < 0;
        var inner = outerLeft ? point - miter : point + miter;
        var outerBefore = outerLeft ? prev.EndLeft : prev.EndRight;
        var outerAfter = outerLeft ? next.StartLeft : next.StartRight;
        if (outerLeft) prev.EndRight = next.StartRight = inner;
        else prev.EndLeft = next.StartLeft = inner;
        var color = ColorAt(t); var u = UAt(t, total, aspect);
        if (_jointMode == LineJointMode.Round)
            Arc(inner, point, outerBefore, outerAfter, cross > 0, color, u, outerLeft ? 0f : 1f);
        else Triangle(_mesh, inner, outerBefore, outerAfter, color, u, outerLeft ? 0f : 1f);
    }

    private void Quad(Vector2 left0, Vector2 right0, Vector2 right1, Vector2 left1, Color c0, Color c1, float u0, float u1)
    {
        Add(_mesh, left0, c0, u0, 0); Add(_mesh, right0, c0, u0, 1); Add(_mesh, right1, c1, u1, 1);
        Add(_mesh, left0, c0, u0, 0); Add(_mesh, right1, c1, u1, 1); Add(_mesh, left1, c1, u1, 0);
    }

    private void Fringe(Segment s, Color c0, Color c1, float u0, float u1)
    {
        var n = s.Normal;
        FeatherQuad(s.StartLeft, s.EndLeft, n, c0, c1, u0, u1, 0);
        FeatherQuad(s.StartRight, s.EndRight, -n, c0, c1, u0, u1, 1);
    }

    private void FeatherQuad(Vector2 a, Vector2 b, Vector2 normal, Color ca, Color cb, float ua, float ub, float v)
    {
        Add(_fringe, a, ca, ua, v); Add(_fringe, a + normal, ca with { A = 0 }, ua, v); Add(_fringe, b + normal, cb with { A = 0 }, ub, v);
        Add(_fringe, a, ca, ua, v); Add(_fringe, b + normal, cb with { A = 0 }, ub, v); Add(_fringe, b, cb, ub, v);
    }

    private void Cap(Segment segment, bool begin, double total, float aspect)
    {
        var point = begin ? segment.Start : segment.End;
        var first = begin ? segment.StartRight : segment.EndRight;
        var last = begin ? segment.StartLeft : segment.EndLeft;
        var t = begin ? segment.StartT : segment.EndT;
        Arc(point, point, first, last, !begin, ColorAt(t), UAt(t, total, aspect), 0.5f);
    }

    private void Arc(Vector2 pivot, Vector2 center, Vector2 first, Vector2 last, bool positive, Color color, float u, float v)
    {
        var startAngle = MathF.Atan2(first.Y - center.Y, first.X - center.X);
        var endAngle = MathF.Atan2(last.Y - center.Y, last.X - center.X);
        var sweep = endAngle - startAngle;
        if (positive && sweep < 0) sweep += Mathf.Tau;
        if (!positive && sweep > 0) sweep -= Mathf.Tau;
        var radius = (first - center).Length();
        var steps = Math.Max(1, (int)Math.Ceiling(Math.Abs(sweep) / Math.PI * _roundPrecision));
        var from = first;
        for (var i = 1; i <= steps; i++)
        {
            var angle = startAngle + sweep * i / steps;
            var to = i == steps ? last : center + new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * radius;
            Triangle(_mesh, pivot, from, to, color, u, v);
            from = to;
        }
    }

    private static void Triangle(List<CanvasVertex> output, Vector2 a, Vector2 b, Vector2 c, Color color, float u, float v)
    { Add(output, a, color, u, .5f); Add(output, b, color, u, v); Add(output, c, color, u, v); }
    private static void Add(List<CanvasVertex> output, Vector2 position, Color color, float u, float v) => output.Add(new(position, color, new(u, v)));

    private struct Segment(Vector2 start, Vector2 end, Vector2 direction, Vector2 normal, float startHalfWidth,
        float endHalfWidth, float startT, float endT, Vector2 startLeft, Vector2 startRight, Vector2 endLeft, Vector2 endRight)
    {
        internal Vector2 Start = start, End = end, Direction = direction, Normal = normal;
        internal float StartHalfWidth = startHalfWidth, EndHalfWidth = endHalfWidth, StartT = startT, EndT = endT;
        internal Vector2 StartLeft = startLeft, StartRight = startRight, EndLeft = endLeft, EndRight = endRight;
        internal void ExtendStart() { var offset = Direction * StartHalfWidth; StartLeft -= offset; StartRight -= offset; }
        internal void ExtendEnd() { var offset = Direction * EndHalfWidth; EndLeft += offset; EndRight += offset; }
    }
}
