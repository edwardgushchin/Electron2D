namespace Electron2D;

public sealed partial class SceneTree
{
    private readonly Dictionary<Viewport, Window> _embeddedFocus = [];
    private readonly Dictionary<Viewport, Window> _embeddedWindowHover = [];
    private readonly Dictionary<Viewport, Window> _embeddedMouseCapture = [];
    private readonly Dictionary<Viewport, (Window Window, Vector2 Start, Vector2i Position, Vector2i Size, int Edges, Rect2 CloseRect)> _embeddedDecorDrag = [];
    private readonly Dictionary<(Viewport, int), Window> _embeddedTouchCapture = [];
    private void ReleaseEmbeddedHost(Viewport host)
    { _embeddedWindowHover.Remove(host); _embeddedFocus.Remove(host); _embeddedMouseCapture.Remove(host); CancelEmbeddedDecoration(host); foreach (var entry in _embeddedTouchCapture.ToArray()) if (entry.Key.Item1 == host) _embeddedTouchCapture.Remove(entry.Key); }
    private void UpdateEmbeddedWindowHover(Viewport host, Vector2 point, bool updatePosition = true)
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
                if (new Rect2(hovered.Position, hovered.Size).HasPoint(point))
                {
                    hovered.NotifyMouseEntered();
                    if (updatePosition && !hovered.IsDisposed && hovered.Visible && !hovered.GUIDisableInput && _embeddedWindowHover.GetValueOrDefault(host) == hovered)
                    {
                        var local = hovered.GetFinalTransform().AffineInverse() * (point - (Vector2)hovered.Position);
                        using var scope = SelectGUI(hovered);
                        try { CaptureInputNodes(); UpdateGUIHover(hovered, local, ref errors); }
                        finally { _gui.InputTraversal.Clear(); }
                    }
                }
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
        CancelEmbeddedDecoration(host);
        previous?.QueueEmbeddedRedraw(); window.QueueEmbeddedRedraw();
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
        if (_embeddedDecorDrag.TryGetValue(host, out var activeDecor) && activeDecor.Window == window) CancelEmbeddedDecoration(host);
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
        if (input is InputEventMouse windowMouse)
        {
            if (!host.IsAttachedToViewport) host.SetLastMousePosition(windowMouse.Position);
            UpdateEmbeddedWindowHover(host, windowMouse.Position, updatePosition: !host.IsAttachedToViewport);
        }
        target = host; if (host.EmbeddedWindows.Count == 0) return RoutePopupInput(host, input, ref errors);
        Vector2? point = input switch { InputEventMouse mouse => mouse.Position, InputEventScreenTouch touch => touch.Position, InputEventScreenDrag drag => drag.Position, InputEventGesture gesture => gesture.Position, _ => null };
        if (ContinueEmbeddedDecoration(host, input, ref errors)) return null;
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
            if (chosen is { Borderless: false } framed)
            {
                if (input is InputEventMouseButton { ButtonIndex: MouseButton.Left, Pressed: true } && BeginEmbeddedDecoration(host, framed, point.Value, ref errors)) return null;
                if (input is InputEventMouseMotion && (framed.EmbeddedTitleRect.HasPoint(point.Value) || framed.EmbeddedResizeEdges(point.Value) != 0)) SetEmbeddedCursor(framed.EmbeddedResizeEdges(point.Value), false, ref errors);
            }
            if (input is InputEventMouseButton button)
            {
                if (button.Pressed && chosen is not null) { _embeddedMouseCapture[host] = chosen; FocusEmbeddedWindow(chosen); }
                else if (!button.Pressed && button.ButtonMask == MouseButtonMask.None) _embeddedMouseCapture.Remove(host);
            }
            if (input is InputEventScreenTouch touchEvent) { if (touchEvent.Pressed && chosen is not null) _embeddedTouchCapture[(host, touchEvent.Index)] = chosen; else if (!touchEvent.Pressed) _embeddedTouchCapture.Remove((host, touchEvent.Index)); }
        }
        else _embeddedFocus.TryGetValue(host, out chosen);
        if (chosen is null || chosen.IsDisposed || !chosen.Visible) return RoutePopupInput(host, input, ref errors);
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
            if (!ReceiveWindowInput(chosen, windowLocal, ref errors)) return null;
            if (chosen.IsDisposed || !ReferenceEquals(chosen.Tree, this) || chosen.GUIDisableInput) return null;
            localized = chosen.MakeViewportInputLocal(windowLocal);
            result = RouteEmbeddedInput(chosen, localized, out target, ref errors);
            return result;
        }
        finally
        {
            if (localized is not null && !ReferenceEquals(localized, input) && !ReferenceEquals(localized, result)) localized.Dispose();
            if (windowLocal is not null && !ReferenceEquals(windowLocal, input) && !ReferenceEquals(windowLocal, localized) && !ReferenceEquals(windowLocal, result)) windowLocal.Dispose();
        }
    }
    private InputEvent? RoutePopupInput(Viewport viewport, InputEvent input, ref List<Exception>? errors)
    {
        if (viewport is Popup popup) try { popup.HandlePopupInput(input); } catch (Exception error) { CollectException(ref errors, error); }
        return !viewport.IsDisposed && ReferenceEquals(viewport.Tree, this) ? input : null;
    }
    private bool ReceiveWindowInput(Window window, InputEvent input, ref List<Exception>? errors)
    {
        if (window.IsDisposed || !ReferenceEquals(window.Tree, this)) return false;
        var blocked = NativeInputBlockedByExclusive(window);
        if (blocked || window.HasExclusiveInputChild)
            try { window.EmitNonclientWindowInput(input); } catch (Exception error) { CollectException(ref errors, error); }
        if (blocked || window.IsDisposed || !ReferenceEquals(window.Tree, this)) return false;
        try { window.EmitWindowInput(input); } catch (Exception error) { CollectException(ref errors, error); }
        return true;
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
