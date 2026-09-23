using System.Runtime.InteropServices;
using CurvePoint = (Electron2D.Vector2 Position, float Left, float Right, Electron2D.Curve.TangentMode LeftMode, Electron2D.Curve.TangentMode RightMode);

namespace Electron2D;

/// <summary>A scalar cubic curve y(x), with ordered points, tangent slopes and a lazily baked sample cache.</summary>
/// <remarks>Point insertion clamps to the current domain/value limits. SetPointValue permits values outside those
/// limits. Tangents are slopes, not angles. State access is serialized; synchronous events run outside the state
/// lock after commitment. Copies own their points and cache. Sampling after a bake allocates no managed memory.</remarks>
public sealed class Curve : Resource
{
    /// <summary>Controls one side of a point's tangent.</summary>
    public enum TangentMode
    {
        /// <summary>A caller-supplied slope.</summary>
        Free = 0,
        /// <summary>The slope to the adjacent point, updated by point edits.</summary>
        Linear = 1,
        /// <summary>Number of modes; not a valid mode for a point.</summary>
        Count = 2,
    }

    private static readonly PropertyDescriptor[] Properties =
    [
        new PropertyDescriptor<Curve, int>(nameof(PointCount), c => c.PointCount, (c, v) => c.PointCount = v, _ => 0),
        new PropertyDescriptor<Curve, int>(nameof(BakeResolution), c => c.BakeResolution, (c, v) => c.BakeResolution = v, _ => 100),
        new PropertyDescriptor<Curve, float>(nameof(MinDomain), c => c.MinDomain, (c, v) => c.MinDomain = v, _ => 0),
        new PropertyDescriptor<Curve, float>(nameof(MaxDomain), c => c.MaxDomain, (c, v) => c.MaxDomain = v, _ => 1),
        new PropertyDescriptor<Curve, float>(nameof(MinValue), c => c.MinValue, (c, v) => c.MinValue = v, _ => 0),
        new PropertyDescriptor<Curve, float>(nameof(MaxValue), c => c.MaxValue, (c, v) => c.MaxValue = v, _ => 1),
    ];

    // ponytail: one lock serializes readers; publish immutable cache snapshots if measured contention warrants it.
    private readonly object _gate = new();
    private readonly List<CurvePoint> _points = [];
    private float _minDomain, _minValue, _maxDomain = 1, _maxValue = 1;
    private int _bakeResolution = 100;
    private float[] _baked = [];
    private bool _dirty;

    /// <summary>Creates an empty curve with unit domain/value limits and bake resolution 100.</summary>
    public Curve() { }

    /// <summary>Gets or sets the number of control points.</summary>
    /// <value>Zero initially; must be nonnegative.</value>
    /// <remarks>Shrinking removes the tail. Growing inserts default points at clamped zero when previously empty,
    /// otherwise at MaxDomain; values use clamped zero. Growth emits Changed for each added point, shrinking once,
    /// followed by PropertyListChanged. Equal assignment is silent. Callback failure retains committed edits.</remarks>
    /// <exception cref="ArgumentOutOfRangeException">The count is negative.</exception>
    /// <exception cref="ObjectDisposedException">The resource is disposed.</exception>
    public int PointCount
    {
        get { lock (_gate) { ThrowIfDisposed(); return _points.Count; } }
        set
        {
            int old; Vector2 position;
            lock (_gate)
            {
                ThrowIfDisposed(); ArgumentOutOfRangeException.ThrowIfNegative(value); old = _points.Count;
                if (value == old) return;
                position = new(old == 0 ? Math.Clamp(0, _minDomain, _maxDomain) : _maxDomain, Math.Clamp(0, _minValue, _maxValue));
                if (value < old) { _points.RemoveRange(value, old - value); UpdateAllTangents(); _dirty = true; }
            }
            if (value < old) EmitChanged();
            else for (var i = old; i < value; i++) { lock (_gate) { ThrowIfDisposed(); Insert(position, 0, 0, TangentMode.Free, TangentMode.Free); } EmitChanged(); if (IsDisposed) return; }
            if (!IsDisposed) NotifyPropertyListChanged();
        }
    }

    /// <summary>Gets or sets the number of evenly spaced cached samples.</summary>
    /// <value>100 initially; allowed range 1 through 1000.</value>
    /// <remarks>Invalidates the cache without emitting Changed. Resolution one samples the last control value.</remarks>
    /// <exception cref="ArgumentOutOfRangeException">The resolution is outside the allowed range.</exception>
    /// <exception cref="ObjectDisposedException">The resource is disposed.</exception>
    public int BakeResolution
    {
        get { lock (_gate) { ThrowIfDisposed(); return _bakeResolution; } }
        set { lock (_gate) { ThrowIfDisposed(); if (value is < 1 or > 1000) throw new ArgumentOutOfRangeException(nameof(value)); _bakeResolution = value; _dirty = true; } }
    }

    /// <summary>Gets or sets the lower horizontal bound.</summary>
    /// <value>Zero initially.</value>
    /// <remarks>Limited to at most MaxDomain minus 0.01 and the first point's offset. Every assignment dirties the
    /// cache and emits Changed followed by DomainChanged, including equal values. Does not move points.</remarks>
    /// <exception cref="ArgumentOutOfRangeException">The value or resulting range is not finite and positive.</exception>
    /// <exception cref="ObjectDisposedException">The resource is disposed.</exception>
    public float MinDomain { get { lock (_gate) { ThrowIfDisposed(); return _minDomain; } } set => SetLimit(value, true, true); }

    /// <summary>Gets or sets the upper horizontal bound.</summary>
    /// <value>One initially.</value>
    /// <remarks>Limited to at least MinDomain plus 0.01 and the last point's offset. Emits Changed then DomainChanged.</remarks>
    /// <exception cref="ArgumentOutOfRangeException">The value or resulting range is not finite and positive.</exception>
    /// <exception cref="ObjectDisposedException">The resource is disposed.</exception>
    public float MaxDomain { get { lock (_gate) { ThrowIfDisposed(); return _maxDomain; } } set => SetLimit(value, true, false); }

    /// <summary>Gets or sets the lower insertion-value bound.</summary>
    /// <value>Zero initially.</value>
    /// <remarks>Limited to at most MaxValue minus 0.01 and every existing point's value. Emits RangeChanged even on
    /// equal assignment, without Changed or cache invalidation. Does not clamp sampled values or existing points.</remarks>
    /// <exception cref="ArgumentOutOfRangeException">The value or resulting range is not finite and positive.</exception>
    /// <exception cref="ObjectDisposedException">The resource is disposed.</exception>
    public float MinValue { get { lock (_gate) { ThrowIfDisposed(); return _minValue; } } set => SetLimit(value, false, true); }

    /// <summary>Gets or sets the upper insertion-value bound.</summary>
    /// <value>One initially.</value>
    /// <remarks>Limited to at least MinValue plus 0.01 and every existing point's value. Emits only RangeChanged.</remarks>
    /// <exception cref="ArgumentOutOfRangeException">The value or resulting range is not finite and positive.</exception>
    /// <exception cref="ObjectDisposedException">The resource is disposed.</exception>
    public float MaxValue { get { lock (_gate) { ThrowIfDisposed(); return _maxValue; } } set => SetLimit(value, false, false); }

    /// <summary>Occurs after a domain setter's Changed event, including equal assignments.</summary>
    public event Action? DomainChanged;
    /// <summary>Occurs after a value-limit setter; that setter does not emit Changed.</summary>
    public event Action? RangeChanged;

    /// <summary>Returns MaxDomain minus MinDomain.</summary>
    /// <returns>The positive finite horizontal range.</returns>
    /// <exception cref="ObjectDisposedException">The resource is disposed.</exception>
    public float GetDomainRange() { lock (_gate) { ThrowIfDisposed(); return _maxDomain - _minDomain; } }
    /// <summary>Returns MaxValue minus MinValue.</summary>
    /// <returns>The positive finite insertion-value range.</returns>
    /// <exception cref="ObjectDisposedException">The resource is disposed.</exception>
    public float GetValueRange() { lock (_gate) { ThrowIfDisposed(); return _maxValue - _minValue; } }

    /// <summary>Inserts a point in horizontal order and updates neighboring automatic tangents.</summary>
    /// <param name="position">Finite coordinates, clamped to the domain/value limits.</param>
    /// <param name="leftTangent">Finite left slope, used in Free mode or when no left neighbor exists.</param>
    /// <param name="rightTangent">Finite right slope, used in Free mode or when no right neighbor exists.</param>
    /// <param name="leftMode">Free or Linear.</param>
    /// <param name="rightMode">Free or Linear.</param>
    /// <returns>The inserted index; equal offsets are retained.</returns>
    /// <remarks>Emits Changed then PropertyListChanged. Equal-offset insertion uses the interval search's tie order.</remarks>
    /// <exception cref="ArgumentOutOfRangeException">A coordinate/tangent is nonfinite or a mode is invalid.</exception>
    /// <exception cref="ObjectDisposedException">The resource is disposed.</exception>
    public int AddPoint(Vector2 position, float leftTangent = 0, float rightTangent = 0, TangentMode leftMode = TangentMode.Free, TangentMode rightMode = TangentMode.Free)
    {
        int index;
        lock (_gate)
        {
            ThrowIfDisposed(); Finite(position.X, nameof(position)); Finite(position.Y, nameof(position));
            Finite(leftTangent, nameof(leftTangent)); Finite(rightTangent, nameof(rightTangent)); Mode(leftMode); Mode(rightMode);
            index = Insert(position, leftTangent, rightTangent, leftMode, rightMode);
        }
        EmitChanged(); if (!IsDisposed) NotifyPropertyListChanged(); return index;
    }

    /// <summary>Returns a point's stored coordinates.</summary>
    /// <param name="index">An existing index.</param>
    /// <returns>The ordered offset and value.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The index is invalid.</exception>
    /// <exception cref="ObjectDisposedException">The resource is disposed.</exception>
    public Vector2 GetPointPosition(int index) { lock (_gate) return Point(index).Position; }

    /// <summary>Sets a point's value without clamping to value limits, updates automatic tangents and emits Changed.</summary>
    /// <param name="index">An existing index.</param>
    /// <param name="y">A finite vertical value.</param>
    /// <exception cref="ArgumentOutOfRangeException">The index is invalid or y is nonfinite.</exception>
    /// <exception cref="ObjectDisposedException">The resource is disposed.</exception>
    public void SetPointValue(int index, float y) { lock (_gate) { Finite(y, nameof(y)); Point(index).Position.Y = y; UpdateTangents(index); _dirty = true; } EmitChanged(); }

    /// <summary>Moves a point horizontally, reinserts it in order, recalculates tangents and emits Changed.</summary>
    /// <param name="index">An existing index.</param>
    /// <param name="offset">Finite horizontal position, clamped to domain limits.</param>
    /// <returns>The new index. Reinsertion also clamps the point's value to current value limits.</returns>
    /// <remarks>Does not emit PropertyListChanged, even when the index changes.</remarks>
    /// <exception cref="ArgumentOutOfRangeException">The index is invalid or offset is nonfinite.</exception>
    /// <exception cref="ObjectDisposedException">The resource is disposed.</exception>
    public int SetPointOffset(int index, float offset)
    {
        int result;
        lock (_gate)
        {
            var point = Point(index); Finite(offset, nameof(offset)); _points.RemoveAt(index);
            result = Insert(new(offset, point.Position.Y), point.Left, point.Right, point.LeftMode, point.RightMode);
            if (index != result) UpdateTangents(index); UpdateTangents(result);
        }
        EmitChanged(); return result;
    }

    /// <summary>Returns a point's left tangent slope.</summary>
    /// <param name="index">An existing index.</param>
    /// <returns>The stored slope; automatic duplicate-offset slopes may be nonfinite.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The index is invalid.</exception>
    /// <exception cref="ObjectDisposedException">The resource is disposed.</exception>
    public float GetPointLeftTangent(int index) { lock (_gate) return Point(index).Left; }
    /// <summary>Returns a point's right tangent slope.</summary>
    /// <param name="index">An existing index.</param>
    /// <returns>The stored slope; automatic duplicate-offset slopes may be nonfinite.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The index is invalid.</exception>
    /// <exception cref="ObjectDisposedException">The resource is disposed.</exception>
    public float GetPointRightTangent(int index) { lock (_gate) return Point(index).Right; }
    /// <summary>Returns a point's left tangent mode.</summary>
    /// <param name="index">An existing index.</param>
    /// <returns>Free or Linear.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The index is invalid.</exception>
    /// <exception cref="ObjectDisposedException">The resource is disposed.</exception>
    public TangentMode GetPointLeftMode(int index) { lock (_gate) return Point(index).LeftMode; }
    /// <summary>Returns a point's right tangent mode.</summary>
    /// <param name="index">An existing index.</param>
    /// <returns>Free or Linear.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The index is invalid.</exception>
    /// <exception cref="ObjectDisposedException">The resource is disposed.</exception>
    public TangentMode GetPointRightMode(int index) { lock (_gate) return Point(index).RightMode; }

    /// <summary>Sets a finite left slope, switches that side to Free, and emits Changed.</summary>
    /// <param name="index">An existing index.</param>
    /// <param name="tangent">Finite slope, not an angle.</param>
    /// <exception cref="ArgumentOutOfRangeException">The index is invalid or tangent is nonfinite.</exception>
    /// <exception cref="ObjectDisposedException">The resource is disposed.</exception>
    public void SetPointLeftTangent(int index, float tangent) { lock (_gate) { Finite(tangent, nameof(tangent)); ref var p = ref Point(index); p.Left = tangent; p.LeftMode = TangentMode.Free; _dirty = true; } EmitChanged(); }
    /// <summary>Sets a finite right slope, switches that side to Free, and emits Changed.</summary>
    /// <param name="index">An existing index.</param>
    /// <param name="tangent">Finite slope, not an angle.</param>
    /// <exception cref="ArgumentOutOfRangeException">The index is invalid or tangent is nonfinite.</exception>
    /// <exception cref="ObjectDisposedException">The resource is disposed.</exception>
    public void SetPointRightTangent(int index, float tangent) { lock (_gate) { Finite(tangent, nameof(tangent)); ref var p = ref Point(index); p.Right = tangent; p.RightMode = TangentMode.Free; _dirty = true; } EmitChanged(); }
    /// <summary>Sets the left mode, recalculates its slope when Linear has a neighbor, and emits Changed.</summary>
    /// <param name="index">An existing index.</param>
    /// <param name="mode">Free or Linear.</param>
    /// <exception cref="ArgumentOutOfRangeException">The index or mode is invalid.</exception>
    /// <exception cref="ObjectDisposedException">The resource is disposed.</exception>
    public void SetPointLeftMode(int index, TangentMode mode) { lock (_gate) { Mode(mode); ref var p = ref Point(index); p.LeftMode = mode; if (mode == TangentMode.Linear && index > 0) p.Left = Slope(p.Position, _points[index - 1].Position); _dirty = true; } EmitChanged(); }
    /// <summary>Sets the right mode, recalculates its slope when Linear has a neighbor, and emits Changed.</summary>
    /// <param name="index">An existing index.</param>
    /// <param name="mode">Free or Linear.</param>
    /// <exception cref="ArgumentOutOfRangeException">The index or mode is invalid.</exception>
    /// <exception cref="ObjectDisposedException">The resource is disposed.</exception>
    public void SetPointRightMode(int index, TangentMode mode) { lock (_gate) { Mode(mode); ref var p = ref Point(index); p.RightMode = mode; if (mode == TangentMode.Linear && index + 1 < _points.Count) p.Right = Slope(p.Position, _points[index + 1].Position); _dirty = true; } EmitChanged(); }

    /// <summary>Removes a point, refreshes surviving automatic tangents and emits Changed then PropertyListChanged.</summary>
    /// <param name="index">An existing index.</param>
    /// <exception cref="ArgumentOutOfRangeException">The index is invalid.</exception>
    /// <exception cref="ObjectDisposedException">The resource is disposed.</exception>
    public void RemovePoint(int index) { lock (_gate) { _ = Point(index); _points.RemoveAt(index); UpdateAllTangents(); _dirty = true; } EmitChanged(); if (!IsDisposed) NotifyPropertyListChanged(); }
    /// <summary>Removes all points and emits Changed then PropertyListChanged; an empty curve is a no-op.</summary>
    /// <exception cref="ObjectDisposedException">The resource is disposed.</exception>
    public void ClearPoints() { lock (_gate) { ThrowIfDisposed(); if (_points.Count == 0) return; _points.Clear(); _dirty = true; } EmitChanged(); if (!IsDisposed) NotifyPropertyListChanged(); }
    /// <summary>Removes later neighbors within Mathf.Epsilon in horizontal offset and refreshes automatic tangents.</summary>
    /// <remarks>Preserves the first of each near-duplicate run. Emits Changed only when points were removed;
    /// does not emit PropertyListChanged. Distinct ordered points are never removed just because their difference is signed.</remarks>
    /// <exception cref="ObjectDisposedException">The resource is disposed.</exception>
    public void CleanDupes()
    {
        lock (_gate)
        {
            ThrowIfDisposed(); var changed = false;
            // ponytail: repeated list removal is quadratic for many duplicates; compact in place if bulk imports require it.
            for (var i = 1; i < _points.Count; i++)
                if (Math.Abs(_points[i].Position.X - _points[i - 1].Position.X) <= Mathf.Epsilon) { _points.RemoveAt(i--); changed = true; }
            if (!changed) return;
            UpdateAllTangents(); _dirty = true;
        }
        EmitChanged();
    }

    /// <summary>Samples the cubic curve at a horizontal coordinate.</summary>
    /// <param name="offset">Finite horizontal position.</param>
    /// <returns>Zero for no points; otherwise a cubic sample with constant extrapolation past endpoints.</returns>
    /// <remarks>Near-zero horizontal spans use the next point's value. Tangent overshoot is not clamped.</remarks>
    /// <exception cref="ArgumentOutOfRangeException">The offset is nonfinite.</exception>
    /// <exception cref="ObjectDisposedException">The resource is disposed.</exception>
    public float Sample(float offset) { lock (_gate) { ThrowIfDisposed(); Finite(offset, nameof(offset)); return SampleCore(offset); } }
    /// <summary>Recomputes the evenly spaced sample cache without emitting an event.</summary>
    /// <exception cref="ObjectDisposedException">The resource is disposed.</exception>
    public void Bake() { lock (_gate) { ThrowIfDisposed(); BakeCore(); } }
    /// <summary>Samples the cached curve, baking lazily when dirty.</summary>
    /// <param name="offset">Finite horizontal position, clamped to the cache endpoints.</param>
    /// <returns>Linearly interpolated cached values, or zero for an empty curve.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The offset is nonfinite.</exception>
    /// <exception cref="ObjectDisposedException">The resource is disposed.</exception>
    public float SampleBaked(float offset)
    {
        lock (_gate)
        {
            ThrowIfDisposed(); Finite(offset, nameof(offset)); if (_dirty) BakeCore();
            if (_baked.Length == 0) return _points.Count == 0 ? 0 : _points[0].Position.Y;
            if (_baked.Length == 1 || offset >= _maxDomain) return _baked[^1];
            if (offset <= _minDomain) return _baked[0];
            var position = (offset - _minDomain) / (_maxDomain - _minDomain) * (_baked.Length - 1);
            var index = (int)MathF.Floor(position);
            return index + 1 < _baked.Length ? Mathf.Lerp(_baked[index], _baked[index + 1], position - index) : _baked[^1];
        }
    }

    private void SetLimit(float value, bool domain, bool minimum)
    {
        lock (_gate)
        {
            ThrowIfDisposed(); Finite(value, nameof(value));
            var low = domain ? _minDomain : _minValue; var high = domain ? _maxDomain : _maxValue;
            value = minimum ? Math.Min(value, high - .01f) : Math.Max(value, low + .01f);
            foreach (var point in _points)
            {
                var coordinate = domain ? point.Position.X : point.Position.Y;
                value = minimum ? Math.Min(value, coordinate) : Math.Max(value, coordinate);
            }
            var range = minimum ? high - value : value - low;
            if (!(range > 0) || !float.IsFinite(range)) throw new ArgumentOutOfRangeException(nameof(value), "The resulting range must be finite and positive.");
            if (domain) { if (minimum) _minDomain = value; else _maxDomain = value; _dirty = true; }
            else if (minimum) _minValue = value; else _maxValue = value;
        }
        if (domain) { EmitChanged(); if (!IsDisposed) DomainChanged?.Invoke(); }
        else RangeChanged?.Invoke();
    }

    private ref CurvePoint Point(int index) { ThrowIfDisposed(); if ((uint)index >= (uint)_points.Count) throw new ArgumentOutOfRangeException(nameof(index)); return ref CollectionsMarshal.AsSpan(_points)[index]; }
    private static void Finite(float value, string name) { if (!float.IsFinite(value)) throw new ArgumentOutOfRangeException(name); }
    private static void Mode(TangentMode mode) { if (mode is not (TangentMode.Free or TangentMode.Linear)) throw new ArgumentOutOfRangeException(nameof(mode)); }
    private static float Slope(Vector2 from, Vector2 to) => (float)(((double)to.Y - from.Y) / ((double)to.X - from.X));
    private int Insert(Vector2 position, float left, float right, TangentMode leftMode, TangentMode rightMode)
    {
        position = new(Math.Clamp(position.X, _minDomain, _maxDomain), Math.Clamp(position.Y, _minValue, _maxValue));
        var index = _points.Count == 0 ? 0 : _points.Count == 1 ? (position.X > _points[0].Position.X ? 1 : 0) :
            position.X < _points[0].Position.X ? 0 : FindIndex(position.X) + 1;
        _points.Insert(index, (position, left, right, leftMode, rightMode)); UpdateTangents(index); _dirty = true; return index;
    }
    private int FindIndex(float offset)
    {
        var first = 0; var last = _points.Count - 1;
        while (last - first > 1)
        {
            var middle = (first + last) / 2; var a = _points[middle].Position.X; var b = _points[middle + 1].Position.X;
            if (a < offset && b < offset) first = middle;
            else if (a > offset) last = middle;
            else return middle;
        }
        return offset > _points[last].Position.X ? last : first;
    }
    private void UpdateTangents(int index)
    {
        ref var point = ref Point(index);
        if (index > 0)
        {
            ref var previous = ref Point(index - 1); var slope = Slope(point.Position, previous.Position);
            if (point.LeftMode == TangentMode.Linear) point.Left = slope;
            if (previous.RightMode == TangentMode.Linear) previous.Right = slope;
        }
        if (index + 1 < _points.Count)
        {
            ref var next = ref Point(index + 1); var slope = Slope(point.Position, next.Position);
            if (point.RightMode == TangentMode.Linear) point.Right = slope;
            if (next.LeftMode == TangentMode.Linear) next.Left = slope;
        }
    }
    private void UpdateAllTangents() { for (var i = 0; i < _points.Count; i++) UpdateTangents(i); }
    private float SampleCore(float offset)
    {
        if (_points.Count == 0) return 0;
        if (_points.Count == 1) return _points[0].Position.Y;
        var index = FindIndex(offset); var a = _points[index];
        if (index == _points.Count - 1 || index == 0 && offset <= a.Position.X) return a.Position.Y;
        var b = _points[index + 1]; var distance = b.Position.X - a.Position.X;
        if (Mathf.IsZeroApprox(distance)) return b.Position.Y;
        var weight = (offset - a.Position.X) / distance; distance /= 3;
        return Mathf.BezierInterpolate(a.Position.Y, a.Position.Y + distance * a.Right, b.Position.Y - distance * b.Left, b.Position.Y, weight);
    }
    private void BakeCore()
    {
        var samples = new float[_bakeResolution];
        for (var i = 1; i < samples.Length - 1; i++) samples[i] = SampleCore((_maxDomain - _minDomain) * (i / (float)(samples.Length - 1)) + _minDomain);
        if (_points.Count > 0) { samples[0] = _points[0].Position.Y; samples[^1] = _points[^1].Position.Y; }
        _baked = samples; _dirty = false;
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
            yield return new PropertyDescriptor<Curve, Vector2>($"Point[{index}].Position", c => c.GetPointPosition(index),
                (c, v) => { c.SetPointValue(index, v.Y); if (!c.IsDisposed) c.SetPointOffset(index, v.X); }, _ => Vector2.Zero);
            if (index > 0)
            {
                yield return new PropertyDescriptor<Curve, float>($"Point[{index}].LeftTangent", c => c.GetPointLeftTangent(index), (c, v) => c.SetPointLeftTangent(index, v), _ => 0);
                yield return new PropertyDescriptor<Curve, TangentMode>($"Point[{index}].LeftMode", c => c.GetPointLeftMode(index), (c, v) => c.SetPointLeftMode(index, v), _ => TangentMode.Free);
            }
            if (index == 0 || index + 1 < count)
            {
                yield return new PropertyDescriptor<Curve, float>($"Point[{index}].RightTangent", c => c.GetPointRightTangent(index), (c, v) => c.SetPointRightTangent(index, v), _ => 0);
                yield return new PropertyDescriptor<Curve, TangentMode>($"Point[{index}].RightMode", c => c.GetPointRightMode(index), (c, v) => c.SetPointRightMode(index, v), _ => TangentMode.Free);
            }
        }
    }

    /// <inheritdoc />
    protected override Resource CreateDuplicateInstance() => new Curve();
    /// <inheritdoc />
    protected override void CopyCustomStateTo(Resource target, bool deep, DeepDuplicateMode subresourceMode, Func<Resource?, Resource?> duplicateSubresource, Func<Resource?, Resource?> forceDuplicateSubresource)
    {
        CurvePoint[] points; float minX, maxX, minY, maxY; int resolution;
        lock (_gate) { ThrowIfDisposed(); points = _points.ToArray(); minX = _minDomain; maxX = _maxDomain; minY = _minValue; maxY = _maxValue; resolution = _bakeResolution; }
        var copy = (Curve)target;
        lock (copy._gate) { copy.ThrowIfDisposed(); copy._points.Clear(); copy._points.AddRange(points); copy._minDomain = minX; copy._maxDomain = maxX; copy._minValue = minY; copy._maxValue = maxY; copy._bakeResolution = resolution; copy._baked = []; copy._dirty = true; }
    }
    /// <inheritdoc />
    protected override void OnResetState() { lock (_gate) { _baked = []; _dirty = true; } }
    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        if (disposing) lock (_gate) { _points.Clear(); _baked = []; DomainChanged = RangeChanged = null; }
        base.Dispose(disposing);
    }
}
