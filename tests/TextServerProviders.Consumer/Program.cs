using Electron2D;

if (args.FirstOrDefault() == "opaque-render") OpaqueBuffers.Render(args[1], args[2]);
else if (args.FirstOrDefault() == "retirement-errors-render") ProviderRendering.Run(args[1], args[2], retireSource: true, failDuringRetirement: true);
else if (args.FirstOrDefault() == "native-retirement-render") ProviderRendering.Run(args[1], args[2], retireSource: true);
else if (args.FirstOrDefault() == "profiles") ConcreteProfiles.Run();
else if (args.FirstOrDefault() == "profiles-render") ConcreteRendering.Run(args[1], args[2]);
else if (args.FirstOrDefault() == "render") ProviderRendering.Run(args[1], args[2]);
else if (args.FirstOrDefault() == "lifetime") Lifetime();
else if (args.FirstOrDefault() == "opaque") OpaqueBuffers.Run();
else if (args.FirstOrDefault() == "consumers") ProviderConsumers.Run();
else Layout();

static void Check(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
static void Reject<T>(Action operation) where T : Exception
{ try { operation(); } catch (T) { return; } throw new InvalidOperationException("Expected " + typeof(T).Name); }
static void Lifetime()
{
    var builtin = TextServerManager.GetPrimaryInterface();
    using (var errors = new OpaqueProbe())
    {
        var rid = errors.CreateFont(); var projection = errors.FontGetResource(rid);
        var notifications = 0;
        Action<Resource> changed = _ => { notifications++; throw new ApplicationException("Controlled font notification failure."); };
        projection.Changed += changed;
        try
        {
            try { errors.FontSetSpacing(rid, TextSpacingType.Top, 1); } catch (ApplicationException) { }
            Check(notifications == 1, "Successful opaque font authoring publishes its resource change.");
            errors.FailFontAuthoring = true;
            Exception? observed = null;
            try { errors.FontSetEmbolden(rid, 1); } catch (Exception error) { observed = error; }
            Check(observed is AggregateException combined && combined.Flatten().InnerExceptions.Any(failure => failure is InvalidDataException) && combined.Flatten().InnerExceptions.Any(failure => failure is ApplicationException), "Both the implementation failure and its notification failure survive: " + observed);
        }
        finally { projection.Changed -= changed; errors.FailFontAuthoring = false; errors.FreeRID(rid); }
    }
    Reject<ArgumentException>(() => new TextCaretInfo(new(float.NaN, 0, 1, 12), TextDirection.Auto, default, TextDirection.Auto));
    Reject<ArgumentOutOfRangeException>(() => new TextCaretInfo(default, TextDirection.Inherited, default, TextDirection.Auto));
    Check(new TextCaretInfo(new(3, -9, -6, 12), TextDirection.RTL, default, TextDirection.Auto).LeadingRect.Size.X == -6, "External caret construction preserves signed geometry.");
    using (var protocols = new Probe())
    {
        foreach (var feature in Enum.GetValues<TextServer.Feature>()) Check(protocols.HasFeature(feature), "Native text capability is advertised.");
        Reject<ArgumentOutOfRangeException>(() => protocols.HasFeature((TextServer.Feature)0));
        var contexts = protocols.ParseStructuredText(StructuredTextParser.URI, [], "a/🙂.b");
        Check(contexts.SequenceEqual(new TextBIDIRange[] { new(0, 1, TextDirection.Auto), new(1, 2, TextDirection.LTR), new(2, 3, TextDirection.Auto), new(3, 4, TextDirection.LTR), new(4, 5, TextDirection.Auto) }), "Structured contexts use scalar positions around supplementary characters.");
        Check(protocols.ParseStructuredText(StructuredTextParser.List, ["🙂"], "a🙂b🙂c").Length == 5, "Typed list delimiters retain independent contexts.");
        protocols.BadParserRange = true;
        Reject<InvalidOperationException>(() => protocols.ParseStructuredText(StructuredTextParser.Default, [], "a"));
    }
    using (var provider = new Probe())
    {
        var ownedFont = provider.CreateFont(); var ownedBuffer = provider.CreateShapedText();
        try
        {
            var widths = provider.WidthCalls; Reject<ArgumentException>(() => provider.ShapedTextGetWidth(ownedFont));
            Check(provider.WidthCalls == widths, "Native role validation runs before an implementation callback.");
            var called = false; provider.DuringNativeRead = _ => called = true;
            try { Reject<ArgumentException>(() => provider.FontGetAscent(ownedBuffer, 18)); Check(!called, "A buffer cannot reach a font callback."); }
            finally { provider.DuringNativeRead = null; }
            Reject<InvalidOperationException>(() => provider.ShapedTextGetWidth(default));
        }
        finally { provider.FreeRID(ownedBuffer); provider.FreeRID(ownedFont); }
        TextServerManager.AddInterface(provider);
        Reject<InvalidOperationException>(() => provider.Dispose());
        TextServerManager.SetPrimaryInterface(provider);
        Reject<InvalidOperationException>(() => TextServerManager.RemoveInterface(provider));
        TextServerManager.SetPrimaryInterface(builtin); TextServerManager.RemoveInterface(provider);
        for (var i = 0; i < 128; i++) _ = provider.GetName();
        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < 128; i++) Check(provider.GetName() == "Probe", "Protected callback dispatch.");
        Check(GC.GetAllocatedBytesForCurrentThread() == before, "Prepared callback admission has no managed allocation.");
    }
    using (var opaque = new OpaqueProbe())
    {
        var font = opaque.CreateFont(); var shaped = opaque.CreateShapedText();
        Check(font != shaped && opaque.Has(font) && opaque.Has(shaped), "Opaque font and buffer identities have separate owned roles.");
        Reject<InvalidOperationException>(() => opaque.Dispose());
        Reject<ArgumentException>(() => opaque.FontGetAscent(shaped, 18));
        Reject<ArgumentException>(() => opaque.ShapedTextGetWidth(font));
        Reject<InvalidOperationException>(() => Task.Run(() => opaque.ShapedTextGetWidth(shaped)).GetAwaiter().GetResult());
        Check(Task.Run(() => opaque.FontGetAscent(font, 18)).GetAwaiter().GetResult() == 9, "Font queries retain coordinated cross-thread access.");
        opaque.ReadProbe = () => Reject<InvalidOperationException>(() => opaque.FreeRID(shaped));
        Check(opaque.ShapedTextGetWidth(shaped) == 13 && opaque.Has(shaped), "A callback cannot retire its currently admitted opaque buffer.");
        opaque.ReadProbe = null;
        var projection = opaque.FontGetResource(font);
        Check(ReferenceEquals(projection, opaque.FontGetResource(font)) && projection.GetRID() == font, "Opaque font projection retains its creating source and identity.");
        Check(projection.GetFontName() == "Opaque" && projection.GetAscent(18) == 9 && projection.GetHeight(18) == 12, "Opaque font metadata and metrics use callbacks without a native face.");
        Check(projection.HasChar('A') && projection.GetCharSize('A', 18) == new Vector2(6, 12), "Opaque font coverage and advance reach the typed Font API.");
        using (var duplicate = (Font)projection.Duplicate())
            Check(duplicate.GetRID() != font && duplicate.GetFontName() == "Opaque", "Opaque duplication creates an independently owned font source.");
        using (var variation = projection.FindVariation(new() { [0x77676874] = 700 }, spacingTop: 2))
        {
            opaque.DuringFontAuthoring = () => { var metadata = Task.Run(() => variation.VariationEmbolden); Check(metadata.Wait(TimeSpan.FromSeconds(3)), "Variant authoring callbacks hold no resource monitor."); };
            RID varied;
            try { varied = variation.GetRID(); } finally { opaque.DuringFontAuthoring = null; }
            Check(varied != font && ReferenceEquals(opaque.FontGetResource(varied), variation) && variation.GetRIDs()[0] == varied, "Typed variation owns its independent opaque source and projection.");
            Check(opaque.FontGetVariationCoordinates(varied)[0x77676874] == 700 && variation.GetAscent(18) == 11, "Variation coordinates and spacing reach creator callbacks.");
            Reject<InvalidOperationException>(() => opaque.FreeRID(varied));
            opaque.FontSetName(varied, "Cache edit");
            Check(variation.GetFontName() == "Cache edit" && variation.GetRID() == varied && projection.GetFontName() == "Opaque", "Live variation cache edits preserve source identity and do not edit the base.");
            variation.VariationEmbolden = 1;
            Check(!opaque.Has(varied), "A retired variation configuration releases its old opaque source.");
            varied = variation.GetRID(); Check(variation.GetFontName() == "Opaque", "A new configured variation copies the current base independently.");
            var changedDuringRead = false;
            opaque.DuringAscent = source =>
            {
                if (source != varied || changedDuringRead) return; changedDuringRead = true;
                Task.Run(() => variation.VariationEmbolden = 2).GetAwaiter().GetResult();
                Check(opaque.FontGetName(varied) == "Opaque", "A retained old source remains readable until its current callback finishes.");
            };
            try { Check(variation.GetAscent(18) == 11 && changedDuringRead, "Opaque variant replacement retries a coherent read after concurrent authoring."); }
            finally { opaque.DuringAscent = null; }
            Check(!opaque.Has(varied), "An old source retires on its last reader even when another thread requested retirement.");
            for (var i = 0; i < 128; i++) _ = variation.GetAscent(18);
            var variantBefore = GC.GetAllocatedBytesForCurrentThread();
            for (var i = 0; i < 128; i++) Check(variation.GetAscent(18) == 11, "Prepared opaque variant ascent.");
            Check(GC.GetAllocatedBytesForCurrentThread() == variantBefore, "Prepared opaque variant metadata has no managed allocation.");
        }
        Check(opaque.FontCount == 1, "Typed variation disposal retires its creator-owned source.");
        using (var notified = projection.FindVariation())
        {
            var beforeRID = notified.GetRID(); opaque.ChangeNameDirect(font, "Notified");
            Check(notified.GetRID() != beforeRID && !opaque.Has(beforeRID) && notified.GetFontName() == "Notified", "Explicit font changes invalidate dependent opaque variation copies.");
            opaque.ChangeNameDirect(font, "Opaque");
        }
        var notifiedDuringRead = false; var ascentCalls = 0;
        opaque.DuringAscent = source => { if (source != font) return; ascentCalls++; if (!notifiedDuringRead) { notifiedDuringRead = true; opaque.ChangeNameDirect(font, "During read"); } };
        try { Check(projection.GetAscent(18) == 9 && ascentCalls >= 2, "An explicit change spanning a callback query retries the coherent source generation."); }
        finally { opaque.DuringAscent = null; opaque.ChangeNameDirect(font, "Opaque"); }
        using (var failedVariation = projection.FindVariation())
        {
            opaque.FailFontAuthoring = true;
            Reject<InvalidDataException>(() => failedVariation.GetRID());
            Check(opaque.FontCount == 1, "A failing variation callback rolls back its partial source copy.");
            opaque.FailFontAuthoring = false; Check(failedVariation.GetRID() != default, "A failed lazy source can be prepared again.");
        }
        for (var i = 0; i < 128; i++) _ = projection.GetAscent(18);
        var metadataBefore = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < 128; i++) Check(projection.GetAscent(18) == 9, "Prepared opaque ascent.");
        Check(GC.GetAllocatedBytesForCurrentThread() == metadataBefore, "Prepared opaque font metadata has no managed allocation.");
        opaque.FreeRID(shaped); opaque.FreeRID(font);
        Check(projection.IsDisposed, "Freeing an opaque font retires its borrowed projection.");
        Check(!opaque.Has(shaped) && !opaque.Has(font), "Retirement removes both opaque identities.");
    }
    using (var retiring = new PreDeleteProbe())
    {
        var beforeCount = TextServerManager.GetInterfaceCount();
        retiring.BeforeDelete = () =>
        {
            Reject<ObjectDisposedException>(() => retiring.CreateFont());
            Reject<ObjectDisposedException>(() => retiring.CreateShapedText());
            Reject<ObjectDisposedException>(() => retiring.AllocateDuringRetirement());
            Reject<ObjectDisposedException>(() => TextServerManager.AddInterface(retiring));
            Reject<ObjectDisposedException>(() => TextServerManager.SetPrimaryInterface(retiring));
        };
        retiring.Dispose();
        Check(retiring.IsDisposed && TextServerManager.GetInterfaceCount() == beforeCount && ReferenceEquals(TextServerManager.GetPrimaryInterface(), builtin), "Pre-delete callbacks cannot reopen provider admission or retain a retiring implementation.");
    }
    using (var cleanup = new CleanupProbe())
    {
        var owned = cleanup.CreateFont(); TextServerManager.AddInterface(cleanup);
        cleanup.BeforeCleanup = () =>
        {
            var snapshot = Task.Run(TextServerManager.GetInterfaces);
            Check(snapshot.Wait(TimeSpan.FromSeconds(3)) && snapshot.Result.Contains(cleanup), "Cleanup runs before unregistration without retaining the registry monitor.");
            Reject<InvalidOperationException>(() => TextServerManager.SetPrimaryInterface(cleanup));
            Reject<InvalidOperationException>(() => TextServerManager.AddInterface(cleanup));
            Reject<InvalidOperationException>(() => Task.Run(cleanup.GetName).GetAwaiter().GetResult());
            cleanup.FreeRID(owned); throw new InvalidDataException("cleanup callback");
        };
        Action<string> removed = _ => { Check(!TextServerManager.GetInterfaces().Contains(cleanup), "The removal event observes committed registry state after failing cleanup."); throw new IOException("removed callback"); };
        TextServerManager.InterfaceRemoved += removed;
        try
        {
            try { TextServerManager.RemoveInterface(cleanup); throw new InvalidOperationException("Removal callback failures were lost."); }
            catch (AggregateException errors) { Check(errors.InnerExceptions.Any(e => e is InvalidDataException) && errors.InnerExceptions.Any(e => e is IOException), "Cleanup and removal event failures both survive committed removal."); }
        }
        finally { TextServerManager.InterfaceRemoved -= removed; }
        Check(cleanup.CleanupCalls == 1 && !cleanup.Has(owned), "Cleanup runs once and may retire quiescent owned sources.");
        var afterCleanup = cleanup.CreateFont(); cleanup.FreeRID(afterCleanup);
    }
    using (var faulty = new FactoryProbe())
    {
        var existing = faulty.CreateFont(); faulty.Existing = existing; faulty.Mode = 1;
        Reject<InvalidOperationException>(() => faulty.CreateFont()); Check(faulty.FontCount == 1 && faulty.Has(existing), "A factory cannot return or roll back an unrelated pre-existing source.");
        faulty.Mode = 2; Reject<InvalidDataException>(() => faulty.CreateFont()); Check(faulty.FontCount == 1, "A failing factory retires its partially allocated source.");
        faulty.Mode = 3; Reject<InvalidOperationException>(() => faulty.CreateFont()); Check(faulty.FontCount == 1 && faulty.BufferCount == 0, "A wrong-role result rolls back all identities allocated by that factory.");
        faulty.Mode = 0; var other = faulty.CreateFont(); faulty.CloneOverride = _ => other;
        Reject<InvalidOperationException>(() => faulty.FontGetResource(existing).Duplicate()); Check(faulty.Has(other), "A copy callback cannot transfer an unrelated live source.");
        faulty.CloneOverride = _ => { faulty.CreateFont(); throw new InvalidDataException("copy"); };
        Reject<InvalidDataException>(() => faulty.FontGetResource(existing).Duplicate()); Check(faulty.FontCount == 2, "A failing source-copy callback rolls back its nested allocations.");
        faulty.CloneOverride = null; faulty.FreeRID(other); faulty.FreeRID(existing);
    }
    foreach (var registration in new[] { false, true })
    {
        using var provider = new RacingProbe(); var events = 0; provider.Disposed += _ => events++;
        var disposal = Task.Run(() => Reject<InvalidOperationException>(() => provider.Dispose()));
        Check(provider.Validated.Wait(TimeSpan.FromSeconds(3)), "Disposal validation reached the controlled race.");
        Task<string>? read = null;
        if (registration) TextServerManager.AddInterface(provider);
        else
        {
            provider.BlockName = true; read = Task.Run(provider.GetName);
            Check(provider.Reading.Wait(TimeSpan.FromSeconds(3)), "Callback acquired admission after disposal validation.");
        }
        provider.ContinueDisposal.Set(); Check(disposal.Wait(TimeSpan.FromSeconds(3)), "Atomic disposal transition rejects the newly admitted user.");
        Check(!provider.IsDisposed && events == 0, "The veto preserves a live implementation without terminal notification.");
        if (registration) TextServerManager.RemoveInterface(provider);
        else { provider.ContinueRead.Set(); Check(read!.GetAwaiter().GetResult() == "Probe", "Admitted callback completes before retirement."); }
        provider.BlockValidation = false; provider.BlockName = false; provider.Dispose();
        Check(provider.IsDisposed && events == 1, "A subsequent unretained retirement completes exactly once.");
        Reject<ObjectDisposedException>(() => provider.GetName());
    }
    Console.WriteLine("Text provider callback/registry admission, post-validation disposal races and 128 prepared callbacks 0 B passed.");
}
static void Layout()
{
    var builtin = TextServerManager.GetPrimaryInterface();
    using var provider = new Probe(); using var font = new FontFile();
    font.LoadDynamicFont(System.IO.Path.Combine(AppContext.BaseDirectory, "Assets/Regular.ttf"));
    using var native = new TextLine(); native.AddString("Provider", font, 18); var width = native.GetLineWidth();
    TextServerManager.AddInterface(provider);
    try
    {
        TextServerManager.SetPrimaryInterface(provider);
        using var line = new TextLine(); line.AddString("Provider", font, 18);
        Check(line.GetLineWidth() == width + 7 && provider.WidthCalls != 0, "The selected provider owns the real TextLine consumer.");
        Check(provider.ShapedTextGetWidth(line.GetRID()) == width + 7, "A canonical TextLine RID dispatches its selected consumer exactly once.");
        Check(provider.ShapedTextGetGlyphs(line.GetRID()).Length != 0, "Canonical glyph metadata follows the same provider layout.");
        var serviceFontRID = builtin.CreateFont();
        try
        {
            builtin.FontSetData(serviceFontRID, File.ReadAllBytes(System.IO.Path.Combine(AppContext.BaseDirectory, "Assets/Regular.ttf")));
            var serviceFont = builtin.FontGetResource(serviceFontRID);
            Check(ReferenceEquals(provider.FontGetResource(serviceFontRID), serviceFont), "A native service source retains its projection across selected implementations.");
            provider.DuringNativeRead = id => { if (id == serviceFontRID) Reject<InvalidOperationException>(() => builtin.FreeRID(serviceFontRID)); };
            try { Check(serviceFont.GetAscent(18) > 0, "Native service fonts remain usable through another implementation's default callbacks."); }
            finally { provider.DuringNativeRead = null; }
        }
        finally { if (builtin.Has(serviceFontRID)) builtin.FreeRID(serviceFontRID); }
        var ownedBreaks = provider.CreateShapedText();
        try
        {
            provider.ShapedTextAddString(ownedBreaks, "AA AA", [font], 18);
            Check(provider.ShapedTextGetLineBreaks(ownedBreaks, 20).Length > 0 && provider.BreakUpdates > 0, "Default line breaking invokes the optional update callback and continues after false.");
            Check(provider.ShapedTextGetWordBreaks(ownedBreaks).Length == 4, "Default word boundaries retain current prepared opportunities.");
            Check(provider.ShapedTextFitToWidth(ownedBreaks, 100) > 0 && provider.JustificationUpdates > 0, "Default fitting invokes the optional justification update callback.");
        }
        finally { provider.FreeRID(ownedBreaks); }
        var canonicalCopy = provider.ShapedTextDuplicate(line.GetRID());
        try { Check(provider.Has(canonicalCopy), "A canonical copy has a live creating-interface identity."); } finally { provider.FreeRID(canonicalCopy); }
        var canonicalSlice = provider.ShapedTextSubstr(line.GetRID(), 0, 3);
        try { Check(provider.ShapedTextGetRange(canonicalSlice) == new Vector2i(0, 3), "A canonical substring keeps its scalar range."); } finally { provider.FreeRID(canonicalSlice); }
        using var metadataLine = new TextLine(); metadataLine.AddString("A", font, 18, "en", 73); _ = metadataLine.GetSize();
        Check(provider.TypedAdds > 0 && provider.LastIntMetadata == 73 && provider.TryGetSpanMetadata<int>(metadataLine.GetRID(), 0, out var payload) && payload == 73, "Typed metadata reaches producer callbacks and canonical aliases without losing its exact type.");
        using var featureLine = new TextLine(); provider.ShapedTextAddString(featureLine.GetRID(), "ffi", [font], 18, new Dictionary<string, int> { ["standard_ligatures"] = 0 }); _ = featureLine.GetSize();
        Check(provider.FeatureAdds > 0, "Authored OpenType span overrides reach the selected producer.");
        using var unprepared = new TextLine(); unprepared.AddString("Provider", font, 18);
        Check(provider.ShapedTextGetWidth(unprepared.GetRID()) == width + 7, "A canonical RID prepares through the selected provider on its first service query.");
        provider.ShapedTextClear(unprepared.GetRID());
        Check(unprepared.SpanCount == 0, "Service authoring through a canonical RID updates its public owner.");
    }
    finally { TextServerManager.SetPrimaryInterface(builtin); TextServerManager.RemoveInterface(provider); }
}
class Probe : TextServerExtension
{
    internal int WidthCalls, ParserCalls, TypedAdds, FeatureAdds, BreakUpdates, JustificationUpdates;
    protected override bool OnShapedTextUpdateBreaks(RID shaped) { BreakUpdates++; return false; }
    protected override bool OnShapedTextUpdateJustificationOps(RID shaped) { JustificationUpdates++; return false; }
    internal int LastIntMetadata;
    internal Action<RID>? DuringNativeRead;
    protected override float OnFontGetAscent(RID fontRID, int size)
    { DuringNativeRead?.Invoke(fontRID); return base.OnFontGetAscent(fontRID, size); }
    protected override bool OnShapedTextAddString<T>(RID shaped, string text, ReadOnlySpan<Font> fonts, int size, IReadOnlyDictionary<string, int>? openTypeFeatures, string language, T metadata)
    { TypedAdds++; if (metadata is int value) LastIntMetadata = value; return base.OnShapedTextAddString(shaped, text, fonts, size, openTypeFeatures, language, metadata); }
    protected override bool OnShapedTextAddString(RID shaped, string text, ReadOnlySpan<Font> fonts, int size, IReadOnlyDictionary<string, int>? openTypeFeatures = null, string language = "")
    { if (openTypeFeatures is not null && openTypeFeatures.TryGetValue("standard_ligatures", out var enabled) && enabled == 0) FeatureAdds++; return base.OnShapedTextAddString(shaped, text, fonts, size, openTypeFeatures, language); }
    internal bool BadParserRange;
    private static readonly TextBIDIRange[] InvalidParserRange = [new(-1, 1, TextDirection.LTR)];
    protected override ReadOnlySpan<TextBIDIRange> OnParseStructuredText(StructuredTextParser parser, IReadOnlyList<string> options, string text)
    { ParserCalls++; return BadParserRange ? InvalidParserRange : base.OnParseStructuredText(parser, options, text); }
    protected override string OnGetName() => "Probe";
    protected override float OnShapedTextGetWidth(RID shaped) { WidthCalls++; return base.OnShapedTextGetWidth(shaped) + 7; }
}
sealed class CleanupProbe : Probe
{
    internal Action? BeforeCleanup;
    internal int CleanupCalls;
    protected override void OnCleanup() { CleanupCalls++; BeforeCleanup?.Invoke(); base.OnCleanup(); }
}
sealed class PreDeleteProbe : Probe
{
    internal Action? BeforeDelete;
    internal RID AllocateDuringRetirement() => AllocateShapedTextRID();
    protected override void OnNotification(int what) { base.OnNotification(what); if (what == NotificationPreDelete) BeforeDelete?.Invoke(); }
}
sealed class RacingProbe : Probe
{
    internal readonly ManualResetEventSlim Validated = new(), ContinueDisposal = new(), Reading = new(), ContinueRead = new();
    internal bool BlockValidation = true, BlockName;
    protected override void ValidateDisposal()
    {
        base.ValidateDisposal();
        if (BlockValidation) { Validated.Set(); if (!ContinueDisposal.Wait(TimeSpan.FromSeconds(3))) throw new TimeoutException("Disposal race was not released."); }
    }
    protected override void Dispose(bool disposing)
    {
        if (disposing) { Validated.Dispose(); ContinueDisposal.Dispose(); Reading.Dispose(); ContinueRead.Dispose(); }
        base.Dispose(disposing);
    }
    protected override string OnGetName()
    {
        if (BlockName) { Reading.Set(); if (!ContinueRead.Wait(TimeSpan.FromSeconds(3))) throw new TimeoutException("Callback race was not released."); }
        return base.OnGetName();
    }
}

sealed class FactoryProbe : OpaqueProbe
{
    internal RID Existing;
    internal int Mode;
    protected override RID OnCreateFont()
    {
        if (Mode == 1) return Existing;
        var rid = base.OnCreateFont();
        if (Mode == 2) throw new InvalidDataException("factory");
        return Mode == 3 ? AllocateShapedTextRID() : rid;
    }
}
class OpaqueProbe : Probe
{
    private readonly Dictionary<RID, float> _widths = [];
    private sealed class FontState
    {
        internal string Name = "Opaque";
        internal Dictionary<uint, float> Coordinates = [];
        internal int[] Spacing = new int[4];
        internal FontState Copy() => new() { Name = Name, Coordinates = new(Coordinates), Spacing = (int[])Spacing.Clone() };
    }
    private readonly Dictionary<RID, FontState> _fontStates = [];
    internal int FontCount => _fontStates.Count;
    protected bool IsProbeFont(RID source) => _fontStates.ContainsKey(source);
    internal int BufferCount => _widths.Count;
    internal Func<RID, RID>? CloneOverride;
    internal void ChangeNameDirect(RID source, string name) { _fontStates[source].Name = name; NotifyFontChanged(source); }
    internal Action? DuringFontAuthoring;
    internal Action<RID>? DuringAscent;
    internal bool FailFontAuthoring;
    internal Action? ReadProbe;
    protected override RID OnCreateFont() { var rid = AllocateFontRID(CloneFont); _fontStates.Add(rid, new()); return rid; }
    private RID CloneFont(RID source) { if (CloneOverride is { } copy) return copy(source); var rid = AllocateFontRID(CloneFont); _fontStates.Add(rid, _fontStates[source].Copy()); return rid; }
    protected override RID OnCreateShapedText(TextDirection direction = TextDirection.Auto, TextOrientation orientation = TextOrientation.Horizontal)
    { var rid = AllocateShapedTextRID(); _widths.Add(rid, 13); return rid; }
    protected override float OnFontGetAscent(RID fontRID, int size) { DuringAscent?.Invoke(fontRID); return 9; }
    protected override float OnFontGetDescent(RID fontRID, int size) => 3;
    protected override string OnFontGetName(RID fontRID) => _fontStates[fontRID].Name;
    protected override void OnFontSetName(RID fontRID, string value) => _fontStates[fontRID].Name = value;
    protected override void OnFontSetVariationCoordinates(RID fontRID, Dictionary<uint, float> value) => _fontStates[fontRID].Coordinates = new(value);
    protected override Dictionary<uint, float> OnFontGetVariationCoordinates(RID fontRID) => new(_fontStates[fontRID].Coordinates);
    protected override void OnFontSetFaceIndex(RID fontRID, int value) { }
    protected override void OnFontSetEmbolden(RID fontRID, float value) { DuringFontAuthoring?.Invoke(); if (FailFontAuthoring) throw new InvalidDataException("Controlled opaque variation authoring failure."); }
    protected override void OnFontSetTransform(RID fontRID, Transform value) { }
    protected override void OnFontSetBaselineOffset(RID fontRID, float value) { }
    protected override void OnFontSetUsedPalette(RID fontRID, int value) { }
    protected override void OnFontSetPaletteCustomColors(RID fontRID, Color[] value) { }
    protected override void OnFontSetSpacing(RID fontRID, TextSpacingType spacing, int value) => _fontStates[fontRID].Spacing[(int)spacing] = value;
    protected override bool OnFontHasChar(RID fontRID, int character) => character == 'A';
    protected override int OnFontGetGlyphIndex(RID fontRID, int size, int character, int variationSelector) => character == 'A' ? 1 : 0;
    protected override Vector2 OnFontGetGlyphAdvance(RID fontRID, int size, int glyph) => new(6, 12);
    protected override int OnFontGetSpacing(RID fontRID, TextSpacingType spacing) => _fontStates[fontRID].Spacing[(int)spacing];
    protected override float OnShapedTextGetWidth(RID shaped) { ReadProbe?.Invoke(); return _widths[shaped]; }
    protected override void OnFreeRID(RID rid) { _widths.Remove(rid); _fontStates.Remove(rid); base.OnFreeRID(rid); }
}
