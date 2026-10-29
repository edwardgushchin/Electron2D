using Electron2D;

internal static class TextProviderRetentionTests
{
    internal static void Run()
    {
        var primary = TextServerManager.GetPrimaryInterface();
        using var provider = new Probe(); using var font = new FontFile { Data = FontTestFixtures.OpenSans };
        try
        {
            using (var read = font.BeginRead())
            {
                Reject(() => TextServerManager.SetPrimaryInterface(provider));
                Check(ReferenceEquals(TextServerManager.GetPrimaryInterface(), primary) && read.IsCurrent, "Native source reads retain the captured text implementation before callbacks can become selected.");
            }
            var owned = primary.CreateFont(); var link = primary.CreateFontLinkedVariation(owned);
            try
            {
                primary.FontSetData(owned, FontTestFixtures.OpenSans);
                using (var read = primary.FontGetResource(link).BeginRead())
                {
                    Reject(() => primary.FreeRID(owned)); Reject(() => primary.FreeRID(link));
                    Check(primary.Has(owned) && primary.Has(link) && read.IsCurrent, "A native projection read retains its linked entry and root source.");
                }
            }
            finally { primary.FreeRID(link); primary.FreeRID(owned); }
            PrivateNativeBuffer(provider);
            RasterReferences();
            using var inherited = new SpacingVariation { BaseFont = font };
            inherited.DuringSpacing = () =>
            {
                var metadata = Task.Run(inherited.GetFontName);
                Check(metadata.Wait(TimeSpan.FromSeconds(3)) && metadata.Result.Length > 0, "Inherited font callbacks execute without the font monitor.");
                Reject(() => TextServerManager.SetPrimaryInterface(provider));
            };
            Check(inherited.GetAscent(18) > 0, "A synchronized inherited callback completes its native font query.");
            inherited.DuringSpacing = null;
            TextServerManager.SetPrimaryInterface(provider);
            Check(font.GetAscent(18) > 0, "The implementation can change after the native and inherited reads finish.");
        }
        finally { TextServerManager.SetPrimaryInterface(primary); }
        Console.WriteLine("Native and inherited font reads retain provider selection; cross-thread callback metadata reads complete without a resource monitor.");
    }
    private static void RasterReferences()
    {
        using var first = new ScaleProbe(); using var second = new ScaleProbe();
        CanvasRasterScale.TrackProvider(first); CanvasRasterScale.TrackProvider(second);
        var firstKey = 64; var secondKey = 64; var rejectedKey = 64;
        second.DuringReference = () =>
        {
            var metadata = Task.Run(() => { CanvasRasterScale.TrackProvider(second); return second.GetName(); });
            Check(metadata.Wait(TimeSpan.FromSeconds(3)), "Raster callbacks release scale, implementation and registry monitors.");
            Reject(first.Dispose);
            Reject(() => CanvasRasterScale.ChangeReference(ref rejectedKey, 192));
            Check(rejectedKey == 64, "A rejected recursive scale change preserves its owner reference.");
        };
        try
        {
            CanvasRasterScale.ChangeReference(ref firstKey, 128); CanvasRasterScale.ChangeReference(ref secondKey, 128);
            Check(first.References == 2 && second.References == 2, "Every active viewport reference reaches every retained implementation.");
            second.FailRelease = true;
            try { CanvasRasterScale.ChangeReference(ref firstKey, 64); throw new InvalidOperationException("A failed scale release was lost."); }
            catch (AggregateException error) when (error.Flatten().InnerExceptions.Any(failure => failure is CallbackFailure)) { }
            Check(firstKey == 64 && first.References == 1 && second.References == 1, "A failing scale callback preserves committed ownership and notifies the other implementation.");
            CanvasRasterScale.ChangeReference(ref secondKey, 64);
            Check(first.References == 0 && second.References == 0 && CanvasRasterScale.ActiveLevel(128) is null, "The final owner retires the actual shared raster level.");
        }
        finally
        { second.DuringReference = null; second.FailRelease = false; CanvasRasterScale.ChangeReference(ref firstKey, 64); CanvasRasterScale.ChangeReference(ref secondKey, 64); }
    }
    private sealed class ScaleProbe : TextServerExtension
    {
        protected override string OnGetName() => "Raster reference probe";
        internal int References;
        internal bool FailRelease;
        internal Action? DuringReference;
        protected override void OnReferenceOversamplingLevel(float oversampling) { References++; DuringReference?.Invoke(); }
        protected override void OnUnreferenceOversamplingLevel(float oversampling) { References--; if (FailRelease) { FailRelease = false; throw new CallbackFailure(); } }
    }
    private static void PrivateNativeBuffer(Probe provider)
    {
        RID buffer;
        using (var consumer = TextServerManager.BeginConsumerRead(provider))
        using (var creation = TextServerManager.BeginBufferCreation(provider)) buffer = provider.CreateShapedText();
        using var entered = new ManualResetEventSlim(); using var release = new ManualResetEventSlim();
        provider.DuringWidth = () => { entered.Set(); if (!release.Wait(TimeSpan.FromSeconds(3))) throw new TimeoutException("Private native buffer reader was not released."); };
        var read = Task.Run(() => { using var consumer = TextServerManager.BeginConsumerRead(provider); return provider.ShapedTextGetWidth(buffer); });
        try
        {
            Check(entered.Wait(TimeSpan.FromSeconds(3)), "A private native buffer acquired its cross-thread consumer read.");
            using var consumer = TextServerManager.BeginConsumerRead(provider);
            Reject(() => provider.ShapedTextClear(buffer)); Reject(() => provider.FreeRID(buffer)); Reject(() => provider.ShapedTextGetWidth(buffer));
        }
        finally { release.Set(); }
        Check(read.GetAwaiter().GetResult() == 0, "A retained private native buffer completes after competing operations reject."); provider.DuringWidth = null;
        using (var consumer = TextServerManager.BeginConsumerRead(provider))
        {
            provider.DuringFree = rid => { if (rid != buffer) return; Reject(() => provider.ShapedTextGetWidth(rid)); throw new CallbackFailure(); };
            try { provider.FreeRID(buffer); throw new InvalidOperationException("A native retirement callback failure was lost."); }
            catch (CallbackFailure) { Check(!provider.Has(buffer), "Native retirement still releases managed ownership after a failing callback."); }
            finally { provider.DuringFree = null; }
        }
    }
    private sealed class CallbackFailure : Exception { }
    private static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    private static void Reject(Action operation)
    { try { operation(); } catch (InvalidOperationException) { return; } throw new InvalidOperationException("An active font read permitted a provider replacement."); }
    private sealed class Probe : TextServerExtension
    {
        internal Action? DuringWidth;
        internal Action<RID>? DuringFree;
        protected override string OnGetName() => "Retention probe";
        protected override float OnShapedTextGetWidth(RID shaped) { DuringWidth?.Invoke(); return base.OnShapedTextGetWidth(shaped); }
        protected override void OnFreeRID(RID rid) { DuringFree?.Invoke(rid); base.OnFreeRID(rid); }
    }
    private sealed class SpacingVariation : FontVariation
    {
        internal Action? DuringSpacing;
        public override int GetSpacing(TextSpacingType spacing) { DuringSpacing?.Invoke(); return base.GetSpacing(spacing); }
    }
}
