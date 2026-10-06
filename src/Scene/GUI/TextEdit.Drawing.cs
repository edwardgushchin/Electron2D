namespace Electron2D;

public partial class TextEdit
{
    private void ReleaseTextures()
    {
        foreach (var texture in _textures) { texture.Changed -= _resourceChanged; texture.Disposed -= _resourceDisposed; if (_texturesResident) texture.ReleaseRendererCacheResidency(); }
        _textures.Clear(); _texturesResident = false;
    }
    private void RefreshTextures()
    {
        ReleaseTextures(); foreach (var line in _lines) foreach (var cell in line.Gutters) if (cell.Icon is { IsDisposed: false } icon) _textures.Add(icon);
        if (_drawSpaces && GetThemeIcon("space") is { IsDisposed: false } space) _textures.Add(space); if (_drawTabs && GetThemeIcon("tab") is { IsDisposed: false } tab) _textures.Add(tab);
        foreach (var texture in _textures) { texture.Changed += _resourceChanged; texture.Disposed += _resourceDisposed; if (IsInsideTree) texture.AcquireRendererCacheResidency(); }
        _texturesResident = IsInsideTree;
    }
    private void PrepareLineColors()
    {
        var normal = GetThemeColor(_editable ? "font_color" : "font_readonly_color");
        foreach (var line in _lines)
        {
            var count = CountScalars(line.Shown); if (line.Colors.Length != count) line.Colors = new Color[count]; Array.Fill(line.Colors, normal);
            line.Characters = line.Shown.EnumerateRunes().Select(r => r.Value).ToArray();
        }
        if (_highlighter is { IsDisposed: false }) for (var i = 0; i < _lines.Count; i++)
            {
                var colors = _lines[i].Colors; var previous = normal; var start = 0;
                foreach (var pair in _highlighter.GetLineSyntaxHighlighting(i)) { var end = Math.Min(colors.Length, pair.Key); if (end > start) Array.Fill(colors, previous, start, end - start); previous = pair.Value; start = end; }
                if (start < colors.Length) Array.Fill(colors, previous, start, colors.Length - start);
            }
        foreach (var line in _lines) foreach (var cell in line.Gutters)
                cell.Layout.Build(_font!, new(cell.Text, _fontSize, 0, HorizontalAlignment.Left, -1, 0, 0, TextDirection.LTR, TextOrientation.Horizontal));
    }
    private void DrawClippedRect(Rect2 rect, Color color, Rect2 clip) { rect = rect.Intersection(clip); if (rect.HasArea() && color.A > 0) DrawRect(rect, color); }
    private void DrawRange(Line line, int wrap, int from, int to, float x, float y, Color color, bool border = false, bool displayColumns = false)
    {
        if (line.Layout.LineCount == 0) return; var row = line.Layout.Lines[wrap]; if (!displayColumns) { from = DisplayColumn(line.Index, from); to = DisplayColumn(line.Index, to); }
        from = Math.Max(from, row.Start); to = Math.Min(to, row.End); if (from >= to) return;
        line.Layout.SelectionRanges(from, to, _selectionRanges);
        foreach (var range in _selectionRanges)
        {
            var rect = new Rect2(new(x + range.X, y), new(Math.Max(1, range.Y - range.X), _rowHeight)).Intersection(_contentRect);
            if (rect.HasArea()) DrawRect(rect, color, filled: !border);
        }
    }
    private void DrawDocument()
    {
        EnsureTextLayout(); if (_drawing) throw new InvalidOperationException("TextEdit drawing cannot reenter."); _drawing = true;
        try
        {
            var bounds = new Rect2(Vector2.Zero, Size); Style?.Draw(this, bounds); if (HasFocus()) GetThemeStyleBox("focus")?.Draw(this, bounds);
            var first = Math.Max(0, (int)_scrollVertical); var end = Math.Min(_visual.Count, first + GetVisibleLineCount() + 2);
            var normal = GetThemeColor(_editable ? "font_color" : "font_readonly_color"); var outline = GetThemeConstant("outline_size");
            ReadOnlySpan<char> selectedWord = default;
            if (_highlightAllOccurrences && _carets.Count == 1 && _carets[0].Selected && _carets[0].Origin.Y == _carets[0].Position.Y) { var a = SelectionStart(_carets[0]); var b = SelectionEnd(_carets[0]); var source = _lines[a.Y].Text; var start = ScalarIndex(source, a.X); selectedWord = source.AsSpan(start, ScalarIndex(source, b.X) - start); }
            for (var index = first; index < end; index++)
            {
                var visual = _visual[index]; var line = _lines[visual.Line]; var y = _contentRect.Position.Y + (float)(index - _scrollVertical) * _rowHeight; var x = _contentRect.Position.X - _scrollHorizontal;
                DrawClippedRect(new(new(_contentRect.Position.X, y), new(_contentRect.Size.X, _rowHeight)), line.Background, _contentRect);
                if (_highlightCurrentLine) foreach (var caret in _carets) if (caret.Position.Y == visual.Line) { DrawClippedRect(new(new(_contentRect.Position.X, y), new(_contentRect.Size.X, _rowHeight)), GetThemeColor("current_line_color"), _contentRect); break; }
                if (_characterColors.Length < line.Colors.Length) _characterColors = new Color[line.Colors.Length]; line.Colors.CopyTo(_characterColors, 0);
                if (_selectingEnabled) foreach (var caret in _carets) if (caret.Selected)
                        {
                            var a = SelectionStart(caret); var b = SelectionEnd(caret); if (visual.Line < a.Y || visual.Line > b.Y) continue;
                            var from = visual.Line == a.Y ? a.X : 0; var to = visual.Line == b.Y ? b.X : line.Scalars;
                            DrawRange(line, visual.Wrap, from, to, x, y, GetThemeColor("selection_color"));
                            var selected = GetThemeColor("font_selected_color"); if (selected.A > 0) for (var c = DisplayColumn(visual.Line, from); c < Math.Min(DisplayColumn(visual.Line, to), line.Colors.Length); c++) _characterColors[c] = selected;
                        }
                DrawMatches(line, visual.Wrap, _searchText, _searchFlags, x, y, "search_result_color", false);
                DrawMatches(line, visual.Wrap, _searchText, _searchFlags, x, y, "search_result_border_color", true);
                if (selectedWord.Length > 0) DrawMatches(line, visual.Wrap, selectedWord, SearchFlags.MatchCase, x, y, "word_highlighted_color", false);
                if (line.Layout.LineCount > 0)
                {
                    var row = line.Layout.Lines[visual.Wrap]; var baseline = new Vector2(x, y + row.Ascent);
                    if (outline > 0) line.Layout.Draw(this, baseline, GetThemeColor("font_outline_color"), outline, firstLine: visual.Wrap, maxLines: 1, outlinePass: true, clipRect: _contentRect);
                    line.Layout.Draw(this, baseline, normal, firstLine: visual.Wrap, maxLines: 1, clipRect: _contentRect, characterColors: _characterColors.AsSpan(0, line.Colors.Length));
                    if (_drawSpaces || _drawTabs) for (var c = row.Start; c < Math.Min(row.End, line.Characters.Length); c++)
                        {
                            var marker = line.Characters[c] == ' ' && _drawSpaces ? GetThemeIcon("space") : line.Characters[c] == '\t' && _drawTabs ? GetThemeIcon("tab") : null;
                            if (marker is { IsDisposed: false }) { var advance = Math.Max(1, RawCaretX(line, c + 1, visual.Wrap) - RawCaretX(line, c, visual.Wrap)); var markerRect = new Rect2(new(x + RawCaretX(line, c, visual.Wrap), y + (_rowHeight - marker.GetHeight()) / 2), new(Math.Min(marker.GetWidth(), advance), marker.GetHeight())); if (_contentRect.Encloses(markerRect)) DrawTextureRect(marker, markerRect, false); }

                        }
                }
                if (visual.Wrap == 0) DrawGutters(visual.Line, y);
            }
            if (_lines.Count == 1 && _lines[0].Text.Length == 0 && _ime.Length == 0) _placeholderLayout.Draw(this, _contentRect.Position + new Vector2(0, _placeholderLayout.FirstAscent), GetThemeColor("font_placeholder_color"), clipRect: _contentRect);
            if ((_editable || _caretDrawWhenEditableDisabled) && HasFocus() && (!_caretBlink || _caretVisible)) foreach (var caret in _carets)
                {
                    var line = _lines[caret.Position.Y]; var wrap = GetLineWrapIndexAtColumn(caret.Position.Y, caret.Position.X); var index = VisualIndex(caret.Position.Y, wrap); if (index < first || index >= end) continue;
                    var column = DisplayColumn(caret.Position.Y, caret.Position.X) + _imeSelection.X; var x = _contentRect.Position.X + RawCaretX(line, column, wrap) - _scrollHorizontal; var y = _contentRect.Position.Y + (float)(index - _scrollVertical) * _rowHeight;
                    var width = _caretType == TextEditCaretType.Block || _overtype ? Math.Max(2, column < line.Scalars ? Math.Abs(RawCaretX(line, column + 1, wrap) - RawCaretX(line, column, wrap)) : _font!.GetCharSize(' ', _fontSize).X) : Math.Max(1, GetThemeConstant("caret_width"));
                    var caretRect = new Rect2(new(x, _overtype ? y + _rowHeight - 2 : y), new(width, _overtype ? 2 : _rowHeight));
                    DrawClippedRect(caretRect, GetThemeColor("caret_color"), _contentRect);
                    if (_caretType == TextEditCaretType.Block && !_overtype && line.Layout.LineCount > 0) line.Layout.Draw(this, new(_contentRect.Position.X - _scrollHorizontal, y + line.Layout.Lines[wrap].Ascent), GetThemeColor("caret_background_color"), firstLine: wrap, maxLines: 1, clipRect: caretRect.Intersection(_contentRect));
                    if (_ime.Length > 0) DrawRange(line, wrap, DisplayColumn(caret.Position.Y, caret.Position.X), DisplayColumn(caret.Position.Y, caret.Position.X) + CountScalars(_ime), _contentRect.Position.X - _scrollHorizontal, y + _rowHeight - 2, GetThemeColor("caret_color"), displayColumns: true);
                }
            if (_minimapDraw) DrawMinimap();
        }
        finally { _drawing = false; }
    }
    private void DrawMatches(Line line, int wrap, ReadOnlySpan<char> text, SearchFlags flags, float x, float y, string themeColor, bool border)
    {
        if (text.Length == 0) return; var start = 0; var comparison = (flags & SearchFlags.MatchCase) != 0 ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
        while (start <= line.Text.Length)
        {
            var match = line.Text.AsSpan(start).IndexOf(text, comparison); if (match < 0) break; start += match;
            var from = CountScalars(line.Text.AsSpan(0, start)); var to = from + CountScalars(text);
            if ((flags & SearchFlags.WholeWords) == 0 || IsWordBoundary(_lines.IndexOf(line), from) && IsWordBoundary(_lines.IndexOf(line), to)) DrawRange(line, wrap, from, to, x, y, GetThemeColor(themeColor), border);
            start += Math.Max(1, text.Length);
        }
    }
    private void DrawGutters(int lineIndex, float y)
    {
        var x = IsLayoutRTL() ? _contentRect.End.X : _contentRect.Position.X - GetTotalGutterWidth(); var line = _lines[lineIndex];
        for (var i = 0; i < _gutters.Count; i++)
        {
            var gutter = _gutters[i]; if (!gutter.Draw) continue; var cell = line.Gutters[i]; var rect = new Rect2(new(x, y), new(gutter.Width, _rowHeight)); x += gutter.Width;
            if (gutter.Type == GutterType.String && cell.Layout.LineCount > 0) cell.Layout.Draw(this, rect.Position + new Vector2(Math.Max(0, (rect.Size.X - cell.Layout.Size.X) / 2), cell.Layout.FirstAscent), cell.Color, clipRect: rect.Intersection(new(Vector2.Zero, Size)));
            else if (gutter.Type == GutterType.Icon && cell.Icon is { IsDisposed: false } icon) DrawTextureRect(icon, rect, false, cell.Color);
            else if (gutter.Type == GutterType.Custom) { gutter.CustomDraw?.Invoke(this, lineIndex, i, rect); if (IsDisposed) return; }
        }
    }
    private void DrawMinimap()
    {
        var rect = MinimapRect(); var step = Math.Max(1, GetThemeConstant("minimap_char_size")); var capacity = Math.Max(1, (int)(rect.Size.Y / step)); var first = Math.Clamp((int)_scrollVertical - capacity / 4, 0, Math.Max(0, _visual.Count - capacity));
        for (var i = first; i < Math.Min(_visual.Count, first + capacity); i++)
        {
            var visual = _visual[i]; var line = _lines[visual.Line]; if (line.Layout.LineCount == 0) continue; var row = line.Layout.Lines[visual.Wrap]; var max = Math.Min(row.End, row.Start + (int)(rect.Size.X / step));
            for (var c = row.Start; c < max; c++) if (line.Characters[c] != ' ' && line.Characters[c] != '\t') DrawClippedRect(new(new(rect.Position.X + (c - row.Start) * step, rect.Position.Y + (i - first) * step), new(step, Math.Max(1, step - 1))), line.Colors[c], rect);
        }
        var indicator = new Rect2(new(rect.Position.X, rect.Position.Y + (float)(_scrollVertical - first) * step), new(rect.Size.X, GetVisibleLineCount() * step)); DrawClippedRect(indicator, GetThemeColor("caret_color") * new Color(1, 1, 1, .2f), rect);
    }
    private Rect2 MinimapRect() => new(new(IsLayoutRTL() ? _contentRect.Position.X - _minimapWidth : _contentRect.End.X, _contentRect.Position.Y), new(_minimapWidth, _contentRect.Size.Y));
    /// <inheritdoc />
    protected override Vector2 OnGetMinimumSize()
    {
        if (_vBar is null) return Vector2.Zero; EnsureTextLayout(); var margins = Style?.GetMinimumSize() ?? default; return margins + new Vector2(_scrollFitContentWidth ? _contentWidth + GetTotalGutterWidth() : GetThemeConstant("minimum_character_width") * _font!.GetCharSize('W', _fontSize).X, _scrollFitContentHeight ? _contentHeight : _rowHeight * 3);
    }
    /// <inheritdoc />
    protected override void OnNotification(int what)
    {
        if (what == NotificationDraw) { base.OnNotification(what); DrawDocument(); return; }
        base.OnNotification(what); if (IsDisposed) return;
        switch (what)
        {
            case NotificationEnterTree: InvalidateTextLayout(); UpdateProcessing(); break;
            case NotificationExitTree: ReleaseTextures(); ActivateIME(false); _ime = ""; _draggingCursor = _draggingMinimap = false; SetInternalProcessing(false, false); break;
            case NotificationFocusEnter: ActivateIME(_editable); ResetBlink(); break;
            case NotificationFocusExit: ApplyIME(); ActivateIME(false); _draggingCursor = false; if (_deselectOnFocusLossEnabled) Deselect(); UpdateProcessing(); break;
            case NotificationThemeChanged: if (_highlighter is { IsDisposed: false }) _highlighter.ClearHighlightingCache(); InvalidateTextLayout(); break;
            case NotificationResized: case NotificationTranslationChanged: case NotificationLayoutDirectionChanged: InvalidateTextLayout(); break;
            case NotificationDragEnd: FinishTextDrag(); break;
            case NotificationInternalProcess:
                if (Interlocked.Exchange(ref _resourcePending, 0) != 0 || _font is { IsDisposed: false } && _font.GetContentGeneration() != _fontGeneration) InvalidateTextLayout();
                if (_caretBlink && HasFocus()) { _blinkTime += ProcessDeltaTime; if (_blinkTime >= _caretBlinkInterval) { _blinkTime %= _caretBlinkInterval; _caretVisible = !_caretVisible; QueueRedraw(); } }
                if (_scrollSmooth && Math.Abs(_verticalTarget - _scrollVertical) > .0001) { var step = _scrollVScrollSpeed * ProcessDeltaTime; _scrollVertical += Math.Sign(_verticalTarget - _scrollVertical) * Math.Min(step, Math.Abs(_verticalTarget - _scrollVertical)); _vBar.SetValueNoSignal(_scrollVertical); QueueRedraw(); UpdateProcessing(); }
                if (HasFocus() && _editable) ActivateIME(true); break;
        }
    }
}
