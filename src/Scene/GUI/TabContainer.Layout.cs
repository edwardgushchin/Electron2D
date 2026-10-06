namespace Electron2D;

public partial class TabContainer
{
    private bool _themeDirty = true, _themeUpdating;
    private StyleBox? _panelStyle, _headerStyle;
    private float _headerHeight;
    private void ApplyTheme()
    {
        if (!_themeDirty || _themeUpdating || _bar is null) return; _themeUpdating = true;
        try
        {
            _bar.BeginBulkThemeOverride();
            try
            {
                foreach (var name in new[] { "tab_unselected", "tab_selected", "tab_hovered", "tab_disabled", "tab_focus" }) _bar.AddThemeStyleBoxOverride(name, GetThemeStyleBox(name)!);
                foreach (var name in new[] { "decrement", "decrement_highlight", "increment", "increment_highlight", "drop_mark" }) _bar.AddThemeIconOverride(name, GetThemeIcon(name)!);
                foreach (var name in new[] { "font_selected_color", "font_hovered_color", "font_unselected_color", "font_disabled_color", "font_outline_color", "icon_selected_color", "icon_hovered_color", "icon_unselected_color", "icon_disabled_color", "drop_mark_color" }) _bar.AddThemeColorOverride(name, GetThemeColor(name));
                _bar.AddThemeFontOverride("font", GetThemeFont("font")!); _bar.AddThemeFontSizeOverride("font_size", GetThemeFontSize("font_size"));
                foreach (var name in new[] { "icon_max_width", "outline_size", "tab_separation" }) _bar.AddThemeConstantOverride(name, GetThemeConstant(name));
                _bar.AddThemeConstantOverride("h_separation", GetThemeConstant("icon_separation"));
            }
            finally { _bar.EndBulkThemeOverride(); }
            _panelStyle = GetThemeStyleBox("panel"); _headerStyle = GetThemeStyleBox("tabbar_background"); PopupHover(false); _themeDirty = false;
        }
        finally { _themeUpdating = false; }
    }
    private Vector2? PageMaximum(float header, Vector2 margins)
    { if (!PropagateMaximumSize) return null; var max = GetCombinedMaximumSize(); return new Vector2(max.X < 0 ? -1 : MathF.Max(0, max.X - margins.X), max.Y < 0 ? -1 : MathF.Max(0, max.Y - margins.Y - header)); }
    private Vector2 MeasurePages(bool desired)
    {
        if (_bar is null) return Vector2.Zero; ApplyTheme(); var panel = _panelStyle?.GetMinimumSize() ?? Vector2.Zero; var headerMargins = _headerStyle?.GetMinimumSize() ?? Vector2.Zero;
        var header = _tabsVisible ? (desired ? _bar.GetBoundDesiredSize() : _bar.GetMinimumSize()) + headerMargins : Vector2.Zero;
        if (_tabsVisible) { if (GetPopup() != null) header.X += _popupButton.GetCombinedMinimumSize().X; if (_bar.TabAlignment != TabBar.AlignmentMode.Center && (_bar.TabAlignment != TabBar.AlignmentMode.Right || GetPopup() == null)) header.X += GetThemeConstant("side_margin"); }
        var pages = Vector2.Zero; var max = PageMaximum(header.Y, panel);
        for (var i = 0; i < _pages.Count; i++) { var child = _pages[i].Control; if (child.IsDisposed || child.Parent != this || child.TopLevel || !child.Visible && !_useHidden) continue; child.ContainerMaximum = max; pages = pages.Max(desired ? child.GetBoundDesiredSize() : child.GetBoundMinimumSize()); }
        return new(MathF.Max(header.X, pages.X + panel.X), header.Y + pages.Y + panel.Y);
    }
    /// <inheritdoc />
    protected override Vector2 OnGetMinimumSize() => MeasurePages(false);
    internal override Vector2 GetDesiredSize() => MeasurePages(true);
    private void Paint()
    {
        if (_disposing || IsDisposed || _bar is null) return; if (_painting) { _paintAgain = true; return; }
        _painting = true; List<Exception>? errors = null;
        try
        {
            for (var pass = 0; pass < 64; pass++)
            {
                _paintAgain = false; ApplyTheme(); _paintPages.Clear(); foreach (var page in _pages) _paintPages.Add(page.Control); var current = _bar.CurrentTab;
                _visibility = true;
                try { for (var i = 0; i < _paintPages.Count; i++) { var c = _paintPages[i]; if (c.IsDisposed || c.Parent != this || c.TopLevel) continue; try { c.Visible = i == current; } catch (Exception e) { CollectException(ref errors, e); } } }
                finally { _visibility = false; }
                ArrangeHeader(); var panelOffset = _panelStyle?.GetOffset() ?? Vector2.Zero; var panelMargins = _panelStyle?.GetMinimumSize() ?? Vector2.Zero;
                var offset = panelOffset + new Vector2(0, _position == TabPosition.Top ? _headerHeight : 0); var available = (Size - panelMargins - new Vector2(0, _headerHeight)).Max(Vector2.Zero); var maximum = PageMaximum(_headerHeight, panelMargins);
                if (IsInsideTree) for (var i = 0; i < _paintPages.Count; i++) { var c = _paintPages[i]; if (c.IsDisposed || c.Parent != this || c.TopLevel || !c.Visible) continue; c.ContainerMaximum = maximum; try { FitChildInRect(c, new(offset, available)); } catch (Exception e) { CollectException(ref errors, e); } }
                UpdateMinimumSize(); QueueRedraw(); if (!_paintAgain) break; if (pass == 63) throw new InvalidOperationException("Tab page layout did not settle.");
            }
        }
        finally { _paintPages.Clear(); _painting = false; }
        ThrowCollected("Tab page layout callbacks failed.", errors);
    }
    private void ArrangeHeader()
    {
        var margins = _headerStyle?.GetMinimumSize() ?? Vector2.Zero; var offset = _headerStyle?.GetOffset() ?? Vector2.Zero;
        _headerHeight = _tabsVisible && _pages.Count > 0 ? _bar.GetMinimumSize().Y + margins.Y : 0; var y = _position == TabPosition.Bottom ? Size.Y - _headerHeight : 0; var rtl = IsLayoutRTL();
        var buttonWidth = _tabsVisible && GetPopup() != null ? _popupButton.GetCombinedMinimumSize().X : 0;
        var side = _pages.Count == 0 || _bar.TabAlignment == TabBar.AlignmentMode.Center || _bar.TabAlignment == TabBar.AlignmentMode.Right && buttonWidth > 0 ? 0 : GetThemeConstant("side_margin");
        var left = rtl ? margins.X - offset.X : offset.X; var right = margins.X - left;
        if (_bar.TabAlignment == TabBar.AlignmentMode.Left) { if (rtl) right += side; else left += side; }
        else if (_bar.TabAlignment == TabBar.AlignmentMode.Right && !_bar.GetOffsetButtonsVisible()) { if (rtl) left += side; else right += side; }
        _bar.Position = new(left + (rtl ? buttonWidth : 0), y + offset.Y); _bar.Size = new(MathF.Max(0, Size.X - left - right - buttonWidth), MathF.Max(0, _headerHeight - margins.Y));
        _popupButton.Position = new(rtl ? left : Size.X - right - buttonWidth, y + offset.Y); _popupButton.Size = new(buttonWidth, MathF.Max(0, _headerHeight - margins.Y));
    }
    /// <inheritdoc />
    protected override void OnNotification(int what)
    {
        base.OnNotification(what);
        if (what == NotificationEnterTree) { SyncPages(); for (var i = 0; i < _pages.Count; i++) _bar.SetTabTitle(i, _pages[i].Title ?? _pages[i].Control.Name); if (_setupCurrent >= -1) { var current = _setupCurrent; _setupCurrent = -2; _bar.CurrentTab = current; } Paint(); }
        else if (what == NotificationSortChildren || what == NotificationVisibilityChanged && IsVisibleInTree) Paint();
        else if (what is NotificationThemeChanged or NotificationLayoutDirectionChanged or NotificationTranslationChanged) { _themeDirty = true; LayoutChanged(); }
        else if (what == NotificationDraw)
        {
            ApplyTheme(); var header = _tabsVisible ? _headerHeight : 0; var y = _position == TabPosition.Bottom ? Size.Y - header : 0;
            if (_tabsVisible && _headerStyle is not null) DrawStyleBox(_headerStyle, new(0, y, Size.X, header));
            if (_panelStyle is not null) DrawStyleBox(_panelStyle, new(0, _position == TabPosition.Top ? header : 0, Size.X, MathF.Max(0, Size.Y - header)));
        }
    }
    /// <inheritdoc />
    protected override SizeFlags[] GetAllowedSizeFlagsHorizontal() => [];
    /// <inheritdoc />
    protected override SizeFlags[] GetAllowedSizeFlagsVertical() => [];
}
