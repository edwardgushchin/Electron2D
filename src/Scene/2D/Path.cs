namespace Electron2D;

/// <summary>A spatial node containing a borrowed curve for direct PathFollow children.</summary>
/// <remarks>The curve is null initially and is not drawn automatically. Owner-thread curve changes update all
/// attached direct followers synchronously. Worker changes enqueue updates on the existing scene deferred queue;
/// no scene transform is changed on the worker. Disposing this node disconnects but does not dispose its curve.</remarks>
public class Path : Entity
{
    private PathCurve? _curve;
    private int _membershipVersion;
    private static readonly PropertyDescriptor[] PathProperties =
    [
        new PropertyDescriptor<Path, PathCurve?>(nameof(Curve), n => n.Curve, (n, v) => n.Curve = v, _ => null, stored: true),
    ];

    /// <summary>Creates a detached path with no curve.</summary>
    public Path() { }

    /// <summary>Gets or sets the borrowed local curve.</summary>
    /// <value>Null initially; null or zero-length curves leave followers' transforms unchanged.</value>
    /// <remarks>Every assignment reconnects Changed and updates attached direct followers, even for the same
    /// resource. Progress is not rewrapped or clamped when replacing/editing the curve. All eligible followers
    /// are attempted after callback failures. Detached paths defer sampling until followers enter a tree.</remarks>
    /// <exception cref="ObjectDisposedException">This node or the assigned curve is disposed.</exception>
    /// <exception cref="InvalidOperationException">Scene mutation is unavailable or curve geometry is invalid.</exception>
    /// <exception cref="AggregateException">A follower update fails after the curve has been assigned.</exception>
    public PathCurve? Curve
    {
        get { ThrowIfDisposed(); return _curve; }
        set
        {
            EnsureMutable();
            if (value?.IsDisposed == true) throw new ObjectDisposedException(nameof(value));
            if (_curve is not null) _curve.Changed -= CurveChanged;
            _curve = value;
            if (_curve is not null) _curve.Changed += CurveChanged;
            RefreshFollowers();
        }
    }

    private void CurveChanged(Resource source)
    {
        if (IsDisposed || !ReferenceEquals(_curve, source) || Tree is not { } tree) return;
        if (tree.IsOwnerThread) { RefreshFollowers(); return; }
        var version = Volatile.Read(ref _membershipVersion);
        try
        {
            tree.Defer(() =>
            {
                if (!IsDisposed && ReferenceEquals(Tree, tree) && version == _membershipVersion && ReferenceEquals(_curve, source)) RefreshFollowers();
            });
        }
        catch (ObjectDisposedException) { /* The tree has closed its queue; later entry samples the current curve. */ }
    }

    private void RefreshFollowers()
    {
        if (Tree is not { } tree) return;
        EnsureMutable();
        List<Exception>? errors = null;
        foreach (var child in Children.ToArray())
        {
            if (IsDisposed || !ReferenceEquals(Tree, tree)) break;
            if (child is not PathFollow follow || follow.IsDisposed || !ReferenceEquals(follow.Parent, this) || !ReferenceEquals(follow.Tree, tree)) continue;
            try { follow.UpdateFromPath(); }
            catch (Exception error) { CollectException(ref errors, error); }
        }
        ThrowCollected("Path follower updates failed.", errors);
    }

    internal override void OnTreeMembershipChanged(bool entering)
    {
        Interlocked.Increment(ref _membershipVersion);
        base.OnTreeMembershipChanged(entering);
    }

    /// <inheritdoc />
    /// <remarks>Appends the stored borrowed Curve resource; local-scene copying uses existing PackedScene rules.</remarks>
    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors() => base.GetPropertyDescriptors().Concat(PathProperties);
    /// <inheritdoc />
    protected override Func<Node> CreateSceneInstanceFactory() => GetType() == typeof(Path) ? CreatePath : base.CreateSceneInstanceFactory();
    private static Node CreatePath() => new Path();

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        if (disposing) { if (_curve is not null) _curve.Changed -= CurveChanged; _curve = null; Interlocked.Increment(ref _membershipVersion); }
        base.Dispose(disposing);
    }
}
