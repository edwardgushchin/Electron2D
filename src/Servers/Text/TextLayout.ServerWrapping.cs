namespace Electron2D;

internal sealed partial class TextLayout
{
    private readonly List<int> _serverLogicalGlyphs = [];
    private Comparison<int>? _serverGlyphOrder;
    private readonly List<ServerBreakCluster> _serverBreakClusters = [];
    private readonly record struct ServerBreakCluster(int Start, int End, float Advance, bool Space, bool Hard, bool Soft, bool Hyphen, float HyphenAdvance);
    private void PrepareStyleBreaks()
    {
        if (_options.Styles is null || _count == 0) return;
        for (var start = 0; start < _count;)
        {
            var end = start + 1; var language = StyleAt(start).Language; var startsWithObject = _richObjects[start].At >= 0;
            while (end < _count && (startsWithObject || _richObjects[end].At >= 0 || StyleAt(end).Language == language))
            {
                if (startsWithObject && _richObjects[end].At < 0) { language = StyleAt(end).Language; startsWithObject = false; }
                end++;
            }
            var offsets = new int[end - start + 1];
            for (var i = 0; i < offsets.Length; i++) offsets[i] = _utf16[start + i] - _utf16[start];
            var boundary = _breaks[start];
            NativeTextBreak.Fill(Key.Text[_utf16[start].._utf16[end]], language, offsets, _breaks.AsSpan(start, offsets.Length), _wordBoundaries.AsSpan(start, offsets.Length), _wordEnds.AsSpan(start, offsets.Length));
            _breaks[start] |= boundary; start = end;
        }
    }
    private void PrepareServerWrapping()
    {
        _serverLogicalGlyphs.Clear(); _serverBreakClusters.Clear();
        for (var i = 0; i < _glyphs.Count; i++) if (_glyphs[i].Start >= 0 && _glyphs[i].Start < _count && _glyphs[i].End > _glyphs[i].Start && _glyphs[i].End <= _count) _serverLogicalGlyphs.Add(i);
        _serverLogicalGlyphs.Sort(_serverGlyphOrder ??= CompareServerGlyphs);
        foreach (var index in _serverLogicalGlyphs)
        {
            var glyph = _glyphs[index]; var advance = glyph.Advance * glyph.Repeat;
            if (_serverBreakClusters.Count > 0 && _serverBreakClusters[^1].Start == glyph.Start && _serverBreakClusters[^1].End == glyph.End)
            { _serverBreakClusters[^1] = _serverBreakClusters[^1] with { Advance = _serverBreakClusters[^1].Advance + advance }; continue; }
            var scalar = _scalars[glyph.Start];
            if (_serverBreakClusters.Count > 0 && scalar == '\n' && _serverBreakClusters[^1] is { Hard: true } previous && _scalars[previous.Start] == '\r' && previous.End == glyph.Start)
            { _serverBreakClusters[^1] = previous with { End = glyph.End, Advance = previous.Advance + advance }; continue; }
            var hyphen = scalar == 0x00ad; var hyphenAdvance = 0f;
            if (hyphen && glyph.Face is { } face) hyphenAdvance = face.GetGlyphAdvance(face.GetGlyphIndex(scalar), GlyphSize(glyph), Key.Orientation == TextOrientation.Vertical);
            _serverBreakClusters.Add(new(glyph.Start, glyph.End, advance, glyph.Space || glyph.Tab, IsHardBreak(scalar), _breaks[glyph.End], hyphen, hyphenAdvance));
        }
    }
    private int CompareServerGlyphs(int a, int b)
    { var order = _glyphs[a].Start.CompareTo(_glyphs[b].Start); return order == 0 ? a.CompareTo(b) : order; }
    internal int[] ServerLineBreaks(ReadOnlySpan<float> widths, int start, int rangeStart, int end, int offset, bool once, TextLineBreakFlags flags)
    {
        var first = 0; var limit = _serverBreakClusters.Count;
        while (first < limit && _serverBreakClusters[first].Start < start) first++;
        while (limit > first && _serverBreakClusters[limit - 1].End > end) limit--;
        if (first == limit) return [start + offset, end + offset];
        var indents = new float[widths.Length];
        if (flags.HasFlag(TextLineBreakFlags.TrimIndent)) for (var columnIndex = 0; columnIndex < widths.Length; columnIndex++)
            {
                var available = widths[columnIndex]; if (available <= 0) continue; var indent = 0f;
                for (var j = 0; j < limit && _serverBreakClusters[j].Space; j++)
                { var advance = _serverBreakClusters[j].Advance; if (indent + advance > available) indent = 0; indent += advance; }
                indents[columnIndex] = Math.Min(indent, .6f * available);
            }
        var lines = new List<int>(); var lineStart = start; var lineFirst = first; var safe = -1; var words = 0; var column = 0; var width = 0f; var trimNext = false;
        var word = flags.HasFlag(TextLineBreakFlags.WordBound); var grapheme = flags.HasFlag(TextLineBreakFlags.GraphemeBound); var adaptive = word && flags.HasFlag(TextLineBreakFlags.Adaptive);
        for (var i = first; i < limit; i++)
        {
            var cluster = _serverBreakClusters[i]; var available = widths[column];
            if (trimNext) available -= indents[column];
            if (available > 0 && width + cluster.Advance > available && safe >= lineFirst)
            {
                AppendServerLine(lines, lineFirst, safe, lineStart, offset, flags, trimNext, true);
                lineStart = _serverBreakClusters[safe].End; lineFirst = safe + 1; i = safe; safe = -1; width = 0; words = 0; trimNext = true;
                if (++column == widths.Length) { column = 0; if (once) return lines.ToArray(); }
                continue;
            }
            if (flags.HasFlag(TextLineBreakFlags.Mandatory) && cluster.Hard)
            {
                AppendServerLine(lines, lineFirst, i, lineStart, offset, flags, trimNext, false);
                lineStart = cluster.End; lineFirst = i + 1; safe = -1; width = 0; words = 0; column = 0; trimNext = true;
                if (once) return lines.ToArray();
                continue;
            }
            if (word && cluster.Soft && (!cluster.Hyphen || width + cluster.Advance + cluster.HyphenAdvance <= widths[column] || cluster.HyphenAdvance >= widths[column])) { safe = i; words++; }
            if (grapheme || adaptive && words == 0) safe = i;
            width += cluster.Advance;
        }
        if (lineFirst < limit)
        {
            if (trimNext && flags.HasFlag(TextLineBreakFlags.TrimStartEdgeSpaces))
                while (lineFirst < limit - 1 && ServerTrimCluster(_serverBreakClusters[lineFirst], false)) lineFirst++;
            lines.Add(Math.Max(lineStart, _serverBreakClusters[lineFirst].Start) + offset); lines.Add(end + offset);
        }
        return lines.ToArray();
    }
    private void AppendServerLine(List<int> lines, int first, int last, int start, int offset, TextLineBreakFlags flags, bool trimNext, bool softWrap)
    {
        var trimmed = flags.HasFlag(TextLineBreakFlags.TrimStartEdgeSpaces) || flags.HasFlag(TextLineBreakFlags.TrimEndEdgeSpaces); var originalLast = last;
        if (trimNext && flags.HasFlag(TextLineBreakFlags.TrimStartEdgeSpaces)) while (first <= last && ServerTrimCluster(_serverBreakClusters[first], softWrap)) first++;
        if (flags.HasFlag(TextLineBreakFlags.TrimEndEdgeSpaces)) while (last >= first && ServerTrimCluster(_serverBreakClusters[last], softWrap)) last--;
        var from = first > originalLast ? _serverBreakClusters[originalLast].End : trimmed ? _serverBreakClusters[first].Start : start;
        var to = last < first ? from : _serverBreakClusters[last].End;
        lines.Add(from + offset); lines.Add(to + offset);
    }
    private static bool ServerTrimCluster(ServerBreakCluster cluster, bool softWrap) => !(softWrap && cluster.Hyphen) && (cluster.Space || cluster.Hard || cluster.Hyphen);
    internal void ServerCopySlice(TextLayout target, int from, int to)
    {
        PrepareRuns(from, to); target._glyphs.Clear(); target._rawGlyphs.Clear(); target._caretsReady = false;
        foreach (var run in _runs)
        {
            var begin = LogicalGlyphAt(run.Start); var finish = LogicalGlyphAt(run.End); var rtl = (run.Level & 1) != 0;
            for (var cursor = rtl ? finish - 1 : begin; rtl ? cursor >= begin : cursor < finish;)
            {
                var first = cursor; var last = cursor + 1;
                if (rtl) while (first > begin && _glyphs[_serverLogicalGlyphs[first - 1]].Start == _glyphs[_serverLogicalGlyphs[cursor]].Start) first--;
                else while (last < finish && _glyphs[_serverLogicalGlyphs[last]].Start == _glyphs[_serverLogicalGlyphs[cursor]].Start) last++;
                for (var index = first; index < last; index++)
                {
                    var glyph = _glyphs[_serverLogicalGlyphs[index]]; if (glyph.End > run.End || glyph.End > to) continue;
                    var local = glyph.Start - from; var objectIndex = glyph.ObjectIndex < 0 ? -1 : target._richObjects[local].Index;
                    if (glyph.End == to && _scalars[glyph.Start] == 0x00ad && glyph.Face is { } face)
                        glyph = glyph with { Index = face.GetGlyphIndex(0x00ad), Advance = face.GetGlyphAdvance(face.GetGlyphIndex(0x00ad), GlyphSize(glyph), Key.Orientation == TextOrientation.Vertical) };
                    if (target._glyphs.Count == 0 && glyph.Offset.X < 0) glyph = glyph with { Advance = glyph.Advance - glyph.Offset.X, Offset = new(0, glyph.Offset.Y) };
                    target._glyphs.Add(glyph with { Start = local, End = glyph.End - from, ObjectIndex = objectIndex, RTL = rtl });
                }
                cursor = rtl ? first - 1 : last;
            }
        }
        var ascent = 0f; var descent = 0f; var width = 0f; var underline = 0f; var thickness = 0f;
        foreach (var glyph in target._glyphs)
        {
            width += glyph.Advance * glyph.Repeat;
            if (glyph.ObjectIndex >= 0) { ascent = Math.Max(ascent, glyph.ObjectAscent); descent = Math.Max(descent, glyph.ObjectDescent); continue; }
            var style = target.StyleAt(glyph.Start); var metrics = style.Font.GetShapingMetrics(glyph.Face, style.Size);
            ascent = Math.Max(ascent, metrics.Ascent); descent = Math.Max(descent, metrics.Descent);
            underline = Math.Max(underline, style.Font.GetShapingUnderline(glyph.Face, style.Size, false)); thickness = Math.Max(thickness, style.Font.GetShapingUnderline(glyph.Face, style.Size, true));
            if (glyph.Missing) { var box = TextMissingGlyph.Size(target.GlyphSize(glyph), glyph.Index); if (Key.Orientation == TextOrientation.Horizontal) { ascent = Math.Max(ascent, box.Y * .85f); descent = Math.Max(descent, box.Y * .15f); } else { ascent = Math.Max(ascent, box.X * .5f); descent = Math.Max(descent, box.X * .5f); } }
        }
        target._rawGlyphs.AddRange(target._glyphs); target._lines.Clear();
        if (from < to) target._lines.Add(new(0, to - from, 0, target._glyphs.Count, width, ascent, descent, 0, true, _paragraphLevels[from], UnderlinePosition: underline, UnderlineThickness: thickness));
        target.NaturalWidth = target.UnwrappedWidth = MathF.Ceiling(width); target.PositionLines(); target.PrepareServerWrapping();
    }
    internal int ServerScalarUTF16(int scalar) => _utf16[scalar];
    private int LogicalGlyphAt(int scalar)
    {
        var lo = 0; var hi = _serverLogicalGlyphs.Count;
        while (lo < hi) { var mid = lo + (hi - lo) / 2; if (_glyphs[_serverLogicalGlyphs[mid]].Start < scalar) lo = mid + 1; else hi = mid; }
        return lo;
    }
}
