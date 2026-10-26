namespace Electron2D;

public partial class RichTextLabel
{
    internal (Vector2 Offset, Vector2 Minimum, Vector2 Maximum, Vector2 Step, Vector2 Page, Rect2 View, byte Axes, byte Ranges) AccessibilityScrollMetrics()
    {
        EnsureRichLayout(); var height = _lines.Count == 0 ? 1 : Math.Max(1, LoadedLineHeight(_lines[0]));
        return AccessibilityServer.ScrollFromBars(null, _scroll, _textRect, new Vector2(1, height), false, _scrollActive);
    }
    internal void AccessibilitySetScrollOffset(Vector2 offset)
    {
        if (_scrollActive) _scroll.Value = offset.Y;
    }
}
