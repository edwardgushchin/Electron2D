namespace Electron2D;

public sealed partial class SceneTree
{
    private readonly Dictionary<Viewport, Window> _embeddedFocus = [];
    private readonly Dictionary<Viewport, Window> _embeddedWindowHover = [];
    private readonly Dictionary<Viewport, Window> _embeddedMouseCapture = [];
    private readonly Dictionary<Viewport, (Window Window, Vector2 Start, Vector2i Position, Vector2i Size, int Edges)> _embeddedDecorDrag = [];
    private readonly Dictionary<(Viewport, int), Window> _embeddedTouchCapture = [];
    private void ReleaseEmbeddedHost(Viewport host)
    { _embeddedWindowHover.Remove(host); _embeddedFocus.Remove(host); _embeddedMouseCapture.Remove(host); _embeddedDecorDrag.Remove(host); foreach (var entry in _embeddedTouchCapture.ToArray()) if (entry.Key.Item1 == host) _embeddedTouchCapture.Remove(entry.Key); }
    private void UpdateEmbeddedWindowHover(Viewport host, Vector2 point)
    {
        Window? hovered = null;
        for (var i = host.EmbeddedWindows.Count - 1; i >= 0; i--)
        {
            var candidate = host.EmbeddedWindows[i];
            if (!candidate.IsDisposed && candidate.Visible && candidate.EmbeddedHitRect.HasPoint(point)) { hovered = candidate; break; }
        }
        var previous = _embeddedWindowHover.GetValueOrDefault(host);
        List<Exception>? errors = null;
        if (previous != hovered)
        {
            if (hovered is null) _embeddedWindowHover.Remove(host); else _embeddedWindowHover[host] = hovered;
            try { if (previous is { IsDisposed: false }) previous.ExitWindowMouse(); } catch (Exception error) { CollectException(ref errors, error); }
            try { if (hovered is { IsDisposed: false, Visible: true } && _embeddedWindowHover.GetValueOrDefault(host) == hovered) hovered.EnterWindowMouse(); } catch (Exception error) { CollectException(ref errors, error); }
        }
        if (hovered is { IsDisposed: false, Visible: true } && _embeddedWindowHover.GetValueOrDefault(host) == hovered)
            try
            {
                if (new Rect2(hovered.Position, hovered.Size).HasPoint(point)) hovered.NotifyMouseEntered();
                else hovered.NotifyMouseExited();
            }
            catch (Exception error) { CollectException(ref errors, error); }
        ThrowCollected("Embedded window hover callbacks failed.", errors);
    }
    internal bool HasEmbeddedWindowFocus(Window window) { EnsureOwnerThread(); return window.Embedder is { } host && _embeddedFocus.GetValueOrDefault(host) == window; }
    internal void FocusEmbeddedWindow(Window window)
    {
        EnsureOwnerThread(); if (window.IsDisposed || window.Embedder is not { } host || !window.Visible || window.Unfocusable) return;
        var previous = _embeddedFocus.GetValueOrDefault(host); if (previous == window) return;
        _embeddedFocus[host] = window; host.EmbeddedWindows.Remove(window); host.EmbeddedWindows.Add(window); window.RaiseEmbeddedCanvas();
        List<Exception>? errors = null;
        try { if (previous is { IsDisposed: false }) previous.EmitEmbeddedFocus(false); } catch (Exception error) { CollectException(ref errors, error); }
        try { ExitGUIViewport(host); } catch (Exception error) { CollectException(ref errors, error); }
        try { window.EmitEmbeddedFocus(true); } catch (Exception error) { CollectException(ref errors, error); }
        ThrowCollected("Embedded window focus callbacks failed.", errors);
    }
    internal void HideEmbeddedWindow(Window window)
    {
        if (window.Embedder is not { } host) return;
        List<Exception>? errors = null;
        foreach (var child in host.EmbeddedWindows.ToArray()) if (child != window && window.IsAncestorOf(child) && !child.IsDisposed && child.Visible) try { child.Hide(); } catch (Exception error) { CollectException(ref errors, error); }
        if (_embeddedWindowHover.GetValueOrDefault(host) == window)
        {
            _embeddedWindowHover.Remove(host);
            try { if (!window.IsDisposed) window.ExitWindowMouse(); } catch (Exception error) { CollectException(ref errors, error); }
        }
        try { DisableGUIViewport(window); } catch (Exception error) { CollectException(ref errors, error); }
        if (_embeddedMouseCapture.GetValueOrDefault(host) == window) _embeddedMouseCapture.Remove(host);
        if (_embeddedDecorDrag.TryGetValue(host, out var activeDecor) && activeDecor.Window == window) _embeddedDecorDrag.Remove(host);
        foreach (var entry in _embeddedTouchCapture.ToArray()) if (entry.Value == window) _embeddedTouchCapture.Remove(entry.Key);
        if (_embeddedFocus.GetValueOrDefault(host) == window)
        {
            _embeddedFocus.Remove(host);
            try { if (!window.IsDisposed) window.EmitEmbeddedFocus(false); } catch (Exception error) { CollectException(ref errors, error); }
            if (IsClosing || host.IsDepartingTree) { ThrowCollected("Embedded window hide callbacks failed.", errors); return; }
            Window? next = null; for (var i = host.EmbeddedWindows.Count - 1; i >= 0; i--) { var candidate = host.EmbeddedWindows[i]; if (candidate != window && !candidate.IsDisposed && !candidate.IsDepartingTree && candidate.Visible && !candidate.Unfocusable) { next = candidate; break; } }
            try { if (next is not null) FocusEmbeddedWindow(next); else if (host is Window { IsDisposed: false } parent) parent.EmitEmbeddedFocus(true); } catch (Exception error) { CollectException(ref errors, error); }
        }
        ThrowCollected("Embedded window hide callbacks failed.", errors);
    }
    internal Window GetEmbeddedTextWindow(Window root) { var window = root; while (_embeddedFocus.GetValueOrDefault(window) is { IsDisposed: false, Visible: true } next) window = next; return window; }
    private InputEvent? RouteEmbeddedInput(Viewport host, InputEvent input, out Viewport target, ref List<Exception>? errors)
    {
        if (input is InputEventMouse windowMouse) UpdateEmbeddedWindowHover(host, windowMouse.Position);
        target = host; if (host.EmbeddedWindows.Count == 0) return input;
        Vector2? point = input switch { InputEventMouse mouse => mouse.Position, InputEventScreenTouch touch => touch.Position, InputEventScreenDrag drag => drag.Position, InputEventGesture gesture => gesture.Position, _ => null };
        if (_embeddedDecorDrag.TryGetValue(host, out var decor))
        {
            if (decor.Window.IsDisposed || !decor.Window.Visible) _embeddedDecorDrag.Remove(host);
            else if (input is InputEventMouseMotion move)
            {
                var delta = (Vector2i)(move.Position - decor.Start);
                if (decor.Edges == 0) decor.Window.Position = decor.Position + delta;
                else
                {
                    var size = decor.Size; var position = decor.Position;
                    if ((decor.Edges & 1) != 0) { size.X = Math.Max(1, decor.Size.X - delta.X); position.X = decor.Position.X + decor.Size.X - size.X; }
                    if ((decor.Edges & 2) != 0) size.X = Math.Max(1, decor.Size.X + delta.X);
                    if ((decor.Edges & 4) != 0) { size.Y = Math.Max(1, decor.Size.Y - delta.Y); position.Y = decor.Position.Y + decor.Size.Y - size.Y; }
                    if ((decor.Edges & 8) != 0) size.Y = Math.Max(1, decor.Size.Y + delta.Y);
                    size.X = Math.Max(size.X, Math.Max(1, decor.Window.MinSize.X)); size.Y = Math.Max(size.Y, Math.Max(1, decor.Window.MinSize.Y));
                    if (decor.Window.MaxSize.X > 0) size.X = Math.Min(size.X, decor.Window.MaxSize.X); if (decor.Window.MaxSize.Y > 0) size.Y = Math.Min(size.Y, decor.Window.MaxSize.Y);
                    decor.Window.Size = size; decor.Window.Position = position;
                }
                return null;
            }
            else if (input is InputEventMouseButton { ButtonIndex: MouseButton.Left, Pressed: false }) { _embeddedDecorDrag.Remove(host); return null; }
        }
        Window? chosen = null;
        if (point is not null)
        {
            if (input is InputEventMouse) _embeddedMouseCapture.TryGetValue(host, out chosen);
            if (input is InputEventScreenDrag drag) _embeddedTouchCapture.TryGetValue((host, drag.Index), out chosen);
            if (input is InputEventScreenTouch { Pressed: false } touch) _embeddedTouchCapture.TryGetValue((host, touch.Index), out chosen);
            if (chosen is { IsDisposed: true } || chosen is { Visible: false }) chosen = null;
            for (var i = host.EmbeddedWindows.Count - 1; chosen is null && i >= 0; i--)
            {
                var window = host.EmbeddedWindows[i]; if (window.IsDisposed || !window.Visible) continue;
                if (window.AcceptEmbeddedPointer(point.Value, input) && (window.PopupWindow || window.Exclusive || window.EmbeddedHitRect.HasPoint(point.Value))) { chosen = window; break; }
            }
            if (chosen is not null && !chosen.EmbeddedHitRect.HasPoint(point.Value) && chosen.PopupWindow && !chosen.Exclusive && input is InputEventMouseButton { Pressed: true }) { chosen.RequestEmbeddedClose(); return null; }
            if (chosen is { Exclusive: true } && !chosen.EmbeddedHitRect.HasPoint(point.Value) && input is InputEventMouseButton { Pressed: true }) return null;
            if (chosen is { Borderless: false } framed && input is InputEventMouseButton { ButtonIndex: MouseButton.Left, Pressed: true })
            {
                var titleHeight = framed.GetThemeConstant("title_height", "Window"); var margin = framed.GetThemeConstant("resize_margin", "Window"); var p = point.Value; var pos = framed.Position; var size = framed.Size; var edges = 0;
                if (!framed.Unresizable) { if (p.X < pos.X + margin) edges |= 1; else if (p.X >= pos.X + size.X - margin) edges |= 2; if (p.Y < pos.Y - titleHeight + margin) edges |= 4; else if (p.Y >= pos.Y + size.Y - margin) edges |= 8; }
                if (edges != 0 || p.Y < pos.Y)
                {
                    FocusEmbeddedWindow(framed);
                    if (edges == 0 && p.X >= pos.X + size.X - 36) framed.RequestEmbeddedClose();
                    else _embeddedDecorDrag[host] = (framed, p, pos, size, edges);
                    return null;
                }
            }
            if (input is InputEventMouseButton button)
            {
                if (button.Pressed && chosen is not null) { _embeddedMouseCapture[host] = chosen; FocusEmbeddedWindow(chosen); }
                else if (!button.Pressed && button.ButtonMask == MouseButtonMask.None) _embeddedMouseCapture.Remove(host);
            }
            if (input is InputEventScreenTouch touchEvent) { if (touchEvent.Pressed && chosen is not null) _embeddedTouchCapture[(host, touchEvent.Index)] = chosen; else if (!touchEvent.Pressed) _embeddedTouchCapture.Remove((host, touchEvent.Index)); }
        }
        else _embeddedFocus.TryGetValue(host, out chosen);
        if (chosen is null || chosen.IsDisposed || !chosen.Visible) return input;
        if (point != null && input is InputEventMouseMotion && chosen is PopupMenu menu)
        {
            while (menu.Parent is PopupMenu parentMenu) menu = parentMenu;
            if (menu.Parent is Control owner && (owner is MenuButton { SwitchOnHover: true } || owner is MenuBar { SwitchOnHover: true }) && !menu.EmbeddedHitRect.HasPoint(point.Value)) RefreshMenuBarHover(owner, host, point.Value);
        }
        target = chosen;
        InputEvent? windowLocal = null, localized = null, result = null;
        try
        {
            windowLocal = input.XformedBy(new Transform(0, -(Vector2)chosen.Position));
            ReceiveWindowInput(chosen, windowLocal, ref errors);
            if (chosen.IsDisposed || !ReferenceEquals(chosen.Tree, this) || chosen.GUIDisableInput) return null;
            localized = chosen.MakeViewportInputLocal(windowLocal);
            result = chosen.EmbeddedWindows.Count > 0 ? RouteEmbeddedInput(chosen, localized, out target, ref errors) : localized;
            return result;
        }
        finally
        {
            if (localized is not null && !ReferenceEquals(localized, input) && !ReferenceEquals(localized, result)) localized.Dispose();
            if (windowLocal is not null && !ReferenceEquals(windowLocal, input) && !ReferenceEquals(windowLocal, localized) && !ReferenceEquals(windowLocal, result)) windowLocal.Dispose();
        }
    }
    private void ReceiveWindowInput(Window window, InputEvent input, ref List<Exception>? errors)
    {
        if (window is Popup popup) try { popup.HandlePopupInput(input); } catch (Exception error) { CollectException(ref errors, error); }
        if (window.IsDisposed || !ReferenceEquals(window.Tree, this)) return;
        try { window.EmitWindowInput(input); } catch (Exception error) { CollectException(ref errors, error); }
    }
    private void RefreshMenuBarHover(Control owner, Viewport host, Vector2 point)
    {
        if (owner.GetViewport() is not { } viewport) return;
        if (viewport != host && viewport is Window window) point -= (Vector2)window.Position;
        using var scope = SelectGUI(viewport); CaptureInputNodes(); List<Exception>? errors = null;
        try
        {
            var target = FindMouseControl(viewport, point, ref errors);
            if (owner is MenuButton button && target is MenuButton other && button.CanSwitchTo(other) || owner is MenuBar && target == owner) UpdateGUIHover(viewport, point, ref errors);
            else try { ClearGUIHover(); } catch (Exception e) { CollectException(ref errors, e); }
        }
        finally { _gui.InputTraversal.Clear(); }
        ThrowCollected("Menu bar hover callbacks failed.", errors);
    }

}
