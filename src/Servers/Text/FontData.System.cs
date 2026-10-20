namespace Electron2D;

internal sealed partial class FontData
{
    internal bool AllowSystemFallback { get; set; }
    internal int[]? SystemFaces { get; set; }
    internal int? AdvertisedFaceCount { get; set; }
    private readonly record struct SystemParameters(int Glyph, int Space, int Top, int Bottom, float Baseline);
    private SystemParameters Parameters(Font? font) => new(
        font?.GetShapingSpacing(this, TextSpacingType.Glyph) ?? Authored?.Spacing[0] ?? 0,
        font?.GetShapingSpacing(this, TextSpacingType.Space) ?? Authored?.Spacing[1] ?? 0,
        font?.GetShapingSpacing(this, TextSpacingType.Top) ?? Authored?.Spacing[2] ?? 0,
        font?.GetShapingSpacing(this, TextSpacingType.Bottom) ?? Authored?.Spacing[3] ?? 0,
        font?.GetSystemBaseline(this) ?? SourceBaseline);
    private readonly Dictionary<(uint Scalar, string Locale, SystemParameters Parameters), FontData?> _systemScalars = [];
    private readonly Dictionary<(string Path, int Face, SystemParameters Parameters), FontFile> _systemSources = [];
    internal FontData? FindSystemFallback(uint scalar, string locale, Font? font = null)
    {
        var parameters = Parameters(font);
        lock (_gate)
        {
            Check(); RefreshSystemSources(); if (!AllowSystemFallback) return null;
            var key = (scalar, locale, parameters); if (_systemScalars.TryGetValue(key, out var cached)) return cached;
            var matches = OS.FontCatalog.Match(FamilyName, new System.Text.Rune((int)scalar).ToString(), locale, FontWeight, FontStretch, (FontStyle & FontStyle.Italic) != 0);
            foreach (var match in matches)
            {
                var sourceKey = (match.Path, match.FaceIndex, parameters);
                if (!_systemSources.TryGetValue(sourceKey, out var file))
                {
                    if (_systemSources.Count >= 64) break;
                    try
                    {
                        file = CreateSystemSource(match, parameters);
                        _systemSources.Add(sourceKey, file);
                    }
                    catch (Exception error) when (error is IOException or ArgumentException or InvalidOperationException or NotSupportedException) { continue; }
                }
                var data = file.PrimaryData!;
                if (data.GetGlyphIndex(scalar) != 0) { _systemScalars[key] = data; return data; }
            }
            if (_systemScalars.Count >= 65536) _systemScalars.Clear(); _systemScalars[key] = null; return null;
        }
    }
    private readonly Dictionary<(ulong Hash, string Locale, SystemParameters Parameters), List<(uint[] Scalars, FontData? Face)>> _systemClusters = [];
    internal FontData? FindSystemFallback(ReadOnlySpan<uint> scalars, string locale, Font? font = null)
    {
        if (scalars.Length == 1) return FindSystemFallback(scalars[0], locale, font);
        var parameters = Parameters(font);
        lock (_gate)
        {
            Check(); RefreshSystemSources(); if (!AllowSystemFallback || scalars.Length == 0) return null;
            ulong hash = 14695981039346656037; foreach (var scalar in scalars) hash = unchecked((hash ^ scalar) * 1099511628211);
            var key = (hash, locale, parameters);
            if (_systemClusters.TryGetValue(key, out var entries)) foreach (var entry in entries) if (entry.Scalars.AsSpan().SequenceEqual(scalars)) return entry.Face;
            var text = new System.Text.StringBuilder(); foreach (var scalar in scalars) if (!System.Text.Rune.IsValid(scalar)) throw new ArgumentOutOfRangeException(nameof(scalars)); else text.Append(new System.Text.Rune((int)scalar).ToString());
            var matches = OS.FontCatalog.Match(FamilyName, text.ToString(), locale, FontWeight, FontStretch, (FontStyle & FontStyle.Italic) != 0);
            FontData? selected = null;
            foreach (var match in matches)
            {
                var sourceKey = (match.Path, match.FaceIndex, parameters);
                if (!_systemSources.TryGetValue(sourceKey, out var file))
                {
                    if (_systemSources.Count >= 64) break;
                    try { file = CreateSystemSource(match, parameters); _systemSources.Add(sourceKey, file); }
                    catch (Exception error) when (error is IOException or ArgumentException or InvalidOperationException or NotSupportedException) { continue; }
                }
                var data = file.PrimaryData!;
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
    internal static FontData LoadSystemSource(SystemFontMatch match, FontInstance? instance = null)
    {
        var source = ReadSource(match.Path); return new FontData(source.Bytes, match.FaceIndex, instance: instance, auxiliaryMetrics: source.Metrics);
    }
    internal byte[] SourceMetrics { get { lock (_gate) { Check(); return _auxiliaryMetrics; } } }
    internal byte[] SourceBytes { get { lock (_gate) { Check(); return _bytes; } } }
    private FontFile CreateSystemSource(SystemFontMatch match, SystemParameters parameters)
    {
        var instance = SourceInstance with { FaceIndex = match.FaceIndex, BaselineOffset = parameters.Baseline, PaletteIndex = 0, CustomColors = [] };
        var data = LoadSystemSource(match, instance);
        try
        {
            data.MultichannelSignedDistanceField = MultichannelSignedDistanceField && !data._colorFont && data._scalableFont;
            data.MSDFPixelRange = MSDFPixelRange; data.MSDFSize = MSDFSize; data.Antialiasing = Antialiasing;
            data.DisableEmbeddedBitmaps = DisableEmbeddedBitmaps; data.ForceAutohinter = ForceAutohinter;
            data.GenerateMIPMaps = GenerateMIPMaps; data.Hinting = Hinting; data.SubpixelPositioning = SubpixelPositioning;
            data.KeepRoundingRemainders = KeepRoundingRemainders; data.Oversampling = Oversampling;
            data.ModulateColorGlyphs = ModulateColorGlyphs; data.OpenTypeFeatures = OpenTypeFeatures;
            data.Authored = new FontCache
            {
                Instance = instance,
                FixedSize = Authored?.FixedSize ?? 0,
                ScaleMode = Authored?.ScaleMode ?? FixedSizeScaleMode.Disable,
                Spacing = [parameters.Glyph, parameters.Space, parameters.Top, parameters.Bottom]
            };
            Font? owner = null;
            if (SourceOwner?.TryGetTarget(out var parent) == true) parent.TryGetNotificationOwner(out owner!);
            var file = FontFile.AdoptSource(data, notificationOwner: owner);
            _systemSourceReads.Add(file, file.AcquireSourceRead());
            var weak = new WeakReference<FontData>(this);
            file.Changed += _ => { if (weak.TryGetTarget(out var source)) source.SystemSourceChanged(); };
            return file;
        }
        catch { data.Dispose(); throw; }
    }
    private void SystemSourceChanged()
    {
        lock (_gate) { if (_disposed) return; RefreshSystemSources(); }
        if (SourceOwner?.TryGetTarget(out var owner) == true) owner.SystemSourceChanged(this);
    }
    private readonly Dictionary<FontFile, (FontData Data, long Generation)> _systemSourceReads = [];
    private readonly List<FontData> _retiredSystemReads = [];
    private long _systemGeneration;
    internal long SystemGeneration => Volatile.Read(ref _systemGeneration);
    internal bool SystemSourcesCurrent
    { get { lock (_gate) { foreach (var pair in _systemSourceReads) if (pair.Key.SourceGeneration != pair.Value.Generation) return false; return true; } } }
    internal bool RefreshSystemSources()
    {
        lock (_gate)
        {
            var changed = false;
            foreach (var pair in _systemSourceReads)
            {
                if (pair.Key.SourceGeneration == pair.Value.Generation) continue;
                var read = pair.Key.AcquireSourceRead(); _retiredSystemReads.Add(pair.Value.Data);
                _systemSourceReads[pair.Key] = read; changed = true;
            }
            if (changed) { Interlocked.Increment(ref _systemGeneration); _systemScalars.Clear(); _systemClusters.Clear(); }
            if (_readers == 0) ReleaseRetiredSystemReads();
            return changed;
        }
    }
    private void ReleaseRetiredSystemReads()
    {
        NativeCleanupDepth++;
        List<Exception>? errors = null;
        try
        {
            while (_retiredSystemReads.Count > 0)
            {
                var last = _retiredSystemReads.Count - 1; var data = _retiredSystemReads[last]; _retiredSystemReads.RemoveAt(last);
                try { data.ReleaseRead(); } catch (Exception error) { (errors ??= []).Add(error); }
            }
        }
        finally { NativeCleanupDepth--; }
        if (errors is not null) throw new AggregateException("Fallback read cleanup failed.", errors);
    }
    private readonly List<FontFile> _retiredSystemSources = [];
    private static void RetireSystemSource(FontFile file)
    {
        if (file.TryGetNotificationOwner(out var owner) && !ReferenceEquals(owner, file)) owner.QueueSourceRetirement(file);
        else
        {
            file.MarkSourceRetiring();
            ThreadPool.QueueUserWorkItem(static source => { try { source.Dispose(); } catch (Exception error) { OS.LogError(error.ToString()); } }, file, false);
        }
    }
    private void ReleaseRetiredSystemSources()
    { ReleaseRetiredSystemReads(); foreach (var source in _retiredSystemSources) RetireSystemSource(source); _retiredSystemSources.Clear(); }
    private void DrainSourceRetirements(Exception? failure = null)
    {
        if (SourceOwner?.TryGetTarget(out var file) == true && !Monitor.IsEntered(file.FontGate) && file.TryGetNotificationOwner(out var owner)) owner.DrainSourceRetirements(failure);
        else if (failure is not null) System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(failure).Throw();
    }
    internal void ClearSystemSources()
    {
        Interlocked.Increment(ref _systemGeneration);
        _systemScalars.Clear(); _systemClusters.Clear();
        foreach (var read in _systemSourceReads.Values) _retiredSystemReads.Add(read.Data);
        _systemSourceReads.Clear();
        foreach (var source in _systemSources.Values)
        {
            source.MarkSourceRetiring();
            if (_readers > 0) _retiredSystemSources.Add(source); else RetireSystemSource(source);
        }
        _systemSources.Clear(); if (_readers == 0) ReleaseRetiredSystemSources();
    }
}
