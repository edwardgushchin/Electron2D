namespace Electron2D;

public partial class TextEdit
{
    internal int AccessibilityTextVersion => _version;
    internal void AccessibilitySelect(int fromLine, int from, int toLine, int to)
    {
        EnsureTextMutable(); if (!_selectingEnabled) return; ValidatePosition(fromLine, from); ValidatePosition(toLine, to);
        RemoveSecondaryCarets(); var caret = _carets[0]; caret.Origin = new(from, fromLine); caret.Position = new(to, toLine); caret.Selected = caret.Position != caret.Origin; CaretsChanged(); AdjustViewportToCaret();
    }
    internal void AccessibilityReplace(string text)
    {
        EnsureTextMutable(); if (!_editable) return; CancelIME(); InsertTextAtCaret(text, 0); AdjustViewportToCaret();
    }
    internal void AccessibilityReplaceRange(int start, int end, string text)
    {
        EnsureTextMutable(); if (!_editable) return; var from = PositionAt(start); var to = PositionAt(end);
        RemoveSecondaryCarets(); var caret = _carets[0]; caret.Origin = from; caret.Position = to; caret.Selected = from != to; AccessibilityReplace(text);
    }
    internal void AccessibilitySelections(List<(int Start, int End, int Caret)> output)
    {
        for (var i = 0; i < _carets.Count; i++) if (_carets[i].Selected) output.Add((Offset(SelectionStart(_carets[i])), Offset(SelectionEnd(_carets[i])), i));
    }
    internal bool AccessibilityNativeSelection(int caretIndex, int start, int end, bool remove = false)
    {
        EnsureTextMutable(); if (!_selectingEnabled) return false;
        if (remove) { Deselect(caretIndex); return true; }
        var from = PositionAt(start); var to = PositionAt(end);
        if (caretIndex < 0) { caretIndex = AddCaret(to.Y, to.X); if (caretIndex < 0) return false; }
        var caret = AtCaret(caretIndex); caret.Origin = from; caret.Position = to; caret.Selected = from != to; CaretsChanged(); return true;
    }
    internal Rect2 AccessibilityCharacterRect(int line, int column)
    {
        EnsureTextLayout(); var value = AtLine(line); column = Math.Min(column, value.Scalars);
        if (value.Hidden) return default;
        var wrap = GetLineWrapIndexAtColumn(line, column); var row = value.Layout.Lines.Count > wrap ? value.Layout.Lines[wrap] : default;
        var rect = column < value.Scalars ? value.Layout.AccessibilityScalarBounds(DisplayColumn(line, column)) : new Rect2(CaretX(value, column, wrap), row.CrossOffset, 0, _rowHeight);
        return new(new Vector2(_contentRect.Position.X + rect.Position.X - _scrollHorizontal, _contentRect.Position.Y + (float)((VisualIndex(line, wrap) - _scrollVertical) * _rowHeight)), new Vector2(rect.Size.X, _rowHeight));
    }
    internal Rect2 AccessibilityLineRect(int line)
    {
        EnsureTextLayout(); var value = AtLine(line); if (value.Hidden) return default;
        return new(new Vector2(_contentRect.Position.X - _scrollHorizontal, _contentRect.Position.Y + (float)((VisualIndex(line, 0) - _scrollVertical) * _rowHeight)), new Vector2(value.Layout.Size.X, Math.Max(1, value.Layout.LineCount) * _rowHeight));
    }
    internal AccessibilityServer.TextStyle AccessibilityTextStyle(int line, int column)
    { EnsureTextLayout(); var style = AtLine(line).Layout.ScalarStyle(DisplayColumn(line, Math.Min(column, AtLine(line).Scalars))); return new(style.Start, style.End, style.Family, style.Size, style.Weight, style.Italic, style.Language, style.Direction); }
    internal bool AccessibilityScrollText(int line, int column, uint hint, Vector2? point)
    {
        EnsureTextMutable(); EnsureTextLayout(); if (AtLine(line).Hidden) return false;
        var wrap = GetLineWrapIndexAtColumn(line, column); var row = VisualIndex(line, wrap); var rect = AccessibilityCharacterRect(line, column);
        if (point is { } p)
        {
            var local = (GetViewport()!.GetScreenTransform() * GetGlobalTransformWithCanvas()).AffineInverse() * p;
            ScrollHorizontal = Math.Max(0, (int)(_scrollHorizontal + rect.Position.X - local.X)); ScrollVertical = _scrollVertical + (rect.Position.Y - local.Y) / _rowHeight;
        }
        else
        {
            if (hint is 0 or 2) SetLineAsFirstVisible(line, wrap);
            else if (hint is 1 or 3) SetLineAsLastVisible(line, wrap);
            else if (row < _scrollVertical) ScrollVertical = row; else if (row >= _scrollVertical + GetVisibleLineCount()) ScrollVertical = row - GetVisibleLineCount() + 1;
            if (hint is 0 or 4) ScrollHorizontal = (int)Math.Max(0, _scrollHorizontal + rect.Position.X - _contentRect.Position.X);
            else if (hint is 1 or 5) ScrollHorizontal = (int)Math.Max(0, _scrollHorizontal + rect.End.X - _contentRect.End.X);
            else if (rect.Position.X < _contentRect.Position.X) ScrollHorizontal += (int)(rect.Position.X - _contentRect.Position.X); else if (rect.End.X > _contentRect.End.X) ScrollHorizontal += (int)(rect.End.X - _contentRect.End.X);
        }
        return true;
    }
}
