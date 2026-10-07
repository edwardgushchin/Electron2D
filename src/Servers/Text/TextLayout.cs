using UnicodeScript = Electron2D.TextFormatting.Unicode.Script;
using System.Buffers;
using System.Text;
using Electron2D.TextFormatting.Unicode;

namespace Electron2D;

internal readonly record struct TextLayoutKey(string Text, int FontSize, float Width, HorizontalAlignment Alignment,
    int MaxLines, TextLineBreakFlags Breaks, TextJustificationFlags Justification, TextDirection Direction, TextOrientation Orientation, bool Multiline = false);

internal readonly record struct TextLayoutOptions(float LineSpacing = 0, float ParagraphSpacing = 0, string Language = "",
    float[]? TabStops = null, int Overrun = 1, string Ellipsis = "\u2026", int VisibleCharacters = -1, int VisibleBehavior = 1, IReadOnlyList<TextBIDIRange>? BIDIOverride = null, bool ApplyAlignment = true, bool? WrappedBehavior = null, bool PreserveControl = false, IReadOnlyList<TextLayoutStyleSpan>? Styles = null, IReadOnlyList<TextLayoutInlineObject>? Objects = null, float DropcapWidth = 0, float DropcapHeight = 0);

internal readonly record struct TextLayoutLine(int Start, int End, int GlyphStart, int GlyphCount, float Width, float Ascent,
    float Descent, float CrossOffset, bool ParagraphEnd, int ParagraphLevel)
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
    private int _active;
    private long _builtFontGeneration = -1;
    internal bool IsBusy => _active != 0;
    private readonly record struct Run(int Start, int End, FontData? Face, UnicodeScript Script, sbyte Level);
    private readonly record struct Glyph(FontData? Face, uint Index, int Start, int End, float Advance, Vector2 Offset,
        uint Flags, bool Space, bool Tab, int Repeat = 1, bool Virtual = false, bool Missing = false, bool Elongation = false, int Size = 0, int ObjectIndex = -1, float ObjectAscent = 0, float ObjectDescent = 0)
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
    internal float FirstAscent => _lines.Count == 0 ? 0 : _lines[0].Ascent;

    internal void Build(Font font, TextLayoutKey key, TextLayoutOptions? options = null)
    {
        if (IsBusy) throw new InvalidOperationException("An active text layout cannot rebuild itself.");
        _active++;
        try
        {
            BeginRichBuild(font, options);
            for (var attempt = 0; attempt < Font.MaximumReadAttempts; attempt++)
            {
                using var read = font.BeginRead();
                try
                {
                    BuildCore(font, key, options);
                    if (read.IsCurrent) { _builtFontGeneration = read.Generation; return; }
                }
                catch (ObjectDisposedException) when (!read.IsCurrent) { }
            }
            throw Font.UnsettledRead();
        }
        finally { try { EndRichBuild(); } finally { _active--; } }
    }

    private void BuildCore(Font font, TextLayoutKey key, TextLayoutOptions? options)
    {
        _font = font; _options = options ?? new TextLayoutOptions(Overrun: 1, VisibleCharacters: -1);
        Key = key; _caretsReady = false; _glyphs.Clear(); _rawGlyphs.Clear(); _lines.Clear(); Size = Vector2.Zero;
        Decode(key.Text); CharacterCount = _count; PrepareRichStyles();
        if (_options.VisibleBehavior == 0 && _options.VisibleCharacters >= 0) _count = Math.Min(_count, _options.VisibleCharacters);
        ResolveProperties(); MeasureParagraphs(); BreakLines(); NaturalWidth = 0;
        foreach (var line in _lines) NaturalWidth = Math.Max(NaturalWidth, MathF.Ceiling(line.Width));
        TransformLines(); PositionLines();
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
        var graphemes = new GraphemeEnumerator(text);
        while (graphemes.MoveNext(out var grapheme)) _grapheme[ScalarAtUTF16(grapheme.Offset + grapheme.Length)] = true;
        NativeTextBreak.Fill(text, _options.Language ?? string.Empty, _utf16.AsSpan(0, _count + 1),
            _breaks.AsSpan(0, _count + 1), _wordBoundaries.AsSpan(0, _count + 1), _wordEnds.AsSpan(0, _count + 1));
        for (var i = 0; i < _count; i++) _nonprinting[i] = NativeTextBreak.IsNonprinting(_scalars[i]);
    }

    private void ResolveProperties()
    {
        var paragraph = 0;
        while (paragraph < _count)
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
        for (var start = 0; start < _count;)
        {
            var end = start + 1; while (end < _count && !_grapheme[end]) end++;
            var sources = StyleAt(start).Font.GetSources();
            FontData? face = null;
            foreach (var source in sources)
            {
                var supported = true;
                for (var i = start; i < end; i++) if (!IsIgnorable(i) && source.GetGlyphIndex(_scalars[i]) == 0) { supported = false; break; }
                if (supported) { face = source; break; }
            }
            for (var i = start; i < end; i++)
            {
                _faces[i] = face;
                if (face is null)
                {
                    foreach (var source in sources) if (source.GetGlyphIndex(_scalars[i]) != 0) { _faces[i] = source; break; }
                }
                _scripts[i] = new Codepoint(_scalars[i]).Script;
            }
            start = end;
        }
        var previous = UnicodeScript.Common;
        for (var i = 0; i < _count; i++)
        {
            if (IsHardBreak(_scalars[i])) { previous = UnicodeScript.Common; continue; }
            if (TextScript.IsStrong(_scripts[i])) { previous = _scripts[i]; continue; }
            if (TextScript.IsStrong(previous) && TextScript.IsCompatible(_scalars[i], previous)) { _scripts[i] = previous; continue; }
            for (var next = i + 1; next < _count && !IsHardBreak(_scalars[next]); next++)
                if (TextScript.IsStrong(_scripts[next]) && TextScript.IsCompatible(_scalars[i], _scripts[next])) { _scripts[i] = _scripts[next]; break; }
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
        if (_count == 0) return;
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
            var occupied = richCross < _options.DropcapHeight ? _options.DropcapWidth : 0;
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
            AddLine(start, end, hard || end == _count); richCross += MathF.Ceiling(_lines[^1].Height) + _options.LineSpacing;
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
        var flags = Key.Breaks; var trimStart = (flags & (TextLineBreakFlags.TrimEdgeSpaces | TextLineBreakFlags.TrimStartEdgeSpaces)) != 0;
        var trimEnd = (flags & (TextLineBreakFlags.TrimEdgeSpaces | TextLineBreakFlags.TrimEndEdgeSpaces)) != 0;
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
                var glyph = new Glyph(face, face.GetGlyphIndex(0x00ad), end - 1, end, SoftHyphenAdvance(end - 1), Vector2.Zero, 0, false, false);
                _lineGlyphs.Insert(_paragraphLevels[start] == 1 ? 0 : _lineGlyphs.Count, glyph);
            }
        }
        var ascent = 0f; var descent = 0f;
        var sources = _font.GetSources(); var lastSource = _options.Styles is null && start < end && sources.Count > 0 ? 0 : -1;
        for (var i = start; i < end; i++)
            if (_options.Styles is null) lastSource = Math.Max(lastSource, _faces[i] is null ? sources.Count - 1 : sources.IndexOf(_faces[i]!));
            else
            {
                var style = StyleAt(i);
                ascent = Math.Max(ascent, style.Font.GetAscent(style.Size)); descent = Math.Max(descent, style.Font.GetDescent(style.Size));
            }
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
            ascent = Math.Max(ascent, metrics.Ascent + (_options.Styles is null ? 0 : styleFont.GetSpacing(TextSpacingType.Top)));
            descent = Math.Max(descent, metrics.Descent + (_options.Styles is null ? 0 : styleFont.GetSpacing(TextSpacingType.Bottom)));
            if (Key.Orientation == TextOrientation.Horizontal) { ascent = Math.Max(ascent, -glyph.Offset.Y); descent = Math.Max(descent, glyph.Offset.Y); }
            else { var halfAdvance = MathF.Round(glyph.Face.GetGlyphAdvance(glyph.Index, GlyphSize(glyph)) * .5f, MidpointRounding.AwayFromZero); ascent = Math.Max(ascent, halfAdvance); descent = Math.Max(descent, halfAdvance); }
        }
        if (ascent == 0 && descent == 0) { ascent = _font.GetAscent(Key.FontSize); descent = _font.GetDescent(Key.FontSize); }
        else if (_options.Styles is null) { ascent += _font.GetSpacing(TextSpacingType.Top); descent += _font.GetSpacing(TextSpacingType.Bottom); }
        var paragraphLevel = start < _count ? _paragraphLevels[start] : Key.Direction == TextDirection.RTL ? 1 : 0;
        var width = LayoutAdvance(); var glyphStart = _rawGlyphs.Count; _rawGlyphs.AddRange(_lineGlyphs);
        _lines.Add(new(start, end, glyphStart, _lineGlyphs.Count, width, ascent, descent, 0, paragraphEnd, paragraphLevel));
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
            var line = _lines[i]; _tabRTL = line.ParagraphLevel == 1; _lineGlyphs.Clear();
            for (var j = line.GlyphStart; j < line.GlyphStart + line.GlyphCount; j++) _lineGlyphs.Add(_rawGlyphs[j]);
            var width = line.Width; _lineTrimmed = false;
            var fill = Key.Alignment == HorizontalAlignment.Fill && i < justifyTo && Key.Width > 0;
            if (fill) width = Justify(line.Start, line.End, width);
            if (!wrapped || i == visible - 1 && (Key.Alignment == HorizontalAlignment.Fill && !fill || visible < _lines.Count))
            {
                TrimLine(line.ParagraphLevel, wrapped && visible < _lines.Count);
                width = LayoutAdvance();
                if (Key.Multiline && !wrapped && fill && _lineTrimmed) width = Justify(line.Start, line.End, width);
            }
            var glyphStart = _glyphs.Count; _glyphs.AddRange(_lineGlyphs);
            _lines[i] = line with { GlyphStart = glyphStart, GlyphCount = _lineGlyphs.Count, Width = width };
        }
    }

    private void ShapeLine(int start, int end)
    {
        _lineGlyphs.Clear(); _runs.Clear(); var length = end - start;
        if (length == 0) return; _tabRTL = _paragraphLevels[start] == 1;
        _scalars.AsSpan(start, length).CopyTo(_shapeScalars);
        if (_options.BIDIOverride is { Count: > 0 } contexts)
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
        foreach (var run in _runs)
        {
            var runStyle = StyleAt(run.Start); var inline = _richObjects[run.Start];
            if (inline.At >= 0) { var metrics = runStyle.Font.GetAscent(runStyle.Size); var descent = runStyle.Font.GetDescent(runStyle.Size); var anchor = ((int)inline.Alignment & 12) switch { 0 => -metrics, 4 => (descent - metrics) / 2, 8 => 0, _ => descent }; var own = ((int)inline.Alignment & 3) switch { 0 => 0, 1 => inline.Size.Y / 2, 2 => inline.Size.Y, _ => inline.Baseline >= 0 ? inline.Baseline : inline.Size.Y }; var top = anchor - own; _lineGlyphs.Add(new(null, 0, run.Start, run.End, inline.Size.X, Vector2.Zero, 0, false, false, Size: runStyle.Size, ObjectIndex: inline.Index, ObjectAscent: Math.Max(0, -top), ObjectDescent: Math.Max(0, top + inline.Size.Y))); continue; }
            if (_options.PreserveControl && _nonprinting[run.Start]) { AddMissingGlyph(run.Start); continue; }
            if (IsSpecial(_scalars[run.Start]))
            {
                var tab = _scalars[run.Start] == '\t';
                _lineGlyphs.Add(new(null, 0, run.Start, run.End, 0, Vector2.Zero, 0, tab, tab)); continue;
            }
            if (run.Face is null)
            {
                var reverse = Key.Orientation == TextOrientation.Horizontal && (run.Level & 1) != 0;
                for (var index = reverse ? run.End - 1 : run.Start; reverse ? index >= run.Start : index < run.End; index += reverse ? -1 : 1) AddMissingGlyph(index);
                continue;
            }
            var direction = Key.Orientation == TextOrientation.Vertical
                ? (run.Level & 1) == 0 ? NativeTextDirection.TTB : NativeTextDirection.BTT
                : (run.Level & 1) == 0 ? NativeTextDirection.LTR : NativeTextDirection.RTL;
            _shaped.Clear(); run.Face.Shape(_shapeScalars, run.Start - start, run.End - run.Start, runStyle.Size, direction,
                TextScript.ToTag(run.Script), runStyle.Language, _shaped, features: runStyle.Font.GetShapingFeatures(run.Face), textLength: length);
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
            for (var shapedIndex = 0; shapedIndex < _shaped.Count; shapedIndex++)
            {
                var shaped = _shaped[shapedIndex];
                var cluster = start + (int)shaped.Cluster; var clusterEnd = _clusterEnds[cluster];
                var advance = (Key.Orientation == TextOrientation.Vertical ? -shaped.YAdvance : shaped.XAdvance) / 64f;
                var space = IsSpace(_scalars[cluster]);
                if (shaped.GlyphIndex == 0 && !IsIgnorable(cluster))
                {
                    for (var scalar = cluster; scalar < clusterEnd; scalar++) AddMissingGlyph(scalar);
                    continue;
                }
                if (shaped.GlyphIndex == 0) advance = 0;
                if (advance != 0)
                {
                    var spaceSpacing = space ? runStyle.Font.GetSpacing(TextSpacingType.Space) : 0;
                    var extraSpacing = spaceSpacing != 0 ? spaceSpacing : runStyle.Font.GetSpacing(TextSpacingType.Glyph);
                    if (shapedIndex < lastAdvance) advance += extraSpacing;
                }
                _lineGlyphs.Add(new(run.Face, shaped.GlyphIndex, cluster, clusterEnd, advance,
                    shaped.GlyphIndex == 0 ? Vector2.Zero : new(shaped.XOffset / 64f, -shaped.YOffset / 64f), shaped.Flags, space, false, Size: runStyle.Size));
            }
        }
        LayoutAdvance();
    }

    private void AppendRuns(int start, int end, sbyte paragraphLevel)
    {
        var firstRun = _runs.Count; var trailing = end;
        while (trailing > start && (IsSpace(_scalars[trailing - 1]) || IsBIDIControl(_scalars[trailing - 1]))) trailing--;
        for (var i = start; i < end;)
        {
            var level = i >= trailing ? paragraphLevel : _levels[i];
            var runEnd = i + 1; var special = IsSpecial(_scalars[i]) || _richObjects[i].At >= 0 || _options.PreserveControl && _nonprinting[i];
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

    private void AddMissingGlyph(int index)
    {
        var scalar = _scalars[index];
        if (!_options.PreserveControl && (IsIgnorable(index) || scalar is 0x200b or 0x2060 or 0xfeff)) return;
        var size = TextMissingGlyph.Size(Key.FontSize, scalar);
        var vertical = Key.Orientation == TextOrientation.Vertical;
        var offset = vertical ? new Vector2(-MathF.Round(size.X * .5f, MidpointRounding.AwayFromZero), size.Y) : Vector2.Zero;
        _lineGlyphs.Add(new(null, scalar, index, index + 1, vertical ? size.Y : size.X, offset, 0, IsSpace(scalar), false, Missing: true));
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
        var width = LayoutAdvance(); var force = forceEllipsis || _options.Overrun is 5 or 6;
        if (width <= Key.Width && !force) return;
        _lineTrimmed = true;
        var addEllipsis = _options.Overrun >= 3;
        var wordOnly = _options.Overrun is 2 or 4 or 6;
        var ellipsisWidth = 0f;
        if (addEllipsis)
        {
            _ellipsis ??= new();
            var key = new TextLayoutKey(_options.Ellipsis ?? "\u2026", Key.FontSize, -1, HorizontalAlignment.Left, 1,
                TextLineBreakFlags.None, TextJustificationFlags.None, Key.Direction, Key.Orientation);
            _ellipsis.Build(_font, key, new TextLayoutOptions(Overrun: 0, VisibleCharacters: -1));
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
            while (_lineGlyphs.Count > 0)
            {
                var index = paragraphLevel == 1 ? 0 : _lineGlyphs.Count - 1; var glyph = _lineGlyphs[index];
                if (_breaks[glyph.End] || glyph.Space) break;
                _lineGlyphs.RemoveAt(index);
            }
        }
        if (addEllipsis && (force || ellipsisWidth <= Key.Width) && _ellipsis is not null)
        {
            var cluster = _lineGlyphs.Count > 0 ? _lineGlyphs[paragraphLevel == 1 ? 0 : _lineGlyphs.Count - 1].End : 0;
            var insert = paragraphLevel == 1 ? 0 : _lineGlyphs.Count;
            foreach (var glyph in _ellipsis._glyphs)
                _lineGlyphs.Insert(insert++, glyph with { Start = cluster, End = cluster, Virtual = true });
        }
    }

    private float Justify(int start, int end, float width)
    {
        if ((Key.Justification & TextJustificationFlags.ConstrainEllipsis) != 0 && !_lineTrimmed) return width;
        var begin = 0; var limit = _lineGlyphs.Count;
        if ((Key.Justification & TextJustificationFlags.AfterLastTab) != 0)
            for (var i = 0; i < limit; i++) if (_lineGlyphs[i].Tab) begin = i + 1;
        if ((Key.Justification & TextJustificationFlags.WordBound) != 0)
        {
            for (var i = begin; i < limit; i++)
            {
                var glyph = _lineGlyphs[i];
                if (glyph.Start <= start || glyph.Start >= end || !_wordEnds[glyph.Start] || glyph.Space || glyph.Virtual || glyph.Missing || glyph.Face is null) continue;
                var category = Rune.GetUnicodeCategory(new Rune(_scalars[glyph.Start]));
                if (category is System.Globalization.UnicodeCategory.ConnectorPunctuation or System.Globalization.UnicodeCategory.DashPunctuation or
                    System.Globalization.UnicodeCategory.OpenPunctuation or System.Globalization.UnicodeCategory.ClosePunctuation or
                    System.Globalization.UnicodeCategory.InitialQuotePunctuation or System.Globalization.UnicodeCategory.FinalQuotePunctuation or
                    System.Globalization.UnicodeCategory.OtherPunctuation) continue;
                var adjacent = (_levels[glyph.Start] & 1) == 0 ? i - 1 : i + 1;
                if (adjacent >= 0 && adjacent < limit && _lineGlyphs[adjacent].Space) continue;
                _lineGlyphs.Insert(i, new(glyph.Face, 0, glyph.Start, glyph.End, 0, Vector2.Zero, 0, true, false, Virtual: true));
                i++; limit++;
            }
        }
        if ((Key.Justification & TextJustificationFlags.Kashida) != 0 && Key.Orientation == TextOrientation.Horizontal && width < Key.Width)
        {
            var opportunities = 0;
            for (var wordStart = start; wordStart < end;)
            {
                var wordEnd = wordStart + 1; while (wordEnd < end && !_wordBoundaries[wordEnd]) wordEnd++;
                var position = _wordEnds[wordEnd] ? TextJustification.FindKashida(_scalars.AsSpan(0, _count), wordStart, wordEnd) : -1;
                if (position >= 0)
                {
                    for (var i = begin; i < limit; i++)
                    {
                        var glyph = _lineGlyphs[i];
                        if (glyph.Start != position || glyph.Face is null) continue;
                        if (glyph.Elongation) { opportunities++; break; }
                        if (_scalars[position] == 0x0640) { _lineGlyphs[i] = glyph with { Elongation = true }; opportunities++; break; }
                        if (i > 0 && (_lineGlyphs[i - 1].Flags & 4) == 0) continue;
                        var index = glyph.Face.GetGlyphIndex(0x0640); if (index == 0) break;
                        var advance = glyph.Face.GetGlyphAdvance(index, Key.FontSize); if (advance <= 0) break;
                        _lineGlyphs.Insert(i, new(glyph.Face, index, glyph.Start, glyph.End, advance, new(0, glyph.Offset.Y), 0, false, false, 0, true, Elongation: true));
                        opportunities++; limit++; break;
                    }
                }
                wordStart = wordEnd;
            }
            if (opportunities > 0)
            {
                var extra = (Key.Width - width) / opportunities;
                for (var i = begin; i < limit; i++)
                {
                    var glyph = _lineGlyphs[i]; if (!glyph.Elongation || glyph.Advance <= 0) continue;
                    var repeat = (int)Math.Clamp(Math.Floor(extra / glyph.Advance) + (glyph.Virtual ? 0 : 1), glyph.Virtual ? 0 : 1, 255);
                    _lineGlyphs[i] = glyph with { Repeat = repeat }; width += (repeat - glyph.Repeat) * glyph.Advance;
                }
            }
        }
        if ((Key.Justification & TextJustificationFlags.WordBound) != 0)
        {
            var spaces = 0;
            for (var i = begin; i < limit; i++) if (_lineGlyphs[i].Space && !_lineGlyphs[i].Tab && (_lineGlyphs[i].Advance > 0 || _lineGlyphs[i].Virtual)) spaces++;
            if (spaces > 0)
            {
                var extra = (Key.Width - width) / spaces;
                for (var i = begin; i < limit; i++)
                {
                    var glyph = _lineGlyphs[i]; if (!glyph.Space || glyph.Tab || glyph.Advance <= 0 && !glyph.Virtual) continue;
                    var advance = Math.Max(glyph.Virtual ? 0 : Key.FontSize * .1f, glyph.Advance + extra);
                    _lineGlyphs[i] = glyph with { Advance = advance }; width += advance - glyph.Advance;
                }
            }
        }
        return width;
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
            line = line with { CrossOffset = cross }; _lines[i] = line;
            var advance = align + (line.ParagraphLevel != 1 ? cap : 0);
            for (var j = line.GlyphStart; j < line.GlyphStart + line.GlyphCount; j++)
            {
                var glyph = _glyphs[j];
                var position = Key.Orientation == TextOrientation.Horizontal ? new Vector2(advance, cross + line.Ascent) : new Vector2(cross + line.Ascent, advance);
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
        int firstLine = 0, int maxLines = -1, int visibleCharacters = -1, int visibleBehavior = 1, bool outlinePass = false, bool clipToWidth = false, Rect2? clipRect = null, ReadOnlySpan<Color> characterColors = default)
    {
        if (IsBusy) throw new InvalidOperationException("An active text layout cannot record itself recursively.");
        for (var attempt = 0; attempt < Font.MaximumReadAttempts; attempt++)
        {
            using var read = _font.BeginRead();
            if (_builtFontGeneration != read.Generation) { Build(_font, Key, _options); continue; }
            if (!read.IsCurrent) continue;
            _active++;
            try { DrawCore(canvas, baseline, color, outline, oversampling, firstLine, maxLines, visibleCharacters, visibleBehavior, outlinePass, clipToWidth, clipRect, characterColors); return; }
            finally { _active--; }
        }
        throw Font.UnsettledRead();
    }

    private void DrawCore(CanvasItem canvas, Vector2 baseline, Color color, int outline, float oversampling,
        int firstLine, int maxLines, int visibleCharacters, int visibleBehavior, bool outlinePass, bool clipToWidth, Rect2? clipRect, ReadOnlySpan<Color> characterColors)
    {
        if (firstLine < 0 || firstLine >= _lines.Count) return;
        var end = Key.MaxLines < 0 ? _lines.Count : Math.Min(Key.MaxLines, _lines.Count);
        if (maxLines >= 0) end = Math.Min(end, firstLine + maxLines);
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
                if ((!glyph.Missing && (glyph.Face is null || glyph.Index == 0)) || glyph.Tab || glyph.Virtual && glyph.Index == 0) continue;
                if (glyph.Missing)
                {
                    if (clipToWidth && Key.Width > 0 && (glyph.Position.X - glyph.Offset.X < 0 || glyph.Position.X - glyph.Offset.X + glyph.Advance > Key.Width)) continue;
                    if (!outlinePass) TextMissingGlyph.Draw(canvas, GlyphSize(glyph), origin + glyph.Position, glyph.Index, glyphColor, clipRect, _options.PreserveControl);
                    continue;
                }
                for (var repeat = 0; repeat < glyph.Repeat; repeat++)
                {
                    if (clipToWidth && Key.Width > 0 && (!glyph.Virtual || glyph.Elongation))
                    {
                        var pen = glyph.Position.X - glyph.Offset.X + repeat * glyph.Advance;
                        if (pen < 0 || pen + glyph.Advance > Key.Width) continue;
                    }
                    var offset = Key.Orientation == TextOrientation.Horizontal ? new Vector2(repeat * glyph.Advance, 0) : new Vector2(0, repeat * glyph.Advance);
                    var position = origin + glyph.Position + offset;
                    var image = glyph.Face!.GetGlyph(glyph.Index, GlyphSize(glyph), outline, oversampling, position, out var rasterPosition);
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
        if (glyph.Colored && !modulateColor) color = new Color(1, 1, 1, color.A);
        if (glyph.Texture is not null && glyph.Size.X > 0 && glyph.Size.Y > 0)
        {
            var rect = new Rect2(baseline + glyph.Offset, glyph.Size);
            if (clipRect is { } clip)
            {
                var clipped = rect.Intersection(clip); if (!clipped.HasArea()) return;
                var region = glyph.Region ?? new Rect2(Vector2.Zero, glyph.Texture.GetSize());
                var scale = region.Size / glyph.Size;
                canvas.DrawTextureRectRegion(glyph.Texture, clipped, new Rect2(region.Position + (clipped.Position - rect.Position) * scale, clipped.Size * scale), color, clipUV: false);
            }
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
