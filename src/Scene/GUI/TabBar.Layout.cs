namespace Electron2D;

public partial class TabBar
{
    private bool _dirty = true, _building, _buttonsVisible, _missingRight;
    private float _lastSizeX = -1, _lastSizeY = -1, _occupiedWidth;
    private Vector2 _minimum;
    private Font? _font;
    private int _fontSize, _separation, _tabSeparation, _outline;
    private StyleBox _selected = null!, _unselected = null!, _hovered = null!, _disabled = null!, _focus = null!, _buttonHighlight = null!, _buttonPressed = null!;
    private Texture _close = null!, _decrement = null!, _increment = null!, _decrementHighlight = null!, _incrementHighlight = null!, _dropMark = null!;
    private Rect2 _backRect, _nextRect;
    internal override Rect2? CanvasClipRect => _clipTabs ? new Rect2(Vector2.Zero, Size) : base.CanvasClipRect;
    private void InvalidateTabs() { _dirty = true; QueueRedraw(); UpdateMinimumSize(); }
    private bool CloseVisible(int index) => _closePolicy == CloseButtonDisplayPolicy.ShowAlways || _closePolicy == CloseButtonDisplayPolicy.ShowActiveOnly && index == _current;
    private void ReadTheme()
    {
        _font = GetThemeFont("font") ?? throw new InvalidOperationException("Tabs require a live font.");
        _fontSize = GetThemeFontSize("font_size"); _separation = GetThemeConstant("h_separation"); _tabSeparation = GetThemeConstant("tab_separation"); _outline = GetThemeConstant("outline_size");
        _selected = GetThemeStyleBox("tab_selected")!; _unselected = GetThemeStyleBox("tab_unselected")!; _hovered = GetThemeStyleBox("tab_hovered")!; _disabled = GetThemeStyleBox("tab_disabled")!; _focus = GetThemeStyleBox("tab_focus")!;
        _buttonHighlight = GetThemeStyleBox("button_highlight")!; _buttonPressed = GetThemeStyleBox("button_pressed")!;
        _close = GetThemeIcon("close")!; _decrement = GetThemeIcon("decrement")!; _increment = GetThemeIcon("increment")!;
        _decrementHighlight = GetThemeIcon("decrement_highlight")!; _incrementHighlight = GetThemeIcon("increment_highlight")!; _dropMark = GetThemeIcon("drop_mark")!;
    }
    private StyleBox MeasureStyle(int index) => _tabs[index].Disabled ? _disabled : index == _current ? _selected : _hovered.GetMinimumSize().X > _unselected.GetMinimumSize().X ? _hovered : _unselected;
    private Vector2 IconSize(Tab tab)
    {
        if (tab.Icon is null) return Vector2.Zero;
        var size = tab.Icon.GetSize(); var cap = tab.IconMaxWidth > 0 ? tab.IconMaxWidth : GetThemeConstant("icon_max_width");
        return cap > 0 && size.X > cap ? size * (cap / size.X) : size;
    }
    private float TabWidth(int index, float textWidth)
    {
        var tab = _tabs[index]; var style = MeasureStyle(index); var width = style.GetMinimumSize().X;
        if (tab.Icon is not null) width += tab.IconSize.X + _separation;
        if (tab.Title.Length != 0) width += textWidth + _separation;
        var close = CloseVisible(index);
        if (tab.ButtonIcon is { } button) width += close ? _buttonHighlight.GetMinimumSize().X + button.GetWidth() : _buttonHighlight.GetMargin(Side.Left) + button.GetWidth() + _separation;
        if (close) width += _buttonHighlight.GetMargin(Side.Left) + _close.GetWidth() + _separation;
        if (width > style.GetMinimumSize().X) width -= _separation;
        return width;
    }
    private void RebuildTabs(bool revealSelection = true)
    {
        ThrowIfDisposed(); Tree?.EnsureOwnerThread();
        if (_building) return;
        if (!_dirty && _lastSizeX == Size.X && _lastSizeY == Size.Y) return;
        _building = true;
        try
        {
            ReadTheme(); var marginY = Math.Max(Math.Max(_selected.GetMinimumSize().Y, _unselected.GetMinimumSize().Y), Math.Max(_hovered.GetMinimumSize().Y, _disabled.GetMinimumSize().Y));
            var maximum = GetCombinedMaximumSize().X; var arrows = _decrement.GetWidth() + _increment.GetWidth();
            var cap = maximum >= 0 ? maximum - arrows : float.PositiveInfinity; if (_maxTabWidth > 0) cap = Math.Min(cap, _maxTabWidth);
            var widest = 0f; var height = 0f; var total = 0f; var visibleCount = 0;
            for (var i = 0; i < _tabs.Count; i++)
            {
                var tab = _tabs[i]; tab.Rect = tab.ButtonRect = tab.CloseRect = default; tab.Offset = 0; tab.IconSize = IconSize(tab);
                var direction = tab.Direction == TextDirection.Inherited ? IsLayoutRTL() ? TextDirection.RTL : TextDirection.LTR : tab.Direction;
                var language = tab.Language; if (language.Length == 0) language = TranslationServer.GetOrAddDomain(TranslationDomain).LocaleOverride; if (language.Length == 0) language = TranslationServer.Culture.Name;
                if (tab.Title.Length != 0)
                {
                    var text = Atr(tab.Title); var key = new TextLayoutKey(text, _fontSize, -1, HorizontalAlignment.Left, 1, TextLineBreakFlags.None, TextJustificationFlags.None, direction, TextOrientation.Horizontal, false);
                    tab.Intrinsic.Build(_font!, key, new TextLayoutOptions(Language: language));
                    var natural = MathF.Ceiling(tab.Intrinsic.Size.X); var naturalWidth = TabWidth(i, natural); var textless = naturalWidth - natural;
                    tab.TextWidth = cap > 0 && naturalWidth > cap ? Math.Max(1, cap - textless) : natural;
                    tab.Layout.Build(_font!, key with { Width = tab.TextWidth }, new TextLayoutOptions(Language: language, Overrun: (int)TextOverrunBehavior.TrimEllipsis));
                }
                else tab.TextWidth = 0;
                tab.Width = TabWidth(i, tab.TextWidth);
                if (!float.IsFinite(tab.Width)) throw new InvalidOperationException("Tab geometry must be finite.");
                if (tab.Hidden) continue;
                widest = Math.Max(widest, tab.Width); if (visibleCount++ != 0) total += _tabSeparation; total += tab.Width;
                height = Math.Max(height, Math.Max(tab.Title.Length == 0 ? 0 : tab.Layout.Size.Y, tab.IconSize.Y) + marginY);
                if (tab.ButtonIcon is { } button) height = Math.Max(height, button.GetHeight() + marginY);
                if (CloseVisible(i)) height = Math.Max(height, _close.GetHeight() + marginY);
            }
            if (!float.IsFinite(total)) throw new InvalidOperationException("Tab strip width overflows.");
            var minWidth = _clipTabs ? widest + (_tabs.Count > 1 ? arrows : 0) : total;
            if (maximum >= 0) minWidth = Math.Min(minWidth, maximum);
            _minimum = visibleCount == 0 ? Vector2.Zero : new(minWidth, height);
            if (!_clipTabs || total <= Size.X) _offset = 0;
            var start = NextVisible(_offset - 1, 1); if (start == -1) { start = NextVisible(_tabs.Count, -1); _offset = Math.Max(0, start); }
            if (_clipTabs && start >= 0)
            {
                // Backfill earlier visible tabs after removal or growth, without scrolling beyond a full trailing page.
                while (start > 0)
                {
                    var before = NextVisible(start, -1); if (before < 0 || WidthFrom(before) > Size.X - arrows) break; start = before;
                }
                _offset = start;
            }
            PlaceTabs();
            if (revealSelection && _scrollToSelected && (uint)_current < (uint)_tabs.Count && !_tabs[_current].Hidden && _buttonsVisible && (_current < _offset || _current > _lastDrawn)) RevealCore(_current);
            _dirty = false; _lastSizeX = Size.X; _lastSizeY = Size.Y;
        }
        finally { _building = false; }
    }
    private int NextVisible(int from, int direction) { for (var i = from + direction; (uint)i < (uint)_tabs.Count; i += direction) if (!_tabs[i].Hidden) return i; return -1; }
    private float WidthFrom(int index) { var width = 0f; var count = 0; for (var i = index; i < _tabs.Count; i++) if (!_tabs[i].Hidden) { if (count++ > 0) width += _tabSeparation; width += _tabs[i].Width; } return width; }
    private void PlaceTabs()
    {
        foreach (var tab in _tabs) { tab.Offset = 0; tab.Rect = tab.ButtonRect = tab.CloseRect = default; }
        var arrows = _decrement.GetWidth() + _increment.GetWidth(); var limit = Size.X;
        var rest = WidthFrom(_offset); _buttonsVisible = _clipTabs && (_offset > 0 || rest > limit); var available = _buttonsVisible ? Math.Max(0, limit - arrows) : limit;
        var width = 0f; _lastDrawn = -1; var count = 0;
        for (var i = _offset; i < _tabs.Count; i++)
        {
            var tab = _tabs[i]; if (tab.Hidden) continue; var proposed = width + (count > 0 ? _tabSeparation : 0) + tab.Width;
            if (_clipTabs && count > 0 && proposed > available) break;
            if (count++ > 0) width += _tabSeparation; tab.Offset = width; width += tab.Width; _lastDrawn = i;
        }
        _missingRight = NextVisible(_lastDrawn, 1) != -1; if (_lastDrawn == -1) _missingRight = false;
        _occupiedWidth = width; var shift = _tabAlignment == AlignmentMode.Center ? (available - width) / 2 : _tabAlignment == AlignmentMode.Right ? available - width : 0;
        for (var i = _offset; i <= _lastDrawn; i++) if (!_tabs[i].Hidden)
            {
                var tab = _tabs[i]; tab.Offset += shift; tab.Rect = new(IsLayoutRTL() ? Size.X - tab.Offset - tab.Width : tab.Offset, 0, tab.Width, Size.Y); PlaceContent(i);
            }
        if (IsLayoutRTL()) { _nextRect = new(0, 0, _decrement.GetWidth(), Size.Y); _backRect = new(_decrement.GetWidth(), 0, _increment.GetWidth(), Size.Y); }
        else { _backRect = new(Size.X - arrows, 0, _decrement.GetWidth(), Size.Y); _nextRect = new(Size.X - _increment.GetWidth(), 0, _increment.GetWidth(), Size.Y); }
    }
    private void PlaceContent(int index)
    {
        var tab = _tabs[index]; var style = DrawStyle(index); var rtl = IsLayoutRTL(); var x = rtl ? tab.Rect.End.X - style.GetMargin(Side.Left) : tab.Rect.Position.X + style.GetMargin(Side.Left);
        if (tab.Icon is not null) x += (rtl ? -1 : 1) * (tab.IconSize.X + _separation);
        if (tab.Title.Length != 0) x += (rtl ? -1 : 1) * (tab.TextWidth + _separation);
        var buttonSize = _buttonHighlight.GetMinimumSize(); var top = style.GetMargin(Side.Top); var contentHeight = Size.Y - style.GetMinimumSize().Y;
        if (tab.ButtonIcon is { } button) { var size = buttonSize + button.GetSize(); tab.ButtonRect = new(rtl ? x - size.X : x, top + (contentHeight - size.Y) / 2, size.X, size.Y); x = rtl ? tab.ButtonRect.Position.X : tab.ButtonRect.End.X; }
        if (CloseVisible(index)) { var size = buttonSize + _close.GetSize(); tab.CloseRect = new(rtl ? x - size.X : x, top + (contentHeight - size.Y) / 2, size.X, size.Y); }
    }
    private StyleBox DrawStyle(int index) => _tabs[index].Disabled ? _disabled : index == _current ? _selected : index == _hover ? _hovered : _unselected;
    /// <summary>Gets the current first visible scroll index.</summary><returns>Zero initially.</returns>
    public int GetTabOffset() { RebuildTabs(); return _offset; }
    /// <summary>Reports whether overflow navigation arrows are visible.</summary><returns>True for a clipped strip with tabs outside its current page.</returns>
    public bool GetOffsetButtonsVisible() { RebuildTabs(); return _buttonsVisible; }
    /// <summary>Gets a tab's current local display rectangle, mirrored in RTL.</summary><param name="tabIndex">A valid zero-based index.</param><returns>Its measured rectangle; hidden or off-page tabs return their measured width at the zero cached offset.</returns>
    public Rect2 GetTabRect(int tabIndex) { ThrowIfDisposed(); Index(tabIndex); RebuildTabs(); var tab = _tabs[tabIndex]; return new(IsLayoutRTL() ? Size.X - tab.Offset - tab.Width : tab.Offset, 0, tab.Width, Size.Y); }
    /// <summary>Finds the last displayed visible tab containing a finite local point.</summary><param name="point">Local coordinates.</param><returns>Minus one outside displayed tabs or inside an arrow area.</returns><exception cref="ArgumentException">The point is nonfinite.</exception>
    public int GetTabIdxAtPoint(Vector2 point)
    {
        ThrowIfDisposed(); if (!point.IsFinite()) throw new ArgumentException("Tab point must be finite.", nameof(point)); RebuildTabs();
        if (_buttonsVisible && (_backRect.HasPoint(point) || _nextRect.HasPoint(point))) return -1;
        var result = -1; for (var i = _offset; i <= _lastDrawn; i++) if (!_tabs[i].Hidden && _tabs[i].Rect.HasPoint(point)) result = i; return result;
    }
    /// <summary>Scrolls a nonhidden tab into view when this strip is attached and clipped.</summary><param name="tabIndex">Minus one is ignored; otherwise a valid index.</param>
    /// <remarks>Does not change selection. Detached calls and already visible tabs do not change the offset.</remarks>
    public void EnsureTabVisible(int tabIndex)
    {
        EnsureMutable(); if (tabIndex == -1) return; Index(tabIndex); RebuildTabs(false); if (Tree is null || !_clipTabs || !_buttonsVisible || _tabs[tabIndex].Hidden || tabIndex >= _offset && tabIndex <= _lastDrawn) return;
        RevealCore(tabIndex); QueueRedraw();
    }
    private void RevealCore(int index)
    {
        if (index < _offset) _offset = index;
        else
        {
            var available = Size.X - _decrement.GetWidth() - _increment.GetWidth(); var width = 0f; var start = index;
            for (var i = index; i >= _offset; i--) if (!_tabs[i].Hidden) { var proposed = width + (width != 0 ? _tabSeparation : 0) + _tabs[i].Width; if (i != index && proposed > available) break; width = proposed; start = i; }
            _offset = start;
        }
        PlaceTabs();
    }
    /// <inheritdoc />
    protected override Vector2 OnGetMinimumSize() { RebuildTabs(); return _minimum; }
    internal override Vector2 GetDesiredSize() { RebuildTabs(); var maximum = GetCombinedMaximumSize().X; return _clipTabs && maximum >= 0 ? new(Math.Min(maximum, _occupiedWidth + (_buttonsVisible ? _decrement.GetWidth() + _increment.GetWidth() : 0)), 0) : Vector2.Zero; }
    /// <inheritdoc />
    protected override void OnNotification(int what)
    {
        base.OnNotification(what);
        if (what == NotificationEnterTree) { _initialized = true; _dirty = true; }
        else if (what is NotificationThemeChanged or NotificationTranslationChanged or NotificationLayoutDirectionChanged or NotificationResized) { _dirty = true; QueueRedraw(); if (what != NotificationResized) UpdateMinimumSize(); }
        else if (what == NotificationDraw) DrawTabs();
        else if (what == NotificationDragEnd) { _dropVisible = false; _hoverTimer.Stop(); QueueRedraw(); }
        else if (what == NotificationInternalProcess) RepeatGamepad();
        else if (what == NotificationExitTree) { ResetInteraction(); SetInternalProcessing(false, false); }
    }
    private void DrawTabs()
    {
        RebuildTabs(); for (var i = _offset; i <= _lastDrawn; i++) if (i != _current && !_tabs[i].Hidden) DrawTab(i);
        if ((uint)_current < (uint)_tabs.Count && _current >= _offset && _current <= _lastDrawn && !_tabs[_current].Hidden) DrawTab(_current);
        if (_buttonsVisible)
        {
            var rtl = IsLayoutRTL(); var back = rtl ? _increment : _decrement; var next = rtl ? _decrement : _increment;
            DrawTextureRect(_arrowHover == -1 && _offset > 0 ? rtl ? _incrementHighlight : _decrementHighlight : back, new(_backRect.Position.X, (Size.Y - back.GetHeight()) / 2, back.GetWidth(), back.GetHeight()), false, new(1, 1, 1, _offset > 0 ? 1 : .5f));
            DrawTextureRect(_arrowHover == 1 && _missingRight ? rtl ? _decrementHighlight : _incrementHighlight : next, new(_nextRect.Position.X, (Size.Y - next.GetHeight()) / 2, next.GetWidth(), next.GetHeight()), false, new(1, 1, 1, _missingRight ? 1 : .5f));
        }
        if (_dropVisible) DrawTextureRect(_dropMark, new(_dropX - _dropMark.GetWidth() / 2f, (Size.Y - _dropMark.GetHeight()) / 2, _dropMark.GetWidth(), _dropMark.GetHeight()), false, GetThemeColor("drop_mark_color"));
    }
    private void DrawTab(int index)
    {
        var tab = _tabs[index]; var style = DrawStyle(index); DrawStyleBox(style, tab.Rect); if (index == _current && HasFocus()) DrawStyleBox(_focus, tab.Rect);
        var state = index == _current ? "selected" : tab.Disabled ? "disabled" : index == _hover ? "hovered" : "unselected";
        var fontColor = GetThemeColor(state switch { "selected" => "font_selected_color", "disabled" => "font_disabled_color", "hovered" => "font_hovered_color", _ => "font_unselected_color" });
        var iconColor = GetThemeColor(state switch { "selected" => "icon_selected_color", "disabled" => "icon_disabled_color", "hovered" => "icon_hovered_color", _ => "icon_unselected_color" });
        var rtl = IsLayoutRTL(); var x = rtl ? tab.Rect.End.X - style.GetMargin(Side.Left) : tab.Rect.Position.X + style.GetMargin(Side.Left); var top = style.GetMargin(Side.Top); var height = Size.Y - style.GetMinimumSize().Y;
        if (tab.Icon is { } icon) { DrawTextureRect(icon, new(rtl ? x - tab.IconSize.X : x, top + (height - tab.IconSize.Y) / 2, tab.IconSize.X, tab.IconSize.Y), false, iconColor); x += (rtl ? -1 : 1) * (tab.IconSize.X + _separation); }
        if (tab.Title.Length != 0)
        {
            var baseline = new Vector2(rtl ? x - tab.TextWidth : x, top + (height - tab.Layout.Size.Y) / 2 + tab.Layout.FirstAscent);
            var outlineColor = GetThemeColor("font_outline_color"); if (_outline > 0 && outlineColor.A > 0) tab.Layout.Draw(this, baseline, outlineColor, _outline, outlinePass: true); tab.Layout.Draw(this, baseline, fontColor);
        }
        if (tab.ButtonIcon is { } button) DrawButton(button, tab.ButtonRect, ReferenceEquals(_pressedTab, tab) && !_pressedClose, _buttonHover == index);
        if (CloseVisible(index)) DrawButton(_close, tab.CloseRect, ReferenceEquals(_pressedTab, tab) && _pressedClose, _closeHover == index && !tab.Disabled);
    }
    private void DrawButton(Texture texture, Rect2 rect, bool pressed, bool hovered)
    {
        if (hovered) DrawStyleBox(pressed ? _buttonPressed : _buttonHighlight, rect);
        DrawTextureRect(texture, new(rect.Position + _buttonHighlight.GetOffset(), texture.GetSize()), false);
    }
    /// <inheritdoc />
    protected override string OnGetTooltip(Vector2 atPosition)
    {
        var index = GetTabIdxAtPoint(atPosition); if (index == -1) return base.OnGetTooltip(atPosition); var tab = _tabs[index]; return tab.Tooltip.Length != 0 ? tab.Tooltip : tab.Title.Length != 0 && tab.TextWidth < tab.Intrinsic.Size.X ? tab.Title : "";
    }
}
