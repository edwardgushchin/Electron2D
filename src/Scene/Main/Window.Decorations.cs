namespace Electron2D;

public partial class Window
{
    private bool _embeddedClosePressed;
    internal int EmbeddedTitleHeight => Math.Max(0, GetThemeConstant("title_height", "Window"));
    internal Rect2 EmbeddedFrameRect => new((Vector2)Position - new Vector2(0, EmbeddedTitleHeight), (Vector2)Size + new Vector2(0, EmbeddedTitleHeight));
    internal Rect2 EmbeddedTitleRect => new((Vector2)Position - new Vector2(0, EmbeddedTitleHeight), new Vector2(Size.X, EmbeddedTitleHeight));
    internal Rect2 EmbeddedCloseRect => new((Vector2)Position + new Vector2((float)Size.X - GetThemeConstant("close_h_offset", "Window"), -(float)GetThemeConstant("close_v_offset", "Window")), GetThemeIcon("close", "Window")?.GetSize() ?? Vector2.Zero);
    internal void SetEmbeddedClosePressed(bool pressed)
    { if (_embeddedClosePressed == pressed) return; _embeddedClosePressed = pressed; if (!IsDisposed) QueueEmbeddedRedraw(); }
    internal int EmbeddedResizeEdges(Vector2 point)
    {
        if (Borderless || Unresizable) return 0;
        var rect = EmbeddedFrameRect; if (rect.HasPoint(point)) return 0;
        var x = point.X < rect.Position.X ? point.X - rect.Position.X : point.X > rect.End.X ? point.X - rect.End.X : 0;
        var y = point.Y < rect.Position.Y ? point.Y - rect.Position.Y : point.Y > rect.End.Y ? point.Y - rect.End.Y : 0;
        var margin = Math.Max(0, GetThemeConstant("resize_margin", "Window")); if (MathF.Abs(x) > margin || MathF.Abs(y) > margin) return 0;
        return (x < 0 ? 1 : x > 0 ? 2 : 0) | (y < 0 ? 4 : y > 0 ? 8 : 0);
    }
    private Vector2 DecorationMetric(bool size)
    {
        var a = GetThemeStyleBox("embedded_border", "Window"); var b = GetThemeStyleBox("embedded_unfocused_border", "Window");
        return (a is null ? Vector2.Zero : size ? a.GetMinimumSize() : a.GetOffset()).Max(b is null ? Vector2.Zero : size ? b.GetMinimumSize() : b.GetOffset());
    }
}
