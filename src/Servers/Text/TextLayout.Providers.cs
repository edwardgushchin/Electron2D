using System.Text;

namespace Electron2D;

internal sealed partial class TextLayout
{
    private ProviderLayout? _providerLayout;
    private static readonly object ProviderLayoutsGate = new();
    private static readonly List<ProviderLayout> ProviderLayouts = [];
    internal static void RetireProviderLayouts(TextServer? owner)
    {
        ProviderLayout[] states;
        lock (ProviderLayoutsGate) states = ProviderLayouts.Where(state => ReferenceEquals(state.Owner, owner)).ToArray();
        List<Exception>? errors = null;
        foreach (var state in states) try { state.Release(); } catch (Exception error) { (errors ??= []).Add(error); }
        if (errors is not null) throw new AggregateException("Text provider cache retirement failed.", errors);
    }
    internal static void RetireProviderFont(Font font)
    {
        List<ProviderLayout>? states = null;
        lock (ProviderLayoutsGate)
            foreach (var state in ProviderLayouts) if (state.DependsOn(font)) (states ??= []).Add(state);
        if (states is null) return;
        List<Exception>? errors = null;
        foreach (var state in states) try { state.Release(); } catch (Exception error) { Node.CollectException(ref errors, error); }
        Node.ThrowCollected("Text provider source retirement failed.", errors);
    }
    private sealed class ProviderLayout
    {
        internal readonly TextServer Owner;
        private readonly object _gate = new();
        private int _users;
        private bool _releasing, _released;
        internal bool Retiring;
        internal ProviderLayout(TextServer owner)
        { Owner = owner; lock (ProviderLayoutsGate) ProviderLayouts.Add(this); }
        internal Use BeginUse()
        { lock (_gate) { if (Retiring) throw new InvalidOperationException("The provider layout is retiring."); _users++; return new(this); } }
        internal readonly struct Use(ProviderLayout state) : IDisposable
        {
            internal void Dispose(Exception? failure)
            { try { Dispose(); } catch (Exception cleanup) { if (failure is not null) throw new AggregateException(failure, cleanup); throw; } }
            public void Dispose()
            { bool release; lock (state._gate) { state._users--; release = state._users == 0 && state.Retiring; } if (release) state.ReleaseCore(); }
        }
        internal RID Full;
        internal int ScalarOffset;
        internal bool RebaseSpanIndexes;
        internal int TrimLine = -1;
        internal float TrimWidth;
        internal TextOverrunFlags TrimFlags;
        internal readonly List<RID> Lines = [];
        internal readonly List<TextObjectKey> Objects = [];
        internal readonly Font[] SpanFonts = new Font[1];
        internal TextLayoutKey Key;
        internal TextLayoutOptions Options;
        private readonly List<Font> _dependencies = [];
        internal bool DependsOn(Font font)
        { lock (_gate) { foreach (var dependency in _dependencies) if (ReferenceEquals(dependency, font)) return true; return false; } }
        internal void CaptureDependencies(Font.ReadScope root, List<Font.ReadScope> styles)
        { lock (_gate) { _dependencies.Clear(); root.CaptureDependencies(_dependencies); foreach (var style in styles) style.CaptureDependencies(_dependencies); } }
        internal void CopyDependenciesTo(ProviderLayout target)
        { lock (_gate) lock (target._gate) target._dependencies.AddRange(_dependencies); }
        internal Font? Font;
        internal long FontGeneration = -1;
        internal readonly List<long> StyleGenerations = [];
        internal readonly List<TextLayoutStyleSpan> AuthoredStyles = [];
        internal readonly List<TextLayoutInlineObject> AuthoredObjects = [];
        internal readonly List<TextBIDIRange> Contexts = [];
        internal readonly List<float> Tabs = [];
        internal bool ContentMatches(TextLayoutOptions options) => Matches(options.Styles, AuthoredStyles) && Matches(options.Objects, AuthoredObjects) && Matches(options.BIDIOverride, Contexts) && Matches(options.TabStops, Tabs);
        internal void CaptureContent(TextLayoutOptions options)
        { Capture(options.Styles, AuthoredStyles); Capture(options.Objects, AuthoredObjects); Capture(options.BIDIOverride, Contexts); Capture(options.TabStops, Tabs); }
        private static bool Matches<T>(IReadOnlyList<T>? values, List<T> captured) where T : IEquatable<T>
        { if ((values?.Count ?? 0) != captured.Count) return false; for (var i = 0; i < captured.Count; i++) if (!captured[i].Equals(values![i])) return false; return true; }
        private static void Capture<T>(IReadOnlyList<T>? values, List<T> captured)
        { captured.Clear(); if (values is not null) for (var i = 0; i < values.Count; i++) captured.Add(values[i]); }
        internal bool Authored;
        internal void ReleaseLines()
        {
            List<Exception>? errors = null;
            for (var i = Lines.Count - 1; i >= 0; i--)
            {
                var rid = Lines[i]; if (rid == Full) { Lines.RemoveAt(i); continue; }
                try { Owner.FreeRID(rid); Lines.RemoveAt(i); }
                catch (Exception error) { if (!Owner.IsOwnedRID(rid)) Lines.RemoveAt(i); (errors ??= []).Add(error); }
            }
            if (errors is not null) throw new AggregateException("Text provider line retirement failed.", errors);
        }
        internal void Release()
        { lock (_gate) { Retiring = true; if (_users != 0) return; } ReleaseCore(); }
        private void ReleaseCore()
        {
            lock (_gate) { if (_users != 0 || _releasing || _released) return; _releasing = true; }
            try
            {
                using var consumer = TextServerManager.BeginConsumerRead(Owner);
                List<Exception>? errors = null;
                try { ReleaseLines(); } catch (Exception error) { (errors ??= []).Add(error); }
                try { if (Full != default) { Owner.FreeRID(Full); Full = default; } }
                catch (Exception error) { if (!Owner.IsOwnedRID(Full)) Full = default; (errors ??= []).Add(error); }
                Font = null; SpanFonts[0] = null!; Authored = false; Options = default; Key = default; Objects.Clear(); StyleGenerations.Clear(); AuthoredStyles.Clear(); AuthoredObjects.Clear(); Contexts.Clear(); Tabs.Clear();
                lock (_gate) _dependencies.Clear();
                if (Full == default && Lines.Count == 0)
                {
                    lock (ProviderLayoutsGate) ProviderLayouts.Remove(this);
                    lock (_gate) _released = true;
                }
                if (errors is not null) throw new AggregateException("Text provider layout retirement failed.", errors);
            }
            finally { lock (_gate) _releasing = false; }
        }
    }
    private void ReleaseProviderLayout()
    {
        if (_providerLayout is not { } state) return;
        _providerLayout = null; state.Release();
    }
    private void CopyProviderFormattingTo(TextLayout target)
    {
        if (ReferenceEquals(target, this)) return;
        if (_providerLayout is not { } source) { target.ReleaseProviderLayout(); return; }
        using var consumer = TextServerManager.BeginConsumerRead(source.Owner); using var creation = TextServerManager.BeginBufferCreation(source.Owner); var use = source.BeginUse(); Exception? useFailure = null;
        try
        {
            var copy = new ProviderLayout(source.Owner);
            try
            {
                copy.Full = source.Owner.ShapedTextDuplicate(source.Full);
                foreach (var line in source.Lines) copy.Lines.Add(line == source.Full ? copy.Full : source.Owner.ShapedTextDuplicate(line));
                copy.ScalarOffset = source.ScalarOffset; copy.RebaseSpanIndexes = source.RebaseSpanIndexes;
                copy.TrimLine = source.TrimLine; copy.TrimWidth = source.TrimWidth; copy.TrimFlags = source.TrimFlags;
                source.CopyDependenciesTo(copy);
                copy.Objects.AddRange(source.Objects); copy.StyleGenerations.AddRange(source.StyleGenerations);
                copy.Key = target.Key; copy.Options = target._options; copy.Font = target._font; copy.FontGeneration = target._font.GetContentGeneration();
                copy.CaptureContent(target._options); copy.Authored = source.Authored;
                target.ReleaseProviderLayout(); target._providerLayout = copy;
            }
            catch (Exception failure)
            { try { copy.Release(); } catch (Exception cleanup) { throw new AggregateException(failure, cleanup); } throw; }

        }
        catch (Exception scopeError) { useFailure = scopeError; throw; }
        finally { use.Dispose(useFailure); }
    }
    internal void PrepareProviderContext(Font font, TextLayoutKey key, TextLayoutOptions options)
    {
        _font = font; _options = options; Key = key; _caretsReady = _serverCharactersReady = false;
        _glyphs.Clear(); _rawGlyphs.Clear(); _lines.Clear(); Size = Vector2.Zero;
        Decode(key.Text); CharacterCount = _count; PrepareRichStyles(); PrepareCustomPunctuation(); if (options.ServerBuffers) PrepareStyleBreaks();
    }
    private bool CopyProviderSliceTo(TextLayout target, int from, int to, int displayLine = -1)
    {
        if (_providerLayout is not { } source) return false;
        using var consumer = TextServerManager.BeginConsumerRead(source.Owner); using var creation = TextServerManager.BeginBufferCreation(source.Owner); var use = source.BeginUse(); Exception? useFailure = null;
        try
        {
            var copy = new ProviderLayout(source.Owner) { ScalarOffset = checked(source.ScalarOffset + from), RebaseSpanIndexes = true };
            try
            {
                copy.Full = displayLine < 0 ? source.Owner.ShapedTextSubstr(source.Full, copy.ScalarOffset, to - from) : source.Owner.ShapedTextDuplicate(source.Lines[displayLine]);
                copy.Lines.Add(copy.Full); copy.StyleGenerations.AddRange(source.StyleGenerations);
                if (target._options.Objects is { } objects) foreach (var item in objects)
                    {
                        while (copy.Objects.Count <= item.Index) copy.Objects.Add(new());
                        copy.Objects[item.Index] = item.Key ?? source.Objects[item.Index];
                    }
                source.CopyDependenciesTo(copy);
                if (displayLine >= 0 && source.TrimLine == displayLine)
                { copy.TrimLine = 0; copy.TrimWidth = source.TrimWidth; copy.TrimFlags = source.TrimFlags; }
                copy.Key = target.Key; copy.Options = target._options; copy.Font = target._font; copy.FontGeneration = target._font.GetContentGeneration(); copy.CaptureContent(target._options); copy.Authored = true;
                target._glyphs.Clear(); target._rawGlyphs.Clear(); target._lines.Clear(); target._caretsReady = target._serverCharactersReady = false;
                target.CopyProviderLine(copy, copy.Full, 0, to - from, true);
                target._rawGlyphs.AddRange(target._glyphs); target.NaturalWidth = target.UnwrappedWidth = MathF.Ceiling(target._lines[0].Width); target.PositionLines(); target.PrepareServerWrapping();
                target.ReleaseProviderLayout(); target._providerLayout = copy; return true;
            }
            catch (Exception failure)
            { try { copy.Release(); } catch (Exception cleanup) { throw new AggregateException(failure, cleanup); } throw; }

        }
        catch (Exception scopeError) { useFailure = scopeError; throw; }
        finally { use.Dispose(useFailure); }
    }
    private void BuildProviderLayout(TextServer provider, Font.ReadScope rootRead)
    {
        if (_providerLayout is { } previous && (previous.Retiring || !ReferenceEquals(previous.Owner, provider))) ReleaseProviderLayout();
        if (_options.RetainedShape is { _providerLayout: { } retained } retainedLayout && ReferenceEquals(retained.Owner, provider))
            retainedLayout.CopyProviderFormattingTo(this);
        var state = _providerLayout ??= new(provider);
        using var consumer = TextServerManager.BeginConsumerRead(provider);
        using var creation = TextServerManager.BeginBufferCreation(provider);
        var stateUse = state.BeginUse(); Exception? buildingFailure = null;
        state.CaptureDependencies(rootRead, _richReads);
        try
        {
            state.ReleaseLines();
            state.TrimLine = -1;
            var generation = _font.GetContentGeneration();
            var stylesCurrent = state.StyleGenerations.Count == _richReads.Count;
            for (var i = 0; stylesCurrent && i < _richReads.Count; i++) stylesCurrent = state.StyleGenerations[i] == _richReads[i].Generation;
            if (!stylesCurrent || !state.ContentMatches(_options) || !state.Authored || state.Key != Key || state.Options != _options || !ReferenceEquals(state.Font, _font) || state.FontGeneration != generation)
            {
                state.Authored = false;
                if (state.Full == default) state.Full = provider.CreateShapedText(Key.Direction == TextDirection.Inherited ? TextDirection.Auto : Key.Direction, Key.Orientation);
                else provider.ShapedTextClear(state.Full);
                state.ScalarOffset = 0; state.RebaseSpanIndexes = false;
                provider.ShapedTextSetDirection(state.Full, Key.Direction == TextDirection.Inherited ? TextDirection.Auto : Key.Direction);
                provider.ShapedTextSetOrientation(state.Full, Key.Orientation);
                provider.ShapedTextSetPreserveControl(state.Full, _options.PreserveControl);
                provider.ShapedTextSetPreserveInvalid(state.Full, _options.PreserveInvalid);
                provider.ShapedTextSetCustomPunctuation(state.Full, _options.CustomPunctuation);
                provider.ShapedTextSetSpacing(state.Full, TextSpacingType.Glyph, _options.BufferGlyphSpacing);
                provider.ShapedTextSetSpacing(state.Full, TextSpacingType.Space, _options.BufferSpaceSpacing);
                provider.ShapedTextSetSpacing(state.Full, TextSpacingType.Top, _options.BufferTopSpacing);
                provider.ShapedTextSetSpacing(state.Full, TextSpacingType.Bottom, _options.BufferBottomSpacing);
                var symbol = (_options.Ellipsis ?? "…").EnumerateRunes();
                provider.ShapedTextSetCustomEllipsis(state.Full, symbol.MoveNext() ? symbol.Current.Value : 0x2026);
                AuthorProviderContent(state);
                if (_options.BIDIOverride is { Count: > 0 } bidi)
                {
                    var ranges = new TextBIDIRange[bidi.Count]; for (var i = 0; i < ranges.Length; i++) ranges[i] = bidi[i];
                    provider.ShapedTextSetBIDIOverride(state.Full, ranges);
                }
                if (_options.TabStops is { Length: > 0 } tabs) provider.ShapedTextTabAlign(state.Full, tabs);
                state.Key = Key; state.Options = _options; state.Font = _font; state.FontGeneration = generation; state.StyleGenerations.Clear(); foreach (var styleRead in _richReads) state.StyleGenerations.Add(styleRead.Generation); state.CaptureContent(_options); state.Authored = true;
            }
            // An uncached request must still run the producer, even with unchanged authored input.
            if (TextBuffers.TryResolve(state.Full, out var native) && native.Owner is TextLine line) line.InvalidateConsumerShape();
            if (!provider.ShapedTextShape(state.Full)) throw new InvalidOperationException("The text implementation could not shape the source.");
            UnwrappedWidth = CheckedProviderMetric(provider.ShapedTextGetWidth(state.Full));
            var breaks = Key.Multiline && _count > 0
                ? ProviderLineRanges(state)
                : new int[] { 0, _count };
            if ((breaks.Length & 1) != 0 || breaks.Length > 2097152) throw new InvalidOperationException("The text implementation returned invalid line ranges.");
            NaturalWidth = 0; DropcapLines = 0;
            for (var i = 0; i < breaks.Length; i += 2)
            {
                var start = breaks[i] - (Key.Multiline ? state.ScalarOffset : 0); var end = breaks[i + 1] - (Key.Multiline ? state.ScalarOffset : 0);
                if (start < 0 || end < start || end > _count || i > 0 && start < breaks[i - 1]) throw new InvalidOperationException("The text implementation returned an invalid scalar line range.");
                var rid = !Key.Multiline ? state.Full : provider.ShapedTextSubstr(state.Full, checked(start + state.ScalarOffset), end - start);
                state.Lines.Add(rid);
                var natural = CheckedProviderMetric(provider.ShapedTextGetWidth(rid)); NaturalWidth = Math.Max(NaturalWidth, natural);
                var cap = ProviderDropcapWidth();
                var available = Key.Width > 0 ? Math.Max(0, Key.Width - cap) : Key.Width;
                var last = i + 2 == breaks.Length; var paragraphEnd = end == _count || end > 0 && IsHardBreak(_scalars[end - 1]);
                if (Key.Alignment == HorizontalAlignment.Fill && available > 0 && (!last || !Key.Multiline) && (!paragraphEnd || !Key.Multiline))
                    provider.ShapedTextFitToWidth(rid, available, Key.Justification);
                if (_options.Overrun != 0 && available > 0 && (!Key.Multiline || Key.MaxLines > 0 && state.Lines.Count == Key.MaxLines))
                {
                    var flags = TextOverrunFlags.Trim;
                    if (_options.Overrun is 2 or 4 or 6) flags |= TextOverrunFlags.TrimWordOnly;
                    if (_options.Overrun >= 3) flags |= TextOverrunFlags.AddEllipsis;
                    if (_options.Overrun >= 5) flags |= TextOverrunFlags.EnforceEllipsis;
                    provider.ShapedTextOverrunTrimToWidth(rid, available, flags);
                    state.TrimLine = state.Lines.Count - 1; state.TrimWidth = available; state.TrimFlags = flags;
                }
                CopyProviderLine(state, rid, start, end, paragraphEnd);
            }
            PositionLines();
            if (_options.ServerBuffers) PrepareServerWrapping();
        }
        catch (Exception failure)
        {
            buildingFailure = failure;
            state.Authored = false;
            try { state.Release(); _providerLayout = null; }
            catch (Exception cleanup) { buildingFailure = new AggregateException(failure, cleanup); throw buildingFailure; }
            throw;
        }
        finally { state.SpanFonts[0] = null!; stateUse.Dispose(buildingFailure); }
    }
    private float FitProvider(float width, TextJustificationFlags flags)
    {
        var state = _providerLayout!;
        using var consumer = TextServerManager.BeginConsumerRead(state.Owner); var use = state.BeginUse(); Exception? useFailure = null;
        try
        {
            var result = CheckedProviderMetric(state.Owner.ShapedTextFitToWidth(state.Full, width, flags));
            RefreshProviderFormatting(state); return result;

        }
        catch (Exception scopeError) { useFailure = scopeError; throw; }
        finally { use.Dispose(useFailure); }
    }
    private void TrimProvider(float width, TextOverrunFlags flags, int scalar)
    {
        var state = _providerLayout!;
        using var consumer = TextServerManager.BeginConsumerRead(state.Owner); var use = state.BeginUse(); Exception? useFailure = null;
        try
        {
            state.Owner.ShapedTextSetCustomEllipsis(state.Full, scalar);
            state.Owner.ShapedTextOverrunTrimToWidth(state.Full, width, flags);
            state.TrimLine = 0; state.TrimWidth = width; state.TrimFlags = flags;
            RefreshProviderFormatting(state);

        }
        catch (Exception scopeError) { useFailure = scopeError; throw; }
        finally { use.Dispose(useFailure); }
    }
    private void RefreshProviderFormatting(ProviderLayout state)
    {
        state.ReleaseLines(); state.Lines.Add(state.Full);
        _glyphs.Clear(); _rawGlyphs.Clear(); _lines.Clear(); _caretsReady = _serverCharactersReady = false;
        CopyProviderLine(state, state.Full, 0, _count, true); _rawGlyphs.AddRange(_glyphs);
        NaturalWidth = UnwrappedWidth = MathF.Ceiling(_lines[0].Width); PositionLines();
        if (_options.ServerBuffers) PrepareServerWrapping();
    }
    private float ProviderDropcapWidth()
    {
        var cross = 0f; foreach (var line in _lines) cross += MathF.Ceiling(line.Height) + _options.LineSpacing;
        if (cross >= _options.DropcapHeight) return 0;
        DropcapLines++; return _options.DropcapWidth;
    }
    private int[] ProviderLineRanges(ProviderLayout state)
    {
        if (_options.DropcapHeight <= 0 || _options.DropcapWidth <= 0)
            return state.Owner.ShapedTextGetLineBreaksAdv(state.Full, [Key.Width], once: false, breakFlags: Key.Breaks);
        var height = CheckedProviderMetric(state.Owner.ShapedTextGetAscent(state.Full)) + CheckedProviderMetric(state.Owner.ShapedTextGetDescent(state.Full));
        var capLines = Math.Clamp((int)MathF.Ceiling(_options.DropcapHeight / Math.Max(1, height + _options.LineSpacing)), 1, 4095);
        var widths = new float[capLines + 1]; Array.Fill(widths, Math.Max(1, Key.Width - _options.DropcapWidth)); widths[^1] = Key.Width;
        return state.Owner.ShapedTextGetLineBreaksAdv(state.Full, widths, once: false, breakFlags: Key.Breaks);
    }
    private void AuthorProviderContent(ProviderLayout state)
    {
        var provider = state.Owner;
        for (var at = 0; at < _count;)
        {
            if (_richObjects[at] is { At: >= 0 } item)
            {
                while (state.Objects.Count < item.Index) state.Objects.Add(new());
                if (state.Objects.Count == item.Index) state.Objects.Add(item.Key ?? new());
                else if (item.Key is { } key) state.Objects[item.Index] = key;
                if (!provider.ShapedTextAddObject(state.Full, state.Objects[item.Index], item.Size, item.Alignment, item.Length, item.ExplicitBaseline ? item.Baseline : 0))
                    throw new InvalidOperationException("The text implementation rejected an inline object.");
                at += item.Length; continue;
            }
            var style = StyleAt(at); var end = at + 1;
            while (end < _count && _richObjects[end].At < 0 && StyleAt(end) == style) end++;
            state.SpanFonts[0] = style.Font;
            var fonts = style.Font is TextBufferFont group ? group.BorrowedFonts : state.SpanFonts;
            var features = style.Font is TextBufferFont authored ? authored.FeatureOverrides : style.Font.GetOpenTypeFeatures();
            var text = Key.Text[_utf16[at].._utf16[end]]; var language = style.Language ?? string.Empty;
            var added = style.Metadata is { } metadata ? metadata.AddText(provider, state.Full, text, fonts, style.Size, features, language)
                : provider.ShapedTextAddString(state.Full, text, fonts, style.Size, features, language);
            if (!added) throw new InvalidOperationException("The text implementation rejected a text span.");
            at = end;
        }
    }
    private static float CheckedProviderMetric(float value)
    { if (!float.IsFinite(value)) throw new InvalidOperationException("The text implementation returned invalid metrics."); return value; }
    private void CopyProviderLine(ProviderLayout state, RID rid, int start, int end, bool paragraphEnd)
    {
        var provider = state.Owner; var first = _glyphs.Count;
        var rtl = provider.ShapedTextGetInferredDirection(rid) == TextDirection.RTL;
        var trim = provider.ShapedTextGetTrimPos(rid); var ellipsis = provider.ShapedTextGetEllipsisPos(rid);
        using var visual = provider.ReadGlyphs(rid); using var ending = provider.ReadGlyphs(rid, true);
        if (rtl && ellipsis >= 0) for (var i = ending.Glyphs.Length - 1; i >= 0; i--) AddProviderGlyph(ending.Glyphs[i], true, state);
        for (var i = 0; i < visual.Glyphs.Length; i++)
        { if (trim >= 0 && (rtl ? i < trim : i >= trim)) continue; AddProviderGlyph(visual.Glyphs[i], false, state); }
        if (!rtl && ellipsis >= 0) foreach (var glyph in ending.Glyphs) AddProviderGlyph(glyph, true, state);
        var width = CheckedProviderMetric(provider.ShapedTextGetWidth(rid));
        var ascent = CheckedProviderMetric(provider.ShapedTextGetAscent(rid)); var descent = CheckedProviderMetric(provider.ShapedTextGetDescent(rid));
        var underlinePosition = provider.ShapedTextGetUnderlinePosition(rid); var underlineThickness = CheckedProviderMetric(provider.ShapedTextGetUnderlineThickness(rid));
        if (!float.IsFinite(underlinePosition)) throw new InvalidOperationException("The text implementation returned invalid underline geometry.");
        _lines.Add(new(start, end, first, _glyphs.Count - first, width, ascent, descent, 0, paragraphEnd, rtl ? 1 : 0,
            UnderlinePosition: underlinePosition, UnderlineThickness: underlineThickness, TrimPosition: trim, EllipsisPosition: ellipsis));
    }
    private void AddProviderGlyph(TextGlyph value, bool ellipsis, ProviderLayout state)
    {
        if (!ellipsis && state.ScalarOffset != 0) value = value with { Start = checked(value.Start - state.ScalarOffset), End = checked(value.End - state.ScalarOffset) };
        if (!value.Offset.IsFinite() || !float.IsFinite(value.Advance) || value.Repeat < 0 || value.Repeat > 1048576 || value.Count < 0 || value.Count > 1048576 || value.Index < 0 || value.SpanIndex < -1 ||
            !ellipsis && (value.Start < 0 || value.End < value.Start || value.End > _count) || value.FontRID != default && value.FontSize <= 0)
            throw new InvalidOperationException("The text implementation returned an invalid glyph record.");
        var start = ellipsis ? _count : value.Start; var end = ellipsis ? _count : value.End;
        var flags = value.Flags; var item = !ellipsis && start < _count ? _richObjects[start] : new(-1, default, default, -1);
        var objectIndex = flags.HasFlag(TextGraphemeFlags.EmbeddedObject) ? item.Index : -1;
        var missing = value.FontRID == default && objectIndex < 0 && (flags & (TextGraphemeFlags.Virtual | TextGraphemeFlags.Valid)) == 0;
        _glyphs.Add(new(null, (uint)value.Index, start, end, value.Advance, value.Offset, 0, flags.HasFlag(TextGraphemeFlags.Space), flags.HasFlag(TextGraphemeFlags.Tab),
            flags.HasFlag(TextGraphemeFlags.RTL), value.Repeat, flags.HasFlag(TextGraphemeFlags.Virtual), missing, flags.HasFlag(TextGraphemeFlags.Elongation),
            value.FontSize, objectIndex, SourceRID: value.FontRID, Ellipsis: ellipsis, ProviderFlags: flags, ProviderCount: value.Count, ProviderSpanIndex: state.RebaseSpanIndexes ? null : value.SpanIndex));
        if (!ellipsis) for (var i = start; i < end; i++) { _levels[i] = flags.HasFlag(TextGraphemeFlags.RTL) ? (sbyte)1 : (sbyte)0; if (i > start) _grapheme[i] = false; }
    }
    private void DrawProviderLines(ProviderLayout state, CanvasItem canvas, Vector2 baseline, Color color, int outline,
        float oversampling, int firstLine, int end, bool outlinePass, bool clipToWidth, float clipWidth, float clipStart, float clipLeft, float clipRight)
    {
        var vertical = Key.Orientation == TextOrientation.Vertical; var first = _lines[firstLine];
        var origin = baseline - (vertical ? new Vector2(first.CrossOffset + first.Ascent, 0) : new Vector2(0, first.CrossOffset + first.Ascent));
        for (var i = firstLine; i < end; i++)
        {
            var line = _lines[i]; var at = line.GlyphCount == 0 ? (vertical ? new Vector2(line.CrossOffset + line.Ascent, 0) : new Vector2(0, line.CrossOffset + line.Ascent))
                : _glyphs[line.GlyphStart].Position - _glyphs[line.GlyphStart].Offset;
            var align = vertical ? at.Y : at.X;
            var left = clipLeft < 0 ? clipLeft : clipLeft - align; var right = clipRight < 0 ? clipRight : clipRight - align;
            if (clipToWidth && clipWidth > 0) { left = Math.Max(left, clipStart - align); right = right < 0 ? clipStart + clipWidth - align : Math.Min(right, clipStart + clipWidth - align); }
            if (outlinePass) state.Owner.ShapedTextDrawOutline(state.Lines[i], canvas, origin + at, left, right, outline, color, oversampling);
            else state.Owner.ShapedTextDraw(state.Lines[i], canvas, origin + at, left, right, color, oversampling);
        }
    }
    private static void DrawProviderGlyph(ProviderLayout state, CanvasItem canvas, Glyph glyph, Vector2 position, Color color,
        int outline, float oversampling, bool outlinePass, Rect2? clip)
    {
        if (glyph.SourceRID == default) return;
        using var consumer = TextServerManager.BeginConsumerRead(state.Owner);
        var stateUse = state.BeginUse(); Exception? stateUseFailure = null;
        try
        {
            using var recordingClip = canvas.BeginTextRecordingClip(clip);
            if (outlinePass) state.Owner.FontDrawGlyphOutline(glyph.SourceRID, canvas, glyph.Size, outline, position, checked((int)glyph.Index), color, oversampling);
            else state.Owner.FontDrawGlyph(glyph.SourceRID, canvas, glyph.Size, position, checked((int)glyph.Index), color, oversampling);

        }
        catch (Exception scopeError) { stateUseFailure = scopeError; throw; }
        finally { stateUse.Dispose(stateUseFailure); }
    }

}
