namespace Electron2D;

public partial class ItemList
{
    internal (Vector2 Offset, Vector2 Minimum, Vector2 Maximum, Vector2 Step, Vector2 Page, Rect2 View, byte Axes, byte Ranges) AccessibilityScrollMetrics()
    {
        RebuildListLayout(); var view = new Rect2(_hBar.Position.X, _vBar.Position.Y, (float)_hBar.Page, (float)_vBar.Page);
        var step = _items.Count == 0 ? Vector2.One : _items[0].Rect.Size.Max(Vector2.One);
        if (_items.Count > _currentColumns) step.Y = Math.Max(1, _items[_currentColumns].Rect.Position.Y - _items[0].Rect.Position.Y);
        return AccessibilityServer.ScrollFromBars(_hBar, _vBar, view, step);
    }
    internal void AccessibilitySetScrollOffset(Vector2 offset)
    {
        _hBar.Value = offset.X; _vBar.Value = offset.Y;
    }
}
