namespace Electron2D;

public partial class ItemList
{
    internal override Rect2? CanvasClipRect => ClipContents ? new Rect2(Vector2.Zero, Size) : null;
    private Vector2 _contentSize;
    private int _lastWidth = -1;
    private bool _ensureCurrentVisible;
    private int _fixedColumnWidth, _maxTextLines = 1, _maxColumns = 1, _currentColumns = 1;
    private bool _sameColumnWidth, _autoWidth, _autoHeight, _wraparoundItems = true;
    private float _autoWidthValue, _autoHeightValue;
    private TextOverrunBehavior _textOverrunBehavior = TextOverrunBehavior.TrimEllipsis;
    private ScrollHintMode _hintMode;
    private bool _tileScrollHint;

    /// <summary>Gets or sets a fixed item-column width, with zero selecting measured widths.</summary>
    /// <value>Zero initially.</value>
    public int FixedColumnWidth
    {
        get { ThrowIfDisposed(); return _fixedColumnWidth; }
        set { EnsureMutable(); if (value < 0) throw new ArgumentOutOfRangeException(nameof(value)); if (_fixedColumnWidth == value) return; _fixedColumnWidth = value; InvalidateListLayout(); }
    }

    /// <summary>Gets or sets whether all item columns use the widest measured item width.</summary>
    /// <value>False initially.</value>
    public bool SameColumnWidth
    {
        get { ThrowIfDisposed(); return _sameColumnWidth; }
        set { EnsureMutable(); if (_sameColumnWidth == value) return; _sameColumnWidth = value; InvalidateListLayout(); }
    }

    /// <summary>Gets or sets the maximum shaped lines per top-icon item.</summary>
    /// <value>One initially; must remain positive.</value>
    public int MaxTextLines
    {
        get { ThrowIfDisposed(); return _maxTextLines; }
        set { EnsureMutable(); if (value < 1) throw new ArgumentOutOfRangeException(nameof(value)); if (_maxTextLines == value) return; _maxTextLines = value; InvalidateListLayout(); }
    }

    /// <summary>Gets or sets the maximum items in one row, with zero allowing any count that fits.</summary>
    /// <value>One initially.</value>
    public int MaxColumns
    {
        get { ThrowIfDisposed(); return _maxColumns; }
        set { EnsureMutable(); if (value < 0) throw new ArgumentOutOfRangeException(nameof(value)); if (_maxColumns == value) return; _maxColumns = value; InvalidateListLayout(); }
    }

    /// <summary>Gets or sets whether the measured content width contributes an intrinsic minimum.</summary>
    /// <value>False initially.</value>
    public bool AutoWidth { get { ThrowIfDisposed(); return _autoWidth; } set { EnsureMutable(); if (_autoWidth == value) return; _autoWidth = value; InvalidateListLayout(); } }

    /// <summary>Gets or sets whether the measured content height contributes an intrinsic minimum.</summary>
    /// <value>False initially.</value>
    public bool AutoHeight { get { ThrowIfDisposed(); return _autoHeight; } set { EnsureMutable(); if (_autoHeight == value) return; _autoHeight = value; InvalidateListLayout(); } }

    /// <summary>Gets or sets whether rows wrap rather than grow a horizontal scroll range.</summary>
    /// <value>True initially.</value>
    public bool WraparoundItems { get { ThrowIfDisposed(); return _wraparoundItems; } set { EnsureMutable(); if (_wraparoundItems == value) return; _wraparoundItems = value; InvalidateListLayout(); } }

    /// <summary>Gets or sets the policy for text extending beyond an item cell.</summary>
    /// <value>TrimEllipsis initially.</value>
    public TextOverrunBehavior TextOverrunBehavior
    {
        get { ThrowIfDisposed(); return _textOverrunBehavior; }
        set { EnsureMutable(); if (!Enum.IsDefined(value)) throw new ArgumentOutOfRangeException(nameof(value)); if (_textOverrunBehavior == value) return; _textOverrunBehavior = value; InvalidateListLayout(); }
    }

    /// <summary>Gets or sets which reachable vertical edges show the themed scroll hint.</summary>
    /// <value>Disabled initially.</value>
    public ScrollHintMode HintMode
    {
        get { ThrowIfDisposed(); return _hintMode; }
        set { EnsureMutable(); if (!Enum.IsDefined(value)) throw new ArgumentOutOfRangeException(nameof(value)); if (_hintMode == value) return; _hintMode = value; QueueRedraw(); }
    }

    /// <summary>Gets or sets whether the scroll-hint texture tiles instead of stretching.</summary>
    /// <value>False initially.</value>
    public bool TileScrollHint
    {
        get { ThrowIfDisposed(); return _tileScrollHint; }
        set { EnsureMutable(); if (_tileScrollHint == value) return; _tileScrollHint = value; QueueRedraw(); }
    }

    /// <inheritdoc />
    protected override Vector2 OnGetMinimumSize() => new(_autoWidth ? _autoWidthValue : 0, _autoHeight ? _autoHeightValue : 0);

    private void InvalidateListLayout()
    {
        _layoutDirty = true;
        QueueRedraw();
        UpdateMinimumSize();
    }

    private void RebuildListLayout()
    {
        var width = Math.Max(0, (int)Size.X);
        if (!_layoutDirty && _lastWidth == width) return;
        _layoutDirty = false;
        _lastWidth = width;
        var panel = GetThemeStyleBox("panel") ?? throw new InvalidOperationException("ItemList requires its panel style.");
        var font = GetThemeFont("font") ?? ThemeDB.FallbackFont ?? throw new InvalidOperationException("ItemList requires a font.");
        var fontSize = GetThemeFontSize("font_size");
        var margin = panel.GetMinimumSize();
        var offset = panel.GetOffset();
        var availableWidth = Math.Max(0, Size.X - margin.X);
        var availableHeight = Math.Max(0, Size.Y - margin.Y);
        var hSeparation = Math.Max(0, GetThemeConstant("h_separation"));
        var vSeparation = Math.Max(0, GetThemeConstant("v_separation"));
        var iconMargin = GetThemeConstant("icon_margin");
        var barWidth = _vBar.GetBoundMinimumSize().X;
        var fitWidth = availableWidth;
        var columns = _maxColumns == 0 ? Math.Max(1, _items.Count) : Math.Min(_maxColumns, Math.Max(1, _items.Count));
        for (var pass = 0; pass < 2; pass++)
        {
            for (; columns >= 1; columns--)
            {
                var widestItem = 0f;
                foreach (var item in _items)
                {
                    var iconSize = IconSize(item);
                    var cellWidth = _fixedColumnWidth > 0 ? _fixedColumnWidth : _wraparoundItems ? fitWidth / columns : -1;
                    var textWidth = cellWidth < 0 ? -1 : _iconMode == IconMode.Top ? Math.Max(0, cellWidth - hSeparation) :
                        Math.Max(0, cellWidth - iconSize.X - (iconSize.X > 0 ? iconMargin : 0) - hSeparation);
                    if (item.Text.Length != 0)
                    {
                        var displayed = item.AutoTranslate switch
                        {
                            NodeAutoTranslateMode.Always => Tr(item.Text),
                            NodeAutoTranslateMode.Disabled => item.Text,
                            _ => Atr(item.Text)
                        };
                        var direction = item.Direction == TextDirection.Inherited
                            ? IsLayoutRTL() ? TextDirection.RTL : TextDirection.LTR
                            : item.Direction;
                        var breaks = _iconMode == IconMode.Top
                            ? TextLineBreakFlags.Mandatory | TextLineBreakFlags.WordBound | TextLineBreakFlags.GraphemeBound |
                              TextLineBreakFlags.TrimStartEdgeSpaces | TextLineBreakFlags.TrimEndEdgeSpaces
                            : TextLineBreakFlags.None;
                        var key = new TextLayoutKey(displayed, fontSize, textWidth, HorizontalAlignment.Left,
                            _iconMode == IconMode.Top ? _maxTextLines : 1, breaks, TextJustificationFlags.None,
                            direction, TextOrientation.Horizontal, _iconMode == IconMode.Top);
                        item.Layout.Build(font, key, new TextLayoutOptions(LineSpacing: GetThemeConstant("line_separation"),
                            Language: item.Language, Overrun: (int)_textOverrunBehavior));
                    }
                    var textSize = item.Text.Length == 0 ? Vector2.Zero : item.Layout.Size;
                    var between = iconSize.X > 0 && textSize.X > 0 ? iconMargin : 0;
                    var itemWidth = _iconMode == IconMode.Top ? Math.Max(iconSize.X, textSize.X) + hSeparation :
                        iconSize.X + between + textSize.X + hSeparation;
                    var itemHeight = (_iconMode == IconMode.Top ? iconSize.Y + between + textSize.Y : Math.Max(iconSize.Y, textSize.Y)) + vSeparation;
                    item.Rect = new(0, 0, _fixedColumnWidth > 0 ? _fixedColumnWidth : itemWidth, itemHeight);
                    widestItem = Math.Max(widestItem, item.Rect.Size.X);
                }

                if (_sameColumnWidth)
                    foreach (var item in _items) item.Rect.Size = new(widestItem, item.Rect.Size.Y);

                var allFit = true;
                var y = 0f;
                var widestRow = 0f;
                for (var rowStart = 0; rowStart < _items.Count; rowStart += columns)
                {
                    var rowEnd = Math.Min(_items.Count, rowStart + columns);
                    var rowHeight = 0f;
                    var rowWidth = 0f;
                    for (var index = rowStart; index < rowEnd; index++)
                    {
                        rowHeight = Math.Max(rowHeight, _items[index].Rect.Size.Y);
                        rowWidth += _items[index].Rect.Size.X;
                    }
                    if (columns > 1 && _wraparoundItems && !_autoWidth && rowWidth > fitWidth)
                    { allFit = false; break; }
                    var x = 0f;
                    for (var index = rowStart; index < rowEnd; index++)
                    {
                        var item = _items[index];
                        item.Rect = new(x, y, item.Rect.Size.X, rowHeight);
                        x += item.Rect.Size.X;
                    }
                    widestRow = Math.Max(widestRow, rowWidth);
                    y += rowHeight;
                }
                if (!allFit) continue;
                _contentSize = new(widestRow, y);
                _currentColumns = columns;
                break;
            }
            if (pass == 0 && _wraparoundItems && _contentSize.Y > availableHeight && fitWidth == availableWidth)
            { fitWidth = Math.Max(0, availableWidth - barWidth); continue; }
            break;
        }
        var verticalVisible = _contentSize.Y > availableHeight;
        var visibleBarWidth = verticalVisible ? barWidth : 0;
        var horizontalPage = Math.Max(0, availableWidth - visibleBarWidth);
        var horizontalVisible = !_wraparoundItems && _contentSize.X > horizontalPage;
        var horizontalHeight = horizontalVisible ? _hBar.GetBoundMinimumSize().Y : 0;
        var verticalPage = Math.Max(0, availableHeight - horizontalHeight);
        verticalVisible = _contentSize.Y > verticalPage;
        visibleBarWidth = verticalVisible ? barWidth : 0;
        horizontalPage = Math.Max(0, availableWidth - visibleBarWidth);
        _vBar.MaxValue = Math.Max(_contentSize.Y, verticalPage);
        _vBar.Page = verticalPage;
        _vBar.Visible = verticalVisible;
        _hBar.MinValue = IsLayoutRTL() && !_wraparoundItems ? -Math.Max(0, _contentSize.X - horizontalPage) : 0;
        _hBar.MaxValue = IsLayoutRTL() && !_wraparoundItems ? horizontalPage : Math.Max(_contentSize.X, horizontalPage);
        _hBar.Page = horizontalPage;
        _hBar.Visible = horizontalVisible;
        _vBar.SetContainerRect(new(IsLayoutRTL() ? offset.X : Size.X - offset.X - visibleBarWidth, offset.Y,
            visibleBarWidth, verticalPage));
        _hBar.SetContainerRect(new(offset.X, Size.Y - offset.Y - horizontalHeight,
            horizontalPage, horizontalHeight));
        var nextWidth = _contentSize.X + margin.X + visibleBarWidth;
        var nextHeight = _contentSize.Y + margin.Y + horizontalHeight;
        if (_autoWidth && _autoWidthValue != nextWidth || _autoHeight && _autoHeightValue != nextHeight)
        {
            _autoWidthValue = nextWidth;
            _autoHeightValue = nextHeight;
            UpdateMinimumSize();
        }
    }

    /// <summary>Gets the layout rectangle for an item in control-local coordinates.</summary>
    /// <param name="index">A valid zero-based item index.</param>
    /// <param name="expand">Extend the final column to the available width.</param>
    /// <returns>The current item rectangle with panel offset; scrolling does not change this content-space rectangle.</returns>
    public Rect2 GetItemRect(int index, bool expand = true)
    {
        ThrowIfDisposed(); RebuildListLayout();
        var panel = GetThemeStyleBox("panel")!;
        var rect = GetItem(index).Rect;
        if (expand && index % _currentColumns == _currentColumns - 1)
            rect.Size = new(Math.Max(rect.Size.X, Size.X - panel.GetMinimumSize().X -
                (_vBar.Visible ? _vBar.GetBoundMinimumSize().X : 0) - rect.Position.X), rect.Size.Y);
        rect.Position += panel.GetOffset();
        return rect;
    }

    /// <summary>Finds the item containing a local point, or the nearest item when exact matching is disabled.</summary>
    /// <param name="position">Finite position in this control's local coordinates.</param>
    /// <param name="exact">Require containment rather than proximity.</param>
    /// <returns>An item index, or minus one when none qualifies.</returns>
    public int GetItemAtPosition(Vector2 position, bool exact = false)
    {
        ThrowIfDisposed(); if (!position.IsFinite()) throw new ArgumentException("The item position must be finite.", nameof(position));
        RebuildListLayout();
        var panel = GetThemeStyleBox("panel")!;
        if (exact && !new Rect2(Vector2.Zero, Size).HasPoint(position)) return -1;
        var candidate = -1; var distance = float.PositiveInfinity;
        var rtl = IsLayoutRTL();
        var scroll = new Vector2(rtl ? (float)_hBar.Value : -(float)_hBar.Value, -(float)_vBar.Value);
        for (var index = 0; index < _items.Count; index++)
        {
            var rect = GetItemRect(index);
            rect.Position += scroll;
            if (rtl) rect.Position = new(Size.X - rect.Position.X - rect.Size.X + panel.GetMargin(Side.Left) - panel.GetMargin(Side.Right), rect.Position.Y);
            if (rect.HasPoint(position)) return index;
            if (exact) continue;
            var x = Math.Max(rect.Position.X - position.X, Math.Max(0, position.X - rect.End.X));
            var y = Math.Max(rect.Position.Y - position.Y, Math.Max(0, position.Y - rect.End.Y));
            var score = x * x + y * y;
            if (score < distance) { candidate = index; distance = score; }
        }
        return candidate;
    }

    /// <summary>Tests whether a local point lies after the last item in list order.</summary>
    /// <param name="position">Finite position in this control's local coordinates.</param>
    /// <returns>True for an empty list or a point below its final item.</returns>
    public bool IsPosAtEndOfItems(Vector2 position)
    {
        ThrowIfDisposed(); if (!position.IsFinite()) throw new ArgumentException("The item position must be finite.", nameof(position));
        if (_items.Count == 0) return true;
        RebuildListLayout();
        return position.Y - GetThemeStyleBox("panel")!.GetOffset().Y + _vBar.Value > _items[^1].Rect.End.Y;
    }

    /// <summary>Queues a scroll adjustment that reveals the current item on the next layout/draw pass.</summary>
    public void EnsureCurrentIsVisible()
    {
        EnsureMutable(); _ensureCurrentVisible = true; QueueRedraw();
    }

    /// <summary>Centers the current item along one or both active scroll axes.</summary>
    /// <param name="centerVertically">Center its vertical span.</param>
    /// <param name="centerHorizontally">Center its horizontal span.</param>
    /// <exception cref="ArgumentException">Both axis choices are false for an existing current item.</exception>
    public void CenterOnCurrent(bool centerVertically = true, bool centerHorizontally = true)
    {
        EnsureMutable(); if ((uint)_current >= (uint)_items.Count) return;
        if (!centerVertically && !centerHorizontally) throw new ArgumentException("At least one centering axis is required.");
        RebuildListLayout(); var rect = _items[_current].Rect;
        if (centerVertically) _vBar.Value = rect.Position.Y + rect.Size.Y * .5f - _vBar.Page * .5f;
        if (centerHorizontally) _hBar.Value = rect.Position.X + rect.Size.X * .5f - _hBar.Page * .5f;
    }

    /// <summary>Synchronously refreshes item rectangles, measured minima and both scroll ranges.</summary>
    /// <remarks>Normal changes queue redraw; use this before querying geometry immediately after a mutation.</remarks>
    public void ForceUpdateListSize()
    {
        EnsureMutable(); RebuildListLayout();
    }

    /// <inheritdoc />
    protected override void OnNotification(int what)
    {
        base.OnNotification(what);
        if (what == NotificationMouseExit && _hovered != -1) { _hovered = -1; QueueRedraw(); }
        if (what is NotificationResized or NotificationThemeChanged or NotificationLayoutDirectionChanged or NotificationTranslationChanged)
            InvalidateListLayout();
        if (what == NotificationDraw) DrawList();
    }

    private void DrawList()
    {
        RebuildListLayout();
        if (_ensureCurrentVisible && (uint)_current < (uint)_items.Count)
        {
            var rect = _items[_current].Rect;
            if (rect.Position.Y < _vBar.Value) _vBar.Value = rect.Position.Y;
            else if (rect.End.Y > _vBar.Value + _vBar.Page) _vBar.Value = rect.End.Y - _vBar.Page;
        }
        _ensureCurrentVisible = false;
        var panel = GetThemeStyleBox("panel") ?? throw new InvalidOperationException("ItemList requires its panel style.");
        DrawStyleBox(panel, new(Vector2.Zero, Size));
        var selection = GetThemeStyleBox(HasFocus(true) ? "selected_focus" : "selected") ??
            throw new InvalidOperationException("ItemList requires its selection style.");
        var textColor = GetThemeColor("font_color");
        var selectedTextColor = GetThemeColor("font_selected_color");
        var hoveredTextColor = GetThemeColor("font_hovered_color");
        var hoveredSelectedTextColor = GetThemeColor("font_hovered_selected_color");
        var hoveredStyle = GetThemeStyleBox("hovered");
        var hoveredSelectedStyle = GetThemeStyleBox(HasFocus(true) ? "hovered_selected_focus" : "hovered_selected");
        var offset = panel.GetOffset() + new Vector2(IsLayoutRTL() ? (float)_hBar.Value : -(float)_hBar.Value, -(float)_vBar.Value);
        var iconMargin = GetThemeConstant("icon_margin");
        var hSeparation = Math.Max(0, GetThemeConstant("h_separation"));
        var vSeparation = Math.Max(0, GetThemeConstant("v_separation"));
        var outlineSize = GetThemeConstant("outline_size");
        var outlineColor = GetThemeColor("font_outline_color");
        var rtl = IsLayoutRTL();
        if (_iconMode != IconMode.Top)
            for (var index = 1; index < _items.Count; index++)
                if (_items[index].Rect.Position.Y > _items[index - 1].Rect.Position.Y)
                {
                    var lineY = _items[index].Rect.Position.Y + offset.Y;
                    DrawLine(new(panel.GetOffset().X, lineY), new(Size.X - panel.GetMargin(Side.Right), lineY),
                        GetThemeColor("guide_color"));
                }
        for (var index = 0; index < _items.Count; index++)
        {
            var item = _items[index];
            var rect = GetItemRect(index);
            rect.Position += new Vector2(rtl ? (float)_hBar.Value : -(float)_hBar.Value, -(float)_vBar.Value);
            if (rect.End.Y < 0 || rect.Position.Y > Size.Y) continue;
            if (rtl) rect.Position = new(Size.X - rect.Position.X - rect.Size.X + panel.GetMargin(Side.Left) - panel.GetMargin(Side.Right), rect.Position.Y);
            if (item.Selected) DrawStyleBox(index == _hovered && hoveredSelectedStyle is not null ? hoveredSelectedStyle : selection, rect);
            else if (index == _hovered && hoveredStyle is not null) DrawStyleBox(hoveredStyle, rect);
            if (item.CustomBG.A > .001f) DrawRect(rect, item.CustomBG);
            var iconSize = IconSize(item);
            if (item.Icon is { } icon)
            {
                var iconPosition = _iconMode == IconMode.Top
                    ? rect.Position + new Vector2(MathF.Floor((rect.Size.X - iconSize.X) * .5f), vSeparation * .5f)
                    : rect.Position + new Vector2(hSeparation * .5f, MathF.Floor((rect.Size.Y - iconSize.Y) * .5f));
                var region = item.IconRegion.Size.X == 0 || item.IconRegion.Size.Y == 0
                    ? new Rect2(Vector2.Zero, icon.GetSize()) : item.IconRegion;
                var modulate = item.IconModulate;
                if (item.Disabled) modulate.A *= .5f;
                var fittedSize = FittedIconSize(item, iconSize);
                iconPosition += (iconSize - fittedSize) * .5f;
                if (fittedSize.X > 0 && fittedSize.Y > 0)
                    DrawTextureRectRegion(icon, new(iconPosition, fittedSize), region, modulate, item.IconTransposed);
            }
            if (item.Text.Length == 0) continue;
            var color = item.CustomFG != default ? item.CustomFG :
                item.Selected ? index == _hovered ? hoveredSelectedTextColor : selectedTextColor :
                index == _hovered ? hoveredTextColor : textColor;
            if (item.Disabled) color.A *= .5f;
            var baseline = _iconMode == IconMode.Top
                ? rect.Position + new Vector2(MathF.Floor((rect.Size.X - item.Layout.Size.X) * .5f),
                    iconSize.Y + (iconSize.Y > 0 ? iconMargin : 0) + vSeparation * .5f + item.Layout.FirstAscent)
                : rect.Position + new Vector2(hSeparation * .5f + (iconSize.X > 0 ? iconSize.X + iconMargin : 0),
                    (rect.Size.Y - item.Layout.Size.Y) * .5f + item.Layout.FirstAscent);
            if (outlineSize > 0 && outlineColor.A > 0) item.Layout.Draw(this, baseline, outlineColor, outlineSize, outlinePass: true);
            item.Layout.Draw(this, baseline, color);
        }
        if (_selectMode != SelectMode.Single && (uint)_current < (uint)_items.Count)
        {
            var cursor = GetThemeStyleBox(HasFocus(true) ? "cursor" : "cursor_unfocused");
            if (cursor is not null)
            {
                var rect = _items[_current].Rect with { Position = _items[_current].Rect.Position + offset };
                if (rtl) rect.Position = new(Size.X - rect.Position.X - rect.Size.X, rect.Position.Y);
                DrawStyleBox(cursor, rect);
            }
        }
        if (_hintMode != ScrollHintMode.Disabled)
        {
            var texture = GetThemeIcon("scroll_hint") ?? throw new InvalidOperationException("ItemList requires its scroll hint icon.");
            var color = GetThemeColor("scroll_hint_color");
            var hintHeight = texture.GetSize().Y;
            if (_vBar.Value > 1 && _hintMode is ScrollHintMode.Both or ScrollHintMode.Top)
                DrawTextureRect(texture, new(0, 0, Size.X, hintHeight), _tileScrollHint, color);
            if (_vBar.Value < _vBar.MaxValue - _vBar.Page - 1 && _hintMode is ScrollHintMode.Both or ScrollHintMode.Bottom)
                DrawTextureRect(texture, new(0, Size.Y - hintHeight, Size.X, -hintHeight), _tileScrollHint, color);
        }
        if (HasFocus(true) && GetThemeStyleBox("focus") is { } focus)
            DrawStyleBox(focus, new(Vector2.Zero, Size));
    }
}
