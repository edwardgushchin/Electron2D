namespace Electron2D;

/// <summary>Selects which directed polygon edges cast a canvas shadow.</summary>
public enum OccluderCullMode
{
    /// <summary>Both sides of each edge cast shadows.</summary>
    Disabled = 0,
    /// <summary>Edges facing clockwise cast shadows.</summary>
    Clockwise = 1,
    /// <summary>Edges facing counterclockwise cast shadows.</summary>
    CounterClockwise = 2,
}

/// <summary>Stores a copied canvas shadow contour.</summary>
/// <remarks>Consumers borrow this resource. Empty and single-point contours have no edges.
/// Open contours omit the closing edge; closed contours also define the interior of the distance field.</remarks>
public sealed class OccluderPolygon : Resource
{
    private Vector2[] _polygon = [];
    private readonly object _ridGate = new();
    private RID _rid;
    /// <summary>Returns a stable borrowed contour identity independent of an active renderer.</summary>
    /// <returns>A borrowed RID until resource disposal; server mutation/free requires server ownership.</returns>
    public override RID GetRID() { lock (_ridGate) { ThrowIfDisposed(); return _rid.IsValid() ? _rid : _rid = RenderingProgramRegistry<OccluderPolygon>.Register(this); } }
    internal void BindRenderingRID(RID rid) { lock (_ridGate) { if (_rid.IsValid()) throw new InvalidOperationException("Contour already has a rendering identity."); _rid = rid; } }
    /// <inheritdoc />
    protected override void Dispose(bool disposing) { if (disposing) lock (_ridGate) { if (_rid.IsValid()) RenderingProgramRegistry<OccluderPolygon>.Remove(_rid); _rid = default; } base.Dispose(disposing); }
    private bool _closed = true;
    private OccluderCullMode _cullMode;

    /// <summary>Creates an empty closed contour with both edge sides enabled.</summary>
    public OccluderPolygon() { }

    /// <summary>Gets or sets whether the last point connects to the first.</summary>
    /// <value>True initially.</value>
    public bool Closed { get { ThrowIfDisposed(); return _closed; } set { ThrowIfDisposed(); if (_closed == value) return; _closed = value; EmitChanged(); } }

    /// <summary>Gets or sets the edge-facing shadow policy.</summary>
    /// <value>Disabled initially; the geometric contour remains available to the distance field.</value>
    /// <exception cref="ArgumentOutOfRangeException">The value is undefined.</exception>
    public OccluderCullMode CullMode
    {
        get { ThrowIfDisposed(); return _cullMode; }
        set { ThrowIfDisposed(); if (!Enum.IsDefined(value)) throw new ArgumentOutOfRangeException(nameof(value)); if (_cullMode == value) return; _cullMode = value; EmitChanged(); }
    }

    /// <summary>Gets an independent point array or replaces the contour with a copy.</summary>
    /// <value>An empty array initially; points use local scene units.</value>
    /// <exception cref="ArgumentNullException">The array is null.</exception>
    /// <exception cref="ArgumentException">A point or the contour bounds is not finite.</exception>
    public Vector2[] Polygon
    {
        get { ThrowIfDisposed(); return (Vector2[])_polygon.Clone(); }
        set
        {
            ThrowIfDisposed(); ArgumentNullException.ThrowIfNull(value);
            var copy = (Vector2[])value.Clone();
            var min = Vector2.Zero; var max = Vector2.Zero;
            for (var i = 0; i < copy.Length; i++)
            {
                var point = copy[i]; if (!point.IsFinite()) throw new ArgumentException("Contour points must be finite.", nameof(value));
                if (i == 0) min = max = point; else { min = min.Min(point); max = max.Max(point); }
            }
            if (!(max - min).IsFinite()) throw new ArgumentException("Contour bounds must be finite.", nameof(value));
            _polygon = copy; EmitChanged();
        }
    }

    internal ReadOnlySpan<Vector2> Points { get { ThrowIfDisposed(); return _polygon; } }

    /// <inheritdoc />
    protected override Resource CreateDuplicateInstance() => new OccluderPolygon();

    /// <inheritdoc />
    protected override void CopyCustomStateTo(Resource target, bool deep, DeepDuplicateMode subresourceMode,
        Func<Resource?, Resource?> duplicateSubresource, Func<Resource?, Resource?> forceDuplicateSubresource)
    {
        var copy = (OccluderPolygon)target; copy._polygon = (Vector2[])_polygon.Clone(); copy._closed = _closed; copy._cullMode = _cullMode;
    }

    /// <inheritdoc />
    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors() => base.GetPropertyDescriptors().Concat(
    [
        new PropertyDescriptor<OccluderPolygon, bool>(nameof(Closed), p => p.Closed, (p, v) => p.Closed = v, _ => true, stored: true),
        new PropertyDescriptor<OccluderPolygon, OccluderCullMode>(nameof(CullMode), p => p.CullMode, (p, v) => p.CullMode = v, _ => OccluderCullMode.Disabled, stored: true),
        new PropertyDescriptor<OccluderPolygon, Vector2[]>(nameof(Polygon), p => p.Polygon, (p, v) => p.Polygon = v, _ => [], stored: true),
    ]);
}
