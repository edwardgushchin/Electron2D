namespace Electron2D;

public partial class TextEdit
{
    private Color[] _characterColors = [];
    private float[] _tabs = [32];
    private Rect2 _contentRect;
    private StyleBox? Style => GetThemeStyleBox(_editable ? "normal" : "read_only");
    private void EnsureTextLayout()
    {
        CheckTextEdit(); var font = GetThemeFont("font") ?? throw new InvalidOperationException("TextEdit requires a live font."); var size = GetThemeFontSize("font_size"); var generation = font.GetContentGeneration();
        if (!_layoutDirty && ReferenceEquals(font, _font) && size == _fontSize && generation == _fontGeneration) return;
        if (_building) throw new InvalidOperationException("TextEdit shaping cannot reenter."); _building = true;
        try
        {
            if (!ReferenceEquals(font, _font)) { if (_font != null) { _font.Changed -= _resourceChanged; _font.Disposed -= _resourceDisposed; } _font = font; font.Changed += _resourceChanged; font.Disposed += _resourceDisposed; }
            _fontSize = size; _rowHeight = Math.Max(1, MathF.Ceiling(font.GetHeight(size)) + GetThemeConstant("line_spacing")); _tabs[0] = Math.Max(1, font.GetCharSize(' ', size).X * _tabSize);
            var style = Style; var offset = style?.GetOffset() ?? Vector2.Zero; var margins = style?.GetMinimumSize() ?? Vector2.Zero; var gutterWidth = GetTotalGutterWidth(); var vWidth = _vBar.GetBoundMinimumSize().X; var hHeight = _hBar.GetBoundMinimumSize().Y;
            var direction = _textDirection == global::Electron2D.TextDirection.Inherited ? IsLayoutRTL() ? global::Electron2D.TextDirection.RTL : global::Electron2D.TextDirection.LTR : _textDirection;
            var language = _language.Length > 0 ? _language : TranslationServer.GetOrAddDomain(TranslationDomain).LocaleOverride; if (language.Length == 0) language = TranslationServer.Culture.Name;
            var width = Math.Max(1, Size.X - margins.X - gutterWidth - (_minimapDraw ? _minimapWidth : 0) - vWidth);
            _visual.Clear(); _contentWidth = 0;
            var breaks = _autowrapMode switch { TextAutowrapMode.Arbitrary => TextLineBreakFlags.GraphemeBound, TextAutowrapMode.Word => TextLineBreakFlags.WordBound, TextAutowrapMode.WordSmart => TextLineBreakFlags.WordBound | TextLineBreakFlags.Adaptive, _ => TextLineBreakFlags.None };
            for (var lineIndex = 0; lineIndex < _lines.Count; lineIndex++)
            {
                var line = _lines[lineIndex]; line.Index = lineIndex; line.Y = _visual.Count * _rowHeight; var shown = GetLineWithIME(lineIndex); line.Shown = shown;
                ParseStructuredText(_structuredTextBIDIOverride, _structuredTextBIDIOverrideOptions, shown, line.Contexts);
                if (_indentWrappedLines) breaks |= TextLineBreakFlags.TrimIndent;
                var key = new TextLayoutKey(shown, size, _wrapMode == LineWrappingMode.Boundary ? Math.Max(1, width - GetThemeConstant("wrap_offset")) : 0, HorizontalAlignment.Left, -1, breaks, 0, direction, TextOrientation.Horizontal, true);
                line.Layout.Build(font, key, new(Language: language, TabStops: _tabs, Overrun: 0, BIDIOverride: line.Contexts, ApplyAlignment: false, PreserveControl: _drawControlChars));
                _contentWidth = Math.Max(_contentWidth, line.Layout.UnwrappedWidth);
                if (line.Hidden) continue;
                var rows = Math.Max(1, line.Layout.LineCount); for (var wrap = 0; wrap < rows; wrap++) _visual.Add(new(lineIndex, wrap, _visual.Count * _rowHeight, _rowHeight));
            }
            _contentHeight = _visual.Count * _rowHeight;
            _contentRect = new(offset + new Vector2(IsLayoutRTL() ? vWidth + (_minimapDraw ? _minimapWidth : 0) : gutterWidth, 0), new(Math.Max(1, Size.X - margins.X - gutterWidth - (_minimapDraw ? _minimapWidth : 0) - vWidth), Math.Max(1, Size.Y - margins.Y - hHeight)));
            _vBar.Position = new(IsLayoutRTL() ? offset.X : Size.X - vWidth - (style?.GetContentMargin(Side.Right) ?? 0), offset.Y); _vBar.Size = new(vWidth, _contentRect.Size.Y);
            _hBar.Position = new(_contentRect.Position.X, Size.Y - hHeight - (style?.GetContentMargin(Side.Bottom) ?? 0)); _hBar.Size = new(_contentRect.Size.X, hHeight);
            _vBar.MinValue = 0; _vBar.Step = .001; _vBar.MaxValue = Math.Max(_visual.Count, _contentRect.Size.Y / _rowHeight) + (_scrollPastEndOfFile ? Math.Max(0, _contentRect.Size.Y / _rowHeight - 1) : 0); _vBar.Page = _contentRect.Size.Y / _rowHeight; _vBar.SetValueNoSignal(_scrollVertical); _scrollVertical = _vBar.Value;
            _hBar.MinValue = 0; _hBar.Step = 1; _hBar.MaxValue = Math.Max(_contentWidth, _contentRect.Size.X); _hBar.Page = _contentRect.Size.X; _hBar.SetValueNoSignal(_scrollHorizontal); _scrollHorizontal = (int)_hBar.Value;
            _vBar.Visible = _vBar.MaxValue > _vBar.Page; _hBar.Visible = _wrapMode == LineWrappingMode.None && _hBar.MaxValue > _hBar.Page;
            var placeholder = Atr(_placeholderText); _placeholderLayout.Build(font, new(placeholder, size, width, HorizontalAlignment.Left, -1, breaks, 0, direction, TextOrientation.Horizontal, true), new(Language: language, TabStops: _tabs, Overrun: 0, ApplyAlignment: false));
            RefreshTextures(); PrepareLineColors(); _fontGeneration = generation; _layoutDirty = false;
        }
        finally { _building = false; }
    }
    private void RequestVerticalScroll(double value) { EnsureTextLayout(); _verticalTarget = Math.Clamp(value, 0, Math.Max(0, _vBar.MaxValue - _vBar.Page)); if (!_scrollSmooth) { _scrollVertical = _verticalTarget; _vBar.SetValueNoSignal(_scrollVertical); } UpdateProcessing(); QueueRedraw(); }
    private void RequestHorizontalScroll(int value) { EnsureTextLayout(); _horizontalTarget = (float)Math.Clamp(value, 0, Math.Max(0, _hBar.MaxValue - _hBar.Page)); _scrollHorizontal = (int)_horizontalTarget; _hBar.SetValueNoSignal(_scrollHorizontal); QueueRedraw(); }
    private void UpdateProcessing() { if (!IsDisposed) SetInternalProcessing(IsInsideTree, false); }
    private void ResetBlink() { _blinkTime = 0; _caretVisible = true; if (!IsDisposed) { QueueRedraw(); UpdateProcessing(); } }
    private int VisualIndex(int line, int wrap) { EnsureTextLayout(); for (var i = 0; i < _visual.Count; i++) if (_visual[i].Line == line && _visual[i].Wrap == wrap) return i; return -1; }
    private float CaretX(Line line, int column, int wrap) => RawCaretX(line, DisplayColumn(line.Index, column), wrap);
    private float RawCaretX(Line line, int column, int wrap)
    { if (line.Layout.LineCount == 0) return 0; var row = line.Layout.Lines[Math.Clamp(wrap, 0, line.Layout.LineCount - 1)]; var pair = line.Layout.Carets(column); return column == row.Start ? pair.Trailing ?? pair.Leading ?? 0 : pair.Leading ?? pair.Trailing ?? 0; }
    /// <summary>Returns the configured caret's local pixel position.</summary><param name="caretIndex">Existing caret.</param><returns>Local top-of-row pixel position.</returns>
    public Vector2 GetCaretDrawPos(int caretIndex = 0) { var caret = AtCaret(caretIndex); return GetPosAtLineColumn(caret.Position.Y, caret.Position.X); }
    /// <summary>Returns the pixel row height.</summary><returns>Positive integer height.</returns>
    public int GetLineHeight() { CheckTextEdit(); EnsureTextLayout(); return (int)_rowHeight; }
    /// <summary>Returns measured pixel width of a logical line or its wrap row.</summary><param name="line">Existing line.</param><param name="wrapIndex">Wrap row or -1 for natural width.</param><returns>Ceiled width.</returns>
    public int GetLineWidth(int line, int wrapIndex = -1) { var value = AtLine(line); EnsureTextLayout(); if (wrapIndex == -1) return (int)MathF.Ceiling(value.Layout.UnwrappedWidth); if ((uint)wrapIndex >= value.Layout.LineCount) throw new ArgumentOutOfRangeException(nameof(wrapIndex)); return (int)MathF.Ceiling(value.Layout.Lines[wrapIndex].Width); }
    /// <summary>Returns the number of additional wrap rows.</summary><param name="line">Existing line.</param><returns>Zero when unwrapped.</returns>
    public int GetLineWrapCount(int line) { var value = AtLine(line); EnsureTextLayout(); return Math.Max(0, value.Layout.LineCount - 1); }
    /// <summary>Returns whether a line occupies multiple visual rows.</summary><param name="line">Existing line.</param><returns>Wrap state.</returns>
    public bool IsLineWrapped(int line) => GetLineWrapCount(line) > 0;
    /// <summary>Returns the wrap row containing a scalar column.</summary><param name="line">Existing line.</param><param name="column">Scalar column.</param><returns>Wrap row.</returns>
    public int GetLineWrapIndexAtColumn(int line, int column) { var value = AtLine(line); ValidatePosition(line, column); EnsureTextLayout(); column = DisplayColumn(line, column); for (var i = 0; i < value.Layout.LineCount; i++) if (column < value.Layout.Lines[i].End || i == value.Layout.LineCount - 1) return i; return 0; }
    /// <summary>Returns copied text for each wrap row.</summary><param name="line">Existing line.</param><returns>Independent strings in wrap order.</returns>
    public string[] GetLineWrappedText(int line) { var value = AtLine(line); EnsureTextLayout(); return value.Layout.LineCount == 0 ? [""] : value.Layout.Lines.Select(row => Slice(value.Shown, row.Start, row.End)).ToArray(); }
    /// <summary>Returns the local pixel coordinate of a logical scalar position.</summary><param name="line">Existing line.</param><param name="column">Scalar column.</param><returns>Local coordinate, or (-1,-1) for a hidden line.</returns>
    public Vector2i GetPosAtLineColumn(int line, int column) { var value = AtLine(line); ValidatePosition(line, column); EnsureTextLayout(); if (value.Hidden) return new(-1, -1); var wrap = GetLineWrapIndexAtColumn(line, column); var index = VisualIndex(line, wrap); return new((int)(_contentRect.Position.X + CaretX(value, column, wrap) - _scrollHorizontal), (int)(_contentRect.Position.Y + index * _rowHeight - _scrollVertical * _rowHeight)); }
    /// <summary>Returns the local character/caret rectangle.</summary><param name="line">Existing line.</param><param name="column">Scalar column.</param><returns>Integer pixel rectangle.</returns>
    public Rect2i GetRectAtLineColumn(int line, int column) { var point = GetPosAtLineColumn(line, column); var next = column < AtLine(line).Scalars ? GetPosAtLineColumn(line, column + 1) : point + new Vector2i(1, 0); return new(point, new(Math.Max(1, Math.Abs(next.X - point.X)), GetLineHeight())); }
    /// <summary>Returns the logical column/line nearest a local pixel point.</summary><param name="position">Local pixels.</param><param name="clampLine">Clamps out-of-content rows.</param><param name="clampColumn">Clamps out-of-line columns.</param><returns>Scalar column/line, with -1 components for unclamped misses.</returns>
    public Vector2i GetLineColumnAtPos(Vector2i position, bool clampLine = true, bool clampColumn = true)
    {
        CheckTextEdit(); EnsureTextLayout(); var index = (int)MathF.Floor((position.Y - _contentRect.Position.Y) / _rowHeight + (float)_scrollVertical); if (_visual.Count == 0) return Vector2i.Zero; if (!clampLine && (index < 0 || index >= _visual.Count)) return new(-1, -1); index = Math.Clamp(index, 0, _visual.Count - 1); var row = _visual[index]; var line = _lines[row.Line]; var wrap = line.Layout.LineCount == 0 ? default : line.Layout.Lines[row.Wrap]; var x = position.X - _contentRect.Position.X + _scrollHorizontal;
        if (!clampColumn && (x < 0 || x > wrap.Width)) return new(-1, row.Line); var best = wrap.Start; var distance = float.MaxValue;
        for (var column = wrap.Start; column <= wrap.End; column++) { if (!_caretMidGrapheme && line.Layout.ClosestGrapheme(column) != column) continue; var next = MathF.Abs(RawCaretX(line, column, row.Wrap) - x); if (next < distance) { best = column; distance = next; } }
        return new(SourceColumn(row.Line, best), row.Line);
    }
    /// <summary>Returns the word nearest a local pixel point.</summary><param name="position">Local point.</param><returns>Copied word text.</returns>
    public string GetWordAtPos(Vector2 position) { var point = GetLineColumnAtPos((Vector2i)position, false, false); if (point.X < 0 || point.Y < 0) return ""; var range = WordRange(point); return Slice(_lines[point.Y].Text, range.From, range.To); }
    /// <summary>Returns logical/scalar grapheme successor.</summary><param name="line">Existing line.</param><param name="column">Scalar column.</param><returns>Next grapheme boundary column.</returns>
    public int GetNextCompositeCharacterColumn(int line, int column) { ValidatePosition(line, column); EnsureTextLayout(); return _lines[line].Layout.NextGrapheme(column); }
    /// <summary>Returns logical/scalar grapheme predecessor.</summary><param name="line">Existing line.</param><param name="column">Scalar column.</param><returns>Previous grapheme boundary column.</returns>
    public int GetPreviousCompositeCharacterColumn(int line, int column) { ValidatePosition(line, column); EnsureTextLayout(); return _lines[line].Layout.PreviousGrapheme(column); }
    /// <summary>Returns the total unhidden visual row count.</summary><returns>Visual rows including wrapping.</returns>
    public int GetTotalVisibleLineCount() { CheckTextEdit(); EnsureTextLayout(); return _visual.Count; }
    /// <summary>Returns the available viewport row count.</summary><returns>Fully visible row capacity.</returns>
    public int GetVisibleLineCount() { CheckTextEdit(); EnsureTextLayout(); return Math.Max(1, (int)(_contentRect.Size.Y / _rowHeight)); }
    /// <summary>Returns visual rows in an inclusive logical line interval.</summary><param name="fromLine">Start line.</param><param name="toLine">End line.</param><returns>Unhidden wrapped row count.</returns>
    public int GetVisibleLineCountInRange(int fromLine, int toLine) { AtLine(fromLine); AtLine(toLine); EnsureTextLayout(); var count = 0; foreach (var row in _visual) if (row.Line >= Math.Min(fromLine, toLine) && row.Line <= Math.Max(fromLine, toLine)) count++; return count; }
    /// <summary>Returns the first viewport logical line.</summary><returns>Logical line index.</returns>
    public int GetFirstVisibleLine() { EnsureTextLayout(); return _visual.Count == 0 ? 0 : _visual[Math.Clamp((int)_scrollVertical, 0, _visual.Count - 1)].Line; }
    /// <summary>Returns the last completely visible logical line.</summary><returns>Logical line index.</returns>
    public int GetLastFullVisibleLine() { EnsureTextLayout(); return _visual.Count == 0 ? 0 : _visual[Math.Clamp((int)_scrollVertical + GetVisibleLineCount() - 1, 0, _visual.Count - 1)].Line; }
    /// <summary>Returns the last completely visible wrap index.</summary><returns>Wrap row index.</returns>
    public int GetLastFullVisibleLineWrapIndex() { EnsureTextLayout(); return _visual.Count == 0 ? 0 : _visual[Math.Clamp((int)_scrollVertical + GetVisibleLineCount() - 1, 0, _visual.Count - 1)].Wrap; }
    /// <summary>Returns the last unhidden logical line.</summary><returns>Logical line index.</returns>
    public int GetLastUnhiddenLine() { CheckTextEdit(); for (var i = _lines.Count - 1; i >= 0; i--) if (!_lines[i].Hidden) return i; return 0; }
    /// <summary>Returns logical-line offset needed to traverse unhidden lines.</summary><param name="line">Starting line.</param><param name="visibleAmount">Signed unhidden line count.</param><returns>Nonnegative count, including the starting line.</returns>
    public int GetNextVisibleLineOffsetFrom(int line, int visibleAmount) { AtLine(line); var at = line; var remaining = Math.Abs(visibleAmount); var direction = Math.Sign(visibleAmount); while (remaining > 0 && at + direction >= 0 && at + direction < _lines.Count) { at += direction; if (!_lines[at].Hidden) remaining--; } return Math.Abs(visibleAmount); }
    /// <summary>Returns wrap/logical offsets for signed visual-row traversal.</summary><param name="line">Starting line.</param><param name="wrapIndex">Starting wrap row.</param><param name="visibleAmount">Signed row count.</param><returns>Logical-line offset/target wrap index.</returns>
    public Vector2i GetNextVisibleLineIndexOffsetFrom(int line, int wrapIndex, int visibleAmount) { AtLine(line); EnsureTextLayout(); var start = VisualIndex(line, wrapIndex); if (start < 0) return default; var target = _visual[Math.Clamp(start + Math.Sign(visibleAmount) * Math.Max(0, Math.Abs(visibleAmount) - 1), 0, _visual.Count - 1)]; return visibleAmount == 0 ? Vector2i.Zero : _wrapMode == LineWrappingMode.None ? new(Math.Abs(visibleAmount), 0) : new(Math.Abs(target.Line - line) + 1, target.Wrap); }
    /// <summary>Returns the vertical scroll position corresponding to a logical/wrapped row.</summary><param name="line">Existing line.</param><param name="wrapIndex">Existing wrap row.</param><returns>Fractional-row scroll coordinate.</returns>
    public double GetScrollPosForLine(int line, int wrapIndex = 0) { AtLine(line); return Math.Max(0, VisualIndex(line, wrapIndex)); }
    /// <summary>Places a logical/wrapped row at the viewport start.</summary><param name="line">Existing line.</param><param name="wrapIndex">Wrap row.</param>
    public void SetLineAsFirstVisible(int line, int wrapIndex = 0) { EnsureTextMutable(); ScrollVertical = GetScrollPosForLine(line, wrapIndex); }
    /// <summary>Places a logical/wrapped row at the viewport center.</summary><param name="line">Existing line.</param><param name="wrapIndex">Wrap row.</param>
    public void SetLineAsCenterVisible(int line, int wrapIndex = 0) { EnsureTextMutable(); ScrollVertical = GetScrollPosForLine(line, wrapIndex) - GetVisibleLineCount() / 2d; }
    /// <summary>Places a logical/wrapped row at the viewport end.</summary><param name="line">Existing line.</param><param name="wrapIndex">Wrap row.</param>
    public void SetLineAsLastVisible(int line, int wrapIndex = 0) { EnsureTextMutable(); ScrollVertical = GetScrollPosForLine(line, wrapIndex) - GetVisibleLineCount() + 1; }
    /// <summary>Scrolls only as needed to reveal a caret.</summary><param name="caretIndex">Existing caret.</param>
    public void AdjustViewportToCaret(int caretIndex = 0) { var caret = AtCaret(caretIndex); EnsureTextLayout(); var row = VisualIndex(caret.Position.Y, GetCaretWrapIndex(caretIndex)); if (row < _scrollVertical) RequestVerticalScroll(row); else if (row >= _scrollVertical + GetVisibleLineCount()) RequestVerticalScroll(row - GetVisibleLineCount() + 1); var x = CaretX(_lines[caret.Position.Y], caret.Position.X, GetCaretWrapIndex(caretIndex)); if (x < _scrollHorizontal) RequestHorizontalScroll((int)x); else if (x > _scrollHorizontal + _contentRect.Size.X - 1) RequestHorizontalScroll((int)(x - _contentRect.Size.X + 1)); }
    /// <summary>Centers the viewport vertically on a caret.</summary><param name="caretIndex">Existing caret.</param>
    public void CenterViewportToCaret(int caretIndex = 0) { SetLineAsCenterVisible(GetCaretLine(caretIndex), GetCaretWrapIndex(caretIndex)); RequestHorizontalScroll(0); }
    /// <summary>Reports whether any visual row of a logical line intersects the viewport.</summary><param name="line">Existing line.</param><returns>Visibility state.</returns>
    public bool IsLineInViewport(int line) { AtLine(line); EnsureTextLayout(); var first = VisualIndex(line, 0); return first >= 0 && first + GetLineWrapCount(line) >= _scrollVertical && first < _scrollVertical + GetVisibleLineCount(); }
    /// <summary>Returns the stable borrowed vertical scrollbar.</summary><returns>Required owned scrollbar.</returns>
    public VScrollBar GetVScrollBar() { CheckTextEdit(); return _vBar; }
    /// <summary>Returns the stable borrowed horizontal scrollbar.</summary><returns>Required owned scrollbar.</returns>
    public HScrollBar GetHScrollBar() { CheckTextEdit(); return _hBar; }
    /// <summary>Returns the current local mouse coordinates, mirrored for RTL layout.</summary><returns>Current local pointer position.</returns>
    public Vector2 GetLocalMousePos() { CheckTextEdit(); var point = GetLocalMousePosition(); return IsLayoutRTL() ? new(Size.X - point.X, point.Y) : point; }
    /// <summary>Returns logical line nearest a minimap local point.</summary><param name="position">Local minimap point.</param><returns>Logical line.</returns>
    public int GetMinimapLineAtPos(Vector2i position) { EnsureTextLayout(); if (_visual.Count == 0) return 0; var i = Math.Clamp((int)((position.Y - _contentRect.Position.Y) / Math.Max(1, _contentRect.Size.Y) * _visual.Count), 0, _visual.Count - 1); return _visual[i].Line; }
    /// <summary>Returns visual rows represented by the minimap.</summary><returns>Minimap row capacity.</returns>
    public int GetMinimapVisibleLines() { EnsureTextLayout(); return Math.Max(0, (int)(_contentRect.Size.Y / Math.Max(1, GetThemeConstant("minimap_char_size")))); }
}
