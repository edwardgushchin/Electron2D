namespace Electron2D;

/// <summary>Borrows and executes a child modification stack on the same skeleton and matching phase.</summary>
/// <remarks>Child strength is independent of parent strength. Graph copies always duplicate the held stack,
/// preserving aliases. Cycles and excessive nesting reject; shared child binding lasts through its final holder.</remarks>
public sealed class SkeletonModificationStackHolder : SkeletonModification
{
    private SkeletonModificationStack? _held;
    private WeakReference<SkeletonModificationStack>? _bound;
    /// <summary>Creates an enabled idle-phase holder without a child.</summary>
    public SkeletonModificationStackHolder() { }
    /// <summary>Returns the borrowed child stack.</summary><returns>Null initially.</returns>
    public SkeletonModificationStack? GetHeldModificationStack() { lock (ModificationGate) { ThrowIfDisposed(); return _held; } }
    /// <summary>Assigns a borrowed child stack, committing before child setup callbacks.</summary>
    /// <param name="heldModificationStack">Live resource or null. Caller retains ownership.</param>
    /// <remarks>Replacement during setup/execution rejects. A graph containing this holder rejects before mutation.
    /// Child setup failure leaves the assigned graph reviewable and permits explicit parent Setup retry.</remarks>
    public void SetHeldModificationStack(SkeletonModificationStack? heldModificationStack)
    {
        lock (ModificationGate)
        {
            EnsureModificationMutable(); if (heldModificationStack is { IsDisposed: true }) throw new ObjectDisposedException(nameof(heldModificationStack));
            if (GetModificationStack() is { IsRunning: true }) throw new InvalidOperationException("A running holder cannot replace its child.");
            if (ReferenceEquals(_held, heldModificationStack)) return;
            ValidateGraph(heldModificationStack); if (BoundSkeleton is { } owner) heldModificationStack?.ValidateBinding(owner);
            OnUnbindStack(); _held = heldModificationStack;
            if (BoundSkeleton is { IsInsideTree: true } skeleton && _held is not null) try { Attach(skeleton); } catch { GetModificationStack()?.InvalidateSetup(); throw; }
        }
        EmitChanged();
    }
    private void ValidateGraph(SkeletonModificationStack? child)
    {
        if (child is null) return;
        var pending = new Stack<(SkeletonModificationStack Stack, int Depth)>(); var visited = new HashSet<SkeletonModificationStack>(); pending.Push((child, 1));
        while (pending.TryPop(out var frame))
        {
            if (frame.Depth > 128 || visited.Count > 16384) throw new InvalidOperationException("Held stack graph exceeds its budget."); if (!visited.Add(frame.Stack)) continue;
            for (var i = 0; i < frame.Stack.ModificationCount; i++)
            {
                var modification = frame.Stack.GetModification(i); if (ReferenceEquals(modification, this)) throw new InvalidOperationException("Held stacks cannot form a cycle.");
                if (modification is SkeletonModificationStackHolder holder && holder.GetHeldModificationStack() is { } nested) pending.Push((nested, frame.Depth + 1));
            }
        }
    }
    private void Attach(Skeleton owner) { if (_bound is null) _bound = new(_held!); else _bound.SetTarget(_held!); _held!.AcquireHolder(this, owner); }
    internal override void OnUnbindStack() { if (_bound is not null && _bound.TryGetTarget(out var stack) && !stack.IsDisposed) stack.ReleaseHolder(this); _bound = null; }
    /// <inheritdoc />
    protected override void OnSetupModification(SkeletonModificationStack modificationStack) { lock (ModificationGate) { OnUnbindStack(); ValidateGraph(_held); if (_held is not null) Attach(modificationStack.GetSkeleton()!); } }
    /// <inheritdoc />
    protected override void OnExecute(double delta) { lock (ModificationGate) _held?.Execute(delta, ExecutionMode); }
    private static readonly PropertyDescriptor[] HolderProperties =
    [new PropertyDescriptor<SkeletonModificationStackHolder, SkeletonModificationStack?>("_held_modification_stack", m => m.GetHeldModificationStack(), (m, v) => m.SetHeldModificationStack(v), _ => null, stored: true) { AlwaysDuplicateResource = true }];
    /// <inheritdoc />
    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors() => base.GetPropertyDescriptors().Concat(HolderProperties);
    /// <inheritdoc />
    protected override Resource CreateDuplicateInstance() => new SkeletonModificationStackHolder();
    /// <inheritdoc />
    protected override void CopyCustomStateTo(Resource target, bool deep, DeepDuplicateMode mode, Func<Resource?, Resource?> duplicateSubresource, Func<Resource?, Resource?> forceDuplicateSubresource)
    { var copy = (SkeletonModificationStackHolder)target; CopyModificationState(copy); lock (ModificationGate) copy._held = (SkeletonModificationStack?)forceDuplicateSubresource(_held); }
}
