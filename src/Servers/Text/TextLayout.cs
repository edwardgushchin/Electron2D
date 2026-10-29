using UnicodeScript = Electron2D.TextFormatting.Unicode.Script;
using System.Buffers;
using System.Text;
using Electron2D.TextFormatting.Unicode;

namespace Electron2D;

internal readonly record struct TextLayoutKey(string Text, int FontSize, float Width, HorizontalAlignment Alignment,
    int MaxLines, TextLineBreakFlags Breaks, TextJustificationFlags Justification, TextDirection Direction, TextOrientation Orientation, bool Multiline = false);

internal readonly record struct TextLayoutOptions(float LineSpacing = 0, float ParagraphSpacing = 0, string Language = "",
    float[]? TabStops = null, int Overrun = 1, string Ellipsis = "\u2026", int VisibleCharacters = -1, int VisibleBehavior = 1, IReadOnlyList<TextBIDIRange>? BIDIOverride = null, bool ApplyAlignment = true, bool? WrappedBehavior = null, bool PreserveControl = false, IReadOnlyList<TextLayoutStyleSpan>? Styles = null, IReadOnlyList<TextLayoutInlineObject>? Objects = null, float DropcapWidth = 0, float DropcapHeight = 0, bool PreserveInvalid = true, bool GlobalObjectMetrics = false, bool ParagraphBuffers = false, string CustomPunctuation = "", bool ServerBuffers = false, int BufferGlyphSpacing = 0, int BufferSpaceSpacing = 0, int BufferTopSpacing = 0, int BufferBottomSpacing = 0, TextLayout? RetainedShape = null, bool SimpleLayout = false);

internal readonly record struct TextLayoutLine(int Start, int End, int GlyphStart, int GlyphCount, float Width, float Ascent,
    float Descent, float CrossOffset, bool ParagraphEnd, int ParagraphLevel, int RangeEnd = -1, float UnderlinePosition = 0, float UnderlineThickness = 0, int TrimPosition = -1, int EllipsisPosition = -1)
{
    internal float Height => Ascent + Descent;
}

// Each instance belongs to its font cache or a single consumer. Buffers survive rebuilds;
// native resources and cached glyph textures remain borrowed from the font faces.
internal sealed partial class TextLayout
{
    private readonly TextBIDI _bidi = new();
    private uint[] _scalars = [], _shapeScalars = [];
    private int[] _utf16 = [], _clusterEnds = [];
    private float[] _advances = [];
    private sbyte[] _levels = [], _paragraphLevels = [];
    private UnicodeScript[] _scripts = [];
    private FontData?[] _faces = [];
    private bool[] _grapheme = [], _breaks = [], _wordBoundaries = [], _wordEnds = [], _nonprinting = [];
    private readonly List<NativeShapedGlyph> _shaped = [];
    private readonly List<Run> _runs = [];
    private readonly List<Glyph> _lineGlyphs = [], _glyphs = [], _rawGlyphs = [];
    private readonly List<TextLayoutLine> _lines = [];
    private TextLayout? _ellipsis;
    private Font _font = null!;
    private TextLayoutOptions _options;
    private int _count;
    private bool _lineTrimmed, _tabRTL;
    private (float Ascent, float Descent, float UnderlinePosition, float UnderlineThickness) _shapeFontMetrics;
    private int _active;
    private long _builtFontGeneration = -1;
    private int _cachedUsers;
    internal bool IsBusy => _active != 0 || _cachedUsers != 0;
    internal void ClaimCachedUse() { if (IsBusy) throw new InvalidOperationException("A cached layout is already in use."); _cachedUsers++; }
    internal void ReleaseCachedUse() { if (_cachedUsers != 1) throw new InvalidOperationException("A cached layout has no matching reader."); _cachedUsers--; }
    private readonly record struct Run(int Start, int End, FontData? Face, UnicodeScript Script, sbyte Level);
    private readonly record struct Glyph(FontData? Face, uint Index, int Start, int End, float Advance, Vector2 Offset,
        uint Flags, bool Space, bool Tab, bool RTL, int Repeat = 1, bool Virtual = false, bool Missing = false, bool Elongation = false, int Size = 0, int ObjectIndex = -1, float ObjectAscent = 0, float ObjectDescent = 0, RID SourceRID = default, bool Ellipsis = false, TextGraphemeFlags? ProviderFlags = null, int? ProviderCount = null, int? ProviderSpanIndex = null)
    {
        internal Vector2 Position { get; init; }
    }

    internal TextLayoutKey Key { get; private set; }
    internal long LastAccess { get; set; }
    internal long Generation { get; set; } = -1;
    internal Vector2 Size { get; private set; }
    internal int CharacterCount { get; private set; }
    internal int GlyphCount => _glyphs.Count;
    internal int LineCount => _lines.Count;
    internal IReadOnlyList<TextLayoutLine> Lines => _lines;
    internal float NaturalWidth { get; private set; }
    internal float UnwrappedWidth { get; private set; }
    internal int DropcapLines { get; private set; }
    internal float FirstAscent => _lines.Count == 0 ? 0 : _lines[0].Ascent;

    internal void Build(Font font, TextLayoutKey key, TextLayoutOptions? options = null)
    {
        if (_active != 0) throw new InvalidOperationException("An active text layout cannot rebuild itself.");
        _active++;
        try
        {
            for (var attempt = 0; attempt < Font.MaximumReadAttempts; attempt++)
            {
                Exception? failure = null;
                try
                {
                    BeginRichBuild(font, options);
                    var read = font.BeginRead(); Exception? readFailure = null;
                    try
                    {
                        BuildCore(font, key, options, read);
                        if (read.IsCurrent && RichReadsCurrent()) { _builtFontGeneration = read.Generation; return; }
                    }
                    catch (ObjectDisposedException) when (!read.IsCurrent || !RichReadsCurrent()) { }
                    catch (Exception error) { readFailure = error; throw; }
                    finally { read.Dispose(readFailure); }
                }
                catch (Exception error) { failure = error; throw; }
                finally { EndRichBuild(failure); }
            }
            throw Font.UnsettledRead();
        }
        finally { _active--; }
    }

    private void BuildCore(Font font, TextLayoutKey key, TextLayoutOptions? options, Font.ReadScope rootRead)
    {
        _font = font; _options = options ?? new TextLayoutOptions(Overrun: 1, VisibleCharacters: -1);
        var implementation = TextServerManager.CallbackProvider;
        if (!TextServer.IsNativeBackendCall) _options = _options with { SimpleLayout = implementation is TextServerFallback };
        if (implementation is TextServerFallback)
        {
            TextServerFallback.ValidateDirection(key.Direction == TextDirection.Inherited ? TextDirection.Auto : key.Direction);
            if (_options.BIDIOverride is { } contexts) foreach (var context in contexts)
                    if (context.Direction == TextDirection.RTL) throw new NotSupportedException("The fallback text implementation cannot reorder right-to-left contexts.");
        }
        Key = key; _caretsReady = false; _serverCharactersReady = false; _glyphs.Clear(); _rawGlyphs.Clear(); _lines.Clear(); Size = Vector2.Zero;
        if (implementation is TextServerDummy)
        {
            ReleaseProviderLayout();
            // Editing retains scalar positions even when text has no shaped geometry.
            _options = _options with { SimpleLayout = true }; Decode(key.Text); CharacterCount = _count;
            _levels.AsSpan(0, _count + 1).Clear(); _paragraphLevels.AsSpan(0, _count + 1).Clear();
            DropcapLines = 0; NaturalWidth = UnwrappedWidth = 0; return;
        }
        Decode(key.Text); CharacterCount = _count; PrepareRichStyles(); PrepareCustomPunctuation(); if (_options.ServerBuffers) PrepareStyleBreaks();
        if (_options.VisibleBehavior == 0 && _options.VisibleCharacters >= 0) _count = Math.Min(_count, _options.VisibleCharacters);
        if (implementation is { } provider && provider is not TextServerFallback) { BuildProviderLayout(provider, rootRead); return; }
        ReleaseProviderLayout();
        ResolveProperties(); MeasureParagraphs(); BreakLines(); NaturalWidth = 0;
        foreach (var line in _lines) NaturalWidth = Math.Max(NaturalWidth, MathF.Ceiling(line.Width));
        TransformLines(); PositionLines(); if (_options.ServerBuffers) PrepareServerWrapping();
    }

    private void Decode(string text)
    {
        Ensure(ref _scalars, text.Length); Ensure(ref _shapeScalars, text.Length); Ensure(ref _utf16, text.Length + 1);
        Ensure(ref _clusterEnds, text.Length + 1); Ensure(ref _advances, text.Length + 1);
        Ensure(ref _levels, text.Length + 1); Ensure(ref _paragraphLevels, text.Length + 1);
        Ensure(ref _scripts, text.Length + 1); Ensure(ref _faces, text.Length + 1);
        Ensure(ref _grapheme, text.Length + 1); Ensure(ref _breaks, text.Length + 1); Ensure(ref _wordBoundaries, text.Length + 1); Ensure(ref _wordEnds, text.Length + 1);
        Ensure(ref _nonprinting, text.Length);
        _count = 0; var offset = 0;
        while (offset < text.Length)
        {
            var status = Rune.DecodeFromUtf16(text.AsSpan(offset), out var rune, out var consumed);
            if (status != OperationStatus.Done) { rune = Rune.ReplacementChar; consumed = 1; }
            _utf16[_count] = offset; _scalars[_count++] = (uint)rune.Value; offset += consumed;
        }
        _utf16[_count] = text.Length;
        Array.Clear(_advances, 0, _count + 1); Array.Clear(_grapheme, 0, _count + 1);
        _grapheme[0] = true; _grapheme[_count] = true;
        if (_options.SimpleLayout) { DecodeSimpleBoundaries(); return; }
        var graphemes = new GraphemeEnumerator(text);
        while (graphemes.MoveNext(out var grapheme)) _grapheme[ScalarAtUTF16(grapheme.Offset + grapheme.Length)] = true;
        NativeTextBreak.Fill(text, _options.Language ?? string.Empty, _utf16.AsSpan(0, _count + 1),
            _breaks.AsSpan(0, _count + 1), _wordBoundaries.AsSpan(0, _count + 1), _wordEnds.AsSpan(0, _count + 1));
        for (var i = 0; i < _count; i++) _nonprinting[i] = NativeTextBreak.IsNonprinting(_scalars[i]);
    }

    private void ResolveProperties()
    {
        var paragraph = 0;
        if (_options.SimpleLayout)
        { _levels.AsSpan(0, _count + 1).Clear(); _paragraphLevels.AsSpan(0, _count + 1).Clear(); }
        while (!_options.SimpleLayout && paragraph < _count)
        {
            var end = paragraph;
            while (end < _count && !IsHardBreak(_scalars[end])) end++;
            var separatorEnd = end;
            if (separatorEnd < _count) { separatorEnd++; if (_scalars[end] == '\r' && separatorEnd < _count && _scalars[separatorEnd] == '\n') separatorEnd++; }
            var level = Key.Direction switch { TextDirection.LTR => (sbyte)0, TextDirection.RTL => (sbyte)1, _ => (sbyte)2 };
            if (level == 2)
            {
                var strong = false;
                for (var i = paragraph; i < end; i++)
                    if (new Codepoint(_scalars[i]).BiDiClass is BidiClass.LeftToRight or BidiClass.RightToLeft or BidiClass.ArabicLetter) { strong = true; break; }
                if (!strong && NativeTextBreak.IsLocaleRTL(_options.Language ?? string.Empty)) level = 1;
            }
            _bidi.Resolve(Key.Text.AsSpan(_utf16[paragraph], _utf16[separatorEnd] - _utf16[paragraph]), level);
            _bidi.Levels.CopyTo(_levels.AsSpan(paragraph));
            for (var i = paragraph; i < separatorEnd; i++) _paragraphLevels[i] = (sbyte)_bidi.ParagraphLevel;
            paragraph = separatorEnd;
        }
        for (var i = 0; i < _count; i++) _scripts[i] = _options.SimpleLayout ? UnicodeScript.Common : new Codepoint(_scalars[i]).Script;
        var previous = UnicodeScript.Common;
        for (var i = 0; i < _count; i++)
        {
            if (IsHardBreak(_scalars[i])) { previous = UnicodeScript.Common; continue; }
            if (TextScript.IsStrong(_scripts[i])) { previous = _scripts[i]; continue; }
            if (TextScript.IsStrong(previous) && TextScript.IsCompatible(_scalars[i], previous)) { _scripts[i] = previous; continue; }
            for (var next = i + 1; next < _count && !IsHardBreak(_scalars[next]); next++)
                if (TextScript.IsStrong(_scripts[next]) && TextScript.IsCompatible(_scalars[i], _scripts[next])) { _scripts[i] = _scripts[next]; break; }
        }
        Span<char> languageKey = stackalloc char[3];
        for (var start = 0; start < _count;)
        {
            var end = start + 1; while (end < _count && !_grapheme[end]) end++;
            var style = StyleAt(start); var sources = style.Font.GetSources(); var primary = style.Font.PrimaryData;
            var language = FontSupportPolicy.LanguageKey(style.Language, languageKey); var script = TextScript.ToTag(_scripts[start]);
            var emoji = new Codepoint(_scalars[start]).HasEmojiPresentation;
            for (var i = start + 1; i < end; i++) { if (_scalars[i] == 0xFE0F) emoji = true; else if (_scalars[i] == 0xFE0E) emoji = false; }
            if (emoji) script = 0x5A737965;
            FontData? face = null;
            for (var tier = -1; tier <= 2 && face is null; tier++) foreach (var source in sources)
                {
                    if (ReferenceEquals(source, primary) ? tier != -1 : tier != source.FallbackPriority(language, script, emoji)) continue;
                    var supported = true;
                    for (var i = start; i < end; i++) if (!IsIgnorable(i) && source.GetGlyphIndex(_scalars[i]) == 0) { supported = false; break; }
                    if (supported) { face = source; break; }
                }
            if (face == null)
                foreach (var source in sources) if (source.FindSystemFallback(_scalars.AsSpan(start, end - start), style.Language, style.Font) is { } fallback) { face = fallback; break; }
            for (var i = start; i < end; i++)
            {
                _faces[i] = face;
                if (face is null)
                {
                    for (var tier = -1; tier <= 2 && _faces[i] is null; tier++) foreach (var source in sources) if ((ReferenceEquals(source, primary) ? tier == -1 : tier == source.FallbackPriority(language, script, emoji)) && source.GetGlyphIndex(_scalars[i]) != 0) { _faces[i] = source; break; }
                }
                if (_faces[i] is null && !IsIgnorable(i))
                    foreach (var source in sources) if (source.FindSystemFallback(_scalars[i], style.Language, style.Font) is { } fallback) { _faces[i] = fallback; break; }

            }
            start = end;
        }

    }

    private void MeasureParagraphs()
    {
        UnwrappedWidth = 0; var start = 0;
        while (start < _count)
        {
            var end = start; while (end < _count && !IsHardBreak(_scalars[end])) end++;
            ShapeLine(start, end); UnwrappedWidth = Math.Max(UnwrappedWidth, MathF.Ceiling(LayoutAdvance()));
            foreach (var glyph in _lineGlyphs)
            {
                if (glyph.Start < _count) _advances[glyph.Start] += glyph.Advance;
                for (var i = glyph.Start + 1; i < glyph.End; i++) _grapheme[i] = false;
            }
            start = end + 1;
        }
    }

    private void BreakLines()
    {
        DropcapLines = 0;
        if (_count == 0) { if (_options.ParagraphBuffers || _options.ServerBuffers) AddLine(0, 0, true); return; }
        var start = 0; var indent = 0f;
        if ((Key.Breaks & TextLineBreakFlags.TrimIndent) != 0 && Key.Width > 0)
        {
            for (var i = 0; i < _count && IsSpace(_scalars[i]); i++)
            {
                var advance = _advances[i];
                if (indent + advance > Key.Width) indent = 0; indent += advance;
            }
            indent = Math.Min(indent, .6f * Key.Width);
        }
        var richCross = 0f;
        while (start < _count)
        {
            var occupied = (_options.ParagraphBuffers ? richCross <= _options.DropcapHeight : richCross < _options.DropcapHeight) ? _options.DropcapWidth : 0;
            var limit = Key.Width - (start > 0 ? indent : 0) - occupied;
            var end = start; var lastBreak = -1; var width = 0f; var hard = false;
            while (end < _count)
            {
                if (IsHardBreak(_scalars[end]) && (Key.Breaks & TextLineBreakFlags.Mandatory) != 0)
                {
                    hard = true; break;
                }
                var advance = _advances[end];
                var exceeds = limit > 0 && width + advance > limit;
                var word = (Key.Breaks & TextLineBreakFlags.WordBound) != 0;
                var grapheme = (Key.Breaks & TextLineBreakFlags.GraphemeBound) != 0;
                var adaptive = word && (Key.Breaks & TextLineBreakFlags.Adaptive) != 0;
                if (exceeds && end > start && (word || grapheme || adaptive))
                {
                    if (lastBreak > start) { end = lastBreak; break; }
                    if ((grapheme || adaptive) && _grapheme[end]) break;
                }
                width += advance; end++;
                if ((word && _breaks[end] || grapheme && _grapheme[end]) && _grapheme[end])
                {
                    if (exceeds && end > start) break;
                    var hyphen = _scalars[end - 1] == 0x00ad ? SoftHyphenAdvance(end - 1) : 0;
                    if (hyphen == 0 || width + hyphen <= limit || hyphen >= limit) lastBreak = end;
                }
            }
            if (end == start && !hard)
            {
                end++; while (end < _count && !_grapheme[end]) end++;
            }
            AddLine(start, end, hard || end == _count);
            if (_options.ParagraphBuffers && hard && (Key.Breaks & TextLineBreakFlags.TrimEndEdgeSpaces) == 0)
            { var rangeEnd = end + 1; if (_scalars[end] == '\r' && rangeEnd < _count && _scalars[rangeEnd] == '\n') rangeEnd++; _lines[^1] = _lines[^1] with { RangeEnd = rangeEnd }; }
            richCross += MathF.Ceiling(_lines[^1].Height) + _options.LineSpacing;
            if (_options.ParagraphBuffers && occupied > 0 && richCross <= _options.DropcapHeight) DropcapLines++;
            if (hard)
            {
                var separator = _scalars[end++]; if (separator == '\r' && end < _count && _scalars[end] == '\n') end++;
            }
            start = end;
        }
    }

    private float SoftHyphenAdvance(int index)
    {
        var face = _faces[index];
        return face is null ? 0 : face.GetGlyphAdvance(face.GetGlyphIndex(0x00ad), Key.FontSize, Key.Orientation == TextOrientation.Vertical);
    }

    private void AddLine(int start, int end, bool paragraphEnd)
    {
        var flags = Key.Breaks; var trimStart = (flags & TextLineBreakFlags.TrimStartEdgeSpaces) != 0;
        var trimEnd = (flags & TextLineBreakFlags.TrimEndEdgeSpaces) != 0;
        trimStart &= _lines.Count > 0;
        trimEnd &= end < _count;
        if (Key.Alignment == HorizontalAlignment.Fill && (Key.Justification & TextJustificationFlags.TrimEdgeSpaces) != 0) trimStart = trimEnd = true;
        if (trimStart) while (start < end && IsSpace(_scalars[start])) start++;
        if (trimEnd) while (end > start && IsSpace(_scalars[end - 1])) end--;
        ShapeLine(start, end);
        if (!paragraphEnd && end > start && _scalars[end - 1] == 0x00ad)
        {
            var face = _faces[end - 1];
            if (face is not null)
            {
                for (var i = _lineGlyphs.Count - 1; i >= 0; i--) if (_lineGlyphs[i].Start == end - 1) _lineGlyphs.RemoveAt(i);
                var glyph = new Glyph(face, face.GetGlyphIndex(0x00ad), end - 1, end, SoftHyphenAdvance(end - 1), Vector2.Zero, 0, false, false, (_levels[end - 1] & 1) != 0, Size: StyleAt(end - 1).Size, SourceRID: StyleAt(end - 1).Font.GetSourceRID(face));
                _lineGlyphs.Insert(_paragraphLevels[start] == 1 ? 0 : _lineGlyphs.Count, glyph);
            }
        }
        var ascent = 0f; var descent = 0f;
        var sources = _font.GetSources(); var lastSource = _options.Styles is null && start < end && sources.Count > 0 ? 0 : -1;
        for (var i = start; i < end; i++)
            if (_options.Styles is null) lastSource = Math.Max(lastSource, _faces[i] is null ? sources.Count - 1 : sources.IndexOf(_faces[i]!));
        if (_options.Styles is not null) { ascent = _shapeFontMetrics.Ascent; descent = _shapeFontMetrics.Descent; }
        for (var i = 0; i <= lastSource; i++)
        {
            var metrics = sources[i].GetMetrics(Key.FontSize); ascent = Math.Max(ascent, metrics.Ascent); descent = Math.Max(descent, metrics.Descent);
        }
        foreach (var glyph in _lineGlyphs)
        {
            if (glyph.ObjectIndex >= 0) { ascent = Math.Max(ascent, glyph.ObjectAscent); descent = Math.Max(descent, glyph.ObjectDescent); continue; }
            if (glyph.Missing)
            {
                var box = TextMissingGlyph.Size(GlyphSize(glyph), glyph.Index);
                if (Key.Orientation == TextOrientation.Horizontal) { ascent = Math.Max(ascent, box.Y * .85f); descent = Math.Max(descent, box.Y * .15f); }
                else { var half = MathF.Round(box.X * .5f, MidpointRounding.AwayFromZero); ascent = Math.Max(ascent, half); descent = Math.Max(descent, half); }
                continue;
            }
            if (glyph.Face is null) continue; var metrics = glyph.Face.GetMetrics(GlyphSize(glyph));
            var styleFont = StyleAt(glyph.Start).Font;
            ascent = Math.Max(ascent, metrics.Ascent + (_options.Styles is null ? 0 : styleFont.GetShapingSpacing(glyph.Face, TextSpacingType.Top)));
            descent = Math.Max(descent, metrics.Descent + (_options.Styles is null ? 0 : styleFont.GetShapingSpacing(glyph.Face, TextSpacingType.Bottom)));
            if (Key.Orientation == TextOrientation.Horizontal) { ascent = Math.Max(ascent, -glyph.Offset.Y); descent = Math.Max(descent, glyph.Offset.Y); }
            else { var halfAdvance = MathF.Round(glyph.Face.GetGlyphAdvance(glyph.Index, GlyphSize(glyph)) * .5f, MidpointRounding.AwayFromZero); ascent = Math.Max(ascent, halfAdvance); descent = Math.Max(descent, halfAdvance); }
        }
        if (ascent == 0 && descent == 0) { var style = _options.ParagraphBuffers && _count > 0 ? StyleAt(Math.Min(start, _count - 1)) : new TextLayoutStyleSpan(0, _count, _font, Key.FontSize); ascent = style.Font.GetAscent(style.Size); descent = style.Font.GetDescent(style.Size); }
        else if (_options.Styles is null) { ascent += _font.GetSpacing(TextSpacingType.Top); descent += _font.GetSpacing(TextSpacingType.Bottom); }
        ascent += _options.BufferTopSpacing; descent += _options.BufferBottomSpacing;
        var paragraphLevel = start < _count ? _paragraphLevels[start] : Key.Direction == TextDirection.RTL ? 1 : 0;
        var width = LayoutAdvance();
        if (_options.ServerBuffers || _options.ParagraphBuffers) PrepareJustificationGlyphs(start, end, true, true);
        var glyphStart = _rawGlyphs.Count; _rawGlyphs.AddRange(_lineGlyphs);
        _lines.Add(new(start, end, glyphStart, _lineGlyphs.Count, width, ascent, descent, 0, paragraphEnd, paragraphLevel, UnderlinePosition: _shapeFontMetrics.UnderlinePosition, UnderlineThickness: _shapeFontMetrics.UnderlineThickness));
    }

    private void TransformLines()
    {
        var wrapped = _options.WrappedBehavior ?? (Key.Multiline && (Key.Breaks & (TextLineBreakFlags.WordBound | TextLineBreakFlags.GraphemeBound)) != 0);
        var visible = Key.MaxLines < 0 ? _lines.Count : Math.Min(Key.MaxLines, _lines.Count);
        var justifyTo = wrapped ? visible : _lines.Count;
        if (Key.Multiline && !(_lines.Count == 1 && (Key.Justification & TextJustificationFlags.DoNotSkipSingleLine) != 0))
        {
            if ((Key.Justification & TextJustificationFlags.SkipLastLine) != 0) justifyTo--;
            if ((Key.Justification & TextJustificationFlags.SkipLastLineWithVisibleChars) != 0)
            {
                for (var i = (wrapped ? visible : _lines.Count) - 1; i >= 0; i--)
                {
                    var line = _lines[i]; var found = false;
                    for (var j = line.Start; j < line.End; j++) if (!IsSpace(_scalars[j]) && !IsSpecial(_scalars[j])) { found = true; break; }
                    if (found) { justifyTo = i; break; }
                }
            }
        }
        for (var i = 0; i < _lines.Count; i++)
        {
            var line = _lines[i]; _lineTrimPosition = _lineEllipsisPosition = -1; _tabRTL = line.ParagraphLevel == 1; _lineGlyphs.Clear();
            for (var j = line.GlyphStart; j < line.GlyphStart + line.GlyphCount; j++) _lineGlyphs.Add(_rawGlyphs[j]);
            var width = line.Width; _lineTrimmed = false;
            var originalKey = Key;
            if (_options.ParagraphBuffers && _options.DropcapWidth > 0 && i <= DropcapLines) Key = Key with { Width = Key.Width - _options.DropcapWidth };
            var fill = Key.Alignment == HorizontalAlignment.Fill && i < justifyTo && Key.Width > 0;
            if (fill) width = Justify(line.Start, line.End, width);
            if (!wrapped || i == visible - 1 && (Key.Alignment == HorizontalAlignment.Fill && !fill || visible < _lines.Count))
            {
                TrimLine(line.ParagraphLevel, wrapped && visible < _lines.Count);
                width = LayoutAdvance();
                if (Key.Multiline && !wrapped && fill && _lineTrimmed) width = Justify(line.Start, line.End, width);
            }
            var glyphStart = _glyphs.Count; _glyphs.AddRange(_lineGlyphs);
            _lines[i] = line with { GlyphStart = glyphStart, GlyphCount = _lineGlyphs.Count, Width = width, TrimPosition = _lineTrimPosition, EllipsisPosition = _lineEllipsisPosition };
            Key = originalKey;
        }
    }

    private void ShapeLine(int start, int end)
    {
        _shapeFontMetrics = default;
        if (_options.Styles is not null)
        {
            Font? previous = null; FontData? face = null; var size = 0;
            for (var i = start; i < end; i++)
            {
                var style = StyleAt(i);
                if (ReferenceEquals(previous, style.Font) && ReferenceEquals(face, _faces[i]) && size == style.Size) continue;
                previous = style.Font; face = _faces[i]; size = style.Size; var metrics = style.Font.GetShapingMetrics(face, size);
                _shapeFontMetrics = (Math.Max(_shapeFontMetrics.Ascent, metrics.Ascent), Math.Max(_shapeFontMetrics.Descent, metrics.Descent),
                    Math.Max(_shapeFontMetrics.UnderlinePosition, style.Font.GetShapingUnderline(face, size, false)),
                    Math.Max(_shapeFontMetrics.UnderlineThickness, style.Font.GetShapingUnderline(face, size, true)));
            }
        }
        _lineGlyphs.Clear();
        if (_options.RetainedShape is { } retained)
        { retained.ServerCopyGlyphs(this, _lineGlyphs, start, end, 0); LayoutAdvance(); return; }
        PrepareRuns(start, end); var length = end - start;
        foreach (var run in _runs)
        {
            var runStyle = StyleAt(run.Start); var inline = _richObjects[run.Start]; var rtl = (run.Level & 1) != 0;
            if (inline.At >= 0)
            {
                var ascent = runStyle.Font.GetAscent(runStyle.Size); var descent = runStyle.Font.GetDescent(runStyle.Size);
                if (_options.GlobalObjectMetrics && _options.Styles is not null)
                { ascent = Math.Max(ascent, _shapeFontMetrics.Ascent); descent = Math.Max(descent, _shapeFontMetrics.Descent); }
                var vertical = Key.Orientation == TextOrientation.Vertical; var cross = vertical ? inline.Size.X : inline.Size.Y;
                var anchor = ((int)inline.Alignment & 12) switch { 0 => -ascent, 4 => (descent - ascent) / 2, 8 => 0, _ => descent };
                var own = ((int)inline.Alignment & 3) switch { 0 => 0, 1 => cross / 2, 2 => cross, _ => inline.ExplicitBaseline || inline.Baseline >= 0 ? inline.Baseline : cross };
                var top = anchor - own;
                _lineGlyphs.Add(new(null, 0, run.Start, run.End, vertical ? inline.Size.Y : inline.Size.X, Vector2.Zero, 0, false, false, rtl, Size: runStyle.Size, ObjectIndex: inline.Index, ObjectAscent: Math.Max(0, -top), ObjectDescent: Math.Max(0, top + cross)));
                for (var index = run.Start; index < run.End; index++) _clusterEnds[index] = run.End;
                continue;
            }
            if (_options.PreserveControl && _nonprinting[run.Start]) { AddMissingGlyph(run.Start, rtl); continue; }
            if (IsSpecial(_scalars[run.Start]))
            {
                var tab = _scalars[run.Start] == '\t';
                _lineGlyphs.Add(new(null, 0, run.Start, run.End, 0, Vector2.Zero, 0, tab, tab, rtl, Size: runStyle.Size)); continue;
            }
            if (run.Face is null)
            {
                var reverse = Key.Orientation == TextOrientation.Horizontal && (run.Level & 1) != 0;
                for (var index = reverse ? run.End - 1 : run.Start; reverse ? index >= run.Start : index < run.End; index += reverse ? -1 : 1) AddMissingGlyph(index, rtl);
                continue;
            }
            var direction = Key.Orientation == TextOrientation.Vertical
                ? (run.Level & 1) == 0 ? NativeTextDirection.TTB : NativeTextDirection.BTT
                : (run.Level & 1) == 0 ? NativeTextDirection.LTR : NativeTextDirection.RTL;
            _shaped.Clear(); run.Face.Shape(_shapeScalars, run.Start - start, run.End - run.Start, runStyle.Size, direction,
                TextScript.ToTag(run.Script), runStyle.Language, _shaped, features: runStyle.Font.GetShapingFeatures(run.Face), textLength: length, simple: _options.SimpleLayout);
            for (var i = run.Start; i < run.End; i++) _clusterEnds[i] = run.End;
            foreach (var shaped in _shaped)
            {
                var cluster = start + (int)shaped.Cluster;
                if (cluster < run.Start || cluster >= run.End) throw new InvalidOperationException("The shaping backend returned an invalid text cluster.");
                _clusterEnds[cluster] = cluster;
            }
            var nextCluster = run.End;
            for (var i = run.End - 1; i >= run.Start; i--)
                if (_clusterEnds[i] == i) { _clusterEnds[i] = nextCluster; nextCluster = i; }
            var spacingEnd = _count;
            while (spacingEnd > 0 && (_scalars[spacingEnd - 1] is 10 or 11 or 12 or 13 or 0x85 or 0x2028 or 0x2029 || IsIgnorable(spacingEnd - 1))) spacingEnd--;
            var lastAdvance = _shaped.Count;
            if (run.End >= spacingEnd) for (var i = _shaped.Count - 1; i >= 0; i--) { lastAdvance = i; if ((Key.Orientation == TextOrientation.Vertical ? _shaped[i].YAdvance : _shaped[i].XAdvance) != 0) break; }
            var sourceRID = runStyle.Font.GetSourceRID(run.Face);
            for (var shapedIndex = 0; shapedIndex < _shaped.Count; shapedIndex++)
            {
                var shaped = _shaped[shapedIndex];
                var cluster = start + (int)shaped.Cluster; var clusterEnd = _clusterEnds[cluster];
                var advance = (Key.Orientation == TextOrientation.Vertical ? -shaped.YAdvance : shaped.XAdvance) / 64f;
                var space = IsSpace(_scalars[cluster]);
                if (shaped.GlyphIndex == 0 && !IsIgnorable(cluster))
                {
                    for (var scalar = cluster; scalar < clusterEnd; scalar++) AddMissingGlyph(scalar, rtl);
                    continue;
                }
                if (shaped.GlyphIndex == 0) advance = 0;
                if (advance != 0)
                {
                    var spaceSpacing = space ? runStyle.Font.GetShapingSpacing(run.Face, TextSpacingType.Space) + (float)_options.BufferSpaceSpacing : 0;
                    var extraSpacing = spaceSpacing != 0 ? spaceSpacing : runStyle.Font.GetShapingSpacing(run.Face, TextSpacingType.Glyph) + (float)_options.BufferGlyphSpacing;
                    if (shapedIndex < lastAdvance) advance += extraSpacing;
                }
                var offset = shaped.GlyphIndex == 0 ? Vector2.Zero : new Vector2(shaped.XOffset / 64f, -shaped.YOffset / 64f);
                if (shaped.GlyphIndex != 0)
                {
                    var baseline = runStyle.Font.GetShapingBaselineOffset(run.Face, runStyle.Size);
                    offset += Key.Orientation == TextOrientation.Horizontal ? new Vector2(0, baseline) : new Vector2(baseline, 0);
                }
                _lineGlyphs.Add(new(run.Face, shaped.GlyphIndex, cluster, clusterEnd, advance,
                    offset, shaped.Flags, space, false, rtl, Size: runStyle.Size, SourceRID: sourceRID));
            }
        }
        LayoutAdvance();
    }

    private void PrepareRuns(int start, int end)
    {
        _runs.Clear(); var length = end - start;
        if (length == 0) return; _tabRTL = _paragraphLevels[start] == 1;
        _scalars.AsSpan(start, length).CopyTo(_shapeScalars);
        if (!_options.SimpleLayout && _options.BIDIOverride is { Count: > 0 } contexts)
        {
            for (var contextIndex = 0; contextIndex < contexts.Count; contextIndex++)
            {
                var context = contexts[contextIndex]; var from = Math.Max(start, context.Start); var to = Math.Min(end, context.End);
                if (from >= to) continue;
                var contextStart = Math.Clamp(context.Start, 0, _count); var contextEnd = Math.Clamp(context.End, contextStart, _count);
                var level = context.Direction switch { TextDirection.LTR => (sbyte)0, TextDirection.RTL => (sbyte)1, TextDirection.Auto => (sbyte)2, _ => _paragraphLevels[from] };
                if (level == 2)
                {
                    var strong = false;
                    for (var i = contextStart; i < contextEnd; i++)
                        if (new Codepoint(_scalars[i]).BiDiClass is BidiClass.LeftToRight or BidiClass.RightToLeft or BidiClass.ArabicLetter) { strong = true; break; }
                    if (!strong) level = _paragraphLevels[from];
                }
                _bidi.Resolve(Key.Text.AsSpan(_utf16[contextStart], _utf16[contextEnd] - _utf16[contextStart]), level);
                _bidi.Levels.CopyTo(_levels.AsSpan(contextStart));
                AppendRuns(from, to, (sbyte)_bidi.ParagraphLevel);
            }
        }
        else AppendRuns(start, end, _paragraphLevels[start]);
    }

    private void AppendRuns(int start, int end, sbyte paragraphLevel)
    {
        var firstRun = _runs.Count; var trailing = end;
        while (trailing > start && (IsSpace(_scalars[trailing - 1]) || IsBIDIControl(_scalars[trailing - 1]))) trailing--;
        for (var i = start; i < end;)
        {
            var level = i >= trailing ? paragraphLevel : _levels[i];
            var runEnd = _richObjects[i].At >= 0 ? Math.Min(end, _richObjects[i].At + _richObjects[i].Length) : i + 1; var special = IsSpecial(_scalars[i]) || _richObjects[i].At >= 0 || _options.PreserveControl && _nonprinting[i];
            if (!special)
                while (runEnd < end && _richObjects[runEnd].At < 0 && StyleAt(runEnd) == StyleAt(i) && !IsSpecial(_scalars[runEnd]) && !(_options.PreserveControl && _nonprinting[runEnd]) && ReferenceEquals(_faces[runEnd], _faces[i]) &&
                    _scripts[runEnd] == _scripts[i] && (runEnd >= trailing ? paragraphLevel : _levels[runEnd]) == level) runEnd++;
            _runs.Add(new(i, runEnd, _faces[i], _scripts[i], level)); i = runEnd;
        }
        var highest = 0; var lowestOdd = int.MaxValue;
        for (var i = firstRun; i < _runs.Count; i++)
        {
            var run = _runs[i]; highest = Math.Max(highest, run.Level); if ((run.Level & 1) != 0) lowestOdd = Math.Min(lowestOdd, run.Level);
        }
        for (var level = highest; level >= lowestOdd; level--)
        {
            for (var i = firstRun; i < _runs.Count;)
            {
                if (_runs[i].Level < level) { i++; continue; }
                var next = i + 1; while (next < _runs.Count && _runs[next].Level >= level) next++;
                _runs.Reverse(i, next - i); i = next;
            }
        }
    }

    private void AddMissingGlyph(int index, bool rtl)
    {
        var scalar = _scalars[index];
        if (!_options.PreserveInvalid && !(_options.PreserveControl && _nonprinting[index])) return;
        if (!_options.PreserveControl && (IsIgnorable(index) || scalar is 0x200b or 0x2060 or 0xfeff)) return;
        var fontSize = StyleAt(index).Size; var size = TextMissingGlyph.Size(fontSize, scalar);
        var vertical = Key.Orientation == TextOrientation.Vertical;
        var offset = vertical ? new Vector2(-MathF.Round(size.X * .5f, MidpointRounding.AwayFromZero), size.Y) : Vector2.Zero;
        _lineGlyphs.Add(new(null, scalar, index, index + 1, vertical ? size.Y : size.X, offset, 0, IsSpace(scalar), false, rtl, Missing: true, Size: fontSize));
    }

    private float LayoutAdvance()
    {
        var width = 0f; var tabOffset = 0f; var tabIndex = 0;
        for (var i = _tabRTL ? _lineGlyphs.Count - 1 : 0; _tabRTL ? i >= 0 : i < _lineGlyphs.Count; i += _tabRTL ? -1 : 1)
        {
            var glyph = _lineGlyphs[i];
            if (glyph.Tab)
            {
                glyph = glyph with { Advance = TabAdvance(tabOffset, ref tabIndex) }; _lineGlyphs[i] = glyph; tabOffset = 0;
            }
            else tabOffset += glyph.Advance * glyph.Repeat;
            width += glyph.Advance * glyph.Repeat;
        }
        return width;
    }

    private void TrimLine(int paragraphLevel, bool forceEllipsis = false)
    {
        _lineTrimmed = false;
        if (Key.Width <= 0 || _options.Overrun == 0) return;
        var originalCount = _lineGlyphs.Count;
        var width = LayoutAdvance(); var force = forceEllipsis || _options.Overrun is 5 or 6;
        if (width <= Key.Width && !force) return;
        _lineTrimmed = true;
        var addEllipsis = _options.Overrun >= 3;
        var wordOnly = _options.Overrun is 2 or 4 or 6;
        var ellipsisWidth = 0f;
        if (addEllipsis)
        {
            _ellipsis ??= new();
            var style = _options.GlobalObjectMetrics && _lineGlyphs.Count > 0 ? StyleAt(_lineGlyphs[^1].Start) : new TextLayoutStyleSpan(0, _count, _font, Key.FontSize, _options.Language);
            var key = new TextLayoutKey(_options.Ellipsis ?? "\u2026", style.Size, -1, HorizontalAlignment.Left, 1,
                TextLineBreakFlags.None, TextJustificationFlags.None, Key.Direction, Key.Orientation);
            _ellipsis.Build(style.Font, key, new TextLayoutOptions(Language: style.Language, Overrun: 0, VisibleCharacters: -1, PreserveInvalid: _options.PreserveInvalid));
            ellipsisWidth = Key.Orientation == TextOrientation.Horizontal ? _ellipsis.Size.X : _ellipsis.Size.Y;
        }
        var available = Math.Max(0, Key.Width - ellipsisWidth);
        if (addEllipsis && !force)
        {
            var keptWidth = width; var kept = _lineGlyphs.Count; var index = paragraphLevel == 1 ? 0 : kept - 1;
            while (kept > 0 && keptWidth > available)
            {
                keptWidth -= _lineGlyphs[index].Advance * _lineGlyphs[index].Repeat;
                kept--; index += paragraphLevel == 1 ? 1 : -1;
            }
            if (kept < 6) { addEllipsis = false; available = Math.Max(0, Key.Width); }
        }
        while (_lineGlyphs.Count > 0 && width > available)
        {
            var index = paragraphLevel == 1 ? 0 : _lineGlyphs.Count - 1; var cluster = _lineGlyphs[index].Start;
            do
            {
                width -= _lineGlyphs[index].Advance * _lineGlyphs[index].Repeat; _lineGlyphs.RemoveAt(index);
                if (_lineGlyphs.Count == 0) break; index = paragraphLevel == 1 ? 0 : _lineGlyphs.Count - 1;
            } while (_lineGlyphs[index].Start == cluster);
        }
        if (wordOnly)
        {
            while (_lineGlyphs.Count >= 6)
            {
                var index = paragraphLevel == 1 ? 0 : _lineGlyphs.Count - 1; var glyph = _lineGlyphs[index];
                if (_breaks[glyph.End] || glyph.Space) break;
                var cluster = glyph.Start; var clusterEnd = glyph.End;
                do
                {
                    _lineGlyphs.RemoveAt(index); if (_lineGlyphs.Count == 0) break;
                    index = paragraphLevel == 1 ? 0 : _lineGlyphs.Count - 1;
                } while (_lineGlyphs[index].Start == cluster && _lineGlyphs[index].End == clusterEnd);
            }
        }
        var keptVisible = _lineGlyphs.Count; _lineTrimPosition = paragraphLevel == 1 ? originalCount - keptVisible : keptVisible;
        if (keptVisible == originalCount) _lineTrimPosition = -1;
        _serverVisibleStart = paragraphLevel == 1 ? originalCount - keptVisible : 0; _serverVisibleCount = keptVisible; _serverEllipsisAdvance = 0;
        if (addEllipsis && (force || ellipsisWidth <= Key.Width) && _ellipsis is not null)
        {
            _lineEllipsisPosition = paragraphLevel == 1 ? originalCount - keptVisible : keptVisible; _serverEllipsisAdvance = ellipsisWidth;
            var cluster = _lineGlyphs.Count > 0 ? _lineGlyphs[paragraphLevel == 1 ? 0 : _lineGlyphs.Count - 1].End : 0;
            var insert = paragraphLevel == 1 ? 0 : _lineGlyphs.Count;
            foreach (var glyph in _ellipsis._glyphs)
                _lineGlyphs.Insert(insert++, glyph with { Start = cluster, End = cluster, Virtual = true, Ellipsis = true, RTL = paragraphLevel == 1 });
        }
    }

    private float Justify(int start, int end, float width, bool prepareAll = false)
    {
        var word = (Key.Justification & TextJustificationFlags.WordBound) != 0; var kashida = (Key.Justification & TextJustificationFlags.Kashida) != 0;
        PrepareJustificationGlyphs(start, end, word || prepareAll, kashida || prepareAll);
        if ((Key.Justification & TextJustificationFlags.ConstrainEllipsis) != 0 && !_lineTrimmed) return width;
        var begin = 0; var limit = _lineGlyphs.Count;
        if ((Key.Justification & TextJustificationFlags.AfterLastTab) != 0)
        {
            if (_tabRTL) { for (var i = 0; i < limit; i++) if (_lineGlyphs[i].Tab && _lineGlyphs[i].End > _lineGlyphs[i].Start) { limit = i; break; } }
            else for (var i = limit - 1; i >= 0; i--) if (_lineGlyphs[i].Tab && _lineGlyphs[i].End > _lineGlyphs[i].Start) { begin = i + 1; break; }
        }
        if ((Key.Justification & TextJustificationFlags.TrimEdgeSpaces) != 0)
        {
            var first = begin; var last = limit - 1;
            while (first <= last && ServerJustificationEdge(_lineGlyphs[first])) { width -= _lineGlyphs[first].Advance * _lineGlyphs[first].Repeat; _lineGlyphs[first] = _lineGlyphs[first] with { Advance = 0 }; first++; }
            while (last >= first && ServerJustificationEdge(_lineGlyphs[last])) { width -= _lineGlyphs[last].Advance * _lineGlyphs[last].Repeat; _lineGlyphs[last] = _lineGlyphs[last] with { Advance = 0 }; last--; }
        }
        if (kashida)
        {
            var opportunities = 0;
            for (var i = begin; i < limit; i++) if (_lineGlyphs[i].Elongation && _lineGlyphs[i].Advance > 0 && (i == begin || !_lineGlyphs[i - 1].Elongation)) opportunities++;
            if (opportunities > 0)
            {
                var extra = ((double)Key.Width - width) / opportunities;
                for (var i = begin; i < limit; i++)
                {
                    var glyph = _lineGlyphs[i]; if (!glyph.Elongation || glyph.Advance <= 0 || i > begin && _lineGlyphs[i - 1].Elongation) continue;
                    var repeat = (int)Math.Clamp(glyph.Repeat + Math.Truncate(extra / glyph.Advance), glyph.Virtual ? 0 : 1, 255);
                    _lineGlyphs[i] = glyph with { Repeat = repeat }; width += (repeat - glyph.Repeat) * glyph.Advance;
                }
            }
        }
        if (word)
        {
            var spaces = 0;
            for (var i = begin; i < limit; i++) if (IsJustificationSpace(_lineGlyphs[i])) spaces++;
            if (spaces > 0)
            {
                var extra = (Key.Width - width) / spaces;
                for (var i = begin; i < limit; i++)
                {
                    var glyph = _lineGlyphs[i]; if (!IsJustificationSpace(glyph)) continue;
                    var advance = Math.Max(glyph.Virtual ? 0 : GlyphSize(glyph) * .1f, glyph.Advance + extra);
                    _lineGlyphs[i] = glyph with { Advance = advance }; width += advance - glyph.Advance;
                }
            }
        }
        return width;
    }

    private bool IsJustificationSpace(Glyph glyph) => glyph.Space && glyph.End > glyph.Start && !glyph.Tab && (glyph.Advance > 0 || glyph.Virtual) && !IsCustomPunctuation(new Rune(_scalars[glyph.Start]));
    private void PrepareJustificationGlyphs(int start, int end, bool word, bool kashida)
    {
        if (_options.SimpleLayout) return;
        if (word) for (var i = 0; i < _lineGlyphs.Count; i++)
            {
                var glyph = _lineGlyphs[i];
                if (glyph.Start <= start || glyph.Start >= end || !_wordEnds[glyph.Start] || glyph.Space || glyph.Virtual || IsCustomPunctuation(new Rune(_scalars[glyph.Start]))) continue;
                var adjacent = !glyph.RTL ? i - 1 : i + 1; if (adjacent >= 0 && adjacent < _lineGlyphs.Count && _lineGlyphs[adjacent].Space) continue;
                _lineGlyphs.Insert(i, new(glyph.Face, 0, glyph.Start, glyph.End, 0, Vector2.Zero, 0, true, false, glyph.RTL, Virtual: true, Size: GlyphSize(glyph), SourceRID: glyph.SourceRID)); i++;
            }
        if (!kashida) return;
        for (var wordStart = start; wordStart < end;)
        {
            var wordEnd = wordStart + 1; while (wordEnd < end && !_wordBoundaries[wordEnd]) wordEnd++;
            var position = _wordEnds[wordEnd] ? TextJustification.FindKashida(_scalars.AsSpan(0, _count), wordStart, wordEnd) : -1;
            if (position >= 0) for (var i = 0; i < _lineGlyphs.Count; i++)
                {
                    var glyph = _lineGlyphs[i]; if (glyph.Start != position || glyph.End <= glyph.Start || glyph.Face is null) continue;
                    if (glyph.Elongation) break;
                    if (_scalars[position] == 0x0640) { _lineGlyphs[i] = glyph with { Elongation = true }; break; }
                    if (i > 0 && (_lineGlyphs[i - 1].Flags & 4) == 0) continue;
                    var style = StyleAt(glyph.Start); _shaped.Clear();
                    glyph.Face.Shape(ServerTatweelScalar, 0, 1, GlyphSize(glyph), NativeTextDirection.RTL, TextScript.ToTag(UnicodeScript.Arabic), style.Language, _shaped, features: style.Font.GetShapingFeatures(glyph.Face), textLength: 1);
                    if (_shaped.Count == 0 || _shaped[0].GlyphIndex == 0) break;
                    var shaped = _shaped[0]; var advance = (Key.Orientation == TextOrientation.Horizontal ? shaped.XAdvance : -shaped.YAdvance) / 64f;
                    var offset = Key.Orientation == TextOrientation.Horizontal ? new Vector2(shaped.XOffset / 64f, glyph.Offset.Y) : new Vector2(glyph.Offset.X, -shaped.YOffset / 64f);
                    _lineGlyphs.Insert(i, new(glyph.Face, shaped.GlyphIndex, glyph.Start, glyph.End, advance, offset, shaped.Flags, false, false, glyph.RTL, 0, true, Elongation: true, Size: GlyphSize(glyph), SourceRID: glyph.SourceRID)); break;
                }
            wordStart = wordEnd;
        }
    }

    private void PositionLines()
    {
        var cross = 0f; var visibleCross = 0f; var maxWidth = 0f; var visible = Key.MaxLines < 0 ? _lines.Count : Math.Min(Key.MaxLines, _lines.Count);
        for (var i = 0; i < _lines.Count; i++)
        {
            var line = _lines[i]; var cap = cross < _options.DropcapHeight ? _options.DropcapWidth : 0; var alignmentWidth = Key.Width - cap; var align = Key.Width <= 0 || !_options.ApplyAlignment ? 0 : Key.Alignment switch
            {
                HorizontalAlignment.Center when line.Width <= alignmentWidth => MathF.Floor((alignmentWidth - line.Width) * .5f),
                HorizontalAlignment.Center when line.ParagraphLevel == 1 => alignmentWidth - line.Width,
                HorizontalAlignment.Right => alignmentWidth - line.Width,
                HorizontalAlignment.Fill when Key.Multiline && line.ParagraphLevel == 1 => alignmentWidth - line.Width,
                _ => 0
            };
            line = line with { CrossOffset = _options.ParagraphBuffers ? 0 : cross }; _lines[i] = line;
            var advance = _options.ParagraphBuffers ? 0 : align + (line.ParagraphLevel != 1 ? cap : 0);
            for (var j = line.GlyphStart; j < line.GlyphStart + line.GlyphCount; j++)
            {
                var glyph = _glyphs[j];
                var position = Key.Orientation == TextOrientation.Horizontal ? new Vector2(advance, line.CrossOffset + line.Ascent) : new Vector2(line.CrossOffset + line.Ascent, advance);
                _glyphs[j] = glyph with { Position = position + glyph.Offset }; advance += glyph.Advance * glyph.Repeat;
            }
            if (i < visible)
            {
                maxWidth = Math.Max(maxWidth, line.Width + cap); visibleCross = cross + MathF.Ceiling(line.Height);
            }
            cross += MathF.Ceiling(line.Height);
            if (i + 1 < _lines.Count) cross += _options.LineSpacing + (line.ParagraphEnd ? _options.ParagraphSpacing : 0);
        }
        Size = Key.Orientation == TextOrientation.Horizontal ? new(MathF.Ceiling(maxWidth), visibleCross) : new(visibleCross, MathF.Ceiling(maxWidth));
        if (!Size.IsFinite()) throw new InvalidOperationException("Text layout exceeds finite geometry.");
    }

    internal void Draw(CanvasItem canvas, Vector2 baseline, Color color, int outline = 0, float oversampling = 0,
        int firstLine = 0, int maxLines = -1, int visibleCharacters = -1, int visibleBehavior = 1, bool outlinePass = false, bool clipToWidth = false, Rect2? clipRect = null, ReadOnlySpan<Color> characterColors = default, float clipStart = 0, bool ignoreLineLimit = false, float? advanceClipWidth = null, float advanceClipLeft = -1, float advanceClipRight = -1)
    {
        if (_active != 0) throw new InvalidOperationException("An active text layout cannot record itself recursively.");
        using var group = Font.BeginReadGroup(_richReadFonts, _font);
        for (var attempt = 0; attempt < Font.MaximumReadAttempts; attempt++)
        {
            var read = _font.BeginRead(); Exception? readFailure = null;
            try
            {
                if (_builtFontGeneration != read.Generation) { Build(_font, Key, _options); continue; }
                if (!read.IsCurrent) continue;
                _active++;
                try { DrawCore(canvas, baseline, color, outline, oversampling, firstLine, maxLines, visibleCharacters, visibleBehavior, outlinePass, clipToWidth, clipRect, characterColors, clipStart, ignoreLineLimit, advanceClipWidth, advanceClipLeft, advanceClipRight); return; }
                finally { _active--; }

            }
            catch (Exception scopeError) { readFailure = scopeError; throw; }
            finally { read.Dispose(readFailure); }
        }
        throw Font.UnsettledRead();
    }

    private bool OutsideAdvanceClip(Glyph glyph, int repeat, float left, float right)
    {
        var pen = (Key.Orientation == TextOrientation.Horizontal ? glyph.Position.X - glyph.Offset.X : glyph.Position.Y - glyph.Offset.Y) + repeat * glyph.Advance;
        return left >= 0 && pen < left || right >= 0 && pen + glyph.Advance > right;
    }

    private void DrawCore(CanvasItem canvas, Vector2 baseline, Color color, int outline, float oversampling,
        int firstLine, int maxLines, int visibleCharacters, int visibleBehavior, bool outlinePass, bool clipToWidth, Rect2? clipRect, ReadOnlySpan<Color> characterColors, float clipStart, bool ignoreLineLimit, float? advanceClipWidth, float advanceClipLeft, float advanceClipRight)
    {
        if (firstLine < 0 || firstLine >= _lines.Count) return;
        var clipWidth = advanceClipWidth ?? Key.Width;
        var end = ignoreLineLimit || Key.MaxLines < 0 ? _lines.Count : Math.Min(Key.MaxLines, _lines.Count);
        if (maxLines >= 0) end = Math.Min(end, firstLine + maxLines);
        if (_providerLayout is { } providerState && visibleCharacters < 0 && characterColors.IsEmpty)
        {
            using var consumer = TextServerManager.BeginConsumerRead(providerState.Owner);
            var stateUse = providerState.BeginUse(); Exception? stateUseFailure = null;
            try
            {
                using var recordingClip = canvas.BeginTextRecordingClip(clipRect);
                DrawProviderLines(providerState, canvas, baseline, color, outline, oversampling, firstLine, end, outlinePass,
                    clipToWidth, clipWidth, clipStart, advanceClipLeft, advanceClipRight);
                return;

            }
            catch (Exception scopeError) { stateUseFailure = scopeError; throw; }
            finally { stateUse.Dispose(stateUseFailure); }
        }
        var first = _lines[firstLine];
        var origin = baseline - (Key.Orientation == TextOrientation.Horizontal ? new Vector2(0, first.CrossOffset + first.Ascent) : new Vector2(first.CrossOffset + first.Ascent, 0));
        var budget = 0; var totalGlyphs = 0;
        for (var i = firstLine; i < end; i++) totalGlyphs += _lines[i].GlyphCount;
        for (var i = firstLine; i < end; i++)
        {
            var line = _lines[i];
            for (var j = line.GlyphStart; j < line.GlyphStart + line.GlyphCount; j++)
            {
                var glyph = _glyphs[j];
                var glyphColor = !outlinePass && glyph.Start >= 0 && glyph.Start < characterColors.Length ? characterColors[glyph.Start] : color;
                if (visibleCharacters >= 0)
                {
                    var reverse = visibleBehavior == 4 || visibleBehavior == 2 && line.ParagraphLevel == 1;
                    if (visibleBehavior <= 1 ? glyph.End > visibleCharacters : reverse ? budget < totalGlyphs - visibleCharacters : budget >= visibleCharacters) { budget++; continue; }
                }
                budget++;
                if ((!glyph.Missing && ((glyph.Face is null && _providerLayout is null) || glyph.Index == 0)) || glyph.Tab || glyph.Virtual && glyph.Index == 0) continue;
                if (glyph.Missing && OutsideAdvanceClip(glyph, 0, advanceClipLeft, advanceClipRight)) continue;
                if (glyph.Missing)
                {
                    if (clipToWidth && clipWidth > 0) { var pen = Key.Orientation == TextOrientation.Horizontal ? glyph.Position.X - glyph.Offset.X : glyph.Position.Y - glyph.Offset.Y; if (pen < clipStart || pen + glyph.Advance > clipStart + clipWidth) continue; }
                    if (!outlinePass) TextMissingGlyph.Draw(canvas, GlyphSize(glyph), origin + glyph.Position, glyph.Index, glyphColor, clipRect, _options.PreserveControl);
                    continue;
                }
                for (var repeat = 0; repeat < glyph.Repeat; repeat++)
                {
                    if (OutsideAdvanceClip(glyph, repeat, advanceClipLeft, advanceClipRight)) continue;
                    if (clipToWidth && clipWidth > 0 && (!glyph.Virtual || glyph.Elongation))
                    {
                        var pen = (Key.Orientation == TextOrientation.Horizontal ? glyph.Position.X - glyph.Offset.X : glyph.Position.Y - glyph.Offset.Y) + repeat * glyph.Advance;
                        if (pen < clipStart || pen + glyph.Advance > clipStart + clipWidth) continue;
                    }
                    var offset = Key.Orientation == TextOrientation.Horizontal ? new Vector2(repeat * glyph.Advance, 0) : new Vector2(0, repeat * glyph.Advance);
                    var position = origin + glyph.Position + offset;
                    if (_providerLayout is { } state)
                    {
                        DrawProviderGlyph(state, canvas, glyph, position, glyphColor, outline, oversampling, outlinePass, clipRect);
                        continue;
                    }
                    var image = glyph.Face!.GetGlyph(glyph.Index, GlyphSize(glyph), outline, oversampling, position, out var rasterPosition, canvas.FontRasterScale);
                    DrawGlyph(canvas, image, rasterPosition, glyphColor, !outlinePass && glyph.Face.ModulateColorGlyphs, clipRect);
                }
            }
        }
    }

    internal Rect2 GetCharacterBounds(int character)
    {
        if ((uint)character >= (uint)_count) return default;
        foreach (var line in _lines)
        {
            if (character < line.Start || character >= line.End) continue;
            var found = false; var result = default(Rect2);
            for (var i = line.GlyphStart; i < line.GlyphStart + line.GlyphCount; i++)
            {
                var glyph = _glyphs[i]; if (glyph.Virtual || character < glyph.Start || character >= glyph.End) continue;
                var position = glyph.Position - glyph.Offset;
                var rect = Key.Orientation == TextOrientation.Horizontal
                    ? new Rect2(position.X, line.CrossOffset, glyph.Advance * glyph.Repeat, line.Height)
                    : new Rect2(line.CrossOffset, position.Y, line.Height, glyph.Advance * glyph.Repeat);
                result = found ? result.Merge(rect) : rect; found = true;
            }
            return result;
        }
        return default;
    }

    internal static void DrawGlyph(CanvasItem canvas, FontGlyph glyph, Vector2 baseline, Color color, bool modulateColor = false, Rect2? clipRect = null)
    {
        if (glyph.Colored && !glyph.LCD && glyph.MSDFPixelRange == 0 && !modulateColor) color = new Color(1, 1, 1, color.A);
        if (glyph.Texture is not null && glyph.Size.X > 0 && glyph.Size.Y > 0)
        {
            var rect = new Rect2(baseline + glyph.Offset, glyph.Size);
            if (clipRect is { } clip)
            {
                var clipped = rect.Intersection(clip); if (!clipped.HasArea()) return;
                var region = glyph.Region ?? new Rect2(Vector2.Zero, glyph.Texture.GetSize());
                var scale = region.Size / glyph.Size;
                var source = new Rect2(region.Position + (clipped.Position - rect.Position) * scale, clipped.Size * scale);
                if (glyph.MSDFPixelRange > 0) canvas.DrawMSDFTextureRectRegion(glyph.Texture, clipped, source, color, glyph.MSDFOutline, glyph.MSDFPixelRange, glyph.MSDFScale);
                else if (glyph.LCD) canvas.DrawLCDTextureRectRegion(glyph.Texture, clipped, source, color);
                else canvas.DrawTextureRectRegion(glyph.Texture, clipped, source, color, clipUV: false);
            }
            else if (glyph.MSDFPixelRange > 0) canvas.DrawMSDFTextureRectRegion(glyph.Texture, rect, glyph.Region ?? new Rect2(Vector2.Zero, glyph.Texture.GetSize()), color, glyph.MSDFOutline, glyph.MSDFPixelRange, glyph.MSDFScale);
            else if (glyph.LCD) canvas.DrawLCDTextureRectRegion(glyph.Texture, rect, glyph.Region ?? new Rect2(Vector2.Zero, glyph.Texture.GetSize()), color);
            else if (glyph.Region is { } region) canvas.DrawTextureRectRegion(glyph.Texture, rect, region, color, clipUV: false);
            else canvas.DrawTextureRect(glyph.Texture, rect, false, color);
        }
    }
    private float TabAdvance(float width, ref int index)
    {
        var stops = _options.TabStops;
        if (stops is not { Length: > 0 }) return 0;
        double period = 0, peak = double.NegativeInfinity;
        for (var i = 0; i < stops.Length; i++) { period += stops[(index + i) % stops.Length]; peak = Math.Max(peak, period); }
        if (!(period > 0) || !double.IsFinite(period)) throw new InvalidOperationException("Tab increments must form a finite positive cycle.");
        var cycles = Math.Max(0, Math.Floor((width - peak) / period) + 1); var offset = cycles * period;
        do { offset += stops[index]; index = (index + 1) % stops.Length; } while (offset <= width);
        var advance = (float)(offset - width);
        if (!float.IsFinite(advance)) throw new InvalidOperationException("Tab alignment exceeds finite geometry.");
        return advance;
    }
    private int ScalarAtUTF16(int position)
    {
        var found = Array.BinarySearch(_utf16, 0, _count + 1, position); return found >= 0 ? found : ~found;
    }
    private static bool IsHardBreak(uint scalar) => new Codepoint(scalar).LineBreakClass is
        LineBreakClass.MandatoryBreak or LineBreakClass.CarriageReturn or LineBreakClass.LineFeed or LineBreakClass.NextLine;
    private static bool IsBIDIControl(uint scalar) => scalar is 0x061c or 0x200e or 0x200f or >= 0x202a and <= 0x202e or >= 0x2066 and <= 0x2069;
    private static bool IsSpecial(uint scalar) => scalar == '\t' || IsHardBreak(scalar) || IsBIDIControl(scalar);
    private bool IsIgnorable(int index) => _nonprinting[index] || IsSpecial(_scalars[index]) ||
        _scalars[index] is 0x200c or 0x200d or >= 0xfe00 and <= 0xfe0f or >= 0xe0100 and <= 0xe01ef;
    private static bool IsSpace(uint scalar) => Rune.IsWhiteSpace(new Rune(scalar)) && !IsHardBreak(scalar);
    private static void Ensure<T>(ref T[] array, int count)
    {
        if (array.Length < count) Array.Resize(ref array, Math.Max(count, Math.Max(16, array.Length * 2)));
    }
}
