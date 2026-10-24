using SDL3;

namespace Electron2D;

public sealed partial class DisplayServer
{
    private readonly DisplayServer? _rootDisplay;
    private readonly Dictionary<int, DisplayServer> _childWindows = [];
    private readonly Dictionary<uint, DisplayServer> _nativeChildren = [];
    private int _nextWindowID;
    private DisplayServer? _pendingInputWindow, _eventInputWindow, _compositionWindow, _pressedInputWindow;
    private bool? _applicationFocused;
    private DisplayServer? _transientDisplay;
    private bool _nativeModal;
    private bool _transientExclusive;
    internal int WindowID { get; }
    internal Window? SceneWindow;
    internal bool HasEventContext => (_rootDisplay ?? this)._eventInputWindow is not null;
    internal Window? EventWindow => (_rootDisplay ?? this)._eventInputWindow?.SceneWindow;

    internal void SetTransientParent(DisplayServer? parent, bool modal)
    {
        EnsureOwner();
        if (parent is not null)
        {
            parent.EnsureOwner();
            if ((SDL.GetWindowFlags(_window.DangerousGetHandle()) & SDL.WindowFlags.Fullscreen) != 0)
                throw new InvalidOperationException("Leave fullscreen before assigning a transient parent.");
            if (!ReferenceEquals(_rootDisplay ?? this, parent._rootDisplay ?? parent)) throw new ArgumentException("Native parents must belong to the same display session.", nameof(parent));
            for (var ancestor = parent; ancestor is not null; ancestor = ancestor.NativePopupParent ?? ancestor._transientDisplay)
                if (ReferenceEquals(ancestor, this)) throw new ArgumentException("Native transient parents cannot form a cycle.", nameof(parent));
        }
        var exclusive = modal;
        modal &= parent is not null;
        if (ReferenceEquals(parent, _transientDisplay) && modal == _nativeModal) { _transientExclusive = exclusive; return; }
        var previous = _transientDisplay; var previousModal = _nativeModal;
        try { ApplyTransientParent(parent, modal); _transientExclusive = exclusive; }
        catch (Exception error)
        {
            try { ApplyTransientParent(previous, previousModal); }
            catch (Exception rollback) { throw new AggregateException(error, rollback); }
            throw;
        }
    }

    private void ApplyTransientParent(DisplayServer? parent, bool modal)
    {
        if (NativePopupParent is not null)
        {
            if (parent is not null && !ReferenceEquals(parent, NativePopupParent)) throw new InvalidOperationException("Hide a native popup before changing its parent.");
            _transientDisplay = parent;
            return;
        }
        var window = _window.DangerousGetHandle();
        if (_nativeModal && (!modal || !ReferenceEquals(parent, _transientDisplay)))
        {
            if (!SDL.SetWindowModal(window, false)) throw SDLFailure("clear window modality");
            _nativeModal = false;
        }
        if (!ReferenceEquals(parent, _transientDisplay))
        {
            if (!SDL.SetWindowParent(window, parent?._window.DangerousGetHandle() ?? 0)) throw SDLFailure("set window transient parent");
            _transientDisplay = parent;
        }
        if (modal && !_nativeModal)
        {
            if (!SDL.SetWindowModal(window, true)) throw SDLFailure("set window modality");
            _nativeModal = true;
        }
    }

    private DisplayServer? NativeContext(nint native)
    {
        var root = _rootDisplay ?? this;
        if (native == 0) return null;
        return native == root._window.DangerousGetHandle() ? root : root._nativeChildren.GetValueOrDefault(SDL.GetWindowID(native));
    }

    private DisplayServer? FocusedWindow() => NativeContext(SDL.GetKeyboardFocus());
    internal Window? FocusedSceneWindow { get { EnsureOwner(); return FocusedWindow()?.SceneWindow; } }

    internal void RefreshNativeTransients()
    {
        foreach (var context in (_rootDisplay ?? this)._childWindows.Values) context.SceneWindow?.UpdateNativeTransient();
    }

    internal Exception? PrepareNativeWindowRetirement()
    {
        var failure = RetireNativePopupChildren(keepRetiring: true);
        try
        {
            foreach (var child in (_rootDisplay ?? this)._childWindows.Values)
                if (child.NativePopupParent is null && ReferenceEquals(child._transientDisplay, this)) child.SetTransientParent(null, false);
            var root = _rootDisplay ?? this;
            if (ReferenceEquals(root._transientDisplay, this)) root.SetTransientParent(null, false);
        }
        catch (Exception error)
        {
            _retiringNativeWindow = false;
            if (failure is not null) throw new AggregateException(failure, error);
            throw;
        }
        return failure;
    }

    internal Exception? RetireNativePopupChildren(bool keepRetiring = false)
    {
        EnsureOwner();
        if (_retiringNativeWindow) throw new InvalidOperationException("Native window retirement cannot reenter callbacks.");
        SceneWindow?.RenderingOwner?.EnsureNativeWindowMutation();
        ValidateNativeWindowRetirement();
        var root = _rootDisplay ?? this;
        foreach (var child in root._childWindows.Values)
            if (child.IsPopupDescendantOf(this))
            {
                child.ValidateNativeWindowRetirement();
                if (child.SceneWindow is null && child._renderingAttached) throw new InvalidOperationException("Release descendant popup rendering owners before retiring their parent.");
            }
        _retiringNativeWindow = true;
        try
        {
            List<Exception>? errors = null;
            foreach (var child in root._childWindows.Values.ToArray())
                if (ReferenceEquals(child.NativePopupParent, this))
                    try { child.ForceClosePopup(); }
                    catch (Exception error) { (errors ??= []).Add(error); }
            if (errors is not null)
            {
                var failure = new AggregateException("Native popup retirement callbacks failed.", errors);
                if (root._childWindows.Values.Any(child => child.IsPopupDescendantOf(this))) throw failure;
                if (!keepRetiring) _retiringNativeWindow = false;
                return failure;
            }
        }
        catch { _retiringNativeWindow = false; throw; }
        if (!keepRetiring) _retiringNativeWindow = false;
        return null;
    }

    private void ClearWindowComposition()
    {
        _imeText = string.Empty; _imeSelection = Vector2i.Zero;
        var root = _rootDisplay ?? this;
        if (ReferenceEquals(root._compositionWindow, this)) root._compositionWindow = null;
    }

    private void ReleaseWindowTouches()
    {
        var root = _rootDisplay ?? this;
        foreach (var contact in _touchContacts.Values) root._freeTouchIndexes.Push(contact.Index);
        _touchContacts.Clear();
    }

    internal Window? FindSceneWindow(long windowID)
    {
        EnsureOwner();
        if (windowID < 0 || windowID > int.MaxValue) return null;
        var root = _rootDisplay ?? this;
        return windowID == MainWindowId ? root.SceneWindow : root._childWindows.GetValueOrDefault((int)windowID)?.SceneWindow;
    }

    private DisplayServer? EventContext(SDL.Event value)
    {
        var type = (SDL.EventType)value.Type;
        if (type is SDL.EventType.Quit or SDL.EventType.SystemThemeChanged ||
            type is >= SDL.EventType.JoystickAxisMotion and <= SDL.EventType.GamepadSteamHandleUpdated) return this;
        var id = type is SDL.EventType.FingerDown or SDL.EventType.FingerUp or SDL.EventType.FingerCanceled or SDL.EventType.FingerMotion
            ? value.TFinger.WindowID : value.Window.WindowID;
        return id == _sdlWindowId ? this : _nativeChildren.GetValueOrDefault(id);
    }

    private DisplayServer ResolveWindow(int windowID)
    {
        EnsureOwner();
        var root = _rootDisplay ?? this;
        if (windowID == MainWindowId) return root;
        if (root._childWindows.TryGetValue(windowID, out var child) && !child.IsDisposed) return child;
        throw new ArgumentOutOfRangeException(nameof(windowID), windowID, "The native window is absent or retired.");
    }

    internal DisplayServer CreateChildWindow(string title, Vector2i size, Window? sceneWindow = null, DisplayServer? popupParent = null, Vector2i popupOffset = default)
    {
        EnsureOwner();
        var root = _rootDisplay ?? this;
        if (OperatingSystem.IsAndroid() || OperatingSystem.IsIOS() || OperatingSystem.IsTvOS() || OperatingSystem.IsBrowser())
            throw new NotSupportedException("This application host supplies one native surface; use embedded windows.");
        ArgumentNullException.ThrowIfNull(title);
        if (title.Contains('\0')) throw new ArgumentException("Window titles cannot contain null characters.", nameof(title));
        if (size.X <= 0 || size.Y <= 0) throw new ArgumentOutOfRangeException(nameof(size));
        var id = checked(root._nextWindowID + 1);
        var flags = SDL.WindowFlags.Hidden | SDL.WindowFlags.Resizable | SDL.WindowFlags.HighPixelDensity;
        if (sceneWindow?.Transparent == true) flags |= SDL.WindowFlags.Transparent;
        nint handle;
        if (popupParent is null) handle = SDL.CreateWindow(title, size.X, size.Y, flags);
        else
        {
            popupParent.EnsureOwner();
            if (popupParent._retiringNativeWindow) throw new InvalidOperationException("The popup parent is retiring.");
            var offset = SDL.GetCurrentVideoDriver() == "wayland" ? WaylandLogicalWindowSize(popupOffset, popupParent._window.DangerousGetHandle()) : popupOffset;
            flags = (flags & ~SDL.WindowFlags.Resizable) | SDL.WindowFlags.PopupMenu;
            if (sceneWindow?.Unfocusable == true) flags |= SDL.WindowFlags.NotFocusable;
            handle = SDL.CreatePopupWindow(popupParent._window.DangerousGetHandle(), offset.X, offset.Y, size.X, size.Y, flags);
        }
        if (handle == 0) throw SDLFailure("create a child window");
        DisplayServer? child = null;
        try
        {
            child = new(handle, 0, 0, null, root, id);
            child.NativePopupParent = popupParent;
            child._popupTitle = title;
            child.SceneWindow = sceneWindow;
            child.PopupInputPolicy = popupParent is not null && (sceneWindow?.PopupWindow ?? true);
            child.ApplyDefaultWindowIcon();
            child._waylandMinimumSize = Vector2i.Zero;
            child._requestedVSync = WindowGetVSyncMode();
            child._vSyncConfigured = true;
            root._childWindows.Add(id, child);
            try { root._nativeChildren.Add(child._sdlWindowId, child); }
            catch { root._childWindows.Remove(id); throw; }
            root._nextWindowID = id;
            return child;
        }
        catch
        {
            if (child is not null) child.Dispose(); else SDL.DestroyWindow(handle);
            throw;
        }
    }
}
