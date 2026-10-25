namespace Electron2D;

public partial class Window
{
    internal Viewport? Embedder;
    private bool _transient, _exclusive, _wrapControls;
    private Window? _transientParent;
    internal Window? ExclusiveChild { get; private set; }
    private LayoutDirection _layoutDirection;
    /// <summary>Gets or sets the direction inherited by this window's controls.</summary>
    /// <value>Inherited initially; roots use the project direction policy.</value>
    /// <exception cref="ArgumentOutOfRangeException">The direction is undefined or Max.</exception>
    public LayoutDirection LayoutDirection
    { get { ThrowIfDisposed(); return _layoutDirection; } set { EnsureMutable(); if (value is < LayoutDirection.Inherited or >= LayoutDirection.Max) throw new ArgumentOutOfRangeException(nameof(value)); if (_layoutDirection == value) return; _layoutDirection = value; PropagateNotification(Control.NotificationLayoutDirectionChanged); QueueEmbeddedRedraw(); } }
    /// <summary>Reports whether the resolved window layout direction is right to left.</summary>
    /// <returns>The explicit, inherited or translated locale direction.</returns>
    /// <remarks>Inherited nodes follow same-domain Window/Control ancestry, then the project root policy.
    /// Forced RTL applies to inherited and locale modes; explicit LTR/RTL remain authoritative. Tree policies are sampled at activation.</remarks>
    public bool IsLayoutRTL()
    {
        ThrowIfDisposed(); Tree?.EnsureOwnerThread();
        return LayoutDirectionPolicy.Resolve(this, _layoutDirection);
    }
    private CanvasLayer? _embeddedLayer;
    private int _embeddedSerial;
    private EmbeddedWindowCanvas? _embeddedCanvas;
    /// <summary>Gets or sets whether this window belongs to its containing parent window's transient chain.</summary>
    /// <value>False initially; Popup defaults to true. Popup methods enable it before showing.</value>
    /// <remarks>While attached, the nearest parent window owns this link. Clearing it also removes this window's active
    /// exclusive selection. Roots and hierarchies without a parent window have no transient owner.</remarks>
    /// <exception cref="ObjectDisposedException">Disposal has started.</exception>
    /// <exception cref="InvalidOperationException">Mutation violates the scene owner, capture or authoring scope.</exception>
    /// <exception cref="System.IO.IOException">The diagnostic writer fails after a conflicting selection commits.</exception>
    public bool Transient
    {
        get { ThrowIfDisposed(); return _transient; }
        set { EnsureMutable(); if (_transient == value) return; _transient = value; UpdateTransientMembership(IsInsideTree); }
    }
    /// <summary>Gets or sets whether input outside this window is blocked while visible.</summary>
    /// <value>False initially.</value>
    /// <remarks>A visible attached transient window becomes its parent's active exclusive child. The latest activation wins
    /// when multiple siblings request exclusivity, with a diagnostic; clearing that selection does not reactivate older siblings.</remarks>
    /// <exception cref="ObjectDisposedException">Disposal has started.</exception>
    /// <exception cref="InvalidOperationException">Mutation violates the scene owner, capture or authoring scope.</exception>
    /// <exception cref="System.IO.IOException">The diagnostic writer fails after a conflicting selection commits.</exception>
    public bool Exclusive
    {
        get { ThrowIfDisposed(); return _exclusive; }
        set { EnsureMutable(); if (_exclusive == value) return; _exclusive = value; UpdateExclusiveSelection(); }
    }
    /// <summary>Gets or sets whether child control minimums constrain this window's size.</summary>
    /// <value>False initially; Popup defaults to true.</value>
    public bool WrapControls { get { ThrowIfDisposed(); return _wrapControls; } set { EnsureMutable(); _wrapControls = value; UpdateEmbeddedContents(); } }
    /// <summary>Gets or sets the popup input and outside-click close-request policy.</summary>
    /// <value>False initially; Popup defaults to true.</value>
    /// <exception cref="InvalidOperationException">The policy changes while visible and attached.</exception>
    public bool PopupWindow { get => GetFlag(WindowFlag.Popup); set => SetFlag(WindowFlag.Popup, value); }
    /// <summary>Gets or sets the popup window-manager role, using an embedded host or a native parent-relative surface.</summary>
    /// <value>False initially; Popup defaults to true. Hide a native child before changing its role.</value>
    public bool PopupWMHint { get => GetFlag(WindowFlag.PopupWmHint); set => SetFlag(WindowFlag.PopupWmHint, value); }
    /// <summary>Gets or sets whether user maximization is disabled.</summary>
    /// <value>False initially; Popup defaults to true.</value>
    public bool MaximizeDisabled { get => GetFlag(WindowFlag.MaximizeDisabled); set => SetFlag(WindowFlag.MaximizeDisabled, value); }
    /// <summary>Gets or sets whether user minimization is disabled.</summary>
    /// <value>False initially; Popup defaults to true.</value>
    public bool MinimizeDisabled { get => GetFlag(WindowFlag.MinimizeDisabled); set => SetFlag(WindowFlag.MinimizeDisabled, value); }
    /// <summary>Gets or sets transparent window composition, configured before native window creation.</summary>
    /// <value>False initially; PopupPanel defaults to true.</value>
    /// <remarks>Main and child native surfaces select alpha before creation. Compatibility presents native alpha;
    /// GPU requires permitted fallback or rejects startup. Changing native alpha while open is unsupported.
    /// Embedded composition retains its viewport host.</remarks>
    /// <exception cref="NotSupportedException">Native alpha would change after creation or the backend cannot present it.</exception>
    public bool Transparent { get => GetFlag(WindowFlag.Transparent); set => SetFlag(WindowFlag.Transparent, value); }
    /// <summary>Occurs before popup positioning, sizing and visibility changes.</summary>
    public event Action? AboutToPopup { add => _publisherAboutToPopup.Add(value); remove => _publisherAboutToPopup.Remove(value); }
    private EventPublisher<Action> _publisherAboutToPopup;
    /// <summary>Reports whether this window uses an attached viewport's embedded host.</summary>
    /// <returns>True only while attached to an embedding viewport.</returns>
    public bool IsEmbedded() { ThrowIfDisposed(); Tree?.EnsureOwnerThread(); return Embedder is not null; }
    /// <summary>Returns the maximum extent of direct child controls and their minimum sizes.</summary>
    /// <returns>Finite nonnegative content dimensions, independent of WrapControls.</returns>
    public Vector2 GetContentsMinimumSize() { ThrowIfDisposed(); Tree?.EnsureOwnerThread(); var size = OnGetContentsMinimumSize(); if (!size.IsFinite() || size.X < 0 || size.Y < 0) throw new InvalidOperationException("Content minimum must be finite and nonnegative."); return size; }
    /// <summary>Computes the minimum dimensions required by direct child controls.</summary>
    /// <returns>The component-wise maximum of child positions plus bounded minimum sizes.</returns>
    protected virtual Vector2 OnGetContentsMinimumSize()
    {
        var size = Vector2.Zero;
        for (var i = 0; i < GetChildCount(); i++) if (GetChild(i) is Control control) size = size.Max(control.Position + control.GetBoundMinimumSize());
        return size;
    }
    /// <summary>Returns the embedded client rectangle or the native parent's usable display rectangle.</summary>
    /// <returns>Coordinates in the embedded host or native desktop plane.</returns>
    /// <exception cref="InvalidOperationException">The window is detached or is the root window.</exception>
    internal Rect2i GetUsableParentRect() { CheckPopup(); if (Embedder is null) { var parent = PopupParentDisplay(); return parent.ScreenGetUsableRectCore(parent.WindowGetCurrentScreenCore()); } var rect = Embedder.GetVisibleRect(); return new((Vector2i)rect.Position, (Vector2i)rect.Size); }
    /// <summary>Shows this child window, applying an optional host-coordinate rectangle.</summary>
    /// <param name="rect">An explicit position and size, or zero to retain configured geometry.</param>
    /// <remarks>Requires an embedding viewport or active native desktop host. Native explicit rectangles use desktop coordinates;
    /// Wayland rejects unavailable global placement. PopupOnParent uses real relative native placement. AboutToPopup precedes geometry changes.</remarks>
    public void Popup(Rect2i rect = default)
    {
        PopupAt(rect, false);
    }
    private void PopupAt(Rect2i rect, bool centered, bool nativeRelative = false)
    {
        EnsureMutable(); if (TryNativePopup()) return; CheckPopup(); var placementRoot = _popupPlacementRoot; BeginEventEmission(_publisherAboutToPopup.Handlers)?.Invoke(); if (IsDisposed || !IsInsideTree) return; if (nativeRelative) _popupPlacementRoot = placementRoot;
        if (rect != default) { Size = rect.Size; if (nativeRelative) PopupPosition = rect.Position; else Position = rect.Position; }
        PreparePopup();
        UpdateEmbeddedContents(); if (centered) { var area = GetPopupCenterRect(); Position = area.Position + (area.Size - Size) / 2; }
        AdjustPopup(); Transient = true; Show(); GrabFocus();
    }
    /// <summary>Shows this window at a rectangle relative to its parent window.</summary>
    /// <param name="parentRect">An embedded host rectangle, or native parent-client rectangle in public window units.</param>
    /// <exception cref="OverflowException">The transformed rectangle exceeds native integer coordinates.</exception>
    public void PopupOnParent(Rect2i parentRect) => PopupRelativeToParent(parentRect);
    /// <summary>Shows this window centered in its embedding viewport or the complete native parent display.</summary>
    /// <param name="minSize">Requested dimensions, or zero to retain the current size.</param>
    public void PopupCentered(Vector2i minSize = default) { EnsureMutable(); if (TryNativePopup()) return; CheckPopup(); if (minSize == default) minSize = Size; var area = GetPopupCenterRect(); PopupAt(new(area.Position + (area.Size - minSize) / 2, minSize), true); }
    /// <summary>Shows this window centered with dimensions proportional to its embedded host or native parent display.</summary>
    /// <param name="ratio">A finite ratio greater than zero and at most one.</param>
    public void PopupCenteredRatio(float ratio = .8f) { EnsureMutable(); CheckRatio(ratio); if (TryNativePopup()) return; CheckPopup(); PopupCentered((Vector2i)((Vector2)GetPopupCenterRect().Size * ratio)); }
    /// <summary>Shows this window centered, limiting dimensions to an embedded-host or native-display ratio.</summary>
    /// <param name="minSize">Requested dimensions, or zero to retain current dimensions.</param>
    /// <param name="fallbackRatio">A finite ratio greater than zero and at most one.</param>
    public void PopupCenteredClamped(Vector2i minSize = default, float fallbackRatio = .75f) { EnsureMutable(); CheckRatio(fallbackRatio); if (TryNativePopup()) return; CheckPopup(); if (minSize == default) minSize = Size; var limit = (Vector2i)((Vector2)GetPopupCenterRect().Size * fallbackRatio); PopupCentered(new(Math.Min(minSize.X, limit.X), Math.Min(minSize.Y, limit.Y))); }
    /// <summary>Parents a detached dialog to the last exclusive window and shows it with an optional host-coordinate rectangle.</summary>
    /// <param name="fromNode">An attached node identifying the owning window.</param>
    /// <param name="rect">The popup geometry argument passed to Popup.</param>
    /// <remarks>Uses <see cref="Node.GetLastExclusiveWindow"/> and attempts detachment after attachment or popup failure.</remarks>
    /// <exception cref="ArgumentNullException"><paramref name="fromNode"/> is null.</exception>
    /// <exception cref="ObjectDisposedException">This dialog or its source node is disposed.</exception>
    /// <exception cref="InvalidOperationException">This dialog is already attached, or the source has no active containing window.</exception>
    /// <exception cref="Exception">A callback fails. If detachment also fails, both errors are retained in an <see cref="AggregateException"/>.</exception>
    public void PopupExclusive(Node fromNode, Rect2i rect = default) => ParentDialog(fromNode, () => Popup(rect));
    /// <summary>Parents a detached dialog to the last exclusive window and shows it with a parent-relative rectangle.</summary>
    /// <param name="fromNode">An attached node identifying the owning window.</param>
    /// <param name="parentRect">The popup geometry argument passed to PopupOnParent.</param>
    /// <remarks>Uses <see cref="Node.GetLastExclusiveWindow"/> and attempts detachment after attachment or popup failure.</remarks>
    /// <exception cref="ArgumentNullException"><paramref name="fromNode"/> is null.</exception>
    /// <exception cref="ObjectDisposedException">This dialog or its source node is disposed.</exception>
    /// <exception cref="InvalidOperationException">This dialog is already attached, or the source has no active containing window.</exception>
    /// <exception cref="Exception">A callback fails. If detachment also fails, both errors are retained in an <see cref="AggregateException"/>.</exception>
    public void PopupExclusiveOnParent(Node fromNode, Rect2i parentRect) => ParentDialog(fromNode, () => PopupOnParent(parentRect));
    /// <summary>Parents a detached dialog to the last exclusive window and shows it with centered dimensions.</summary>
    /// <param name="fromNode">An attached node identifying the owning window.</param>
    /// <param name="minSize">The popup geometry argument passed to PopupCentered.</param>
    /// <remarks>Uses <see cref="Node.GetLastExclusiveWindow"/> and attempts detachment after attachment or popup failure.</remarks>
    /// <exception cref="ArgumentNullException"><paramref name="fromNode"/> is null.</exception>
    /// <exception cref="ObjectDisposedException">This dialog or its source node is disposed.</exception>
    /// <exception cref="InvalidOperationException">This dialog is already attached, or the source has no active containing window.</exception>
    /// <exception cref="Exception">A callback fails. If detachment also fails, both errors are retained in an <see cref="AggregateException"/>.</exception>
    public void PopupExclusiveCentered(Node fromNode, Vector2i minSize = default) => ParentDialog(fromNode, () => PopupCentered(minSize));
    /// <summary>Parents a detached dialog to the last exclusive window and shows it with a proportional host size.</summary>
    /// <param name="fromNode">An attached node identifying the owning window.</param>
    /// <param name="ratio">The popup geometry argument passed to PopupCenteredRatio.</param>
    /// <remarks>Uses <see cref="Node.GetLastExclusiveWindow"/> and attempts detachment after attachment or popup failure.</remarks>
    /// <exception cref="ArgumentNullException"><paramref name="fromNode"/> is null.</exception>
    /// <exception cref="ObjectDisposedException">This dialog or its source node is disposed.</exception>
    /// <exception cref="InvalidOperationException">This dialog is already attached, or the source has no active containing window.</exception>
    /// <exception cref="Exception">A callback fails. If detachment also fails, both errors are retained in an <see cref="AggregateException"/>.</exception>
    public void PopupExclusiveCenteredRatio(Node fromNode, float ratio = .8f) => ParentDialog(fromNode, () => PopupCenteredRatio(ratio));
    /// <summary>Parents a detached dialog to the last exclusive window and shows it with clamped centered dimensions.</summary>
    /// <param name="fromNode">An attached node identifying the owning window.</param>
    /// <param name="minSize">The popup geometry argument passed to PopupCenteredClamped.</param>
    /// <param name="fallbackRatio">The popup geometry argument passed to PopupCenteredClamped.</param>
    /// <remarks>Uses <see cref="Node.GetLastExclusiveWindow"/> and attempts detachment after attachment or popup failure.</remarks>
    /// <exception cref="ArgumentNullException"><paramref name="fromNode"/> is null.</exception>
    /// <exception cref="ObjectDisposedException">This dialog or its source node is disposed.</exception>
    /// <exception cref="InvalidOperationException">This dialog is already attached, or the source has no active containing window.</exception>
    /// <exception cref="Exception">A callback fails. If detachment also fails, both errors are retained in an <see cref="AggregateException"/>.</exception>
    public void PopupExclusiveCenteredClamped(Node fromNode, Vector2i minSize = default, float fallbackRatio = .75f) => ParentDialog(fromNode, () => PopupCenteredClamped(minSize, fallbackRatio));
    private void ParentDialog(Node fromNode, Action show)
    {
        EnsureMutable(); ArgumentNullException.ThrowIfNull(fromNode); if (fromNode.IsDisposed) throw new ObjectDisposedException(nameof(fromNode));
        if (Parent is not null || IsInsideTree) throw new InvalidOperationException("Exclusive popup requires a detached dialog.");
        var parent = fromNode.GetLastExclusiveWindow() ?? throw new InvalidOperationException("Dialog owner has no containing window.");
        if (parent.Tree is null) throw new InvalidOperationException("Dialog owner must be attached.");
        try { parent.AddChild(this); show(); }
        catch (Exception error)
        {
            try { if (!IsDisposed && Parent == parent) parent.RemoveChild(this); }
            catch (Exception cleanup) { throw new AggregateException("Dialog attachment and cleanup failed.", error, cleanup); }
            throw;
        }
    }
    private static void CheckRatio(float ratio) { if (!float.IsFinite(ratio) || ratio <= 0 || ratio > 1) throw new ArgumentOutOfRangeException(nameof(ratio)); }
    private void UpdateTransientMembership(bool entering)
    {
        if (_transientParent?.ExclusiveChild == this) _transientParent.ExclusiveChild = null;
        _transientParent = entering && _transient ? Parent?.GetWindow() : null;
        UpdateExclusiveSelection();
        UpdateNativeTransient();
    }
    private void UpdateExclusiveSelection()
    {
        UpdateNativeTransient();
        if (_transientParent is not { } parent) return;
        if (_exclusive && _visible && IsInsideTree)
        {
            var previous = parent.ExclusiveChild;
            parent.ExclusiveChild = this;
            if (previous is not null && previous != this) OS.LogError("A window already has another active exclusive child.");
        }
        else if (parent.ExclusiveChild == this) parent.ExclusiveChild = null;
    }
    private void CheckPopup() { EnsureMutable(); if (!IsInsideTree || Parent is null) throw new InvalidOperationException("Popup requires an attached child window."); if (Embedder is null && (Tree?.Root is not Window { NativeDisplay: not null } || RenderingServer.Service is null)) throw new NotSupportedException("Popup presentation requires an embedded viewport or an active native scene host."); }
    internal virtual bool TryNativeVisibility(bool visible) => false;
    internal virtual bool TryNativePopup() => false;
    internal virtual void AfterVisibilityChanged(bool visible) { }
    internal virtual bool AcceptEmbeddedPointer(Vector2 point, InputEvent input) => true;
    internal virtual void PreparePopup() { }
    internal virtual void AdjustPopup() { }
    internal void UpdateEmbeddedContents()
    {
        if (WrapControls || _keepTitleVisible) { var size = _size.Max(GetWindowMinimumSize()); if (MaxSize.X > 0) size.X = Math.Min(size.X, MaxSize.X); if (MaxSize.Y > 0) size.Y = Math.Min(size.Y, MaxSize.Y); if (size != _size) RequestMeasuredWindowSize(size); }
        if (ClampToEmbedder && Embedder != null) Position = Position;
        _embeddedCanvas?.QueueRedraw();
    }
    internal void EmbeddedVisibilityChanged()
    {
        if (Embedder is null) { if (Visible && IsInsideTree && Parent is not null) OpenNativeChild(); return; }
        if (Visible) ApplyInitialEmbeddedPosition();
        if (_embeddedCanvas is not null) { _embeddedCanvas.Visible = Visible; _embeddedCanvas.QueueRedraw(); }
        if (Visible) { Embedder.EmbeddedWindows.Remove(this); Embedder.EmbeddedWindows.Add(this); if (_embeddedLayer?.Parent is { } p) p.MoveChild(_embeddedLayer, -1); Tree?.FocusEmbeddedWindow(this); }
        else Tree?.HideEmbeddedWindow(this);
    }
    internal void QueueEmbeddedRedraw() => _embeddedCanvas?.QueueRedraw();
    internal void RaiseEmbeddedCanvas() { if (_embeddedLayer?.Parent is { } parent) parent.MoveChild(_embeddedLayer, -1); }
    internal Rect2 EmbeddedHitRect => Borderless ? new(Position, Size) : EmbeddedFrameRect.Grow(Unresizable ? 0 : Math.Max(0, GetThemeConstant("resize_margin", "Window")));
    internal void RequestEmbeddedClose() => EmitWindowEvent(NotificationWmCloseRequest);
    internal void EmitEmbeddedFocus(bool focus) => HandleFocus(focus);
    private void UpdateEmbeddedMembership(bool entering)
    {
        if (!entering)
        {
            List<Exception>? errors = null;
            if (ReferenceEquals(_focusedWindow, this)) _focusedWindow = null;
            try { if (Embedder is not null) Tree?.HideEmbeddedWindow(this); } catch (Exception error) { CollectException(ref errors, error); }
            try { if (Parent is not null) CloseNative(); } catch (Exception error) { CollectException(ref errors, error); }
            Embedder?.EmbeddedWindows.Remove(this); Embedder = null;
            try
            {
                if (_embeddedCanvas is { IsDisposed: false } canvas) canvas.Visible = false;
                if (_embeddedLayer is { IsDisposed: false } layer && Tree is { IsClosing: false } tree) tree.Defer(layer.Dispose);
            }
            catch (Exception error) { CollectException(ref errors, error); }
            _embeddedLayer = null; _embeddedCanvas = null;
            ThrowCollected("Embedded window cleanup failed.", errors); return;
        }
        if (Parent is null) return;
        for (var parent = Parent; parent is not null; parent = parent.Parent) if (!_forceNative && parent is Viewport { GUIEmbedSubwindows: true } viewport) { Embedder = viewport; break; }
        if (Embedder is null) { if (Visible) OpenNativeChild(); return; }
        Embedder.EmbeddedWindows.Add(this);
        _embeddedLayer = new CanvasLayer { Name = "_embedded_window_" + InstanceID + "_" + (++_embeddedSerial), Layer = int.MaxValue };
        _embeddedCanvas = new(this) { Name = "_window_canvas", MouseFilter = MouseFilter.Ignore, Visible = Visible };
        _embeddedLayer.AddChild(_embeddedCanvas, InternalMode.Back); Embedder.AddChild(_embeddedLayer, InternalMode.Back);
        EmbeddedVisibilityChanged();
    }
    private sealed class EmbeddedWindowCanvas(Window window) : Control
    {
        private readonly TextLine _title = new();
        private string? _titleSource;
        private Font? _titleFont;
        private int _titleSize;
        protected override void Dispose(bool disposing)
        { try { if (disposing) _title.Dispose(); } finally { base.Dispose(disposing); } }
        protected override void OnDraw()
        {
            base.OnDraw(); if (window.IsDisposed || !window.Visible) return;
            var rect = new Rect2(window.Position, window.Size);
            if (!window.Borderless)
            {
                var panel = window.GetThemeStyleBox(window.HasFocus() ? "embedded_border" : "embedded_unfocused_border", "Window");
                if (panel is not null) DrawStyleBox(panel, rect);
                if (window.GetThemeFont("title_font", "Window") is { } font)
                {
                    var fontSize = window.GetThemeFontSize("title_font_size", "Window");
                    if (_titleSource != window.Title || _titleFont != font || _titleSize != fontSize)
                    {
                        _title.Clear(); _title.AddString(window.Title, font, fontSize); _titleSource = window.Title; _titleFont = font; _titleSize = fontSize;
                    }
                    var width = rect.Size.X - (panel?.GetMinimumSize().X ?? 0) - window.GetThemeConstant("close_h_offset", "Window");
                    if (width > 0)
                    {
                        _title.Width = width; _title.Direction = window.IsLayoutRTL() ? TextDirection.RTL : TextDirection.LTR;
                        var size = _title.GetSize(); var at = rect.Position + new Vector2((int)((rect.Size.X - size.X) / 2), (int)((-window.EmbeddedTitleHeight - size.Y) / 2));
                        var outline = Math.Max(0, window.GetThemeConstant("title_outline_size", "Window")); var color = window.GetThemeColor("title_outline_modulate", "Window");
                        if (outline > 0 && color.A > 0) _title.DrawOutline(this, at, outline, color);
                        _title.Draw(this, at, window.GetThemeColor("title_color", "Window"));
                    }
                }
                if (window.GetThemeIcon(window._embeddedClosePressed && window.HasFocus() ? "close_pressed" : "close", "Window") is { } icon)
                    DrawTexture(icon, window.EmbeddedCloseRect.Position);
            }
            if (!window.Transparent) DrawRect(rect, Colors.Black);
            var fitted = window.ContentLayout.ScreenRect;
            var destination = new Rect2(fitted.Position, fitted.Size).Intersection(new Rect2(Vector2.Zero, window.Size));
            if (destination.HasArea())
            {
                var texture = window.GetTexture(); var dimensions = texture.GetSize();
                var source = new Rect2((destination.Position - (Vector2)fitted.Position) / (Vector2)fitted.Size * dimensions, destination.Size / (Vector2)fitted.Size * dimensions);
                DrawTextureRectRegion(texture, new(rect.Position + destination.Position, destination.Size), source, clipUV: false);
            }
        }
    }
    private static readonly PropertyDescriptor[] PopupProperties =
    [
        new PropertyDescriptor<Window,bool>(nameof(TransientToFocused),w=>w.TransientToFocused,(w,v)=>w.TransientToFocused=v,_=>false,stored:true),
        new PropertyDescriptor<Window,bool>(nameof(ForceNative),w=>w.ForceNative,(w,v)=>w.ForceNative=v,_=>false,stored:true),
        new PropertyDescriptor<Window,bool>(nameof(KeepTitleVisible),w=>w.KeepTitleVisible,(w,v)=>w.KeepTitleVisible=v,_=>false,stored:true),
        new PropertyDescriptor<Window,LayoutDirection>(nameof(LayoutDirection),w=>w.LayoutDirection,(w,v)=>w.LayoutDirection=v,_=>LayoutDirection.Inherited,stored:true),
        new PropertyDescriptor<Window,bool>(nameof(Transient),w=>w.Transient,(w,v)=>w.Transient=v,_=>false,stored:true),
        new PropertyDescriptor<Window,bool>(nameof(Exclusive),w=>w.Exclusive,(w,v)=>w.Exclusive=v,_=>false,stored:true),
        new PropertyDescriptor<Window,bool>(nameof(WrapControls),w=>w.WrapControls,(w,v)=>w.WrapControls=v,_=>false,stored:true),
        new PropertyDescriptor<Window,bool>(nameof(PopupWindow),w=>w.PopupWindow,(w,v)=>w.PopupWindow=v,_=>false,stored:true),
        new PropertyDescriptor<Window,bool>(nameof(PopupWMHint),w=>w.PopupWMHint,(w,v)=>w.PopupWMHint=v,_=>false,stored:true),
        new PropertyDescriptor<Window,bool>(nameof(MinimizeDisabled),w=>w.MinimizeDisabled,(w,v)=>w.MinimizeDisabled=v,_=>false,stored:true),
        new PropertyDescriptor<Window,bool>(nameof(MaximizeDisabled),w=>w.MaximizeDisabled,(w,v)=>w.MaximizeDisabled=v,_=>false,stored:true),
        new PropertyDescriptor<Window,bool>(nameof(Transparent),w=>w.Transparent,(w,v)=>w.Transparent=v,_=>false,stored:true),
    ];
}
