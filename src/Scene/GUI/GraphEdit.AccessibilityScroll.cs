namespace Electron2D;

public partial class GraphEdit
{
    internal (Vector2 Offset, Vector2 Minimum, Vector2 Maximum, Vector2 Step, Vector2 Page, Rect2 View, byte Axes, byte Ranges) AccessibilityScrollMetrics()
    {
        CheckEdit(); var view = new Rect2(Vector2.Zero, Size); var step = Math.Max(1, _snapDistance * _zoom);
        return (_scroll, Vector2.Zero, Vector2.Zero, new Vector2(step, step), Size, view, 3, 0);
    }
    internal void AccessibilitySetScrollOffset(Vector2 offset)
    {
        UserScroll(offset);
    }
}
