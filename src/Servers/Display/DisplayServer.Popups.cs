using SDL3;

namespace Electron2D;

public sealed partial class DisplayServer
{
    private string _popupTitle = string.Empty;
    internal DisplayServer? NativePopupParent { get; private set; }
    private bool _retiringNativeWindow;
    private Rect2i _popupSafeRect;
    private bool _popupSafeRectIsLocal;
    internal bool IsNativePopup => NativePopupParent is not null;
    internal bool PopupInputPolicy { get; private set; }

    private void SetPopupInputPolicy(bool enabled)
    {
        PopupInputPolicy = enabled;
        if (SceneWindow is { Tree: { } tree } window) { if (enabled) tree.RegisterNativePopup(window); else tree.UnregisterNativePopup(window); }
    }

    private bool IsPopupDescendantOf(DisplayServer parent)
    {
        for (var current = NativePopupParent; current is not null; current = current.NativePopupParent)
            if (ReferenceEquals(current, parent)) return true;
        return false;
    }

    internal Vector2i NativePopupOffset
    {
        get
        {
            EnsureOwner();
            var parent = NativePopupParent ?? throw new InvalidOperationException("The native window is not a popup.");
            if (!SDL.GetWindowPosition(_window.DangerousGetHandle(), out var x, out var y)) throw SDLFailure("read popup offset");
            if (SDL.GetCurrentVideoDriver() != "wayland") return new(x, y);
            var scale = WaylandWindowPixelDensity(parent._window.DangerousGetHandle());
            return new(checked((int)Math.Round(x * (double)scale, MidpointRounding.AwayFromZero)), checked((int)Math.Round(y * (double)scale, MidpointRounding.AwayFromZero)));
        }
        set
        {
            EnsureOwner();
            var parent = NativePopupParent ?? throw new InvalidOperationException("The native window is not a popup.");
            var offset = SDL.GetCurrentVideoDriver() == "wayland" ? WaylandLogicalWindowSize(value, parent._window.DangerousGetHandle()) : value;
            if (!SDL.SetWindowPosition(_window.DangerousGetHandle(), offset.X, offset.Y)) throw SDLFailure("position popup");
        }
    }

    internal Vector2i PopupClientOrigin => NativePopupParent is { } parent ? parent.PopupClientOrigin + NativePopupOffset : Vector2i.Zero;

    internal DisplayServer NativePopupRoot
    {
        get { var parent = this; while (parent.NativePopupParent is { } next) parent = next; return parent; }
    }

    internal int WindowGetActivePopupCore()
    {
        EnsureOwner();
        return (_rootDisplay ?? this).SceneWindow?.Tree?.ActiveNativePopup?.GetWindowID() ?? InvalidWindowId;
    }

    internal Rect2i WindowGetPopupSafeRectCore(int windowID)
    {
        var window = ResolveWindow(windowID);
        if (!window._popupSafeRectIsLocal || !window._popupSafeRect.HasArea()) return window._popupSafeRect;
        return new(window._popupSafeRect.Position + window.NativePopupRoot.WindowGetPositionCore(), window._popupSafeRect.Size);
    }

    internal void WindowSetPopupSafeRectCore(int windowID, Rect2i rectangle)
    {
        var window = ResolveWindow(windowID);
        if (rectangle.Size.X < 0 || rectangle.Size.Y < 0) throw new ArgumentOutOfRangeException(nameof(rectangle));
        if (rectangle.HasArea()) EnsureGlobalWindowCoordinatesAvailable();
        window._popupSafeRect = rectangle; window._popupSafeRectIsLocal = false;
    }

    internal void SetPopupSafeClientRect(Rect2i rectangle) { EnsureOwner(); _popupSafeRect = rectangle; _popupSafeRectIsLocal = true; }

    internal bool IsPopupSafeClientPoint(Vector2 point)
    {
        if (!_popupSafeRect.HasArea()) return false;
        if (!_popupSafeRectIsLocal) point += (Vector2)NativePopupRoot.WindowGetPositionCore();
        return new Rect2(_popupSafeRect.Position, _popupSafeRect.Size).HasPoint(point);
    }
}
