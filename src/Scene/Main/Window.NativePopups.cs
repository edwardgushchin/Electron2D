namespace Electron2D;

public partial class Window
{
    private Vector2i? _popupClientPosition;
    private Window? _popupPlacementRoot;

    private Window VisiblePopupParent()
    {
        for (var parent = Parent?.GetWindow(); parent is not null; parent = parent.Parent?.GetWindow())
            if (parent.Visible) return parent;
        throw new InvalidOperationException("Native popup presentation requires a visible parent window.");
    }

    private DisplayServer PopupParentDisplay()
    {
        var owner = _transientToFocused ? (DisplayServer.Service?.FocusedSceneWindow ?? _transientParent ?? VisiblePopupParent()) : VisiblePopupParent();
        for (var window = owner; window is not null; window = window.Embedder?.GetWindow() ?? window.Parent?.GetWindow())
            if (window._display is { } display) return display;
        throw new InvalidOperationException("Native popup presentation requires a live native parent.");
    }

    private Vector2i InitialNativePopupOffset(DisplayServer parent)
    {
        if (_popupClientPosition is { } position)
        {
            if (!ReferenceEquals(_popupPlacementRoot, parent.NativePopupRoot.SceneWindow))
            {
                var source = _popupPlacementRoot?._display ?? throw new InvalidOperationException("The popup placement source is no longer active.");
                position += source.WindowGetPositionCore() - parent.NativePopupRoot.WindowGetPositionCore();
            }
            return position - parent.PopupClientOrigin;
        }
        return _screenPosition is { } global ? global - parent.WindowGetPositionCore() : Vector2i.Zero;
    }

    internal Vector2i PopupPosition
    {
        get
        {
            if (_display is { IsNativePopup: true } display) return display.PopupClientOrigin;
            if (_popupClientPosition is { } position) return position;
            if (Embedder is null && PopupWMHint && IsInsideTree && Parent is not null)
            {
                var parent = PopupParentDisplay();
                return _screenPosition is { } global ? global - parent.NativePopupRoot.WindowGetPositionCore() : parent.PopupClientOrigin;
            }
            return Position;
        }
        set
        {
            if (Embedder is null && PopupWMHint && IsInsideTree && Parent is not null)
            {
                EnsureMutable(); RenderingOwner?.EnsureViewportMutation();
                if (_display is { IsNativePopup: true } display) display.NativePopupOffset = value - display.NativePopupParent!.PopupClientOrigin;
                _popupClientPosition = value;
                _popupPlacementRoot ??= PopupParentDisplay().NativePopupRoot.SceneWindow;
            }
            else Position = value;
        }
    }

    internal void PopupFromViewport(Viewport source, Rect2i rectangle, Rect2i safeRect = default)
    {
        EnsureMutable(); CheckPopup();
        if (!ReferenceEquals(source.Tree, Tree)) throw new InvalidOperationException("The popup source belongs to another scene tree.");
        var screen = source.GetScreenTransform();
        var rect = new Rect2(rectangle.Position, rectangle.Size);
        if (Embedder is { } embedder)
        {
            rect = embedder.GetScreenTransform().AffineInverse() * screen * rect;
            Popup(CheckedPopupRectangle(rect));
            return;
        }
        var sourceWindow = source.GetWindow() ?? throw new InvalidOperationException("The popup source has no containing window.");
        while (sourceWindow.Embedder is { } host) sourceWindow = host.GetWindow()!;
        var display = sourceWindow._display ?? throw new InvalidOperationException("The popup source has no native window.");
        rect = screen * rect;
        var placement = CheckedPopupRectangle(rect);
        var at = placement.Position + display.PopupClientOrigin;
        var size = placement.Size;
        if (!PopupWMHint) { Popup(new(display.NativePopupRoot.WindowGetPositionCore() + at, size)); return; }
        _popupPlacementRoot = display.NativePopupRoot.SceneWindow;
        PopupAt(new(at, size), centered: false, nativeRelative: true);
        if (_display is { IsNativePopup: true } popup && safeRect.HasArea())
        {
            var safe = CheckedPopupRectangle(screen * new Rect2(safeRect.Position, safeRect.Size));
            popup.SetPopupSafeClientRect(new(safe.Position + display.PopupClientOrigin, safe.Size));
        }
    }

    private static Rect2i CheckedPopupRectangle(Rect2 rect) => new(
        new(checked((int)rect.Position.X), checked((int)rect.Position.Y)),
        new(Math.Max(1, checked((int)rect.Size.X)), Math.Max(1, checked((int)rect.Size.Y))));

    private Rect2i GetPopupCenterRect()
    {
        if (Embedder is not null) return GetUsableParentRect();
        var parent = PopupParentDisplay(); var screen = parent.WindowGetCurrentScreenCore();
        return new(parent.ScreenGetPositionCore(screen), parent.ScreenGetSizeCore(screen));
    }

    private void PopupRelativeToParent(Rect2i rectangle)
    {
        EnsureMutable(); CheckPopup();
        if (Embedder is not null) { Popup(rectangle); return; }
        var parent = VisiblePopupParent();
        var size = rectangle == default ? Size : rectangle.Size;
        if (size.X <= 0 || size.Y <= 0) throw new ArgumentOutOfRangeException(nameof(rectangle));
        var rect = parent.GetFinalTransform().AffineInverse() * new Rect2(rectangle.Position, size);
        PopupFromViewport(parent, CheckedPopupRectangle(rect));
    }
}
