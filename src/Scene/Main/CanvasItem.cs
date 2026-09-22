namespace Electron2D;

/// <summary>Provides shared canvas drawing, visibility, materials and transform queries.</summary>
/// <remarks>Only direct canvas children inherit canvas state. Derived types define their own local transform model.</remarks>
public abstract partial class CanvasItem : Node
{
    /// <summary>Initializes a detached canvas item with visibility enabled, white modulation and no material.</summary>
    protected CanvasItem() { }

    /// <summary>Returns the local transform supplied by this item's placement model.</summary>
    /// <returns>The transform relative to the direct canvas parent.</returns>
    /// <exception cref="ObjectDisposedException">The item is disposed.</exception>
    public abstract Transform GetTransform();

    /// <summary>Returns the transform composed through the direct canvas-parent chain.</summary>
    /// <returns>The local transform when the parent is non-canvas or TopLevel is enabled.</returns>
    /// <exception cref="ObjectDisposedException">The item is disposed.</exception>
    public Transform GetGlobalTransform()
    {
        ThrowIfDisposed();
        return GetParentItem() is { } parent ? parent.GetGlobalTransform() * GetTransform() : GetTransform();
    }

    internal CanvasItem? GetParentItem() => TopLevel ? null : Parent as CanvasItem;

    /// <summary>Delivers enabled transform notifications after a derived placement model changes.</summary>
    /// <remarks>The local transform must be committed first. Descendant delivery stops at neutral nodes and
    /// top-level canvas items. All affected items are attempted before callback failures are aggregated.</remarks>
    /// <exception cref="InvalidOperationException">The caller is not the scene owner or a capture is active.</exception>
    /// <exception cref="ObjectDisposedException">The item is disposed.</exception>
    /// <exception cref="AggregateException">A notification or event handler fails.</exception>
    protected void NotifyLocalTransformChanged()
    {
        EnsureMutable();
        List<Exception>? errors = null;
        try { if (_notifyLocalTransformChanges) DispatchNotification(NotificationLocalTransformChanged); }
        catch (Exception error) { CollectException(ref errors, error); }
        try { LocalTransformChanged?.Invoke(this); }
        catch (Exception error) { CollectException(ref errors, error); }
        try { PropagateGlobalTransformChanged(); }
        catch (Exception error) { CollectException(ref errors, error); }
        ThrowCollected("Canvas transform callbacks failed.", errors);
    }

    /// <inheritdoc />
    protected override void OnNotification(int what)
    {
        if (what is not (NotificationParented or NotificationUnparented))
        {
            base.OnNotification(what);
            return;
        }
        List<Exception>? errors = null;
        try { base.OnNotification(what); }
        catch (Exception error) { CollectException(ref errors, error); }
        try { PropagateGlobalTransformChanged(); }
        catch (Exception error) { CollectException(ref errors, error); }
        try { PropagateVisibilityChanged(); }
        catch (Exception error) { CollectException(ref errors, error); }
        ThrowCollected("Canvas lifecycle callbacks failed.", errors);
    }

    /// <inheritdoc />
    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors() =>
        base.GetPropertyDescriptors().Concat(CanvasItemProperties).Concat(DrawingProperties);

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        try { base.Dispose(disposing); }
        finally
        {
            if (disposing)
            {
                _canvasCommands?.Clear(); _material = null;
                VisibilityChanged = null; LocalTransformChanged = null; TransformChanged = null;
            }
        }
    }

    private static readonly PropertyDescriptor[] CanvasItemProperties =
    [
        new PropertyDescriptor<CanvasItem, bool>(nameof(Visible), node => node.Visible, (node, value) => node.Visible = value, _ => true, stored: true),
        new PropertyDescriptor<CanvasItem, int>(
            nameof(ZIndex),
            node => node.ZIndex,
            (node, value) => node.ZIndex = value,
            _ => 0,
            (_, value) => value is >= MinimumZIndex and <= MaximumZIndex,
            stored: true),
        new PropertyDescriptor<CanvasItem, bool>(nameof(ZAsRelative), node => node.ZAsRelative, (node, value) => node.ZAsRelative = value, _ => true, stored: true),
        new PropertyDescriptor<CanvasItem, bool>(nameof(TopLevel), node => node.TopLevel, (node, value) => node.TopLevel = value, _ => false, stored: true)
    ];

    /// <summary>Identifies the notification propagated after local or inherited visibility changes.</summary>
    public const int NotificationVisibilityChanged = 31;

    /// <summary>Identifies a local-transform change notification when local notification delivery is enabled.</summary>
    public const int NotificationLocalTransformChanged = 35;

    /// <summary>Identifies a global-transform change notification when global notification delivery is enabled.</summary>
    public const int NotificationTransformChanged = 2000;

    /// <summary>Specifies the smallest supported local or effective Z index.</summary>
    public const int MinimumZIndex = -4096;

    /// <summary>Specifies the largest supported local or effective Z index.</summary>
    public const int MaximumZIndex = 4096;

    private bool _visible = true;

    private bool _zAsRelative = true;

    private bool _topLevel;

    private bool _notifyLocalTransformChanges;

    private bool _notifyTransformChanges;

    private int _zIndex;

    /// <summary>Gets or sets whether this node ignores its parent's transform.</summary>
    /// <value><see langword="false"/> by default.</value>
    /// <remarks>The local transform stays unchanged; global coordinates are recomputed against the new canvas boundary.</remarks>
    /// <exception cref="InvalidOperationException">Mutation occurs off the owner thread or during packed-scene capture.</exception>
    /// <exception cref="ObjectDisposedException">This node or an ancestor is disposing on another thread, or has finished disposing.</exception>
    /// <exception cref="Exception">A transform notification or event handler throws after the mode changes.</exception>
    public bool TopLevel
    {
        get
        {
            ThrowIfDisposed();
            return _topLevel;
        }
        set
        {
            EnsureMutable();

            if (_topLevel == value)
                return;

            _topLevel = value;
            PropagateGlobalTransformChanged();
        }
    }

    /// <summary>Gets or sets this node's local logical visibility.</summary>
    /// <value><see langword="true"/> by default.</value>
    /// <remarks>An actual change synchronously propagates visibility notifications and events through direct canvas descendants.</remarks>
    /// <exception cref="InvalidOperationException">An attached node is mutated off the owner thread.</exception>
    /// <exception cref="ObjectDisposedException">The node is disposing on another thread or has finished disposing.</exception>
    /// <exception cref="Exception">A visibility notification or event handler throws after visibility changes.</exception>
    public bool Visible
    {
        get
        {
            ThrowIfDisposed();
            return _visible;
        }
        set
        {
            EnsureMutable();

            if (_visible == value)
                return;

            _visible = value;
            PropagateVisibilityChanged();
        }
    }

    /// <summary>Gets whether this node is active and locally visible through its direct canvas ancestor chain.</summary>
    /// <value><see langword="true"/> only inside a tree when this node, its direct canvas ancestors and its window are visible.</value>
    /// <exception cref="ObjectDisposedException">This node or a queried ancestor is disposing on another thread, or has finished disposing.</exception>
    public bool IsVisibleInTree => IsInsideTree && Visible && ((Parent as CanvasItem)?.IsVisibleInTree ?? GetWindow()?.Visible ?? true);

    /// <summary>Gets or sets this node's local Z-order value.</summary>
    /// <value>An integer from <see cref="MinimumZIndex"/> through <see cref="MaximumZIndex"/>; the default is zero.</value>
    /// <exception cref="ArgumentOutOfRangeException">The assigned value is outside the supported range.</exception>
    /// <exception cref="InvalidOperationException">An attached node is mutated off the owner thread.</exception>
    /// <exception cref="ObjectDisposedException">The node is disposing on another thread or has finished disposing.</exception>
    public int ZIndex
    {
        get
        {
            ThrowIfDisposed();
            return _zIndex;
        }
        set
        {
            EnsureMutable();

            if (value is < MinimumZIndex or > MaximumZIndex)
                throw new ArgumentOutOfRangeException(nameof(value), value, $"Z index must be between {MinimumZIndex} and {MaximumZIndex}.");

            _zIndex = value;
        }
    }

    /// <summary>Gets or sets whether effective Z order accumulates ancestor Z values.</summary>
    /// <value><see langword="true"/> by default.</value>
    /// <exception cref="InvalidOperationException">An attached node is mutated off the owner thread.</exception>
    /// <exception cref="ObjectDisposedException">The node is disposing on another thread or has finished disposing.</exception>
    public bool ZAsRelative
    {
        get
        {
            ThrowIfDisposed();
            return _zAsRelative;
        }
        set
        {
            EnsureMutable();
            _zAsRelative = value;
        }
    }

    /// <summary>Gets the Z order after optional ancestor accumulation.</summary>
    /// <value>The accumulated or absolute value, clamped to the supported Z range.</value>
    /// <exception cref="ObjectDisposedException">This node or a queried ancestor is disposing on another thread, or has finished disposing.</exception>
    internal int EffectiveZIndex => ZAsRelative && GetParentItem() is { } parent
        ? Mathf.Clamp(parent.EffectiveZIndex + ZIndex, MinimumZIndex, MaximumZIndex)
        : ZIndex;

    /// <summary>Gets or sets whether local transform changes dispatch <see cref="NotificationLocalTransformChanged"/>.</summary>
    /// <value><see langword="false"/> by default. <see cref="LocalTransformChanged"/> is raised regardless.</value>
    /// <exception cref="InvalidOperationException">An attached node is mutated off the owner thread.</exception>
    /// <exception cref="ObjectDisposedException">The node is disposing on another thread or has finished disposing.</exception>
    public bool NotifyLocalTransformChanges
    {
        get
        {
            ThrowIfDisposed();
            return _notifyLocalTransformChanges;
        }
        set
        {
            EnsureMutable();
            _notifyLocalTransformChanges = value;
        }
    }

    /// <summary>Gets or sets whether global transform changes dispatch <see cref="NotificationTransformChanged"/>.</summary>
    /// <value><see langword="false"/> by default. <see cref="TransformChanged"/> is raised regardless.</value>
    /// <exception cref="InvalidOperationException">An attached node is mutated off the owner thread.</exception>
    /// <exception cref="ObjectDisposedException">The node is disposing on another thread or has finished disposing.</exception>
    public bool NotifyTransformChanges
    {
        get
        {
            ThrowIfDisposed();
            return _notifyTransformChanges;
        }
        set
        {
            EnsureMutable();
            _notifyTransformChanges = value;
        }
    }

    /// <summary>Occurs after local or inherited logical visibility is propagated to this node.</summary>
    /// <remarks>Delivery follows <see cref="NotificationVisibilityChanged"/> and continues through descendants.</remarks>
    public event Action<CanvasItem>? VisibilityChanged;

    /// <summary>Occurs after this node's local transform actually changes.</summary>
    /// <remarks>The event is always enabled; numeric local-transform notification delivery is separately configurable.</remarks>
    public event Action<CanvasItem>? LocalTransformChanged;

    /// <summary>Occurs when this node's global transform is affected by a local or ancestor change.</summary>
    /// <remarks>Propagation stops at top-level descendants. The event is independent of numeric transform notifications.</remarks>
    public event Action<CanvasItem>? TransformChanged;

    /// <summary>Moves this node to the last position among its siblings.</summary>
    /// <remarks>A detached or hierarchy-root node is left unchanged.</remarks>
    /// <exception cref="InvalidOperationException">An attached parent is mutated off the owner thread.</exception>
    /// <exception cref="ObjectDisposedException">This node or its parent is disposing on another thread or has finished disposing.</exception>
    /// <exception cref="AggregateException">One or more child-order or tree-change callbacks fail after the order changes.</exception>
    public void MoveToFront()
    {
        ThrowIfDisposed();
        Parent?.MoveChild(this, -1);
    }

    /// <summary>Sets <see cref="Visible"/> to <see langword="true"/>.</summary>
    /// <exception cref="InvalidOperationException">An attached node is mutated off the owner thread.</exception>
    /// <exception cref="ObjectDisposedException">This node is disposing on another thread or has finished disposing.</exception>
    /// <exception cref="Exception">A visibility notification or event handler throws after visibility changes.</exception>
    public void Show() => Visible = true;

    /// <summary>Sets <see cref="Visible"/> to <see langword="false"/>.</summary>
    /// <exception cref="InvalidOperationException">An attached node is mutated off the owner thread.</exception>
    /// <exception cref="ObjectDisposedException">This node is disposing on another thread or has finished disposing.</exception>
    /// <exception cref="Exception">A visibility notification or event handler throws after visibility changes.</exception>
    public void Hide() => Visible = false;

    private void PropagateGlobalTransformChanged()
    {
        List<Exception>? errors = null;
        try { if (_notifyTransformChanges) DispatchNotification(NotificationTransformChanged); }
        catch (Exception error) { CollectException(ref errors, error); }
        try { TransformChanged?.Invoke(this); }
        catch (Exception error) { CollectException(ref errors, error); }
        foreach (var child in Children.ToArray())
        {
            if (child is not CanvasItem { TopLevel: false } item || item.IsDisposed || !ReferenceEquals(item.Parent, this)) continue;
            try { item.PropagateGlobalTransformChanged(); }
            catch (Exception error) { CollectException(ref errors, error); }
        }
        ThrowCollected("Canvas transform callbacks failed.", errors);
    }

    internal void PropagateVisibilityChanged()
    {
        List<Exception>? errors = null;
        try { DispatchNotification(NotificationVisibilityChanged); }
        catch (Exception error) { CollectException(ref errors, error); }
        try { VisibilityChanged?.Invoke(this); }
        catch (Exception error) { CollectException(ref errors, error); }
        foreach (var child in Children.ToArray())
        {
            if (child is not CanvasItem item || item.IsDisposed || !ReferenceEquals(item.Parent, this)) continue;
            try { item.PropagateVisibilityChanged(); }
            catch (Exception error) { CollectException(ref errors, error); }
        }
        ThrowCollected("Canvas visibility callbacks failed.", errors);
    }
}
