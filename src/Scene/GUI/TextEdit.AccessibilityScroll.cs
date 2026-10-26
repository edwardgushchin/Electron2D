namespace Electron2D;

public partial class TextEdit
{
    internal (Vector2 Offset, Vector2 Minimum, Vector2 Maximum, Vector2 Step, Vector2 Page, Rect2 View, byte Axes, byte Ranges) AccessibilityScrollMetrics()
    {
        EnsureTextLayout(); var state = AccessibilityServer.ScrollFromBars(_hBar, _vBar, _contentRect, new Vector2(Math.Max(1, GetThemeFontSize("font_size")), _rowHeight));
        state.Offset.Y = (float)(_scrollVertical * _rowHeight); state.Maximum.Y *= _rowHeight;
        return state;
    }
    internal void AccessibilitySetScrollOffset(Vector2 offset)
    {
        ScrollHorizontal = AccessibilityServer.ScrollInteger(offset.X);
        ScrollVertical = offset.Y / Math.Max(1, _rowHeight);
    }
    internal void AccessibilityScrollBy(int horizontal, int vertical, AccessibilityServer.AccessibilityScrollUnit unit)
    {
        EnsureTextLayout();
        if (horizontal != 0) RequestHorizontalScroll(AccessibilityServer.ScrollInteger(_horizontalTarget + horizontal * (unit == AccessibilityServer.AccessibilityScrollUnit.Page ? _contentRect.Size.X : Math.Max(1, GetThemeFontSize("font_size")))));
        if (vertical != 0) RequestVerticalScroll(_verticalTarget + vertical * (unit == AccessibilityServer.AccessibilityScrollUnit.Page ? _contentRect.Size.Y / Math.Max(1, _rowHeight) : 1));
    }
}
