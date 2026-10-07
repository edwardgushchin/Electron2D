using System.Globalization;
using System.Text;
namespace Electron2D;

public partial class CodeEdit
{
    private Font? _codeFont;
    private long _codeFontGeneration = -1;
    private int _codeFontSize;
    private int[] _foldEnds = [];
    private bool[] _regionHeaders = [];
    private Vector2 _codeMouse;
    private float[] _completionPrefixWidths = [];
    private Rect2[] _hintSpans = [];
    private float _hintX = float.NaN, _completionBaseWidth, _hintPrefixWidth;
    private int _completionVisibleLines = 1;
    private bool _completionScrollPressed;
    private float _hoverWidth;
    private void EnsureCodeLayout()
    {
        CheckCode(); var font = GetThemeFont("font") ?? throw new InvalidOperationException("CodeEdit needs a live font."); var size = GetThemeFontSize("font_size");
        if (!_codeDirty && _preparedVersion == GetVersion() && _codeFont == font && _codeFontGeneration == font.GetContentGeneration() && _codeFontSize == size) return;
        if (_refreshing) return; _refreshing = true; try
        {
            _codeFont = font; _codeFontSize = size; var height = GetLineHeight(); var width = font.GetCharSize('0', size).X; var digits = Math.Max(_minDigits, GetLineCount().ToString(CultureInfo.InvariantCulture).Length);
            if (_mainGutter >= 0) { SetGutterDraw(_mainGutter, _drawBookmarks || _drawBreakpoints || _drawExecuting); SetGutterWidth(_mainGutter, height); SetGutterClickable(_mainGutter, _drawBreakpoints); }
            if (_numberGutter >= 0) { SetGutterDraw(_numberGutter, _drawNumbers); SetGutterWidth(_numberGutter, (int)Math.Ceiling(width * (digits + 1))); }
            if (_foldGutter >= 0) { SetGutterDraw(_foldGutter, _drawFold); SetGutterWidth(_foldGutter, height); SetGutterClickable(_foldGutter, true); }
            while (_numberLayouts.Count < GetLineCount()) _numberLayouts.Add(new()); if (_foldEnds.Length < GetLineCount()) Array.Resize(ref _foldEnds, GetLineCount()); if (_regionHeaders.Length < GetLineCount()) Array.Resize(ref _regionHeaders, GetLineCount());
            for (var line = 0; line < GetLineCount(); line++) { var number = (line + 1).ToString(CultureInfo.InvariantCulture).PadLeft(digits, _zeroPad ? '0' : ' '); _numberLayouts[line].Build(font, new(number, size, 0, HorizontalAlignment.Right, -1, 0, 0, TextDirection.LTR, TextOrientation.Horizontal)); _foldEnds[line] = FoldEnd(line); _regionHeaders[line] = IsLineCodeRegionStart(line); }
            while (_optionLayouts.Count < _options.Count) _optionLayouts.Add(new()); if (_completionPrefixWidths.Length < _options.Count) Array.Resize(ref _completionPrefixWidths, _options.Count); _completionWidth = 0; _optionHeight = height;
            for (var i = 0; i < _options.Count; i++) { _optionLayouts[i].Build(font, new(_options[i].DisplayText, size, 0, HorizontalAlignment.Left, -1, 0, 0, TextDirection.LTR, TextOrientation.Horizontal)); _completionWidth = Math.Max(_completionWidth, _optionLayouts[i].Size.X + height * 2); _completionPrefixWidths[i] = _completionBase.Length > 0 && _options[i].DisplayText.StartsWith(_completionBase, StringComparison.OrdinalIgnoreCase) ? font.GetStringSize(_options[i].DisplayText[..Math.Min(_completionBase.Length, _options[i].DisplayText.Length)], HorizontalAlignment.Left, -1, size).X : 0; }
            var maxWidth = Math.Max(1, GetThemeConstant("completion_max_width")) * Math.Max(1, font.GetCharSize('x', size).X); _completionWidth = Math.Min(Math.Max(height * 4, _completionWidth), maxWidth);
            var hintSpans = new List<Rect2>(); var hintY = 0f; foreach (var hintLine in _hint.Split('\n')) { var first = hintLine.IndexOf('\uffff'); var last = hintLine.LastIndexOf('\uffff'); if (first >= 0 && last > first) { var from = font.GetStringSize(hintLine[..first], HorizontalAlignment.Left, -1, size).X; var to = font.GetStringSize(hintLine[..last].Replace("\uffff", ""), HorizontalAlignment.Left, -1, size).X; hintSpans.Add(new(from, hintY, to - from, font.GetHeight(size))); } hintY += font.GetHeight(size) + GetThemeConstant("line_spacing"); }
            _hintSpans = hintSpans.ToArray(); _completionBaseWidth = font.GetStringSize(_completionBase, HorizontalAlignment.Left, -1, size).X; var firstHint = _hint.IndexOf('\uffff'); _hintPrefixWidth = firstHint < 0 ? 0 : font.GetStringSize(_hint[..firstHint], HorizontalAlignment.Left, -1, size).X;
            _hintLayout.Build(font, new(_hint.Replace("\uffff", ""), size, 0, HorizontalAlignment.Left, -1, TextLineBreakFlags.Mandatory, 0, TextDirection.LTR, TextOrientation.Horizontal));
            _hoverWidth = font.GetStringSize(_hoverWord, HorizontalAlignment.Left, -1, size).X; _preparedVersion = GetVersion(); _codeFontGeneration = font.GetContentGeneration(); _codeDirty = false;
        }
        finally { _refreshing = false; }
    }
    private void DrawMainGutter(CanvasItem canvas, int line, int gutter, Rect2 region)
    { var flags = MarkersAt(line); var count = 0; if (_drawBookmarks && (flags & Marker.Bookmark) != 0) count++; if (_drawBreakpoints && (flags & Marker.Breakpoint) != 0) count++; if (_drawExecuting && (flags & Marker.Executing) != 0) count++; if (count == 0) return; var part = region.Size.X / count; var at = 0; void Icon(string name) { var texture = GetThemeIcon(name); if (texture is not { IsDisposed: false }) return; var size = texture.GetSize(); var scale = Math.Min(1, Math.Min(part / Math.Max(1, size.X), region.Size.Y / Math.Max(1, size.Y))); size *= scale; canvas.DrawTextureRect(texture, new(region.Position + new Vector2(at++ * part + (part - size.X) / 2, (region.Size.Y - size.Y) / 2), size), false, GetThemeColor(name == "executing_line" ? "executing_line_color" : name == "bookmark" ? "bookmark_color" : "breakpoint_color")); } if (_drawBookmarks && (flags & Marker.Bookmark) != 0) Icon("bookmark"); if (_drawBreakpoints && (flags & Marker.Breakpoint) != 0) Icon("breakpoint"); if (_drawExecuting && (flags & Marker.Executing) != 0) Icon("executing_line"); }
    private void DrawLineNumber(CanvasItem canvas, int line, int gutter, Rect2 region)
    { if ((uint)line >= _numberLayouts.Count) return; var layout = _numberLayouts[line]; layout.Draw(canvas, region.Position + new Vector2(region.Size.X - layout.Size.X - 2, (region.Size.Y - layout.Size.Y) / 2 + layout.FirstAscent), GetThemeColor("line_number_color"), clipRect: region); }
    private void DrawFoldGutter(CanvasItem canvas, int line, int gutter, Rect2 region)
    { if ((uint)line >= _foldEnds.Length || _foldEnds[line] <= line) return; var name = _folded.Contains(line) ? _regionHeaders[line] ? "folded_code_region" : "folded" : _regionHeaders[line] ? "can_fold_code_region" : "can_fold"; var icon = GetThemeIcon(name); if (icon is not { IsDisposed: false }) return; var size = icon.GetSize(); canvas.DrawTextureRect(icon, new(region.Position + (region.Size - size) / 2, size), false, GetThemeColor("code_folding_color")); }
    private void FitCodeOverlays()
    {
        var caret = GetCaretDrawPos(); var hintSize = _hintLayout.Size + new Vector2(8, 8); var hintHeight = _hint.Length > 0 ? hintSize.Y : 0;
        if (float.IsNaN(_hintX)) { _hintX = caret.X - _hintPrefixWidth; }
        _hintRect = new(new(_hintX, _hintBelow ? caret.Y + GetLineHeight() : caret.Y - hintSize.Y), hintSize);
        var above = Math.Max(0, caret.Y - (!_hintBelow ? hintHeight : 0)); var below = Math.Max(0, Size.Y - caret.Y - GetLineHeight() - (_hintBelow ? hintHeight : 0)); var wanted = Math.Min(_options.Count, Math.Max(1, GetThemeConstant("completion_lines"))); var useAbove = below < wanted * _optionHeight && above > below; var available = useAbove ? above : below; _completionVisibleLines = Math.Max(1, Math.Min(wanted, (int)(available / Math.Max(1, _optionHeight)))); var height = _completionVisibleLines * _optionHeight;
        var x = Math.Clamp(caret.X - _completionBaseWidth, 0, Math.Max(0, Size.X - _completionWidth)); var y = useAbove ? caret.Y - height - (!_hintBelow ? hintHeight : 0) : caret.Y + GetLineHeight() + (_hintBelow ? hintHeight : 0); _completionRect = new(new(x, y), new(Math.Min(Size.X, _completionWidth), height)); FitCompletionSelection();
    }
    private void DrawCodeOverlay()
    {
        SetCodeDrawingScope(true); try
        {
            FitCodeOverlays(); var caret = GetCaretDrawPos(); if (_guidelines.Length > 0 && _codeFont != null) { var origin = GetPosAtLineColumn(GetCaretLine(), 0); var charWidth = _codeFont.GetCharSize('0', _codeFontSize).X; for (var i = 0; i < _guidelines.Length; i++) { var color = GetThemeColor("line_length_guideline_color"); if (i > 0) color.A *= .6f; var x = origin.X + _guidelines[i] * charWidth; DrawLine(new(x, 0), new(x, Size.Y), color); } }
            if (_completionActive)
            { DrawRect(_completionRect, GetThemeColor("completion_background_color")); GetThemeStyleBox("completion")?.Draw(this, _completionRect); var visible = Math.Min(_options.Count - _optionOffset, _completionVisibleLines); for (var n = 0; n < visible; n++) { var index = n + _optionOffset; var rect = new Rect2(_completionRect.Position + new Vector2(0, n * _optionHeight), new(_completionRect.Size.X, _optionHeight)); if (index == _selectedOption) DrawRect(rect, GetThemeColor("completion_selected_color")); var option = _options[index]; var x = rect.Position.X + 4; if (option.Icon is { IsDisposed: false } icon) { DrawTextureRect(icon, new(new(x, rect.Position.Y + 2), new(_optionHeight - 4, _optionHeight - 4)), false); x += _optionHeight; } if (option.TryGetDefaultValue<Color>(out var preview)) { var previewRect = new Rect2(new(rect.End.X - _optionHeight, rect.Position.Y + 2), new(_optionHeight - 4, _optionHeight - 4)); var background = GetThemeIcon("completion_color_bg"); if (background is { IsDisposed: false }) DrawTextureRect(background, previewRect, true); DrawRect(previewRect, preview); } if (_completionPrefixWidths[index] > 0) DrawRect(new(new(x, rect.Position.Y), new(_completionPrefixWidths[index], rect.Size.Y)), GetThemeColor("completion_existing_color")); var layout = _optionLayouts[index]; layout.Draw(this, new(x, rect.Position.Y + (_optionHeight - layout.Size.Y) / 2 + layout.FirstAscent), option.FontColor, clipRect: rect); } }
            if (_options.Count > Math.Max(1, GetThemeConstant("completion_lines")) && _completionActive) { var width = GetThemeConstant("completion_scroll_width"); var page = _completionVisibleLines; var track = new Rect2(new(_completionRect.End.X - width, _completionRect.Position.Y), new(width, _completionRect.Size.Y)); DrawRect(track, GetThemeColor("completion_scroll_color")); var thumb = new Rect2(track.Position + new Vector2(0, track.Size.Y * _optionOffset / _options.Count), new(width, track.Size.Y * page / _options.Count)); DrawRect(thumb, GetThemeColor(thumb.HasPoint(_codeMouse) ? "completion_scroll_hovered_color" : "completion_scroll_color")); }
            if (_hint.Length > 0 && IsCaretVisible()) { GetThemeStyleBox("panel", "TooltipPanel")?.Draw(this, _hintRect); foreach (var span in _hintSpans) { var rect = new Rect2(_hintRect.Position + new Vector2(4, 4) + span.Position, span.Size); var color = GetThemeColor("font_color", "TooltipLabel"); var fill = color; fill.A *= .2f; DrawRect(rect, fill); DrawLine(new(rect.Position.X, rect.End.Y), rect.End, color, 2); } _hintLayout.Draw(this, _hintRect.Position + new Vector2(4, 4 + _hintLayout.FirstAscent), GetThemeColor("font_color", "TooltipLabel"), clipRect: _hintRect); }
            foreach (var line in _folded) { var end = GetPosAtLineColumn(line, LengthOf(line)); if (end.X < 0 || end.Y < 0 || end.Y > Size.Y) continue; if (_regionHeaders[line]) { var start = GetPosAtLineColumn(line, 0); DrawRect(new(start, new(Math.Max(0, end.X - start.X), GetLineHeight())), GetThemeColor("folded_code_region_color")); } var icon = GetThemeIcon("folded_eol_icon"); if (icon is { IsDisposed: false }) DrawTextureRect(icon, new((Vector2)end + new Vector2(2, 0), icon.GetSize()), false, GetThemeColor("code_folding_color")); }
            if (_highlightPairs) DrawMatchingPair();
            if (_symbolValid && _hoverWord.Length > 0) { var position = GetPosAtLineColumn(_symbolPosition.Y, _symbolPosition.X); DrawLine(position + new Vector2(0, GetLineHeight() - 1), position + new Vector2(_hoverWidth, GetLineHeight() - 1), GetThemeColor("font_color")); }
        }
        finally { SetCodeDrawingScope(false); }
    }
    private bool FindBraceMatch(int line, int column, out Vector2i anchor, out Vector2i match, out string key)
    {
        anchor = match = new(-1, -1); key = ""; var text = GetLine(line); var utf = ScalarIndex(text, column);
        foreach (var pair in _bracePairs)
        {
            var forward = utf >= pair.Key.Length && text.AsSpan(0, utf).EndsWith(pair.Key, StringComparison.Ordinal); var backward = text.AsSpan(utf).StartsWith(pair.Value, StringComparison.Ordinal); if (!forward && !backward) continue;
            key = forward ? pair.Key : pair.Value; var anchorUtf = forward ? utf - pair.Key.Length : utf; anchor = new(CountScalars(text.AsSpan(0, anchorUtf)), line);
            var region = RegionAt(line, Math.Min(LengthOf(line), anchor.X + 1));
            if (pair.Key == pair.Value) { if (region == null || _delimiters[region.Index].Comment) return false; var start = new Vector2i(region.Start.X - 1, region.Start.Y); var end = region.End.Y < 0 ? new Vector2i(-1, -1) : new(region.End.X - CountScalars(pair.Value.AsSpan()), region.End.Y); match = anchor == start ? end : start; return true; }
            if (region != null) return false; var depth = 1; var atLine = line; var at = forward ? utf : utf - 1;
            while (atLine >= 0 && atLine < GetLineCount())
            { text = GetLine(atLine); while (at >= 0 && at < text.Length) { if (char.IsLowSurrogate(text[at])) { at += forward ? 1 : -1; continue; } var col = CountScalars(text.AsSpan(0, at)); if (RegionAt(atLine, Math.Min(LengthOf(atLine), col + 1)) == null) { var open = text.AsSpan(at).StartsWith(pair.Key, StringComparison.Ordinal); var close = text.AsSpan(at).StartsWith(pair.Value, StringComparison.Ordinal); if (forward ? open : close) depth++; else if (forward ? close : open) { if (--depth == 0) { match = new(col, atLine); return true; } } } at += forward ? 1 : -1; } atLine += forward ? 1 : -1; if (atLine >= 0 && atLine < GetLineCount()) at = forward ? 0 : GetLine(atLine).Length - 1; }
            return true;
        }
        return false;
    }
    private void DrawMatchingPair()
    {
        for (var caret = 0; caret < GetCaretCount(); caret++) { if (!FindBraceMatch(GetCaretLine(caret), GetCaretColumn(caret), out var anchor, out var match, out var key)) continue; var color = GetThemeColor(match.Y < 0 ? "brace_mismatch_color" : "font_color"); var width = Math.Max(1, _codeFont!.GetCharSize(Rune.GetRuneAt(key, 0).Value, _codeFontSize).X); void Underline(Vector2i position) { if (position.Y < 0) return; var point = GetPosAtLineColumn(position.Y, position.X); if (point.X < 0 || point.Y < 0 || point.Y > Size.Y) return; var at = (Vector2)point + new Vector2(0, GetLineHeight() - 1); DrawLine(at, at + new Vector2(width, 0), color, 2); } Underline(anchor); Underline(match); }
    }
    private static bool CodeAction(InputEvent input, string action) => InputMap.HasAction(action) && input.IsActionPressed(action, true, true);
    /// <inheritdoc />
    protected override CursorShape OnGetCursorShape(Vector2 atPosition) => _symbolLookup && _symbolValid && _hoverWord.Length > 0 ? CursorShape.PointingHand : base.OnGetCursorShape(atPosition);
    /// <inheritdoc />
    protected override void OnGUIInput(InputEvent inputEvent)
    {
        if (inputEvent is InputEventMouseMotion motion)
        { _codeMouse = motion.Position; var position = GetLineColumnAtPos((Vector2i)motion.Position); var word = WordAt(position); if (word != _hoverWord || position.Y != _hoverPosition.Y) { _hoverWord = word; _hoverPosition = position; _symbolPosition = position; _symbolValid = false; if (_symbolLookup && word.Length > 0 && (motion.ControlPressed || motion.MetaPressed)) SymbolValidate?.Invoke(word); if (IsDisposed) return; _codeDirty = true; if (_symbolTooltip && word.Length > 0 && IsInsideTree) _symbolTimer.Start(); else _symbolTimer.Stop(); } if (!(motion.ControlPressed || motion.MetaPressed)) _symbolValid = false; if (_completionScrollPressed && _completionActive) { SetCodeCompletionSelectedIndex(Math.Clamp((int)((motion.Position.Y - _completionRect.Position.Y) / Math.Max(1, _completionRect.Size.Y) * _options.Count), 0, _options.Count - 1)); AcceptEvent(); return; } if (_completionActive && _completionRect.HasPoint(motion.Position)) { QueueRedraw(); AcceptEvent(); return; } base.OnGUIInput(inputEvent); return; }
        if (inputEvent is InputEventMouseButton { Pressed: false, ButtonIndex: MouseButton.Left } && _completionScrollPressed) { _completionScrollPressed = false; AcceptEvent(); return; }
        if (inputEvent is InputEventMouseButton mouse && mouse.Pressed)
        { if (_completionActive && _completionRect.HasPoint(mouse.Position)) { if (mouse.ButtonIndex == MouseButton.Left) { if (_options.Count > _completionVisibleLines && mouse.Position.X >= _completionRect.End.X - GetThemeConstant("completion_scroll_width")) { _completionScrollPressed = true; SetCodeCompletionSelectedIndex(Math.Clamp((int)((mouse.Position.Y - _completionRect.Position.Y) / Math.Max(1, _completionRect.Size.Y) * _options.Count), 0, _options.Count - 1)); AcceptEvent(); return; } SetCodeCompletionSelectedIndex(Math.Clamp((int)((mouse.Position.Y - _completionRect.Position.Y) / _optionHeight) + _optionOffset, 0, _options.Count - 1)); if (mouse.DoubleClick) ConfirmCodeCompletion(); } else if (mouse.ButtonIndex is MouseButton.WheelUp or MouseButton.WheelDown) SetCodeCompletionSelectedIndex(Math.Clamp(_selectedOption + (mouse.ButtonIndex == MouseButton.WheelUp ? -1 : 1), 0, _options.Count - 1)); AcceptEvent(); return; } if (_symbolLookup && _symbolValid && mouse.ButtonIndex == MouseButton.Left && (mouse.ControlPressed || mouse.MetaPressed)) { SymbolLookup?.Invoke(_hoverWord, _symbolPosition.Y, _symbolPosition.X); if (!IsDisposed) AcceptEvent(); return; } if (_completionActive) CancelCodeCompletion(); }
        if (inputEvent is InputEventKey { Pressed: false, ControlPressed: false, MetaPressed: false } && _symbolValid) SetSymbolLookupWordAsValid(false);
        if (inputEvent is InputEventKey { Pressed: true } key && ShortcutKeysEnabled)
        {
            if (_completionActive) { if (CodeAction(key, "ui_up") || CodeAction(key, "ui_down") || CodeAction(key, "ui_page_up") || CodeAction(key, "ui_page_down")) { var step = CodeAction(key, "ui_page_up") || CodeAction(key, "ui_page_down") ? _completionVisibleLines : 1; var selected = _selectedOption + (CodeAction(key, "ui_up") || CodeAction(key, "ui_page_up") ? -step : step); SetCodeCompletionSelectedIndex(step == 1 ? (selected + _options.Count) % _options.Count : Math.Clamp(selected, 0, _options.Count - 1)); AcceptEvent(); return; } if (CodeAction(key, "ui_text_completion_accept") || CodeAction(key, "ui_text_completion_replace")) { ConfirmCodeCompletion(CodeAction(key, "ui_text_completion_replace")); AcceptEvent(); return; } if (CodeAction(key, "ui_cancel")) { CancelCodeCompletion(); AcceptEvent(); return; } }

            if (Editable && CodeAction(key, "ui_text_newline")) { NewCodeLine(); AcceptEvent(); return; }
            if (_completionEnabled && CodeAction(key, "ui_text_completion_query")) { RequestCodeCompletion(true); AcceptEvent(); return; }
            if (CodeAction(key, "ui_text_indent")) { DoIndent(); AcceptEvent(); return; }
            if (CodeAction(key, "ui_text_dedent")) { UnindentLines(); AcceptEvent(); return; }
            if (CodeAction(key, "ui_text_newline_blank")) { NewCodeLine(false); AcceptEvent(); return; }
            if (CodeAction(key, "ui_text_newline_above")) { NewCodeLine(false, true); AcceptEvent(); return; }

        }
        base.OnGUIInput(inputEvent);
    }
    /// <inheritdoc />
    protected override void OnNotification(int what)
    { if (what == NotificationDraw) { EnsureCodeLayout(); base.OnNotification(what); DrawCodeOverlay(); return; } base.OnNotification(what); if (IsDisposed) return; switch (what) { case NotificationThemeChanged: case NotificationResized: case NotificationTranslationChanged: case NotificationLayoutDirectionChanged: _codeDirty = true; break; case NotificationReady: _symbolTimer.WaitTime = Math.Max(.000001, ProjectSettings.Get(ProjectSettings.TooltipDelaySeconds)); break; case NotificationFocusExit: CancelCodeCompletion(); _hint = ""; _symbolTimer?.Stop(); break; } }
}
