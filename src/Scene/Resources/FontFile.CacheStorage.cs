using System.Text;

namespace Electron2D;

public partial class FontFile
{
    private static readonly PropertyDescriptor CacheProperty = new PropertyDescriptor<FontFile, byte[]>(
        "_font_cache", font => font.SaveCaches(), (font, value) => font.RestoreCaches(value), _ => [], stored: true);
    private byte[] SaveCaches()
    {
        lock (FontGate)
        {
            ThrowIfDisposed(); if (_caches.Count == 0) return [];
            using var stream = new MemoryStream(); using var writer = new BinaryWriter(stream, Encoding.UTF8, true);
            writer.Write(1); writer.Write(_caches.Count);
            foreach (var cache in _caches)
            {
                var index = _caches.IndexOf(cache); var data = DataLocked(index);
                lock (data.Gate)
                {
                    writer.Write(cache.FixedSize); writer.Write((int)cache.ScaleMode); writer.Write(cache.Instance.FaceIndex); writer.Write(cache.Instance.Embolden); writer.Write(cache.Instance.BaselineOffset);
                    V(writer, cache.Instance.Transform.X); V(writer, cache.Instance.Transform.Y); V(writer, cache.Instance.Transform.Origin);
                    writer.Write(cache.Instance.Coordinates.Count); foreach (var p in cache.Instance.Coordinates) { writer.Write(p.Key); writer.Write(p.Value); }
                    foreach (var spacing in cache.Spacing) writer.Write(spacing);
                    writer.Write(cache.Sizes.Count);
                    foreach (var p in cache.Sizes)
                    {
                        writer.Write(p.Key.X); writer.Write(p.Key.Y); var size = p.Value; foreach (var metric in size.Metrics) writer.Write(metric); writer.Write(size.Scale);
                        writer.Write(size.Advances.Count); foreach (var value in size.Advances) { writer.Write(value.Key); V(writer, value.Value); }
                        writer.Write(size.Kerning.Count); foreach (var value in size.Kerning) { writer.Write(value.Key.X); writer.Write(value.Key.Y); V(writer, value.Value); }
                        writer.Write(size.Glyphs.Count); foreach (var value in size.Glyphs) { writer.Write(value.Key); V(writer, value.Value.Offset); V(writer, value.Value.Size); V(writer, value.Value.Region.Position); V(writer, value.Value.Region.Size); writer.Write(value.Value.Page); writer.Write(value.Value.Colored); writer.Write(value.Value.Authored); }
                        writer.Write(size.Pages.Count); foreach (var page in size.Pages)
                        {
                            writer.Write(page != null); if (page == null) continue;
                            writer.Write(page.Width); writer.Write(page.Height); writer.Write(page.Pixels.Length); writer.Write(page.Pixels);
                            writer.Write(page.Offsets.Length); foreach (var offset in page.Offsets) writer.Write(offset);
                        }
                        if (stream.Length > MaximumFontBytes) throw new InvalidDataException("Authored font cache exceeds sixty-four MiB.");
                    }
                }
            }
            return stream.ToArray();
        }
    }
    private static void V(BinaryWriter writer, Vector2 value) { writer.Write(value.X); writer.Write(value.Y); }
    private static Vector2 V(BinaryReader reader) => Finite(new Vector2(reader.ReadSingle(), reader.ReadSingle()));
    private static int Count(BinaryReader reader, int maximum = 65536) { var value = reader.ReadInt32(); if (value < 0 || value > maximum) throw new InvalidDataException("Font cache count exceeds its budget."); return value; }
    private void RestoreCaches(byte[] bytes)
    {
        ArgumentNullException.ThrowIfNull(bytes); if (bytes.Length > MaximumFontBytes) throw new InvalidDataException("Authored font cache exceeds sixty-four MiB.");
        var caches = new List<FontCache>();
        if (bytes.Length > 0)
        {
            using var stream = new MemoryStream(bytes, false); using var reader = new BinaryReader(stream, Encoding.UTF8, true);
            if (reader.ReadInt32() != 1) throw new InvalidDataException("Unsupported font cache version.");
            var count = Count(reader, 4096);
            for (var i = 0; i < count; i++)
            {
                var cache = new FontCache { FixedSize = Count(reader, 16384), ScaleMode = (FixedSizeScaleMode)Count(reader, 2) }; var face = Count(reader); var embolden = Finite(reader.ReadSingle()); var baseline = Finite(reader.ReadSingle());
                var transform = new Transform(V(reader), V(reader), V(reader)); var coordinates = new Dictionary<uint, float>();
                var n = Count(reader); for (var j = 0; j < n; j++) coordinates.Add(reader.ReadUInt32(), Finite(reader.ReadSingle()));
                cache.Instance = new(face, coordinates, embolden, transform, baseline, 0, []);
                for (var j = 0; j < cache.Spacing.Length; j++) cache.Spacing[j] = reader.ReadInt32();
                n = Count(reader); for (var j = 0; j < n; j++)
                {
                    var key = SizeKey(new Vector2i(reader.ReadInt32(), reader.ReadInt32())); var size = new FontCacheSize();
                    for (var k = 0; k < 4; k++) { var metric = reader.ReadSingle(); if (float.IsInfinity(metric)) throw new InvalidDataException("Nonfinite font metric."); size.Metrics[k] = metric; }
                    size.Scale = Finite(reader.ReadSingle()); if (size.Scale <= 0) throw new InvalidDataException("Nonpositive font scale.");
                    var entries = Count(reader); for (var k = 0; k < entries; k++) size.Advances.Add(GlyphKey(reader.ReadInt32()), V(reader));
                    entries = Count(reader); for (var k = 0; k < entries; k++) size.Kerning.Add(PairKey(new(reader.ReadInt32(), reader.ReadInt32())), V(reader));
                    entries = Count(reader); for (var k = 0; k < entries; k++)
                    {
                        var glyph = GlyphKey(reader.ReadInt32()); var offset = V(reader); var dimensions = V(reader); var uv = new Rect2(V(reader), V(reader)); var page = reader.ReadInt32();
                        if (dimensions.X < 0 || dimensions.Y < 0 || uv.Size.X < 0 || uv.Size.Y < 0 || page < -1 || page >= 65536) throw new InvalidDataException("Invalid glyph record.");
                        size.Glyphs.Add(glyph, new(offset, dimensions, uv, page, reader.ReadBoolean(), reader.ReadBoolean()));
                    }
                    entries = Count(reader); for (var k = 0; k < entries; k++)
                    {
                        if (!reader.ReadBoolean()) { size.Pages.Add(null); continue; }
                        var width = Count(reader, 16384); var height = Count(reader, 16384); var length = Count(reader, MaximumFontBytes);
                        if (width == 0 || height == 0 || (long)width * height * 4 != length || length > stream.Length - stream.Position) throw new InvalidDataException("Invalid cache page dimensions.");
                        var pixels = reader.ReadBytes(length); var offsets = new int[Count(reader, 16384)];
                        for (var x = 0; x < offsets.Length; x++) { offsets[x] = Count(reader, 16384); }
                        ValidateShelves(offsets, width, height);
                        size.Pages.Add(new(width, height, pixels, offsets));
                    }
                    cache.Sizes.Add(key, size);
                }
                caches.Add(cache);
            }
            if (stream.Position != stream.Length) throw new InvalidDataException("Trailing font cache data.");
        }
        PublishCaches(caches);
    }
    private void PublishCaches(List<FontCache> caches)
    {
        List<FontData> retired;
        lock (FontGate) { ThrowIfDisposed(); EnsureLoadedLocked(); retired = PublishCachesLocked(caches); }
        NotifyRetired(retired);
    }
    private List<FontData> PublishCachesLocked(List<FontCache> caches)
    {
        List<FontData> created = [], retired = [];
        try { foreach (var cache in caches) created.Add(BuildCacheData(cache)); if (created.Count == 0) { var data = new FontData(_bytes); CopyConfiguration(_fontData!, data); created.Add(data); } }
        catch { foreach (var data in created) data.Dispose(); throw; }
        retired.Add(_fontData!); retired.AddRange(_cacheData.Values); _cacheData.Clear(); _caches.Clear(); _caches.AddRange(caches);
        if (caches.Count > 0) { _fixedSize = caches[0].FixedSize; _fixedScaleMode = caches[0].ScaleMode; }
        _fontData = created[0]; for (var i = 1; i < created.Count; i++) _cacheData.Add(i, created[i]); InvalidateFontStateLocked(); return retired;
    }
    private void NotifyRetired(List<FontData> retired)
    {
        List<Exception>? errors = null;
        try { EmitChanged(); } catch (Exception error) { (errors ??= []).Add(error); }
        foreach (var data in retired) try { data.Dispose(); } catch (Exception error) { (errors ??= []).Add(error); }
        if (errors is { Count: 1 }) System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(errors[0]).Throw();
        if (errors != null) throw new AggregateException("Font cache publication cleanup failed.", errors);
    }
}
