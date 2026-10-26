namespace Electron2D;

public partial class ScrollContainer
{
    internal (Vector2 Offset, Vector2 Minimum, Vector2 Maximum, Vector2 Step, Vector2 Page, Rect2 View, byte Axes, byte Ranges) AccessibilityScrollMetrics()
    {
        var (left, top, right, bottom) = Margins();
        var vw = VVisible || _verticalMode == ScrollMode.Reserve ? _vBar.GetMinimumSize().X + GetThemeConstant("scrollbar_h_separation") : 0;
        var hh = HVisible || _horizontalMode == ScrollMode.Reserve ? _hBar.GetMinimumSize().Y + GetThemeConstant("scrollbar_v_separation") : 0;
        var view = new Rect2(new Vector2(left + (IsLayoutRTL() ? vw : 0), top), (Size - new Vector2(left + right + vw, top + bottom + hh)).Max(Vector2.Zero));
        return AccessibilityServer.ScrollFromBars(_hBar, _vBar, view, new Vector2((float)(_hBar.CustomStep > 0 ? _hBar.CustomStep : Math.Max(1, _hBar.Step)), (float)(_vBar.CustomStep > 0 ? _vBar.CustomStep : Math.Max(1, _vBar.Step))), HEnabled, VEnabled);
    }
    internal bool AccessibilityScrollsChild(Node child) => child != _hBar && child != _vBar && child != _focusPanel && Sortable(child);
    internal void AccessibilitySetScrollOffset(Vector2 offset)
    {
        if (HEnabled) ScrollHorizontal = AccessibilityServer.ScrollInteger(offset.X);
        if (VEnabled) ScrollVertical = AccessibilityServer.ScrollInteger(offset.Y);
        Arrange();
    }
}
