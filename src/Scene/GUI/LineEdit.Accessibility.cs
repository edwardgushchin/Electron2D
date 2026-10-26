namespace Electron2D;

public partial class LineEdit
{
    private string? _accessibilitySecretSource, _accessibilitySecretMask;
    private string _accessibilitySecretCharacter = "";
    internal string AccessibilityTextValue
    {
        get
        {
            if (!_secret) return _text;
            if (_accessibilitySecretSource != _text || _accessibilitySecretCharacter != _secretCharacter)
            {
                _accessibilitySecretSource = _text; _accessibilitySecretCharacter = _secretCharacter;
                _accessibilitySecretMask = string.Concat(Enumerable.Repeat(_secretCharacter.Length == 0 ? "•" : _secretCharacter, ScalarCount(_text)));
            }
            return _accessibilitySecretMask ?? "";
        }
    }
    internal void AccessibilitySelect(int anchor, int focus)
    {
        EnsureMutable(); if (!_selectingEnabled) return; var count = ScalarCount(_text);
        if ((uint)anchor > count || (uint)focus > count) throw new ArgumentOutOfRangeException(nameof(anchor));
        Deselect(); CaretColumn = focus;
        if (anchor != focus) { Select(Math.Min(anchor, focus), Math.Max(anchor, focus)); _selectionAnchor = anchor; }
    }
    internal void AccessibilityReplace(string text)
    {
        EnsureMutable(); if (!_editable) return;
        if (text.Length != 0) { UserInsert(text); return; }
        if (_selecting) { DeleteRange(_selectionFrom, _selectionTo); ChangedByUser(); }
    }
    internal void AccessibilityReplaceRange(int start, int end, string text)
    {
        EnsureMutable(); if (!_editable) return;
        var count = ScalarCount(_text); if (start < 0 || end < start || end > count) throw new ArgumentOutOfRangeException(nameof(start));
        _caret = end; _selectionAnchor = start; _selectionFrom = start; _selectionTo = end; _selecting = start != end;
        AccessibilityReplace(text.Replace("\r", "").Replace("\n", "").Replace("\t", ""));
    }
    internal Rect2 AccessibilityCharacterRect(int column)
    {
        EnsureLayout(); var content = ContentRect(); var top = content.Position.Y + Math.Max(0, (content.Size.Y - Math.Max(_layout.Size.Y, _font!.GetHeight(_fontSize))) / 2);
        var displayColumn = column >= _caret ? column + ScalarCount(_ime) : column;
        var local = displayColumn < _layout.CharacterCount && !IsPlaceholder ? _layout.AccessibilityScalarBounds(displayColumn) : new Rect2(_layout.CaretX(Math.Min(displayColumn, _layout.CharacterCount), _inputDirection), 0, 0, _font!.GetHeight(_fontSize));
        return new(local.Position + new Vector2(TextOrigin(content), top), local.Size);
    }
    internal Rect2 AccessibilityLineRect()
    {
        EnsureLayout(); var content = ContentRect(); var height = Math.Max(_layout.Size.Y, _font!.GetHeight(_fontSize));
        return new(new Vector2(TextOrigin(content), content.Position.Y + Math.Max(0, (content.Size.Y - height) / 2)), new Vector2(IsPlaceholder ? 0 : _layout.Size.X, height));
    }
    internal AccessibilityServer.TextStyle AccessibilityTextStyle(int column)
    { EnsureLayout(); var style = _layout.ScalarStyle(column >= _caret ? column + ScalarCount(_ime) : column); return new(style.Start, style.End, style.Family, style.Size, style.Weight, style.Italic, style.Language, style.Direction); }
    internal bool AccessibilityScrollText(int column, uint hint, Vector2? point)
    {
        EnsureLayout(); var content = ContentRect(); var x = _layout.CaretX(column, _inputDirection);
        var localPoint = point is { } p ? (GetViewport()!.GetScreenTransform() * GetGlobalTransformWithCanvas()).AffineInverse() * p : (Vector2?)null;
        if (localPoint is { } at) _scroll += at.X - AccessibilityCharacterRect(column).Position.X;
        else if (hint is 0 or 2 or 4) _scroll = -x;
        else if (hint is 1 or 3 or 5) _scroll = content.Size.X - x;
        else { var rect = AccessibilityCharacterRect(column); if (rect.Position.X < content.Position.X) _scroll += content.Position.X - rect.Position.X; else if (rect.End.X > content.End.X) _scroll -= rect.End.X - content.End.X; }
        _scroll = Math.Clamp(_scroll, Math.Min(0, content.Size.X - _layout.Size.X), 0); QueueRedraw(); return true;
    }
}
