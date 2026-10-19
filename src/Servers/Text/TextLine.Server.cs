namespace Electron2D;

public sealed partial class TextLine
{
    private readonly List<TextBufferFont> _serverFonts = [];
    internal Action? ServerChanged, ServerGuard;
    internal int ServerOffset, ServerOuterScalars;
    internal ReadOnlyMemory<char> ServerPrefix, ServerSuffix;
    internal string ServerFullText
    {
        get { Check(); var text = _contentDirty ? _text.ToString() : _source; return ServerPrefix.IsEmpty && ServerSuffix.IsEmpty ? text : string.Concat(ServerPrefix.Span, text.AsSpan(), ServerSuffix.Span); }
    }
    private TextDirection ServerSourceDirection()
    {
        var full = ServerFullText; var scalar = 0; var utf16 = 0; var from = 0; var to = full.Length;
        foreach (var rune in full.EnumerateRunes())
        {
            if (rune.Value is 10 or 13 or 0x2028 or 0x2029)
            {
                if (scalar < ServerOffset) from = utf16 + rune.Utf16SequenceLength;
                else { to = utf16; break; }
            }
            utf16 += rune.Utf16SequenceLength; scalar++;
        }
        var bidi = new TextBIDI(); bidi.Resolve(full.AsSpan(from, to - from), 2);
        return bidi.ParagraphLevel == 1 ? TextDirection.RTL : TextDirection.LTR;
    }
    internal long ServerCurrentRevision => _revision;
    internal void ServerAccess() { Check(); if (_busy) throw new InvalidOperationException("An active text buffer cannot reenter."); ServerGuard?.Invoke(); }
    internal int ServerSpanCountInRange(int from, int to)
    {
        Check(); var count = 0;
        foreach (var span in _spans) if (span.At < to && span.At + span.Length > from) count++;
        foreach (var item in _objects) if (item.At < to && item.At + item.Length > from) count++;
        return count;
    }
    internal (int At, int Length, TextObjectKey? Key, RichTextMetadata? Metadata) ServerSpan(int index, int from, int to)
    {
        Check(); if (index < 0) throw new ArgumentOutOfRangeException(nameof(index)); var si = 0; var oi = 0;
        while (si < _spans.Count || oi < _objects.Count)
        {
            (int At, int Length, TextObjectKey? Key, RichTextMetadata? Metadata) value;
            if (si < _spans.Count && (oi == _objects.Count || _spans[si].At < _objects[oi].At)) { var span = _spans[si++]; value = (span.At, span.Length, null, span.Metadata); }
            else { var item = _objects[oi++]; value = (item.At, item.Length, item.Key, null); }
            if (value.At < to && value.At + value.Length > from && index-- == 0) return value;
        }
        throw new ArgumentOutOfRangeException(nameof(index));
    }
    internal bool ServerReady { get { Check(); return !_dirty && !FontsChanged(); } }
    internal float ServerDisplayWidth => _display.LineCount == 0 ? 0 : _display.Lines[0].Width;
    internal TextLayout ServerUnpreparedLayout => _full;
    internal TextLayout ServerLayout { get { Ensure(); return _full; } }
    internal bool ServerAdd(string text, ReadOnlySpan<Font> fonts, int size, IReadOnlyDictionary<string, int>? features, string language, RichTextMetadata? metadata)
    {
        CheckMutation(); ArgumentNullException.ThrowIfNull(text); ArgumentNullException.ThrowIfNull(language); Font.ValidateSize(size);
        var group = new TextBufferFont(fonts, features);
        try
        {
            _serverFonts.EnsureCapacity(_serverFonts.Count + 1);
            var added = AddText(text, group, size, language, metadata);
            if (text.Length != 0) { _serverFonts.Add(group); group = null!; }
            if (text.Length != 0) ServerChanged?.Invoke(); return added;
        }
        finally { group?.Release(); }
    }
    internal void ServerCopyFonts(TextLine copy)
    {
        if (_serverFonts.Count == 0) return; var used = new HashSet<Font>(copy._fonts);
        foreach (var font in _serverFonts) if (used.Contains(font)) { font.Retain(); copy._serverFonts.Add(font); }
    }
    internal TextLine ServerSlice(int from, int to, bool completeObjectsOnly = false)
    {
        Check(); if (from < 0 || to < from || to > _count) throw new ArgumentOutOfRangeException(nameof(from));
        var copy = new TextLine { _direction = _direction, _orientation = _orientation, _preserveControl = _preserveControl, _preserveInvalid = _preserveInvalid, _overrun = TextOverrunBehavior.NoTrimming, ServerOffset = ServerOffset + from, ServerOuterScalars = ServerOuterScalars + from + _count - to, ServerPrefix = default, ServerSuffix = default };
        try
        {
            var text = ServerFullText; var prefixLength = ServerPrefix.Length;
            var fromUTF16 = prefixLength + ServerUTF16(from); var toUTF16 = prefixLength + ServerUTF16(to);
            copy.ServerPrefix = text.AsMemory(0, fromUTF16); copy.ServerSuffix = text.AsMemory(toUTF16);
            var spanIndex = 0; var objectIndex = 0; var at = from;
            while (at < to)
            {
                while (spanIndex < _spans.Count && _spans[spanIndex].At + _spans[spanIndex].Length <= at) spanIndex++;
                while (objectIndex < _objects.Count && _objects[objectIndex].At + _objects[objectIndex].Length <= at) objectIndex++;
                if (objectIndex < _objects.Count && _objects[objectIndex].At <= at)
                {
                    var item = _objects[objectIndex++]; var end = Math.Min(to, item.At + item.Length);
                    if (completeObjectsOnly && (at != item.At || end != item.At + item.Length)) copy.AddText(ServerSliceText(at, end), copy._neutral, 16, string.Empty, null);
                    else copy.AddObject(item.Key, item.Size, item.Alignment, end - at, item.Baseline);
                    at = end;
                }
                else
                {
                    var span = _spans[spanIndex++]; var end = Math.Min(to, span.At + span.Length);
                    copy.AddText(ServerSliceText(at, end), ReferenceEquals(span.Font, _neutral) ? copy._neutral : span.Font, span.Size, span.Language, span.Metadata); at = end;
                }
            }
            ServerCopyFonts(copy); copy._tabs = (float[])_tabs.Clone();
            var ranges = new List<TextBIDIRange>(); foreach (var r in _bidi) if (r.Start < to && r.End > from) ranges.Add(new(Math.Max(r.Start, from) - from, Math.Min(r.End, to) - from, r.Direction));
            copy._bidi = ranges.ToArray(); return copy;
        }
        catch { copy.Dispose(); throw; }
    }
    private int ServerUTF16(int scalar)
    {
        if (!_contentDirty) return _full.ServerScalarUTF16(scalar);
        var position = 0; var index = 0;
        foreach (var rune in _text.ToString().EnumerateRunes()) { if (index++ == scalar) break; position += rune.Utf16SequenceLength; }
        return position;
    }
    internal string ServerSliceText(int from, int to)
    {
        Check(); var text = _contentDirty ? _text.ToString() : _source;
        return text[ServerUTF16(from)..ServerUTF16(to)];
    }
    private void ReleaseServerFonts() { foreach (var font in _serverFonts) font.Release(); _serverFonts.Clear(); }
    internal void ServerDraw(CanvasItem canvas, Vector2 baseline, float clipL, float clipR, int outline, Color color, float oversampling, bool outlinePass)
    {
        Check(); Font.ValidateDraw(canvas, baseline, color, oversampling);
        if (!float.IsFinite(clipL) || !float.IsFinite(clipR)) throw new ArgumentOutOfRangeException(nameof(clipL));
        Ensure(); _busy = true;
        try
        {
            BeginPreparedReads();
            _display.Draw(canvas, baseline, color, outline, oversampling, outlinePass: outlinePass, advanceClipLeft: clipL, advanceClipRight: clipR);
        }
        finally { try { EndReads(); } finally { _busy = false; } }
    }
    internal Vector2i ServerObjectRange(TextObjectKey key)
    {
        Check(); ArgumentNullException.ThrowIfNull(key);
        foreach (var item in _objects) if (ReferenceEquals(item.Key, key)) return new(item.At, item.At + item.Length);
        return new(-1, -1);
    }
}

// A span owns this wrapper; native faces and caller fonts remain borrowed and generation-tracked.
internal sealed class TextBufferFont : Font
{
    private readonly Font[] _fonts;
    private readonly NativeFontFeature[] _features;
    private readonly Dictionary<FontData, (NativeFontFeature[] Defaults, NativeFontFeature[] Values)> _compiled = [];
    private int _references = 1;
    private long _featureGeneration = -1;
    internal TextBufferFont(ReadOnlySpan<Font> fonts, IReadOnlyDictionary<string, int>? features)
    {
        if (fonts.Length == 0 || fonts.Length > 64) throw new ArgumentException("A text span requires one to sixty-four fonts.", nameof(fonts));
        foreach (var font in fonts) { ArgumentNullException.ThrowIfNull(font); if (font.IsDisposed) throw new ObjectDisposedException(nameof(fonts)); }
        if (features is { Count: > 4096 }) throw new ArgumentException("Too many OpenType features.", nameof(features));
        _features = features is null ? [] : features.Select(pair => new NativeFontFeature(OpenTypeFeatureTags.Resolve(pair.Key), unchecked((uint)pair.Value))).ToArray();
        _fonts = fonts.ToArray();
    }
    internal override FontData? PrimaryData => _fonts[0].PrimaryData;
    internal override Font?[] EffectiveFallbacks => _fonts;
    internal override NativeFontFeature[]? GetShapingFeatures(FontData source)
    {
        var defaults = source.OpenTypeFeatures;
        foreach (var font in _fonts) if (font.GetSources().Contains(source)) { defaults = font.GetShapingFeatures(source) ?? source.OpenTypeFeatures; break; }
        if (_features.Length == 0) return defaults;
        var generation = GetContentGeneration(); if (generation != _featureGeneration) { _compiled.Clear(); _featureGeneration = generation; }
        if (_compiled.TryGetValue(source, out var cached) && ReferenceEquals(cached.Defaults, defaults)) return cached.Values;
        var merged = new Dictionary<uint, NativeFontFeature>(); foreach (var item in defaults) merged[item.Tag] = item; foreach (var item in _features) merged[item.Tag] = item;
        var result = merged.Values.ToArray(); _compiled[source] = (defaults, result); return result;
    }
    internal override int GetShapingSpacing(FontData? source, TextSpacingType spacing)
    {
        if (source is not null) foreach (var font in _fonts) if (font.GetSources().Contains(source)) return font.GetSpacing(spacing);
        return _fonts[0].GetSpacing(spacing);
    }
    private (float Ascent, float Descent, float Position, float Thickness) SourceMetrics(FontData? selected, int size)
    {
        var sources = GetSources(); var last = selected is null ? sources.Count - 1 : sources.IndexOf(selected);
        var includeSelected = selected is not null && last < 0; if (includeSelected) last = sources.Count - 1;
        float ascent = 0, descent = 0, position = 0, thickness = 0;
        for (var i = 0; i <= last + (includeSelected ? 1 : 0); i++)
        {
            var source = i < sources.Count ? sources[i] : selected!; var metric = source.GetMetrics(size);
            ascent = Math.Max(ascent, metric.Ascent + GetShapingSpacing(source, TextSpacingType.Top));
            descent = Math.Max(descent, metric.Descent + GetShapingSpacing(source, TextSpacingType.Bottom));
            position = Math.Max(position, metric.UnderlinePosition); thickness = Math.Max(thickness, metric.UnderlineThickness);
        }
        return (ascent, descent, position, thickness);
    }
    internal override (float Ascent, float Descent) GetShapingMetrics(FontData? source, int size)
    { var result = SourceMetrics(source, size); return (result.Ascent, result.Descent); }
    internal override float GetShapingUnderline(FontData? source, int size, bool thickness)
    { var result = SourceMetrics(source, size); return thickness ? result.Thickness : result.Position; }
    public override int GetSpacing(TextSpacingType spacing) => _fonts[0].GetSpacing(spacing);
    internal void Retain() => _references++;
    internal void Release() { if (--_references == 0) Dispose(); }
}
