using Electron2D.TextFormatting.Unicode;

namespace Electron2D;

internal sealed partial class TextLayout
{
    private readonly List<int> _serverCharacters = [];
    private bool _serverCharactersReady;
    private void PrepareServerCharacters()
    {
        if (_serverCharactersReady) return;
        _serverCharacters.Clear();
        for (var start = 0; start < _count;)
        {
            var end = start + 1; var language = StyleAt(start).Language;
            while (end < _count && StyleAt(end).Language == language) end++;
            var utf16 = _utf16[start]; var iterator = new GraphemeEnumerator(Key.Text.AsSpan(utf16, _utf16[end] - utf16));
            while (iterator.MoveNext(out var grapheme)) _serverCharacters.Add(ScalarAtUTF16(utf16 + grapheme.Offset + grapheme.Length));
            start = end;
        }
        _serverCharactersReady = true;
    }
    internal int[] ServerCharacterBreaks(int start, int end, int offset)
    {
        PrepareServerCharacters(); var count = 0;
        foreach (var at in _serverCharacters) if (at > start && at <= end) count++;
        var result = new int[count]; count = 0;
        foreach (var at in _serverCharacters) if (at > start && at <= end) result[count++] = at + offset;
        return result;
    }
    internal int ServerCharacterPosition(int start, int end, int offset, int position, int mode)
    {
        PrepareServerCharacters(); var previous = start + offset;
        foreach (var local in _serverCharacters)
        {
            if (local <= start || local > end) continue;
            var current = local + offset;
            if (mode < 0 && current >= position) return previous;
            if (mode > 0 && current > position) return current;
            if (mode == 0 && current >= position) return current == position || (long)current - position < (long)position - previous ? current : previous;
            previous = current;
        }
        return previous;
    }
    private int ServerClusterEnd(int index, int limit, out float advance)
    {
        var first = _glyphs[index]; advance = first.Advance * first.Repeat; var next = index + 1;
        while (next < limit && _glyphs[next].Start == first.Start && _glyphs[next].End == first.End && _glyphs[next].Virtual == first.Virtual && _glyphs[next].ObjectIndex == first.ObjectIndex)
        { advance += _glyphs[next].Advance * _glyphs[next].Repeat; next++; }
        return next;
    }
    internal int ServerHitGrapheme(int line, float coordinate)
    {
        if ((uint)line >= (uint)_lines.Count) return -1;
        var metrics = _lines[line]; var pen = 0f;
        for (var i = metrics.GlyphStart; i < metrics.GlyphStart + metrics.GlyphCount; i++)
        { var advance = _glyphs[i].Advance * _glyphs[i].Repeat; if (coordinate >= pen && coordinate < pen + advance) return i - metrics.GlyphStart; pen += advance; }
        return -1;
    }
    internal int? ServerGraphemePosition(int line, long position, bool next)
    {
        if ((uint)line >= (uint)_lines.Count) return null;
        var metrics = _lines[line];
        for (var i = metrics.GlyphStart; i < metrics.GlyphStart + metrics.GlyphCount; i++)
        { var g = _glyphs[i]; if (next ? position >= g.Start && position < g.End : position > g.Start && position <= g.End) return next ? g.End : g.Start; }
        return null;
    }
    internal Vector2 ServerGraphemeBounds(int line, long position)
    {
        if ((uint)line >= (uint)_lines.Count) return default;
        var metrics = _lines[line]; var pen = 0f; var limit = metrics.GlyphStart + metrics.GlyphCount;
        for (var i = metrics.GlyphStart; i < limit;)
        {
            var glyph = _glyphs[i]; var next = ServerClusterEnd(i, limit, out var advance);
            if ((glyph.Index != 0 || glyph.Space) && glyph.Start <= position && glyph.End >= position) return new(pen, pen + advance);
            pen += advance; i = next;
        }
        return default;
    }
    internal bool ServerVisible(int line)
    {
        if ((uint)line >= (uint)_lines.Count) return false;
        var metrics = _lines[line];
        for (var i = metrics.GlyphStart; i < metrics.GlyphStart + metrics.GlyphCount; i++) if (_glyphs[i].Index != 0 && !_glyphs[i].Virtual) return true;
        return false;
    }
    internal TextDirection ServerDominantDirection(int line, long from, long to)
    {
        if ((uint)line >= (uint)_lines.Count) return TextDirection.Auto;
        if (from > to) (from, to) = (to, from);
        if (from == to) return TextDirection.Auto;
        var metrics = _lines[line]; var ltr = 0; var rtl = 0; var limit = metrics.GlyphStart + metrics.GlyphCount;
        for (var i = metrics.GlyphStart; i < limit;)
        { var glyph = _glyphs[i]; if (glyph.Start < to && glyph.End > from) { if (glyph.RTL) rtl++; else ltr++; } i = ServerClusterEnd(i, limit, out _); }
        return ltr == rtl ? TextDirection.Auto : ltr > rtl ? TextDirection.LTR : TextDirection.RTL;
    }
    internal Vector2[] ServerSelection(int line, long from, long to)
    {
        if ((uint)line >= (uint)_lines.Count) return [];
        if (from == to) return [];
        if (from > to) (from, to) = (to, from);
        var ranges = new List<Vector2>(); var metrics = _lines[line]; var pen = 0f; var limit = metrics.GlyphStart + metrics.GlyphCount;
        for (var i = metrics.GlyphStart; i < limit;)
        {
            var glyph = _glyphs[i]; var next = ServerClusterEnd(i, limit, out var advance);
            if ((glyph.Index != 0 || glyph.Space) && glyph.End > glyph.Start && glyph.Start < to && glyph.End > from)
            {
                var start = Math.Max(from, glyph.Start); var end = Math.Min(to, glyph.End); var rtl = glyph.RTL;
                var count = glyph.End - glyph.Start; var step = advance / count;
                var left = rtl ? glyph.End - end : start - glyph.Start; var right = rtl ? glyph.End - start : end - glyph.Start;
                var lo = left == 0 ? pen : pen + step * left; var hi = right == count ? pen + advance : pen + step * right;
                if (ranges.Count > 0 && Mathf.IsEqualApprox(ranges[^1].Y, lo, SelectionAdjacencyTolerance)) ranges[^1] = new(ranges[^1].X, hi); else ranges.Add(new(lo, hi));
            }
            pen += advance; i = next;
        }
        return ranges.ToArray();
    }
    private Rect2 ServerCaretRect(float advance, float cross, float advanceSize, float crossSize) => Key.Orientation == TextOrientation.Horizontal
        ? new(advance, cross, advanceSize, crossSize) : new(cross, advance, crossSize, advanceSize);
    internal TextCaretInfo ServerCarets(int line, long position, int rangeStart, int rangeEnd)
    {
        if ((uint)line >= (uint)_lines.Count) return default;
        if (position < rangeStart || position > rangeEnd) return default;
        var metrics = _lines[line]; var height = metrics.Height; var half = height * .5f; var pen = 0f; var limit = metrics.GlyphStart + metrics.GlyphCount;
        Rect2 leading = default, trailing = default; var leadingDirection = TextDirection.Auto; var trailingDirection = TextDirection.Auto;
        for (var i = metrics.GlyphStart; i < limit;)
        {
            var glyph = _glyphs[i]; var next = ServerClusterEnd(i, limit, out var advance);
            if (!glyph.Virtual && glyph.End > glyph.Start)
            {
                var rtl = glyph.RTL; var step = advance / (glyph.End - glyph.Start); var direction = rtl ? TextDirection.RTL : TextDirection.LTR;
                if (position == glyph.Start)
                { trailing = ServerCaretRect(pen + (rtl ? advance : 0), -metrics.Ascent, rtl ? -step : step, glyph.Start == rangeStart ? height : half); trailingDirection = direction; }
                if (position == glyph.End)
                { var edge = glyph.End == rangeEnd; leading = ServerCaretRect(pen + (rtl ? 0 : advance), -metrics.Ascent + (edge ? 0 : half), rtl ? step : -step, edge ? height : half); leadingDirection = direction; }
                if (position > glyph.Start && position < glyph.End)
                { leading = trailing = ServerCaretRect(pen + step * (rtl ? glyph.End - position : position - glyph.Start), -metrics.Ascent, rtl ? -step : step, height); }
            }
            pen += advance; i = next;
        }
        return new(leading, leadingDirection, trailing, trailingDirection);
    }
}
