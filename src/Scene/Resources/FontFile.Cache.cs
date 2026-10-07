namespace Electron2D;

public partial class FontFile
{
    private readonly List<FontCache> _caches = [];
    private readonly Dictionary<int, FontData> _cacheData = [];
    internal FontData CreateVariationData(FontInstance instance, ReadOnlySpan<int> spacing)
    {
        lock (FontGate)
        {
            ThrowIfDisposed(); EnsureLoadedLocked(); EnsurePrimaryCacheLocked();
            for (var i = 0; i < _caches.Count; i++) if (_caches[i].Matches(instance) && _caches[i].Spacing.AsSpan().SequenceEqual(spacing)) return DataLocked(i).CreateVariation(instance);
            return _fontData!.CreateVariation(instance);
        }
    }
    private void EnsurePrimaryCacheLocked()
    {
        if (_caches.Count == 0) _caches.Add(new() { FixedSize = _fixedSize, ScaleMode = _fixedScaleMode });
        _fontData!.Authored ??= _caches[0];
    }
    private FontCache CacheLocked(int index)
    {
        ThrowIfDisposed(); EnsureLoadedLocked();
        if ((uint)index >= 4096) throw new ArgumentOutOfRangeException(nameof(index), "Font cache indices must be between zero and 4095.");
        EnsurePrimaryCacheLocked();
        while (_caches.Count <= index) _caches.Add(new() { FixedSize = _fixedSize, ScaleMode = _fixedScaleMode });
        return _caches[index];
    }
    private FontData DataLocked(int index)
    {
        var cache = CacheLocked(index);
        if (index == 0) return _fontData!;
        if (!_cacheData.TryGetValue(index, out var data)) _cacheData.Add(index, data = BuildCacheData(cache));
        return data;
    }
    private FontData BuildCacheData(FontCache cache)
    {
        var data = new FontData(_bytes, cache.Instance.FaceIndex, cache.Instance, cache);
        try { CopyConfiguration(_fontData!, data); data.FamilyName = _fontData!.FamilyName; data.StyleName = _fontData.StyleName; data.FontStyle = _fontData.FontStyle; data.FontWeight = _fontData.FontWeight; data.FontStretch = _fontData.FontStretch; return data; } catch { data.Dispose(); throw; }
    }
    private T Query<T>(int index, Func<FontCache, FontData, T> query)
    {
        lock (FontGate) { var cache = CacheLocked(index); var data = DataLocked(index); lock (data.Gate) return query(cache, data); }
    }
    private void Edit(int index, Action<FontCache> edit)
    {
        FontData previous;
        lock (FontGate)
        {
            var count = _caches.Count;
            try
            {
                var original = CacheLocked(index); var old = DataLocked(index); FontCache copy;
                lock (old.Gate) copy = original.Clone();
                edit(copy); var replacement = BuildCacheData(copy);
                _caches[index] = copy; previous = old;
                if (index == 0) _fontData = replacement; else _cacheData[index] = replacement;
                InvalidateFontStateLocked();
            }
            catch
            {
                for (var slot = count; slot < _caches.Count; slot++) if (_cacheData.Remove(slot, out var discarded)) discarded.Dispose();
                if (_caches.Count > count) _caches.RemoveRange(count, _caches.Count - count); throw;
            }
        }
        FinishReplacement(previous);
    }
    private void ClearAuthoredLocked()
    {
        foreach (var data in _cacheData.Values) data.Dispose(); _cacheData.Clear(); _caches.Clear();
    }
    private static Vector2i SizeKey(Vector2i size)
    {
        if (size.X is <= 0 or > 16384 || size.Y is < 0 or > 16384) throw new ArgumentOutOfRangeException(nameof(size)); return size;
    }
    private static int SizeKey(int size) => SizeKey(new Vector2i(size, 0)).X;
    private static uint GlyphKey(int glyph) { ArgumentOutOfRangeException.ThrowIfNegative(glyph); return (uint)glyph; }
    private static Vector2i PairKey(Vector2i pair) { GlyphKey(pair.X); GlyphKey(pair.Y); return pair; }
    private static float Finite(float value) { if (!float.IsFinite(value)) throw new ArgumentOutOfRangeException(nameof(value)); return value; }
    private static Vector2 Finite(Vector2 value) { if (!value.IsFinite()) throw new ArgumentException("Glyph geometry must be finite.", nameof(value)); return value; }
    private static int PageKey(int index) { if ((uint)index >= 65536) throw new ArgumentOutOfRangeException(nameof(index)); return index; }
    private static FontCachePage? Page(FontCacheSize size, int index) => index < size.Pages.Count ? size.Pages[index] : null;
    private static void PutPage(FontCacheSize size, int index, FontCachePage? page) { while (size.Pages.Count <= index) size.Pages.Add(null); size.Pages[index] = page; }
    private static void PutGlyph(FontCache cache, Vector2i size, uint glyph, Func<FontCacheGlyph, FontCacheGlyph> edit)
    {
        var record = cache.Size(size); var value = record.Glyphs.GetValueOrDefault(glyph, new(default, default, default, -1)); record.Glyphs[glyph] = edit(value);
    }
    /// <summary>Returns number of the font cache entries.</summary>
    /// <returns>A typed value or independent snapshot of the current cache state.</returns>
    /// <remarks>Authored edits invalidate text and emit Changed after committing. Image and collection queries return independent copies; invalid geometry or indices fail before publication.</remarks>
    /// <exception cref="ObjectDisposedException">The font is disposed.</exception>
    public int GetCacheCount() { lock (FontGate) { ThrowIfDisposed(); return _caches.Count; } }
    /// <summary>Removes all font cache entries.</summary>
    /// <remarks>Authored edits invalidate text and emit Changed after committing. Image and collection queries return independent copies; invalid geometry or indices fail before publication.</remarks>
    /// <exception cref="ObjectDisposedException">The font is disposed.</exception>
    public void ClearCache() { ResetCaches(null); }
    /// <summary>Removes specified font cache entry.</summary>
    /// <param name="cacheIndex">Nonnegative cache index; absent entries are created, up to 4095.</param>
    /// <remarks>Authored edits invalidate text and emit Changed after committing. Image and collection queries return independent copies; invalid geometry or indices fail before publication.</remarks>
    /// <exception cref="ObjectDisposedException">The font is disposed.</exception>
    public void RemoveCache(int cacheIndex) { ResetCaches(cacheIndex); }
    /// <summary>Returns list of the font sizes in the cache. Each size is [Vector2i] with font size and outline size.</summary>
    /// <param name="cacheIndex">Nonnegative cache index; absent entries are created, up to 4095.</param>
    /// <returns>A typed value or independent snapshot of the current cache state.</returns>
    /// <remarks>Authored edits invalidate text and emit Changed after committing. Image and collection queries return independent copies; invalid geometry or indices fail before publication.</remarks>
    /// <exception cref="ObjectDisposedException">The font is disposed.</exception>
    public Vector2i[] GetSizeCacheList(int cacheIndex) => Query(cacheIndex, static (cache, _) => cache.Sizes.Keys.ToArray());
    /// <summary>Removes all font sizes from the cache entry.</summary>
    /// <param name="cacheIndex">Nonnegative cache index; absent entries are created, up to 4095.</param>
    /// <remarks>Authored edits invalidate text and emit Changed after committing. Image and collection queries return independent copies; invalid geometry or indices fail before publication.</remarks>
    /// <exception cref="ObjectDisposedException">The font is disposed.</exception>
    public void ClearSizeCache(int cacheIndex) => Edit(cacheIndex, static cache => cache.Sizes.Clear());
    /// <summary>Removes specified font size from the cache entry.</summary>
    /// <param name="cacheIndex">Nonnegative cache index; absent entries are created, up to 4095.</param>
    /// <param name="size">Positive pixel size, or pixel size and nonnegative outline thickness.</param>
    /// <remarks>Authored edits invalidate text and emit Changed after committing. Image and collection queries return independent copies; invalid geometry or indices fail before publication.</remarks>
    /// <exception cref="ObjectDisposedException">The font is disposed.</exception>
    public void RemoveSizeCache(int cacheIndex, Vector2i size) { SizeKey(size); Edit(cacheIndex, cache => cache.Sizes.Remove(size)); }
    /// <summary>Removes all rendered glyph information from the cache entry.</summary>
    /// <param name="cacheIndex">Nonnegative cache index; absent entries are created, up to 4095.</param>
    /// <param name="size">Positive pixel size, or pixel size and nonnegative outline thickness.</param>
    /// <remarks>Authored edits invalidate text and emit Changed after committing. Image and collection queries return independent copies; invalid geometry or indices fail before publication.</remarks>
    /// <exception cref="ObjectDisposedException">The font is disposed.</exception>
    public void ClearGlyphs(int cacheIndex, Vector2i size) { SizeKey(size); Edit(cacheIndex, cache => { cache.Size(size).Glyphs.Clear(); if (size.Y == 0) cache.Size(size).Advances.Clear(); }); }
    /// <summary>Removes specified rendered glyph information from the cache entry.</summary>
    /// <param name="cacheIndex">Nonnegative cache index; absent entries are created, up to 4095.</param>
    /// <param name="size">Positive pixel size, or pixel size and nonnegative outline thickness.</param>
    /// <param name="glyph">Nonnegative source glyph index.</param>
    /// <remarks>Authored edits invalidate text and emit Changed after committing. Image and collection queries return independent copies; invalid geometry or indices fail before publication.</remarks>
    /// <exception cref="ObjectDisposedException">The font is disposed.</exception>
    public void RemoveGlyph(int cacheIndex, Vector2i size, int glyph) { SizeKey(size); var key = GlyphKey(glyph); Edit(cacheIndex, cache => { cache.Size(size).Glyphs.Remove(key); if (size.Y == 0) cache.Size(size).Advances.Remove(key); }); }
    /// <summary>Returns list of rendered glyphs in the cache entry.</summary>
    /// <param name="cacheIndex">Nonnegative cache index; absent entries are created, up to 4095.</param>
    /// <param name="size">Positive pixel size, or pixel size and nonnegative outline thickness.</param>
    /// <returns>A typed value or independent snapshot of the current cache state.</returns>
    /// <remarks>Authored edits invalidate text and emit Changed after committing. Image and collection queries return independent copies; invalid geometry or indices fail before publication.</remarks>
    /// <exception cref="ObjectDisposedException">The font is disposed.</exception>
    public int[] GetGlyphList(int cacheIndex, Vector2i size) { SizeKey(size); return Query(cacheIndex, (cache, _) => cache.Size(size).Glyphs.Keys.Select(glyph => checked((int)glyph)).ToArray()); }
    /// <summary>Returns the glyph index of a the character, optionally modified by the the variation selector.</summary>
    /// <param name="size">Positive pixel size, or pixel size and nonnegative outline thickness.</param>
    /// <param name="character">Valid Unicode scalar.</param>
    /// <param name="variationSelector">Zero or a valid Unicode variation selector.</param>
    /// <returns>A typed value or independent snapshot of the current cache state.</returns>
    /// <remarks>Authored edits invalidate text and emit Changed after committing. Image and collection queries return independent copies; invalid geometry or indices fail before publication.</remarks>
    /// <exception cref="ObjectDisposedException">The font is disposed.</exception>
    public int GetGlyphIndex(int size, int character, int variationSelector) { SizeKey(size); return Query(0, (_, data) => checked((int)data.GetGlyphIndex((uint)character, (uint)variationSelector))); }
    /// <summary>Returns character code associated with the glyph index, or [code]0[/code] if the glyph index is invalid.</summary>
    /// <param name="size">Positive pixel size, or pixel size and nonnegative outline thickness.</param>
    /// <param name="glyph">Nonnegative source glyph index.</param>
    /// <returns>A typed value or independent snapshot of the current cache state.</returns>
    /// <remarks>Authored edits invalidate text and emit Changed after committing. Image and collection queries return independent copies; invalid geometry or indices fail before publication.</remarks>
    /// <exception cref="ObjectDisposedException">The font is disposed.</exception>
    public int GetCharFromGlyphIndex(int size, int glyph) { SizeKey(size); var key = GlyphKey(glyph); return Query(0, (_, data) => data.CharacterFromGlyph(key)); }
    /// <summary>Returns glyph advance (offset of the next glyph).</summary>
    /// <param name="cacheIndex">Nonnegative cache index; absent entries are created, up to 4095.</param>
    /// <param name="size">Positive pixel size, or pixel size and nonnegative outline thickness.</param>
    /// <param name="glyph">Nonnegative source glyph index.</param>
    /// <returns>A typed value or independent snapshot of the current cache state.</returns>
    /// <remarks>Authored edits invalidate text and emit Changed after committing. Image and collection queries return independent copies; invalid geometry or indices fail before publication.</remarks>
    /// <exception cref="ObjectDisposedException">The font is disposed.</exception>
    public Vector2 GetGlyphAdvance(int cacheIndex, int size, int glyph) { SizeKey(size); var key = GlyphKey(glyph); return Query(cacheIndex, (_, data) => new Vector2(data.GetGlyphAdvance(key, size), data.GetGlyphAdvance(key, size, true))); }
    /// <summary>Sets glyph advance (offset of the next glyph).</summary>
    /// <param name="cacheIndex">Nonnegative cache index; absent entries are created, up to 4095.</param>
    /// <param name="size">Positive pixel size, or pixel size and nonnegative outline thickness.</param>
    /// <param name="glyph">Nonnegative source glyph index.</param>
    /// <param name="advance">Finite horizontal and vertical advances in pixels.</param>
    /// <remarks>Authored edits invalidate text and emit Changed after committing. Image and collection queries return independent copies; invalid geometry or indices fail before publication.</remarks>
    /// <exception cref="ObjectDisposedException">The font is disposed.</exception>
    public void SetGlyphAdvance(int cacheIndex, int size, int glyph, Vector2 advance) { SizeKey(size); var key = GlyphKey(glyph); Finite(advance); Edit(cacheIndex, cache => cache.Size(new(size, 0)).Advances[key] = advance); }
    /// <summary>Renders specified glyph to the font cache texture.</summary>
    /// <param name="cacheIndex">Nonnegative cache index; absent entries are created, up to 4095.</param>
    /// <param name="size">Positive pixel size, or pixel size and nonnegative outline thickness.</param>
    /// <param name="glyph">Nonnegative source glyph index.</param>
    /// <remarks>Authored edits invalidate text and emit Changed after committing. Image and collection queries return independent copies; invalid geometry or indices fail before publication.</remarks>
    /// <exception cref="ObjectDisposedException">The font is disposed.</exception>
    public void RenderGlyph(int cacheIndex, Vector2i size, int glyph) { SizeKey(size); var key = GlyphKey(glyph); Query(cacheIndex, (_, data) => data.GetGlyph(key, size.X, size.Y, 1)); }
    /// <summary>Renders the range of characters to the font cache texture.</summary>
    /// <param name="cacheIndex">Nonnegative cache index; absent entries are created, up to 4095.</param>
    /// <param name="size">Positive pixel size, or pixel size and nonnegative outline thickness.</param>
    /// <param name="start">Typed start value.</param>
    /// <param name="end">Typed end value.</param>
    /// <remarks>Authored edits invalidate text and emit Changed after committing. Image and collection queries return independent copies; invalid geometry or indices fail before publication.</remarks>
    /// <exception cref="ObjectDisposedException">The font is disposed.</exception>
    public void RenderRange(int cacheIndex, Vector2i size, int start, int end) { SizeKey(size); if (!System.Text.Rune.IsValid(start) || !System.Text.Rune.IsValid(end) || end < start || (long)end - start > 65536) throw new ArgumentOutOfRangeException(nameof(end)); Query(cacheIndex, (_, data) => { for (var scalar = start; scalar <= end; scalar++) if (System.Text.Rune.IsValid(scalar)) { var glyph = data.GetGlyphIndex((uint)scalar); if (glyph != 0) data.GetGlyph(glyph, size.X, size.Y, 1); } return 0; }); }
    /// <summary>Returns number of textures used by font cache entry.</summary>
    /// <param name="cacheIndex">Nonnegative cache index; absent entries are created, up to 4095.</param>
    /// <param name="size">Positive pixel size, or pixel size and nonnegative outline thickness.</param>
    /// <returns>A typed value or independent snapshot of the current cache state.</returns>
    /// <remarks>Authored edits invalidate text and emit Changed after committing. Image and collection queries return independent copies; invalid geometry or indices fail before publication.</remarks>
    /// <exception cref="ObjectDisposedException">The font is disposed.</exception>
    public int GetTextureCount(int cacheIndex, Vector2i size) { SizeKey(size); return Query(cacheIndex, (cache, _) => cache.Size(size).Pages.Count); }
    /// <summary>Removes all textures from font cache entry.</summary>
    /// <param name="cacheIndex">Nonnegative cache index; absent entries are created, up to 4095.</param>
    /// <param name="size">Positive pixel size, or pixel size and nonnegative outline thickness.</param>
    /// <remarks>Authored edits invalidate text and emit Changed after committing. Image and collection queries return independent copies; invalid geometry or indices fail before publication.</remarks>
    /// <exception cref="ObjectDisposedException">The font is disposed.</exception>
    public void ClearTextures(int cacheIndex, Vector2i size) { SizeKey(size); Edit(cacheIndex, cache => cache.Size(size).Pages.Clear()); }
    /// <summary>Removes specified texture from the cache entry.</summary>
    /// <param name="cacheIndex">Nonnegative cache index; absent entries are created, up to 4095.</param>
    /// <param name="size">Positive pixel size, or pixel size and nonnegative outline thickness.</param>
    /// <param name="textureIndex">Texture slot between zero and 65535.</param>
    /// <remarks>Authored edits invalidate text and emit Changed after committing. Image and collection queries return independent copies; invalid geometry or indices fail before publication.</remarks>
    /// <exception cref="ObjectDisposedException">The font is disposed.</exception>
    public void RemoveTexture(int cacheIndex, Vector2i size, int textureIndex) { SizeKey(size); PageKey(textureIndex); Edit(cacheIndex, cache => { var pages = cache.Size(size).Pages; if (textureIndex >= pages.Count) throw new ArgumentOutOfRangeException(nameof(textureIndex)); pages.RemoveAt(textureIndex); }); }
    /// <summary>Returns a copy of the font cache texture image.</summary>
    /// <param name="cacheIndex">Nonnegative cache index; absent entries are created, up to 4095.</param>
    /// <param name="size">Positive pixel size, or pixel size and nonnegative outline thickness.</param>
    /// <param name="textureIndex">Texture slot between zero and 65535.</param>
    /// <returns>Independent caller-owned image copy, or null for an absent page.</returns>
    /// <remarks>Authored edits invalidate text and emit Changed after committing. Image and collection queries return independent copies; invalid geometry or indices fail before publication.</remarks>
    /// <exception cref="ObjectDisposedException">The font is disposed.</exception>
    public Image? GetTextureImage(int cacheIndex, Vector2i size, int textureIndex) { SizeKey(size); PageKey(textureIndex); return Query(cacheIndex, (cache, _) => Page(cache.Size(size), textureIndex)?.Image()); }
    /// <summary>Sets font cache texture image.</summary>
    /// <param name="cacheIndex">Nonnegative cache index; absent entries are created, up to 4095.</param>
    /// <param name="size">Positive pixel size, or pixel size and nonnegative outline thickness.</param>
    /// <param name="textureIndex">Texture slot between zero and 65535.</param>
    /// <param name="image">Image copied immediately; ownership remains with the caller.</param>
    /// <remarks>Authored edits invalidate text and emit Changed after committing. Image and collection queries return independent copies; invalid geometry or indices fail before publication.</remarks>
    /// <exception cref="ObjectDisposedException">The font is disposed.</exception>
    public void SetTextureImage(int cacheIndex, Vector2i size, int textureIndex, Image image) { SizeKey(size); PageKey(textureIndex); ArgumentNullException.ThrowIfNull(image); using var copy = (Image)image.Duplicate(); copy.Convert(Image.Format.Rgba8); var pixels = copy.GetData(); var width = copy.Width; var height = copy.Height; if (width <= 0 || width > 16384 || height <= 0 || height > 16384 || pixels.Length > MaximumFontBytes || copy.HasMipmaps) throw new ArgumentException("A cache page must have nonempty base-level pixels.", nameof(image)); Edit(cacheIndex, cache => { var record = cache.Size(size); PutPage(record, textureIndex, new(width, height, pixels, Page(record, textureIndex)?.Offsets ?? [])); foreach (var key in record.Glyphs.Keys) if (record.Glyphs[key].Page == textureIndex) record.Glyphs[key] = record.Glyphs[key] with { Authored = true }; }); }
    /// <summary>Returns a copy of the array containing glyph packing data.</summary>
    /// <param name="cacheIndex">Nonnegative cache index; absent entries are created, up to 4095.</param>
    /// <param name="size">Positive pixel size, or pixel size and nonnegative outline thickness.</param>
    /// <param name="textureIndex">Texture slot between zero and 65535.</param>
    /// <returns>A typed value or independent snapshot of the current cache state.</returns>
    /// <remarks>Authored edits invalidate text and emit Changed after committing. Image and collection queries return independent copies; invalid geometry or indices fail before publication.</remarks>
    /// <exception cref="ObjectDisposedException">The font is disposed.</exception>
    public int[] GetTextureOffsets(int cacheIndex, Vector2i size, int textureIndex) { SizeKey(size); PageKey(textureIndex); return Query(cacheIndex, (cache, _) => (int[])(Page(cache.Size(size), textureIndex)?.Offsets.Clone() ?? Array.Empty<int>())); }
    /// <summary>Sets array containing glyph packing data.</summary>
    /// <param name="cacheIndex">Nonnegative cache index; absent entries are created, up to 4095.</param>
    /// <param name="size">Positive pixel size, or pixel size and nonnegative outline thickness.</param>
    /// <param name="textureIndex">Texture slot between zero and 65535.</param>
    /// <param name="offsets">Typed offsets value.</param>
    /// <remarks>Authored edits invalidate text and emit Changed after committing. Image and collection queries return independent copies; invalid geometry or indices fail before publication.</remarks>
    /// <exception cref="ObjectDisposedException">The font is disposed.</exception>
    public void SetTextureOffsets(int cacheIndex, Vector2i size, int textureIndex, int[] offsets) { SizeKey(size); PageKey(textureIndex); ArgumentNullException.ThrowIfNull(offsets); var copy = (int[])offsets.Clone(); if (copy.Any(value => value < 0)) throw new ArgumentOutOfRangeException(nameof(offsets)); Edit(cacheIndex, cache => { var page = Page(cache.Size(size), textureIndex) ?? throw new ArgumentOutOfRangeException(nameof(textureIndex)); ValidateShelves(copy, page.Width, page.Height); page.Offsets = copy; }); }
    /// <summary>Returns glyph offset from the baseline.</summary>
    /// <param name="cacheIndex">Nonnegative cache index; absent entries are created, up to 4095.</param>
    /// <param name="size">Positive pixel size, or pixel size and nonnegative outline thickness.</param>
    /// <param name="glyph">Nonnegative source glyph index.</param>
    /// <returns>A typed value or independent snapshot of the current cache state.</returns>
    /// <remarks>Authored edits invalidate text and emit Changed after committing. Image and collection queries return independent copies; invalid geometry or indices fail before publication.</remarks>
    /// <exception cref="ObjectDisposedException">The font is disposed.</exception>
    public Vector2 GetGlyphOffset(int cacheIndex, Vector2i size, int glyph) { SizeKey(size); var key = GlyphKey(glyph); return Query(cacheIndex, (cache, data) => { data.GetGlyph(key, size.X, size.Y, 1); return cache.Size(size).Glyphs.GetValueOrDefault(key).Offset; }); }
    /// <summary>Sets glyph offset from the baseline.</summary>
    /// <param name="cacheIndex">Nonnegative cache index; absent entries are created, up to 4095.</param>
    /// <param name="size">Positive pixel size, or pixel size and nonnegative outline thickness.</param>
    /// <param name="glyph">Nonnegative source glyph index.</param>
    /// <param name="offset">Finite offset from the baseline in pixels.</param>
    /// <remarks>Authored edits invalidate text and emit Changed after committing. Image and collection queries return independent copies; invalid geometry or indices fail before publication.</remarks>
    /// <exception cref="ObjectDisposedException">The font is disposed.</exception>
    public void SetGlyphOffset(int cacheIndex, Vector2i size, int glyph, Vector2 offset) { SizeKey(size); var key = GlyphKey(glyph); Finite(offset); Edit(cacheIndex, cache => PutGlyph(cache, size, key, entry => entry with { Authored = true, Offset = offset })); }
    /// <summary>Returns glyph size.</summary>
    /// <param name="cacheIndex">Nonnegative cache index; absent entries are created, up to 4095.</param>
    /// <param name="size">Positive pixel size, or pixel size and nonnegative outline thickness.</param>
    /// <param name="glyph">Nonnegative source glyph index.</param>
    /// <returns>A typed value or independent snapshot of the current cache state.</returns>
    /// <remarks>Authored edits invalidate text and emit Changed after committing. Image and collection queries return independent copies; invalid geometry or indices fail before publication.</remarks>
    /// <exception cref="ObjectDisposedException">The font is disposed.</exception>
    public Vector2 GetGlyphSize(int cacheIndex, Vector2i size, int glyph) { SizeKey(size); var key = GlyphKey(glyph); return Query(cacheIndex, (cache, data) => { data.GetGlyph(key, size.X, size.Y, 1); return cache.Size(size).Glyphs.GetValueOrDefault(key).Size; }); }
    /// <summary>Sets glyph size.</summary>
    /// <param name="cacheIndex">Nonnegative cache index; absent entries are created, up to 4095.</param>
    /// <param name="size">Positive pixel size, or pixel size and nonnegative outline thickness.</param>
    /// <param name="glyph">Nonnegative source glyph index.</param>
    /// <param name="glyphSize">Finite nonnegative glyph dimensions in pixels.</param>
    /// <remarks>Authored edits invalidate text and emit Changed after committing. Image and collection queries return independent copies; invalid geometry or indices fail before publication.</remarks>
    /// <exception cref="ObjectDisposedException">The font is disposed.</exception>
    public void SetGlyphSize(int cacheIndex, Vector2i size, int glyph, Vector2 glyphSize) { SizeKey(size); var key = GlyphKey(glyph); Finite(glyphSize); if (glyphSize.X < 0 || glyphSize.Y < 0) throw new ArgumentOutOfRangeException(nameof(glyphSize)); Edit(cacheIndex, cache => PutGlyph(cache, size, key, entry => entry with { Authored = true, Size = glyphSize })); }
    /// <summary>Returns index of the cache texture containing the glyph.</summary>
    /// <param name="cacheIndex">Nonnegative cache index; absent entries are created, up to 4095.</param>
    /// <param name="size">Positive pixel size, or pixel size and nonnegative outline thickness.</param>
    /// <param name="glyph">Nonnegative source glyph index.</param>
    /// <returns>A typed value or independent snapshot of the current cache state.</returns>
    /// <remarks>Authored edits invalidate text and emit Changed after committing. Image and collection queries return independent copies; invalid geometry or indices fail before publication.</remarks>
    /// <exception cref="ObjectDisposedException">The font is disposed.</exception>
    public int GetGlyphTextureIndex(int cacheIndex, Vector2i size, int glyph) { SizeKey(size); var key = GlyphKey(glyph); return Query(cacheIndex, (cache, data) => { data.GetGlyph(key, size.X, size.Y, 1); return cache.Size(size).Glyphs.GetValueOrDefault(key, new(default, default, default, -1)).Page; }); }
    /// <summary>Sets index of the cache texture containing the glyph.</summary>
    /// <param name="cacheIndex">Nonnegative cache index; absent entries are created, up to 4095.</param>
    /// <param name="size">Positive pixel size, or pixel size and nonnegative outline thickness.</param>
    /// <param name="glyph">Nonnegative source glyph index.</param>
    /// <param name="textureIndex">Texture slot between zero and 65535.</param>
    /// <remarks>Authored edits invalidate text and emit Changed after committing. Image and collection queries return independent copies; invalid geometry or indices fail before publication.</remarks>
    /// <exception cref="ObjectDisposedException">The font is disposed.</exception>
    public void SetGlyphTextureIndex(int cacheIndex, Vector2i size, int glyph, int textureIndex) { SizeKey(size); var key = GlyphKey(glyph); if (textureIndex < -1 || textureIndex >= 65536) throw new ArgumentOutOfRangeException(nameof(textureIndex)); Edit(cacheIndex, cache => PutGlyph(cache, size, key, entry => entry with { Authored = true, Page = textureIndex })); }
    /// <summary>Returns rectangle in the cache texture containing the glyph.</summary>
    /// <param name="cacheIndex">Nonnegative cache index; absent entries are created, up to 4095.</param>
    /// <param name="size">Positive pixel size, or pixel size and nonnegative outline thickness.</param>
    /// <param name="glyph">Nonnegative source glyph index.</param>
    /// <returns>A typed value or independent snapshot of the current cache state.</returns>
    /// <remarks>Authored edits invalidate text and emit Changed after committing. Image and collection queries return independent copies; invalid geometry or indices fail before publication.</remarks>
    /// <exception cref="ObjectDisposedException">The font is disposed.</exception>
    public Rect2 GetGlyphUVRect(int cacheIndex, Vector2i size, int glyph) { SizeKey(size); var key = GlyphKey(glyph); return Query(cacheIndex, (cache, data) => { data.GetGlyph(key, size.X, size.Y, 1); return cache.Size(size).Glyphs.GetValueOrDefault(key).Region; }); }
    /// <summary>Sets rectangle in the cache texture containing the glyph.</summary>
    /// <param name="cacheIndex">Nonnegative cache index; absent entries are created, up to 4095.</param>
    /// <param name="size">Positive pixel size, or pixel size and nonnegative outline thickness.</param>
    /// <param name="glyph">Nonnegative source glyph index.</param>
    /// <param name="uvRect">Finite nonnegative pixel rectangle within the page.</param>
    /// <remarks>Authored edits invalidate text and emit Changed after committing. Image and collection queries return independent copies; invalid geometry or indices fail before publication.</remarks>
    /// <exception cref="ObjectDisposedException">The font is disposed.</exception>
    public void SetGlyphUVRect(int cacheIndex, Vector2i size, int glyph, Rect2 uvRect) { SizeKey(size); var key = GlyphKey(glyph); if (!uvRect.Position.IsFinite() || !uvRect.Size.IsFinite() || uvRect.Size.X < 0 || uvRect.Size.Y < 0) throw new ArgumentOutOfRangeException(nameof(uvRect)); Edit(cacheIndex, cache => PutGlyph(cache, size, key, entry => entry with { Authored = true, Region = uvRect })); }
    /// <summary>Returns the font ascent (number of pixels above the baseline).</summary>
    /// <param name="cacheIndex">Nonnegative cache index; absent entries are created, up to 4095.</param>
    /// <param name="size">Positive pixel size, or pixel size and nonnegative outline thickness.</param>
    /// <returns>A typed value or independent snapshot of the current cache state.</returns>
    /// <remarks>Authored edits invalidate text and emit Changed after committing. Image and collection queries return independent copies; invalid geometry or indices fail before publication.</remarks>
    /// <exception cref="ObjectDisposedException">The font is disposed.</exception>
    public float GetCacheAscent(int cacheIndex, int size) { SizeKey(size); return Query(cacheIndex, (_, data) => data.GetMetrics(size).Ascent); }
    /// <summary>Sets the font ascent (number of pixels above the baseline).</summary>
    /// <param name="cacheIndex">Nonnegative cache index; absent entries are created, up to 4095.</param>
    /// <param name="size">Positive pixel size, or pixel size and nonnegative outline thickness.</param>
    /// <param name="value">New typed value.</param>
    /// <remarks>Authored edits invalidate text and emit Changed after committing. Image and collection queries return independent copies; invalid geometry or indices fail before publication.</remarks>
    /// <exception cref="ObjectDisposedException">The font is disposed.</exception>
    public void SetCacheAscent(int cacheIndex, int size, float value) { SizeKey(size); Finite(value); Edit(cacheIndex, cache => cache.Size(new(size, 0)).Metrics[0] = value); }
    /// <summary>Returns the font descent (number of pixels below the baseline).</summary>
    /// <param name="cacheIndex">Nonnegative cache index; absent entries are created, up to 4095.</param>
    /// <param name="size">Positive pixel size, or pixel size and nonnegative outline thickness.</param>
    /// <returns>A typed value or independent snapshot of the current cache state.</returns>
    /// <remarks>Authored edits invalidate text and emit Changed after committing. Image and collection queries return independent copies; invalid geometry or indices fail before publication.</remarks>
    /// <exception cref="ObjectDisposedException">The font is disposed.</exception>
    public float GetCacheDescent(int cacheIndex, int size) { SizeKey(size); return Query(cacheIndex, (_, data) => data.GetMetrics(size).Descent); }
    /// <summary>Sets the font descent (number of pixels below the baseline).</summary>
    /// <param name="cacheIndex">Nonnegative cache index; absent entries are created, up to 4095.</param>
    /// <param name="size">Positive pixel size, or pixel size and nonnegative outline thickness.</param>
    /// <param name="value">New typed value.</param>
    /// <remarks>Authored edits invalidate text and emit Changed after committing. Image and collection queries return independent copies; invalid geometry or indices fail before publication.</remarks>
    /// <exception cref="ObjectDisposedException">The font is disposed.</exception>
    public void SetCacheDescent(int cacheIndex, int size, float value) { SizeKey(size); Finite(value); Edit(cacheIndex, cache => cache.Size(new(size, 0)).Metrics[1] = value); }
    /// <summary>Returns pixel offset of the underline below the baseline.</summary>
    /// <param name="cacheIndex">Nonnegative cache index; absent entries are created, up to 4095.</param>
    /// <param name="size">Positive pixel size, or pixel size and nonnegative outline thickness.</param>
    /// <returns>A typed value or independent snapshot of the current cache state.</returns>
    /// <remarks>Authored edits invalidate text and emit Changed after committing. Image and collection queries return independent copies; invalid geometry or indices fail before publication.</remarks>
    /// <exception cref="ObjectDisposedException">The font is disposed.</exception>
    public float GetCacheUnderlinePosition(int cacheIndex, int size) { SizeKey(size); return Query(cacheIndex, (_, data) => data.GetMetrics(size).UnderlinePosition); }
    /// <summary>Sets pixel offset of the underline below the baseline.</summary>
    /// <param name="cacheIndex">Nonnegative cache index; absent entries are created, up to 4095.</param>
    /// <param name="size">Positive pixel size, or pixel size and nonnegative outline thickness.</param>
    /// <param name="value">New typed value.</param>
    /// <remarks>Authored edits invalidate text and emit Changed after committing. Image and collection queries return independent copies; invalid geometry or indices fail before publication.</remarks>
    /// <exception cref="ObjectDisposedException">The font is disposed.</exception>
    public void SetCacheUnderlinePosition(int cacheIndex, int size, float value) { SizeKey(size); Finite(value); Edit(cacheIndex, cache => cache.Size(new(size, 0)).Metrics[2] = value); }
    /// <summary>Returns thickness of the underline in pixels.</summary>
    /// <param name="cacheIndex">Nonnegative cache index; absent entries are created, up to 4095.</param>
    /// <param name="size">Positive pixel size, or pixel size and nonnegative outline thickness.</param>
    /// <returns>A typed value or independent snapshot of the current cache state.</returns>
    /// <remarks>Authored edits invalidate text and emit Changed after committing. Image and collection queries return independent copies; invalid geometry or indices fail before publication.</remarks>
    /// <exception cref="ObjectDisposedException">The font is disposed.</exception>
    public float GetCacheUnderlineThickness(int cacheIndex, int size) { SizeKey(size); return Query(cacheIndex, (_, data) => data.GetMetrics(size).UnderlineThickness); }
    /// <summary>Sets thickness of the underline in pixels.</summary>
    /// <param name="cacheIndex">Nonnegative cache index; absent entries are created, up to 4095.</param>
    /// <param name="size">Positive pixel size, or pixel size and nonnegative outline thickness.</param>
    /// <param name="value">New typed value.</param>
    /// <remarks>Authored edits invalidate text and emit Changed after committing. Image and collection queries return independent copies; invalid geometry or indices fail before publication.</remarks>
    /// <exception cref="ObjectDisposedException">The font is disposed.</exception>
    public void SetCacheUnderlineThickness(int cacheIndex, int size, float value) { SizeKey(size); Finite(value); Edit(cacheIndex, cache => cache.Size(new(size, 0)).Metrics[3] = value); }
    /// <summary>Returns scaling factor of the color bitmap font.</summary>
    /// <param name="cacheIndex">Nonnegative cache index; absent entries are created, up to 4095.</param>
    /// <param name="size">Positive pixel size, or pixel size and nonnegative outline thickness.</param>
    /// <returns>A typed value or independent snapshot of the current cache state.</returns>
    /// <remarks>Authored edits invalidate text and emit Changed after committing. Image and collection queries return independent copies; invalid geometry or indices fail before publication.</remarks>
    /// <exception cref="ObjectDisposedException">The font is disposed.</exception>
    public float GetCacheScale(int cacheIndex, int size) { SizeKey(size); return Query(cacheIndex, (cache, _) => cache.Size(new(size, 0)).Scale); }
    /// <summary>Sets scaling factor of the color bitmap font.</summary>
    /// <param name="cacheIndex">Nonnegative cache index; absent entries are created, up to 4095.</param>
    /// <param name="size">Positive pixel size, or pixel size and nonnegative outline thickness.</param>
    /// <param name="value">New typed value.</param>
    /// <remarks>Authored edits invalidate text and emit Changed after committing. Image and collection queries return independent copies; invalid geometry or indices fail before publication.</remarks>
    /// <exception cref="ObjectDisposedException">The font is disposed.</exception>
    public void SetCacheScale(int cacheIndex, int size, float value) { SizeKey(size); Finite(value); if (value <= 0) throw new ArgumentOutOfRangeException(nameof(value)); Edit(cacheIndex, cache => { var record = cache.Size(new(size, 0)); record.Scale = value; foreach (var key in record.Glyphs.Keys) record.Glyphs[key] = record.Glyphs[key] with { Authored = true }; }); }
    /// <summary>Returns kerning for the pair of glyphs.</summary>
    /// <param name="cacheIndex">Nonnegative cache index; absent entries are created, up to 4095.</param>
    /// <param name="size">Positive pixel size, or pixel size and nonnegative outline thickness.</param>
    /// <param name="glyphPair">Pair of nonnegative source glyph indices.</param>
    /// <returns>A typed value or independent snapshot of the current cache state.</returns>
    /// <remarks>Authored edits invalidate text and emit Changed after committing. Image and collection queries return independent copies; invalid geometry or indices fail before publication.</remarks>
    /// <exception cref="ObjectDisposedException">The font is disposed.</exception>
    public Vector2 GetKerning(int cacheIndex, int size, Vector2i glyphPair) { SizeKey(size); PairKey(glyphPair); return Query(cacheIndex, (_, data) => data.GetKerning(glyphPair, size)); }
    /// <summary>Sets kerning for the pair of glyphs.</summary>
    /// <param name="cacheIndex">Nonnegative cache index; absent entries are created, up to 4095.</param>
    /// <param name="size">Positive pixel size, or pixel size and nonnegative outline thickness.</param>
    /// <param name="glyphPair">Pair of nonnegative source glyph indices.</param>
    /// <param name="value">New typed value.</param>
    /// <remarks>Authored edits invalidate text and emit Changed after committing. Image and collection queries return independent copies; invalid geometry or indices fail before publication.</remarks>
    /// <exception cref="ObjectDisposedException">The font is disposed.</exception>
    public void SetKerning(int cacheIndex, int size, Vector2i glyphPair, Vector2 value) { SizeKey(size); PairKey(glyphPair); Finite(value); Edit(cacheIndex, cache => cache.Size(new(size, 0)).Kerning[glyphPair] = value); }
    /// <summary>Returns list of the kerning overrides.</summary>
    /// <param name="cacheIndex">Nonnegative cache index; absent entries are created, up to 4095.</param>
    /// <param name="size">Positive pixel size, or pixel size and nonnegative outline thickness.</param>
    /// <returns>A typed value or independent snapshot of the current cache state.</returns>
    /// <remarks>Authored edits invalidate text and emit Changed after committing. Image and collection queries return independent copies; invalid geometry or indices fail before publication.</remarks>
    /// <exception cref="ObjectDisposedException">The font is disposed.</exception>
    public Vector2i[] GetKerningList(int cacheIndex, int size) { SizeKey(size); return Query(cacheIndex, (cache, _) => cache.Size(new(size, 0)).Kerning.Keys.ToArray()); }
    /// <summary>Removes all kerning overrides.</summary>
    /// <param name="cacheIndex">Nonnegative cache index; absent entries are created, up to 4095.</param>
    /// <param name="size">Positive pixel size, or pixel size and nonnegative outline thickness.</param>
    /// <remarks>Authored edits invalidate text and emit Changed after committing. Image and collection queries return independent copies; invalid geometry or indices fail before publication.</remarks>
    /// <exception cref="ObjectDisposedException">The font is disposed.</exception>
    public void ClearKerningMap(int cacheIndex, int size) { SizeKey(size); Edit(cacheIndex, cache => cache.Size(new(size, 0)).Kerning.Clear()); }
    /// <summary>Removes kerning override for the pair of glyphs.</summary>
    /// <param name="cacheIndex">Nonnegative cache index; absent entries are created, up to 4095.</param>
    /// <param name="size">Positive pixel size, or pixel size and nonnegative outline thickness.</param>
    /// <param name="glyphPair">Pair of nonnegative source glyph indices.</param>
    /// <remarks>Authored edits invalidate text and emit Changed after committing. Image and collection queries return independent copies; invalid geometry or indices fail before publication.</remarks>
    /// <exception cref="ObjectDisposedException">The font is disposed.</exception>
    public void RemoveKerning(int cacheIndex, int size, Vector2i glyphPair) { SizeKey(size); PairKey(glyphPair); Edit(cacheIndex, cache => cache.Size(new(size, 0)).Kerning.Remove(glyphPair)); }
    /// <summary>Returns an active face index in the TrueType / OpenType collection.</summary>
    /// <param name="cacheIndex">Nonnegative cache index; absent entries are created, up to 4095.</param>
    /// <returns>A typed value or independent snapshot of the current cache state.</returns>
    /// <remarks>Authored edits invalidate text and emit Changed after committing. Image and collection queries return independent copies; invalid geometry or indices fail before publication.</remarks>
    /// <exception cref="ObjectDisposedException">The font is disposed.</exception>
    public int GetFaceIndex(int cacheIndex) => Query(cacheIndex, static (cache, _) => cache.Instance.FaceIndex);
    /// <summary>Sets an active face index in the TrueType / OpenType collection.</summary>
    /// <param name="cacheIndex">Nonnegative cache index; absent entries are created, up to 4095.</param>
    /// <param name="value">New typed value.</param>
    /// <remarks>Authored edits invalidate text and emit Changed after committing. Image and collection queries return independent copies; invalid geometry or indices fail before publication.</remarks>
    /// <exception cref="ObjectDisposedException">The font is disposed.</exception>
    public void SetFaceIndex(int cacheIndex, int value) { ArgumentOutOfRangeException.ThrowIfNegative(value); Edit(cacheIndex, cache => { cache.Instance = cache.Instance with { FaceIndex = value }; if (_bytes.Length > 0) cache.Sizes.Clear(); }); }
    /// <summary>Returns embolden strength, if is not equal to zero, emboldens the font outlines. Negative values reduce the outline thickness.</summary>
    /// <param name="cacheIndex">Nonnegative cache index; absent entries are created, up to 4095.</param>
    /// <returns>A typed value or independent snapshot of the current cache state.</returns>
    /// <remarks>Authored edits invalidate text and emit Changed after committing. Image and collection queries return independent copies; invalid geometry or indices fail before publication.</remarks>
    /// <exception cref="ObjectDisposedException">The font is disposed.</exception>
    public float GetEmbolden(int cacheIndex) => Query(cacheIndex, static (cache, _) => cache.Instance.Embolden);
    /// <summary>Sets embolden strength, if is not equal to zero, emboldens the font outlines. Negative values reduce the outline thickness.</summary>
    /// <param name="cacheIndex">Nonnegative cache index; absent entries are created, up to 4095.</param>
    /// <param name="value">New typed value.</param>
    /// <remarks>Authored edits invalidate text and emit Changed after committing. Image and collection queries return independent copies; invalid geometry or indices fail before publication.</remarks>
    /// <exception cref="ObjectDisposedException">The font is disposed.</exception>
    public void SetEmbolden(int cacheIndex, float value) { Finite(value); Edit(cacheIndex, cache => { cache.Instance = cache.Instance with { Embolden = value }; if (_bytes.Length > 0) cache.Sizes.Clear(); }); }
    /// <summary>Returns extra baseline offset (as a fraction of font height).</summary>
    /// <param name="cacheIndex">Nonnegative cache index; absent entries are created, up to 4095.</param>
    /// <returns>A typed value or independent snapshot of the current cache state.</returns>
    /// <remarks>Authored edits invalidate text and emit Changed after committing. Image and collection queries return independent copies; invalid geometry or indices fail before publication.</remarks>
    /// <exception cref="ObjectDisposedException">The font is disposed.</exception>
    public float GetExtraBaselineOffset(int cacheIndex) => Query(cacheIndex, static (cache, _) => cache.Instance.BaselineOffset);
    /// <summary>Sets extra baseline offset (as a fraction of font height).</summary>
    /// <param name="cacheIndex">Nonnegative cache index; absent entries are created, up to 4095.</param>
    /// <param name="value">New typed value.</param>
    /// <remarks>Authored edits invalidate text and emit Changed after committing. Image and collection queries return independent copies; invalid geometry or indices fail before publication.</remarks>
    /// <exception cref="ObjectDisposedException">The font is disposed.</exception>
    public void SetExtraBaselineOffset(int cacheIndex, float value) { Finite(value); Edit(cacheIndex, cache => { cache.Instance = cache.Instance with { BaselineOffset = value }; if (_bytes.Length > 0) cache.Sizes.Clear(); }); }
    /// <summary>Returns transform, applied to the font outlines, can be used for slanting, flipping and rotating glyphs.</summary>
    /// <param name="cacheIndex">Nonnegative cache index; absent entries are created, up to 4095.</param>
    /// <returns>A typed value or independent snapshot of the current cache state.</returns>
    /// <remarks>Authored edits invalidate text and emit Changed after committing. Image and collection queries return independent copies; invalid geometry or indices fail before publication.</remarks>
    /// <exception cref="ObjectDisposedException">The font is disposed.</exception>
    public Transform GetTransform(int cacheIndex) => Query(cacheIndex, static (cache, _) => cache.Instance.Transform);
    /// <summary>Sets transform, applied to the font outlines, can be used for slanting, flipping, and rotating glyphs.</summary>
    /// <param name="cacheIndex">Nonnegative cache index; absent entries are created, up to 4095.</param>
    /// <param name="value">New typed value.</param>
    /// <remarks>Authored edits invalidate text and emit Changed after committing. Image and collection queries return independent copies; invalid geometry or indices fail before publication.</remarks>
    /// <exception cref="ObjectDisposedException">The font is disposed.</exception>
    public void SetTransform(int cacheIndex, Transform value) { if (!value.X.IsFinite() || !value.Y.IsFinite() || !value.Origin.IsFinite()) throw new ArgumentOutOfRangeException(nameof(value)); Edit(cacheIndex, cache => { cache.Instance = cache.Instance with { Transform = value }; if (_bytes.Length > 0) cache.Sizes.Clear(); }); }
    /// <summary>Returns variation coordinates for the specified font cache entry.</summary>
    /// <param name="cacheIndex">Nonnegative cache index; absent entries are created, up to 4095.</param>
    /// <returns>A typed value or independent snapshot of the current cache state.</returns>
    /// <remarks>Authored edits invalidate text and emit Changed after committing. Image and collection queries return independent copies; invalid geometry or indices fail before publication.</remarks>
    /// <exception cref="ObjectDisposedException">The font is disposed.</exception>
    public Dictionary<uint, float> GetVariationCoordinates(int cacheIndex) => Query<Dictionary<uint, float>>(cacheIndex, static (cache, _) => new(cache.Instance.Coordinates));
    /// <summary>Sets variation coordinates for the specified font cache entry.</summary>
    /// <param name="cacheIndex">Nonnegative cache index; absent entries are created, up to 4095.</param>
    /// <param name="value">New typed value.</param>
    /// <remarks>Authored edits invalidate text and emit Changed after committing. Image and collection queries return independent copies; invalid geometry or indices fail before publication.</remarks>
    /// <exception cref="ObjectDisposedException">The font is disposed.</exception>
    public void SetVariationCoordinates(int cacheIndex, Dictionary<uint, float> value) { ArgumentNullException.ThrowIfNull(value); value = new(value); foreach (var coordinate in value.Values) Finite(coordinate); Edit(cacheIndex, cache => { cache.Instance = cache.Instance with { Coordinates = value }; if (_bytes.Length > 0) cache.Sizes.Clear(); }); }
    /// <summary>Returns spacing for the spacing category in pixels (not relative to the font size).</summary>
    /// <param name="cacheIndex">Nonnegative cache index; absent entries are created, up to 4095.</param>
    /// <param name="spacing">Valid text spacing category.</param>
    /// <returns>A typed value or independent snapshot of the current cache state.</returns>
    /// <remarks>Authored edits invalidate text and emit Changed after committing. Image and collection queries return independent copies; invalid geometry or indices fail before publication.</remarks>
    /// <exception cref="ObjectDisposedException">The font is disposed.</exception>
    public int GetExtraSpacing(int cacheIndex, TextSpacingType spacing) { ValidateSpacing(spacing); lock (FontGate) return CacheLocked(cacheIndex).Spacing[(int)spacing]; }
    /// <summary>Sets the spacing for the spacing category to [param value] in pixels (not relative to the font size).</summary>
    /// <param name="cacheIndex">Nonnegative cache index; absent entries are created, up to 4095.</param>
    /// <param name="spacing">Valid text spacing category.</param>
    /// <param name="value">New typed value.</param>
    /// <remarks>Authored edits invalidate text and emit Changed after committing. Image and collection queries return independent copies; invalid geometry or indices fail before publication.</remarks>
    /// <exception cref="ObjectDisposedException">The font is disposed.</exception>
    public void SetExtraSpacing(int cacheIndex, TextSpacingType spacing, int value) { ValidateSpacing(spacing); Edit(cacheIndex, cache => cache.Spacing[(int)spacing] = value); }
    private static void ValidateShelves(int[] shelves, int width, int height)
    {
        if (shelves.Length % 4 != 0 || shelves.Length > 65536) throw new ArgumentException("Packing offsets require x/y/remaining-width/height tuples.", nameof(shelves));
        for (var i = 0; i < shelves.Length; i += 4)
            if (shelves[i] < 0 || shelves[i + 1] < 0 || shelves[i + 2] < 0 || shelves[i + 3] <= 0 || (long)shelves[i] + shelves[i + 2] > width || (long)shelves[i + 1] + shelves[i + 3] > height)
                throw new ArgumentOutOfRangeException(nameof(shelves));
    }
    private static void ValidateSpacing(TextSpacingType spacing) { if (spacing < TextSpacingType.Glyph || spacing >= TextSpacingType.Max) throw new ArgumentOutOfRangeException(nameof(spacing)); }
    /// <inheritdoc />
    public override int GetSpacing(TextSpacingType spacing) => GetExtraSpacing(0, spacing);
    private void ResetCaches(int? remove)
    {
        EditCacheList(caches => { if (remove is { } index) { if (index < 0 || index >= caches.Count) throw new ArgumentOutOfRangeException(nameof(remove)); caches.RemoveAt(index); } else caches.Clear(); }, false);
    }
    private void EditCacheList(Action<List<FontCache>> edit, bool ensurePrimary = true)
    {
        List<FontData> retired;
        lock (FontGate)
        {
            ThrowIfDisposed(); EnsureLoadedLocked(); if (ensurePrimary) CacheLocked(0);
            var copies = new List<FontCache>();
            for (var i = 0; i < _caches.Count; i++) { var data = DataLocked(i); lock (data.Gate) copies.Add(_caches[i].Clone()); }
            edit(copies); retired = PublishCachesLocked(copies);
        }
        NotifyRetired(retired);
    }
}
