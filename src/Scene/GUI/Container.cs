namespace Electron2D;

/// <summary>Base control for deferred arrangement of direct non-top-level child controls.</summary>
/// <remarks>Child sizing/visibility/flags/order changes coalesce sorting. Layout notifications and events run on
/// the owner thread; callback errors continue required phases. Native accessibility regions remain separate.</remarks>
public class Container : Control
{
    /// <summary>Identifies the notification immediately before a child arrangement pass.</summary>
    public const int NotificationPreSortChildren = 50;
    /// <summary>Identifies the child arrangement notification.</summary>
    public const int NotificationSortChildren = 51;
    private bool _pendingSort;
    private bool _sorting, _sortAgain;
    private SceneTree? _queuedTree;
    private Action? _sortAction;
    private ulong _sortGeneration;
    private static readonly PropertyDescriptor[] ContainerProperties =
    [
        new PropertyDescriptor<Container, MouseFilter>(nameof(MouseFilter), node => node.MouseFilter, (node, value) => node.MouseFilter = value, _ => MouseFilter.Pass, stored: true),
        new PropertyDescriptor<Container, bool>(nameof(PropagateMaximumSize), node => node.PropagateMaximumSize, (node, value) => node.PropagateMaximumSize = value, _ => true, stored: true)
    ];
    /// <summary>Creates a container with pass-through pointer input and maximum-size propagation enabled.</summary>
    public Container()
    {
        MouseFilter = MouseFilter.Pass; PropagateMaximumSize = true;
        ChildAdded += ChildEntered; ChildRemoved += ChildLeft; ChildOrderChanged += ChildOrder;
        MaximumSizeChanged += QueueSort;
    }
    /// <summary>Occurs after the pre-sort notification and before arrangement.</summary>
    public event Action? PreSortChildren;
    /// <summary>Occurs after the sort notification and concrete arrangement.</summary>
    public event Action? SortChildren;
    /// <summary>Queues one deferred child arrangement while attached.</summary>
    /// <remarks>Repeated requests coalesce. Requests during the current sort queue a later deferred pass without
    /// recursion. Detached requests do nothing.</remarks>
    /// <exception cref="InvalidOperationException">Mutation is off-owner or during scene capture.</exception>
    /// <exception cref="ObjectDisposedException">The container is disposed.</exception>
    public void QueueSort()
    {
        EnsureMutable(); if (!IsInsideTree) return;
        if (_pendingSort) { if (_sorting) _sortAgain = true; return; }
        _pendingSort = true; _queuedTree = Tree; Tree!.Defer(_sortAction!);
    }
    private void Sort(SceneTree expectedTree, ulong generation)
    {
        bool Current() => !IsDisposed && IsInsideTree && generation == _sortGeneration && ReferenceEquals(Tree, expectedTree) && ReferenceEquals(_queuedTree, expectedTree);
        if (!Current()) return;
        _sorting = true; _sortAgain = false;
        List<Exception>? errors = null;
        try
        {
            try { DispatchNotification(NotificationPreSortChildren); } catch (Exception error) { CollectException(ref errors, error); }
            try { if (Current()) PreSortChildren?.Invoke(); } catch (Exception error) { CollectException(ref errors, error); }
            try { if (Current()) DispatchNotification(NotificationSortChildren); } catch (Exception error) { CollectException(ref errors, error); }
            try { if (Current()) SortChildren?.Invoke(); } catch (Exception error) { CollectException(ref errors, error); }
        }
        finally
        {
            if (generation == _sortGeneration)
            {
                var again = _sortAgain;
                _sorting = false; _sortAgain = false; _pendingSort = false; _queuedTree = null;
                if (again && !IsDisposed && IsInsideTree) QueueSort();
            }
        }
        ThrowCollected("Container layout callbacks failed.", errors);
    }
    private void ChildEntered(Node _, Node child)
    {
        if (child is not Control control) return;
        control.SizeFlagsChanged += ChildSizeChanged; control.MinimumSizeChanged += ChildSizeChanged; control.MaximumSizeChanged += ChildSizeChanged; control.VisibilityChanged += ChildVisibility;
        ChildSizeChanged();
    }
    private void ChildLeft(Node _, Node child)
    {
        if (child is not Control control) return;
        control.SizeFlagsChanged -= ChildSizeChanged; control.MinimumSizeChanged -= ChildSizeChanged; control.MaximumSizeChanged -= ChildSizeChanged; control.VisibilityChanged -= ChildVisibility;
        control.ContainerMaximum = null; ChildSizeChanged();
    }
    private void ChildOrder(Node _) { if (!IsDisposed) ChildSizeChanged(); }
    private void ChildVisibility(CanvasItem _) => ChildSizeChanged();
    private void ChildSizeChanged() { if (IsDisposed) return; UpdateMinimumSize(); QueueSort(); }
    internal static bool Sortable(Node node, bool localVisibility = false) => node is Control { IsDisposed: false, TopLevel: false } control && (localVisibility ? control.Visible : control.IsVisibleInTree);
    /// <summary>Returns the available horizontal sizing choices for a host inspector.</summary>
    /// <returns>A caller-owned array. Choices are advisory and do not restrict stored flag bits.</returns>
    protected virtual SizeFlags[] GetAllowedSizeFlagsHorizontal() => [SizeFlags.Fill, SizeFlags.Expand, SizeFlags.ShrinkBegin, SizeFlags.ShrinkCenter, SizeFlags.ShrinkEnd];
    /// <summary>Returns the available vertical sizing choices for a host inspector.</summary>
    /// <returns>A caller-owned advisory array.</returns>
    protected virtual SizeFlags[] GetAllowedSizeFlagsVertical() => [SizeFlags.Fill, SizeFlags.Expand, SizeFlags.ShrinkBegin, SizeFlags.ShrinkCenter, SizeFlags.ShrinkEnd];
    /// <summary>Fits one direct child into its allocated local rectangle using fill/shrink flags.</summary>
    /// <param name="child">A live direct child control.</param>
    /// <param name="rect">Finite nonnegative destination allocation.</param>
    /// <remarks>Minimum/maximum bounds apply; end takes priority over center. Horizontal shrink respects RTL.
    /// Resets anchors to zero, assigns the rectangle in one reflow, then resets rotation and scale.</remarks>
    /// <exception cref="ArgumentNullException">The child is null.</exception>
    /// <exception cref="ArgumentException">The child is not live/direct or the rectangle is invalid.</exception>
    /// <exception cref="InvalidOperationException">Mutation is off-owner or capture-owned.</exception>
    /// <exception cref="ObjectDisposedException">The container is disposed.</exception>
    public void FitChildInRect(Control child, Rect2 rect)
    {
        EnsureMutable(); ArgumentNullException.ThrowIfNull(child);
        if (child.IsDisposed || !ReferenceEquals(child.Parent, this)) throw new ArgumentException("Layout requires a live direct child.", nameof(child));
        if (!rect.IsFinite() || rect.Size.X < 0 || rect.Size.Y < 0) throw new ArgumentException("Layout allocation must be finite and nonnegative.", nameof(rect));
        var minimum = child.GetBoundMinimumSize(); var position = rect.Position; var size = rect.Size;
        if ((child.SizeFlagsHorizontal & SizeFlags.Fill) == 0)
        {
            size.X = minimum.X;
            if ((child.SizeFlagsHorizontal & SizeFlags.ShrinkEnd) != 0) position.X += IsLayoutRTL() ? 0 : rect.Size.X - size.X;
            else if ((child.SizeFlagsHorizontal & SizeFlags.ShrinkCenter) != 0) position.X += MathF.Floor((rect.Size.X - size.X) / 2);
            else if (IsLayoutRTL()) position.X += rect.Size.X - size.X;
        }
        if ((child.SizeFlagsVertical & SizeFlags.Fill) == 0)
        {
            size.Y = minimum.Y;
            if ((child.SizeFlagsVertical & SizeFlags.ShrinkEnd) != 0) position.Y += rect.Size.Y - size.Y;
            else if ((child.SizeFlagsVertical & SizeFlags.ShrinkCenter) != 0) position.Y += MathF.Floor((rect.Size.Y - size.Y) / 2);
        }
        child.SetContainerRect(new(position, size)); child.Rotation = 0; child.Scale = Vector2.One;
    }
    /// <inheritdoc />
    protected override void OnNotification(int what)
    {
        try { base.OnNotification(what); }
        finally
        {
            if (!IsDisposed && (what is NotificationResized or NotificationLayoutDirectionChanged or NotificationThemeChanged || what == NotificationVisibilityChanged && IsVisibleInTree)) QueueSort();
        }
    }
    internal override void OnTreeMembershipChanged(bool entering)
    {
        _sortGeneration++;
        _sorting = false; _sortAgain = false;
        if (!entering) { _pendingSort = false; _queuedTree = null; _sortAction = null; }
        else
        {
            var expectedTree = Tree!; var generation = _sortGeneration;
            _sortAction = () => Sort(expectedTree, generation);
        }
        base.OnTreeMembershipChanged(entering);
        if (entering && !IsDisposed && IsInsideTree) QueueSort();
    }
    /// <inheritdoc />
    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors() => base.GetPropertyDescriptors().Where(property => property.Name != nameof(MouseFilter) && property.Name != nameof(PropagateMaximumSize)).Concat(ContainerProperties);
    /// <inheritdoc />
    protected override Func<Node> CreateSceneInstanceFactory() => GetType() == typeof(Container) ? CreateContainer : base.CreateSceneInstanceFactory();
    private static Node CreateContainer() => new Container();
    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        for (var index = 0; index < GetChildCount(includeInternal: true); index++) if (GetChild(index, includeInternal: true) is Control child)
            { child.SizeFlagsChanged -= ChildSizeChanged; child.MinimumSizeChanged -= ChildSizeChanged; child.MaximumSizeChanged -= ChildSizeChanged; child.VisibilityChanged -= ChildVisibility; child.ContainerMaximum = null; }
        try { base.Dispose(disposing); }
        finally { PreSortChildren = null; SortChildren = null; }
    }
}
