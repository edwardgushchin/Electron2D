using System.Text;

namespace Electron2D;

internal sealed partial class TextLayout
{
    private static readonly uint[] ServerTatweelScalar = [0x0640];
    private readonly HashSet<uint> _customPunctuation = [];
    private string? _customPunctuationText;
    private int _serverVisibleStart, _serverVisibleCount;
    private float _serverEllipsisAdvance;
    private int _lineTrimPosition = -1, _lineEllipsisPosition = -1;
    private void PrepareCustomPunctuation()
    {
        var text = _options.CustomPunctuation ?? string.Empty; if (text == _customPunctuationText) return;
        _customPunctuation.Clear(); foreach (var rune in text.EnumerateRunes()) _customPunctuation.Add((uint)rune.Value); _customPunctuationText = text;
    }
    private bool IsCustomPunctuation(Rune rune) => !string.IsNullOrEmpty(_options.CustomPunctuation) ? _customPunctuation.Contains((uint)rune.Value) : IsPinnedPunctuation((uint)rune.Value) && rune.Value != '_';
    internal void ServerCopyFormatting(TextLayout target, bool shareSource = true)
    {
        // Same-owner workspaces share analysis; source snapshots retain independent arrays.
        if (shareSource)
        {
            target.Key = Key; target._options = _options; target._font = _font; target._count = _count; target.CharacterCount = CharacterCount;
            target._scalars = _scalars; target._shapeScalars = _shapeScalars; target._utf16 = _utf16; target._clusterEnds = _clusterEnds; target._advances = _advances;
            target._levels = _levels; target._paragraphLevels = _paragraphLevels; target._scripts = _scripts; target._faces = _faces;
            target._grapheme = _grapheme; target._breaks = _breaks; target._wordBoundaries = _wordBoundaries; target._wordEnds = _wordEnds; target._nonprinting = _nonprinting;
            target._richStyles = _richStyles; target._richObjects = _richObjects; target._builtFontGeneration = _builtFontGeneration;
        }
        CopyProviderFormattingTo(target);
        target._glyphs.Clear(); target._glyphs.AddRange(_glyphs); target._rawGlyphs.Clear(); target._rawGlyphs.AddRange(_glyphs);
        target._lines.Clear(); target._lines.AddRange(_lines); target.Size = Size; target.NaturalWidth = NaturalWidth; target.UnwrappedWidth = UnwrappedWidth;
        target._caretsReady = false; target._serverCharactersReady = false; target.PrepareCustomPunctuation();
        target._serverVisibleStart = _serverVisibleStart; target._serverVisibleCount = _serverVisibleCount; target._serverEllipsisAdvance = _serverEllipsisAdvance;
        if (target._options.ServerBuffers) target.PrepareServerWrapping();
    }
    private void PublishServerGlyphs(float width, int trim = -1, int ellipsis = -1)
    {
        if (!float.IsFinite(width)) throw new OverflowException("Text formatting exceeds finite geometry.");
        _glyphs.Clear(); _glyphs.AddRange(_lineGlyphs); _rawGlyphs.Clear(); _rawGlyphs.AddRange(_lineGlyphs); _caretsReady = false;
        if (_lines.Count != 0) _lines[0] = _lines[0] with { GlyphStart = 0, GlyphCount = _glyphs.Count, Width = width, TrimPosition = trim, EllipsisPosition = ellipsis };
        NaturalWidth = MathF.Ceiling(width); PositionLines(); if (_options.ServerBuffers) PrepareServerWrapping();
    }
    internal void ServerCopyDisplayLine(TextLayout target, TextLayout raw, int lineIndex, int from, int to)
    {
        if (CopyProviderSliceTo(target, from, to, lineIndex)) return;
        var line = _lines[lineIndex]; var original = raw._lines[lineIndex]; target._glyphs.Clear();
        for (var i = line.GlyphStart; i < line.GlyphStart + line.GlyphCount; i++)
        {
            var glyph = _glyphs[i]; var local = Math.Clamp(glyph.Start - from, 0, to - from); var end = Math.Clamp(glyph.End - from, 0, to - from);
            var objectIndex = glyph.ObjectIndex < 0 || local >= target._count ? -1 : target._richObjects[local].Index;
            target._glyphs.Add(glyph with { Start = local, End = end, ObjectIndex = objectIndex });
        }
        target._rawGlyphs.Clear(); target._rawGlyphs.AddRange(target._glyphs); target._lines.Clear(); target._lines.Add(line with { Start = 0, End = to - from, RangeEnd = to - from, GlyphStart = 0, CrossOffset = 0 });
        target._caretsReady = false; target._serverVisibleStart = line.ParagraphLevel == 1 && line.TrimPosition >= 0 ? line.TrimPosition : 0;
        target._serverVisibleCount = line.TrimPosition >= 0 ? line.ParagraphLevel == 1 ? original.GlyphCount - line.TrimPosition : line.TrimPosition : original.GlyphCount;
        var visibleAdvance = 0f;
        for (var i = original.GlyphStart + target._serverVisibleStart; i < original.GlyphStart + target._serverVisibleStart + target._serverVisibleCount; i++) visibleAdvance += raw._glyphs[i].Advance * raw._glyphs[i].Repeat;
        target._serverEllipsisAdvance = line.EllipsisPosition >= 0 ? line.Width - visibleAdvance : 0; target.PositionLines(); target.PrepareServerWrapping();
    }
    internal bool ServerAdoptFormattedDisplay(TextLayout display)
    {
        if (_providerLayout is not null) return false;
        if (display._lines.Count == 0 || _lines.Count == 0 || display._lines[0].TrimPosition < 0 && display._lines[0].EllipsisPosition < 0) return false;
        var first = display._serverVisibleStart; var previousCount = display._serverVisibleCount; var sourceCount = _glyphs.Count;
        _lineGlyphs.Clear(); for (var i = 0; i < first; i++) _lineGlyphs.Add(_glyphs[i]);
        var actualCount = 0; var ellipsisAdvance = 0f;
        foreach (var glyph in display._glyphs)
        { if (glyph.End > glyph.Start) { _lineGlyphs.Add(glyph); actualCount++; } else ellipsisAdvance += glyph.Advance * glyph.Repeat; }
        for (var i = first + previousCount; i < sourceCount; i++) _lineGlyphs.Add(_glyphs[i]);
        var advance = 0f; foreach (var glyph in _lineGlyphs) advance += glyph.Advance * glyph.Repeat; PublishServerGlyphs(advance);
        var line = display._lines[0]; var boundary = line.ParagraphLevel == 1 ? first : actualCount;
        display._lines[0] = line with { TrimPosition = line.TrimPosition < 0 ? -1 : boundary, EllipsisPosition = line.EllipsisPosition < 0 ? -1 : boundary };
        display._serverVisibleCount = actualCount; display._serverEllipsisAdvance = ellipsisAdvance;
        return true;
    }
    internal float ServerFit(float width, TextJustificationFlags flags)
    {
        if (_providerLayout is not null) return FitProvider(width, flags);
        if (_lines.Count == 0 || _glyphs.Count == 0) return _lines.Count == 0 ? 0 : _lines[0].Width;
        var original = Key; var line = _lines[0]; _lineGlyphs.Clear(); _lineGlyphs.AddRange(_glyphs);
        Key = Key with { Width = width, Justification = flags & ~TextJustificationFlags.ConstrainEllipsis }; _lineTrimmed = line.TrimPosition >= 0;
        try
        {
            var advance = line.Width; _tabRTL = line.ParagraphLevel == 1;
            advance = Justify(line.Start, line.End, advance, true); PublishServerGlyphs(advance); return advance;
        }
        finally { Key = original; }
    }
    private bool ServerJustificationEdge(Glyph glyph) => glyph.Start >= 0 && glyph.Start < _count && glyph.End > glyph.Start && _scalars[glyph.Start] != 0x00ad && (glyph.Space || glyph.Tab || IsHardBreak(_scalars[glyph.Start]));
    internal float ServerFitVisible(TextLayout display, float width, TextJustificationFlags flags)
    {
        if (display._providerLayout is not null)
        {
            var result = display.FitProvider(width, flags);
            display.ServerCopyFormatting(this);
            TrimProvider(0, TextOverrunFlags.NoTrim, _providerLayout!.Owner.ShapedTextGetCustomEllipsis(_providerLayout.Full));
            return result;
        }
        if (display._lines.Count == 0 || display._lines[0].TrimPosition < 0) return _lines.Count == 0 ? 0 : _lines[0].Width;
        var first = display._serverVisibleStart; var count = display._serverVisibleCount; var sourceCount = _glyphs.Count;
        display._lineGlyphs.Clear(); for (var i = first; i < first + count; i++) display._lineGlyphs.Add(_glyphs[i]);
        var original = display.Key; display.Key = Key with { Width = width - display._serverEllipsisAdvance, Justification = flags & ~TextJustificationFlags.ConstrainEllipsis };
        var oldEllipsisStart = display._lines[0].ParagraphLevel == 1 ? 0 : count;
        var ellipsisCount = display._glyphs.Count - count;
        try
        {
            display._lineTrimmed = true; display._tabRTL = display._lines[0].ParagraphLevel == 1; var visible = display.LayoutAdvance(); var result = display.Justify(0, _count, visible, true); var newCount = display._lineGlyphs.Count;
            _lineGlyphs.Clear(); for (var i = 0; i < first; i++) _lineGlyphs.Add(_glyphs[i]); _lineGlyphs.AddRange(display._lineGlyphs); for (var i = first + count; i < sourceCount; i++) _lineGlyphs.Add(_glyphs[i]);
            var rawWidth = 0f; foreach (var glyph in _lineGlyphs) rawWidth += glyph.Advance * glyph.Repeat; PublishServerGlyphs(rawWidth);
            if (display._lines[0].ParagraphLevel == 1) for (var i = 0; i < ellipsisCount; i++) display._lineGlyphs.Insert(i, display._glyphs[oldEllipsisStart + i]);
            else for (var i = 0; i < ellipsisCount; i++) display._lineGlyphs.Add(display._glyphs[oldEllipsisStart + i]);
            var trim = display._lines[0].ParagraphLevel == 1 ? first : newCount; var ellipsis = display._lines[0].EllipsisPosition < 0 ? -1 : trim;
            display.PublishServerGlyphs(result + display._serverEllipsisAdvance, trim, ellipsis); display._serverVisibleCount = newCount; return result + display._serverEllipsisAdvance;
        }
        finally { display.Key = original; }
    }
    internal void ServerReapplyTrim(TextLayout previous, TextLayout previousRaw)
    {
        if (_lines.Count == 0 || previous._lines.Count == 0) return;
        var oldLine = previous._lines[0]; if (oldLine.TrimPosition < 0 && oldLine.EllipsisPosition < 0) return;
        if (_providerLayout is not null && previous._providerLayout is { TrimLine: 0 } policy)
        { TrimProvider(policy.TrimWidth, policy.TrimFlags, policy.Owner.ShapedTextGetCustomEllipsis(policy.Full)); return; }
        var oldFirst = previous._serverVisibleStart; var oldLast = oldFirst + previous._serverVisibleCount;
        var first = oldFirst == 0 ? 0 : FindServerCluster(previousRaw._glyphs[oldFirst]);
        var last = oldLast == previousRaw._glyphs.Count ? _glyphs.Count : FindServerCluster(previousRaw._glyphs[oldLast]);
        var rtl = oldLine.ParagraphLevel == 1; var payload = previous._glyphs.Count - previous._serverVisibleCount;
        _lineGlyphs.Clear(); var width = 0f;
        if (rtl) for (var i = 0; i < payload; i++) _lineGlyphs.Add(previous._glyphs[i]);
        for (var i = first; i < last; i++) { _lineGlyphs.Add(_glyphs[i]); width += _glyphs[i].Advance * _glyphs[i].Repeat; }
        if (!rtl) for (var i = previous._serverVisibleCount; i < previous._glyphs.Count; i++) _lineGlyphs.Add(previous._glyphs[i]);
        _serverVisibleStart = first; _serverVisibleCount = last - first; _serverEllipsisAdvance = previous._serverEllipsisAdvance;
        var boundary = rtl ? first : last;
        PublishServerGlyphs(width + _serverEllipsisAdvance, oldLine.TrimPosition < 0 ? -1 : boundary, oldLine.EllipsisPosition < 0 ? -1 : boundary);
    }
    private int FindServerCluster(Glyph anchor)
    {
        for (var i = 0; i < _glyphs.Count; i++) if (_glyphs[i].Start == anchor.Start && _glyphs[i].End == anchor.End) return i;
        throw new InvalidOperationException("A fitted line lost an original cluster.");
    }
    internal void ServerTrim(float width, TextOverrunFlags flags, int scalar, bool minimumReached)
    {
        if (_providerLayout is not null) { TrimProvider(width, flags, scalar); return; }
        if (_lines.Count == 0) return;
        var line = _lines[0]; var total = _glyphs.Count; _serverVisibleStart = 0; _serverVisibleCount = total; _serverEllipsisAdvance = 0;
        _lines[0] = line with { TrimPosition = -1, EllipsisPosition = -1 };
        var force = flags.HasFlag(TextOverrunFlags.EnforceEllipsis); var shortString = flags.HasFlag(TextOverrunFlags.ShortStringEllipsis);
        if (!flags.HasFlag(TextOverrunFlags.Trim) || width <= 0 || total == 0 || line.Width <= width && !force || flags.HasFlag(TextOverrunFlags.JustificationAware) && !minimumReached) return;
        var rtl = line.ParagraphLevel == 1; var word = flags.HasFlag(TextOverrunFlags.TrimWordOnly); var add = flags.HasFlag(TextOverrunFlags.AddEllipsis);
        var gap = 0f; var dot = add ? ServerEllipsisGlyph(scalar, out gap) : null; var trailingGap = add ? gap : 0;
        Glyph? space = add && word ? ServerEllipsisGlyph(' ', out _, repeatedFallback: false) : null;
        var ellipsisAdvance = dot is { } d ? d.Advance * d.Repeat + trailingGap + (space?.Advance ?? 0) : 0;
        var first = 0; var last = total; var advance = line.Width;
        TrimServerRange(width - (add ? ellipsisAdvance : 0), word, rtl, ref first, ref last, ref advance);
        if (add && !force && !shortString && last - first < 6)
        { add = false; first = 0; last = total; advance = line.Width; TrimServerRange(width, word, rtl, ref first, ref last, ref advance); }
        _serverVisibleStart = first; _serverVisibleCount = last - first; _serverEllipsisAdvance = add ? ellipsisAdvance : 0;
        var trimmed = first != 0 || last != total; var trim = trimmed ? rtl ? first : last : -1; var ellipse = add && dot is not null ? rtl ? first : last : -1;
        _lineGlyphs.Clear();
        var cluster = last > first ? _glyphs[rtl ? first : last - 1].End : line.Start;
        if (rtl && add) AppendServerEllipsis(dot, space, trailingGap, cluster, true);
        for (var i = first; i < last; i++) _lineGlyphs.Add(_glyphs[i]);
        if (!rtl && add) AppendServerEllipsis(dot, space, trailingGap, cluster, false);
        PublishServerGlyphs(advance + _serverEllipsisAdvance, trim, ellipse);
    }
    private void TrimServerRange(float available, bool word, bool rtl, ref int first, ref int last, ref float advance)
    {
        while (last > first && advance > available)
        {
            var at = rtl ? first : last - 1; var start = _glyphs[at].Start; var end = _glyphs[at].End;
            do { at = rtl ? first++ : --last; advance -= _glyphs[at].Advance * _glyphs[at].Repeat; } while (last > first && _glyphs[rtl ? first : last - 1].Start == start && _glyphs[rtl ? first : last - 1].End == end);
        }
        if (word && last - first >= 6)
        {
            while (last - first >= 6)
            {
                var at = rtl ? first : last - 1; var glyph = _glyphs[at];
                if (_breaks[glyph.End] || glyph.Space || glyph.Tab) break;
                var start = glyph.Start; var end = glyph.End;
                do { at = rtl ? first++ : --last; advance -= _glyphs[at].Advance * _glyphs[at].Repeat; } while (last > first && _glyphs[rtl ? first : last - 1].Start == start && _glyphs[rtl ? first : last - 1].End == end);
            }
        }
    }
    private Glyph? ServerEllipsisGlyph(int scalar, out float gap, bool repeatedFallback = true)
    {
        gap = 0; if (_glyphs.Count == 0) return null; var last = _glyphs[^1]; var style = StyleAt(Math.Clamp(last.Start, 0, _count - 1)); var size = GlyphSize(last); var face = last.Face;
        var index = face?.GetGlyphIndex((uint)scalar) ?? 0;
        if (index == 0) foreach (var candidate in style.Font.GetSources()) { var found = candidate.GetGlyphIndex((uint)scalar); if (found == 0) continue; face = candidate; index = found; break; }
        var repeat = 1;
        if (index == 0 && repeatedFallback)
        {
            repeat = 3; index = face?.GetGlyphIndex('.') ?? 0;
            if (index == 0) foreach (var candidate in style.Font.GetSources()) { var found = candidate.GetGlyphIndex('.'); if (found == 0) continue; face = candidate; index = found; break; }
        }
        if (index == 0 || face is null) return null;
        if (repeatedFallback) gap = style.Font.GetShapingSpacing(face, TextSpacingType.Glyph) + (float)_options.BufferGlyphSpacing;
        return new(face, index, 0, 0, face.GetGlyphAdvance(index, size, Key.Orientation == TextOrientation.Vertical), Vector2.Zero, 0, scalar == ' ', false, _lines[0].ParagraphLevel == 1, repeat, true, Size: size, SourceRID: style.Font.GetSourceRID(face), Ellipsis: true);
    }
    private void AppendServerEllipsis(Glyph? dot, Glyph? space, float gap, int cluster, bool rtl)
    {
        if (dot is not { } value) return;
        if (!rtl && space is { } leading) _lineGlyphs.Add(leading with { Start = cluster, End = cluster });
        _lineGlyphs.Add(value with { Start = cluster, End = cluster });
        if (gap != 0) _lineGlyphs.Add(new(null, 0, cluster, cluster, gap, Vector2.Zero, 0, false, false, rtl, Virtual: true, Ellipsis: true));
        if (rtl && space is { } trailing) _lineGlyphs.Add(trailing with { Start = cluster, End = cluster });
    }
}
