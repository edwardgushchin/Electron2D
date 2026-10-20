namespace Electron2D;

internal sealed partial class FontData
{
    private FontGlyphContours _contourResult;
    internal int FaceIndex => _faceIndex;
    private uint _cacheGlyph;
    private void PublishRasterRecord(uint key, int size, int outline, FontGlyph image)
    {
        if (Authored is null || image.Texture is null || image.Region is not { } region) return;
        var record = Authored.Size(new(size, outline));
        if (record.Glyphs.TryGetValue(key, out var current) && current.Region == region && current.Offset == image.Offset && current.Size == image.Size) return;
        for (var i = 0; i < record.Pages.Count; i++) if (ReferenceEquals(record.Pages[i]?.RealizedTexture, image.Texture))
            { record.Glyphs[key] = new(image.Offset, image.Size, region, i, image.Colored, false); return; }
    }
    internal FontGlyphContours GetGlyphContours(uint glyph, int size)
    {
        ValidateSize(size);
        lock (_gate)
        {
            Check(); if (!HasData) return new([], [], false);
            _glyph = glyph; _size = size; Run(Operation.Contours); return _contourResult;
        }
    }
    internal float[] RasterScales(Vector2i size) => _glyphs.Keys.Where(key => key.Size == size.X && key.Outline == size.Y).Select(static key => key.Oversampling).Distinct().Order().ToArray();
    internal void ClearSystemFallbackCache() { lock (_gate) { if (!_disposed) ClearSystemSources(); } }
}
