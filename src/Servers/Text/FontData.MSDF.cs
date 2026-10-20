namespace Electron2D;

internal sealed partial class FontData
{
    internal Vector2i CacheSize(Vector2i requested)
    {
        if (!MultichannelSignedDistanceField) return Authored is { FixedSize: > 0 } cache ? new(cache.FixedSize, requested.Y) : requested;
        CheckMSDFConfiguration(); return new(MSDFSize, 0);
    }
    internal float MSDFScale(int size) => MultichannelSignedDistanceField ? size / (float)MSDFSize : 1;
    private void CheckMSDFConfiguration()
    {
        if (MSDFSize <= 0 || MSDFSize > 16384 || MSDFPixelRange <= 0 || MSDFPixelRange > 16384)
            throw new InvalidOperationException("MSDF generation requires source size and pixel range between 1 and 16384.");
    }
    private FontGlyph GetMSDFGlyph(uint glyph, int size, int outline)
    {
        CheckMSDFConfiguration();
        if (TryAuthoredGlyph(glyph, size, outline, out var authored)) return authored;
        if (_bytes.Length == 0) return default;
        var key = (glyph, MSDFSize, 0, 1f, Hinting, 0);
        if (!_glyphs.TryGetValue(key, out var source))
        {
            _glyph = _cacheGlyph = glyph; _size = MSDFSize; _outline = 0; _oversampling = 1; _phase = 0; Run(Operation.Glyph);
            source = _glyphResult; _glyphs.Add(key, source);
        }
        var scale = size / (float)MSDFSize;
        return source with { Offset = source.Offset * scale, Size = source.Size * scale, MSDFScale = scale, MSDFOutline = Math.Max(0, outline) };
    }
}
