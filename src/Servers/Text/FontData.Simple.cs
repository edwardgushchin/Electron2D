namespace Electron2D;

internal sealed partial class FontData
{
    private bool _simpleShaping;
    private NativeShapedGlyph[] _simpleGlyphs = [];
    private ReadOnlySpan<NativeShapedGlyph> ShapeSimple()
    {
        if (_simpleGlyphs.Length < _count) Array.Resize(ref _simpleGlyphs, Math.Max(_count, Math.Max(16, _simpleGlyphs.Length * 2)));
        var horizontal = _direction is NativeTextDirection.LTR or NativeTextDirection.RTL;
        var cache = SelectAuthored(_size);
        for (var i = 0; i < _count; i++)
        {
            var at = _offset + i; var glyph = _precision!.GetGlyphIndex(_scalars![at]);
            var advance = _precision.GetGlyphAdvance(glyph, !horizontal);
            var x = horizontal ? 0 : checked(-(int)Math.Round(_precision.GetGlyphAdvance(glyph) / 128d) * 64);
            var y = horizontal ? 0 : checked(-(int)Math.Round(_precision.Ascent * 64d));
            _simpleGlyphs[i] = new(glyph, (uint)at, horizontal ? advance : 0, horizontal ? 0 : advance, x, y);
            if (i == 0 || cache?.Kerning.ContainsKey(new((int)_simpleGlyphs[i - 1].GlyphIndex, (int)glyph)) == true) continue;
            var previous = _simpleGlyphs[i - 1]; var kerning = _precision.GetKerning(previous.GlyphIndex, glyph);
            var delta = checked((int)Math.Round((horizontal ? kerning.X : -kerning.Y) * 64d));
            _simpleGlyphs[i - 1] = horizontal ? previous with { XAdvance = previous.XAdvance + delta } : previous with { YAdvance = previous.YAdvance + delta };
        }
        return _simpleGlyphs.AsSpan(0, _count);
    }
}
