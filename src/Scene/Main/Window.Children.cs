namespace Electron2D;

public partial class Window
{
    private bool _forceNative;
    private bool _transientToFocused, _unparentWhenInvisible;
    private static Window? _focusedWindow;

    /// <summary>Gets or sets whether native activation chooses the currently focused window as transient owner.</summary>
    /// <value>False initially.</value>
    /// <remarks>Requires Transient. The choice is made when showing; changing this stored policy does not relink
    /// an already visible window. An absent focused host falls back to scene ancestry. Embedded windows use scene ancestry.</remarks>
    /// <exception cref="InvalidOperationException">Mutation violates scene ownership or capture rules.</exception>
    /// <exception cref="ObjectDisposedException">The window is disposed.</exception>
    public bool TransientToFocused
    {
        get { ThrowIfDisposed(); return _transientToFocused; }
        set { EnsureMutable(); _transientToFocused = value; }
    }

    /// <summary>Returns the scene window that most recently received keyboard focus.</summary>
    /// <returns>The borrowed live native or embedded window, or null while no scene window has focus.</returns>
    /// <remarks>Focus events commit this identity before notifications and signals. Hiding, detachment and disposal retire it.</remarks>
    /// <exception cref="InvalidOperationException">The caller is not the active scene owner.</exception>
    public static Window? GetFocusedWindow()
    {
        if (Engine.MainLoop is SceneTree tree) tree.EnsureOwnerThread();
        _focusedWindow?.Tree?.EnsureOwnerThread();
        return _focusedWindow is { IsDisposed: false, Visible: true, IsInsideTree: true } window ? window : null;
    }

    /// <summary>Configures automatic scene detachment when this window becomes hidden.</summary>
    /// <param name="unparent">True to remove the window from its parent on a later visibility notification.</param>
    /// <remarks>False initially. Detachment preserves the object; keep a reference and dispose it explicitly or reattach it.
    /// The runtime-only request is not stored in scene archives.</remarks>
    /// <exception cref="InvalidOperationException">Mutation violates scene ownership or capture rules.</exception>
    /// <exception cref="ObjectDisposedException">The window is disposed.</exception>
    public void SetUnparentWhenInvisible(bool unparent) { EnsureMutable(); _unparentWhenInvisible = unparent; }

    /// <summary>Gets or sets whether this child uses a native window despite a containing embedding viewport.</summary>
    /// <value>False initially.</value>
    /// <remarks>Change the policy while hidden. The root keeps its application-owned native host.
    /// Independent child surfaces require the active desktop scene host.</remarks>
    /// <exception cref="InvalidOperationException">A displayed child changes host policy, or mutation violates owner or submission rules.</exception>
    /// <exception cref="ObjectDisposedException">The window is disposed.</exception>
    public bool ForceNative
    {
        get { ThrowIfDisposed(); return _forceNative; }
        set
        {
            EnsureMutable();
            if (_forceNative == value || _display?.WindowID == DisplayServer.MainWindowId) return;
            if (_visible && IsInsideTree) throw new InvalidOperationException("Hide the window before changing its native host policy.");
            RenderingOwner?.EnsureNativeWindowMutation();
            _forceNative = value;
            if (IsInsideTree) { UpdateEmbeddedMembership(false); UpdateEmbeddedMembership(true); }
        }
    }

    private void OpenNativeChild()
    {
        if (_display is not null || Parent is null || !IsInsideTree) return;
        var renderer = RenderingServer.Service ?? throw new InvalidOperationException("Independent child windows require an active native scene host.");
        renderer.EnsureNativeWindowMutation();
        if (Tree?.Root is not Window { NativeDisplay: { } root })
            throw new InvalidOperationException("Independent child windows require an active native scene root.");
        if (_transient && _transientToFocused)
        {
            var owner = root.FocusedSceneWindow ?? Parent.GetWindow();
            for (var current = owner; current is not null; current = current._transientParent)
                if (ReferenceEquals(current, this)) throw new InvalidOperationException("Transient window ownership cannot form a cycle.");
            if (_transientParent?.ExclusiveChild == this) _transientParent.ExclusiveChild = null;
            _transientParent = owner;
        }
        var popupParent = PopupWMHint ? PopupParentDisplay() : null;
        var popupOffset = popupParent is null ? Vector2i.Zero : InitialNativePopupOffset(popupParent);
        _display = root.CreateChildWindow(_title, _size, this, popupParent, popupOffset);
        try
        {
            ConfigureNative();
            renderer.RegisterNativeWindow(this, _display);
            _renderer = renderer;
            UpdateNativeTransient();
            _display.SetWindowVisible(true);
            Tree?.RegisterNativePopup(this);
            root.RefreshNativeTransients();
        }
        catch (Exception error)
        {
            try { CloseNative(); }
            catch (Exception cleanup) { throw new AggregateException(error, cleanup); }
            throw;
        }
    }

    internal void UpdateNativeTransient()
    {
        if (_display is not { WindowID: > 0 } display) return;
        _renderer?.EnsureNativeWindowMutation();
        DisplayServer? parent = null;
        for (var window = _transientParent; window is not null; window = window.Parent?.GetWindow())
            if (window._display is { } native) { parent = native; break; }
        display.SetTransientParent(parent, _exclusive && _visible);
    }
}
