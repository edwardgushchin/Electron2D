namespace Electron2D;

public sealed partial class SceneTree
{
    private readonly List<Window> _nativePopups = [];

    internal void RegisterNativePopup(Window window)
    {
        if (window.NativeDisplay is not { IsNativePopup: true, PopupInputPolicy: true }) return;
        _nativePopups.Remove(window); _nativePopups.Add(window);
    }

    internal void UnregisterNativePopup(Window window) => _nativePopups.Remove(window);

    internal Window? ActiveNativePopup
    {
        get
        {
            EnsureOwnerThread();
            for (var i = _nativePopups.Count - 1; i >= 0; i--)
                if (_nativePopups[i] is { IsDisposed: false, Visible: true } popup && popup.NativeDisplay is not null) return popup;
            return null;
        }
    }

    private static bool NativeInputBlockedByExclusive(Window source)
    {
        if (source.NativeDisplay?.HasExclusiveNativeChild() == true) return true;
        var target = source.GetLastExclusiveWindow();
        while (target?.Embedder is { } host) target = host.GetWindow();
        return target is not null && !ReferenceEquals(target.NativeDisplay, source.NativeDisplay);
    }

    private InputEvent? RouteNativePopupInput(Window source, InputEvent input, out Window target)
    {
        target = source;
        if (ActiveNativePopup is not { } popup || popup == source || source.NativeDisplay is not { } sourceDisplay || popup.NativeDisplay is not { } popupDisplay) return input;
        var sameRoot = ReferenceEquals(sourceDisplay.NativePopupRoot, popupDisplay.NativePopupRoot);
        Vector2? point = input switch { InputEventMouse mouse => mouse.Position, InputEventScreenTouch touch => touch.Position, InputEventScreenDrag drag => drag.Position, InputEventGesture gesture => gesture.Position, _ => null };
        if (sameRoot && point is { } at)
        {
            var rootPoint = at + (Vector2)sourceDisplay.PopupClientOrigin;
            if (popupDisplay.IsPopupSafeClientPoint(rootPoint)) return input;
            if (new Rect2(popupDisplay.PopupClientOrigin, popup.Size).HasPoint(rootPoint))
            {
                target = popup;
                return input.XformedBy(new Transform(0, sourceDisplay.PopupClientOrigin - popupDisplay.PopupClientOrigin));
            }
        }
        if (input is InputEventMouseButton { Pressed: true } or InputEventScreenTouch { Pressed: true })
        {
            if (popupDisplay.TransientExclusive) return sameRoot ? null : input;
            for (var i = _nativePopups.Count - 1; i >= 0; i--)
            {
                var candidate = _nativePopups[i];
                if (candidate.IsDisposed || !candidate.Visible) continue;
                if (candidate == source || (candidate.NativeDisplay?.TransientExclusive ?? candidate.Exclusive)) break;
                candidate.RequestEmbeddedClose();
            }
            return sourceDisplay.IsNativePopup ? input : null;
        }
        if (sameRoot && point is null && !popupDisplay.WindowGetFlagCore(WindowFlag.NoFocus)) { target = popup; return input; }
        return input;
    }
}
