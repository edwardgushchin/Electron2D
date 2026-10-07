namespace Electron2D;

internal sealed partial class FontData
{
    internal bool AllowSystemFallback { get; set; }
    internal int[]? SystemFaces { get; set; }
    internal int? AdvertisedFaceCount { get; set; }
    private readonly Dictionary<(uint Scalar, string Locale), FontData?> _systemScalars = [];
    private readonly Dictionary<(string Path, int Face), FontData> _systemSources = [];
    internal FontData? FindSystemFallback(uint scalar, string locale)
    {
        lock (_gate)
        {
            Check(); if (!AllowSystemFallback) return null;
            var key = (scalar, locale); if (_systemScalars.TryGetValue(key, out var cached)) return cached;
            var matches = OS.FontCatalog.Match(FamilyName, new System.Text.Rune((int)scalar).ToString(), locale, FontWeight, FontStretch, (FontStyle & FontStyle.Italic) != 0);
            foreach (var match in matches)
            {
                var sourceKey = (match.Path, match.FaceIndex);
                if (!_systemSources.TryGetValue(sourceKey, out var data))
                {
                    if (_systemSources.Count >= 64) break;
                    try
                    {
                        data = LoadSystemSource(match);
                        data.Hinting = Hinting; data.SubpixelPositioning = SubpixelPositioning; data.KeepRoundingRemainders = KeepRoundingRemainders;
                        data.Oversampling = Oversampling; data.ModulateColorGlyphs = ModulateColorGlyphs; data.OpenTypeFeatures = OpenTypeFeatures;
                        _systemSources.Add(sourceKey, data);
                    }
                    catch (Exception error) when (error is IOException or ArgumentException or InvalidOperationException or NotSupportedException) { continue; }
                }
                if (data.GetGlyphIndex(scalar) != 0) { _systemScalars[key] = data; return data; }
            }
            if (_systemScalars.Count >= 65536) _systemScalars.Clear(); _systemScalars[key] = null; return null;
        }
    }
    private readonly Dictionary<(ulong Hash, string Locale), List<(uint[] Scalars, FontData? Face)>> _systemClusters = [];
    internal FontData? FindSystemFallback(ReadOnlySpan<uint> scalars, string locale)
    {
        if (scalars.Length == 1) return FindSystemFallback(scalars[0], locale);
        lock (_gate)
        {
            Check(); if (!AllowSystemFallback || scalars.Length == 0) return null;
            ulong hash = 14695981039346656037; foreach (var scalar in scalars) hash = unchecked((hash ^ scalar) * 1099511628211);
            var key = (hash, locale);
            if (_systemClusters.TryGetValue(key, out var entries)) foreach (var entry in entries) if (entry.Scalars.AsSpan().SequenceEqual(scalars)) return entry.Face;
            var text = new System.Text.StringBuilder(); foreach (var scalar in scalars) if (!System.Text.Rune.IsValid(scalar)) throw new ArgumentOutOfRangeException(nameof(scalars)); else text.Append(new System.Text.Rune((int)scalar).ToString());
            var matches = OS.FontCatalog.Match(FamilyName, text.ToString(), locale, FontWeight, FontStretch, (FontStyle & FontStyle.Italic) != 0);
            FontData? selected = null;
            foreach (var match in matches)
            {
                var sourceKey = (match.Path, match.FaceIndex);
                if (!_systemSources.TryGetValue(sourceKey, out var data))
                {
                    if (_systemSources.Count >= 64) break;
                    try { data = LoadSystemSource(match); data.Hinting = Hinting; data.SubpixelPositioning = SubpixelPositioning; data.KeepRoundingRemainders = KeepRoundingRemainders; data.Oversampling = Oversampling; data.ModulateColorGlyphs = ModulateColorGlyphs; data.OpenTypeFeatures = OpenTypeFeatures; _systemSources.Add(sourceKey, data); }
                    catch (Exception error) when (error is IOException or ArgumentException or InvalidOperationException or NotSupportedException) { continue; }
                }
                var supported = true;
                foreach (var scalar in scalars)
                    if (scalar is not (0x200C or 0x200D) && scalar is not (>= 0xFE00 and <= 0xFE0F) && scalar is not (>= 0xE0100 and <= 0xE01EF) && data.GetGlyphIndex(scalar) == 0) { supported = false; break; }
                if (supported) { selected = data; break; }
            }
            if (_systemClusters.Count >= 4096) _systemClusters.Clear();
            if (!_systemClusters.TryGetValue(key, out entries)) _systemClusters.Add(key, entries = []);
            entries.Add((scalars.ToArray(), selected)); return selected;
        }
    }
    internal static FontData LoadSystemSource(SystemFontMatch match)
    {
        using var source = FileAccess.Open(match.Path, FileAccessModeFlags.Read); var length = source.Length;
        if (length is <= 0 or > 64 * 1024 * 1024) throw new InvalidDataException("System font source exceeds its byte budget.");
        var bytes = source.ReadBytes((int)length); if (bytes.Length != length || source.Length != length) throw new IOException("System font changed while being read.");
        return new FontData(bytes, match.FaceIndex);
    }
    internal byte[] SourceBytes { get { lock (_gate) { Check(); return _bytes; } } }
    private readonly List<FontData> _retiredSystemSources = [];
    private void ReleaseRetiredSystemSources() { foreach (var source in _retiredSystemSources) source.Dispose(); _retiredSystemSources.Clear(); }
    internal void ClearSystemSources()
    {
        _systemScalars.Clear(); _systemClusters.Clear(); foreach (var source in _systemSources.Values) { if (_readers > 0) _retiredSystemSources.Add(source); else source.Dispose(); }
        _systemSources.Clear(); if (_readers == 0) ReleaseRetiredSystemSources();
    }
}
