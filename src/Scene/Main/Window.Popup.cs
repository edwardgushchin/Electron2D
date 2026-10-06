namespace Electron2D;

public partial class Window
{
    internal Viewport? Embedder;
    private bool _transient, _exclusive, _wrapControls;
    private LayoutDirection _layoutDirection;
    /// <summary>Gets or sets the direction inherited by this window's controls.</summary>
    /// <value>Inherited initially; root windows resolve the application locale.</value>
    /// <exception cref="ArgumentOutOfRangeException">The direction is undefined or Max.</exception>
    public LayoutDirection LayoutDirection
    { get { ThrowIfDisposed(); return _layoutDirection; } set { EnsureMutable(); if (value is < LayoutDirection.Inherited or >= LayoutDirection.Max) throw new ArgumentOutOfRangeException(nameof(value)); if (_layoutDirection == value) return; _layoutDirection = value; PropagateNotification(Control.NotificationLayoutDirectionChanged); QueueEmbeddedRedraw(); } }
    /// <summary>Reports whether the resolved window layout direction is right to left.</summary>
    /// <returns>The explicit, inherited or translated locale direction.</returns>
    public bool IsLayoutRTL()
    {
        ThrowIfDisposed(); Tree?.EnsureOwnerThread();
        if (_layoutDirection == LayoutDirection.LTR) return false; if (_layoutDirection == LayoutDirection.RTL) return true;
        if (_layoutDirection == LayoutDirection.Inherited) for (var parent = Parent; parent is not null; parent = parent.Parent) { if (parent.TranslationDomain != TranslationDomain) break; if (parent is Control control) return control.IsLayoutRTL(); if (parent is Window window) return window.IsLayoutRTL(); }
        var domain = TranslationServer.GetOrAddDomain(TranslationDomain); var culture = _layoutDirection == LayoutDirection.SystemLocale ? System.Globalization.CultureInfo.CurrentUICulture : domain.EffectiveCulture;
        return culture.TextInfo.IsRightToLeft && (domain.HasTranslationForCulture(culture, false) || TranslationServer.FallbackCulture?.TwoLetterISOLanguageName == culture.TwoLetterISOLanguageName);
    }
    private CanvasLayer? _embeddedLayer;
    private int _embeddedSerial;
    private EmbeddedWindowCanvas? _embeddedCanvas;
    /// <summary>Gets or sets whether this window returns focus to its parent when hidden.</summary>
    /// <value>False initially; Popup defaults to true.</value>
    public bool Transient { get { ThrowIfDisposed(); return _transient; } set { EnsureMutable(); _transient = value; } }
    /// <summary>Gets or sets whether input outside this window is blocked while visible.</summary>
    /// <value>False initially.</value>
    public bool Exclusive { get { ThrowIfDisposed(); return _exclusive; } set { EnsureMutable(); _exclusive = value; } }
    /// <summary>Gets or sets whether child control minimums constrain this window's size.</summary>
    /// <value>False initially; Popup defaults to true.</value>
    public bool WrapControls { get { ThrowIfDisposed(); return _wrapControls; } set { EnsureMutable(); _wrapControls = value; UpdateEmbeddedContents(); } }
    /// <summary>Gets or sets the popup input and outside-click close-request policy.</summary>
    /// <value>False initially; Popup defaults to true.</value>
    /// <exception cref="InvalidOperationException">The policy changes while visible and attached.</exception>
    public bool PopupWindow { get => GetFlag(WindowFlag.Popup); set => SetFlag(WindowFlag.Popup, value); }
    /// <summary>Gets or sets the popup window-manager hint, retained by the embedded host.</summary>
    /// <value>False initially; Popup defaults to true. Native child windows remain unavailable.</value>
    public bool PopupWMHint { get => GetFlag(WindowFlag.PopupWmHint); set => SetFlag(WindowFlag.PopupWmHint, value); }
    /// <summary>Gets or sets whether user maximization is disabled.</summary>
    /// <value>False initially; Popup defaults to true.</value>
    public bool MaximizeDisabled { get => GetFlag(WindowFlag.MaximizeDisabled); set => SetFlag(WindowFlag.MaximizeDisabled, value); }
    /// <summary>Gets or sets whether user minimization is disabled.</summary>
    /// <value>False initially; Popup defaults to true.</value>
    public bool MinimizeDisabled { get => GetFlag(WindowFlag.MinimizeDisabled); set => SetFlag(WindowFlag.MinimizeDisabled, value); }
    /// <summary>Gets or sets transparent embedded window composition.</summary>
    /// <value>False initially; PopupPanel defaults to true.</value>
    public bool Transparent { get => GetFlag(WindowFlag.Transparent); set => SetFlag(WindowFlag.Transparent, value); }
    /// <summary>Occurs before popup positioning, sizing and visibility changes.</summary>
    public event Action? AboutToPopup;
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
    /// <summary>Returns the embedded host's usable client rectangle.</summary>
    /// <returns>The containing viewport rectangle; native child-window geometry is unavailable.</returns>
    /// <exception cref="InvalidOperationException">The window is detached or is the root window.</exception>
    internal Rect2i GetUsableParentRect() { CheckPopup(); var rect = Embedder!.GetVisibleRect(); return new((Vector2i)rect.Position, (Vector2i)rect.Size); }
    /// <summary>Shows this child window, applying an optional host-coordinate rectangle.</summary>
    /// <param name="rect">An explicit position and size, or zero to retain configured geometry.</param>
    /// <remarks>Requires an embedding viewport. AboutToPopup precedes changes; popup subclasses fit their rectangle into the host.</remarks>
    public void Popup(Rect2i rect = default)
    {
        PopupAt(rect, false);
    }
    private void PopupAt(Rect2i rect, bool centered)
    {
        EnsureMutable(); if (TryNativePopup()) return; CheckPopup(); AboutToPopup?.Invoke(); if (IsDisposed || !IsInsideTree) return;
        if (rect != default) { Size = rect.Size; Position = rect.Position; }
        PreparePopup();
        UpdateEmbeddedContents(); if (centered) { var area = GetUsableParentRect(); Position = area.Position + (area.Size - Size) / 2; }
        AdjustPopup(); Transient = true; Show(); GrabFocus();
    }
    /// <summary>Shows this window at a rectangle relative to its parent window.</summary>
    /// <param name="parentRect">An embedded host-coordinate rectangle.</param>
    public void PopupOnParent(Rect2i parentRect) => Popup(parentRect);
    /// <summary>Shows this window centered in its embedding viewport.</summary>
    /// <param name="minSize">Requested dimensions, or zero to retain the current size.</param>
    public void PopupCentered(Vector2i minSize = default) { EnsureMutable(); if (TryNativePopup()) return; CheckPopup(); if (minSize == default) minSize = Size; var area = GetUsableParentRect(); PopupAt(new(area.Position + (area.Size - minSize) / 2, minSize), true); }
    /// <summary>Shows this window centered with dimensions proportional to its host.</summary>
    /// <param name="ratio">A finite ratio greater than zero and at most one.</param>
    public void PopupCenteredRatio(float ratio = .8f) { EnsureMutable(); CheckRatio(ratio); if (TryNativePopup()) return; CheckPopup(); PopupCentered((Vector2i)((Vector2)GetUsableParentRect().Size * ratio)); }
    /// <summary>Shows this window centered, limiting requested dimensions to a host-size ratio.</summary>
    /// <param name="minSize">Requested dimensions, or zero to retain current dimensions.</param>
    /// <param name="fallbackRatio">A finite ratio greater than zero and at most one.</param>
    public void PopupCenteredClamped(Vector2i minSize = default, float fallbackRatio = .75f) { EnsureMutable(); CheckRatio(fallbackRatio); if (TryNativePopup()) return; CheckPopup(); if (minSize == default) minSize = Size; var limit = (Vector2i)((Vector2)GetUsableParentRect().Size * fallbackRatio); PopupCentered(new(Math.Min(minSize.X, limit.X), Math.Min(minSize.Y, limit.Y))); }
    /// <summary>Parents a detached dialog to the last exclusive window and shows it with an optional host-coordinate rectangle.</summary>
    /// <param name="fromNode">An attached node identifying the owning window.</param>
    /// <param name="rect">The popup geometry argument passed to Popup.</param>
    public void PopupExclusive(Node fromNode, Rect2i rect = default) => ParentDialog(fromNode, () => Popup(rect));
    /// <summary>Parents a detached dialog to the last exclusive window and shows it with a parent-relative rectangle.</summary>
    /// <param name="fromNode">An attached node identifying the owning window.</param>
    /// <param name="parentRect">The popup geometry argument passed to PopupOnParent.</param>
    public void PopupExclusiveOnParent(Node fromNode, Rect2i parentRect) => ParentDialog(fromNode, () => PopupOnParent(parentRect));
    /// <summary>Parents a detached dialog to the last exclusive window and shows it with centered dimensions.</summary>
    /// <param name="fromNode">An attached node identifying the owning window.</param>
    /// <param name="minSize">The popup geometry argument passed to PopupCentered.</param>
    public void PopupExclusiveCentered(Node fromNode, Vector2i minSize = default) => ParentDialog(fromNode, () => PopupCentered(minSize));
    /// <summary>Parents a detached dialog to the last exclusive window and shows it with a proportional host size.</summary>
    /// <param name="fromNode">An attached node identifying the owning window.</param>
    /// <param name="ratio">The popup geometry argument passed to PopupCenteredRatio.</param>
    public void PopupExclusiveCenteredRatio(Node fromNode, float ratio = .8f) => ParentDialog(fromNode, () => PopupCenteredRatio(ratio));
    /// <summary>Parents a detached dialog to the last exclusive window and shows it with clamped centered dimensions.</summary>
    /// <param name="fromNode">An attached node identifying the owning window.</param>
    /// <param name="minSize">The popup geometry argument passed to PopupCenteredClamped.</param>
    /// <param name="fallbackRatio">The popup geometry argument passed to PopupCenteredClamped.</param>
    public void PopupExclusiveCenteredClamped(Node fromNode, Vector2i minSize = default, float fallbackRatio = .75f) => ParentDialog(fromNode, () => PopupCenteredClamped(minSize, fallbackRatio));
    private void ParentDialog(Node fromNode, Action show)
    {
        EnsureMutable(); ArgumentNullException.ThrowIfNull(fromNode); if (fromNode.IsDisposed) throw new ObjectDisposedException(nameof(fromNode));
        if (Parent is not null || IsInsideTree) throw new InvalidOperationException("Exclusive popup requires a detached dialog.");
        var parent = fromNode.GetWindow() ?? throw new InvalidOperationException("Dialog owner has no containing window.");
        if (parent.Tree is null) throw new InvalidOperationException("Dialog owner must be attached.");
        if (parent.Embedder is { } host) for (var i = host.EmbeddedWindows.Count - 1; i >= 0; i--) { var candidate = host.EmbeddedWindows[i]; if (candidate.Visible && candidate.Exclusive && parent.IsAncestorOf(candidate)) { parent = candidate; break; } }
        parent.AddChild(this);
        try { show(); } catch { if (!IsDisposed && Parent == parent) parent.RemoveChild(this); throw; }
    }
    private static void CheckRatio(float ratio) { if (!float.IsFinite(ratio) || ratio <= 0 || ratio > 1) throw new ArgumentOutOfRangeException(nameof(ratio)); }
    private void CheckPopup() { EnsureMutable(); if (!IsInsideTree || Parent is null) throw new InvalidOperationException("Popup requires an attached child window."); if (Embedder is null) throw new NotSupportedException("Enable GUIEmbedSubwindows on a containing viewport; independent native child windows are unavailable."); }
    internal virtual bool TryNativeVisibility(bool visible) => false;
    internal virtual bool TryNativePopup() => false;
    internal virtual void AfterVisibilityChanged(bool visible) { }
    internal virtual bool AcceptEmbeddedPointer(Vector2 point, InputEvent input) => true;
    internal virtual void PreparePopup() { }
    internal virtual void AdjustPopup() { }
    internal void UpdateEmbeddedContents()
    {
        if (WrapControls || _keepTitleVisible) { var min = WrapControls ? GetContentsMinimumSize().Ceil() : Vector2.Zero; var size = new Vector2i(Math.Max(_size.X, checked((int)min.X)), Math.Max(_size.Y, checked((int)min.Y))); size = size.Max(MinSize).Max(TitleMinimum()); if (MaxSize.X > 0) size.X = Math.Min(size.X, MaxSize.X); if (MaxSize.Y > 0) size.Y = Math.Min(size.Y, MaxSize.Y); if (size != _size) CommitSize(size); }
        if (ClampToEmbedder && Embedder != null) Position = Position;
        _embeddedCanvas?.QueueRedraw();
    }
    internal void EmbeddedVisibilityChanged()
    {
        if (Embedder is null) { if (Visible && IsInsideTree && Parent is not null) throw new NotSupportedException("Native child windows are unavailable."); return; }
        if (_embeddedCanvas is not null) { _embeddedCanvas.Visible = Visible; _embeddedCanvas.QueueRedraw(); }
        if (Visible) { Embedder.EmbeddedWindows.Remove(this); Embedder.EmbeddedWindows.Add(this); if (_embeddedLayer?.Parent is { } p) p.MoveChild(_embeddedLayer, -1); Tree?.FocusEmbeddedWindow(this); }
        else Tree?.HideEmbeddedWindow(this);
    }
    internal void QueueEmbeddedRedraw() => _embeddedCanvas?.QueueRedraw();
    internal void RaiseEmbeddedCanvas() { if (_embeddedLayer?.Parent is { } parent) parent.MoveChild(_embeddedLayer, -1); }
    internal Rect2 EmbeddedHitRect => Borderless ? new(Position, Size) : new((Vector2)Position - new Vector2(0, GetThemeConstant("title_height", "Window")), (Vector2)Size + new Vector2(0, GetThemeConstant("title_height", "Window")));
    internal void RequestEmbeddedClose() => CloseRequested?.Invoke();
    internal void EmitEmbeddedFocus(bool focus) { if (focus) FocusEntered?.Invoke(); else FocusExited?.Invoke(); }
    private void UpdateEmbeddedMembership(bool entering)
    {
        if (!entering)
        {
            List<Exception>? errors = null;
            try { if (Embedder is not null) Tree?.HideEmbeddedWindow(this); } catch (Exception error) { CollectException(ref errors, error); }
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
        for (var parent = Parent; parent is not null; parent = parent.Parent) if (parent is Viewport { GUIEmbedSubwindows: true } viewport) { Embedder = viewport; break; }
        if (Embedder is null) { if (Visible) throw new NotSupportedException("Native child windows are unavailable; configure an embedding viewport."); return; }
        Embedder.EmbeddedWindows.Add(this);
        _embeddedLayer = new CanvasLayer { Name = "_embedded_window_" + InstanceID + "_" + (++_embeddedSerial), Layer = int.MaxValue };
        _embeddedCanvas = new(this) { Name = "_window_canvas", MouseFilter = MouseFilter.Ignore, Visible = Visible };
        _embeddedLayer.AddChild(_embeddedCanvas, InternalMode.Back); Embedder.AddChild(_embeddedLayer, InternalMode.Back);
        EmbeddedVisibilityChanged();
    }
    private sealed class EmbeddedWindowCanvas(Window window) : Control
    {
        protected override void OnDraw()
        {
            base.OnDraw(); if (window.IsDisposed || !window.Visible) return;
            var rect = new Rect2(window.Position, window.Size);
            if (!window.Borderless)
            {
                var titleHeight = window.GetThemeConstant("title_height", "Window");
                var title = new Rect2(rect.Position - new Vector2(0, titleHeight), new Vector2(rect.Size.X, titleHeight));
                DrawStyleBox(window.GetThemeStyleBox("embedded_border", "Window")!, new(title.Position, new Vector2(rect.Size.X, titleHeight + rect.Size.Y)));
                if (window.GetThemeFont("title_font", "Window") is { } font) DrawString(font, title.Position + new Vector2(8, titleHeight - 10), window.Title, width: MathF.Max(0, rect.Size.X - 40), fontSize: window.GetThemeFontSize("title_font_size", "Window"), modulate: window.GetThemeColor("title_color", "Window"));
                var close = title.Position + new Vector2(title.Size.X - 18, 18); DrawLine(close - new Vector2(4, 4), close + new Vector2(4, 4), Colors.White, 2); DrawLine(close + new Vector2(-4, 4), close + new Vector2(4, -4), Colors.White, 2);
            }
            if (!window.Transparent) DrawRect(rect, RenderingServer.GetDefaultClearColor() with { A = 1 });
            DrawTextureRect(window.GetTexture(), rect, false);
        }
    }
    private static readonly PropertyDescriptor[] PopupProperties =
    [
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
