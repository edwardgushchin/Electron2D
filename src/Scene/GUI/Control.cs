namespace Electron2D;

/// <summary>A canvas item with a rectangular layout and a pivot-based transform.</summary>
/// <remarks>Anchors and offsets resolve against the direct canvas parent's rectangle or the viewport.
/// The root viewport routes pointer and focused keyboard input to controls. Theme, container layout and complete GUI routing remain separate capabilities.</remarks>
public partial class Control : CanvasItem
{
    private readonly float[] _anchors = new float[4];
    private readonly float[] _offsets = new float[4];
    private Vector2 _position;
    private Vector2 _size;
    private float _rotation;
    private Vector2 _scale = Vector2.One;
    private Vector2 _pivotOffset;
    private CanvasItem? _layoutParent;
    private Viewport? _layoutViewport;

    /// <summary>Creates a detached control with zero size, zero anchors and identity transform.</summary>
    public Control() { }

    /// <summary>Identifies a size change delivered after the new rectangle and transform are committed.</summary>
    public const int NotificationResized = 40;

    /// <summary>Occurs after a size change while attached to the scene tree.</summary>
    public event Action? Resized;

    /// <summary>Gets or sets the local rectangle's upper-left point.</summary>
    /// <value>The position before pivot, rotation and scale.</value>
    public Vector2 Position
    {
        get { ThrowIfDisposed(); return _position; }
        set
        {
            EnsureMutable(); EnsureFinite(value, nameof(value));
            if (value == _position) return;
            var area = GetParentAreaSize();
            _offsets[0] = value.X - _anchors[0] * area.X;
            _offsets[1] = value.Y - _anchors[1] * area.Y;
            _offsets[2] = value.X + _size.X - _anchors[2] * area.X;
            _offsets[3] = value.Y + _size.Y - _anchors[3] * area.Y;
            Reflow();
        }
    }

    /// <summary>Gets or sets the local rectangle's size before rotation and scale.</summary>
    /// <value>A finite, nonnegative size.</value>
    public Vector2 Size
    {
        get { ThrowIfDisposed(); return _size; }
        set
        {
            EnsureMutable(); EnsureFinite(value, nameof(value));
            if (value.X < 0 || value.Y < 0) throw new ArgumentOutOfRangeException(nameof(value));
            if (value == _size) return;
            var area = GetParentAreaSize();
            _offsets[2] = _position.X + value.X - _anchors[2] * area.X;
            _offsets[3] = _position.Y + value.Y - _anchors[3] * area.Y;
            Reflow();
        }
    }

    /// <summary>Gets or sets the local rotation in radians around PivotOffset.</summary>
    public float Rotation
    {
        get { ThrowIfDisposed(); return _rotation; }
        set { EnsureMutable(); EnsureFinite(value, nameof(value)); if (_rotation == value) return; _rotation = value; NotifyLocalTransformChanged(); QueueRedraw(); }
    }

    /// <summary>Gets or sets the local rotation in degrees.</summary>
    public float RotationDegrees
    {
        get => Rotation * (180f / Mathf.Pi);
        set => Rotation = value * (Mathf.Pi / 180f);
    }

    /// <summary>Gets or sets the local scale around PivotOffset.</summary>
    /// <remarks>Zero components are replaced by a small positive epsilon to keep the transform invertible.</remarks>
    public Vector2 Scale
    {
        get { ThrowIfDisposed(); return _scale; }
        set
        {
            EnsureMutable(); EnsureFinite(value, nameof(value));
            if (value == _scale) return;
            _scale = new(value.X == 0 ? 0.00001f : value.X, value.Y == 0 ? 0.00001f : value.Y);
            NotifyLocalTransformChanged(); QueueRedraw();
        }
    }

    /// <summary>Gets or sets the point around which local rotation and scale are applied.</summary>
    public Vector2 PivotOffset
    {
        get { ThrowIfDisposed(); return _pivotOffset; }
        set { EnsureMutable(); EnsureFinite(value, nameof(value)); if (_pivotOffset == value) return; _pivotOffset = value; NotifyLocalTransformChanged(); QueueRedraw(); }
    }

    /// <summary>Gets or sets the transformed origin in global canvas coordinates.</summary>
    public Vector2 GlobalPosition
    {
        get => GetGlobalTransform().Origin;
        set
        {
            EnsureMutable(); EnsureFinite(value, nameof(value));
            var local = GetParentItem() is { } parent ? parent.GetGlobalTransform().AffineInverse() * value : value;
            Position += local - GetTransform().Origin;
        }
    }

    /// <summary>Gets or sets the left anchor fraction.</summary>
    public float AnchorLeft { get => GetAnchor(Side.Left); set => SetAnchor(Side.Left, value); }
    /// <summary>Gets or sets the top anchor fraction.</summary>
    public float AnchorTop { get => GetAnchor(Side.Top); set => SetAnchor(Side.Top, value); }
    /// <summary>Gets or sets the right anchor fraction.</summary>
    public float AnchorRight { get => GetAnchor(Side.Right); set => SetAnchor(Side.Right, value); }
    /// <summary>Gets or sets the bottom anchor fraction.</summary>
    public float AnchorBottom { get => GetAnchor(Side.Bottom); set => SetAnchor(Side.Bottom, value); }
    /// <summary>Gets or sets the left offset.</summary>
    public float OffsetLeft { get => GetOffset(Side.Left); set => SetOffset(Side.Left, value); }
    /// <summary>Gets or sets the top offset.</summary>
    public float OffsetTop { get => GetOffset(Side.Top); set => SetOffset(Side.Top, value); }
    /// <summary>Gets or sets the right offset.</summary>
    public float OffsetRight { get => GetOffset(Side.Right); set => SetOffset(Side.Right, value); }
    /// <summary>Gets or sets the bottom offset.</summary>
    public float OffsetBottom { get => GetOffset(Side.Bottom); set => SetOffset(Side.Bottom, value); }

    /// <summary>Gets the anchor for a rectangle side.</summary>
    /// <param name="side">The side to query.</param>
    /// <returns>The fraction of the parent area's corresponding axis.</returns>
    public float GetAnchor(Side side) { ThrowIfDisposed(); return _anchors[SideIndex(side)]; }

    /// <summary>Sets an anchor, optionally preserving its offset and moving the opposite anchor.</summary>
    /// <param name="side">The side to update.</param>
    /// <param name="anchor">A finite fraction of the parent area.</param>
    /// <param name="keepOffset">Keep the current offset instead of the current edge position.</param>
    /// <param name="pushOppositeAnchor">Move the opposite anchor when the anchors cross.</param>
    public void SetAnchor(Side side, float anchor, bool keepOffset = false, bool pushOppositeAnchor = true)
    {
        EnsureMutable(); EnsureFinite(anchor, nameof(anchor));
        var index = SideIndex(side);
        var opposite = (index + 2) % 4;
        var area = GetParentAreaSize();
        var range = index % 2 == 0 ? area.X : area.Y;
        var previous = _offsets[index] + _anchors[index] * range;
        var previousOpposite = _offsets[opposite] + _anchors[opposite] * range;
        _anchors[index] = anchor;
        if (index < 2 ? anchor > _anchors[opposite] : anchor < _anchors[opposite])
        {
            if (pushOppositeAnchor) _anchors[opposite] = anchor;
            else _anchors[index] = _anchors[opposite];
        }
        if (!keepOffset)
        {
            _offsets[index] = previous - _anchors[index] * range;
            if (pushOppositeAnchor) _offsets[opposite] = previousOpposite - _anchors[opposite] * range;
        }
        Reflow(); QueueRedraw();
    }

    /// <summary>Arranges all four anchors in a standard layout preset.</summary>
    /// <param name="preset">The arrangement to apply.</param>
    /// <param name="keepOffsets">Keep each local offset instead of preserving the current rectangle edges.</param>
    /// <exception cref="ArgumentOutOfRangeException">The preset is not defined.</exception>
    /// <remarks>Applies left, top, right, then bottom through <see cref="SetAnchor"/>; attached controls reflow after each side.</remarks>
    public void SetAnchorsPreset(ControlLayoutPreset preset, bool keepOffsets = false)
    {
        EnsureMutable();
        var (left, top, right, bottom) = preset switch
        {
            ControlLayoutPreset.TopLeft => (0f, 0f, 0f, 0f),
            ControlLayoutPreset.TopRight => (1f, 0f, 1f, 0f),
            ControlLayoutPreset.BottomLeft => (0f, 1f, 0f, 1f),
            ControlLayoutPreset.BottomRight => (1f, 1f, 1f, 1f),
            ControlLayoutPreset.CenterLeft => (0f, .5f, 0f, .5f),
            ControlLayoutPreset.CenterTop => (.5f, 0f, .5f, 0f),
            ControlLayoutPreset.CenterRight => (1f, .5f, 1f, .5f),
            ControlLayoutPreset.CenterBottom => (.5f, 1f, .5f, 1f),
            ControlLayoutPreset.Center => (.5f, .5f, .5f, .5f),
            ControlLayoutPreset.LeftWide => (0f, 0f, 0f, 1f),
            ControlLayoutPreset.TopWide => (0f, 0f, 1f, 0f),
            ControlLayoutPreset.RightWide => (1f, 0f, 1f, 1f),
            ControlLayoutPreset.BottomWide => (0f, 1f, 1f, 1f),
            ControlLayoutPreset.VCenterWide => (.5f, 0f, .5f, 1f),
            ControlLayoutPreset.HCenterWide => (0f, .5f, 1f, .5f),
            ControlLayoutPreset.FullRect => (0f, 0f, 1f, 1f),
            _ => throw new ArgumentOutOfRangeException(nameof(preset), preset, "Unknown layout preset.")
        };
        SetAnchor(Side.Left, left, keepOffsets);
        SetAnchor(Side.Top, top, keepOffsets);
        SetAnchor(Side.Right, right, keepOffsets);
        SetAnchor(Side.Bottom, bottom, keepOffsets);
    }

    /// <summary>Gets the offset for a rectangle side.</summary>
    /// <param name="side">The side to query.</param>
    /// <returns>The offset in local canvas units.</returns>
    public float GetOffset(Side side) { ThrowIfDisposed(); return _offsets[SideIndex(side)]; }

    /// <summary>Sets the offset for a rectangle side and resolves the rectangle.</summary>
    /// <param name="side">The side to update.</param>
    /// <param name="offset">A finite local offset.</param>
    public void SetOffset(Side side, float offset)
    {
        EnsureMutable(); EnsureFinite(offset, nameof(offset));
        var index = SideIndex(side);
        if (_offsets[index] == offset) return;
        _offsets[index] = offset;
        Reflow();
    }

    /// <summary>Returns the direct parent when it is a Control.</summary>
    /// <returns>The parent control or null.</returns>
    public Control? GetParentControl() { ThrowIfDisposed(); return Parent as Control; }

    /// <summary>Returns the size used to resolve anchors, or zero while detached.</summary>
    /// <returns>The direct parent control's size or the viewport's visible size.</returns>
    public Vector2 GetParentAreaSize()
    {
        ThrowIfDisposed();
        if (!IsInsideTree) return Vector2.Zero;
        return GetParentItem() switch
        {
            Control parent => parent.Size,
            null => GetViewport()?.GetVisibleRect().Size ?? Vector2.Zero,
            _ => Vector2.Zero
        };
    }

    /// <summary>Returns the unrotated local rectangle after pivot and scale.</summary>
    /// <returns>The transformed origin and scaled size.</returns>
    public Rect GetRect() { var transform = GetTransform(); return new(transform.Origin, transform.Scale * _size); }

    /// <summary>Returns the unrotated global rectangle after pivot and scale.</summary>
    /// <returns>The global transformed origin and scaled size.</returns>
    public Rect GetGlobalRect() { var transform = GetGlobalTransform(); return new(transform.Origin, transform.Scale * _size); }

    /// <inheritdoc />
    public override Transform GetTransform()
    {
        ThrowIfDisposed();
        Tree?.EnsureOwnerThread();
        var transform = new Transform(_rotation, _scale, 0f, _pivotOffset);
        transform.Origin += _position - transform.BasisXform(_pivotOffset);
        return transform;
    }

    /// <summary>Moves this control under another node, preserving its global position by default.</summary>
    /// <param name="newParent">The destination parent.</param>
    /// <param name="keepGlobalTransform">Preserve the transformed origin in global canvas coordinates.</param>
    /// <remarks>Validates the destination canvas inverse before changing the hierarchy. Size and anchors reflow
    /// against the new parent; only the global origin is preserved.</remarks>
    public override void Reparent(Node newParent, bool keepGlobalTransform = true)
    {
        EnsureMutable();
        ArgumentNullException.ThrowIfNull(newParent);
        if (ReferenceEquals(Parent, newParent)) return;
        var global = keepGlobalTransform ? GlobalPosition : Vector2.Zero;
        if (keepGlobalTransform && !TopLevel && newParent is CanvasItem canvasParent)
            _ = canvasParent.GetGlobalTransform().AffineInverse() * global;
        List<Exception>? errors = null;
        try { base.Reparent(newParent, keepGlobalTransform); }
        catch (AggregateException error) { CollectException(ref errors, error); }
        if (keepGlobalTransform && !IsDisposed && ReferenceEquals(Parent, newParent))
        {
            try { GlobalPosition = global; }
            catch (Exception error) { CollectException(ref errors, error); }
        }
        ThrowCollected("Reparent callbacks failed.", errors);
    }

    /// <inheritdoc />
    protected override void OnNotification(int what)
    {
        if (what == NotificationExitTree || what == NotificationVisibilityChanged && !IsVisibleInTree)
        {
            List<Exception>? errors = null;
            try { base.OnNotification(what); } catch (Exception error) { CollectException(ref errors, error); }
            try { Tree?.ReleaseGUIFocus(this); } catch (Exception error) { CollectException(ref errors, error); }
            try { Tree?.ReleaseGUIHover(this); } catch (Exception error) { CollectException(ref errors, error); }
            ThrowCollected("Control visibility callbacks failed.", errors);
        }
        else base.OnNotification(what);
        if (what == NotificationEnterCanvas)
        {
            _layoutParent = GetParentItem();
            if (_layoutParent is not null) _layoutParent.ItemRectChanged += OnParentRectChanged;
            else if ((_layoutViewport = GetViewport()) is not null) _layoutViewport.SizeChanged += Reflow;
            Reflow();
        }
        else if (what == NotificationExitCanvas) DisconnectLayoutSource();
        else if (what == NotificationResized) Resized?.Invoke();
    }

    /// <inheritdoc />
    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors() => base.GetPropertyDescriptors().Concat(ControlProperties).Concat(FocusProperties);

    /// <inheritdoc />
    protected override Func<Node> CreateSceneInstanceFactory() => GetType() == typeof(Control) ? CreateControl : base.CreateSceneInstanceFactory();

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        if (disposing) { DisconnectLayoutSource(); Resized = null; GUIInput = null; FocusEntered = null; FocusExited = null; MouseEntered = null; MouseExited = null; }
        base.Dispose(disposing);
    }

    private static Node CreateControl() => new Control();
    private void OnParentRectChanged(CanvasItem _) => Reflow();

    private void DisconnectLayoutSource()
    {
        if (_layoutParent is not null) _layoutParent.ItemRectChanged -= OnParentRectChanged;
        if (_layoutViewport is not null) _layoutViewport.SizeChanged -= Reflow;
        _layoutParent = null; _layoutViewport = null;
    }

    private void Reflow()
    {
        var area = GetParentAreaSize();
        var position = new Vector2(_offsets[0] + _anchors[0] * area.X, _offsets[1] + _anchors[1] * area.Y);
        var end = new Vector2(_offsets[2] + _anchors[2] * area.X, _offsets[3] + _anchors[3] * area.Y);
        var size = end - position;
        size = new(Mathf.Max(0, size.X), Mathf.Max(0, size.Y));
        if (position == _position && size == _size) return;
        var sizeChanged = size != _size;
        _position = position; _size = size;
        NotifyLocalTransformChanged();
        NotifyItemRectChanged(sizeChanged);
        if (sizeChanged && IsInsideTree) DispatchNotification(NotificationResized);
    }

    private static int SideIndex(Side side) => side is >= Side.Left and <= Side.Bottom ? (int)side : throw new ArgumentOutOfRangeException(nameof(side));
    private static void EnsureFinite(float value, string name) { if (!Mathf.IsFinite(value)) throw new ArgumentOutOfRangeException(name); }
    private static void EnsureFinite(Vector2 value, string name) { if (!Mathf.IsFinite(value.X) || !Mathf.IsFinite(value.Y)) throw new ArgumentOutOfRangeException(name); }

    private static readonly PropertyDescriptor[] ControlProperties =
    [
        new PropertyDescriptor<Control, Vector2>(nameof(Position), node => node.Position, (node, value) => node.Position = value, _ => Vector2.Zero, stored: true),
        new PropertyDescriptor<Control, Vector2>(nameof(Size), node => node.Size, (node, value) => node.Size = value, _ => Vector2.Zero, stored: true),
        new PropertyDescriptor<Control, float>(nameof(RotationDegrees), node => node.RotationDegrees, (node, value) => node.RotationDegrees = value, _ => 0f, stored: true),
        new PropertyDescriptor<Control, Vector2>(nameof(Scale), node => node.Scale, (node, value) => node.Scale = value, _ => Vector2.One, stored: true),
        new PropertyDescriptor<Control, Vector2>(nameof(PivotOffset), node => node.PivotOffset, (node, value) => node.PivotOffset = value, _ => Vector2.Zero, stored: true),
        new PropertyDescriptor<Control, float>(nameof(AnchorLeft), node => node.AnchorLeft, (node, value) => node.AnchorLeft = value, _ => 0f, stored: true),
        new PropertyDescriptor<Control, float>(nameof(AnchorTop), node => node.AnchorTop, (node, value) => node.AnchorTop = value, _ => 0f, stored: true),
        new PropertyDescriptor<Control, float>(nameof(AnchorRight), node => node.AnchorRight, (node, value) => node.AnchorRight = value, _ => 0f, stored: true),
        new PropertyDescriptor<Control, float>(nameof(AnchorBottom), node => node.AnchorBottom, (node, value) => node.AnchorBottom = value, _ => 0f, stored: true),
        new PropertyDescriptor<Control, float>(nameof(OffsetLeft), node => node.OffsetLeft, (node, value) => node.OffsetLeft = value, _ => 0f, stored: true),
        new PropertyDescriptor<Control, float>(nameof(OffsetTop), node => node.OffsetTop, (node, value) => node.OffsetTop = value, _ => 0f, stored: true),
        new PropertyDescriptor<Control, float>(nameof(OffsetRight), node => node.OffsetRight, (node, value) => node.OffsetRight = value, _ => 0f, stored: true),
        new PropertyDescriptor<Control, float>(nameof(OffsetBottom), node => node.OffsetBottom, (node, value) => node.OffsetBottom = value, _ => 0f, stored: true),
        new PropertyDescriptor<Control, ControlMouseFilter>(nameof(MouseFilter), node => node.MouseFilter, (node, value) => node.MouseFilter = value, _ => ControlMouseFilter.Stop, stored: true),
        new PropertyDescriptor<Control, bool>(nameof(MouseForcePassScrollEvents), node => node.MouseForcePassScrollEvents, (node, value) => node.MouseForcePassScrollEvents = value, _ => true, stored: true),
        new PropertyDescriptor<Control, ControlFocusMode>(nameof(FocusMode), node => node.FocusMode, (node, value) => node.FocusMode = value, _ => ControlFocusMode.None, stored: true),
        new PropertyDescriptor<Control, CursorShape>(nameof(MouseDefaultCursorShape), node => node.MouseDefaultCursorShape, (node, value) => node.MouseDefaultCursorShape = value, _ => CursorShape.Arrow, stored: true)
    ];
}
