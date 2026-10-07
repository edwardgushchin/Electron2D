namespace Electron2D;

/// <summary>Transfers active physical body poses to scene bones and controls named physical simulators.</summary>
/// <remarks>Body paths resolve relative to its skeleton. Nodes, joints and shapes stay scene-owned; only
/// copied path configuration is resource-owned. Default idle execution observes completed physics poses.</remarks>
public sealed class SkeletonModificationPhysicalBones : SkeletonModification
{
    private string[] _paths = [];
    private SkeletonIKBinding[] _bindings = [];
    private bool _pending, _requested;
    private string[] _names = [];
    /// <summary>Creates an empty enabled idle-phase physical pose consumer.</summary>
    public SkeletonModificationPhysicalBones() { }
    /// <summary>Gets or resizes the copied physical body path list, retaining the prefix.</summary><value>Zero initially; zero through 4096. New slots have empty paths.</value>
    public int PhysicalBoneChainLength
    {
        get { lock (ModificationGate) { ThrowIfDisposed(); return _paths.Length; } }
        set { lock (ModificationGate) { EnsureModificationMutable(); if (value < 0 || value > 4096) throw new ArgumentOutOfRangeException(nameof(value)); var old = _paths.Length; Array.Resize(ref _paths, value); Array.Resize(ref _bindings, value); for (var i = old; i < value; i++) { _paths[i] = string.Empty; _bindings[i] = new(); } } NotifyPropertyListChanged(); EmitChanged(); }
    }
    private int Index(int index) { ThrowIfDisposed(); if ((uint)index >= (uint)_paths.Length) throw new ArgumentOutOfRangeException(nameof(index)); return index; }
    /// <summary>Returns a path from a consumer-list slot.</summary><param name="jointIndex">Valid slot, independent of the Skeleton bone index.</param><returns>Path relative to its skeleton.</returns>
    public string GetPhysicalBoneNode(int jointIndex) { lock (ModificationGate) return _paths[Index(jointIndex)]; }
    /// <summary>Authors a physical body path.</summary><param name="jointIndex">Valid consumer slot.</param><param name="physicalBoneNode">Relative bounded scene path.</param>
    public void SetPhysicalBoneNode(int jointIndex, string physicalBoneNode) { SkeletonIKBinding.Path(physicalBoneNode); lock (ModificationGate) { EnsureModificationMutable(); _paths[Index(jointIndex)] = physicalBoneNode; } EmitChanged(); }
    /// <summary>Replaces the list with breadth-first descendant PhysicalBone paths in the bound skeleton.</summary>
    /// <remarks>Requires an attached binding. Nested foreign rigs and invalid parent chains are omitted; no body is constructed.</remarks>
    public void FetchPhysicalBones()
    {
        lock (ModificationGate)
        {
            EnsureModificationMutable(); var owner = BoundSkeleton; if (owner is not { IsInsideTree: true }) throw new InvalidOperationException("Fetching physical bones requires an attached skeleton.");
            var queue = new Queue<Node>(); var paths = new List<string>(); queue.Enqueue(owner);
            while (queue.TryDequeue(out var node))
            {
                if (!ReferenceEquals(node, owner) && node is Skeleton) continue;
                if (node is PhysicalBone physical && physical.ResolveBone() is { } bone && ReferenceEquals(bone.SkeletonOwner, owner)) { if (paths.Count == 4096) throw new InvalidOperationException("Physical bone budget exceeded."); paths.Add(owner.GetPathTo(physical)); }
                for (var i = 0; i < node.GetChildCount(); i++) queue.Enqueue(node.GetChild(i));
            }
            SetPaths(paths.ToArray());
        }
        NotifyPropertyListChanged(); EmitChanged();
    }
    /// <summary>Requests simulation on all selected physical nodes or an ordinal set of node names.</summary><param name="bones">Copied physical node names; null or empty selects all.</param>
    /// <remarks>Applies immediately when attached, otherwise once at setup. Transition/fixture preparation is cold work.</remarks>
    public void StartSimulation(string[]? bones = null) => Request(true, bones);
    /// <summary>Requests follower mode on all selected physical nodes or an ordinal set of node names.</summary><param name="bones">Copied physical node names; null or empty selects all.</param>
    public void StopSimulation(string[]? bones = null) => Request(false, bones);
    private void Request(bool simulate, string[]? names)
    {
        if (names is { Length: > 4096 }) throw new ArgumentOutOfRangeException(nameof(names)); var copy = names is null || names.Length == 0 ? Array.Empty<string>() : (string[])names.Clone(); foreach (var name in copy) SkeletonIKBinding.Path(name);
        lock (ModificationGate) { EnsureModificationMutable(); _requested = simulate; _names = copy; _pending = true; if (BoundSkeleton is { IsInsideTree: true } owner) ApplyRequest(owner); }
    }
    private PhysicalBone? Resolve(Skeleton owner, int index) => _paths[index].Length == 0 ? null : _bindings[index].Resolve(owner, _paths[index]) as PhysicalBone;
    private void ApplyRequest(Skeleton owner)
    {
        if (!_pending) return;
        for (var i = 0; i < _paths.Length; i++) if (Resolve(owner, i) is { } physical && (_names.Length == 0 || Array.IndexOf(_names, physical.Name) >= 0) && physical.ResolveBone() is { } bone && ReferenceEquals(bone.SkeletonOwner, owner)) physical.SimulatePhysics = _requested;
        _pending = false;
    }
    /// <inheritdoc />
    protected override void OnSetupModification(SkeletonModificationStack modificationStack) { lock (ModificationGate) ApplyRequest(modificationStack.GetSkeleton()!); }
    /// <inheritdoc />
    protected override void OnExecute(double delta)
    {
        var stack = GetModificationStack(); var owner = BoundSkeleton; if (stack is null || owner is not { IsInsideTree: true }) return;
        lock (ModificationGate)
        {
            ApplyRequest(owner);
            for (var i = 0; i < _paths.Length; i++)
            {
                var physical = Resolve(owner, i); if (physical is null || !physical.IsSimulatingPhysics() || physical.FollowBoneWhenSimulating || physical.ResolveBone() is not { } bone || !ReferenceEquals(bone.SkeletonOwner, owner)) continue;
                bone.GlobalTransform = physical.GlobalTransform; SkeletonIKBinding.Request(owner, bone, stack.Strength);
            }
        }
    }
    private string[] StoredPaths { get { lock (ModificationGate) { ThrowIfDisposed(); return (string[])_paths.Clone(); } } set { ArgumentNullException.ThrowIfNull(value); if (value.Length > 4096) throw new ArgumentOutOfRangeException(nameof(value)); var copy = (string[])value.Clone(); foreach (var path in copy) SkeletonIKBinding.Path(path); lock (ModificationGate) { EnsureModificationMutable(); SetPaths(copy); } } }
    private void SetPaths(string[] paths) { _paths = paths; _bindings = new SkeletonIKBinding[paths.Length]; for (var i = 0; i < paths.Length; i++) _bindings[i] = new(); }
    private static readonly PropertyDescriptor[] PhysicalProperties =
    [
        new PropertyDescriptor<SkeletonModificationPhysicalBones, int>(nameof(PhysicalBoneChainLength), m => m.PhysicalBoneChainLength, (m, v) => m.PhysicalBoneChainLength = v, _ => 0),
        new PropertyDescriptor<SkeletonModificationPhysicalBones, string[]>("_physical_bone_paths", m => m.StoredPaths, (m, v) => m.StoredPaths = v, _ => [], stored: true)
    ];
    /// <inheritdoc />
    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors() => base.GetPropertyDescriptors().Concat(PhysicalProperties);
    /// <inheritdoc />
    protected override Resource CreateDuplicateInstance() => new SkeletonModificationPhysicalBones();
    /// <inheritdoc />
    protected override void CopyCustomStateTo(Resource target, bool deep, DeepDuplicateMode mode, Func<Resource?, Resource?> duplicateSubresource, Func<Resource?, Resource?> forceDuplicateSubresource)
    { var copy = (SkeletonModificationPhysicalBones)target; CopyModificationState(copy); lock (ModificationGate) copy.SetPaths((string[])_paths.Clone()); }
}
