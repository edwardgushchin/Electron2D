namespace Electron2D;

public partial class Tree
{
    internal (Vector2 Offset, Vector2 Minimum, Vector2 Maximum, Vector2 Step, Vector2 Page, Rect2 View, byte Axes, byte Ranges) AccessibilityScrollMetrics()
    {
        EnsureTreeLayout(); var step = _rows.Count > 1 ? _rows[1].Y - _rows[0].Y : _rows.Count == 1 ? _rows[0].Height : 1;
        return AccessibilityServer.ScrollFromBars(_hBar, _vBar, _body, new Vector2(Math.Max(1, _body.Size.X / 8), Math.Max(1, step)), _scrollH, _scrollV, IsLayoutRTL());
    }
    internal void AccessibilitySetScrollOffset(Vector2 offset)
    {
        if (_scrollH) _hBar.Value = IsLayoutRTL() ? -offset.X : offset.X;
        if (_scrollV) _vBar.Value = offset.Y;
    }
}
