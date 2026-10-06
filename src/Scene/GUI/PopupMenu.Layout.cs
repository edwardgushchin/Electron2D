namespace Electron2D;

public partial class PopupMenu
{
    private bool _allowSearch = true, _hideItems = true, _hideChecks = true, _hideStates, _shrinkWidth = true, _shrinkHeight = true, _searchEnabled, _fuzzy = true;
    private int _searchMinimum, _misses = 2;
    private double _submenuDelay = .2;
    /// <summary>Gets or sets the AllowSearch policy.</summary><value>true initially.</value>
    public bool AllowSearch { get { CheckMenu(); return _allowSearch; } set { EnsureMutable(); if (_allowSearch == value) return; _allowSearch = value; _dirty = true; Arrange(); } }
    /// <summary>Gets or sets the HideOnItemSelection policy.</summary><value>true initially.</value>
    public bool HideOnItemSelection { get { CheckMenu(); return _hideItems; } set { EnsureMutable(); if (_hideItems == value) return; _hideItems = value; _dirty = true; Arrange(); } }
    /// <summary>Gets or sets the HideOnCheckableItemSelection policy.</summary><value>true initially.</value>
    public bool HideOnCheckableItemSelection { get { CheckMenu(); return _hideChecks; } set { EnsureMutable(); if (_hideChecks == value) return; _hideChecks = value; _dirty = true; Arrange(); } }
    /// <summary>Gets or sets the HideOnStateItemSelection policy.</summary><value>false initially.</value>
    public bool HideOnStateItemSelection { get { CheckMenu(); return _hideStates; } set { EnsureMutable(); if (_hideStates == value) return; _hideStates = value; _dirty = true; Arrange(); } }
    /// <summary>Gets or sets the ShrinkWidth policy.</summary><value>true initially.</value>
    public bool ShrinkWidth { get { CheckMenu(); return _shrinkWidth; } set { EnsureMutable(); if (_shrinkWidth == value) return; _shrinkWidth = value; _dirty = true; Arrange(); } }
    /// <summary>Gets or sets the ShrinkHeight policy.</summary><value>true initially.</value>
    public bool ShrinkHeight { get { CheckMenu(); return _shrinkHeight; } set { EnsureMutable(); if (_shrinkHeight == value) return; _shrinkHeight = value; _dirty = true; Arrange(); } }
    /// <summary>Gets or sets the SearchBarEnabled policy.</summary><value>false initially.</value>
    public bool SearchBarEnabled { get { CheckMenu(); return _searchEnabled; } set { EnsureMutable(); if (_searchEnabled == value) return; _searchEnabled = value; _dirty = true; Arrange(); } }
    /// <summary>Gets or sets the SearchBarFuzzySearchEnabled policy.</summary><value>true initially.</value>
    public bool SearchBarFuzzySearchEnabled { get { CheckMenu(); return _fuzzy; } set { EnsureMutable(); if (_fuzzy == value) return; _fuzzy = value; _dirty = true; FilterChanged(_search.Text); } }
    /// <summary>Gets or sets the nonnegative SearchBarMinItemCount threshold.</summary><value>0 initially.</value><exception cref="ArgumentOutOfRangeException">The value is negative.</exception>
    public int SearchBarMinItemCount { get { CheckMenu(); return _searchMinimum; } set { EnsureMutable(); ArgumentOutOfRangeException.ThrowIfNegative(value); if (_searchMinimum == value) return; _searchMinimum = value; _dirty = true; FilterChanged(_search.Text); } }
    /// <summary>Gets or sets the nonnegative SearchBarFuzzySearchMaxMisses threshold.</summary><value>2 initially.</value><exception cref="ArgumentOutOfRangeException">The value is negative.</exception>
    public int SearchBarFuzzySearchMaxMisses { get { CheckMenu(); return _misses; } set { EnsureMutable(); ArgumentOutOfRangeException.ThrowIfNegative(value); if (_misses == value) return; _misses = value; _dirty = true; FilterChanged(_search.Text); } }
    /// <summary>Gets or sets the finite nonnegative submenu hover delay.</summary><value>0.2 seconds initially.</value>
    public double SubmenuPopupDelay { get { CheckMenu(); return _submenuDelay; } set { EnsureMutable(); if (!double.IsFinite(value) || value < 0) throw new ArgumentOutOfRangeException(nameof(value)); _submenuDelay = value; if (value > 0) _submenuTimer.WaitTime = value; } }
    private Font _font = null!, _separatorFont = null!;
    private StyleBox _style = null!, _hoverStyle = null!, _separatorStyle = null!, _separatorLeft = null!, _separatorRight = null!;
    private int _fontSize, _separatorFontSize, _hsep, _vsep, _indentStep, _outline, _separatorOutline;
    private float _iconColumn, _checkColumn, _acceleratorColumn, _contentWidth, _totalHeight, _rowWidth;
    private Vector2 _minimum, _shadowStart, _shadowEnd;
    private Rect2i? _contentRect;
    private Vector2i _expandedSize;
    private sealed class MenuItems(PopupMenu menu) : Control
    {
        protected override Vector2 OnGetMinimumSize() { menu.Measure(); return new(menu._contentWidth, menu._totalHeight); }
        protected override void OnDraw() { base.OnDraw(); menu.DrawItems(this); }
        protected override void OnGUIInput(InputEvent input) { base.OnGUIInput(input); menu.ItemsInput(input); }
        protected override string OnGetTooltip(Vector2 atPosition) { var i = menu.ItemAt(atPosition.Y); return i < 0 ? "" : menu._items[i].Tooltip; }
    }
    private void Measure()
    {
        CheckMenu(); if (!_dirty) return;
        _font = GetThemeFont("font") ?? throw new InvalidOperationException("Menus require a font."); _separatorFont = GetThemeFont("font_separator") ?? _font;
        _fontSize = GetThemeFontSize("font_size"); _separatorFontSize = GetThemeFontSize("font_separator_size");
        _hsep = GetThemeConstant("h_separation"); _vsep = GetThemeConstant("v_separation"); _indentStep = GetThemeConstant("indent"); _outline = GetThemeConstant("outline_size"); _separatorOutline = GetThemeConstant("separator_outline_size");
        _style = GetThemeStyleBox("panel")!; _hoverStyle = GetThemeStyleBox("hover")!; _separatorStyle = GetThemeStyleBox("separator")!; _separatorLeft = GetThemeStyleBox("labeled_separator_left")!; _separatorRight = GetThemeStyleBox("labeled_separator_right")!;
        _iconColumn = _acceleratorColumn = _contentWidth = _totalHeight = 0; _checkColumn = 0; var hasCheck = false; var compact = GetThemeConstant("gutter_compact") != 0;
        foreach (var item in _items)
        {
            item.DisplayText = item.AutoTranslate == NodeAutoTranslateMode.Disabled ? item.Text : item.AutoTranslate == NodeAutoTranslateMode.Always ? Tr(item.Text) : Atr(item.Text);
            item.AcceleratorText = item.Shortcut is { IsDisposed: false } shortcut && shortcut.HasValidEvent() ? shortcut.GetAsText() : item.Accelerator == Key.None ? "" : AcceleratorText(item.Accelerator);
            var language = item.Language.Length == 0 ? TranslationServer.Culture.Name : item.Language; var direction = item.Direction == TextDirection.Inherited ? IsLayoutRTL() ? TextDirection.RTL : TextDirection.LTR : item.Direction;
            var font = item.Separator ? _separatorFont : _font; var size = item.Separator ? _separatorFontSize : _fontSize;
            item.TextLayout.Build(font, new(item.DisplayText, size, -1, HorizontalAlignment.Left, 1, TextLineBreakFlags.None, TextJustificationFlags.None, direction, TextOrientation.Horizontal, false), new TextLayoutOptions(Language: language));
            item.AcceleratorLayout.Build(_font, new(item.AcceleratorText, _fontSize, -1, HorizontalAlignment.Left, 1, TextLineBreakFlags.None, TextJustificationFlags.None, direction, TextOrientation.Horizontal, false), new TextLayoutOptions(Language: language));
            item.IconSize = item.Icon is { IsDisposed: false } liveIcon ? liveIcon.GetSize() : Vector2.Zero; var cap = GetThemeConstant("icon_max_width"); if (item.IconMaxWidth > 0 && (cap <= 0 || item.IconMaxWidth < cap)) cap = item.IconMaxWidth; if (cap > 0 && item.IconSize.X > cap) item.IconSize *= cap / item.IconSize.X;
            _iconColumn = Math.Max(_iconColumn, item.IconSize.X); _acceleratorColumn = Math.Max(_acceleratorColumn, item.AcceleratorLayout.Size.X);
            if (item.Checkable != 0 && !item.Separator) { hasCheck = true; if (item.Icon is not null) compact = false; }
            _contentWidth = Math.Max(_contentWidth, item.Indent * _indentStep + item.TextLayout.Size.X + (item.Submenu is null ? 0 : GetThemeIcon("submenu")!.GetWidth() + _hsep));
        }
        var checkWidth = Math.Max(GetThemeIcon("checked")!.GetWidth(), GetThemeIcon("radio_checked")!.GetWidth());
        if (compact) { _iconColumn = Math.Max(_iconColumn, checkWidth); _checkColumn = 0; } else _checkColumn = hasCheck ? checkWidth + _hsep : 0;
        _contentWidth += _checkColumn + (_iconColumn > 0 ? _iconColumn + _hsep : 0) + (_acceleratorColumn > 0 ? _acceleratorColumn + _hsep * 2 : 0) + GetThemeConstant("item_start_padding") + GetThemeConstant("item_end_padding");
        foreach (var item in _items)
        {
            item.Rect = default; if (!item.Visible) continue;
            var height = item.Separator ? Math.Max(item.TextLayout.Size.Y, Math.Max(_separatorStyle.GetMinimumSize().Y, Math.Max(_separatorLeft.GetMinimumSize().Y, _separatorRight.GetMinimumSize().Y))) : Math.Max(Math.Max(item.TextLayout.Size.Y, item.IconSize.Y), item.Checkable == 0 ? 0 : GetThemeIcon("checked")!.GetHeight());
            item.Rect = new(0, _totalHeight, _contentWidth, height + _vsep); _totalHeight += height + _vsep;
        }
        if (!float.IsFinite(_contentWidth) || !float.IsFinite(_totalHeight)) throw new InvalidOperationException("Menu geometry overflowed.");
        var entries = 0; foreach (var item in _items) if (!item.Separator) entries++; _searchVisible = _searchEnabled && entries >= _searchMinimum;
        var searchHeight = _searchVisible ? _search.GetCombinedMinimumSize().Y + GetThemeConstant("search_bar_separation") : 0;
        _minimum = _style.GetMinimumSize() + new Vector2(Math.Max(_contentWidth, _searchVisible ? _search.GetCombinedMinimumSize().X : 0), _totalHeight + searchHeight);
        if (Embedder is { } host) _minimum.Y = Math.Min(_minimum.Y, host.GetVisibleRect().Size.Y);
        _dirty = false;
    }
    private static string AcceleratorText(Key accelerator) { using var input = new InputEventKey { Keycode = (Key)((int)accelerator & (int)KeyModifierMask.CodeMask), ShiftPressed = ((int)accelerator & (int)KeyModifierMask.Shift) != 0, ControlPressed = ((int)accelerator & (int)KeyModifierMask.Control) != 0, AltPressed = ((int)accelerator & (int)KeyModifierMask.Alt) != 0, MetaPressed = ((int)accelerator & (int)KeyModifierMask.Meta) != 0 }; return input.AsText(); }
    private void ShadowInsets(bool visible)
    { if (visible && _style is StyleBoxFlat flat) { var offset = flat.ShadowOffset; var size = flat.ShadowSize; _shadowStart = new(MathF.Max(0, size - offset.X), MathF.Max(0, size - offset.Y)); _shadowEnd = new(MathF.Max(0, size + offset.X), MathF.Max(0, size + offset.Y)); if (IsLayoutRTL()) (_shadowStart.X, _shadowEnd.X) = (_shadowEnd.X, _shadowStart.X); } else _shadowStart = _shadowEnd = default; }
    private void Arrange()
    {
        if (_arranging || _view is null || IsDisposed) return; _arranging = true;
        try
        {
            Measure(); ShadowInsets(Visible);
            if (Visible && _contentRect is not null && Size != _expandedSize) _contentRect = null;
            _panel.AddThemeStyleBoxOverride("panel", _style); _panel.Position = _shadowStart; _panel.Size = ((Vector2)Size - _shadowStart - _shadowEnd).Max(Vector2.Zero);
            var inset = _style.GetOffset() + _shadowStart; var area = ((Vector2)Size - _style.GetMinimumSize() - _shadowStart - _shadowEnd).Max(Vector2.Zero);
            _search.RightIcon = GetThemeIcon("search"); _search.Visible = _searchVisible; var height = _searchVisible ? _search.GetCombinedMinimumSize().Y : 0;
            _search.Position = inset; _search.Size = new(area.X, height); if (_searchVisible) height += GetThemeConstant("search_bar_separation");
            _scroll.Position = inset + new Vector2(0, height); _scroll.Size = new(area.X, Math.Max(0, area.Y - height)); _view.CustomMinimumSize = new(_contentWidth, _totalHeight);
            _rowWidth = Math.Max(_contentWidth, _scroll.Size.X - (_scroll.GetVScrollBar().Visible ? _scroll.GetVScrollBar().Size.X : 0)); _view.QueueRedraw();
        }
        finally { _arranging = false; }
    }
    /// <inheritdoc />
    protected override Vector2 OnGetContentsMinimumSize() { if (_view is null) return Vector2.Zero; Measure(); return _minimum; }
    internal override void PreparePopup()
    {
        base.PreparePopup(); _search.Text = ""; FilterChanged(""); Measure(); var width = _shrinkWidth ? Math.Max(1, (int)MathF.Ceiling(_minimum.X)) : Size.X; var height = _shrinkHeight ? Math.Max(1, (int)MathF.Ceiling(_minimum.Y)) : Size.Y; Size = new(width, height);
    }
    internal override void AdjustPopup() { base.AdjustPopup(); Measure(); _contentRect = new(Position, Size); ShadowInsets(true); _expandedSize = Size + (Vector2i)(_shadowStart + _shadowEnd); _arranging = true; try { Position -= (Vector2i)_shadowStart; Size = _expandedSize; } finally { _arranging = false; } Arrange(); }
    private static MouseButtonMask InitialButtonMask() { MouseButtonMask mask = 0; for (var i = 1; i <= 9; i++) if (Input.IsMouseButtonPressed((MouseButton)i)) mask |= (MouseButtonMask)(1u << (i - 1)); return mask; }
    private void VisibilityUpdated()
    {
        if (Visible) { _openedElapsed = 0; _initialButtons = InitialButtonMask(); _grabbedOpen = _initialButtons != 0; _dirty = true; Arrange(); _scroll.ScrollVertical = 0; SetInternalProcessing(true, false); }
        else { _submenuTimer.Stop(); _gamepadDirection = 0; _pendingSubmenu = -1; _activeSubmenu = null; _focused = -1; _search.Text = ""; FilterChanged(""); if (_contentRect is { } rect) { _contentRect = null; Position = rect.Position; Size = rect.Size; } SetInternalProcessing(false, false); }
    }
    internal override void AfterVisibilityChanged(bool visible) { base.AfterVisibilityChanged(visible); if (visible) { if (_searchVisible) _search.GrabFocus(); else _view.GrabFocus(); } }
    /// <summary>Scrolls the item rectangle fully into the visible menu body.</summary><param name="index">A nonnegative item index.</param>
    public void ScrollToItem(int index) { EnsureMutable(); Index(index); Measure(); var rect = _items[index].Rect; if (!Visible) return; var offset = _scroll.ScrollVertical; var height = _scroll.Size.Y; if (rect.Position.Y < offset) _scroll.ScrollVertical = (int)rect.Position.Y; else if (rect.End.Y > offset + height) _scroll.ScrollVertical = (int)MathF.Ceiling(rect.End.Y - height); }
    private void DrawItems(Control canvas)
    {
        Measure(); var rtl = IsLayoutRTL(); var width = Math.Max(_rowWidth, canvas.Size.X);
        for (var index = 0; index < _items.Count; index++)
        {
            var item = _items[index]; if (!item.Visible) continue; var rect = item.Rect with { Size = new(width, item.Rect.Size.Y) };
            if (index == _focused && !item.Separator) canvas.DrawStyleBox(_hoverStyle, rect);
            var x = GetThemeConstant("item_start_padding") + item.Indent * _indentStep + _checkColumn + (_iconColumn > 0 ? _iconColumn + _hsep : 0); var height = rect.Size.Y - _vsep;
            Vector2 IconPoint(float start, Vector2 size) => new(rtl ? width - start - size.X : start, rect.Position.Y + (height - size.Y) / 2 + _vsep / 2f);
            if (item.Checkable != 0 && !item.Separator) { var key = item.Checkable == 2 ? item.Checked ? item.Disabled ? "radio_checked_disabled" : "radio_checked" : item.Disabled ? "radio_unchecked_disabled" : "radio_unchecked" : item.Checked ? item.Disabled ? "checked_disabled" : "checked" : item.Disabled ? "unchecked_disabled" : "unchecked"; var check = GetThemeIcon(key)!; canvas.DrawTextureRect(check, new(IconPoint(GetThemeConstant("item_start_padding"), check.GetSize()), check.GetSize()), false, item.IconModulate); }
            if (item.Icon is { IsDisposed: false } icon) canvas.DrawTextureRect(icon, new(IconPoint(GetThemeConstant("item_start_padding") + _checkColumn, item.IconSize), item.IconSize), false, item.IconModulate);
            if (item.Separator)
            {
                if (item.DisplayText.Length == 0) canvas.DrawStyleBox(_separatorStyle, new(0, rect.Position.Y + height / 2, width, _separatorStyle.GetMinimumSize().Y));
                else { var textWidth = item.TextLayout.Size.X; var left = (width - textWidth) / 2; canvas.DrawStyleBox(_separatorLeft, new(0, rect.Position.Y + height / 2, Math.Max(0, left - _hsep), _separatorLeft.GetMinimumSize().Y)); canvas.DrawStyleBox(_separatorRight, new(left + textWidth + _hsep, rect.Position.Y + height / 2, Math.Max(0, width - left - textWidth - _hsep), _separatorRight.GetMinimumSize().Y)); x = left; }
            }
            var color = GetThemeColor(item.Separator ? "font_separator_color" : item.Disabled ? "font_disabled_color" : index == _focused ? "font_hover_color" : "font_color");
            var baseline = new Vector2(item.Separator ? x : rtl ? width - x - item.TextLayout.Size.X : x, rect.Position.Y + (height - item.TextLayout.Size.Y) / 2 + _vsep / 2 + item.TextLayout.FirstAscent);
            var outline = item.Separator ? _separatorOutline : _outline; var outlineColor = GetThemeColor(item.Separator ? "font_separator_outline_color" : "font_outline_color"); if (outline > 0 && outlineColor.A > 0) item.TextLayout.Draw(canvas, baseline, outlineColor, outline, outlinePass: true); item.TextLayout.Draw(canvas, baseline, color);
            if (item.AcceleratorText.Length != 0) { var ax = rtl ? GetThemeConstant("item_end_padding") : width - GetThemeConstant("item_end_padding") - item.AcceleratorLayout.Size.X; var acceleratorPoint = new Vector2(ax, baseline.Y); if (_outline > 0 && outlineColor.A > 0) item.AcceleratorLayout.Draw(canvas, acceleratorPoint, outlineColor, _outline, outlinePass: true); item.AcceleratorLayout.Draw(canvas, acceleratorPoint, GetThemeColor(index == _focused ? "font_hover_color" : "font_accelerator_color")); }
            if (item.Submenu is not null) { var arrow = GetThemeIcon(rtl ? "submenu_mirrored" : "submenu")!; canvas.DrawTextureRect(arrow, new(IconPoint(width - GetThemeConstant("item_end_padding") - arrow.GetWidth(), arrow.GetSize()), arrow.GetSize()), false, item.IconModulate); }
        }
    }
    /// <inheritdoc />
    protected override void OnNotification(int what)
    { base.OnNotification(what); if (what is NotificationThemeChanged or Control.NotificationLayoutDirectionChanged or Control.NotificationTranslationChanged) { _dirty = true; Arrange(); } else if (what == NotificationInternalProcess) ProcessMenu(); else if (what == NotificationExitTree) { _submenuTimer.Stop(); CloseSubmenu(); } }
}
