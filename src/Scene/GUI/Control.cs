using System.Globalization;

namespace Electron2D;

/// <summary>A canvas item with a rectangular layout and a pivot-based transform.</summary>
/// <remarks>Anchors and offsets resolve against the direct canvas parent's rectangle or the viewport.
/// The root viewport routes pointer and focused keyboard input to controls. Container flags drive concrete layout;
/// typed themes supply inherited appearance values and local overrides.</remarks>
public partial class Control : CanvasItem
{
    private readonly float[] _anchors = new float[4];
    private readonly float[] _offsets = new float[4];
    private Vector2 _position;
    private Vector2 _size;
    private float _rotation;
    private Vector2 _scale = Vector2.One;
    private Vector2 _pivotOffset;
    private Vector2 _pivotOffsetRatio;
    private bool _offsetTransformEnabled;
    private Vector2 _offsetTransformPosition;
    private Vector2 _offsetTransformPositionRatio;
    private Vector2 _offsetTransformScale = Vector2.One;
    private float _offsetTransformRotation;
    private Vector2 _offsetTransformPivot;
    private Vector2 _offsetTransformPivotRatio = new(.5f, .5f);
    private bool _offsetTransformVisualOnly = true;
    private Vector2 _customMinimumSize;
    private Vector2 _lastMinimumSize;
    private Vector2 _customMaximumSize = new(-1, -1);
    private Vector2 _lastMaximumSize = new(-1, -1);
    private bool _propagateMaximumSize;
    private bool _clipContents;
    private GrowDirection _growHorizontal = GrowDirection.End;
    private GrowDirection _growVertical = GrowDirection.End;
    private bool _minimumSizeUpdatePending;
    private bool _maximumSizeUpdatePending;
    private Action? _minimumSizeUpdateAction, _maximumSizeUpdateAction;
    private LayoutDirection _layoutDirection;
    private CanvasItem? _layoutParent;
    private Viewport? _layoutViewport;

    /// <summary>Creates a detached control with zero size, zero anchors and identity transform.</summary>
    public Control() => PhysicsInterpolationMode = PhysicsInterpolationMode.Off;

    /// <summary>Identifies a size change delivered after the new rectangle and transform are committed.</summary>
    public const int NotificationResized = 40;

    /// <summary>Identifies a layout-direction change propagated through the scene subtree.</summary>
    public const int NotificationLayoutDirectionChanged = 49;

    /// <summary>Occurs after a size change while attached to the scene tree.</summary>
    public event Action? Resized;

    /// <summary>Occurs after a changed minimum size has been applied in the scene tree.</summary>
    public event Action? MinimumSizeChanged;

    /// <summary>Occurs after a changed maximum size has been applied in the scene tree.</summary>
    public event Action? MaximumSizeChanged;

    /// <summary>Gets or sets the local rectangle's upper-left point.</summary>
    /// <value>The position before pivot, rotation and scale.</value>
    public Vector2 Position
    {
        get { ThrowIfDisposed(); return _position; }
        set => SetPosition(value);
    }

    /// <summary>Gets or sets the local rectangle's size before rotation and scale.</summary>
    /// <value>A finite requested size clamped to the effective minimum and maximum.</value>
    public Vector2 Size
    {
        get { ThrowIfDisposed(); return _size; }
        set => SetSize(value);
    }

    /// <summary>Gets or sets whether this control clips its canvas descendants and pointer targeting to its rectangle.</summary>
    /// <value>False by default. The control's own drawing is not clipped while its clip has visible area.</value>
    /// <remarks>Only direct canvas descendants inherit clipping; a top-level or non-canvas boundary starts a new canvas branch. Changes refresh root-viewport hover and affect the next rendered frame without rerecording retained commands.</remarks>
    /// <exception cref="InvalidOperationException">An attached mutation is off the scene owner thread or occurs during scene capture.</exception>
    /// <exception cref="ObjectDisposedException">The control is disposed.</exception>
    public bool ClipContents
    {
        get { ThrowIfDisposed(); return _clipContents; }
        set
        {
            EnsureMutable();
            if (_clipContents == value) return;
            _clipContents = value;
            Tree?.RefreshGUIHover();
            QueueRedraw();
        }
    }

    internal bool ContainsClipPoint(Vector2 viewportPoint)
    {
        var point = MakeCanvasPositionLocal(viewportPoint);
        return point.IsFinite() && point.X >= 0 && point.Y >= 0 && point.X < _size.X && point.Y < _size.Y;
    }

    /// <summary>Gets or sets the caller-supplied lower bound for layout size.</summary>
    /// <remarks>Each component is combined with the intrinsic minimum and zero. Updates in a scene tree are coalesced for deferred delivery.</remarks>
    public Vector2 CustomMinimumSize
    {
        get { ThrowIfDisposed(); return _customMinimumSize; }
        set
        {
            EnsureMutable(); EnsureFinite(value, nameof(value));
            if (_customMinimumSize == value) return;
            _customMinimumSize = value;
            UpdateMinimumSize();
        }
    }

    /// <summary>Gets or sets the finite caller-supplied maximum size; a negative component disables its bound.</summary>
    public Vector2 CustomMaximumSize
    {
        get { ThrowIfDisposed(); return _customMaximumSize; }
        set
        {
            EnsureMutable(); EnsureFinite(value, nameof(value));
            var normalized = new Vector2(value.X < 0 ? -1 : value.X, value.Y < 0 ? -1 : value.Y);
            if (_customMaximumSize == normalized) return;
            _customMaximumSize = normalized;
            UpdateMaximumSize();
        }
    }

    /// <summary>Gets or sets whether this control's maximum size constrains direct child controls.</summary>
    public bool PropagateMaximumSize
    {
        get { ThrowIfDisposed(); return _propagateMaximumSize; }
        set { EnsureMutable(); if (_propagateMaximumSize == value) return; _propagateMaximumSize = value; UpdateMaximumSize(); }
    }

    /// <summary>Gets or sets the horizontal layout direction policy.</summary>
    /// <remarks>Changes notify the subtree before the resulting rectangles are observed by callers.</remarks>
    public LayoutDirection LayoutDirection
    {
        get { ThrowIfDisposed(); return _layoutDirection; }
        set
        {
            EnsureMutable();
            if (value is < LayoutDirection.Inherited or >= LayoutDirection.Max)
                throw new ArgumentOutOfRangeException(nameof(value), value, "Unknown layout direction.");
            if (_layoutDirection == value) return;
            _layoutDirection = value;
            PropagateNotification(NotificationLayoutDirectionChanged);
        }
    }

    /// <summary>Reports whether this control currently resolves its layout from right to left.</summary>
    public bool IsLayoutRTL()
    {
        ThrowIfDisposed();
        Tree?.EnsureOwnerThread();
        return _layoutDirection switch
        {
            LayoutDirection.LTR => false,
            LayoutDirection.RTL => true,
            LayoutDirection.SystemLocale => IsLocaleRTL(CultureInfo.CurrentUICulture),
            LayoutDirection.ApplicationLocale => IsLocaleRTL(GetApplicationCulture()),
            _ => InheritedLayoutRTL()
        };
    }

    private bool InheritedLayoutRTL()
    {
        var domain = TranslationDomain;
        for (Node? ancestor = Parent; ancestor is not null; ancestor = ancestor.Parent)
        {
            if (ancestor.TranslationDomain != domain) break;
            if (ancestor is Control control) return control.IsLayoutRTL();
        }
        return IsLocaleRTL(GetApplicationCulture());
    }

    private CultureInfo GetApplicationCulture()
    {
        return TranslationServer.GetOrAddDomain(TranslationDomain).EffectiveCulture;
    }

    private bool IsLocaleRTL(CultureInfo culture)
    {
        if (!culture.TextInfo.IsRightToLeft) return false;
        var domain = TranslationServer.GetOrAddDomain(TranslationDomain);
        return domain.HasTranslationForCulture(culture, exact: false)
            || TranslationServer.FallbackCulture?.TwoLetterISOLanguageName == culture.TwoLetterISOLanguageName;
    }

    /// <summary>Gets or sets which horizontal edge stays fixed when the minimum width grows.</summary>
    public GrowDirection GrowHorizontal
    {
        get { ThrowIfDisposed(); return _growHorizontal; }
        set { EnsureMutable(); ValidateGrowDirection(value); if (_growHorizontal == value) return; _growHorizontal = value; Reflow(); }
    }

    /// <summary>Gets or sets which vertical edge stays fixed when the minimum height grows.</summary>
    public GrowDirection GrowVertical
    {
        get { ThrowIfDisposed(); return _growVertical; }
        set { EnsureMutable(); ValidateGrowDirection(value); if (_growVertical == value) return; _growVertical = value; Reflow(); }
    }

    /// <summary>Gets the intrinsic minimum size supplied by this control type.</summary>
    public Vector2 GetMinimumSize()
    {
        ThrowIfDisposed();
        Tree?.EnsureOwnerThread();
        var size = OnGetMinimumSize();
        EnsureFinite(size, nameof(size));
        return size;
    }

    /// <summary>Gets the componentwise maximum of the intrinsic minimum, custom minimum, and zero.</summary>
    public Vector2 GetCombinedMinimumSize()
    {
        ThrowIfDisposed();
        var intrinsic = GetMinimumSize();
        return new(Mathf.Max(0, Mathf.Max(intrinsic.X, _customMinimumSize.X)),
            Mathf.Max(0, Mathf.Max(intrinsic.Y, _customMinimumSize.Y)));
    }

    /// <summary>Gets the intrinsic maximum size, with negative components meaning unbounded.</summary>
    public Vector2 GetMaximumSize()
    {
        ThrowIfDisposed();
        Tree?.EnsureOwnerThread();
        var size = OnGetMaximumSize();
        EnsureFinite(size, nameof(size));
        return size;
    }

    /// <summary>Gets the effective maximum from intrinsic, custom and propagated bounds.</summary>
    public Vector2 GetCombinedMaximumSize()
    {
        ThrowIfDisposed();
        var intrinsic = GetMaximumSize();
        var maximum = new Vector2(CombineMaximum(intrinsic.X, _customMaximumSize.X),
            CombineMaximum(intrinsic.Y, _customMaximumSize.Y));
        if (!TopLevel && Parent is Control { PropagateMaximumSize: true } parent)
        {
            var inherited = ContainerMaximum ?? parent.GetCombinedMaximumSize();
            maximum = new(CombineMaximum(maximum.X, inherited.X), CombineMaximum(maximum.Y, inherited.Y));
        }
        return maximum;
    }

    /// <summary>Gets the combined minimum capped by each enabled maximum component.</summary>
    public Vector2 GetBoundMinimumSize()
    {
        var minimum = GetCombinedMinimumSize();
        var maximum = GetCombinedMaximumSize();
        return new(maximum.X >= 0 ? Mathf.Min(minimum.X, maximum.X) : minimum.X,
            maximum.Y >= 0 ? Mathf.Min(minimum.Y, maximum.Y) : minimum.Y);
    }

    internal virtual Vector2 GetDesiredSize() => Vector2.Zero;

    internal Vector2 GetBoundDesiredSize()
    {
        var minimum = GetCombinedMinimumSize().Max(GetDesiredSize());
        var maximum = GetCombinedMaximumSize();
        return new(maximum.X >= 0 ? Mathf.Min(minimum.X, maximum.X) : minimum.X,
            maximum.Y >= 0 ? Mathf.Min(minimum.Y, maximum.Y) : minimum.Y);
    }

    /// <summary>Requests a coalesced minimum-size update after an intrinsic minimum changes.</summary>
    public void UpdateMinimumSize()
    {
        EnsureMutable();
        if (!IsInsideTree || !IsVisibleInTree || _minimumSizeUpdatePending) return;
        _minimumSizeUpdatePending = true;
        Tree!.Defer(_minimumSizeUpdateAction ??= ApplyMinimumSizeUpdate);
    }

    /// <summary>Requests a coalesced maximum-size update, invalidates child allocation caches and refreshes child bounds.</summary>
    public void UpdateMaximumSize()
    {
        EnsureMutable();
        for (var index = 0; index < GetChildCount(includeInternal: true); index++)
            if (GetChild(index, includeInternal: true) is Control { TopLevel: false } control)
            {
                control.ContainerMaximum = null;
                control.UpdateMaximumSize();
            }
        if (!IsInsideTree || !IsVisibleInTree || _maximumSizeUpdatePending) return;
        _maximumSizeUpdatePending = true;
        Tree!.Defer(_maximumSizeUpdateAction ??= ApplyMaximumSizeUpdate);
    }

    /// <summary>Supplies the intrinsic minimum size; the base control has none.</summary>
    protected virtual Vector2 OnGetMinimumSize() => Vector2.Zero;

    /// <summary>Supplies the intrinsic maximum size; the base control is unbounded.</summary>
    protected virtual Vector2 OnGetMaximumSize() => new(-1, -1);

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

    /// <summary>Gets or sets the portion of the current size added to PivotOffset.</summary>
    public Vector2 PivotOffsetRatio
    {
        get { ThrowIfDisposed(); return _pivotOffsetRatio; }
        set { EnsureMutable(); EnsureFinite(value, nameof(value)); if (_pivotOffsetRatio == value) return; _pivotOffsetRatio = value; NotifyLocalTransformChanged(); QueueRedraw(); }
    }

    /// <summary>Gets the absolute pivot plus its current size-relative component.</summary>
    public Vector2 GetCombinedPivotOffset()
    {
        ThrowIfDisposed();
        Tree?.EnsureOwnerThread();
        return _pivotOffset + _pivotOffsetRatio * _size;
    }

    /// <summary>Gets or sets whether the additional transform affects this control.</summary>
    public bool OffsetTransformEnabled
    {
        get { ThrowIfDisposed(); return _offsetTransformEnabled; }
        set { EnsureMutable(); if (_offsetTransformEnabled == value) return; _offsetTransformEnabled = value; NotifyLocalTransformChanged(); QueueRedraw(); }
    }

    /// <summary>Gets or sets the absolute translation of the additional transform.</summary>
    public Vector2 OffsetTransformPosition
    {
        get { ThrowIfDisposed(); return _offsetTransformPosition; }
        set { EnsureMutable(); EnsureFinite(value, nameof(value)); if (_offsetTransformPosition == value) return; _offsetTransformPosition = value; NotifyOffsetTransformChanged(); }
    }

    /// <summary>Gets or sets the size-relative translation of the additional transform.</summary>
    public Vector2 OffsetTransformPositionRatio
    {
        get { ThrowIfDisposed(); return _offsetTransformPositionRatio; }
        set { EnsureMutable(); EnsureFinite(value, nameof(value)); if (_offsetTransformPositionRatio == value) return; _offsetTransformPositionRatio = value; NotifyOffsetTransformChanged(); }
    }

    /// <summary>Gets or sets the componentwise scale of the additional transform.</summary>
    public Vector2 OffsetTransformScale
    {
        get { ThrowIfDisposed(); return _offsetTransformScale; }
        set { EnsureMutable(); EnsureFinite(value, nameof(value)); if (_offsetTransformScale == value) return; _offsetTransformScale = value; NotifyOffsetTransformChanged(); }
    }

    /// <summary>Gets or sets the rotation in radians of the additional transform.</summary>
    public float OffsetTransformRotation
    {
        get { ThrowIfDisposed(); return _offsetTransformRotation; }
        set { EnsureMutable(); EnsureFinite(value, nameof(value)); if (_offsetTransformRotation == value) return; _offsetTransformRotation = value; NotifyOffsetTransformChanged(); }
    }

    /// <summary>Gets or sets the absolute pivot of the additional transform.</summary>
    public Vector2 OffsetTransformPivot
    {
        get { ThrowIfDisposed(); return _offsetTransformPivot; }
        set { EnsureMutable(); EnsureFinite(value, nameof(value)); if (_offsetTransformPivot == value) return; _offsetTransformPivot = value; NotifyOffsetTransformChanged(); }
    }

    /// <summary>Gets or sets the size-relative pivot of the additional transform.</summary>
    public Vector2 OffsetTransformPivotRatio
    {
        get { ThrowIfDisposed(); return _offsetTransformPivotRatio; }
        set { EnsureMutable(); EnsureFinite(value, nameof(value)); if (_offsetTransformPivotRatio == value) return; _offsetTransformPivotRatio = value; NotifyOffsetTransformChanged(); }
    }

    /// <summary>Gets or sets whether the additional transform affects drawing only.</summary>
    public bool OffsetTransformVisualOnly
    {
        get { ThrowIfDisposed(); return _offsetTransformVisualOnly; }
        set { EnsureMutable(); if (_offsetTransformVisualOnly == value) return; _offsetTransformVisualOnly = value; NotifyOffsetTransformChanged(); }
    }

    private void NotifyOffsetTransformChanged()
    {
        if (!_offsetTransformEnabled) return;
        NotifyLocalTransformChanged();
        QueueRedraw();
    }

    private Transform GetOffsetTransform()
    {
        if (!_offsetTransformEnabled) return Transform.Identity;
        var translation = _offsetTransformPosition + _offsetTransformPositionRatio * _size;
        var pivot = _offsetTransformPivot + _offsetTransformPivotRatio * _size;
        var transform = new Transform(_offsetTransformRotation, _offsetTransformScale, 0f, pivot + translation);
        transform.Origin -= transform.BasisXform(pivot);
        return transform;
    }

    /// <summary>Gets or sets the transformed origin in global canvas coordinates.</summary>
    public Vector2 GlobalPosition
    {
        get => GetGlobalTransform().Origin;
        set => SetGlobalPosition(value);
    }

    /// <summary>Sets physical local position by changing offsets or, when requested, anchors.</summary>
    /// <param name="position">The finite upper-left point.</param>
    /// <param name="keepOffsets">Keep offsets and recompute anchors; requires a nonzero parent area.</param>
    public void SetPosition(Vector2 position, bool keepOffsets = false) => SetLayoutRect(position, _size, keepOffsets);

    /// <summary>Sets physical global position while keeping the local pivot transform.</summary>
    /// <param name="position">The finite global canvas point.</param>
    /// <param name="keepOffsets">Keep offsets and recompute anchors; requires a nonzero parent area.</param>
    public void SetGlobalPosition(Vector2 position, bool keepOffsets = false)
    {
        EnsureMutable(); EnsureFinite(position, nameof(position));
        var local = GetParentItem() is { } parent ? parent.GetGlobalTransform().AffineInverse() * position : position;
        SetPosition(_position + local - GetTransform().Origin, keepOffsets);
    }

    /// <summary>Sets size within its minimum and maximum bounds, changing offsets or anchors.</summary>
    /// <param name="size">The finite requested size, clamped to the current bounds.</param>
    /// <param name="keepOffsets">Keep offsets and recompute anchors; requires a nonzero parent area.</param>
    public void SetSize(Vector2 size, bool keepOffsets = false)
    {
        EnsureMutable(); EnsureFinite(size, nameof(size));
        var minimum = GetCombinedMinimumSize();
        var maximum = GetCombinedMaximumSize();
        size = new(Mathf.Max(size.X, minimum.X), Mathf.Max(size.Y, minimum.Y));
        if (maximum.X >= 0) size.X = Mathf.Min(size.X, maximum.X);
        if (maximum.Y >= 0) size.Y = Mathf.Min(size.Y, maximum.Y);
        SetLayoutRect(_position, size, keepOffsets);
    }

    /// <summary>Resets size to its effective minimum, capped by any maximum.</summary>
    public void ResetSize() => SetSize(Vector2.Zero);

    private void SetLayoutRect(Vector2 position, Vector2 size, bool keepOffsets, bool resetAnchors = false)
    {
        EnsureMutable(); EnsureFinite(position, nameof(position)); EnsureFinite(size, nameof(size));
        if (size.X < 0 || size.Y < 0) throw new ArgumentOutOfRangeException(nameof(size));
        var parentRect = GetParentAnchorRect();
        var area = parentRect.Size;
        if (keepOffsets && (area.X == 0 || area.Y == 0))
            throw new InvalidOperationException("Anchors require a nonzero parent area on both axes.");
        var logicalX = IsLayoutRTL() ? area.X + 2 * parentRect.Position.X - position.X - size.X : position.X;
        Span<float> values = stackalloc float[4];
        if (keepOffsets)
        {
            values[0] = (logicalX - _offsets[0]) / area.X;
            values[1] = (position.Y - _offsets[1]) / area.Y;
            values[2] = (logicalX + size.X - _offsets[2]) / area.X;
            values[3] = (position.Y + size.Y - _offsets[3]) / area.Y;
        }
        else
        {
            values[0] = logicalX - (resetAnchors ? 0 : _anchors[0]) * area.X;
            values[1] = position.Y - (resetAnchors ? 0 : _anchors[1]) * area.Y;
            values[2] = logicalX + size.X - (resetAnchors ? 0 : _anchors[2]) * area.X;
            values[3] = position.Y + size.Y - (resetAnchors ? 0 : _anchors[3]) * area.Y;
        }
        foreach (var value in values) EnsureFinite(value, nameof(position));
        if (resetAnchors) Array.Clear(_anchors);
        for (var index = 0; index < 4; index++)
            if (keepOffsets) _anchors[index] = values[index]; else _offsets[index] = values[index];
        Reflow();
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

    /// <summary>Gets the left and top offsets as a pair.</summary>
    public Vector2 GetBegin() { ThrowIfDisposed(); return new(_offsets[0], _offsets[1]); }

    /// <summary>Gets the right and bottom offsets as a pair.</summary>
    public Vector2 GetEnd() { ThrowIfDisposed(); return new(_offsets[2], _offsets[3]); }

    /// <summary>Sets the left and top offsets together and resolves the rectangle once.</summary>
    public void SetBegin(Vector2 position)
    {
        EnsureMutable(); EnsureFinite(position, nameof(position));
        if (_offsets[0] == position.X && _offsets[1] == position.Y) return;
        _offsets[0] = position.X; _offsets[1] = position.Y;
        Reflow();
    }

    /// <summary>Sets the right and bottom offsets together and resolves the rectangle once.</summary>
    public void SetEnd(Vector2 position)
    {
        EnsureMutable(); EnsureFinite(position, nameof(position));
        if (_offsets[2] == position.X && _offsets[3] == position.Y) return;
        _offsets[2] = position.X; _offsets[3] = position.Y;
        Reflow();
    }

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
    public void SetAnchorsPreset(LayoutPreset preset, bool keepOffsets = false)
    {
        EnsureMutable();
        var (left, top, right, bottom) = GetPresetAnchors(preset);
        SetAnchor(Side.Left, left, keepOffsets);
        SetAnchor(Side.Top, top, keepOffsets);
        SetAnchor(Side.Right, right, keepOffsets);
        SetAnchor(Side.Bottom, bottom, keepOffsets);
    }

    /// <summary>Sets one anchor and its local offset in sequence.</summary>
    /// <param name="side">The rectangle side to update.</param>
    /// <param name="anchor">A finite anchor fraction.</param>
    /// <param name="offset">A finite local offset.</param>
    /// <param name="pushOppositeAnchor">Move the opposite anchor when they cross.</param>
    public void SetAnchorAndOffset(Side side, float anchor, float offset, bool pushOppositeAnchor = false)
    {
        EnsureMutable();
        _ = SideIndex(side);
        EnsureFinite(anchor, nameof(anchor));
        EnsureFinite(offset, nameof(offset));
        SetAnchor(side, anchor, pushOppositeAnchor: pushOppositeAnchor);
        SetOffset(side, offset);
    }

    /// <summary>Places this control at a standard preset without changing its anchors.</summary>
    /// <param name="preset">The target placement.</param>
    /// <param name="resizeMode">Which current size components to preserve.</param>
    /// <param name="margin">The signed gap from edges used by the preset.</param>
    /// <remarks>All four offsets are committed before one rectangle reflow. Wide presets span their selected parent axis regardless of resize mode.</remarks>
    public void SetOffsetsPreset(LayoutPreset preset, LayoutPresetMode resizeMode = LayoutPresetMode.MinSize, int margin = 0)
    {
        EnsureMutable();
        var (left, top, right, bottom) = GetPresetAnchors(preset);
        if (resizeMode is < LayoutPresetMode.MinSize or > LayoutPresetMode.KeepSize)
            throw new ArgumentOutOfRangeException(nameof(resizeMode), resizeMode, "Unknown layout preset mode.");
        var minimum = GetMinimumSize();
        var width = resizeMode is LayoutPresetMode.MinSize or LayoutPresetMode.KeepHeight ? minimum.X : _size.X;
        var height = resizeMode is LayoutPresetMode.MinSize or LayoutPresetMode.KeepWidth ? minimum.Y : _size.Y;
        var parentRect = GetParentAnchorRect();
        var x = IsLayoutRTL() ? -width : parentRect.Size.X;
        var y = parentRect.Size.Y;
        var offsets = new float[4]
        {
            PresetEdge(left, x, width, margin, trailing: false) - _anchors[0] * x + parentRect.Position.X,
            PresetEdge(top, y, height, margin, trailing: false) - _anchors[1] * y + parentRect.Position.Y,
            PresetEdge(right, x, width, margin, trailing: true) - _anchors[2] * x + parentRect.Position.X,
            PresetEdge(bottom, y, height, margin, trailing: true) - _anchors[3] * y + parentRect.Position.Y
        };
        foreach (var offset in offsets) EnsureFinite(offset, nameof(margin));
        Array.Copy(offsets, _offsets, offsets.Length);
        Reflow();
    }

    /// <summary>Applies an anchor preset followed by the matching offset preset.</summary>
    /// <param name="preset">The standard arrangement.</param>
    /// <param name="resizeMode">Which current size components to preserve.</param>
    /// <param name="margin">The signed gap from edges used by the preset.</param>
    /// <remarks>The anchor step is retained if the following offset step rejects an invalid resize mode.</remarks>
    public void SetAnchorsAndOffsetsPreset(LayoutPreset preset, LayoutPresetMode resizeMode = LayoutPresetMode.MinSize, int margin = 0)
    {
        SetAnchorsPreset(preset);
        SetOffsetsPreset(preset, resizeMode, margin);
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
        return GetParentAnchorRect().Size;
    }

    private Rect2 GetParentAnchorRect()
    {
        ThrowIfDisposed();
        if (!IsInsideTree) return default;
        return GetParentItem() switch
        {
            Control parent => new Rect2(Vector2.Zero, parent.Size),
            null => GetViewport()?.GetVisibleRect() ?? default,
            _ => default
        };
    }

    /// <summary>Returns the unrotated local rectangle after pivot and scale.</summary>
    /// <returns>The transformed origin and scaled size.</returns>
    public Rect2 GetRect() { var transform = GetTransform(); return new(transform.Origin, transform.Scale * _size); }

    /// <summary>Returns the unrotated global rectangle after pivot and scale.</summary>
    /// <returns>The global transformed origin and scaled size.</returns>
    public Rect2 GetGlobalRect() { var transform = GetGlobalTransform(); return new(transform.Origin, transform.Scale * _size); }

    /// <inheritdoc />
    public override Transform GetTransform()
    {
        ThrowIfDisposed();
        Tree?.EnsureOwnerThread();
        var pivot = GetCombinedPivotOffset();
        var transform = new Transform(_rotation, _scale, 0f, pivot);
        transform.Origin += _position - transform.BasisXform(pivot);
        if (_offsetTransformEnabled && !_offsetTransformVisualOnly) transform *= GetOffsetTransform();
        return transform;
    }

    /// <inheritdoc />
    internal override Transform GetVisualTransform()
    {
        var transform = GetTransform();
        return _offsetTransformEnabled && _offsetTransformVisualOnly ? transform * GetOffsetTransform() : transform;
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
            try { Tree?.ReleaseGUIDrag(this); } catch (Exception error) { CollectException(ref errors, error); }
            ThrowCollected("Control visibility callbacks failed.", errors);
        }
        else base.OnNotification(what);
        if (what == NotificationEnterTree) ThemeOwner.NotifySelf();
        else if (what is NotificationParented or NotificationUnparented) ThemeOwner.ParentChanged();
        else if (what == NotificationThemeChanged) ProcessThemeNotification();
        if (what == NotificationEnterCanvas)
        {
            if (Parent is Container { IsDisposed: false } container)
            {
                ContainerMaximum = null;
                container.UpdateMinimumSize(); container.QueueSort();
            }
            _layoutParent = GetParentItem();
            if (_layoutParent is not null) _layoutParent.ItemRectChanged += OnParentRectChanged;
            else if ((_layoutViewport = GetViewport()) is not null) _layoutViewport.SizeChanged += Reflow;
            _lastMinimumSize = GetCombinedMinimumSize();
            _lastMaximumSize = GetCombinedMaximumSize();
            Reflow();
        }
        else if (what == NotificationExitCanvas) DisconnectLayoutSource();
        else if (what == NotificationVisibilityChanged && IsVisibleInTree) { UpdateMinimumSize(); UpdateMaximumSize(); }
        else if (what is NotificationLayoutDirectionChanged or NotificationTranslationChanged) Reflow();
        else if (what == NotificationResized) Resized?.Invoke();
    }

    /// <inheritdoc />
    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors() => base.GetPropertyDescriptors().Concat(ControlProperties).Concat(FocusProperties).Concat(SizeFlagProperties).Concat(ThemeProperties).Concat(TooltipProperties).Concat(ThemeOwner.Properties<Control>());

    /// <inheritdoc />
    protected override Func<Node> CreateSceneInstanceFactory() => GetType() == typeof(Control) ? CreateControl : base.CreateSceneInstanceFactory();

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        if (disposing) { _themeOwner?.Dispose(); ThemeChanged = null; DisconnectLayoutSource(); Resized = null; MinimumSizeChanged = null; MaximumSizeChanged = null; GUIInput = null; TextInput = null; IMECompositionChanged = null; SizeFlagsChanged = null; FocusEntered = null; FocusExited = null; MouseEntered = null; MouseExited = null; _forwardGetDragData = null; _forwardCanDropData = null; _forwardDropData = null; }
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

    private void ApplyMinimumSizeUpdate()
    {
        _minimumSizeUpdatePending = false;
        if (IsDisposed || !IsInsideTree || !IsVisibleInTree) return;
        var minimum = GetCombinedMinimumSize();
        if (minimum == _lastMinimumSize) return;
        _lastMinimumSize = minimum;
        Reflow();
        MinimumSizeChanged?.Invoke();
    }

    private void ApplyMaximumSizeUpdate()
    {
        _maximumSizeUpdatePending = false;
        if (IsDisposed || !IsInsideTree || !IsVisibleInTree) return;
        var maximum = GetCombinedMaximumSize();
        if (maximum == _lastMaximumSize) return;
        _lastMaximumSize = maximum;
        Reflow();
        MaximumSizeChanged?.Invoke();
    }

    private void Reflow()
    {
        var parentRect = GetParentAnchorRect();
        var area = parentRect.Size;
        var position = new Vector2(_offsets[0] + _anchors[0] * area.X, _offsets[1] + _anchors[1] * area.Y);
        var end = new Vector2(_offsets[2] + _anchors[2] * area.X, _offsets[3] + _anchors[3] * area.Y);
        var size = end - position;
        var minimum = GetCombinedMinimumSize();
        if (size.X < minimum.X)
        {
            position.X += (size.X - minimum.X) * GrowthShift(_growHorizontal);
            size.X = minimum.X;
        }
        var maximum = GetCombinedMaximumSize();
        if (maximum.X >= 0 && size.X > maximum.X)
        {
            position.X += (size.X - maximum.X) * GrowthShift(_growHorizontal);
            size.X = maximum.X;
        }
        if (IsLayoutRTL()) position.X = area.X + 2 * parentRect.Position.X - position.X - size.X;
        if (size.Y < minimum.Y)
        {
            position.Y += (size.Y - minimum.Y) * GrowthShift(_growVertical);
            size.Y = minimum.Y;
        }
        if (maximum.Y >= 0 && size.Y > maximum.Y)
        {
            position.Y += (size.Y - maximum.Y) * GrowthShift(_growVertical);
            size.Y = maximum.Y;
        }
        if (position == _position && size == _size) return;
        var sizeChanged = size != _size;
        _position = position; _size = size;
        NotifyLocalTransformChanged();
        NotifyItemRectChanged(sizeChanged);
        if (sizeChanged && IsInsideTree) DispatchNotification(NotificationResized);
    }

    private static (float Left, float Top, float Right, float Bottom) GetPresetAnchors(LayoutPreset preset)
    {
        return preset switch
        {
            LayoutPreset.TopLeft => (0f, 0f, 0f, 0f),
            LayoutPreset.TopRight => (1f, 0f, 1f, 0f),
            LayoutPreset.BottomLeft => (0f, 1f, 0f, 1f),
            LayoutPreset.BottomRight => (1f, 1f, 1f, 1f),
            LayoutPreset.CenterLeft => (0f, .5f, 0f, .5f),
            LayoutPreset.CenterTop => (.5f, 0f, .5f, 0f),
            LayoutPreset.CenterRight => (1f, .5f, 1f, .5f),
            LayoutPreset.CenterBottom => (.5f, 1f, .5f, 1f),
            LayoutPreset.Center => (.5f, .5f, .5f, .5f),
            LayoutPreset.LeftWide => (0f, 0f, 0f, 1f),
            LayoutPreset.TopWide => (0f, 0f, 1f, 0f),
            LayoutPreset.RightWide => (1f, 0f, 1f, 1f),
            LayoutPreset.BottomWide => (0f, 1f, 1f, 1f),
            LayoutPreset.VCenterWide => (.5f, 0f, .5f, 1f),
            LayoutPreset.HCenterWide => (0f, .5f, 1f, .5f),
            LayoutPreset.FullRect => (0f, 0f, 1f, 1f),
            _ => throw new ArgumentOutOfRangeException(nameof(preset), preset, "Unknown layout preset.")
        };
    }

    private static float PresetEdge(float target, float span, float size, int margin, bool trailing) => target switch
    {
        0f => trailing ? size + margin : margin,
        .5f => span * .5f + (trailing ? size * .5f : -size * .5f),
        1f => trailing ? span - margin : span - size - margin,
        _ => throw new ArgumentOutOfRangeException(nameof(target))
    };

    private static int SideIndex(Side side) => side is >= Side.Left and <= Side.Bottom ? (int)side : throw new ArgumentOutOfRangeException(nameof(side));
    private static float GrowthShift(GrowDirection direction) => direction switch
    {
        GrowDirection.Begin => 1f,
        GrowDirection.Both => .5f,
        _ => 0f
    };
    private static float CombineMaximum(float first, float second) => first < 0 ? second < 0 ? -1 : second : second < 0 ? first : Mathf.Min(first, second);
    private static void ValidateGrowDirection(GrowDirection direction)
    {
        if (direction is < GrowDirection.Begin or > GrowDirection.Both)
            throw new ArgumentOutOfRangeException(nameof(direction));
    }
    private static void EnsureFinite(float value, string name) { if (!Mathf.IsFinite(value)) throw new ArgumentOutOfRangeException(name); }
    private static void EnsureFinite(Vector2 value, string name) { if (!Mathf.IsFinite(value.X) || !Mathf.IsFinite(value.Y)) throw new ArgumentOutOfRangeException(name); }

    private static readonly PropertyDescriptor[] ControlProperties =
    [
        new PropertyDescriptor<Control, bool>(nameof(ClipContents), node => node.ClipContents, (node, value) => node.ClipContents = value, _ => false, stored: true),
        new PropertyDescriptor<Control, Vector2>(nameof(Position), node => node.Position, (node, value) => node.Position = value, _ => Vector2.Zero, stored: true),
        new PropertyDescriptor<Control, Vector2>(nameof(Size), node => node.Size, (node, value) => node.Size = value, _ => Vector2.Zero, stored: true),
        new PropertyDescriptor<Control, float>(nameof(RotationDegrees), node => node.RotationDegrees, (node, value) => node.RotationDegrees = value, _ => 0f, stored: true),
        new PropertyDescriptor<Control, Vector2>(nameof(Scale), node => node.Scale, (node, value) => node.Scale = value, _ => Vector2.One, stored: true),
        new PropertyDescriptor<Control, Vector2>(nameof(PivotOffset), node => node.PivotOffset, (node, value) => node.PivotOffset = value, _ => Vector2.Zero, stored: true),
        new PropertyDescriptor<Control, Vector2>(nameof(PivotOffsetRatio), node => node.PivotOffsetRatio, (node, value) => node.PivotOffsetRatio = value, _ => Vector2.Zero, stored: true),
        new PropertyDescriptor<Control, bool>(nameof(OffsetTransformEnabled), node => node.OffsetTransformEnabled, (node, value) => node.OffsetTransformEnabled = value, _ => false, stored: true),
        new PropertyDescriptor<Control, Vector2>(nameof(OffsetTransformPosition), node => node.OffsetTransformPosition, (node, value) => node.OffsetTransformPosition = value, _ => Vector2.Zero, stored: true),
        new PropertyDescriptor<Control, Vector2>(nameof(OffsetTransformPositionRatio), node => node.OffsetTransformPositionRatio, (node, value) => node.OffsetTransformPositionRatio = value, _ => Vector2.Zero, stored: true),
        new PropertyDescriptor<Control, Vector2>(nameof(OffsetTransformScale), node => node.OffsetTransformScale, (node, value) => node.OffsetTransformScale = value, _ => Vector2.One, stored: true),
        new PropertyDescriptor<Control, float>(nameof(OffsetTransformRotation), node => node.OffsetTransformRotation, (node, value) => node.OffsetTransformRotation = value, _ => 0f, stored: true),
        new PropertyDescriptor<Control, Vector2>(nameof(OffsetTransformPivot), node => node.OffsetTransformPivot, (node, value) => node.OffsetTransformPivot = value, _ => Vector2.Zero, stored: true),
        new PropertyDescriptor<Control, Vector2>(nameof(OffsetTransformPivotRatio), node => node.OffsetTransformPivotRatio, (node, value) => node.OffsetTransformPivotRatio = value, _ => new Vector2(.5f, .5f), stored: true),
        new PropertyDescriptor<Control, bool>(nameof(OffsetTransformVisualOnly), node => node.OffsetTransformVisualOnly, (node, value) => node.OffsetTransformVisualOnly = value, _ => true, stored: true),
        new PropertyDescriptor<Control, Vector2>(nameof(CustomMinimumSize), node => node.CustomMinimumSize, (node, value) => node.CustomMinimumSize = value, _ => Vector2.Zero, stored: true),
        new PropertyDescriptor<Control, Vector2>(nameof(CustomMaximumSize), node => node.CustomMaximumSize, (node, value) => node.CustomMaximumSize = value, _ => new Vector2(-1, -1), stored: true),
        new PropertyDescriptor<Control, bool>(nameof(PropagateMaximumSize), node => node.PropagateMaximumSize, (node, value) => node.PropagateMaximumSize = value, _ => false, stored: true),
        new PropertyDescriptor<Control, LayoutDirection>(nameof(LayoutDirection), node => node.LayoutDirection, (node, value) => node.LayoutDirection = value, _ => LayoutDirection.Inherited, stored: true),
        new PropertyDescriptor<Control, GrowDirection>(nameof(GrowHorizontal), node => node.GrowHorizontal, (node, value) => node.GrowHorizontal = value, _ => GrowDirection.End, stored: true),
        new PropertyDescriptor<Control, GrowDirection>(nameof(GrowVertical), node => node.GrowVertical, (node, value) => node.GrowVertical = value, _ => GrowDirection.End, stored: true),
        new PropertyDescriptor<Control, float>(nameof(AnchorLeft), node => node.AnchorLeft, (node, value) => node.AnchorLeft = value, _ => 0f, stored: true),
        new PropertyDescriptor<Control, float>(nameof(AnchorTop), node => node.AnchorTop, (node, value) => node.AnchorTop = value, _ => 0f, stored: true),
        new PropertyDescriptor<Control, float>(nameof(AnchorRight), node => node.AnchorRight, (node, value) => node.AnchorRight = value, _ => 0f, stored: true),
        new PropertyDescriptor<Control, float>(nameof(AnchorBottom), node => node.AnchorBottom, (node, value) => node.AnchorBottom = value, _ => 0f, stored: true),
        new PropertyDescriptor<Control, float>(nameof(OffsetLeft), node => node.OffsetLeft, (node, value) => node.OffsetLeft = value, _ => 0f, stored: true),
        new PropertyDescriptor<Control, float>(nameof(OffsetTop), node => node.OffsetTop, (node, value) => node.OffsetTop = value, _ => 0f, stored: true),
        new PropertyDescriptor<Control, float>(nameof(OffsetRight), node => node.OffsetRight, (node, value) => node.OffsetRight = value, _ => 0f, stored: true),
        new PropertyDescriptor<Control, float>(nameof(OffsetBottom), node => node.OffsetBottom, (node, value) => node.OffsetBottom = value, _ => 0f, stored: true),
        new PropertyDescriptor<Control, MouseFilter>(nameof(MouseFilter), node => node.MouseFilter, (node, value) => node.MouseFilter = value, _ => MouseFilter.Stop, stored: true),
        new PropertyDescriptor<Control, MouseBehaviorRecursive>(nameof(MouseBehaviorRecursive), node => node.MouseBehaviorRecursive, (node, value) => node.MouseBehaviorRecursive = value, _ => MouseBehaviorRecursive.Inherited, stored: true),
        new PropertyDescriptor<Control, bool>(nameof(MouseForcePassScrollEvents), node => node.MouseForcePassScrollEvents, (node, value) => node.MouseForcePassScrollEvents = value, _ => true, stored: true),
        new PropertyDescriptor<Control, FocusMode>(nameof(FocusMode), node => node.FocusMode, (node, value) => node.FocusMode = value, _ => FocusMode.None, stored: true),
        new PropertyDescriptor<Control, FocusBehaviorRecursive>(nameof(FocusBehaviorRecursive), node => node.FocusBehaviorRecursive, (node, value) => node.FocusBehaviorRecursive = value, _ => FocusBehaviorRecursive.Inherited, stored: true),
        new PropertyDescriptor<Control, CursorShape>(nameof(MouseDefaultCursorShape), node => node.MouseDefaultCursorShape, (node, value) => node.MouseDefaultCursorShape = value, _ => CursorShape.Arrow, stored: true)
    ];
}
