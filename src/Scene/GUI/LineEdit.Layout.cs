namespace Electron2D;

public partial class LineEdit
{
    private bool _building;
    private bool IsPlaceholder => _text.Length == 0 && _ime.Length == 0;
    private void EnsureLayout()
    {
        if (_building) throw new InvalidOperationException("LineEdit shaping cannot be re-entered.");
        var font = GetThemeFont("font") ?? throw new InvalidOperationException("LineEdit requires a font.");
        var size = GetThemeFontSize("font_size"); var generation = font.GetContentGeneration();
        if (!_dirty && ReferenceEquals(font, _font) && _fontSize == size && generation == _fontGeneration) return;
        _building = true;
        try
        {
            if (!ReferenceEquals(_font, font)) { if (_font is not null) { _font.Changed -= _resourceChanged; _font.Disposed -= _resourceDisposed; } _font = font; font.Changed += _resourceChanged; font.Disposed += _resourceDisposed; }
            _fontSize = size;
            var language = _language;
            if (language.Length == 0) language = TranslationServer.GetOrAddDomain(TranslationDomain).LocaleOverride;
            if (language.Length == 0) language = TranslationServer.Culture.Name;
            if (language.Length == 0) language = TranslationServer.GetToolLocale();
            var direction = _textDirection == TextDirection.Inherited ? IsLayoutRTL() ? TextDirection.RTL : TextDirection.LTR : _textDirection;
            var source = _text.Insert(UTF16Index(_text, _caret), _ime);
            var shown = IsPlaceholder ? Atr(_placeholderText) : _secret ? string.Concat(Enumerable.Repeat(_secretCharacter.Length == 0 ? "•" : _secretCharacter, ScalarCount(source))) : source;
            ParseStructuredText(_structuredTextBIDIOverride, _structuredOptions, shown, _contexts);
            var sourceKey = new TextLayoutKey(shown, size, 0, HorizontalAlignment.Left, 1, 0, 0, direction, TextOrientation.Horizontal);
            _sourceLayout.Build(font, sourceKey, new(Language: language, Overrun: 0, BIDIOverride: _contexts, ApplyAlignment: false, PreserveControl: _drawControlChars));
            var layoutKey = sourceKey with { Text = shown };
            if (_alignment == HorizontalAlignment.Fill) layoutKey = layoutKey with { Width = Math.Max(_sourceLayout.Size.X, ContentRect().Size.X), Alignment = HorizontalAlignment.Fill, Justification = TextJustificationFlags.WordBound | TextJustificationFlags.Kashida };
            _layout.Build(font, layoutKey, new(Language: language, Overrun: 0, BIDIOverride: _contexts, ApplyAlignment: false, PreserveControl: _drawControlChars));
            _fontGeneration = font.GetContentGeneration(); _dirty = false;
        }
        finally { _building = false; }
    }
    private StyleBox? FieldStyle => GetThemeStyleBox(_editable ? "normal" : "read_only");
    private bool IsClearIcon => _clearButtonEnabled && _editable && _text.Length > 0;
    private Texture? FieldIcon => IsClearIcon ? GetThemeIcon("clear") : _rightIcon is { IsDisposed: false } ? _rightIcon : null;
    private Vector2 IconSize(Texture? icon)
    {
        if (icon is null || icon.IsDisposed) return default; var natural = icon.GetSize(); if (natural.Y <= 0) return default;
        if (_iconExpandMode == LineEditIconExpandMode.FitToText) return Vector2.One * _font!.GetHeight(_fontSize);
        if (_iconExpandMode != LineEditIconExpandMode.FitToLineEdit) return natural;
        var fit = natural * (Size.Y / natural.Y); if (fit.X > Size.X && natural.X > 0) fit = natural * (Size.X / natural.X);
        return fit * _rightIconScale;
    }
    private Rect2 ContentRect()
    {
        var style = FieldStyle; var left = style?.GetContentMargin(Side.Left) ?? 0; var right = style?.GetContentMargin(Side.Right) ?? 0;
        var top = style?.GetContentMargin(Side.Top) ?? 0; var bottom = style?.GetContentMargin(Side.Bottom) ?? 0;
        var icon = IconSize(FieldIcon); var reserve = icon.X > 0 ? icon.X + GetThemeConstant("h_separation") : 0;
        if (IsLayoutRTL()) left += reserve; else right += reserve;
        return new(new(left, top), new(Math.Max(0, Size.X - left - right), Math.Max(0, Size.Y - top - bottom)));
    }
    private Rect2 IconRect()
    {
        var size = IconSize(FieldIcon); var style = FieldStyle;
        var x = IsLayoutRTL() ? style?.GetContentMargin(Side.Left) ?? 0 : Size.X - (style?.GetContentMargin(Side.Right) ?? 0) - size.X;
        return new(new(x, (Size.Y - size.Y) / 2), size);
    }
    private float TextOrigin(Rect2 content)
    {
        var surplus = Math.Max(0, content.Size.X - _layout.Size.X); var right = _alignment == HorizontalAlignment.Right ^ IsLayoutRTL();
        var offset = _scroll != 0 ? 0 : _alignment == HorizontalAlignment.Center ? MathF.Floor(surplus / 2) : right ? surplus : 0;
        return content.Position.X + offset + _scroll;
    }
    private int DisplayCaret => IsPlaceholder ? 0 : _caret + ScalarCount(_ime);
    private void FitCaret()
    {
        if (!IsInsideTree) { _scroll = 0; return; }
        EnsureLayout(); var content = ContentRect();
        var width = Math.Max(0, content.Size.X - GetThemeConstant("caret_width")); var x = _layout.CaretX(DisplayCaret, _inputDirection);
        _scroll = Math.Clamp(_scroll, Math.Min(0, width - _layout.Size.X), 0);
        if (x + _scroll < 0) _scroll = -x; else if (x + _scroll > width) _scroll = width - x;
        QueueRedraw();
    }
    private int HitColumn(Vector2 position)
    {
        EnsureLayout();
        var column = _layout.HitColumn(position.X - TextOrigin(ContentRect()), true);
        return _caretMidGrapheme ? Math.Min(column, ScalarCount(_text)) : _sourceLayout.ClosestGrapheme(Math.Min(column, ScalarCount(_text)));
    }
    /// <inheritdoc />
    protected override Vector2 OnGetMinimumSize()
    {
        EnsureLayout(); var style = FieldStyle; var margins = (GetThemeStyleBox("normal")?.GetMinimumSize() ?? default).Max(GetThemeStyleBox("read_only")?.GetMinimumSize() ?? default);
        var width = GetThemeConstant("minimum_character_width") * _font!.GetCharSize('W', _fontSize).X;
        if (_expandToTextLength) width = Math.Max(width, _layout.Size.X + GetThemeConstant("caret_width"));
        var icon = IconSize(_rightIcon); if (_clearButtonEnabled) icon = icon.Max(IconSize(GetThemeIcon("clear"))); width += Math.Max(0, icon.X);
        return margins + new Vector2(width, Math.Max(Math.Max(_layout.Size.Y, _font.GetHeight(_fontSize)), icon.Y));
    }
    private void ReleaseIconResidency() { _residentIcon?.ReleaseRendererCacheResidency(); _residentIcon = null; }
    private void RetainIcon(Texture? icon)
    {
        if (ReferenceEquals(icon, _residentIcon)) return; ReleaseIconResidency();
        if (IsInsideTree && icon is { IsDisposed: false }) { icon.AcquireRendererCacheResidency(); _residentIcon = icon; }
    }
    private void DrawField()
    {
        EnsureLayout(); var bounds = new Rect2(Vector2.Zero, Size); if (!_flat) FieldStyle?.Draw(this, bounds);
        if (HasFocus(ignoreHiddenFocus: true)) GetThemeStyleBox("focus")?.Draw(this, bounds);
        var content = ContentRect(); var x = TextOrigin(content); var textHeight = Math.Max(_layout.Size.Y, _font!.GetHeight(_fontSize)); var y = content.Position.Y + Math.Max(0, (content.Size.Y - textHeight) / 2);
        var baseline = new Vector2(x, y + _layout.FirstAscent);
        if (_selecting && !IsPlaceholder)
        {
            _layout.SelectionRanges(_selectionFrom, _selectionTo, _selectionRanges);
            foreach (var range in _selectionRanges) DrawClippedRect(new(new(x + range.X, y), new(range.Y - range.X, _layout.Size.Y)), GetThemeColor("selection_color"), content);
        }
        var color = GetThemeColor(IsPlaceholder ? "font_placeholder_color" : _editable ? "font_color" : "font_uneditable_color");
        var outline = GetThemeConstant("outline_size");
        if (outline > 0) _layout.Draw(this, baseline, GetThemeColor("font_outline_color"), outline, outlinePass: true, clipRect: content);
        _layout.Draw(this, baseline, color, clipRect: content);
        if (_selecting && !IsPlaceholder)
            foreach (var range in _selectionRanges)
                _layout.Draw(this, baseline, GetThemeColor("font_selected_color"), clipRect: content.Intersection(new(new(x + range.X, y), new(range.Y - range.X, _layout.Size.Y))));
        if (_ime.Length > 0)
        {
            _layout.SelectionRanges(_caret, _caret + ScalarCount(_ime), _selectionRanges);
            foreach (var range in _selectionRanges) DrawClippedRect(new(new(x + range.X, y + _layout.Size.Y - 1), new(range.Y - range.X, 1)), GetThemeColor("caret_color"), content);
        }
        if (_ime.Length > 0 && _imeSelection.Y > 0)
        {
            _layout.SelectionRanges(_caret + _imeSelection.X, _caret + _imeSelection.X + _imeSelection.Y, _selectionRanges);
            foreach (var range in _selectionRanges) DrawClippedRect(new(new(x + range.X, y + textHeight - 3), new(range.Y - range.X, 3)), GetThemeColor("caret_color"), content);
        }
        if (_editable && (_editing || _caretForceDisplayed) && (!_caretBlink || _caretVisible))
        {
            var carets = _layout.Carets(DisplayCaret); var primary = _layout.CaretX(DisplayCaret, _inputDirection); var caretWidth = Math.Max(1, GetThemeConstant("caret_width"));
            DrawClippedRect(new(new(x + primary, y), new(caretWidth, textHeight)), GetThemeColor("caret_color"), content);
            if (carets.Leading is { } leading && carets.Trailing is { } trailing && !Mathf.IsEqualApprox(leading, trailing))
                DrawClippedRect(new(new(x + (Mathf.IsEqualApprox(primary, leading) ? trailing : leading), y), new(caretWidth, Math.Max(1, textHeight / 2))), GetThemeColor("caret_color"), content);
        }
        var icon = FieldIcon; RetainIcon(icon); if (icon is { IsDisposed: false }) DrawTextureRect(icon, IconRect(), false, GetThemeColor(!IsClearIcon ? "right_icon_modulate" : _clearPressed ? "clear_button_color_pressed" : "clear_button_color"));
    }
    private void DrawClippedRect(Rect2 rect, Color color, Rect2 clip) { rect = rect.Intersection(clip); if (rect.HasArea()) DrawRect(rect, color); }
    /// <inheritdoc />
    protected override void OnNotification(int what)
    {
        if (what == NotificationDraw) { base.OnNotification(what); DrawField(); return; }
        List<Exception>? errors = null; try { base.OnNotification(what); } catch (Exception error) { CollectException(ref errors, error); }
        if (!IsDisposed) try
            {
                switch (what)
                {
                    case NotificationEnterTree: SetInternalProcessing(true, false); Invalidate(); break;
                    case NotificationDragEnd: FinishTextDrag(); break;
                    case NotificationExitTree: ReleaseIconResidency(); _selectAllOnRelease = false; _editing = false; _pointerSelecting = false; _ime = ""; SetInternalProcessing(false, false); break;
                    case NotificationFocusEnter: var was = _editing; Edit(); if (was != _editing) EditingToggled?.Invoke(_editing); break;
                    case NotificationFocusExit: var editing = _editing; Unedit(); _pointerSelecting = false; if (editing != _editing) EditingToggled?.Invoke(_editing); break;
                    case NotificationResized: _dirty = true; FitCaret(); break;
                    case NotificationThemeChanged:
                    case NotificationTranslationChanged:
                    case NotificationLayoutDirectionChanged: Invalidate(); break;
                    case NotificationInternalProcess:
                        if (Interlocked.Exchange(ref _resourcePending, 0) != 0) Invalidate();
                        if (_font is { IsDisposed: false } && _font.GetContentGeneration() != _fontGeneration) Invalidate();
                        if (_caretBlink && _editing) { _blinkTime += (float)ProcessDeltaTime; if (_blinkTime >= _caretBlinkInterval) { _blinkTime = (float)(_blinkTime % _caretBlinkInterval); _caretVisible = !_caretVisible; QueueRedraw(); } }
                        if (_editing) UpdateIMEPosition(); break;
                }
            }
            catch (Exception error) { CollectException(ref errors, error); }
        ThrowCollected("LineEdit notification callbacks failed.", errors);
    }
    private void UpdateIMEPosition()
    {
        if (DisplayServer.Service is not { } display || !display.HasFeatureCore(DisplayServer.Feature.Ime)) return;
        var window = GetWindow();
        while (window?.Embedder is { } host) window = host.GetWindow(); if (window is null || window.GetWindowID() == DisplayServer.InvalidWindowId) return;
        EnsureLayout(); var content = ContentRect(); var local = new Vector2(TextOrigin(content) + _layout.CaretX(DisplayCaret, _inputDirection), (Size.Y + _font!.GetHeight(_fontSize)) / 2);
        var position = GetViewport()!.GetScreenTransform() * GetGlobalTransformWithCanvas() * local; display.WindowSetIMEActiveCore(true, window.GetWindowID()); display.WindowSetIMEPositionCore(new((int)position.X, (int)position.Y), window.GetWindowID());
    }
}
