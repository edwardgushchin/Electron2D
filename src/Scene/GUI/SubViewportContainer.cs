namespace Electron2D;

/// <summary>Displays direct offscreen viewports and forwards their scene and GUI input.</summary>
/// <remarks>Child viewports draw in sibling order at the local origin. Stretch owns their native sizes;
/// StretchShrink reduces resolution while preserving the displayed rectangle. Connected viewports share
/// handled input and drag state, but retain independent focus, hover and pointer capture.</remarks>
public class SubViewportContainer : Container
{
    private bool _stretch, _mouseTarget;
    private int _shrink = 1;
    /// <summary>Creates a click-focus container with nonpositional unhandled-input forwarding enabled.</summary>
    public SubViewportContainer()
    {
        FocusMode = FocusMode.Click; UnhandledInputEnabled = true;
        ChildAdded += ChildChanged; ChildRemoved += ChildChanged;
    }
    /// <summary>Gets or sets whether this control owns child viewport sizes and fills its rectangle.</summary>
    /// <value>False initially.</value>
    /// <remarks>Equal assignments are silent. Changed assignments commit before resizing children and requesting layout/redraw.</remarks>
    /// <exception cref="InvalidOperationException">Mutation is off-owner or during scene capture/native submission.</exception>
    /// <exception cref="ObjectDisposedException">The container is disposed.</exception>
    public bool Stretch { get { ThrowIfDisposed(); Tree?.EnsureOwnerThread(); return _stretch; } set { EnsureMutable(); if (_stretch == value) return; _stretch = value; RefreshChildren(); } }
    /// <summary>Gets or sets the positive integer divisor of stretched child resolution.</summary>
    /// <value>One initially. Has no sizing effect while Stretch is false.</value>
    /// <exception cref="ArgumentOutOfRangeException">The divisor is less than one.</exception>
    /// <exception cref="InvalidOperationException">Mutation is off-owner or capture-owned.</exception>
    /// <exception cref="ObjectDisposedException">The container is disposed.</exception>
    public int StretchShrink { get { ThrowIfDisposed(); Tree?.EnsureOwnerThread(); return _shrink; } set { EnsureMutable(); if (value < 1) throw new ArgumentOutOfRangeException(nameof(value)); if (_shrink == value) return; _shrink = value; RecalculateSizes(); QueueRedraw(); } }
    /// <summary>Gets or sets whether mouse cursor and drag targeting select this container instead of embedded controls.</summary>
    /// <value>False initially. Input forwarding and child hover remain active in either mode.</value>
    /// <exception cref="InvalidOperationException">Mutation is off-owner or capture-owned.</exception>
    /// <exception cref="ObjectDisposedException">The container is disposed.</exception>
    public bool MouseTarget { get { ThrowIfDisposed(); Tree?.EnsureOwnerThread(); return _mouseTarget; } set { EnsureMutable(); _mouseTarget = value; Tree?.RefreshGUICursor(this); } }
    /// <inheritdoc />
    protected override Vector2 OnGetMinimumSize()
    {
        ThrowIfDisposed(); Tree?.EnsureOwnerThread(); var minimum = Vector2.Zero;
        if (!Stretch) for (var i = 0; i < GetChildCount(); i++) if (GetChild(i) is SubViewport viewport) minimum = minimum.Max(viewport.Size);
        return minimum;
    }
    /// <summary>Decides whether a borrowed event is forwarded to live direct child viewports.</summary>
    /// <param name="inputEvent">The original container-local positional event or unchanged nonpositional event.</param>
    /// <returns>True by default; false suppresses forwarding without accepting the parent event.</returns>
    /// <remarks>Called once before coordinate scaling. Overrides must not dispose the borrowed event.</remarks>
    protected virtual bool PropagateInputEvent(InputEvent inputEvent) => true;
    internal Vector2 ViewportPoint(Vector2 local) => Stretch ? local / StretchShrink : local;
    internal Transform ViewportScale => Stretch ? Transform.Identity.Scaled(new(StretchShrink, StretchShrink)) : Transform.Identity;
    private static bool Positional(InputEvent input) => input is InputEventMouse or InputEventScreenTouch or InputEventScreenDrag or InputEventGesture;
    private void Forward(InputEvent input)
    {
        if (!PropagateInputEvent(input) || IsDisposed || !IsInsideTree) return;
        InputEvent? scaled = null;
        try
        {
            var sent = input;
            if (Positional(input) && Stretch && StretchShrink > 1) sent = scaled = input.XformedBy(Transform.Identity.Scaled(new(1f / StretchShrink, 1f / StretchShrink)));
            List<Exception>? errors = null;
            for (var i = 0; i < GetChildCount(); i++)
                if (GetChild(i) is SubViewport { GUIDisableInput: false } viewport)
                    try { Tree!.DispatchEmbeddedViewportInput(this, viewport, sent); } catch (Exception error) { CollectException(ref errors, error); }
            ThrowCollected("Embedded viewport input callbacks failed.", errors);
        }
        finally { scaled?.Dispose(); }
    }
    /// <inheritdoc />
    protected override void OnGUIInput(InputEvent inputEvent) { base.OnGUIInput(inputEvent); if (Positional(inputEvent)) Forward(inputEvent); }
    /// <inheritdoc />
    protected override void OnInput(InputEvent inputEvent) { base.OnInput(inputEvent); if (!Positional(inputEvent)) Forward(inputEvent); }
    /// <inheritdoc />
    protected override void OnUnhandledInput(InputEvent inputEvent) { base.OnUnhandledInput(inputEvent); if (!Positional(inputEvent)) Forward(inputEvent); }
    /// <inheritdoc />
    protected override void OnDraw()
    {
        base.OnDraw();
        for (var i = 0; i < GetChildCount(); i++) if (GetChild(i) is SubViewport viewport) DrawTextureRect(viewport.GetTexture(), new(Vector2.Zero, Stretch ? Size : viewport.Size), false);
    }
    private void ChildChanged(Node _, Node child) { if (child is SubViewport && !IsDisposed) RefreshChildren(); }
    internal void RefreshChildren()
    {
        List<Exception>? errors = null;
        try { RecalculateSizes(); } catch (Exception error) { CollectException(ref errors, error); }
        for (var i = 0; i < GetChildCount(); i++) if (GetChild(i) is SubViewport viewport)
                try { viewport.HandleInputLocally = false; if (IsInsideTree) viewport.RenderTargetUpdateMode = IsVisibleInTree ? ViewportUpdateMode.Always : ViewportUpdateMode.Disabled; } catch (Exception error) { CollectException(ref errors, error); }
        try { UpdateMinimumSize(); } catch (Exception error) { CollectException(ref errors, error); }
        QueueSort(); QueueRedraw(); ThrowCollected("Embedded viewport configuration callbacks failed.", errors);
    }
    private void RecalculateSizes()
    {
        if (!Stretch) return;
        List<Exception>? errors = null;
        var resolution = new Vector2i((int)(Size.X / StretchShrink), (int)(Size.Y / StretchShrink));
        for (var i = 0; i < GetChildCount(); i++) if (GetChild(i) is SubViewport viewport)
                try { viewport.SetContainerSize(resolution); } catch (Exception error) { CollectException(ref errors, error); }
        ThrowCollected("Embedded viewport resizing callbacks failed.", errors);
    }
    /// <inheritdoc />
    protected override void OnNotification(int what)
    {
        base.OnNotification(what);
        if (what == NotificationResized) RecalculateSizes();
        else if (what is NotificationEnterTree or NotificationVisibilityChanged) RefreshChildren();
        else if (what == NotificationFocusEnter) { InputEnabled = true; UnhandledInputEnabled = false; }
        else if (what == NotificationFocusExit) { InputEnabled = false; UnhandledInputEnabled = true; }
    }
    /// <inheritdoc />
    protected override SizeFlags[] GetAllowedSizeFlagsHorizontal() => [];
    /// <inheritdoc />
    protected override SizeFlags[] GetAllowedSizeFlagsVertical() => [];
    /// <inheritdoc />
    protected override CursorShape OnGetCursorShape(Vector2 atPosition) { ThrowIfDisposed(); return CursorShape.Arrow; }
    /// <inheritdoc />
    public override string[] GetConfigurationWarnings()
    {
        var warnings = new List<string>(base.GetConfigurationWarnings()); var hasViewport = false;
        for (var i = 0; i < GetChildCount(); i++) if (GetChild(i) is SubViewport) { hasViewport = true; break; }
        if (!hasViewport) warnings.Add("This container requires a direct SubViewport child to display content.");
        if (MouseDefaultCursorShape != CursorShape.Arrow) warnings.Add("The container's default cursor shape has no effect; embedded controls supply it.");
        return warnings.ToArray();
    }
    private static readonly PropertyDescriptor[] Properties =
    [new PropertyDescriptor<SubViewportContainer, bool>(nameof(Stretch), n => n.Stretch, (n,v) => n.Stretch=v, _ => false, stored:true),
     new PropertyDescriptor<SubViewportContainer, int>(nameof(StretchShrink), n => n.StretchShrink, (n,v) => n.StretchShrink=v, _ => 1, (_,v) => v>=1, stored:true),
     new PropertyDescriptor<SubViewportContainer, bool>(nameof(MouseTarget), n => n.MouseTarget, (n,v) => n.MouseTarget=v, _ => false, stored:true),
     new PropertyDescriptor<SubViewportContainer, FocusMode>(nameof(FocusMode), n => n.FocusMode, (n,v) => n.FocusMode=v, _ => FocusMode.Click, stored:true)];
    /// <inheritdoc />
    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors() => base.GetPropertyDescriptors().Where(p => p.Name != nameof(FocusMode)).Concat(Properties);
    /// <inheritdoc />
    protected override Func<Node> CreateSceneInstanceFactory() => GetType() == typeof(SubViewportContainer) ? CreateContainer : base.CreateSceneInstanceFactory();
    private static Node CreateContainer() => new SubViewportContainer();
}
