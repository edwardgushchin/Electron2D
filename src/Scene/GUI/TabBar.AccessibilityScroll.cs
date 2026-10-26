namespace Electron2D;

public partial class TabBar
{
    private ulong _accessibilityScrollVersion, _accessibilityScrollCachedVersion = ulong.MaxValue;
    private bool _accessibilityScrollEnabled;
    private (Vector2 Offset, Vector2 Minimum, Vector2 Maximum, Vector2 Step, Vector2 Page, Rect2 View, byte Axes, byte Ranges) _accessibilityScrollMetrics;
    internal (Vector2 Offset, Vector2 Minimum, Vector2 Maximum, Vector2 Step, Vector2 Page, Rect2 View, byte Axes, byte Ranges) AccessibilityScrollMetrics()
    {
        RebuildTabs(); var enabled = _scrollingEnabled && _clipTabs;
        if (_accessibilityScrollCachedVersion == _accessibilityScrollVersion && _accessibilityScrollEnabled == enabled) return _accessibilityScrollMetrics;
        var arrows = _buttonsVisible ? _decrement.GetWidth() + _increment.GetWidth() : 0;
        var page = Math.Max(0, Size.X - arrows); var view = new Rect2(IsLayoutRTL() ? arrows : 0, 0, page, Size.Y);
        var last = _tabs.Count; while (last > 0 && _tabs[last - 1].Hidden) last--;
        var end = last - 1; var width = 0f;
        for (var i = last - 1; i >= 0; i--)
        {
            if (_tabs[i].Hidden) continue;
            var next = width + (width == 0 ? 0 : _tabSeparation) + _tabs[i].Width;
            if (width > 0 && next > page) break; width = next; end = i;
        }
        var prefix = 0f; var position = 0f; var max = 0f; var step = 1f; var count = 0;
        for (var i = 0; i < _tabs.Count; i++)
        {
            if (_tabs[i].Hidden) continue; if (count++ > 0) prefix += _tabSeparation;
            if (i == _offset) { position = prefix; step = Math.Max(1, _tabs[i].Width + _tabSeparation); }
            if (i == end) max = prefix; prefix += _tabs[i].Width;
        }
        max = enabled ? Math.Max(max, position) : position;
        var offset = new Vector2(IsLayoutRTL() ? -position : position, 0);
        var min = new Vector2(IsLayoutRTL() ? -max : 0, 0); var maximum = new Vector2(IsLayoutRTL() ? 0 : max, 0);
        _accessibilityScrollMetrics = (offset, min, maximum, new Vector2(step, 1), view.Size, view, (byte)(enabled && max > 0 ? 1 : 0), 4);
        _accessibilityScrollCachedVersion = _accessibilityScrollVersion; _accessibilityScrollEnabled = enabled;
        return _accessibilityScrollMetrics;
    }
    internal void AccessibilitySetScrollOffset(Vector2 offset)
    {
        var state = AccessibilityScrollMetrics(); if (!_scrollingEnabled || !_clipTabs) return;
        var wanted = Math.Clamp(offset.X, state.Minimum.X, state.Maximum.X); if (IsLayoutRTL()) wanted = -wanted;
        var prefix = 0f; var selected = -1; var count = 0; var distance = float.PositiveInfinity;
        var maximum = IsLayoutRTL() ? -state.Minimum.X : state.Maximum.X;
        for (var i = 0; i < _tabs.Count; i++)
        {
            if (_tabs[i].Hidden) continue; if (count++ > 0) prefix += _tabSeparation;
            if (prefix > maximum) break;
            var candidate = Math.Abs(prefix - wanted); if (candidate < distance) { distance = candidate; selected = i; }
            prefix += _tabs[i].Width;
        }
        if (selected >= 0 && selected != _offset) { _offset = selected; PlaceTabs(); QueueRedraw(); }
    }
    internal void AccessibilityScrollBy(int horizontal, AccessibilityServer.AccessibilityScrollUnit unit)
    {
        var state = AccessibilityScrollMetrics(); if (!_scrollingEnabled || !_clipTabs || horizontal == 0) return;
        if (unit == AccessibilityServer.AccessibilityScrollUnit.Page)
        {
            var step = Math.Max(state.Page.X, state.Step.X);
            AccessibilitySetScrollOffset(new Vector2(state.Offset.X + horizontal * step, 0)); return;
        }
        var direction = IsLayoutRTL() ? -horizontal : horizontal; var next = NextVisible(_offset, direction);
        if (next < 0 || direction > 0 && !_missingRight) return;
        _offset = next; PlaceTabs(); QueueRedraw();
    }
}
