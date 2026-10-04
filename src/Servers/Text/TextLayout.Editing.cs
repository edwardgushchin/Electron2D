namespace Electron2D;

internal sealed partial class TextLayout
{
    internal int NextGrapheme(int column) { column = Math.Clamp(column, 0, _count); do { column++; } while (column < _count && !_grapheme[column]); return Math.Min(column, _count); }
    internal int PreviousGrapheme(int column) { column = Math.Clamp(column, 0, _count); do { column--; } while (column > 0 && !_grapheme[column]); return Math.Max(0, column); }
    internal int ClosestGrapheme(int column)
    {
        column = Math.Clamp(column, 0, _count); if (_grapheme[column]) return column;
        var left = column; var right = column;
        while (left > 0 && !_grapheme[left]) left--; while (right < _count && !_grapheme[right]) right++;
        return column - left < right - column ? left : right;
    }
    internal int PreviousWord(int column)
    {
        column = Math.Clamp(column, 0, _count);
        while (--column > 0)
        {
            if (!_wordBoundaries[column]) continue;
            var next = column + 1; while (next < _count && !_wordBoundaries[next]) next++;
            if (_wordEnds[next]) return column;
        }
        return 0;
    }
    internal int NextWord(int column)
    {
        column = Math.Clamp(column, 0, _count); do { column++; } while (column < _count && !_wordEnds[column]); return Math.Min(column, _count);
    }
    internal TextDirection DominantDirection(int from, int to)
    {
        var ltr = 0; var rtl = 0;
        for (var i = Math.Max(0, from); i < Math.Min(_count, to); i++) if ((_levels[i] & 1) == 0) ltr++; else rtl++;
        return ltr == rtl ? TextDirection.Auto : ltr > rtl ? TextDirection.LTR : TextDirection.RTL;
    }
    private (float? Leading, float? Trailing, TextDirection LeadingDirection, TextDirection TrailingDirection)[] _carets = [];
    private bool _caretsReady;
    internal (float? Leading, float? Trailing, TextDirection LeadingDirection, TextDirection TrailingDirection) Carets(int column)
    {
        if (!_caretsReady) PrepareCarets();
        return _carets[Math.Clamp(column, 0, _count)];
    }
    private void PrepareCarets()
    {
        Ensure(ref _carets, _count + 1); Array.Clear(_carets, 0, _count + 1);
        for (var i = 0; i < _glyphs.Count;)
        {
            var glyph = _glyphs[i]; var end = i + 1; var advance = glyph.Advance * glyph.Repeat;
            while (end < _glyphs.Count && _glyphs[end].Start == glyph.Start && _glyphs[end].End == glyph.End) { advance += _glyphs[end].Advance * _glyphs[end].Repeat; end++; }
            if (!glyph.Virtual && glyph.End > glyph.Start)
            {
                var rtl = (_levels[glyph.Start] & 1) != 0; var direction = rtl ? TextDirection.RTL : TextDirection.LTR;
                var start = glyph.Position.X - glyph.Offset.X;
                for (var column = glyph.Start; column <= glyph.End; column++)
                {
                    var x = start + advance * (rtl ? glyph.End - column : column - glyph.Start) / (glyph.End - glyph.Start);
                    ref var caret = ref _carets[column];
                    if (column == glyph.Start) { caret.Trailing = x; caret.TrailingDirection = direction; }
                    else if (column == glyph.End) { caret.Leading = x; caret.LeadingDirection = direction; }
                    else caret = (x, x, TextDirection.Auto, TextDirection.Auto);
                }
            }
            i = end;
        }
        _caretsReady = true;
    }
    internal float CaretX(int column, TextDirection inputDirection)
    {
        var caret = Carets(column);
        return caret.Leading is { } l && (caret.LeadingDirection == TextDirection.Auto || caret.LeadingDirection == inputDirection || caret.Trailing is null) ? l : caret.Trailing ?? caret.Leading ?? 0;
    }
    internal int HitColumn(float position, bool midGrapheme)
    {
        var best = 0; var distance = float.PositiveInfinity;
        for (var column = 0; column <= _count; column++)
        {
            if (!midGrapheme && !_grapheme[column]) continue;
            var caret = Carets(column);
            if (caret.Leading is { } l && MathF.Abs(l - position) < distance) { best = column; distance = MathF.Abs(l - position); }
            if (caret.Trailing is { } t && MathF.Abs(t - position) < distance) { best = column; distance = MathF.Abs(t - position); }

        }
        return best;
    }
    internal void SelectionRanges(int from, int to, List<Vector2> output)
    {
        output.Clear();
        for (var i = 0; i < _glyphs.Count;)
        {
            var glyph = _glyphs[i]; var end = i + 1; var advance = glyph.Advance * glyph.Repeat;
            while (end < _glyphs.Count && _glyphs[end].Start == glyph.Start && _glyphs[end].End == glyph.End) { advance += _glyphs[end].Advance * _glyphs[end].Repeat; end++; }
            if (!glyph.Virtual && glyph.End > glyph.Start && glyph.Start < to && glyph.End > from)
            {
                var a = Math.Max(from, glyph.Start); var b = Math.Min(to, glyph.End); var rtl = (_levels[glyph.Start] & 1) != 0; var pen = glyph.Position.X - glyph.Offset.X;
                var lo = pen + advance * (rtl ? glyph.End - b : a - glyph.Start) / (glyph.End - glyph.Start); var hi = pen + advance * (rtl ? glyph.End - a : b - glyph.Start) / (glyph.End - glyph.Start);
                if (output.Count > 0 && Mathf.IsEqualApprox(output[^1].Y, lo)) output[^1] = new(output[^1].X, hi); else output.Add(new(lo, hi));
            }
            i = end;
        }
    }
}
