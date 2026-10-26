namespace Electron2D;

public partial class LineEdit
{
    internal (Vector2 Offset, Vector2 Minimum, Vector2 Maximum, Vector2 Step, Vector2 Page, Rect2 View, byte Axes, byte Ranges) AccessibilityScrollMetrics()
    {
        EnsureLayout(); var view = ContentRect(); var max = Math.Max(0, _layout.Size.X - view.Size.X + GetThemeConstant("caret_width"));
        return (new Vector2(-_scroll, 0), Vector2.Zero, new Vector2(max, 0), new Vector2(Math.Max(1, _fontSize), 1), view.Size, view, (byte)(max > 0 ? 1 : 0), 4);
    }
    internal void AccessibilitySetScrollOffset(Vector2 offset)
    {
        EnsureLayout(); var view = ContentRect(); var max = Math.Max(0, _layout.Size.X - view.Size.X + GetThemeConstant("caret_width"));
        _scroll = -Math.Clamp(offset.X, 0, max); QueueRedraw();
    }
}
