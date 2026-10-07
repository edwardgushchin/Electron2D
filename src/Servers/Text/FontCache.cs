namespace Electron2D;

// A cache entry is a source/instance configuration with independently mutable size records.
// Published records are copied before authored edits; native realization only fills prepared caches.
internal sealed class FontCache
{
    internal FontInstance Instance = new(0, [], 0, Transform.Identity, 0, 0, []);
    internal int[] Spacing = new int[(int)TextSpacingType.Max];
    internal int FixedSize;
    internal FixedSizeScaleMode ScaleMode;
    internal float Factor(float size) => FixedSize <= 0 || ScaleMode == FixedSizeScaleMode.Disable ? 1 : ScaleMode == FixedSizeScaleMode.Enabled ? size / FixedSize : MathF.Round(size / FixedSize, MidpointRounding.AwayFromZero);
    internal readonly Dictionary<Vector2i, FontCacheSize> Sizes = [];
    internal bool Matches(FontInstance value)
    {
        if (Instance.FaceIndex != value.FaceIndex || Instance.Embolden != value.Embolden || Instance.Transform != value.Transform || Instance.BaselineOffset != value.BaselineOffset || Instance.PaletteIndex != value.PaletteIndex || !Instance.CustomColors.AsSpan().SequenceEqual(value.CustomColors) || Instance.Coordinates.Count != value.Coordinates.Count) return false;
        foreach (var pair in Instance.Coordinates) if (!value.Coordinates.TryGetValue(pair.Key, out var coordinate) || coordinate != pair.Value) return false;
        return true;
    }
    internal FontCache Clone()
    {
        var copy = new FontCache { Instance = Instance with { Coordinates = new(Instance.Coordinates), CustomColors = (Color[])Instance.CustomColors.Clone() }, Spacing = (int[])Spacing.Clone(), FixedSize = FixedSize, ScaleMode = ScaleMode };
        foreach (var pair in Sizes) copy.Sizes.Add(pair.Key, pair.Value.Clone());
        return copy;
    }
    internal FontCacheSize Size(Vector2i key)
    {
        if (!Sizes.TryGetValue(key, out var size)) Sizes.Add(key, size = new());
        return size;
    }
    internal FontCacheSize? Select(int size, int outline = 0)
    {
        if (FixedSize > 0) return Sizes.GetValueOrDefault(new(FixedSize, outline));
        return Sizes.GetValueOrDefault(new(size, outline));
    }
    internal bool HasGlyph(uint scalar) { foreach (var pair in Sizes) if (pair.Key.Y == 0 && pair.Value.Glyphs.ContainsKey(scalar)) return true; return false; }
    internal Vector2 Advance(uint glyph, int size)
    {
        var record = Select(size);
        var factor = Factor(size);
        return record?.Advances.GetValueOrDefault(glyph) * factor ?? Vector2.Zero;
    }
    internal void RetireTextures()
    {
        foreach (var size in Sizes.Values) foreach (var page in size.Pages) page?.Retire();
    }
}
internal sealed class FontCacheSize
{
    internal float[] Metrics = [float.NaN, float.NaN, float.NaN, float.NaN];
    internal float Scale = 1;
    internal readonly Dictionary<uint, Vector2> Advances = [];
    internal readonly Dictionary<uint, FontCacheGlyph> Glyphs = [];
    internal readonly Dictionary<Vector2i, Vector2> Kerning = [];
    internal readonly List<FontCachePage?> Pages = [];
    internal FontCacheSize Clone()
    {
        var copy = new FontCacheSize { Metrics = (float[])Metrics.Clone(), Scale = Scale };
        foreach (var p in Advances) copy.Advances.Add(p.Key, p.Value);
        foreach (var p in Glyphs) copy.Glyphs.Add(p.Key, p.Value);
        foreach (var p in Kerning) copy.Kerning.Add(p.Key, p.Value);
        foreach (var page in Pages) copy.Pages.Add(page?.Clone());
        return copy;
    }
}
internal readonly record struct FontCacheGlyph(Vector2 Offset, Vector2 Size, Rect2 Region, int Page = -1, bool Colored = false, bool Authored = true);
internal sealed class FontCachePage(int width, int height, byte[] rgba, int[] offsets)
{
    internal readonly int Width = width, Height = height;
    internal readonly byte[] Pixels = rgba;
    internal int[] Offsets = offsets;
    private ImageTexture? _texture;
    internal ImageTexture? RealizedTexture => _texture;
    internal FontCachePage Clone() => new(Width, Height, Pixels, (int[])Offsets.Clone());
    internal Image Image() => global::Electron2D.Image.CreateFromData(Width, Height, false, global::Electron2D.Image.Format.Rgba8, Pixels);
    internal ImageTexture Texture()
    {
        if (_texture != null) return _texture;
        using var image = Image(); _texture = ImageTexture.CreateFromImage(image); _texture.RetainRendererCache = true; return _texture;
    }
    internal void Retire() { if (_texture != null) _texture.RetainRendererCache = false; }
}
