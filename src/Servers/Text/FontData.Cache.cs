namespace Electron2D;

internal sealed partial class FontData
{
    private FontCacheSize? SelectAuthored(int size, int outline = 0) => _bytes.Length > 0 ? Authored?.Sizes.GetValueOrDefault(new(size, outline)) : Authored?.Select(size, outline);
    internal Vector2 GetKerning(Vector2i pair, int size)
    {
        ValidateSize(size);
        lock (_gate)
        {
            Check(); if (SelectAuthored(size)?.Kerning.TryGetValue(pair, out var value) == true) return value * BitmapScale(size);
            if (!HasData) return Vector2.Zero; var key = (pair, size); if (_kernings.TryGetValue(key, out var cached)) return cached;
            _pair = pair; _size = size; Run(Operation.Kerning); _kernings.Add(key, _kerningResult); return _kerningResult;
        }
    }
    internal FontCache? Authored { get; set; }
    private float BitmapScale(int size) => _bytes.Length == 0 && Authored is { FixedSize: > 0 } cache ? cache.Factor(size) : 1;
    private FontMetrics AuthoredMetrics(int size, FontMetrics metrics)
    {
        if (SelectAuthored(size) is not { } cache) return metrics;
        var factor = BitmapScale(size);
        var ascent = float.IsNaN(cache.Metrics[0]) ? metrics.Ascent : cache.Metrics[0] * factor;
        var descent = float.IsNaN(cache.Metrics[1]) ? metrics.Descent : cache.Metrics[1] * factor;
        return new(ascent, descent, ascent + descent,
            float.IsNaN(cache.Metrics[2]) ? metrics.UnderlinePosition : cache.Metrics[2] * factor,
            float.IsNaN(cache.Metrics[3]) ? metrics.UnderlineThickness : cache.Metrics[3] * factor);
    }
    private bool TryAuthoredGlyph(uint glyph, int size, int outline, out FontGlyph result)
    {
        result = default;
        if (SelectAuthored(size, outline) is not { } cache || !cache.Glyphs.TryGetValue(glyph, out var record) || !record.Authored) return false;
        var page = record.Page >= 0 && record.Page < cache.Pages.Count ? cache.Pages[record.Page] : null;
        var factor = BitmapScale(size) * cache.Scale;
        result = new(page?.Texture(), record.Offset * factor, record.Size * factor, record.Colored, record.Region); return true;
    }
    private FontGlyph CacheRaster(uint glyph, Vector2i key, NativeRasterGlyph raster)
    {
        var size = Authored!.Size(key); var index = -1; Vector2i position = default;
        var width = raster.Width + 2; var height = raster.Height + 2;
        for (var i = 0; i < size.Pages.Count; i++)
            if (size.Pages[i] is { } page && TryPack(page, width, height, out position)) { index = i; break; }
        if (index < 0)
        {
            var dimension = Math.Max(256, Math.Max(width, height));
            if (dimension > 16384 || (long)dimension * dimension * 4 > 64 * 1024 * 1024) throw new InvalidOperationException("Font atlas page exceeds its pixel budget.");
            index = size.Pages.Count; var page = new FontCachePage(dimension, dimension, new byte[dimension * dimension * 4], []);
            size.Pages.Add(page); if (!TryPack(page, width, height, out position)) throw new InvalidOperationException("Font atlas packing failed.");
        }
        var previous = size.Pages[index]!; var pixels = (byte[])previous.Pixels.Clone();
        // Extrude each edge to preserve standalone clamp sampling when the canvas scales a glyph.
        for (var y = -1; y <= raster.Height; y++)
        {
            var source = raster.Pixels.AsSpan(Math.Clamp(y, 0, raster.Height - 1) * raster.Width * 4, raster.Width * 4);
            var destination = pixels.AsSpan(((position.Y + 1 + y) * previous.Width + position.X) * 4, width * 4);
            source.CopyTo(destination[4..]); source[..4].CopyTo(destination[..4]); source[^4..].CopyTo(destination[^4..]);
        }
        var replacement = new FontCachePage(previous.Width, previous.Height, pixels, (int[])previous.Offsets.Clone());
        var texture = replacement.Texture();
        if (previous.RealizedTexture is { } previousTexture)
            foreach (var cachedKey in _glyphs.Keys)
                if (ReferenceEquals(_glyphs[cachedKey].Texture, previousTexture)) _glyphs[cachedKey] = _glyphs[cachedKey] with { Texture = texture };
        previous.Retire(); size.Pages[index] = replacement;
        var offset = new Vector2(raster.Left, -raster.Top); var dimensions = new Vector2(raster.Width, raster.Height);
        var region = new Rect2(position.X + 1, position.Y + 1, raster.Width, raster.Height);
        size.Glyphs[glyph] = new(offset, dimensions, region, index, raster.Colored, false);
        return new(texture, offset, dimensions, raster.Colored, region);
    }
    private static bool TryPack(FontCachePage page, int width, int height, out Vector2i position)
    {
        var shelves = page.Offsets; var bottom = 0;
        for (var i = 0; i < shelves.Length; i += 4)
        {
            bottom = Math.Max(bottom, shelves[i + 1] + shelves[i + 3]);
            if (width > shelves[i + 2] || height > shelves[i + 3]) continue;
            position = new(shelves[i], shelves[i + 1]); shelves[i] += width; shelves[i + 2] -= width; return true;
        }
        if (width <= page.Width && height <= page.Height - bottom)
        {
            Array.Resize(ref shelves, shelves.Length + 4); var i = shelves.Length - 4;
            shelves[i] = width; shelves[i + 1] = bottom; shelves[i + 2] = page.Width - width; shelves[i + 3] = height; page.Offsets = shelves;
            position = new(0, bottom); return true;
        }
        position = default; return false;
    }
    internal int CharacterFromGlyph(uint glyph)
    {
        foreach (var rune in GetSupportedChars().EnumerateRunes()) if (GetGlyphIndex((uint)rune.Value) == glyph) return rune.Value;
        return 0;
    }
}
