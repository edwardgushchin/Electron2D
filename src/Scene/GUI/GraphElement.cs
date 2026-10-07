namespace Electron2D;

/// <summary>Provides selectable graph-space placement and request-driven resizing for graph controls.</summary>
/// <remarks>PositionOffset belongs to the graph model. A graph owner projects it into Control.Position.
/// ResizeRequest asks the application to assign Size; it never silently changes the element's size.</remarks>
public class GraphElement : Container
{
    private Vector2 _offset, _dragFrom, _resizeFrom, _resizeSize;
    private bool _draggable = true, _resizable, _selectable = true, _selected, _scalingMenus, _resizing;
    internal bool Releasing, GraphDrawing;
    internal void MutableGraph() { EnsureMutable(); if (GraphDrawing) throw new InvalidOperationException("Graph drawing cannot mutate its element."); }
    /// <inheritdoc />
    protected override void ValidateDisposal() { if (GraphDrawing) throw new InvalidOperationException("Graph drawing cannot dispose its element."); base.ValidateDisposal(); }
    internal void CheckGraph() { ThrowIfDisposed(); Tree?.EnsureOwnerThread(); }
    /// <summary>Creates a movable, selectable graph element without a resize handle.</summary>
    public GraphElement() { }
    /// <summary>Gets or sets whether pointer movement may move this element.</summary>
    public bool Draggable { get { CheckGraph(); return _draggable; } set { MutableGraph(); _draggable = value; } }
    /// <summary>Gets or sets finite placement in unscaled graph coordinates.</summary>
    public Vector2 PositionOffset { get { CheckGraph(); return _offset; } set { MutableGraph(); if (!value.IsFinite()) throw new ArgumentOutOfRangeException(nameof(value)); if (_offset == value) return; _offset = value; PositionOffsetChanged?.Invoke(); QueueRedraw(); } }
    /// <summary>Gets or sets whether a resize handle issues resize requests.</summary>
    public bool Resizable { get { CheckGraph(); return _resizable; } set { MutableGraph(); if (_resizable == value) return; _resizable = value; _resizing = false; QueueRedraw(); } }
    /// <summary>Gets or sets whether descendant popup menus follow graph zoom.</summary>
    public bool ScalingMenus { get { CheckGraph(); return _scalingMenus; } set { MutableGraph(); _scalingMenus = value; RefreshGraphMenus(); } }
    /// <summary>Gets or sets selection eligibility; disabling it clears selection.</summary>
    public bool Selectable { get { CheckGraph(); return _selectable; } set { MutableGraph(); if (!value) Selected = false; _selectable = value; } }
    /// <summary>Gets or sets selection, ignored while Selectable is false.</summary>
    public bool Selected { get { CheckGraph(); return _selected; } set { MutableGraph(); if (!_selectable || _selected == value) return; _selected = value; QueueRedraw(); if (value) NodeSelected?.Invoke(); else NodeDeselected?.Invoke(); } }
    /// <summary>Requests deletion by the owning application.</summary>
    public event Action? DeleteRequest;
    /// <summary>Reports graph placement before and after a completed drag.</summary>
    public event Action<Vector2, Vector2>? Dragged;
    /// <summary>Reports deselection.</summary>
    public event Action? NodeDeselected;
    /// <summary>Reports selection.</summary>
    public event Action? NodeSelected;
    /// <summary>Reports a changed graph position.</summary>
    public event Action? PositionOffsetChanged;
    /// <summary>Requests raising this element in the graph's stacking order.</summary>
    public event Action? RaiseRequest;
    /// <summary>Reports the actual size when resizing ends.</summary>
    public event Action<Vector2>? ResizeEnd;
    /// <summary>Requests a finite new size during pointer resizing.</summary>
    public event Action<Vector2>? ResizeRequest;
    internal void StartGraphDrag() => _dragFrom = PositionOffset;
    internal void EndGraphDrag() => Dragged?.Invoke(_dragFrom, PositionOffset);
    internal void RequestGraphDelete() => DeleteRequest?.Invoke();
    internal virtual bool CanResize => Resizable;
    internal void RefreshGraphMenus()
    { var scale = ScalingMenus && Parent is GraphEdit graph ? graph.Zoom : 1; ApplyMenus(this, scale); }
    private static void ApplyMenus(Node node, float scale) { for (var i = 0; i < node.GetChildCount(includeInternal: true); i++) { var child = node.GetChild(i, includeInternal: true); if (child is PopupMenu menu) menu.ApplyGraphScale(scale); ApplyMenus(child, scale); } }
    internal Vector2 ResizeHandleSize => GetThemeIcon("resizer")?.GetSize() ?? new Vector2(16, 16);
    internal bool IsResizePoint(Vector2 position) => CanResize && new Rect2(Size - ResizeHandleSize, ResizeHandleSize).HasPoint(position);
    /// <inheritdoc />
    protected override CursorShape OnGetCursorShape(Vector2 atPosition) => CanResize && (_resizing || IsResizePoint(atPosition)) ? CursorShape.FDiagSize : base.OnGetCursorShape(atPosition);
    /// <inheritdoc />
    protected override void OnGUIInput(InputEvent inputEvent)
    {
        base.OnGUIInput(inputEvent);
        if (inputEvent is InputEventMouseButton { ButtonIndex: MouseButton.Left } button)
        {
            if (button.Pressed && IsResizePoint(button.Position)) { _resizing = true; _resizeFrom = button.Position; _resizeSize = Size; AcceptEvent(); return; }
            if (!button.Pressed && _resizing) { _resizing = false; ResizeEnd?.Invoke(Size); AcceptEvent(); return; }
            if (button.Pressed) RaiseRequest?.Invoke();
        }
        else if (inputEvent is InputEventMouseMotion motion && _resizing) { ResizeRequest?.Invoke((_resizeSize + motion.Position - _resizeFrom).Max(Vector2.Zero)); AcceptEvent(); return; }
        if (Parent is GraphEdit graph) graph.ElementInput(this, inputEvent);
    }
    /// <inheritdoc />
    protected override Vector2 OnGetMinimumSize()
    {
        var result = Vector2.Zero;
        for (var i = 0; i < GetChildCount(); i++) if (GetChild(i) is Control child && Sortable(child, true)) result = result.Max(child.GetBoundMinimumSize());
        return result;
    }
    internal virtual void ArrangeGraphChildren()
    {
        for (var i = 0; i < GetChildCount(); i++) if (GetChild(i) is Control child && Sortable(child)) FitChildInRect(child, new(Vector2.Zero, Size));
    }
    /// <inheritdoc />
    protected override void OnNotification(int what)
    {
        base.OnNotification(what);
        if (what == NotificationSortChildren) ArrangeGraphChildren();
        else if (what is NotificationResized or NotificationThemeChanged) { QueueSort(); QueueRedraw(); }
        else if (what == NotificationDraw && GetType() == typeof(GraphElement) && CanResize && GetThemeIcon("resizer") is { } icon) DrawTexture(icon, Size - icon.GetSize());
    }
    /// <inheritdoc />
    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors()
    {
        foreach (var p in base.GetPropertyDescriptors()) yield return p;
        yield return new PropertyDescriptor<GraphElement, bool>(nameof(Draggable), n => n.Draggable, (n, v) => n.Draggable = v, _ => true, stored: true);
        yield return new PropertyDescriptor<GraphElement, Vector2>(nameof(PositionOffset), n => n.PositionOffset, (n, v) => n.PositionOffset = v, _ => Vector2.Zero, stored: true);
        yield return new PropertyDescriptor<GraphElement, bool>(nameof(Resizable), n => n.Resizable, (n, v) => n.Resizable = v, _ => false, stored: true);
        yield return new PropertyDescriptor<GraphElement, bool>(nameof(ScalingMenus), n => n.ScalingMenus, (n, v) => n.ScalingMenus = v, _ => false, stored: true);
        yield return new PropertyDescriptor<GraphElement, bool>(nameof(Selectable), n => n.Selectable, (n, v) => n.Selectable = v, _ => true, stored: true);
        yield return new PropertyDescriptor<GraphElement, bool>(nameof(Selected), n => n.Selected, (n, v) => n.Selected = v, _ => false, stored: true);
    }
    /// <inheritdoc />
    protected override Func<Node> CreateSceneInstanceFactory() => GetType() == typeof(GraphElement) ? CreateGraphElement : base.CreateSceneInstanceFactory();
    private static Node CreateGraphElement() => new GraphElement();
    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        if (disposing) { Releasing = true; DeleteRequest = null; Dragged = null; NodeSelected = null; NodeDeselected = null; PositionOffsetChanged = null; RaiseRequest = null; ResizeEnd = null; ResizeRequest = null; }
        base.Dispose(disposing);
    }
}

internal sealed class GraphTitlebar : HBoxContainer
{
    private readonly GraphElement _owner;
    internal readonly Label Label;
    internal GraphTitlebar(GraphElement owner, bool center)
    {
        _owner = owner; Name = "_graph_titlebar"; MouseFilter = MouseFilter.Ignore; SizeFlagsHorizontal = SizeFlags.ExpandFill;
        Label = new OwnedLabel(owner) { Name = "_graph_title", ThemeTypeVariation = center ? "GraphFrameTitleLabel" : "GraphNodeTitleLabel", SizeFlagsHorizontal = SizeFlags.ExpandFill, MouseFilter = MouseFilter.Ignore, HorizontalAlignment = center ? HorizontalAlignment.Center : HorizontalAlignment.Left };
        AddChild(Label); owner.AddChild(this, InternalMode.Front);
    }
    private sealed class OwnedLabel(GraphElement owner) : Label { protected override void ValidateDisposal() { if (!owner.IsDisposed && !owner.Releasing) throw new InvalidOperationException("The graph element owns its title label."); base.ValidateDisposal(); } }
    protected override void ValidateDisposal() { if (!_owner.IsDisposed && !_owner.Releasing) throw new InvalidOperationException("The graph element owns its titlebar."); base.ValidateDisposal(); }
}
