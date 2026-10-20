namespace Electron2D;

internal sealed partial class FontData
{
    private readonly Dictionary<int, CanvasRasterScale.ScaleLevel> _rasterLevels = [];
    private HashSet<int>? _explicitRasterScales;

    private void TrackRasterScale(float factor, bool explicitFactor)
    {
        var key = (int)(factor * 64);
        if (explicitFactor) { (_explicitRasterScales ??= []).Add(key); return; }
        if (_explicitRasterScales?.Contains(key) == true) return;
        if (CanvasRasterScale.ActiveLevel(key) is not { } level ||
            _rasterLevels.TryGetValue(key, out var known) && ReferenceEquals(known, level)) return;
        if (CanvasRasterScale.TrackFont(key, level, this)) _rasterLevels[key] = level;
    }

    internal void RetireRasterScale(int key, CanvasRasterScale.ScaleLevel level)
    {
        lock (_gate)
        {
            if (_disposed || !_rasterLevels.TryGetValue(key, out var current) || !ReferenceEquals(current, level)) return;
            _rasterLevels.Remove(key);
            if (_explicitRasterScales?.Contains(key) == true) return;
            var factor = key / 64f;
            foreach (var glyph in _glyphs.Keys.Where(k => k.Oversampling == factor).ToArray())
            {
                if (!_glyphs.Remove(glyph, out var image)) continue;
                var authorOwnsTexture = false;
                if (Authored?.Sizes.TryGetValue(new(glyph.Size, glyph.Outline), out var cache) == true)
                {
                    authorOwnsTexture = cache.Pages.Any(page => page is { Authored: true } && ReferenceEquals(page.RealizedTexture, image.Texture));
                    for (var phase = 0; phase < 4; phase++)
                    {
                        var index = glyph.Glyph | ((uint)phase << 27);
                        if (cache.Glyphs.TryGetValue(index, out var record) && !record.Authored && record.Region == image.Region &&
                            record.Page >= 0 && record.Page < cache.Pages.Count && ReferenceEquals(cache.Pages[record.Page]?.RealizedTexture, image.Texture))
                            cache.Glyphs.Remove(index);
                    }
                }
                if (!authorOwnsTexture && image.Texture is { } texture) texture.RetainRendererCache = false;
            }
            if (Authored is not null)
                foreach (var size in Authored.Sizes.Values)
                    for (var i = 0; i < size.Pages.Count; i++)
                        if (size.Pages[i] is { Authored: false } page && page.RasterScale == factor)
                        {
                            page.Retire(); size.Pages[i] = null;
                            foreach (var glyph in size.Glyphs.Where(g => !g.Value.Authored && g.Value.Page == i).Select(g => g.Key).ToArray())
                                size.Glyphs.Remove(glyph);
                        }
            _glyphResult = default;
        }
    }
}
