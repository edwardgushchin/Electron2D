namespace Electron2D;

public sealed partial class SceneTree
{
    private readonly Dictionary<Viewport, Window> _embeddedFocus = [];
    private readonly Dictionary<Viewport, Window> _embeddedMouseCapture = [];
    private readonly Dictionary<Viewport, (Window Window, Vector2 Start, Vector2i Position, Vector2i Size, int Edges)> _embeddedDecorDrag = [];
    private readonly Dictionary<(Viewport, int), Window> _embeddedTouchCapture = [];
    private void ReleaseEmbeddedHost(Viewport host)
    { _embeddedFocus.Remove(host); _embeddedMouseCapture.Remove(host); _embeddedDecorDrag.Remove(host); foreach (var entry in _embeddedTouchCapture.ToArray()) if (entry.Key.Item1 == host) _embeddedTouchCapture.Remove(entry.Key); }
    internal bool HasEmbeddedWindowFocus(Window window) { EnsureOwnerThread(); return window.Embedder is { } host && _embeddedFocus.GetValueOrDefault(host) == window; }
    internal void FocusEmbeddedWindow(Window window)
    {
        EnsureOwnerThread(); if (window.Embedder is not { } host || !window.Visible || window.Unfocusable) return;
        var previous = _embeddedFocus.GetValueOrDefault(host); if (previous == window) return;
        _embeddedFocus[host] = window; host.EmbeddedWindows.Remove(window); host.EmbeddedWindows.Add(window); window.RaiseEmbeddedCanvas();
        List<Exception>? errors = null;
        try { previous?.EmitEmbeddedFocus(false); } catch (Exception error) { CollectException(ref errors, error); }
        try { ExitGUIViewport(host); } catch (Exception error) { CollectException(ref errors, error); }
        try { window.EmitEmbeddedFocus(true); } catch (Exception error) { CollectException(ref errors, error); }
        ThrowCollected("Embedded window focus callbacks failed.", errors);
    }
    internal void HideEmbeddedWindow(Window window)
    {
        if (window.Embedder is not { } host) return;
        List<Exception>? errors = null;
        foreach (var child in host.EmbeddedWindows.ToArray()) if (child != window && window.IsAncestorOf(child) && !child.IsDisposed && child.Visible) try { child.Hide(); } catch (Exception error) { CollectException(ref errors, error); }
        try { DisableGUIViewport(window); } catch (Exception error) { CollectException(ref errors, error); }
        if (_embeddedMouseCapture.GetValueOrDefault(host) == window) _embeddedMouseCapture.Remove(host);
        if (_embeddedDecorDrag.TryGetValue(host, out var activeDecor) && activeDecor.Window == window) _embeddedDecorDrag.Remove(host);
        foreach (var entry in _embeddedTouchCapture.ToArray()) if (entry.Value == window) _embeddedTouchCapture.Remove(entry.Key);
        if (_embeddedFocus.GetValueOrDefault(host) == window)
        {
            _embeddedFocus.Remove(host);
            try { window.EmitEmbeddedFocus(false); } catch (Exception error) { CollectException(ref errors, error); }
            Window? next = null; for (var i = host.EmbeddedWindows.Count - 1; i >= 0; i--) { var candidate = host.EmbeddedWindows[i]; if (candidate != window && !candidate.IsDisposed && candidate.Visible && !candidate.Unfocusable) { next = candidate; break; } }
            try { if (next is not null) FocusEmbeddedWindow(next); else if (host is Window parent) parent.EmitEmbeddedFocus(true); } catch (Exception error) { CollectException(ref errors, error); }
        }
        ThrowCollected("Embedded window hide callbacks failed.", errors);
    }
    internal Window GetEmbeddedTextWindow(Window root) { var window = root; while (_embeddedFocus.GetValueOrDefault(window) is { IsDisposed: false, Visible: true } next) window = next; return window; }
    private InputEvent? RouteEmbeddedInput(Viewport host, InputEvent input, out Viewport target)
    {
        target = host; if (host.EmbeddedWindows.Count == 0) { if (host is Popup directPopup) directPopup.HandlePopupInput(input); return input; }
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
        if (chosen is null || chosen.IsDisposed || !chosen.Visible) { if (host is Popup directPopup) directPopup.HandlePopupInput(input); return input; }
        if (point != null && input is InputEventMouseMotion && chosen is PopupMenu menu)
        {
            while (menu.Parent is PopupMenu parentMenu) menu = parentMenu;
            if (menu.Parent is MenuButton { SwitchOnHover: true } owner && !menu.EmbeddedHitRect.HasPoint(point.Value)) RefreshMenuBarHover(owner, host, point.Value);
        }
        target = chosen;
        var localized = input.XformedBy(new Transform(0, -(Vector2)chosen.Position));
        if (localized is InputEventMouse mouseLocal) mouseLocal.GlobalPosition = mouseLocal.Position;
        try
        {
            if (chosen.EmbeddedWindows.Count > 0) { var nested = RouteEmbeddedInput(chosen, localized, out target); if (!ReferenceEquals(nested, localized)) localized.Dispose(); return nested; }
            if (chosen is Popup popup) popup.HandlePopupInput(localized); return localized;
        }
        catch { if (!ReferenceEquals(localized, input)) localized.Dispose(); throw; }
    }
    private void RefreshMenuBarHover(MenuButton owner, Viewport host, Vector2 point)
    {
        if (owner.GetViewport() is not { } viewport) return;
        if (viewport != host && viewport is Window window) point -= (Vector2)window.Position;
        using var scope = SelectGUI(viewport); CaptureInputNodes(); List<Exception>? errors = null;
        try
        {
            var target = FindMouseControl(viewport, point, ref errors);
            if (target is MenuButton other && owner.CanSwitchTo(other)) UpdateGUIHover(viewport, point, ref errors);
            else try { ClearGUIHover(); } catch (Exception e) { CollectException(ref errors, e); }
        }
        finally { _gui.InputTraversal.Clear(); }
        ThrowCollected("Menu bar hover callbacks failed.", errors);
    }

}
