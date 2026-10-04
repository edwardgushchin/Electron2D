namespace Electron2D;

/// <summary>A reusable 1D animation blend space with typed per-tree position and clock state.</summary>
/// <remarks>Points borrow root resources. At most 64 named points are supported. Authoring bounds/snap/labels do not clamp playback. Resource copies preserve child aliases; runtime state belongs to AnimationTree.</remarks>
public sealed class AnimationNodeBlendSpace1D : AnimationRootNode
{
    /// <summary>The finite per-tree blend position; defaults to zero.</summary>
    /// <exception cref="ObjectDisposedException">This resource has been disposed.</exception>
    public static readonly AnimationParameter<float> BlendPosition = new("blend_position", 0);
    private readonly AnimationBlendPoints<float> _points;
    private AnimationBlendMode _mode; private AnimationSyncMode _sync; private double _cycle;
    private float _min = -1, _max = 1, _snap = .1f;
    private string _label = "value";
    private static readonly PropertyDescriptor[] SpaceProperties =
    [
        new PropertyDescriptor<AnimationNodeBlendSpace1D, AnimationBlendMode>(nameof(BlendMode), n => n.BlendMode, (n, v) => n.BlendMode = v, _ => AnimationBlendMode.Interpolated),
        new PropertyDescriptor<AnimationNodeBlendSpace1D, AnimationSyncMode>(nameof(SyncMode), n => n.SyncMode, (n, v) => n.SyncMode = v, _ => AnimationSyncMode.None),
        new PropertyDescriptor<AnimationNodeBlendSpace1D, bool>(nameof(Sync), n => n.Sync, (n, v) => n.Sync = v, _ => false),
        new PropertyDescriptor<AnimationNodeBlendSpace1D, double>(nameof(CyclicLength), n => n.CyclicLength, (n, v) => n.CyclicLength = v, _ => 0),
        new PropertyDescriptor<AnimationNodeBlendSpace1D, float>(nameof(MinSpace), n => n.MinSpace, (n, v) => n.MinSpace = v, _ => -1),
        new PropertyDescriptor<AnimationNodeBlendSpace1D, float>(nameof(MaxSpace), n => n.MaxSpace, (n, v) => n.MaxSpace = v, _ => 1),
        new PropertyDescriptor<AnimationNodeBlendSpace1D, float>(nameof(Snap), n => n.Snap, (n, v) => n.Snap = v, _ => .1f),
        new PropertyDescriptor<AnimationNodeBlendSpace1D, string>(nameof(ValueLabel), n => n.ValueLabel, (n, v) => n.ValueLabel = v, _ => "value"),
    ];
    /// <summary>Creates an empty blend space with independent point storage.</summary>
    public AnimationNodeBlendSpace1D() { _points = new(this); }
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
    /// <value>The configured MinSpace value; initial value is -1.</value>
    /// <exception cref="ObjectDisposedException">The resource has been disposed.</exception>
    /// <exception cref="ArgumentOutOfRangeException">The enum/numeric value is invalid or nonfinite.</exception>
    public float MinSpace { get { ThrowIfDisposed(); return _min; } set { ThrowIfDisposed(); Animation.Finite(value); _min = value >= _max ? _max - 1 : value; } }
    /// <summary>Sets finite authoring maximum; crossing the minimum adjusts each affected component to min plus one.</summary>
    /// <value>The configured MaxSpace value; initial value is 1.</value>
    /// <exception cref="ObjectDisposedException">The resource has been disposed.</exception>
    /// <exception cref="ArgumentOutOfRangeException">The enum/numeric value is invalid or nonfinite.</exception>
    public float MaxSpace { get { ThrowIfDisposed(); return _max; } set { ThrowIfDisposed(); Animation.Finite(value); _max = value <= _min ? _min + 1 : value; } }
    /// <summary>Sets the finite authoring snap increment; it does not quantize runtime blending.</summary>
    /// <value>The configured Snap value; initial value is .1f.</value>
    /// <exception cref="ObjectDisposedException">The resource has been disposed.</exception>
    /// <exception cref="ArgumentOutOfRangeException">The enum/numeric value is invalid or nonfinite.</exception>
    public float Snap { get { ThrowIfDisposed(); return _snap; } set { ThrowIfDisposed(); Animation.Finite(value); _snap = value; } }
    /// <summary>Sets the authoring axis label.</summary>
    /// <value>The configured ValueLabel value; initial value is value.</value>
    /// <exception cref="ObjectDisposedException">The resource has been disposed.</exception>
    public string ValueLabel { get { ThrowIfDisposed(); return _label; } set { ThrowIfDisposed(); ArgumentNullException.ThrowIfNull(value); _label = value; } }
    /// <summary>Adds a borrowed point; empty names use the next unused safe numeric name, conflicts gain a numeric suffix.</summary>
    /// <param name="node">A live root resource without a containment cycle.</param>
    /// <param name="position">Finite point coordinates.</param>
    /// <param name="atIndex">Insertion index, or minus one to append.</param>
    /// <param name="name">Local name without slash/dot, or empty for a safe index.</param>
    /// <exception cref="ArgumentException">The name/node/position is invalid.</exception>
    /// <exception cref="InvalidOperationException">Capacity or resource-cycle validation fails.</exception>
    /// <exception cref="ObjectDisposedException">This resource has been disposed.</exception>
    public void AddBlendPoint(AnimationRootNode node, float position, int atIndex = -1, string name = "")
    { ThrowIfDisposed(); Animation.Finite(position); var index = _points.Add(node, position, atIndex, name); EmitGraphChanged(); }
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
    public float GetBlendPointPosition(int point) { ThrowIfDisposed(); return _points.Items[point].Position; }
    /// <summary>Changes finite point coordinates without clamping them to authoring bounds.</summary>
    /// <param name="point">An existing index.</param>
    /// <param name="position">The finite coordinates.</param>
    /// <exception cref="ObjectDisposedException">This resource has been disposed.</exception>
    /// <exception cref="ArgumentOutOfRangeException">An index or numeric value is outside its documented domain.</exception>
    public void SetBlendPointPosition(int point, float position) { ThrowIfDisposed(); Animation.Finite(position); _points.Items[point].Position = position; EmitGraphChanged(); }
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
    public void RemoveBlendPoint(int point) { ThrowIfDisposed(); var item = _points.Remove(point); try { Removed(InstanceID, item.Name); } finally { EmitGraphChanged(); } }
    /// <summary>Swaps two point entries, preserving their names/clocks and remapping triangle indices.</summary>
    /// <param name="fromIndex">An existing source index.</param>
    /// <param name="toIndex">An existing destination index.</param>
    /// <exception cref="ObjectDisposedException">This resource has been disposed.</exception>
    /// <exception cref="ArgumentOutOfRangeException">An index or numeric value is outside its documented domain.</exception>
    public void ReorderBlendPoint(int fromIndex, int toIndex) { ThrowIfDisposed(); var a = _points.Items[fromIndex]; var b = _points.Items[toIndex]; if (fromIndex == toIndex) return; (_points.Items[fromIndex], _points.Items[toIndex]) = (b, a); EmitGraphChanged(); }
    /// <inheritdoc />
    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors() => base.GetPropertyDescriptors().Concat(SpaceProperties);
    /// <inheritdoc />
    protected override IEnumerable<AnimationParameter> OnGetParameterList() => base.OnGetParameterList().Append(BlendPosition).Append(AnimationBlendSpacePlayback<float>.Closest);
    /// <inheritdoc />
    protected override IEnumerable<KeyValuePair<string, AnimationNode>> OnGetChildNodes() => _points.Children();
    /// <inheritdoc />
    protected override AnimationNode? OnGetChildByName(string name) { var i = _points.Find(name); return i < 0 ? null : _points.Items[i].Node; }
    /// <inheritdoc />
    protected override string OnGetCaption() => "BlendSpace1D";
    /// <inheritdoc />
    protected override Resource CreateDuplicateInstance() => new AnimationNodeBlendSpace1D();
    /// <inheritdoc />
    protected override void CopyCustomStateTo(Resource target, bool deep, DeepDuplicateMode mode, Func<Resource?, Resource?> duplicate, Func<Resource?, Resource?> force)
    { var copy = (AnimationNodeBlendSpace1D)target; CopyNodeState(copy); _points.CopyTo(copy._points, deep, duplicate); copy._mode = _mode; copy._sync = _sync; copy._cycle = _cycle; copy._min = _min; copy._max = _max; copy._snap = _snap; copy._label = _label; }
    /// <inheritdoc />
    protected override void Dispose(bool disposing) { if (disposing) { _points.Dispose(); } base.Dispose(disposing); }
    /// <inheritdoc />
    protected override double OnProcess(double time, bool seek, bool isExternalSeeking, bool testOnly)
    {
        var points = _points.Items; if (points.Count == 0) throw new InvalidOperationException("Blend space has no points.");
        Span<double> weights = stackalloc double[64]; weights.Clear(); var pos = GetParameter(BlendPosition); Animation.Finite(pos); var closest = 0;
        if (_mode == AnimationBlendMode.Interpolated)
        {
            var low = -1; var high = -1; for (var i = 0; i < points.Count; i++) { var p = points[i].Position; if (p <= pos) { if (low < 0 || p > points[low].Position) low = i; } else if (high < 0 || p < points[high].Position) high = i; }
            if (low < 0) weights[high] = 1; else if (high < 0) weights[low] = 1; else { var f = ((double)pos - points[low].Position) / ((double)points[high].Position - points[low].Position); weights[low] = 1 - f; weights[high] = f; }
            for (var i = 0; i < points.Count; i++) if (weights[i] >= weights[closest]) closest = i;
        }
        else { var best = double.PositiveInfinity; for (var i = 0; i < points.Count; i++) { var d = Math.Abs((double)points[i].Position - pos); if (d < best) { closest = i; best = d; } } weights[closest] = 1; }
        return AnimationBlendSpacePlayback<float>.Process(this, points, weights, closest, _mode, _sync, _cycle, points.Count == 1);
    }
}
