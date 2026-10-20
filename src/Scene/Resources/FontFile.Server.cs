using System.Text;

namespace Electron2D;

public partial class FontFile
{
    internal void ServerSetData(byte[] value)
    {
        ArgumentNullException.ThrowIfNull(value); if (value.Length > MaximumFontBytes) throw new InvalidDataException("Font data exceeds sixty-four MiB.");
        var bytes = (byte[])value.Clone(); FontData previous;
        lock (FontGate)
        {
            var original = CacheLocked(0);
            var cache = new FontCache
            {
                FixedSize = _fixedSize,
                ScaleMode = _fixedScaleMode,
                Instance = original.Instance with { Coordinates = new(original.Instance.Coordinates), CustomColors = (Color[])original.Instance.CustomColors.Clone() }
            };
            FontData replacement;
            try { replacement = new FontData(bytes, cache.Instance.FaceIndex, cache.Instance, cache); }
            catch (InvalidOperationException error) { throw new InvalidDataException("The font source or selected instance is invalid.", error); }
            try { CopyConfiguration(_fontData!, replacement); }
            catch { replacement.Dispose(); throw; }
            previous = _fontData!; ClearAuthoredLocked(); _fontData = replacement; _bytes = bytes; _type1Metrics = []; _deferred = false; _caches.Add(cache); InvalidateFontStateLocked();
        }
        FinishReplacement(previous);
    }
    internal int ServerPalette { get => Query(0, static (cache, _) => cache.Instance.PaletteIndex); set => Edit(0, cache => { cache.Instance = cache.Instance with { PaletteIndex = value }; if (_bytes.Length > 0) cache.Sizes.Clear(); }); }
    internal Color[] ServerCustomColors
    {
        get => Query(0, static (cache, _) => (Color[])cache.Instance.CustomColors.Clone());
        set
        {
            ArgumentNullException.ThrowIfNull(value); if (value.Length > 65536) throw new ArgumentOutOfRangeException(nameof(value)); var copy = (Color[])value.Clone();
            if (copy.Any(static color => !color.IsFinite())) throw new ArgumentException("Palette colors must be finite.", nameof(value));
            Edit(0, cache => { cache.Instance = cache.Instance with { CustomColors = copy }; if (_bytes.Length > 0) cache.Sizes.Clear(); });
        }
    }
    internal int[] ServerSupportedGlyphs() => Query(0, (cache, data) =>
    {
        if (_bytes.Length == 0) return cache.Sizes.Count == 0 ? [] : cache.Sizes.First().Value.Glyphs.Keys.Select(static glyph => checked((int)glyph)).ToArray();
        return data.GetSupportedChars().EnumerateRunes().Select(rune => checked((int)data.GetGlyphIndex((uint)rune.Value))).ToArray();
    });
    internal FontGlyphContours ServerContours(int size, int index)
    { SizeKey(size); var glyph = GlyphKey(index); return Query(0, (_, data) => data.GetGlyphContours(glyph, size)); }
    internal Vector2 ServerGlyphAdvance(int size, int index)
    {
        SizeKey(size); var glyph = GlyphKey(index);
        return Query(0, (cache, data) =>
        {
            var advance = new Vector2(data.GetGlyphAdvance(glyph, size), data.GetGlyphAdvance(glyph, size, true));
            var pixelSize = cache.FixedSize > 0 ? cache.FixedSize : size;
            if (!data.MultichannelSignedDistanceField && !(cache.FixedSize > 0 && cache.ScaleMode != FixedSizeScaleMode.Disable) &&
                (data.SubpixelPositioning == FontSubpixelPositioning.Disabled || data.SubpixelPositioning == FontSubpixelPositioning.Auto && pixelSize > (int)FontSubpixelPositioning.HalfMaxSize))
                advance = new(MathF.Round(advance.X, MidpointRounding.AwayFromZero), MathF.Round(advance.Y, MidpointRounding.AwayFromZero));
            return advance;
        });
    }
    internal RID ServerGlyphTexture(Vector2i size, int index) => ServerGlyph(size, index).Texture?.GetRID() ?? default;
    internal Vector2 ServerGlyphTextureSize(Vector2i size, int index) => ServerGlyph(size, index).Texture?.GetSize() ?? default;
    internal int ServerGlyphTextureIndex(Vector2i size, int index)
    {
        SizeKey(size); var glyph = GlyphKey(index);
        return Query(0, (cache, data) =>
        {
            var image = data.GetGlyph(glyph, size.X, size.Y, 0); if (image.Texture is null) return -1;
            return CacheSize(cache, size).Pages.FindIndex(page => ReferenceEquals(page?.RealizedTexture, image.Texture));
        });
    }
    internal Rect2 ServerGlyphUVRect(Vector2i size, int index)
    {
        var image = ServerGlyph(size, index);
        return image.Region ?? (image.Texture is { } texture ? new(Vector2.Zero, texture.GetSize()) : default);
    }
    internal Vector2 ServerGlyphOffset(Vector2i size, int index) => ServerGlyphGeometry(size, index).Offset;
    internal Vector2 ServerGlyphSize(Vector2i size, int index) => ServerGlyphGeometry(size, index).Size;
    private (Vector2 Offset, Vector2 Size) ServerGlyphGeometry(Vector2i size, int index)
    {
        SizeKey(size); var glyph = GlyphKey(index);
        return Query(0, (cache, data) =>
        {
            if (data.MultichannelSignedDistanceField) glyph &= 0xFFFFFF;
            data.GetGlyph(glyph, size.X, size.Y, 0);
            var record = CacheSize(cache, size).Glyphs.GetValueOrDefault(glyph);
            var factor = data.MultichannelSignedDistanceField ? data.MSDFScale(size.X) : cache.Factor(size.X);
            return (record.Offset * factor, record.Size * factor);
        });
    }
    private FontGlyph ServerGlyph(Vector2i size, int index)
    { SizeKey(size); var glyph = GlyphKey(index); return Query(0, (_, data) => data.GetGlyph(glyph, size.X, size.Y, 0)); }
    internal FontSizeCacheInfo[] ServerCacheInfo() => Query(0, (cache, data) => cache.Sizes.Select(pair =>
        new FontSizeCacheInfo(pair.Key, pair.Value.Glyphs.Count, pair.Value.Pages.Count,
            pair.Value.Pages.Sum(static page => page is null ? 0L : page.Pixels.LongLength * 2), data.RasterScales(pair.Key))).ToArray());
    internal void ServerDrawGlyph(CanvasItem canvas, int size, int outline, Vector2 position, int index, Color color, float oversampling, bool outlinePass = false)
    {
        SizeKey(size); var glyph = GlyphKey(index); if (outline < 0 || outline > 16384) throw new ArgumentOutOfRangeException(nameof(outline));
        ValidateDraw(canvas, position, color, oversampling);
        for (var attempt = 0; attempt < MaximumReadAttempts; attempt++)
        {
            using var read = BeginRead(); var sources = GetSources(); var data = sources.Count == 0 ? null : sources[0];
            if (data is null) { if (read.IsCurrent) return; continue; }
            var image = data.GetGlyph(glyph, size, outline, oversampling, position, out var rasterPosition, canvas.CanvasViewport?.FontRasterScale ?? 1);
            if (!read.IsCurrent) continue;
            TextLayout.DrawGlyph(canvas, image, rasterPosition, color, !outlinePass && data.ModulateColorGlyphs); return;
        }
        throw new InvalidOperationException("Font sources changed continuously while recording a glyph.");
    }
}
