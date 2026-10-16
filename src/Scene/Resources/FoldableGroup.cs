namespace Electron2D;

/// <summary>Coordinates mutually exclusive foldable containers without owning its member nodes.</summary>
/// <remarks>Membership is runtime state. Copies contain only configuration; scene-local duplication creates an
/// independent group for each scene instance. Selection keeps other members folded. Container ordering is not a public guarantee.</remarks>
public class FoldableGroup : Resource
{
    private readonly object _gate = new();
    private readonly List<WeakReference<FoldableContainer>> _containers = [];
    private readonly Stack<List<(FoldableContainer Container, ulong Generation)>> _snapshots = [];
    private bool _allowFoldingAll;
    private ulong _selection;
    internal ulong BeginSelection() => ++_selection;
    internal bool CurrentSelection(ulong value) => value == _selection;
    private static readonly PropertyDescriptor[] GroupProperties =
    [
        new PropertyDescriptor<FoldableGroup,bool>(nameof(AllowFoldingAll),g=>g.AllowFoldingAll,(g,v)=>g.AllowFoldingAll=v,_=>false,stored:true),
        new PropertyDescriptor<FoldableGroup,bool>(nameof(ResourceLocalToScene),g=>g.ResourceLocalToScene,(g,v)=>g.ResourceLocalToScene=v,_=>true,stored:true)
    ];
    /// <summary>Creates a scene-local group which disallows folding its last expanded panel.</summary>
    public FoldableGroup() { ResourceLocalToScene = true; }
    /// <summary>Occurs after other members fold, before the requesting member commits expansion.</summary>
    public event Action<FoldableContainer>? Expanded;
    /// <summary>Gets or sets whether every group member may be folded.</summary>
    /// <value>False initially. Turning this off expands a member if all members are folded.</value>
    /// <exception cref="ObjectDisposedException">The group is disposed.</exception>
    public bool AllowFoldingAll
    {
        get { lock (_gate) { ThrowIfDisposed(); return _allowFoldingAll; } }
        set
        {
            ValidateMembers(); lock (_gate) _allowFoldingAll = value;
            if (!value && GetExpandedContainer() is null)
            {
                var members = TakeSnapshot();
                try { if (members.Count != 0 && Current(members[0])) members[0].Container.SetFoldedCore(false, forced: true); }
                finally { ReturnSnapshot(members); }
            }
        }
    }

    /// <summary>Returns a caller-owned snapshot of live containers assigned to this group.</summary>
    /// <returns>Borrowed member nodes; the returned array does not change with later membership changes.</returns>
    /// <exception cref="ObjectDisposedException">The group is disposed.</exception>
    public FoldableContainer[] GetContainers()
    {
        var snapshot = TakeSnapshot();
        try { var result = new FoldableContainer[snapshot.Count]; for (var i = 0; i < result.Length; i++) result[i] = snapshot[i].Container; return result; }
        finally { ReturnSnapshot(snapshot); }
    }
    /// <summary>Returns an expanded member, or null if all members are folded.</summary>
    /// <returns>The selected live member is borrowed.</returns>
    /// <exception cref="InvalidOperationException">An attached member is queried off its scene owner thread.</exception>
    /// <exception cref="ObjectDisposedException">The group is disposed.</exception>
    public FoldableContainer? GetExpandedContainer()
    {
        var snapshot = TakeSnapshot();
        try { foreach (var entry in snapshot) if (Current(entry) && !entry.Container.Folded) return entry.Container; return null; }
        finally { ReturnSnapshot(snapshot); }
    }
    internal void Add(WeakReference<FoldableContainer> container) { lock (_gate) { ThrowIfDisposed(); if (!_containers.Contains(container)) _containers.Add(container); } }
    internal void Remove(WeakReference<FoldableContainer> container) { lock (_gate) _containers.Remove(container); }
    internal void ValidateMembers()
    {
        lock (_gate)
        {
            ThrowIfDisposed();
            foreach (var weak in _containers) if (weak.TryGetTarget(out var b) && !b.IsDisposed && ReferenceEquals(b.MembershipGroup, this)) b.ValidateGroupMutation();
        }
    }
    private List<(FoldableContainer Container, ulong Generation)> TakeSnapshot()
    {
        lock (_gate)
        {
            ThrowIfDisposed(); var result = _snapshots.Count == 0 ? [] : _snapshots.Pop();
            for (var i = 0; i < _containers.Count;)
            {
                if (!_containers[i].TryGetTarget(out var b) || b.IsDisposed || !ReferenceEquals(b.MembershipGroup, this)) { _containers.RemoveAt(i); continue; }
                result.Add((b, b.MembershipGeneration)); i++;
            }
            return result;
        }
    }
    private void ReturnSnapshot(List<(FoldableContainer Container, ulong Generation)> snapshot) { snapshot.Clear(); lock (_gate) if (!IsDisposed) _snapshots.Push(snapshot); }
    private bool Current((FoldableContainer Container, ulong Generation) entry) => !entry.Container.IsDisposed && ReferenceEquals(entry.Container.MembershipGroup, this) && entry.Container.MembershipGeneration == entry.Generation;
    internal void FoldOthers(FoldableContainer source)
    {
        var snapshot = TakeSnapshot(); List<Exception>? errors = null; var generation = source.InteractionGeneration;
        try
        {
            foreach (var entry in snapshot)
            {
                if (source.IsDisposed || source.InteractionGeneration != generation || !ReferenceEquals(source.MembershipGroup, this)) break;
                if (!ReferenceEquals(entry.Container, source) && Current(entry))
                    try { entry.Container.SetFoldedCore(true, forced: true); } catch (Exception error) { Node.CollectException(ref errors, error); }
            }
        }
        finally { ReturnSnapshot(snapshot); }
        Node.ThrowCollected("Container group callbacks failed.", errors);
    }
    internal void NotifyExpanded(FoldableContainer container) { ThrowIfDisposed(); BeginEventEmission(Expanded)?.Invoke(container); }
    /// <inheritdoc />
    protected override Resource CreateDuplicateInstance() => GetType() == typeof(FoldableGroup) ? new FoldableGroup() : base.CreateDuplicateInstance();
    /// <inheritdoc />
    protected override void CopyCustomStateTo(Resource target, bool deep, DeepDuplicateMode subresourceMode, Func<Resource?, Resource?> duplicateSubresource, Func<Resource?, Resource?> forceDuplicateSubresource)
    {
        ((FoldableGroup)target).AllowFoldingAll = AllowFoldingAll;
    }
    /// <inheritdoc />
    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors() => base.GetPropertyDescriptors().Where(p => p.Name != nameof(ResourceLocalToScene)).Concat(GroupProperties);
    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        lock (_gate) { _containers.Clear(); _snapshots.Clear(); Expanded = null; }
        base.Dispose(disposing);
    }
}
