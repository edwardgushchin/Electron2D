namespace Electron2D;

/// <summary>A reusable 2D animation blend space with typed per-tree position and clock state.</summary>
/// <remarks>Points borrow root resources. At most 64 named points are supported. Authoring bounds/snap/labels do not clamp playback. Resource copies preserve child aliases; runtime state belongs to AnimationTree.</remarks>
public sealed class AnimationNodeBlendSpace2D : AnimationRootNode
{
    /// <summary>The finite per-tree blend position; defaults to zero.</summary>
    /// <exception cref="ObjectDisposedException">This resource has been disposed.</exception>
    public static readonly AnimationParameter<Vector2> BlendPosition = new("blend_position", Vector2.Zero);
    private readonly AnimationBlendPoints<Vector2> _points;
    private AnimationBlendMode _mode; private AnimationSyncMode _sync; private double _cycle;
    private Vector2 _min = new(-1, -1), _max = Vector2.One, _snap = new(.1f, .1f);
    private string _x = "x", _y = "y"; private bool _auto = true, _trianglesDirty = true;
    private readonly List<(int A, int B, int C)> _triangles = [];
    /// <summary>Occurs synchronously after an automatic triangle rebuild or a point reorder commits.</summary>
    public event Action? TrianglesUpdated;
    private static readonly PropertyDescriptor[] SpaceProperties =
    [
        new PropertyDescriptor<AnimationNodeBlendSpace2D, AnimationBlendMode>(nameof(BlendMode), n => n.BlendMode, (n, v) => n.BlendMode = v, _ => AnimationBlendMode.Interpolated),
        new PropertyDescriptor<AnimationNodeBlendSpace2D, AnimationSyncMode>(nameof(SyncMode), n => n.SyncMode, (n, v) => n.SyncMode = v, _ => AnimationSyncMode.None),
        new PropertyDescriptor<AnimationNodeBlendSpace2D, bool>(nameof(Sync), n => n.Sync, (n, v) => n.Sync = v, _ => false),
        new PropertyDescriptor<AnimationNodeBlendSpace2D, double>(nameof(CyclicLength), n => n.CyclicLength, (n, v) => n.CyclicLength = v, _ => 0),
        new PropertyDescriptor<AnimationNodeBlendSpace2D, Vector2>(nameof(MinSpace), n => n.MinSpace, (n, v) => n.MinSpace = v, _ => new Vector2(-1, -1)),
        new PropertyDescriptor<AnimationNodeBlendSpace2D, Vector2>(nameof(MaxSpace), n => n.MaxSpace, (n, v) => n.MaxSpace = v, _ => Vector2.One),
        new PropertyDescriptor<AnimationNodeBlendSpace2D, Vector2>(nameof(Snap), n => n.Snap, (n, v) => n.Snap = v, _ => new Vector2(.1f, .1f)),
        new PropertyDescriptor<AnimationNodeBlendSpace2D, string>(nameof(XLabel), n => n.XLabel, (n, v) => n.XLabel = v, _ => "x"),
        new PropertyDescriptor<AnimationNodeBlendSpace2D, string>(nameof(YLabel), n => n.YLabel, (n, v) => n.YLabel = v, _ => "y"),
        new PropertyDescriptor<AnimationNodeBlendSpace2D, bool>(nameof(AutoTriangles), n => n.AutoTriangles, (n, v) => n.AutoTriangles = v, _ => true),
    ];
    /// <summary>Creates an empty blend space with independent point storage.</summary>
    public AnimationNodeBlendSpace2D() { _points = new(this); }
    /// <summary>Selects interpolated, discrete or carry playback.</summary>
    /// <value>The configured BlendMode value; initial value is AnimationBlendMode.Interpolated.</value>
    /// <exception cref="ObjectDisposedException">The resource has been disposed.</exception>
    /// <exception cref="ArgumentOutOfRangeException">The enum/numeric value is invalid or nonfinite.</exception>
    public AnimationBlendMode BlendMode { get { ThrowIfDisposed(); return _mode; } set { ThrowIfDisposed(); Animation.Valid(value); _mode = value; EmitGraphChanged(); } }
    /// <summary>Selects inactive/cyclic clock synchronization; cyclic modes require clip leaves.</summary>
    /// <value>The configured SyncMode value; initial value is AnimationSyncMode.None.</value>
    /// <exception cref="ObjectDisposedException">The resource has been disposed.</exception>
    /// <exception cref="ArgumentOutOfRangeException">The enum/numeric value is invalid or nonfinite.</exception>
    public AnimationSyncMode SyncMode { get { ThrowIfDisposed(); return _sync; } set { ThrowIfDisposed(); Animation.Valid(value); _sync = value; EmitGraphChanged(); } }
    /// <summary>Projects SyncMode: true sets Independent, false sets None; reads true for every non-None mode.</summary>
    /// <value>The configured Sync value; initial value is false.</value>
    /// <exception cref="ObjectDisposedException">The resource has been disposed.</exception>
    public bool Sync { get { ThrowIfDisposed(); return _sync != AnimationSyncMode.None; } set { ThrowIfDisposed(); _sync = value ? AnimationSyncMode.Independent : AnimationSyncMode.None; EmitGraphChanged(); } }
    /// <summary>Sets the finite constant cycle length in seconds; nonpositive values freeze cyclic clocks.</summary>
    /// <value>The configured CyclicLength value; initial value is 0.</value>
    /// <exception cref="ObjectDisposedException">The resource has been disposed.</exception>
    /// <exception cref="ArgumentOutOfRangeException">The enum/numeric value is invalid or nonfinite.</exception>
    public double CyclicLength { get { ThrowIfDisposed(); return _cycle; } set { ThrowIfDisposed(); Animation.Finite(value); _cycle = value; EmitGraphChanged(); } }
    /// <summary>Sets finite authoring minimum; crossing the maximum adjusts each affected component to max minus one.</summary>
    /// <value>The configured MinSpace value; initial value is new Vector2(-1, -1).</value>
    /// <exception cref="ObjectDisposedException">The resource has been disposed.</exception>
    /// <exception cref="ArgumentOutOfRangeException">The enum/numeric value is invalid or nonfinite.</exception>
    public Vector2 MinSpace { get { ThrowIfDisposed(); return _min; } set { ThrowIfDisposed(); if (!value.IsFinite()) throw new ArgumentOutOfRangeException(nameof(value)); _min = new(value.X >= _max.X ? _max.X - 1 : value.X, value.Y >= _max.Y ? _max.Y - 1 : value.Y); } }
    /// <summary>Sets finite authoring maximum; crossing the minimum adjusts each affected component to min plus one.</summary>
    /// <value>The configured MaxSpace value; initial value is Vector2.One.</value>
    /// <exception cref="ObjectDisposedException">The resource has been disposed.</exception>
    /// <exception cref="ArgumentOutOfRangeException">The enum/numeric value is invalid or nonfinite.</exception>
    public Vector2 MaxSpace { get { ThrowIfDisposed(); return _max; } set { ThrowIfDisposed(); if (!value.IsFinite()) throw new ArgumentOutOfRangeException(nameof(value)); _max = new(value.X <= _min.X ? _min.X + 1 : value.X, value.Y <= _min.Y ? _min.Y + 1 : value.Y); } }
    /// <summary>Sets the finite authoring snap increment; it does not quantize runtime blending.</summary>
    /// <value>The configured Snap value; initial value is new Vector2(.1f, .1f).</value>
    /// <exception cref="ObjectDisposedException">The resource has been disposed.</exception>
    /// <exception cref="ArgumentOutOfRangeException">The enum/numeric value is invalid or nonfinite.</exception>
    public Vector2 Snap { get { ThrowIfDisposed(); return _snap; } set { ThrowIfDisposed(); if (!value.IsFinite()) throw new ArgumentOutOfRangeException(nameof(value)); _snap = value; } }
    /// <summary>Sets the authoring x-axis label.</summary>
    /// <value>The configured XLabel value; initial value is x.</value>
    /// <exception cref="ObjectDisposedException">The resource has been disposed.</exception>
    public string XLabel { get { ThrowIfDisposed(); return _x; } set { ThrowIfDisposed(); ArgumentNullException.ThrowIfNull(value); _x = value; } }
    /// <summary>Sets the authoring y-axis label.</summary>
    /// <value>The configured YLabel value; initial value is y.</value>
    /// <exception cref="ObjectDisposedException">The resource has been disposed.</exception>
    public string YLabel { get { ThrowIfDisposed(); return _y; } set { ThrowIfDisposed(); ArgumentNullException.ThrowIfNull(value); _y = value; } }
    /// <summary>Enables Delaunay triangulation rebuilt on the next triangle query or evaluation.</summary>
    /// <value>The configured AutoTriangles value; initial value is true.</value>
    /// <exception cref="ObjectDisposedException">The resource has been disposed.</exception>
    public bool AutoTriangles { get { ThrowIfDisposed(); return _auto; } set { ThrowIfDisposed(); if (_auto == value) return; _auto = value; if (value) _trianglesDirty = true; EmitGraphChanged(); } }
    /// <summary>Adds a borrowed point; empty names use the next unused safe numeric name, conflicts gain a numeric suffix.</summary>
    /// <param name="node">A live root resource without a containment cycle.</param>
    /// <param name="position">Finite point coordinates.</param>
    /// <param name="atIndex">Insertion index, or minus one to append.</param>
    /// <param name="name">Local name without slash/dot, or empty for a safe index.</param>
    /// <exception cref="ArgumentException">The name/node/position is invalid.</exception>
    /// <exception cref="InvalidOperationException">Capacity or resource-cycle validation fails.</exception>
    /// <exception cref="ObjectDisposedException">This resource has been disposed.</exception>
    public void AddBlendPoint(AnimationRootNode node, Vector2 position, int atIndex = -1, string name = "")
    { ThrowIfDisposed(); if (!position.IsFinite()) throw new ArgumentOutOfRangeException(nameof(position)); var index = _points.Add(node, position, atIndex, name); for (var i = 0; i < _triangles.Count; i++) { var t = _triangles[i]; _triangles[i] = (t.A >= index ? t.A + 1 : t.A, t.B >= index ? t.B + 1 : t.B, t.C >= index ? t.C + 1 : t.C); } _trianglesDirty = true; EmitGraphChanged(); }
    /// <summary>Returns the current point count.</summary>
    /// <returns>The count, from zero through 64.</returns>
    /// <exception cref="ObjectDisposedException">This resource has been disposed.</exception>
    public int GetBlendPointCount() { ThrowIfDisposed(); return _points.Items.Count; }
    /// <summary>Returns a borrowed point definition.</summary>
    /// <param name="point">An existing zero-based index.</param>
    /// <returns>The borrowed root resource.</returns>
    /// <exception cref="ObjectDisposedException">This resource has been disposed.</exception>
    /// <exception cref="ArgumentOutOfRangeException">An index or numeric value is outside its documented domain.</exception>
    public AnimationRootNode GetBlendPointNode(int point) { ThrowIfDisposed(); return _points.Items[point].Node; }
    /// <summary>Replaces a borrowed point definition, preserving its name and coordinates.</summary>
    /// <param name="point">An existing index.</param>
    /// <param name="node">A live root resource without a containment cycle.</param>
    /// <exception cref="ObjectDisposedException">This resource has been disposed.</exception>
    /// <exception cref="ArgumentOutOfRangeException">An index or numeric value is outside its documented domain.</exception>
    public void SetBlendPointNode(int point, AnimationRootNode node) { ThrowIfDisposed(); _points.SetNode(point, node); EmitGraphChanged(); }
    /// <summary>Returns the point coordinates.</summary>
    /// <param name="point">An existing index.</param>
    /// <returns>The finite stored coordinates.</returns>
    /// <exception cref="ObjectDisposedException">This resource has been disposed.</exception>
    /// <exception cref="ArgumentOutOfRangeException">An index or numeric value is outside its documented domain.</exception>
    public Vector2 GetBlendPointPosition(int point) { ThrowIfDisposed(); return _points.Items[point].Position; }
    /// <summary>Changes finite point coordinates without clamping them to authoring bounds.</summary>
    /// <param name="point">An existing index.</param>
    /// <param name="position">The finite coordinates.</param>
    /// <exception cref="ObjectDisposedException">This resource has been disposed.</exception>
    /// <exception cref="ArgumentOutOfRangeException">An index or numeric value is outside its documented domain.</exception>
    public void SetBlendPointPosition(int point, Vector2 position) { ThrowIfDisposed(); if (!position.IsFinite()) throw new ArgumentOutOfRangeException(nameof(position)); _points.Items[point].Position = position; _trianglesDirty = true; EmitGraphChanged(); }
    /// <summary>Returns an exact point name.</summary>
    /// <param name="point">An existing index.</param>
    /// <returns>The nonempty local name.</returns>
    /// <exception cref="ObjectDisposedException">This resource has been disposed.</exception>
    /// <exception cref="ArgumentOutOfRangeException">An index or numeric value is outside its documented domain.</exception>
    public string GetBlendPointName(int point) { ThrowIfDisposed(); return _points.Items[point].Name; }
    /// <summary>Returns the index for an ordinal name, or minus one.</summary>
    /// <param name="name">The non-null local name.</param>
    /// <returns>The index or minus one.</returns>
    /// <exception cref="ObjectDisposedException">This resource has been disposed.</exception>
    public int FindBlendPointByName(string name) { ThrowIfDisposed(); return _points.Find(name); }
    /// <summary>Renames a point, generating a unique suffix for conflicts and preserving per-tree child state.</summary>
    /// <param name="point">An existing index.</param>
    /// <param name="name">A nonempty local name without slash/dot.</param>
    /// <exception cref="ObjectDisposedException">This resource has been disposed.</exception>
    /// <exception cref="ArgumentOutOfRangeException">An index or numeric value is outside its documented domain.</exception>
    public void SetBlendPointName(int point, string name) { ThrowIfDisposed(); ArgumentException.ThrowIfNullOrEmpty(name); var item = _points.Items[point]; var next = _points.UniqueName(name, point, item); if (next == item.Name) return; var old = item.Name; item.Name = next; try { Renamed(InstanceID, old, next); } finally { EmitGraphChanged(); } }
    /// <summary>Removes a point without disposing its resource; referring triangles are removed and later indices shift.</summary>
    /// <param name="point">An existing index.</param>
    /// <exception cref="ObjectDisposedException">This resource has been disposed.</exception>
    /// <exception cref="ArgumentOutOfRangeException">An index or numeric value is outside its documented domain.</exception>
    public void RemoveBlendPoint(int point) { ThrowIfDisposed(); var item = _points.Remove(point); for (var i = _triangles.Count - 1; i >= 0; i--) { var t = _triangles[i]; if (t.A == point || t.B == point || t.C == point) _triangles.RemoveAt(i); else _triangles[i] = (t.A > point ? t.A - 1 : t.A, t.B > point ? t.B - 1 : t.B, t.C > point ? t.C - 1 : t.C); } _trianglesDirty = true; try { Removed(InstanceID, item.Name); } finally { EmitGraphChanged(); } }
    /// <summary>Swaps two point entries, preserving their names/clocks and remapping triangle indices.</summary>
    /// <param name="fromIndex">An existing source index.</param>
    /// <param name="toIndex">An existing destination index.</param>
    /// <exception cref="ObjectDisposedException">This resource has been disposed.</exception>
    /// <exception cref="ArgumentOutOfRangeException">An index or numeric value is outside its documented domain.</exception>
    public void ReorderBlendPoint(int fromIndex, int toIndex) { ThrowIfDisposed(); var a = _points.Items[fromIndex]; var b = _points.Items[toIndex]; if (fromIndex == toIndex) return; (_points.Items[fromIndex], _points.Items[toIndex]) = (b, a); int Swap(int i) => i == fromIndex ? toIndex : i == toIndex ? fromIndex : i; for (var i = 0; i < _triangles.Count; i++) { var t = _triangles[i]; _triangles[i] = Canonical(Swap(t.A), Swap(t.B), Swap(t.C)); } _trianglesDirty = true; try { EmitGraphChanged(); } finally { TrianglesUpdated?.Invoke(); } }
    /// <inheritdoc />
    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors() => base.GetPropertyDescriptors().Concat(SpaceProperties);
    /// <inheritdoc />
    protected override IEnumerable<AnimationParameter> OnGetParameterList() => base.OnGetParameterList().Append(BlendPosition).Append(AnimationBlendSpacePlayback<Vector2>.Closest);
    /// <inheritdoc />
    protected override IEnumerable<KeyValuePair<string, AnimationNode>> OnGetChildNodes() => _points.Children();
    /// <inheritdoc />
    protected override AnimationNode? OnGetChildByName(string name) { var i = _points.Find(name); return i < 0 ? null : _points.Items[i].Node; }
    /// <inheritdoc />
    protected override string OnGetCaption() => "BlendSpace2D";
    /// <inheritdoc />
    protected override Resource CreateDuplicateInstance() => new AnimationNodeBlendSpace2D();
    /// <inheritdoc />
    protected override void CopyCustomStateTo(Resource target, bool deep, DeepDuplicateMode mode, Func<Resource?, Resource?> duplicate, Func<Resource?, Resource?> force)
    { var copy = (AnimationNodeBlendSpace2D)target; CopyNodeState(copy); _points.CopyTo(copy._points, deep, duplicate); copy._mode = _mode; copy._sync = _sync; copy._cycle = _cycle; copy._min = _min; copy._max = _max; copy._snap = _snap; copy._x = _x; copy._y = _y; copy._auto = _auto; copy._triangles.Clear(); copy._triangles.AddRange(_triangles); copy._trianglesDirty = _trianglesDirty; }
    /// <inheritdoc />
    protected override void Dispose(bool disposing) { if (disposing) { _points.Dispose(); _triangles.Clear(); TrianglesUpdated = null; } base.Dispose(disposing); }
    /// <summary>Adds a canonical triangle of three distinct point indices; geometric degeneracy is permitted.</summary>
    /// <param name="x">The first point index.</param>
    /// <param name="y">The second point index.</param>
    /// <param name="z">The third point index.</param>
    /// <param name="atIndex">Insertion index, or minus one to append.</param>
    /// <exception cref="ArgumentException">Indices repeat or the triangle already exists.</exception>
    /// <exception cref="ArgumentOutOfRangeException">An index is outside its current collection.</exception>
    /// <exception cref="ObjectDisposedException">This resource has been disposed.</exception>
    /// <exception cref="InvalidOperationException">A rebuild observer changes the selected point topology before insertion.</exception>
    public void AddTriangle(int x, int y, int z, int atIndex = -1)
    {
        ThrowIfDisposed(); var px = _points.Items[x]; var py = _points.Items[y]; var pz = _points.Items[z];
        if (x == y || y == z || z == x) throw new ArgumentException("Triangle indices must be distinct.");
        UpdateTriangles(); ThrowIfDisposed(); if (x >= _points.Items.Count || y >= _points.Items.Count || z >= _points.Items.Count || !ReferenceEquals(_points.Items[x], px) || !ReferenceEquals(_points.Items[y], py) || !ReferenceEquals(_points.Items[z], pz)) throw new InvalidOperationException("Point topology changed during triangle preparation."); var t = Canonical(x, y, z); if (_triangles.Contains(t)) throw new ArgumentException("Triangle already exists.");
        if (atIndex < -1 || atIndex > _triangles.Count) throw new ArgumentOutOfRangeException(nameof(atIndex));
        _triangles.Insert(atIndex == -1 ? _triangles.Count : atIndex, t); EmitGraphChanged();
    }
    /// <summary>Returns the triangle count after any pending automatic rebuild.</summary>
    /// <returns>The current count.</returns>
    /// <exception cref="ObjectDisposedException">This resource has been disposed.</exception>
    public int GetTriangleCount() { ThrowIfDisposed(); UpdateTriangles(); ThrowIfDisposed(); return _triangles.Count; }
    /// <summary>Returns a triangle's canonical point index after pending automatic rebuilding.</summary>
    /// <param name="triangle">An existing triangle index.</param>
    /// <param name="point">Vertex index zero, one or two.</param>
    /// <returns>The referenced point index.</returns>
    /// <exception cref="ObjectDisposedException">This resource has been disposed.</exception>
    /// <exception cref="ArgumentOutOfRangeException">An index or numeric value is outside its documented domain.</exception>
    public int GetTrianglePoint(int triangle, int point) { ThrowIfDisposed(); UpdateTriangles(); ThrowIfDisposed(); var t = _triangles[triangle]; return point switch { 0 => t.A, 1 => t.B, 2 => t.C, _ => throw new ArgumentOutOfRangeException(nameof(point)) }; }
    /// <summary>Removes a triangle after any pending automatic rebuild.</summary>
    /// <param name="triangle">An existing triangle index.</param>
    /// <exception cref="ObjectDisposedException">This resource has been disposed.</exception>
    /// <exception cref="ArgumentOutOfRangeException">An index or numeric value is outside its documented domain.</exception>
    public void RemoveTriangle(int triangle) { ThrowIfDisposed(); UpdateTriangles(); ThrowIfDisposed(); _triangles.RemoveAt(triangle); EmitGraphChanged(); }
    private static (int A, int B, int C) Canonical(int a, int b, int c) { if (a > b) (a, b) = (b, a); if (b > c) (b, c) = (c, b); if (a > b) (a, b) = (b, a); return (a, b, c); }
    private void UpdateTriangles()
    {
        if (!_auto || !_trianglesDirty) return;
        var points = _points.Items; var positions = new Vector2[points.Count];
        if (points.Count != 0)
        {
            double minX = points[0].Position.X, maxX = minX, minY = points[0].Position.Y, maxY = minY;
            foreach (var point in points) { minX = Math.Min(minX, point.Position.X); maxX = Math.Max(maxX, point.Position.X); minY = Math.Min(minY, point.Position.Y); maxY = Math.Max(maxY, point.Position.Y); }
            var scale = Math.Max(maxX - minX, maxY - minY); if (scale == 0) scale = 1;
            for (var i = 0; i < points.Count; i++) positions[i] = new((float)(((double)points[i].Position.X - minX) / scale), (float)(((double)points[i].Position.Y - minY) / scale));
        }
        var indices = Geometry.TriangulateDelaunay(positions); var prepared = new List<(int A, int B, int C)>(indices.Length / 3);
        for (var i = 0; i < indices.Length; i += 3) { var t = Canonical(indices[i], indices[i + 1], indices[i + 2]); if (!prepared.Contains(t)) prepared.Add(t); }
        _triangles.Clear(); _triangles.AddRange(prepared); _trianglesDirty = false; TrianglesUpdated?.Invoke();
    }
    /// <inheritdoc />
    protected override double OnProcess(double time, bool seek, bool isExternalSeeking, bool testOnly)
    {
        var context = Current(); var generation = context.Tree.GraphGeneration; UpdateTriangles();
        if (IsDisposed || context.Tree.IsDisposed || generation != context.Tree.GraphGeneration) { context.HasTime = true; context.Result = default; return 0; }
        var points = _points.Items; if (points.Count == 0) throw new InvalidOperationException("Blend space has no points.");
        var pos = GetParameter(BlendPosition); if (!pos.IsFinite()) throw new ArgumentOutOfRangeException(nameof(BlendPosition));
        Span<double> weights = stackalloc double[64]; weights.Clear(); var closest = 0;
        if (_mode == AnimationBlendMode.Interpolated)
        {
            if (_triangles.Count == 0) { AnimationBlendSpacePlayback<Vector2>.Validate(points, _sync); context.HasTime = true; context.Result = default; return 0; }
            var selected = -1; var best = double.PositiveInfinity; double wa = 0, wb = 0, wc = 0;
            for (var i = 0; i < _triangles.Count; i++)
            {
                var t = _triangles[i]; var a = points[t.A].Position; var b = points[t.B].Position; var c = points[t.C].Position;
                if (Barycentric(pos, a, b, c, out var u, out var v, out var w)) { selected = i; wa = u; wb = v; wc = w; break; }
                for (var edge = 0; edge < 3; edge++)
                {
                    var start = edge == 0 ? a : edge == 1 ? b : c; var end = edge == 0 ? b : edge == 1 ? c : a;
                    double dx = (double)end.X - start.X, dy = (double)end.Y - start.Y, px = (double)pos.X - start.X, py = (double)pos.Y - start.Y;
                    var length = dx * dx + dy * dy; var f = length == 0 ? 0 : Math.Clamp((px * dx + py * dy) / length, 0, 1); var rx = px - dx * f; var ry = py - dy * f; var distance = rx * rx + ry * ry;
                    if (distance < best) { best = distance; selected = i; wa = edge == 0 ? 1 - f : edge == 2 ? f : 0; wb = edge == 0 ? f : edge == 1 ? 1 - f : 0; wc = edge == 1 ? f : edge == 2 ? 1 - f : 0; }
                }
            }
            var chosen = _triangles[selected]; weights[chosen.A] = wa; weights[chosen.B] = wb; weights[chosen.C] = wc;
            closest = chosen.A; if (wb >= wa) closest = chosen.B; if (wc >= weights[closest]) closest = chosen.C;
        }
        else { var best = double.PositiveInfinity; for (var i = 0; i < points.Count; i++) { double x = (double)points[i].Position.X - pos.X, y = (double)points[i].Position.Y - pos.Y; var distance = x * x + y * y; if (distance < best) { best = distance; closest = i; } } weights[closest] = 1; }
        return AnimationBlendSpacePlayback<Vector2>.Process(this, points, weights, closest, _mode, _sync, _cycle);
    }
    private static bool Barycentric(Vector2 p, Vector2 a, Vector2 b, Vector2 c, out double u, out double v, out double w)
    {
        double x0 = (double)b.X - a.X, y0 = (double)b.Y - a.Y, x1 = (double)c.X - a.X, y1 = (double)c.Y - a.Y, x2 = (double)p.X - a.X, y2 = (double)p.Y - a.Y;
        var determinant = x0 * y1 - x1 * y0; if (determinant == 0) { u = v = w = 0; return false; }
        v = (x2 * y1 - x1 * y2) / determinant; w = (x0 * y2 - x2 * y0) / determinant; u = 1 - v - w;
        return u >= 0 && v >= 0 && w >= 0;
    }
}
