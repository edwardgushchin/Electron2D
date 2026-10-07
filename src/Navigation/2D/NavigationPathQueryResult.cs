namespace Electron2D;

/// <summary>Stores one atomically published typed path and its optional parallel point metadata.</summary>
/// <remarks>Caller-owned reusable result. Public arrays are copied both ways; setters remain independent source fields.
/// A successful query publishes all selected arrays/length together, before its optional callback. Reset clears arrays and length.</remarks>
public sealed class NavigationPathQueryResult : ElectronObject
{
    /// <summary>Primitive owning a returned path point.</summary>
    public enum PathSegmentType
    { /// <summary>Walkable navigation region.</summary>
        Region = 0, /// <summary>Off-surface navigation link.</summary>
        Link = 1
    }
    private readonly object _gate = new();
    private NavigationPathQueryData _data = NavigationPathQueryData.Empty;
    /// <summary>Creates an empty result with zero path length.</summary>
    public NavigationPathQueryResult() { }
    /// <summary>Gets or replaces a copied array of finite world-space path positions.</summary>
    /// <value>Empty initially; metadata arrays are empty when their query flags are disabled. Setters are independent.</value>
    /// <exception cref="ArgumentNullException">Assigned array is null.</exception>
    /// <exception cref="ObjectDisposedException">This result is disposed.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Positions are nonfinite or point types are unsupported.</exception>
    public Vector2[] Path { get { lock (_gate) { ThrowIfDisposed(); return (Vector2[])_data.Path.Clone(); } } set { ArgumentNullException.ThrowIfNull(value); var copy = (Vector2[])value.Clone(); foreach (var point in copy) if (!point.IsFinite()) throw new ArgumentOutOfRangeException(nameof(value)); lock (_gate) { ThrowIfDisposed(); _data = _data with { Path = copy }; } } }
    /// <summary>Gets or replaces a copied array of typed region/link owners of path points.</summary>
    /// <value>Empty initially; metadata arrays are empty when their query flags are disabled. Setters are independent.</value>
    /// <exception cref="ArgumentNullException">Assigned array is null.</exception>
    /// <exception cref="ObjectDisposedException">This result is disposed.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Positions are nonfinite or point types are unsupported.</exception>
    public PathSegmentType[] PathTypes { get { lock (_gate) { ThrowIfDisposed(); return (PathSegmentType[])_data.PathTypes.Clone(); } } set { ArgumentNullException.ThrowIfNull(value); var copy = (PathSegmentType[])value.Clone(); foreach (var type in copy) if (!Enum.IsDefined(type)) throw new ArgumentOutOfRangeException(nameof(value)); lock (_gate) { ThrowIfDisposed(); _data = _data with { PathTypes = copy }; } } }
    /// <summary>Gets or replaces a copied array of region/link RID owners of path points.</summary>
    /// <value>Empty initially; metadata arrays are empty when their query flags are disabled. Setters are independent.</value>
    /// <exception cref="ArgumentNullException">Assigned array is null.</exception>
    /// <exception cref="ObjectDisposedException">This result is disposed.</exception>
    public RID[] PathRIDs { get { lock (_gate) { ThrowIfDisposed(); return (RID[])_data.PathRIDs.Clone(); } } set { ArgumentNullException.ThrowIfNull(value); var copy = (RID[])value.Clone(); lock (_gate) { ThrowIfDisposed(); _data = _data with { PathRIDs = copy }; } } }
    /// <summary>Gets or replaces a copied array of logical instance identities associated with path point owners.</summary>
    /// <value>Empty initially; metadata arrays are empty when their query flags are disabled. Setters are independent.</value>
    /// <exception cref="ArgumentNullException">Assigned array is null.</exception>
    /// <exception cref="ObjectDisposedException">This result is disposed.</exception>
    public ulong[] PathOwnerIDs { get { lock (_gate) { ThrowIfDisposed(); return (ulong[])_data.PathOwnerIDs.Clone(); } } set { ArgumentNullException.ThrowIfNull(value); var copy = (ulong[])value.Clone(); lock (_gate) { ThrowIfDisposed(); _data = _data with { PathOwnerIDs = copy }; } } }
    /// <summary>Gets or replaces the independently stored world-space path length.</summary>
    /// <value>Zero initially and after reset; queries compute it after simplification and clipping.</value>
    /// <exception cref="ArgumentOutOfRangeException">Value is nonfinite or negative.</exception>
    /// <exception cref="ObjectDisposedException">This result is disposed.</exception>
    public float PathLength { get { lock (_gate) { ThrowIfDisposed(); return _data.PathLength; } } set { if (!float.IsFinite(value) || value < 0) throw new ArgumentOutOfRangeException(nameof(value)); lock (_gate) { ThrowIfDisposed(); _data = _data with { PathLength = value }; } } }
    /// <summary>Resets all arrays and length to their initial values.</summary>
    /// <exception cref="ObjectDisposedException">This result is disposed.</exception>
    public void Reset() => Publish(NavigationPathQueryData.Empty);
    internal void Publish(NavigationPathQueryData data) { lock (_gate) { ThrowIfDisposed(); _data = data; } }
    internal void EnsureQueryable() { lock (_gate) ThrowIfDisposed(); }
    /// <inheritdoc />
    protected override void Dispose(bool disposing) { if (disposing) lock (_gate) _data = NavigationPathQueryData.Empty; base.Dispose(disposing); }
    /// <inheritdoc />
    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors() => base.GetPropertyDescriptors().Concat(ResultProperties);
    private static readonly PropertyDescriptor[] ResultProperties =
    [new PropertyDescriptor<NavigationPathQueryResult,Vector2[]>(nameof(Path),n=>n.Path,(n,v)=>n.Path=v,_=>[]),
     new PropertyDescriptor<NavigationPathQueryResult,PathSegmentType[]>(nameof(PathTypes),n=>n.PathTypes,(n,v)=>n.PathTypes=v,_=>[]),
     new PropertyDescriptor<NavigationPathQueryResult,RID[]>(nameof(PathRIDs),n=>n.PathRIDs,(n,v)=>n.PathRIDs=v,_=>[]),
     new PropertyDescriptor<NavigationPathQueryResult,ulong[]>(nameof(PathOwnerIDs),n=>n.PathOwnerIDs,(n,v)=>n.PathOwnerIDs=v,_=>[]),
     new PropertyDescriptor<NavigationPathQueryResult,float>(nameof(PathLength),n=>n.PathLength,(n,v)=>n.PathLength=v,_=>0f)];
}
internal readonly record struct NavigationPathQueryData(Vector2[] Path, NavigationPathQueryResult.PathSegmentType[] PathTypes, RID[] PathRIDs, ulong[] PathOwnerIDs, float PathLength)
{ internal static NavigationPathQueryData Empty => new([], [], [], [], 0); }
