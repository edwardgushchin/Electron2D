namespace Electron2D;

public sealed partial class SceneTree
{
    private void CancelEmbeddedDecoration(Viewport host)
    { if (_embeddedDecorDrag.Remove(host, out var active)) active.Window.SetEmbeddedClosePressed(false); }
    internal void CancelEmbeddedDecoration(Window window, bool resizingOnly = false)
    {
        if (window.Embedder is { } host && _embeddedDecorDrag.TryGetValue(host, out var active) && active.Window == window && (!resizingOnly || active.Edges > 0)) CancelEmbeddedDecoration(host);
    }
    internal void CancelEmbeddedDecorationTree(Window window)
    {
        CancelEmbeddedDecoration(window);
        foreach (var pair in _embeddedDecorDrag.ToArray()) if (pair.Key == window || window.IsAncestorOf(pair.Key)) CancelEmbeddedDecoration(pair.Key);
    }
    private static void SetEmbeddedCursor(int edges, bool moving, ref List<Exception>? errors)
    {
        if (DisplayServer.Service is not { } display) return;
        var shape = moving ? CursorShape.Move : edges switch { 5 or 10 => CursorShape.FDiagSize, 6 or 9 => CursorShape.BDiagSize, 1 or 2 => CursorShape.HSize, 4 or 8 => CursorShape.VSize, _ => CursorShape.Arrow };
        try { if (display.CursorGetShapeCore() != shape) display.CursorSetShapeCore(shape); } catch (Exception error) { CollectException(ref errors, error); }
    }
    private bool BeginEmbeddedDecoration(Viewport host, Window window, Vector2 point, ref List<Exception>? errors)
    {
        var title = window.EmbeddedTitleRect.HasPoint(point); var edges = title ? 0 : window.EmbeddedResizeEdges(point); if (!title && edges == 0) return false;
        try { FocusEmbeddedWindow(window); } catch (Exception error) { CollectException(ref errors, error); }
        if (window.IsDisposed || !window.Visible || window.Embedder != host || window.Borderless) return true;
        var close = window.EmbeddedCloseRect; var closing = title && close.HasPoint(point);
        _embeddedDecorDrag[host] = (window, point, window.Position, window.Size, closing ? -1 : edges, close); window.SetEmbeddedClosePressed(closing);
        return true;
    }
    private bool ContinueEmbeddedDecoration(Viewport host, InputEvent input, ref List<Exception>? errors)
    {
        if (!_embeddedDecorDrag.TryGetValue(host, out var active)) return false;
        var window = active.Window;
        if (window.IsDisposed || !window.Visible || window.Embedder != host || window.Borderless || active.Edges > 0 && window.Unresizable)
        { CancelEmbeddedDecoration(host); return false; }
        if (input is InputEventMouseButton { ButtonIndex: MouseButton.Left, Pressed: false } release)
        {
            CancelEmbeddedDecoration(host);
            if (active.Edges < 0 && active.CloseRect.HasPoint(release.Position)) try { window.RequestEmbeddedClose(); } catch (Exception error) { CollectException(ref errors, error); }
        }
        else if (input is InputEventMouseMotion motion)
        {
            if (active.Edges < 0) window.SetEmbeddedClosePressed(active.CloseRect.HasPoint(motion.Position));
            else
            {
                static int Position(long value) => (int)Math.Clamp(value, int.MinValue, int.MaxValue);
                static int Dimension(long value) => (int)Math.Clamp(value, 1, int.MaxValue);
                var dx = (long)Math.Clamp((double)motion.Position.X - active.Start.X, int.MinValue, int.MaxValue); var dy = (long)Math.Clamp((double)motion.Position.Y - active.Start.Y, int.MinValue, int.MaxValue);
                if (active.Edges == 0)
                {
                    try { window.Position = new(Position((long)active.Position.X + dx), Position((long)active.Position.Y + dy)); } catch (Exception error) { CollectException(ref errors, error); }
                }
                else
                {
                    var size = active.Size;
                    if ((active.Edges & 1) != 0) size.X = Dimension((long)size.X - dx); else if ((active.Edges & 2) != 0) size.X = Dimension((long)size.X + dx);
                    if ((active.Edges & 4) != 0) size.Y = Dimension((long)size.Y - dy); else if ((active.Edges & 8) != 0) size.Y = Dimension((long)size.Y + dy);
                    try { window.Size = size; } catch (Exception error) { CollectException(ref errors, error); }
                    if (!window.IsDisposed && window.Visible && window.Embedder == host)
                    {
                        var position = active.Position;
                        if ((active.Edges & 1) != 0) position.X = Position((long)active.Position.X + active.Size.X - window.Size.X);
                        if ((active.Edges & 4) != 0) position.Y = Position((long)active.Position.Y + active.Size.Y - window.Size.Y);
                        try { window.Position = position; } catch (Exception error) { CollectException(ref errors, error); }
                    }
                }
            }
            SetEmbeddedCursor(Math.Max(0, active.Edges), active.Edges == 0, ref errors);
        }
        return true;
    }
}
