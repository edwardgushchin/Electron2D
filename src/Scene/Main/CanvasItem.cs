namespace Electron2D;

/// <summary>Provides shared canvas drawing, visibility, materials and transform queries.</summary>
/// <remarks>Only direct canvas children inherit canvas state. Derived types define their own local transform model.</remarks>
public abstract partial class CanvasItem : Node
{
    /// <summary>Initializes a detached canvas item with visibility enabled, white modulation and no material.</summary>
    protected CanvasItem() => TransformQueueEntry = new(this);

    private ClipChildrenMode _clipChildren;
    /// <summary>Gets or sets how drawn alpha masks same-Z canvas descendants.</summary>
    /// <value>Disabled initially. Max is a sentinel and cannot be assigned.</value>
    /// <remarks>Only takes color from the children and alpha from this item's geometry, texture and tint.
    /// AndDraw additionally draws this item before children. Without recorded commands, children draw normally;
    /// without a captured same-Z child range, this item draws normally.
    /// CanvasGroup stores the policy but keeps its group compositor. Equal assignments are silent; changed
    /// assignments commit before warning refresh. Nested masks and groups share storage and cannot render together.</remarks>
    /// <exception cref="ArgumentOutOfRangeException">The mode is not an assignable enum value.</exception>
    /// <exception cref="InvalidOperationException">Access is off-owner or mutation occurs during scene capture.</exception>
    /// <exception cref="ObjectDisposedException">This node is disposed.</exception>
    /// <exception cref="Exception">A warning-refresh observer throws after the value commits.</exception>
    public ClipChildrenMode ClipChildren
    {
        get { ThrowIfDisposed(); Tree?.EnsureOwnerThread(); return _clipChildren; }
        set { EnsureMutable(); if (value is < ClipChildrenMode.Disabled or >= ClipChildrenMode.Max) throw new ArgumentOutOfRangeException(nameof(value)); if (_clipChildren == value) return; _clipChildren = value; UpdateConfigurationWarnings(); }
    }
    /// <inheritdoc />
    /// <remarks>Reports attached clipping and group ancestors through physical node ancestry.</remarks>
    public override string[] GetConfigurationWarnings()
    {
        var warnings = new List<string>(base.GetConfigurationWarnings());
        if (IsInsideTree && (ClipChildren != ClipChildrenMode.Disabled || this is CanvasGroup))
        {
            var clipping = false; var group = false;
            for (var node = Parent; node is not null && (!clipping || !group); node = node.Parent)
            {
                if (!clipping && node is CanvasItem { ClipChildren: not ClipChildrenMode.Disabled }) { warnings.Add($"Ancestor '{node.Name}' clips children; nested masks share a backbuffer and cannot compose independently."); clipping = true; }
                if (!group && node is CanvasGroup) { warnings.Add($"Ancestor '{node.Name}' is a CanvasGroup; nested groups and masks share a backbuffer and cannot compose independently."); group = true; }
            }
        }
        return warnings.ToArray();
    }

    internal readonly LinkedListNode<CanvasItem> TransformQueueEntry;
    private bool _globalTransformInvalid = true;
    private Transform _globalTransform;

    /// <summary>Returns the local transform supplied by this item's placement model.</summary>
    /// <returns>The transform relative to the direct canvas parent.</returns>
    /// <exception cref="InvalidOperationException">An attached item is queried off its scene owner thread; concrete overrides enforce this guard.</exception>
    /// <exception cref="ObjectDisposedException">The item is disposed.</exception>
    public abstract Transform GetTransform();

    /// <summary>Returns the current logical rendering transform, which may include a visual-only control offset.</summary>
    internal virtual Transform GetVisualTransform() => GetTransform();

    /// <summary>Returns the transform composed through the direct canvas-parent chain.</summary>
    /// <returns>The local transform when the parent is non-canvas or TopLevel is enabled.</returns>
    /// <remarks>Resolves global invalidation without consuming an already queued notification. Global values are
    /// current immediately; ForceUpdateTransform controls notification delivery, not mathematical composition.</remarks>
    /// <exception cref="InvalidOperationException">An attached item is queried off-owner.</exception>
    /// <exception cref="ObjectDisposedException">The item is disposed.</exception>
    public Transform GetGlobalTransform()
    {
        ThrowIfDisposed();
        Tree?.EnsureOwnerThread();
        if (_globalTransformInvalid)
        {
            _globalTransform = GetParentItem() is { } parent ? parent.GetGlobalTransform() * GetTransform() : GetTransform();
            _globalTransformInvalid = false;
        }
        return _globalTransform;
    }

    internal Viewport? CanvasViewport => _canvasLayer?.CanvasViewport ?? GetViewport();

    internal CanvasItem? GetParentItem() => TopLevel ? null : Parent as CanvasItem;

    /// <summary>Reports a change that may affect this item's local drawing bounds.</summary>
    /// <param name="sizeChanged">Whether to request redraw before delivering the event; defaults to true.</param>
    /// <remarks>Commit the geometry first. Delivery is synchronous, including while hidden or detached, and does not
    /// propagate to children. Redraw follows QueueRedraw rules. A throwing subscriber stops later subscribers.</remarks>
    /// <exception cref="InvalidOperationException">The caller is not the scene owner or a capture is active.</exception>
    /// <exception cref="ObjectDisposedException">The item is disposed.</exception>
    /// <exception cref="Exception">An ItemRectChanged subscriber throws.</exception>
    protected void NotifyItemRectChanged(bool sizeChanged = true)
    {
        EnsureMutable();
        if (sizeChanged) QueueRedraw();
        ItemRectChanged?.Invoke(this);
    }

    /// <summary>Invalidates global transforms and delivers an enabled local-transform notification.</summary>
    /// <remarks>Commit the local transform first. Global invalidation stops at neutral and TopLevel descendants.
    /// Global notifications coalesce in the scene queue; local notification and its typed event are synchronous,
    /// only while attached and NotifyLocalTransformChanges is enabled. Even equal assignments notify.</remarks>
    /// <exception cref="InvalidOperationException">The caller is not the scene owner or a capture is active.</exception>
    /// <exception cref="ObjectDisposedException">The item is disposed.</exception>
    /// <exception cref="Exception">A local notification callback throws after global invalidation.</exception>
    protected void NotifyLocalTransformChanged()
    {
        EnsureMutable();
        PropagateGlobalTransformChanged();
        if (Tree is { IsPhysicsInterpolationActive: true, IsInPhysicsFrame: false }) ResetInterpolationSnapshot();
        if (IsInsideTree && _notifyLocalTransformChanges) DispatchNotification(NotificationLocalTransformChanged);
    }

    /// <summary>Immediately delivers this item's pending global-transform notification, if any.</summary>
    /// <remarks>Requires active tree membership. Removes the pending entry before calling user code. Does not
    /// flush descendants, change the transform, clear its invalidation state or request redraw. A second call
    /// without a new pending entry does nothing. Callback failure leaves the consumed entry removed.</remarks>
    /// <exception cref="InvalidOperationException">The item is detached, accessed off-owner, or a scene capture is active.</exception>
    /// <exception cref="ObjectDisposedException">The item is disposed.</exception>
    /// <exception cref="Exception">A transform notification callback throws.</exception>
    public void ForceUpdateTransform()
    {
        EnsureMutable();
        var tree = Tree ?? throw new InvalidOperationException("A transform update requires an active scene tree.");
        if (TransformQueueEntry.List is null) return;
        tree.CancelTransformNotification(this);
        tree.DeliverTransformNotification(this);
    }

    /// <inheritdoc />
    protected override void OnNotification(int what)
    {
        if (what == NotificationResetPhysicsInterpolation)
        {
            ResetInterpolationSnapshot();
            base.OnNotification(what);
            return;
        }
        if (what is NotificationVisibilityChanged or NotificationLocalTransformChanged or NotificationTransformChanged)
        {
            base.OnNotification(what);
            if (what == NotificationVisibilityChanged) VisibilityChanged?.Invoke(this);
            else if (what == NotificationLocalTransformChanged) LocalTransformChanged?.Invoke(this);
            else TransformChanged?.Invoke(this);
            return;
        }
        if (what is not (NotificationParented or NotificationUnparented))
        {
            base.OnNotification(what);
            return;
        }
        List<Exception>? errors = null;
        try { base.OnNotification(what); }
        catch (Exception error) { CollectException(ref errors, error); }
        try { if (what == NotificationParented) NotifyLocalTransformChanged(); else PropagateGlobalTransformChanged(); }
        catch (Exception error) { CollectException(ref errors, error); }
        ThrowCollected("Canvas lifecycle callbacks failed.", errors);
    }

    private bool _inCanvas;
    private bool _parentVisible;
    private bool _rebindingCanvas;
    private CanvasLayer? _canvasLayer;

    internal override void OnTreeMembershipChanged(bool entering)
    {
        if (!entering)
        {
            _interpolationValid = false;
            Tree!.CancelTransformNotification(this);
            _globalTransformInvalid = true;
            try { ExitCanvas(); }
            finally { _parentVisible = false; }
            return;
        }
        _globalTransformInvalid = true;
        _parentVisible = GetParentVisibility();
        List<Exception>? errors = null;
        try { EnterCanvas(); }
        catch (Exception error) { CollectException(ref errors, error); }
        try { if (IsVisibleInTree) DispatchNotification(NotificationVisibilityChanged); }
        catch (Exception error) { CollectException(ref errors, error); }
        if (!IsDisposed && IsInsideTree) Tree!.QueueTransformNotification(this);
        ThrowCollected("Canvas entry callbacks failed.", errors);
    }

    private void EnterCanvas()
    {
        if (IsDisposed || _inCanvas || !IsInsideTree) return;
        _inCanvas = true;
        _canvasLayer = GetParentItem()?._canvasLayer;
        if (_canvasLayer is null)
            for (var ancestor = Parent; ancestor is not null && ancestor is not Viewport; ancestor = ancestor.Parent)
                if (ancestor is CanvasLayer layer) { _canvasLayer = layer; break; }
        UpdateTextureSampling(filter: true); UpdateTextureSampling(filter: false);
        InvalidateCanvas();
        DispatchNotification(NotificationEnterCanvas);
    }

    private void ExitCanvas()
    {
        if (!_inCanvas) return;
        _inCanvas = false;
        try { DispatchNotification(NotificationExitCanvas); }
        finally { _canvasLayer = null; }
    }

    /// <inheritdoc />
    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors() =>
        base.GetPropertyDescriptors().Concat(CanvasItemProperties).Concat(DrawingProperties).Concat(SamplingProperties);

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        try { base.Dispose(disposing); }
        finally
        {
            if (disposing)
            {
                lock (_canvasItemRIDGate) { if (_canvasItemRID.IsValid()) RenderingCanvasItemRegistry.Remove(_canvasItemRID); _canvasItemRID = AttachedSkeleton = default; }
                _canvasCommands?.Clear(); _meshes?.Clear(); _meshCount = 0; _multiMeshes?.Clear(); _multiMeshCount = 0; _polygons?.Clear(); _polygonCount = 0; _strokes?.Clear(); _strokePoints = []; _strokeCount = 0; _material = null;
                VisibilityChanged = null; Hidden = null; Draw = null; ItemRectChanged = null; LocalTransformChanged = null; TransformChanged = null;
            }
        }
    }

    private static readonly PropertyDescriptor[] CanvasItemProperties =
    [
        new PropertyDescriptor<CanvasItem, ClipChildrenMode>(nameof(ClipChildren), n => n.ClipChildren, (n, v) => n.ClipChildren = v, _ => ClipChildrenMode.Disabled, (_, v) => v is >= ClipChildrenMode.Disabled and < ClipChildrenMode.Max, stored: true),
        new PropertyDescriptor<CanvasItem, bool>(nameof(ShowBehindParent), node => node.ShowBehindParent, (node, value) => node.ShowBehindParent = value, _ => false, stored: true),
        new PropertyDescriptor<CanvasItem, bool>(nameof(YSortEnabled), node => node.YSortEnabled, (node, value) => node.YSortEnabled = value, _ => false, stored: true),
        new PropertyDescriptor<CanvasItem, bool>(nameof(Visible), node => node.Visible, (node, value) => node.Visible = value, _ => true, stored: true),
        new PropertyDescriptor<CanvasItem, uint>(nameof(VisibilityLayer), node => node.VisibilityLayer, (node, value) => node.VisibilityLayer = value, _ => 1u, stored: true),
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

    /// <summary>Identifies the drawing notification delivered before Draw and OnDraw.</summary>
    public const int NotificationDraw = 30;

    /// <summary>Identifies canvas attachment, delivered parent-first during tree entry and after TopLevel rebinding.</summary>
    public const int NotificationEnterCanvas = 32;

    /// <summary>Identifies canvas detachment, delivered child-first during tree exit and before TopLevel rebinding.</summary>
    /// <remarks>Overrides use ordinary C# virtual/base dispatch; no automatic reverse inheritance dispatch is performed.</remarks>
    public const int NotificationExitCanvas = 33;

    /// <summary>Identifies the notification propagated after local or inherited visibility changes.</summary>
    public const int NotificationVisibilityChanged = 31;

    /// <summary>Identifies a local-transform change notification when local notification delivery is enabled.</summary>
    public const int NotificationLocalTransformChanged = 35;

    /// <summary>Identifies a global-transform notification queued on tree entry or on enabled global invalidation.</summary>
    public const int NotificationTransformChanged = 2000;

    /// <summary>Specifies the smallest supported local or effective Z index.</summary>
    public const int MinimumZIndex = -4096;

    /// <summary>Specifies the largest supported local or effective Z index.</summary>
    public const int MaximumZIndex = 4096;

    private bool _visible = true;
    private uint _visibilityLayer = 1;

    /// <summary>Gets or sets the rendering visibility bits tested against the viewport's canvas cull mask.</summary>
    /// <value>One initially; all 32 bits are available. Zero excludes this item's canvas subtree from rendering.</value>
    /// <remarks>Each direct canvas ancestor must independently intersect the viewport mask. Masks are not
    /// inherited or combined with each other. TopLevel, neutral-node and CanvasLayer boundaries start independent
    /// canvas roots. Changes affect retained submission without redraw, visibility events or changes to Visible,
    /// IsVisibleInTree, input or processing. Logically visible items still record their pending drawing commands.</remarks>
    /// <exception cref="InvalidOperationException">An attached item is accessed off-owner or mutated during scene capture.</exception>
    /// <exception cref="ObjectDisposedException">The item is disposed.</exception>
    public uint VisibilityLayer
    {
        get { ThrowIfDisposed(); Tree?.EnsureOwnerThread(); return _visibilityLayer; }
        set { EnsureMutable(); _visibilityLayer = value; }
    }

    /// <summary>Returns whether a zero-based rendering visibility bit is enabled.</summary>
    /// <param name="layer">Bit index from zero through 31, inclusive.</param>
    /// <returns>True when the selected bit in VisibilityLayer is set.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The bit index is outside zero through 31.</exception>
    /// <exception cref="InvalidOperationException">An attached item is queried off-owner.</exception>
    /// <exception cref="ObjectDisposedException">The item is disposed.</exception>
    public bool GetVisibilityLayerBit(int layer)
    {
        var mask = VisibilityLayer;
        if ((uint)layer >= 32) throw new ArgumentOutOfRangeException(nameof(layer));
        return (mask & (1u << layer)) != 0;
    }

    /// <summary>Changes one rendering visibility bit without changing the other bits.</summary>
    /// <param name="layer">Bit index from zero through 31, inclusive.</param>
    /// <param name="enabled">True to enable the bit; false to clear it.</param>
    /// <remarks>Does not request redraw or emit visibility events.</remarks>
    /// <exception cref="ArgumentOutOfRangeException">The bit index is outside zero through 31.</exception>
    /// <exception cref="InvalidOperationException">An attached item is mutated off-owner or during scene capture.</exception>
    /// <exception cref="ObjectDisposedException">The item is disposed.</exception>
    public void SetVisibilityLayerBit(int layer, bool enabled)
    {
        EnsureMutable();
        if ((uint)layer >= 32) throw new ArgumentOutOfRangeException(nameof(layer));
        var bit = 1u << layer;
        _visibilityLayer = enabled ? _visibilityLayer | bit : _visibilityLayer & ~bit;
    }

    private bool _zAsRelative = true;

    private bool _topLevel;

    private bool _notifyLocalTransformChanges;

    private bool _notifyTransformChanges;

    private int _zIndex;

    private bool _showBehindParent;
    private bool _ySortEnabled;

    /// <summary>Gets or sets whether this canvas subtree draws before its canvas parent.</summary>
    /// <value>False by default.</value>
    /// <remarks>Effective Z remains the primary ordering key within one canvas. A parent sorting its children by Y uses their Y
    /// positions instead of this flag. Neutral parents and TopLevel items have no canvas parent to draw behind.
    /// Changes affect the next submission without requiring QueueRedraw.</remarks>
    /// <exception cref="InvalidOperationException">Attached access is off the owner thread, or mutation occurs during scene capture.</exception>
    /// <exception cref="ObjectDisposedException">The item is disposed.</exception>
    public bool ShowBehindParent
    {
        get { ThrowIfDisposed(); Tree?.EnsureOwnerThread(); return _showBehindParent; }
        set { EnsureMutable(); _showBehindParent = value; }
    }

    /// <summary>Gets or sets whether this item and its canvas children draw in ascending local Y order.</summary>
    /// <value>False by default.</value>
    /// <remarks>Sorting uses positions relative to this item's coordinate system; this item's own position in
    /// that system is zero. Nested Y-sorted children join the same group. Other child subtrees remain together
    /// at their root's Y position. Approximate Y ties retain scene order. Effective Z takes precedence, and
    /// neutral nodes and TopLevel children start independent canvas roots. Processing and input order are unchanged.
    /// Changes affect the next submission without requiring QueueRedraw.</remarks>
    /// <exception cref="InvalidOperationException">Attached access is off the owner thread, or mutation occurs during scene capture.</exception>
    /// <exception cref="ObjectDisposedException">The item is disposed.</exception>
    public bool YSortEnabled
    {
        get { ThrowIfDisposed(); Tree?.EnsureOwnerThread(); return _ySortEnabled; }
        set { EnsureMutable(); _ySortEnabled = value; }
    }

    /// <summary>Gets or sets whether this node ignores its parent's transform.</summary>
    /// <value><see langword="false"/> by default.</value>
    /// <remarks>The local transform stays unchanged; global coordinates are recomputed against the new canvas boundary.
    /// This item becomes a separate canvas root, drawn after the preceding root's entire canvas subtree at the same Z.
    /// Canvas roots retain scene order; Z still takes precedence. Logical visibility continues to follow direct canvas ancestors.
    /// Attached changes deliver NotificationExitCanvas before committing the mode and NotificationEnterCanvas afterward, scheduling redraw.
    /// Callback failures are aggregated after the transition; recursive rebinding is rejected.</remarks>
    /// <exception cref="InvalidOperationException">Mutation occurs off the owner thread, during packed-scene capture, or recursively during canvas rebinding.</exception>
    /// <exception cref="ObjectDisposedException">This node or an ancestor is disposing on another thread, or has finished disposing.</exception>
    /// <exception cref="AggregateException">A canvas or transform callback fails; the mode transition still completes.</exception>
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

            if (_rebindingCanvas) throw new InvalidOperationException("Canvas rebinding cannot be re-entered.");
            _rebindingCanvas = true;
            List<Exception>? errors = null;
            try
            {
                try { ExitCanvas(); }
                catch (Exception error) { CollectException(ref errors, error); }
                _topLevel = value;
                try { EnterCanvas(); }
                catch (Exception error) { CollectException(ref errors, error); }
                try { if (!IsDisposed) NotifyLocalTransformChanged(); }
                catch (Exception error) { CollectException(ref errors, error); }
            }
            finally { _rebindingCanvas = false; }
            ThrowCollected("Canvas rebinding callbacks failed.", errors);
        }
    }

    /// <summary>Gets or sets this node's local logical visibility.</summary>
    /// <value><see langword="true"/> by default.</value>
    /// <remarks>An actual local change notifies this item. Effective tree-visibility changes propagate through locally visible direct canvas children, including TopLevel items. Showing schedules redraw; hiding raises Hidden after visibility delivery.</remarks>
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
            if (IsInsideTree && _parentVisible) ApplyVisibilityChange();
            else DispatchNotification(NotificationVisibilityChanged);
        }
    }

    /// <summary>Gets whether this node is active and locally visible through its direct canvas ancestor chain.</summary>
    /// <value><see langword="true"/> only inside a tree when this node and its direct canvas visibility chain are visible.
    /// A direct CanvasLayer parent supplies its own visibility; other non-canvas boundaries use the selected viewport: root window visibility or an independent offscreen canvas.</value>
    /// <remarks>This logical query does not account for VisibilityLayer or Viewport.CanvasCullMask.</remarks>
    /// <exception cref="ObjectDisposedException">This node or a queried ancestor is disposing on another thread, or has finished disposing.</exception>
    public bool IsVisibleInTree => IsInsideTree && Visible && _parentVisible;

    /// <summary>Gets or sets this node's local Z-order value.</summary>
    /// <value>An integer from <see cref="MinimumZIndex"/> through <see cref="MaximumZIndex"/>; the default is zero.</value>
    /// <remarks>Every valid assignment commits the value then requests configuration-warning refresh, even if unchanged.</remarks>
    /// <exception cref="Exception">A configuration-warning subscriber fails after assignment.</exception>
    /// <exception cref="ArgumentOutOfRangeException">The assigned value is outside the supported range.</exception>
    /// <exception cref="InvalidOperationException">An attached node is read or mutated off the owner thread.</exception>
    /// <exception cref="ObjectDisposedException">The node is disposing on another thread or has finished disposing.</exception>
    public int ZIndex
    {
        get
        {
            ThrowIfDisposed();
            Tree?.EnsureOwnerThread();
            return _zIndex;
        }
        set
        {
            EnsureMutable();

            if (value is < MinimumZIndex or > MaximumZIndex)
                throw new ArgumentOutOfRangeException(nameof(value), value, $"Z index must be between {MinimumZIndex} and {MaximumZIndex}.");

            _zIndex = value;
            UpdateConfigurationWarnings();
        }
    }

    /// <summary>Gets or sets whether effective Z order accumulates ancestor Z values.</summary>
    /// <value><see langword="true"/> by default.</value>
    /// <exception cref="InvalidOperationException">An attached node is read or mutated off the owner thread.</exception>
    /// <exception cref="ObjectDisposedException">The node is disposing on another thread or has finished disposing.</exception>
    public bool ZAsRelative
    {
        get
        {
            ThrowIfDisposed();
            Tree?.EnsureOwnerThread();
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
    /// <value>False by default. Enables both the numeric local notification and its typed event while attached.</value>
    /// <exception cref="InvalidOperationException">Attached access is off-owner, or mutation occurs during scene capture.</exception>
    /// <exception cref="ObjectDisposedException">The node is disposing on another thread or has finished disposing.</exception>
    public bool NotifyLocalTransformChanges
    {
        get
        {
            ThrowIfDisposed();
            Tree?.EnsureOwnerThread();
            return _notifyLocalTransformChanges;
        }
        set
        {
            EnsureMutable();
            _notifyLocalTransformChanges = value;
        }
    }

    /// <summary>Gets or sets whether global transform changes dispatch <see cref="NotificationTransformChanged"/>.</summary>
    /// <value>False by default. Enables queuing on global invalidation; initial tree entry always queues once.</value>
    /// <remarks>Enabling while attached resolves the current global transform without queuing. Disabling does not
    /// cancel an already queued notification. Delivery includes the typed TransformChanged event.</remarks>
    /// <exception cref="InvalidOperationException">Attached access is off-owner, or mutation occurs during scene capture.</exception>
    /// <exception cref="ObjectDisposedException">The node is disposing on another thread or has finished disposing.</exception>
    public bool NotifyTransformChanges
    {
        get
        {
            ThrowIfDisposed();
            Tree?.EnsureOwnerThread();
            return _notifyTransformChanges;
        }
        set
        {
            EnsureMutable();
            if (_notifyTransformChanges == value) return;
            _notifyTransformChanges = value;
            if (value && IsInsideTree) _ = GetGlobalTransform();
        }
    }

    /// <summary>Occurs after local or inherited logical visibility is propagated to this node.</summary>
    /// <remarks>The base notification handler raises this event, including manual visibility notifications. Effective changes propagate through locally visible direct canvas descendants; visible tree entry also delivers it.</remarks>
    public event Action<CanvasItem>? VisibilityChanged;

    /// <summary>Occurs after visibility delivery when this item becomes hidden in its tree.</summary>
    /// <remarks>Explicit changes below a hidden parent and tree exit do not raise this event. Delivery is synchronous.</remarks>
    public event Action<CanvasItem>? Hidden;

    /// <summary>Occurs when an operation may change this item's local drawing bounds.</summary>
    /// <remarks>Derived geometry setters report this independently of transform notifications. Delivery is synchronous
    /// on the mutating thread, restricted to the scene owner while attached, including while hidden.</remarks>
    public event Action<CanvasItem>? ItemRectChanged;

    /// <summary>Projects the local-transform notification with this item as sender.</summary>
    /// <remarks>Automatic delivery is synchronous, attached-only and controlled by NotifyLocalTransformChanges. Manual notifications also deliver it.</remarks>
    public event Action<CanvasItem>? LocalTransformChanged;

    /// <summary>Projects the global-transform notification with this item as sender.</summary>
    /// <remarks>Automatic delivery uses the scene queue or ForceUpdateTransform. It follows the numeric notification, including initial entry and manual notifications.</remarks>
    public event Action<CanvasItem>? TransformChanged;

    /// <summary>Moves this node to the last position among its siblings.</summary>
    /// <remarks>A detached or hierarchy-root node is left unchanged.</remarks>
    /// <exception cref="InvalidOperationException">An attached item is accessed off the owner thread.</exception>
    /// <exception cref="ObjectDisposedException">This node or its parent is disposing on another thread or has finished disposing.</exception>
    /// <exception cref="AggregateException">One or more child-order or tree-change callbacks fail after the order changes.</exception>
    public void MoveToFront()
    {
        ThrowIfDisposed();
        Tree?.EnsureOwnerThread();
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
        if (_globalTransformInvalid) return;
        _globalTransformInvalid = true;
        if (_notifyTransformChanges && IsInsideTree) Tree!.QueueTransformNotification(this);
        for (var i = 0; i < GetChildCount(includeInternal: true); i++)
            if (GetChild(i, includeInternal: true) is CanvasItem { TopLevel: false } child && !child.IsDisposed)
                child.PropagateGlobalTransformChanged();
    }

    internal void PropagateVisibilityChanged()
    {
        if (IsDisposed || !IsInsideTree) return;
        var parentVisible = GetParentVisibility();
        if (_parentVisible == parentVisible) return;
        _parentVisible = parentVisible;
        if (Visible) ApplyVisibilityChange();
    }

    private bool GetParentVisibility() => Parent switch
    {
        CanvasItem item => item.IsVisibleInTree,
        CanvasLayer layer => layer.Visible,
        _ => GetViewport() is not Window window || window.Visible,
    };

    private void ApplyVisibilityChange()
    {
        var visible = IsVisibleInTree;
        List<Exception>? errors = null;
        try { DispatchNotification(NotificationVisibilityChanged); }
        catch (Exception error) { CollectException(ref errors, error); }
        if (!IsDisposed)
        {
            if (IsVisibleInTree) InvalidateCanvas();
            else if (!visible)
            {
                try { Hidden?.Invoke(this); }
                catch (Exception error) { CollectException(ref errors, error); }
            }
            foreach (var child in AllChildren.ToArray())
            {
                if (child is not CanvasItem item || item.IsDisposed || !ReferenceEquals(item.Parent, this)) continue;
                try { item.PropagateVisibilityChanged(); }
                catch (Exception error) { CollectException(ref errors, error); }
            }
        }
        ThrowCollected("Canvas visibility callbacks failed.", errors);
    }
}
