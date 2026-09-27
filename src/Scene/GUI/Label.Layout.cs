using System.Globalization;
using System.Text;

namespace Electron2D;

public partial class Label
{
    private int _layoutGeneration;
    private readonly List<LabelSettings.Outline> _drawOutlines = [];
    private readonly List<LabelSettings.Shadow> _drawShadows = [];
    internal override Rect2? CanvasClipRect => _clipText ? new Rect2(Vector2.Zero, Size) : null;

    private void Invalidate(bool text = false)
    {
        _layoutGeneration++; _dirty = true; _textDirty |= text; QueueRedraw(); UpdateMinimumSize();
    }
    private void MaximumChanged() { if (_autowrapMode != TextAutowrapMode.Off || _textOverrunBehavior != global::Electron2D.TextOverrunBehavior.NoTrimming) Invalidate(); }
    private void ResourceChanged(Resource resource)
    {
        if (IsDisposed || !ReferenceEquals(resource, _labelSettings) && !ReferenceEquals(resource, _font)) return;
        var mutable = true;
        try { EnsureMutable(); }
        catch (ObjectDisposedException) { return; }
        catch (InvalidOperationException) { mutable = false; }
        if (mutable) { Invalidate(); return; }
        var tree = Volatile.Read(ref _resourceTree); var dispatch = Volatile.Read(ref _resourceDispatch);
        if (tree is null || dispatch is null) { _dirty = true; return; }
        if (Interlocked.Exchange(ref _resourcePending, 1) == 0)
            try { tree.Defer(dispatch); } catch (ObjectDisposedException) { Interlocked.Exchange(ref _resourcePending, 0); }
    }
    private void ResourceDisposed(ElectronObject resource) => ResourceChanged((Resource)resource);
    private void EnterResourceTree()
    {
        var tree = Tree!; var generation = ++_entryGeneration; _resourceTree = tree;
        _resourceDispatch = () => FlushResourceChange(tree, generation); Interlocked.Exchange(ref _resourcePending, 0);
        _translatedText = Atr(_text); if (_visibleRatio < 1) _visibleCharacters = RatioCount(_visibleRatio);
        SetInternalProcessing(true, false); Invalidate(true);
    }
    private void FlushResourceChange(SceneTree tree, int generation)
    {
        if (IsDisposed || !ReferenceEquals(Tree, tree) || _entryGeneration != generation) return;
        Interlocked.Exchange(ref _resourcePending, 0); Invalidate();
    }
    private Font EffectiveFont()
    {
        var font = _labelSettings?.Font ?? GetThemeFont("font") ?? throw new InvalidOperationException("A label requires a theme font or a LabelSettings font.");
        if (font.IsDisposed) throw new ObjectDisposedException(nameof(Font));
        if (!ReferenceEquals(font, _font))
        {
            if (_font is not null) { _font.Changed -= _resourceChanged; _font.Disposed -= _resourceDisposed; }
            _font = font; font.Changed += _resourceChanged; font.Disposed += _resourceDisposed; _dirty = true;
        }
        return font;
    }
    private void CheckResourceVersions()
    {
        if (_labelSettings?.IsDisposed == true) throw new ObjectDisposedException(nameof(LabelSettings));
        var font = EffectiveFont(); var generation = font.GetContentGeneration(); var revision = _labelSettings?.ContentRevision ?? -1;
        if (_fontGeneration != generation || _settingsRevision != revision)
        {
            _fontGeneration = generation; _settingsRevision = revision; _dirty = true;
        }
    }

    private void EnsureShaped()
    {
        CheckLabel(); if (_shaping) return; CheckResourceVersions(); if (!_dirty && !_textDirty) return;
        var generation = _layoutGeneration; _shaping = true;
        try
        {
            var font = _font!; _fontSize = _labelSettings?.FontSize ?? GetThemeFontSize("font_size"); Font.ValidateSize(_fontSize);
            _fontHeight = font.GetHeight(_fontSize);
            _lineSpacing = checked((int)(_labelSettings?.LineSpacing ?? GetThemeConstant("line_spacing")));
            _paragraphSpacing = checked((int)(_labelSettings?.ParagraphSpacing ?? GetThemeConstant("paragraph_spacing")));
            _normalStyle = GetThemeStyleBox("normal") ?? throw new InvalidOperationException("A label requires its normal theme style.");
            var styleMinimum = _normalStyle.GetMinimumSize();
            var width = checked((int)(Size.X - styleMinimum.X)); var maximum = GetCombinedMaximumSize().X;
            var wrapMaximum = _autowrapMode != TextAutowrapMode.Off && maximum > 0;
            if (wrapMaximum) width = Math.Max(1, checked((int)(maximum - styleMinimum.X)));
            if (_textDirty) RebuildParagraphs();
            _lines.Clear(); _naturalWidth = _translatedText.Length == 0 ? 1 : 0;
            var flags = _autowrapMode switch
            {
                TextAutowrapMode.WordSmart => TextLineBreakFlags.WordBound | TextLineBreakFlags.Adaptive,
                TextAutowrapMode.Word => TextLineBreakFlags.WordBound,
                TextAutowrapMode.Arbitrary => TextLineBreakFlags.GraphemeBound,
                _ => TextLineBreakFlags.None
            };
            flags |= TextLineBreakFlags.Mandatory | _autowrapTrimFlags;
            var justification = _justificationFlags | (_tabStops.Length > 0 ? TextJustificationFlags.AfterLastTab : 0);
            var direction = _textDirection == global::Electron2D.TextDirection.Inherited
                ? IsLayoutRTL() ? global::Electron2D.TextDirection.RTL : global::Electron2D.TextDirection.LTR
                : (int)_textDirection < 0 ? global::Electron2D.TextDirection.Auto : _textDirection;
            for (var p = 0; p < _paragraphCount; p++)
            {
                var paragraph = _paragraphs[p];
                var key = new TextLayoutKey(paragraph.Text, _fontSize, width,
                    _horizontalAlignment == HorizontalAlignment.Fill ? HorizontalAlignment.Fill : HorizontalAlignment.Left,
                    -1, flags, justification, direction, TextOrientation.Horizontal, true);
                paragraph.Layout.Build(font, key, new TextLayoutOptions(Language: _layoutLanguage, TabStops: _tabStops,
                    Overrun: EffectiveOverrun, Ellipsis: _ellipsisChar.Length == 0 ? "…" : _ellipsisChar, VisibleCharacters: -1,
                    BIDIOverride: paragraph.Contexts, ApplyAlignment: false, WrappedBehavior: _autowrapMode != TextAutowrapMode.Off));
                _naturalWidth = Math.Max(_naturalWidth, wrapMaximum ? paragraph.Layout.UnwrappedWidth : paragraph.Layout.NaturalWidth);
                for (var line = 0; line < paragraph.Layout.LineCount; line++)
                {
                    var item = paragraph.Layout.Lines[line]; var height = Math.Max(item.Height, _fontHeight);
                    _lines.Add(new(p, line, height, item.Ascent + (height - item.Height) * .5f, MathF.Ceiling(item.Width)));
                }
            }
            if (wrapMaximum) _naturalWidth = Math.Min(_naturalWidth, width);
            _minimumHeight = DisplayHeight(Math.Min(_linesSkipped, _lines.Count), DisplayLimit());
            var visible = VisibleLineCount(); var last = Math.Min(_lines.Count, _linesSkipped + visible);
            if (_autowrapMode != TextAutowrapMode.Off && visible > 0 && last < _lines.Count)
            {
                var lastLine = _lines[last - 1]; var paragraph = _paragraphs[lastLine.Paragraph];
                var layout = paragraph.Layout; var key = layout.Key with { MaxLines = lastLine.Local + 1 };
                layout.Build(font, key, new TextLayoutOptions(Language: _layoutLanguage, TabStops: _tabStops, Overrun: EffectiveOverrun,
                    Ellipsis: _ellipsisChar.Length == 0 ? "…" : _ellipsisChar, VisibleCharacters: -1, BIDIOverride: paragraph.Contexts, ApplyAlignment: false, WrappedBehavior: _autowrapMode != TextAutowrapMode.Off));
                for (var i = 0; i < _lines.Count; i++)
                    if (_lines[i].Paragraph == lastLine.Paragraph) _lines[i] = _lines[i] with { Width = MathF.Ceiling(layout.Lines[_lines[i].Local].Width) };
            }
            _dirty = _layoutGeneration != generation;
        }
        finally { _shaping = false; }
    }

    private void RebuildParagraphs()
    {
        _layoutLanguage = _language;
        if (_layoutLanguage.Length == 0)
        {
            _layoutLanguage = TranslationServer.GetOrAddDomain(TranslationDomain).LocaleOverride;
            if (_layoutLanguage.Length == 0) _layoutLanguage = TranslationServer.Culture.Name;
            if (_layoutLanguage.Length == 0) _layoutLanguage = TranslationServer.GetToolLocale();
        }
        var key = new PreparedKey(_translatedText, _layoutLanguage, _paragraphSeparator, _uppercase,
            _visibleCharactersBehavior == TextVisibleCharactersBehavior.CharsBeforeShaping ? _visibleCharacters : -1);
        PreparedText? prepared = null;
        foreach (var cached in _textCache) if (cached.Key == key) { prepared = cached; break; }
        if (prepared is null)
        {
            if (_textCache.Count < 16) { prepared = new(); _textCache.Add(prepared); }
            else { prepared = _textCache[0]; foreach (var cached in _textCache) if (cached.Access < prepared.Access) prepared = cached; }
            prepared.Key = default; prepared.Paragraphs.Clear();
            var text = _uppercase ? TextCase.ToUpper(_translatedText, _layoutLanguage) : _translatedText;
            if (key.VisibleCharacters >= 0) text = PrefixScalars(text, key.VisibleCharacters);
            var separator = UnescapeSeparator(_paragraphSeparator); var offset = 0; var scalarOffset = 0;
            while (offset <= text.Length)
            {
                var end = separator.Length == 0 ? text.Length : text.IndexOf(separator, offset, StringComparison.Ordinal);
                if (end < 0) end = text.Length;
                var content = text.Substring(offset, end - offset);
                prepared.Paragraphs.Add(new(content + "\u200B", scalarOffset));
                if (end == text.Length) break;
                scalarOffset += CountScalars(content.AsSpan()) + CountScalars(separator.AsSpan()); offset = end + separator.Length;
            }
            prepared.Key = key;
        }
        prepared.Access = ++_textCacheAccess; _paragraphCount = prepared.Paragraphs.Count; _textDirty = false;
        for (var i = 0; i < _paragraphCount; i++)
        {
            if (i == _paragraphs.Count) _paragraphs.Add(new());
            var paragraph = _paragraphs[i]; var item = prepared.Paragraphs[i]; paragraph.Text = item.Text; paragraph.Start = item.Start;
            ParseStructuredText(_structuredTextBIDIOverride, _structuredOptionsView, paragraph.Text, paragraph.Contexts); CheckLabel();
        }
    }
    private static string UnescapeSeparator(string value)
    {
        if (!value.Contains('\\')) return value;
        var result = new StringBuilder(value.Length);
        for (var i = 0; i < value.Length; i++)
        {
            if (value[i] != '\\' || i + 1 == value.Length) { result.Append(value[i]); continue; }
            var next = value[++i];
            result.Append(next switch { 'n' => '\n', 'r' => '\r', 't' => '\t', 'b' => '\b', 'f' => '\f', 'v' => '\v', 'a' => '\a', '\\' => '\\', '\'' => '\'', '"' => '"', _ => next });
        }
        return result.ToString();
    }
    private int EffectiveOverrun => (int)_textOverrunBehavior is >= 0 and <= 6 ? (int)_textOverrunBehavior : 0;
    private int DisplayLimit() => _maxLinesVisible < 0 ? _lines.Count : Math.Min(_maxLinesVisible, _lines.Count);
    private float DisplayHeight(int start, int count)
    {
        var end = Math.Min(_lines.Count, start + count); var height = 0f;
        for (var i = start; i < end; i++)
        {
            height += _lines[i].Height;
            if (i + 1 < end) height += _lineSpacing + (_lines[i + 1].Paragraph != _lines[i].Paragraph ? _paragraphSpacing : 0);
        }
        return height;
    }
    private int VisibleLineCount()
    {
        if (_normalStyle is null) return 0;
        var available = MathF.Ceiling(Size.Y - _normalStyle.GetMinimumSize().Y + _lineSpacing);
        var height = 0f; var count = 0; var limit = DisplayLimit();
        for (var i = Math.Min(_linesSkipped, _lines.Count); i < _lines.Count && count < limit; i++)
        {
            height += _lines[i].Height + _lineSpacing;
            if (height > available) break;
            count++;
            if (i + 1 < _lines.Count && _lines[i + 1].Paragraph != _lines[i].Paragraph) height += _paragraphSpacing;
        }
        return count;
    }
    private void GetDisplayGeometry(out int start, out int end, out float y, out float extraGap)
    {
        start = Math.Min(_linesSkipped, _lines.Count); var visible = VisibleLineCount(); end = Math.Min(_lines.Count, start + visible);
        var style = _normalStyle!; var height = DisplayHeight(start, visible) + style.GetMargin(Side.Top) + style.GetMargin(Side.Bottom);
        var remaining = Size.Y - height; y = style.GetOffset().Y; extraGap = 0;
        if (visible == 0) return;
        if (_verticalAlignment == VerticalAlignment.Center) y += checked((int)(remaining / 2));
        else if (_verticalAlignment == VerticalAlignment.Bottom) y += checked((int)remaining);
        else if (_verticalAlignment == VerticalAlignment.Fill && visible > 1) extraGap = checked((int)(remaining / (visible - 1)));
    }
    private float LineX(LabelLine line)
    {
        var style = _normalStyle!; var rtl = IsLayoutRTL();
        return _horizontalAlignment switch
        {
            HorizontalAlignment.Center => checked((int)(Size.X - line.Width)) / 2,
            HorizontalAlignment.Right when !rtl => checked((int)(Size.X - style.GetMargin(Side.Right) - line.Width)),
            HorizontalAlignment.Left when rtl => checked((int)(Size.X - style.GetMargin(Side.Right) - line.Width)),
            HorizontalAlignment.Fill when _autowrapMode != TextAutowrapMode.Off && _paragraphs[line.Paragraph].Layout.Lines[line.Local].ParagraphLevel == 1
                => checked((int)(Size.X - style.GetMargin(Side.Right) - line.Width)),
            _ => style.GetOffset().X
        };
    }

    /// <inheritdoc />
    /// <exception cref="InvalidOperationException">A required font or style is missing, or shaping fails.</exception>
    /// <exception cref="ObjectDisposedException">A borrowed settings, font or style resource is disposed.</exception>
    protected override Vector2 OnGetMinimumSize()
    {
        EnsureShaped(); var size = new Vector2(_naturalWidth, Math.Max(_minimumHeight, _fontHeight));
        if (_autowrapMode != TextAutowrapMode.Off)
        {
            size.X = 1;
            if (!_clipText && _textOverrunBehavior != global::Electron2D.TextOverrunBehavior.NoTrimming && _maxLinesVisible > 0)
                size.Y = Math.Min(size.Y, (_fontHeight + _lineSpacing) * _maxLinesVisible);
            else if (_clipText || _textOverrunBehavior != global::Electron2D.TextOverrunBehavior.NoTrimming) size.Y = 1;
        }
        else if (_clipText || _textOverrunBehavior != global::Electron2D.TextOverrunBehavior.NoTrimming) size.X = 1;
        return size + (_normalStyle?.GetMinimumSize() ?? Vector2.Zero);
    }
    private Rect2 CharacterBounds(int position)
    {
        CheckLabel(); EnsureShaped(); if (position < 0) return default;
        GetDisplayGeometry(out var start, out var end, out var y, out var gap);
        for (var i = start; i < end; i++)
        {
            var line = _lines[i]; var paragraph = _paragraphs[line.Paragraph]; var local = position - paragraph.Start;
            var layoutLine = paragraph.Layout.Lines[line.Local];
            if (local >= layoutLine.Start && local < layoutLine.End)
            {
                var bounds = paragraph.Layout.GetCharacterBounds(local);
                if (bounds.Size.X > 0) return new(new Vector2(LineX(line) + bounds.Position.X, y), new Vector2(bounds.Size.X, line.Height));
            }
            y += line.Height + _lineSpacing + gap;
            if (i + 1 < end && _lines[i + 1].Paragraph != line.Paragraph) y += _paragraphSpacing;
        }
        return default;
    }

    private void DrawLabel()
    {
        EnsureShaped(); var style = HasFocus() ? GetThemeStyleBox("focus") : _normalStyle;
        style?.Draw(this, new Rect2(Vector2.Zero, Size));
        var settings = _labelSettings?.Capture(_drawOutlines, _drawShadows);
        if (settings is null) { _drawOutlines.Clear(); _drawShadows.Clear(); }
        var color = settings?.FontColor ?? GetThemeColor("font_color");
        var shadow = settings?.ShadowColor ?? GetThemeColor("font_shadow_color");
        var shadowOffset = settings?.ShadowOffset ?? new Vector2(GetThemeConstant("shadow_offset_x"), GetThemeConstant("shadow_offset_y"));
        var shadowSize = settings?.ShadowSize ?? GetThemeConstant("shadow_outline_size");
        var outlineColor = settings?.OutlineColor ?? GetThemeColor("font_outline_color");
        var outlineSize = settings?.OutlineSize ?? GetThemeConstant("outline_size");
        GetDisplayGeometry(out var start, out var end, out var y, out var gap);
        var totalGlyphs = 0; for (var i = start; i < end; i++) totalGlyphs += _paragraphs[_lines[i].Paragraph].Layout.Lines[_lines[i].Local].GlyphCount;
        var visibleGlyphs = (int)Math.Clamp(Math.Truncate(totalGlyphs * (double)_visibleRatio), 0, int.MaxValue); var processed = 0;
        for (var i = start; i < end; i++)
        {
            var line = _lines[i]; var paragraph = _paragraphs[line.Paragraph]; var glyphCount = paragraph.Layout.Lines[line.Local].GlyphCount;
            var baseline = new Vector2(LineX(line), y + line.Ascent);
            var visible = -1; var behavior = 1;
            if (_visibleCharacters >= 0)
            {
                if (_visibleCharactersBehavior == TextVisibleCharactersBehavior.CharsAfterShaping) visible = Math.Max(0, _visibleCharacters - paragraph.Start);
                else if (_visibleCharactersBehavior is TextVisibleCharactersBehavior.GlyphsAuto or TextVisibleCharactersBehavior.GlyphsLTR or TextVisibleCharactersBehavior.GlyphsRTL)
                {
                    var rtl = _visibleCharactersBehavior == TextVisibleCharactersBehavior.GlyphsRTL || _visibleCharactersBehavior == TextVisibleCharactersBehavior.GlyphsAuto && IsLayoutRTL();
                    visible = Math.Clamp(visibleGlyphs - (rtl ? totalGlyphs - processed - glyphCount : processed), 0, glyphCount); behavior = rtl ? 4 : 3;
                }
            }
            var stackedSize = outlineSize;
            foreach (var item in _drawOutlines) if (item.Size > 0) stackedSize = checked(stackedSize + item.Size);
            if (shadow.A != 0 && shadowSize > 0) DrawLine(paragraph, line.Local, baseline + shadowOffset, shadow, shadowSize, visible, behavior, true);
            if (shadow.A > 0) DrawLine(paragraph, line.Local, baseline + shadowOffset, shadow, 0, visible, behavior, true);
            for (var layer = _drawShadows.Count - 1; layer >= 0; layer--)
            {
                var item = _drawShadows[layer];
                if (item.OutlineSize > 0) DrawLine(paragraph, line.Local, baseline + item.Offset, item.Color, item.OutlineSize, visible, behavior, true);
                DrawLine(paragraph, line.Local, baseline + item.Offset, item.Color, 0, visible, behavior, true);
            }
            for (var layer = _drawOutlines.Count - 1; layer >= 0; layer--)
            {
                var item = _drawOutlines[layer]; if (item.Size <= 0) continue;
                if (item.Color.A != 0 && stackedSize > 0) DrawLine(paragraph, line.Local, baseline, item.Color, stackedSize, visible, behavior, true);
                stackedSize -= item.Size;
            }
            if (outlineSize > 0 && outlineColor.A != 0) DrawLine(paragraph, line.Local, baseline, outlineColor, outlineSize, visible, behavior, true);
            DrawLine(paragraph, line.Local, baseline, color, 0, visible, behavior, false);
            processed += glyphCount; y += line.Height + _lineSpacing + gap;
            if (i + 1 < end && _lines[i + 1].Paragraph != line.Paragraph) y += _paragraphSpacing;
        }
    }
    private void DrawLine(Paragraph paragraph, int line, Vector2 baseline, Color color, int outline, int visible, int behavior, bool effect) =>
        paragraph.Layout.Draw(this, baseline, color, outline, firstLine: line, maxLines: 1, visibleCharacters: visible, visibleBehavior: behavior, outlinePass: effect);

    /// <inheritdoc />
    /// <exception cref="Exception">Resource, theme, shaping, drawing or inherited notification callbacks fail.</exception>
    protected override void OnNotification(int what)
    {
        if (what == NotificationDraw) { base.OnNotification(what); DrawLabel(); return; }
        List<Exception>? errors = null;
        try { base.OnNotification(what); } catch (Exception error) { CollectException(ref errors, error); }
        if (!IsDisposed)
            try
            {
                switch (what)
                {
                    case NotificationEnterTree:
                        EnterResourceTree(); break;
                    case NotificationExitTree:
                        _entryGeneration++; _resourceTree = null; _resourceDispatch = null; Interlocked.Exchange(ref _resourcePending, 0);
                        SetInternalProcessing(false, false); break;
                    case NotificationInternalProcess:
                        CheckResourceVersions(); if (_dirty || _textDirty) { QueueRedraw(); UpdateMinimumSize(); }
                        break;
                    case NotificationTranslationChanged:
                        _translatedText = Atr(_text); if (_visibleRatio < 1) _visibleCharacters = RatioCount(_visibleRatio); Invalidate(true); break;
                    case NotificationThemeChanged:
                    case NotificationLayoutDirectionChanged:
                        Invalidate(); break;
                    case NotificationResized:
                        Invalidate(); break;
                    case NotificationFocusEnter:
                    case NotificationFocusExit:
                        QueueRedraw(); break;
                }
            }
            catch (Exception error) { CollectException(ref errors, error); }
        ThrowCollected("Label notification callbacks failed.", errors);
    }
}
