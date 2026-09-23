using System.Runtime.InteropServices;
using PathPoint = (Electron2D.Vector2 Position, Electron2D.Vector2 In, Electron2D.Vector2 Out);

namespace Electron2D;

/// <summary>A spatial cubic Bézier curve with relative control handles and a lazy distance/tangent cache.</summary>
/// <remarks>Coordinates and distances are local, normally pixels. Point order is explicit; handles are relative
/// to their vertex. State access is serialized and Changed is synchronous outside the lock after commitment.
/// Returned arrays and duplicated resources own their storage. Warm cached sampling and closest queries allocate nothing.</remarks>
public sealed class PathCurve : Resource
{
    private static readonly PropertyDescriptor[] Properties =
    [
        new PropertyDescriptor<PathCurve, int>(nameof(PointCount), c => c.PointCount, (c, v) => c.PointCount = v, _ => 0),
        new PropertyDescriptor<PathCurve, float>(nameof(BakeInterval), c => c.BakeInterval, (c, v) => c.BakeInterval = v, _ => 5),
    ];

    // ponytail: one lock serializes readers; publish immutable cache snapshots if measured contention warrants it.
    private readonly object _gate = new();
    private readonly List<PathPoint> _points = [];
    private Vector2[] _baked = [], _forward = [];
    private float[] _distances = [];
    private float _bakeInterval = 5;
    private bool _dirty;

    /// <summary>Creates an empty spatial curve with a bake interval of five local units.</summary>
    public PathCurve() { }

    /// <summary>Gets or sets the number of vertices.</summary>
    /// <value>Zero initially; must be nonnegative.</value>
    /// <remarks>Growing appends zero-position, zero-handle points and emits Changed once per point. Shrinking
    /// removes the tail and emits Changed once. Both then emit PropertyListChanged; equal assignment is silent.</remarks>
    /// <exception cref="ArgumentOutOfRangeException">The count is negative.</exception>
    /// <exception cref="ObjectDisposedException">The resource is disposed.</exception>
    public int PointCount
    {
        get { lock (_gate) { ThrowIfDisposed(); return _points.Count; } }
        set
        {
            int old;
            lock (_gate)
            {
                ThrowIfDisposed(); ArgumentOutOfRangeException.ThrowIfNegative(value); old = _points.Count; if (old == value) return;
                if (value < old) { _points.RemoveRange(value, old - value); _dirty = true; }
            }
            if (value < old) EmitChanged();
            else for (var i = old; i < value; i++) { lock (_gate) { ThrowIfDisposed(); _points.Add(default); _dirty = true; } EmitChanged(); if (IsDisposed) return; }
            if (!IsDisposed) NotifyPropertyListChanged();
        }
    }

    /// <summary>Gets or sets the target distance between cached points.</summary>
    /// <value>Five initially; finite and positive.</value>
    /// <remarks>Every assignment invalidates the cache and emits Changed. Baking uses at most ten subdivision
    /// stages per segment, so this is a target rather than an unconditional maximum distance.</remarks>
    /// <exception cref="ArgumentOutOfRangeException">The interval is nonpositive or nonfinite.</exception>
    /// <exception cref="ObjectDisposedException">The resource is disposed.</exception>
    public float BakeInterval
    {
        get { lock (_gate) { ThrowIfDisposed(); return _bakeInterval; } }
        set { lock (_gate) { ThrowIfDisposed(); if (!(value > 0) || !float.IsFinite(value)) throw new ArgumentOutOfRangeException(nameof(value)); _bakeInterval = value; _dirty = true; } EmitChanged(); }
    }

    /// <summary>Inserts a vertex with relative incoming and outgoing handles.</summary>
    /// <param name="position">Finite local position.</param>
    /// <param name="inHandle">Finite relative incoming handle; zero by default.</param>
    /// <param name="outHandle">Finite relative outgoing handle; zero by default.</param>
    /// <param name="index">Insert before an existing index; any other value appends.</param>
    /// <remarks>Emits Changed then PropertyListChanged, with state committed before callbacks.</remarks>
    /// <exception cref="ArgumentException">A vector is nonfinite.</exception>
    /// <exception cref="ObjectDisposedException">The resource is disposed.</exception>
    public void AddPoint(Vector2 position, Vector2 inHandle = default, Vector2 outHandle = default, int index = -1)
    {
        lock (_gate) { ThrowIfDisposed(); Finite(position, nameof(position)); Finite(inHandle, nameof(inHandle)); Finite(outHandle, nameof(outHandle)); _points.Insert((uint)index < (uint)_points.Count ? index : _points.Count, (position, inHandle, outHandle)); _dirty = true; }
        EmitChanged(); if (!IsDisposed) NotifyPropertyListChanged();
    }

    /// <summary>Returns a vertex's local position.</summary>
    /// <param name="index">An existing index.</param>
    /// <returns>The stored position.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The index is invalid.</exception>
    /// <exception cref="ObjectDisposedException">The resource is disposed.</exception>
    public Vector2 GetPointPosition(int index) { lock (_gate) return Point(index).Position; }
    /// <summary>Returns a vertex's relative incoming handle.</summary>
    /// <param name="index">An existing index.</param>
    /// <returns>The stored relative offset.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The index is invalid.</exception>
    /// <exception cref="ObjectDisposedException">The resource is disposed.</exception>
    public Vector2 GetPointIn(int index) { lock (_gate) return Point(index).In; }
    /// <summary>Returns a vertex's relative outgoing handle.</summary>
    /// <param name="index">An existing index.</param>
    /// <returns>The stored relative offset.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The index is invalid.</exception>
    /// <exception cref="ObjectDisposedException">The resource is disposed.</exception>
    public Vector2 GetPointOut(int index) { lock (_gate) return Point(index).Out; }
    /// <summary>Sets a vertex position without changing its relative handles and emits Changed.</summary>
    /// <param name="index">An existing index.</param>
    /// <param name="position">Finite local position.</param>
    /// <exception cref="ArgumentException">The vector is nonfinite.</exception>
    /// <exception cref="ArgumentOutOfRangeException">The index is invalid.</exception>
    /// <exception cref="ObjectDisposedException">The resource is disposed.</exception>
    public void SetPointPosition(int index, Vector2 position) { lock (_gate) { Finite(position, nameof(position)); Point(index).Position = position; _dirty = true; } EmitChanged(); }
    /// <summary>Sets a relative incoming handle and emits Changed.</summary>
    /// <param name="index">An existing index.</param>
    /// <param name="position">Finite relative offset.</param>
    /// <exception cref="ArgumentException">The vector is nonfinite.</exception>
    /// <exception cref="ArgumentOutOfRangeException">The index is invalid.</exception>
    /// <exception cref="ObjectDisposedException">The resource is disposed.</exception>
    public void SetPointIn(int index, Vector2 position) { lock (_gate) { Finite(position, nameof(position)); Point(index).In = position; _dirty = true; } EmitChanged(); }
    /// <summary>Sets a relative outgoing handle and emits Changed.</summary>
    /// <param name="index">An existing index.</param>
    /// <param name="position">Finite relative offset.</param>
    /// <exception cref="ArgumentException">The vector is nonfinite.</exception>
    /// <exception cref="ArgumentOutOfRangeException">The index is invalid.</exception>
    /// <exception cref="ObjectDisposedException">The resource is disposed.</exception>
    public void SetPointOut(int index, Vector2 position) { lock (_gate) { Finite(position, nameof(position)); Point(index).Out = position; _dirty = true; } EmitChanged(); }
    /// <summary>Removes an existing vertex and emits Changed then PropertyListChanged.</summary>
    /// <param name="index">An existing index.</param>
    /// <exception cref="ArgumentOutOfRangeException">The index is invalid.</exception>
    /// <exception cref="ObjectDisposedException">The resource is disposed.</exception>
    public void RemovePoint(int index) { lock (_gate) { _ = Point(index); _points.RemoveAt(index); _dirty = true; } EmitChanged(); if (!IsDisposed) NotifyPropertyListChanged(); }
    /// <summary>Removes all vertices, emitting Changed then PropertyListChanged; empty curves are unchanged.</summary>
    /// <exception cref="ObjectDisposedException">The resource is disposed.</exception>
    public void ClearPoints() { lock (_gate) { ThrowIfDisposed(); if (_points.Count == 0) return; _points.Clear(); _dirty = true; } EmitChanged(); if (!IsDisposed) NotifyPropertyListChanged(); }

    /// <summary>Samples one cubic segment at an unbounded finite parameter.</summary>
    /// <param name="index">Segment index; negative selects the first vertex and index at/past the last selects the last.</param>
    /// <param name="t">Zero starts a valid segment, one ends it; other finite values extrapolate.</param>
    /// <returns>A local position.</returns>
    /// <exception cref="ArgumentOutOfRangeException">t is nonfinite.</exception>
    /// <exception cref="InvalidOperationException">The curve is empty or derived geometry is nonfinite.</exception>
    /// <exception cref="ObjectDisposedException">The resource is disposed.</exception>
    public Vector2 Sample(int index, float t) { lock (_gate) { RequirePoints(); Finite(t, nameof(t)); return SampleCore(index, t); } }
    /// <summary>Samples by combined segment index and fractional parameter.</summary>
    /// <param name="offset">Finite index plus fraction, clamped between zero and the point count.</param>
    /// <returns>A local position using Sample's endpoint behavior.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The offset is nonfinite.</exception>
    /// <exception cref="InvalidOperationException">The curve is empty or derived geometry is nonfinite.</exception>
    /// <exception cref="ObjectDisposedException">The resource is disposed.</exception>
    public Vector2 Samplef(float offset) { lock (_gate) { RequirePoints(); Finite(offset, nameof(offset)); offset = Math.Clamp(offset, 0, _points.Count); return SampleCore((int)offset, offset % 1); } }

    /// <summary>Returns the cumulative chord length of the baked polyline.</summary>
    /// <returns>Zero for fewer than two distinct baked positions.</returns>
    /// <exception cref="InvalidOperationException">Derived geometry or cumulative length is nonfinite.</exception>
    /// <exception cref="ObjectDisposedException">The resource is disposed.</exception>
    public float GetBakedLength() { lock (_gate) { ThrowIfDisposed(); BakeCore(); return _distances.Length == 0 ? 0 : _distances[^1]; } }
    /// <summary>Returns an independent copy of the baked positions, baking first if dirty.</summary>
    /// <returns>A new array, possibly empty.</returns>
    /// <exception cref="InvalidOperationException">Derived geometry is nonfinite.</exception>
    /// <exception cref="ObjectDisposedException">The resource is disposed.</exception>
    public Vector2[] GetBakedPoints() { lock (_gate) { ThrowIfDisposed(); BakeCore(); return (Vector2[])_baked.Clone(); } }
    /// <summary>Samples the baked polyline by distance.</summary>
    /// <param name="offset">Finite local distance, clamped to the baked length.</param>
    /// <param name="cubic">Use cubic interpolation of neighboring baked positions instead of linear interpolation.</param>
    /// <returns>The local sample; a one-point curve returns that point.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The distance is nonfinite.</exception>
    /// <exception cref="InvalidOperationException">The curve is empty or derived geometry is nonfinite.</exception>
    /// <exception cref="ObjectDisposedException">The resource is disposed.</exception>
    public Vector2 SampleBaked(float offset = 0, bool cubic = false)
    {
        lock (_gate) { RequirePoints(); Finite(offset, nameof(offset)); BakeCore(); if (_baked.Length == 1) return _baked[0]; var interval = FindInterval(offset); return SampleCache(interval.Index, interval.Fraction, cubic); }
    }
    /// <summary>Samples local position and an interpolated unit tangent frame by baked distance.</summary>
    /// <param name="offset">Finite local distance, clamped to the baked length.</param>
    /// <param name="cubic">Whether position uses cubic cached interpolation; tangent interpolation remains spherical.</param>
    /// <returns>A transform with tangent X, perpendicular Y and sampled Origin. A one-point or fully degenerate curve uses identity orientation.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The distance is nonfinite.</exception>
    /// <exception cref="InvalidOperationException">The curve is empty or derived geometry is nonfinite.</exception>
    /// <exception cref="ObjectDisposedException">The resource is disposed.</exception>
    public Transform SampleBakedWithRotation(float offset = 0, bool cubic = false)
    {
        lock (_gate)
        {
            RequirePoints(); Finite(offset, nameof(offset)); BakeCore();
            if (_baked.Length == 1) return new(Vector2.Right, Vector2.Down, _baked[0]);
            var (index, fraction) = FindInterval(offset);
            var direction = _forward[index].Slerp(_forward[index + 1], fraction).Normalized();
            if (direction.IsZeroApprox()) direction = Vector2.Right;
            return new(direction, new(-direction.Y, direction.X), SampleCache(index, fraction, cubic));
        }
    }
    /// <summary>Returns the closest point on the baked polyline in local coordinates.</summary>
    /// <param name="toPoint">Finite local query position.</param>
    /// <returns>The earliest segment's nearest point on ties; degenerate segments remain valid point candidates.</returns>
    /// <exception cref="ArgumentException">The query is nonfinite.</exception>
    /// <exception cref="InvalidOperationException">The curve is empty or derived geometry is nonfinite.</exception>
    /// <exception cref="ObjectDisposedException">The resource is disposed.</exception>
    public Vector2 GetClosestPoint(Vector2 toPoint) { lock (_gate) return Closest(toPoint).Position; }
    /// <summary>Returns the local distance along the baked polyline to its closest point.</summary>
    /// <param name="toPoint">Finite local query position.</param>
    /// <returns>The earliest closest distance on ties; zero for a one-point curve.</returns>
    /// <exception cref="ArgumentException">The query is nonfinite.</exception>
    /// <exception cref="InvalidOperationException">The curve is empty or derived geometry is nonfinite.</exception>
    /// <exception cref="ObjectDisposedException">The resource is disposed.</exception>
    public float GetClosestOffset(Vector2 toPoint) { lock (_gate) return Closest(toPoint).Offset; }

    /// <summary>Returns a curvature-controlled polyline, including both endpoints.</summary>
    /// <param name="maxStages">Subdivision depth, zero through twenty; default five.</param>
    /// <param name="toleranceDegrees">Finite nonnegative turning-angle threshold in degrees; default four.</param>
    /// <returns>An independent array; one vertex returns one point, no vertices returns empty.</returns>
    /// <remarks>Midpoints are evaluated at depths zero through maxStages, so a segment can produce up to 2^(maxStages+1)+1 points.</remarks>
    /// <exception cref="ArgumentOutOfRangeException">A parameter is outside its finite range.</exception>
    /// <exception cref="InvalidOperationException">Derived geometry is nonfinite.</exception>
    /// <exception cref="ObjectDisposedException">The resource is disposed.</exception>
    public Vector2[] Tessellate(int maxStages = 5, float toleranceDegrees = 4)
    {
        lock (_gate) { ThrowIfDisposed(); Stages(maxStages); Finite(toleranceDegrees, nameof(toleranceDegrees)); ArgumentOutOfRangeException.ThrowIfNegative(toleranceDegrees); return TessellateCore(maxStages, MathF.Cos(MathF.DegToRad(toleranceDegrees)), false).ToArray(); }
    }
    /// <summary>Returns a polyline subdivided by chord length, preserving nonconstant closed segments.</summary>
    /// <param name="maxStages">Subdivision depth, zero through twenty; default five.</param>
    /// <param name="toleranceLength">Finite positive target chord length; default twenty local units.</param>
    /// <returns>An independent array; fewer than two vertices returns empty. At most 2^maxStages+1 points per segment.</returns>
    /// <exception cref="ArgumentOutOfRangeException">A parameter is outside its finite range.</exception>
    /// <exception cref="InvalidOperationException">Derived geometry is nonfinite.</exception>
    /// <exception cref="ObjectDisposedException">The resource is disposed.</exception>
    public Vector2[] TessellateEvenLength(int maxStages = 5, float toleranceLength = 20)
    {
        lock (_gate) { ThrowIfDisposed(); Stages(maxStages); if (!(toleranceLength > 0) || !float.IsFinite(toleranceLength)) throw new ArgumentOutOfRangeException(nameof(toleranceLength)); return _points.Count < 2 ? [] : TessellateCore(maxStages, toleranceLength, true).ToArray(); }
    }

    private ref PathPoint Point(int index) { ThrowIfDisposed(); if ((uint)index >= (uint)_points.Count) throw new ArgumentOutOfRangeException(nameof(index)); return ref CollectionsMarshal.AsSpan(_points)[index]; }
    private void RequirePoints() { ThrowIfDisposed(); if (_points.Count == 0) throw new InvalidOperationException("The curve has no points."); }
    private static void Finite(Vector2 value, string name) { if (!value.IsFinite()) throw new ArgumentException("Vector must be finite.", name); }
    private static void Finite(float value, string name) { if (!float.IsFinite(value)) throw new ArgumentOutOfRangeException(name); }
    private static void Stages(int value) { if (value is < 0 or > 20) throw new ArgumentOutOfRangeException(nameof(value), "Subdivision depth must be between zero and twenty."); }
    private static Vector2 Geometry(Vector2 value) => value.IsFinite() ? value : throw new InvalidOperationException("Curve geometry overflowed finite coordinates.");
    private Vector2 SampleCore(int index, float t)
    {
        if (index < 0) return _points[0].Position;
        if (index >= _points.Count - 1) return _points[^1].Position;
        var a = _points[index]; var b = _points[index + 1];
        return Geometry(a.Position.BezierInterpolate(Geometry(a.Position + a.Out), Geometry(b.Position + b.In), b.Position, t));
    }
    private Vector2 Tangent(int index, float t)
    {
        var a = _points[index]; var b = _points[index + 1]; var begin = a.Position; var end = b.Position;
        var first = Geometry(begin + a.Out); var second = Geometry(end + b.In);
        if (MathF.IsZeroApprox(t) && first.IsEqualApprox(begin)) return Geometry((first.IsEqualApprox(second) ? end : second) - begin).Normalized();
        if (MathF.IsZeroApprox(t - 1) && second.IsEqualApprox(end)) return Geometry(end - (second.IsEqualApprox(first) ? begin : first)).Normalized();
        if (first.IsEqualApprox(end) && second.IsEqualApprox(begin)) return Geometry(end - begin).Normalized();
        return Geometry(begin.BezierDerivative(first, second, end, t)).Normalized();
    }
    private List<Vector2> TessellateCore(int stages, float tolerance, bool length, List<Vector2>? forwards = null)
    {
        var result = new List<Vector2>();
        if (_points.Count == 0) return result;
        result.Add(_points[0].Position);
        forwards?.Add(_points.Count == 1 ? Vector2.Down : Tangent(0, 0));
        for (var i = 0; i + 1 < _points.Count; i++)
        {
            Subdivide(i, 0, 1, 0, stages, tolerance, length, result, forwards);
            result.Add(_points[i + 1].Position); forwards?.Add(Tangent(i, 1));
        }
        return result;
    }
    private void Subdivide(int index, float begin, float end, int depth, int stages, float tolerance, bool length, List<Vector2> result, List<Vector2>? forwards)
    {
        var middle = begin + (end - begin) * .5f;
        var a = SampleCore(index, begin); var b = SampleCore(index, end); var m = SampleCore(index, middle);
        bool add, recurse;
        if (length)
        {
            var distance = a.DistanceTo(b); if (!float.IsFinite(distance)) throw new InvalidOperationException("Curve distance overflowed.");
            var closed = a == b && (m != a || SampleCore(index, begin + (end - begin) * .25f) != a || SampleCore(index, begin + (end - begin) * .75f) != a);
            add = recurse = depth < stages && (distance > tolerance || closed);
        }
        else { add = Geometry(m - a).Normalized().Dot(Geometry(b - m).Normalized()) < tolerance; recurse = depth < stages; }
        // In-order traversal produces the same ordered midpoints without a sorted map per segment.
        if (recurse) Subdivide(index, begin, middle, depth + 1, stages, tolerance, length, result, forwards);
        if (add) { result.Add(m); forwards?.Add(Tangent(index, middle)); }
        if (recurse) Subdivide(index, middle, end, depth + 1, stages, tolerance, length, result, forwards);
    }
    private void BakeCore()
    {
        if (!_dirty) return;
        var forward = new List<Vector2>(); var points = TessellateCore(10, _bakeInterval, true, forward).ToArray(); var distances = new float[points.Length];
        for (var i = 1; i < points.Length; i++)
        {
            distances[i] = distances[i - 1] + points[i - 1].DistanceTo(points[i]);
            if (!float.IsFinite(distances[i])) throw new InvalidOperationException("Baked curve length overflowed.");
        }
        _baked = points; _forward = forward.ToArray(); _distances = distances; _dirty = false;
    }
    private (int Index, float Fraction) FindInterval(float offset)
    {
        offset = Math.Clamp(offset, 0, _distances[^1]); var first = 0; var last = _baked.Length; var middle = last / 2;
        while (first < middle) { if (offset <= _distances[middle]) last = middle; else first = middle; middle = (first + last) / 2; }
        var distance = _distances[middle + 1] - _distances[middle];
        return (middle, distance < 1.1920928955078125e-7f ? .5f : (offset - _distances[middle]) / distance);
    }
    private Vector2 SampleCache(int index, float fraction, bool cubic) => Geometry(cubic
        ? _baked[index].CubicInterpolate(_baked[index + 1], _baked[Math.Max(0, index - 1)], _baked[Math.Min(_baked.Length - 1, index + 2)], fraction)
        : _baked[index].Lerp(_baked[index + 1], fraction));
    private (Vector2 Position, float Offset) Closest(Vector2 query)
    {
        RequirePoints(); Finite(query, nameof(query)); BakeCore();
        var nearest = _baked[0]; var nearestOffset = 0f; var nearestDistance = double.PositiveInfinity;
        for (var i = 0; i + 1 < _baked.Length; i++)
        {
            var origin = _baked[i]; var end = _baked[i + 1];
            var dx = (double)end.X - origin.X; var dy = (double)end.Y - origin.Y; var squared = dx * dx + dy * dy;
            var fraction = squared == 0 ? 0 : Math.Clamp((((double)query.X - origin.X) * dx + ((double)query.Y - origin.Y) * dy) / squared, 0, 1);
            var x = origin.X + dx * fraction; var y = origin.Y + dy * fraction;
            var distance = (x - query.X) * (x - query.X) + (y - query.Y) * (y - query.Y);
            if (distance < nearestDistance) { nearest = new((float)x, (float)y); nearestOffset = _distances[i] + (_distances[i + 1] - _distances[i]) * (float)fraction; nearestDistance = distance; }
        }
        return (nearest, nearestOffset);
    }

    /// <inheritdoc />
    /// <remarks>Tooling receives typed descriptors for limits/cache policy and indexed points. Indexed descriptors
    /// address the current index and can become invalid after structural edits; copying uses the custom resource hook.</remarks>
    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors()
    {
        foreach (var property in base.GetPropertyDescriptors()) yield return property;
        foreach (var property in Properties) yield return property;
        var count = PointCount;
        for (var i = 0; i < count; i++)
        {
            var index = i;
            yield return new PropertyDescriptor<PathCurve, Vector2>($"Point[{index}].Position", c => c.GetPointPosition(index), (c, v) => c.SetPointPosition(index, v), _ => Vector2.Zero);
            if (index > 0) yield return new PropertyDescriptor<PathCurve, Vector2>($"Point[{index}].In", c => c.GetPointIn(index), (c, v) => c.SetPointIn(index, v), _ => Vector2.Zero);
            if (index == 0 || index + 1 < count) yield return new PropertyDescriptor<PathCurve, Vector2>($"Point[{index}].Out", c => c.GetPointOut(index), (c, v) => c.SetPointOut(index, v), _ => Vector2.Zero);
        }
    }

    /// <inheritdoc />
    protected override Resource CreateDuplicateInstance() => new PathCurve();
    /// <inheritdoc />
    protected override void CopyCustomStateTo(Resource target, bool deep, DeepDuplicateMode subresourceMode, Func<Resource?, Resource?> duplicateSubresource, Func<Resource?, Resource?> forceDuplicateSubresource)
    {
        PathPoint[] points; float interval;
        lock (_gate) { ThrowIfDisposed(); points = _points.ToArray(); interval = _bakeInterval; }
        var copy = (PathCurve)target;
        lock (copy._gate) { copy.ThrowIfDisposed(); copy._points.Clear(); copy._points.AddRange(points); copy._bakeInterval = interval; copy._baked = []; copy._forward = []; copy._distances = []; copy._dirty = true; }
    }
    /// <inheritdoc />
    protected override void OnResetState() { lock (_gate) { _baked = []; _forward = []; _distances = []; _dirty = true; } }
    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        if (disposing) lock (_gate) { _points.Clear(); _baked = []; _forward = []; _distances = []; }
        base.Dispose(disposing);
    }
}
