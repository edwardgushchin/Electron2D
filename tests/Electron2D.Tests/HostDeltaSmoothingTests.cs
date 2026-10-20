using Electron2D;
using System.Reflection;
using System.Text.Json;

internal static class HostDeltaSmoothingTests
{
    private static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    internal static void RunManaged()
    {
        var old = OS.DeltaSmoothing;
        try
        {
            VerifySettings();
            Check(OS.DeltaSmoothing, "Process default enables host delta smoothing");
            var property = (PropertyDescriptor<OS, bool>)OS.Service.GetPropertyList().Single(p => p.Name == nameof(OS.DeltaSmoothing));
            property.SetValue(OS.Service, false); Check(!OS.DeltaSmoothing && !property.GetValue(OS.Service) && !property.IsStored, "Typed nonstored service property");
            Task.Run(() => OS.DeltaSmoothing = true).GetAwaiter().GetResult(); Check(OS.DeltaSmoothing, "Cross-thread setting access");
            var smoother = new FrameDeltaSmoother();
            for (var i = 0; i < 179; i++) Check(smoother.Smooth(16670, true, true, out var applied) == 16670 && !applied, "Estimator startup preserves measured input");
            Check(smoother.Smooth(16670, true, true, out var ready) == 16666 && ready, "Confident cadence quantizes to one refresh period");
            foreach (var delta in new long[] { 0, 999, 1_000_001, long.MaxValue }) Check(smoother.Smooth(delta, false, true, out var applied) == delta && !applied, "Disabled path is exact");
            Check(smoother.Smooth(40000, true, false, out var adaptive) == 40000 && !adaptive, "Nonclassical VSync bypass");
            Check(smoother.Smooth(1_000_001, true, true, out var resume) == 1_000_001 && !resume, "Resume interval bypass");
            for (var i = 0; i < 64; i++) _ = smoother.Smooth(33333, true, true, out _);
            Check(smoother.Smooth(33333, true, true, out var unstable) == 33333 && !unstable, "Measured rate mismatch disables smoothing");
            smoother.Reset(); Check(smoother.Smooth(16670, true, true, out var reset) == 16670 && !reset, "New run resets confidence");
            var first = Manual(false); var second = Manual(true); Check(first.SequenceEqual(second), "Explicit embedding elapsed time and physics remain independent of the host policy");
            DumpTraces();
            for (var i = 0; i < 2000; i++) _ = smoother.Smooth(16670, true, true, out _);
            var owner = GC.GetAllocatedBytesForCurrentThread(); var all = GC.GetTotalAllocatedBytes(true); long checksum = 0;
            for (var i = 0; i < 16384; i++) checksum += smoother.Smooth(16000 + i % 3 * 666, true, true, out _);
            var ownerBytes = GC.GetAllocatedBytesForCurrentThread() - owner; var allBytes = GC.GetTotalAllocatedBytes(true) - all;
            Check(checksum > 0 && ownerBytes == 0 && allBytes == 0, "16384 prepared clock samples owner/all bytes " + ownerBytes + "/" + allBytes);
            Console.WriteLine("Host delta policy/startup/bypass/reset/manual clocks and 16384 zero-owner/all-byte samples passed.");
        }
        finally { OS.DeltaSmoothing = old; }
    }
    private static void VerifySettings()
    {
        Check(ProjectSettings.Get(ProjectSettings.DeltaSmoothing), "Project default enables smoothing");
        var folder = System.IO.Directory.CreateTempSubdirectory("electron2d-host-delta-");
        try
        {
            using var authored = new ProjectSettingsRegistry(folder.FullName, System.IO.Path.Combine(folder.FullName, "user"));
            authored.Set(ProjectSettings.DeltaSmoothing, false); authored.SetFeatureOverride(ProjectSettings.DeltaSmoothing, "delta-test", true);
            var path = System.IO.Path.Combine(folder.FullName, "delta.e2d"); authored.SaveCustom(path);
            using var restored = new ProjectSettingsRegistry(folder.FullName, System.IO.Path.Combine(folder.FullName, "user")); restored.LoadCustom(path);
            Check(!restored.Get(ProjectSettings.DeltaSmoothing), "Saved base policy round trips in a separately registered project");
            restored.AddCustomFeature("delta-test"); Check(restored.GetWithOverride(ProjectSettings.DeltaSmoothing), "Restored active feature override");
            restored.RemoveCustomFeature("delta-test"); restored.Reset(ProjectSettings.DeltaSmoothing); Check(restored.Get(ProjectSettings.DeltaSmoothing), "Reset restores true initial value");
        }
        finally { folder.Delete(true); }
    }
    private static (double Delta, int Physics)[] Manual(bool enabled)
    {
        OS.DeltaSmoothing = enabled; var node = new Probe(); using var tree = new SceneTree(node); Engine.Start(tree);
        try { for (var i = 0; i < 128; i++) Engine.AdvanceFrame(i % 3 == 0 ? .01 : .02); }
        finally { Engine.Stop(); }
        return node.Values.ToArray();
    }
    private sealed class Probe : Node
    {
        internal readonly List<(double Delta, int Physics)> Values = [];
        private int _physics;
        internal Probe() { ProcessEnabled = PhysicsProcessEnabled = true; }
        protected override void OnPhysicsProcess(double delta) { _physics++; }
        protected override void OnProcess(double delta) { Values.Add((delta, _physics)); }
    }
    private static void DumpTraces()
    {
        if (Environment.GetEnvironmentVariable("ELECTRON2D_HOST_DELTA_TRACE") is not { } path) return;
        using var output = new StreamWriter(path); uint random = 0x52E2D;
        var smoother = new FrameDeltaSmoother();
        for (var profile = 0; profile < 16; profile++)
        {
            smoother.Reset();
            for (var i = 0; i < 2048; i++)
            {
                random = unchecked(random * 1664525 + 1013904223);
                long delta = profile switch
                {
                    0 => 16670,
                    1 => i % 2 == 0 ? 16000 : 17332,
                    2 => 11111,
                    3 => 33333,
                    4 => 999,
                    5 => 1000,
                    6 => i % 217 == 0 ? 1_000_001 : 16666,
                    7 => i % 217 == 0 ? 1_000_000 : 16666,
                    8 => i < 600 ? 16666 : 16393,
                    9 => i < 600 ? 16666 : 16949,
                    10 => i < 600 ? 20000 : 8333,
                    11 => i < 600 ? 8333 : 12500,
                    12 => i % 61 == 0 ? 0 : 16666,
                    _ => 15333 + random % 2667
                };
                var enabled = profile != 13 || i / 73 % 2 == 0;
                var vsync = profile != 14 || i / 83 % 2 == 0;
                var value = smoother.Smooth(delta, enabled, vsync, out _);
                output.WriteLine($"{(i == 0 ? 1 : 0)} {delta} {(enabled ? 1 : 0)} {(vsync ? 1 : 0)} {value}");
            }
        }
    }
    private static readonly FieldInfo EngineSmoother = typeof(Engine).GetField("_frameDeltaSmoother", BindingFlags.NonPublic | BindingFlags.Instance)!;
    private static readonly FieldInfo MeasurementFrames = typeof(FrameDeltaSmoother).GetField("_measurementFrames", BindingFlags.NonPublic | BindingFlags.Instance)!;
    private static readonly FieldInfo Rate = typeof(FrameDeltaSmoother).GetField("_rate", BindingFlags.NonPublic | BindingFlags.Instance)!;
    private static readonly FieldInfo Ready = typeof(FrameDeltaSmoother).GetField("_ready", BindingFlags.NonPublic | BindingFlags.Instance)!;
    private static readonly FieldInfo Remaining = typeof(FrameDeltaSmoother).GetField("_remaining", BindingFlags.NonPublic | BindingFlags.Instance)!;
    private static (int Frames, int FPS, bool Ready, long Remainder) State()
    {
        var value = EngineSmoother.GetValue(Engine.Service)!;
        return ((int)MeasurementFrames.GetValue(value)!, (int)Rate.GetValue(value)!, (bool)Ready.GetValue(value)!, (long)Remaining.GetValue(value)!);
    }
    internal static void RunNative(string renderer)
    {
        RunManaged();
        var method = ProjectSettings.Get(ProjectSettings.RenderingMethod); var fallback = ProjectSettings.Get(ProjectSettings.RenderingFallback); var fps = Engine.MaxFPS; var smooth = OS.DeltaSmoothing; var projectSmooth = ProjectSettings.Get(ProjectSettings.DeltaSmoothing);
        const string feature = "host-delta-native-tests"; ProjectSettings.Set(ProjectSettings.DeltaSmoothing, true); ProjectSettings.SetFeatureOverride(ProjectSettings.DeltaSmoothing, feature, false); ProjectSettings.AddCustomFeature(feature); OS.DeltaSmoothing = true;
        ProjectSettings.Set(ProjectSettings.RenderingMethod, renderer); ProjectSettings.Set(ProjectSettings.RenderingFallback, false); Engine.MaxFPS = 60;
        try
        {
            var root = new Window { Size = new(96, 64) }; var actor = new Actor(); root.AddChild(actor);
            var frozen = default((int Frames, int FPS, bool Ready, long Remainder)); var nativeSamples = false; var observedReady = false; var frames = 0; var effectiveDisabled = DisplayServer.VSyncMode.Enabled;
            root.Ready += _ =>
            {
                Check(!OS.DeltaSmoothing, "Effective project policy is sampled before readiness"); ProjectSettings.SetFeatureOverride(ProjectSettings.DeltaSmoothing, feature, true); Check(!OS.DeltaSmoothing, "Project edits apply to the next owned run");
                DisplayServer.WindowSetVSyncMode(DisplayServer.VSyncMode.Enabled); Check(DisplayServer.WindowGetVSyncMode() == DisplayServer.VSyncMode.Enabled, "Classical native VSync request");
                RenderingServer.FramePostDraw += () =>
                {
                    frames++; var state = State(); nativeSamples |= state.Frames != 0; observedReady |= state.Ready;
                    if (frames < 16) Check(state == default, "Disabled actual window loop preserves estimator state");
                    if (frames is >= 32 and < 48 || frames is >= 64 and < 80 && effectiveDisabled != DisplayServer.VSyncMode.Enabled) Check(state == frozen, "Live OS/VSync bypass freezes actual host estimator");
                    if (frames == 96)
                    {
                        using var image = root.GetTexture().GetImage()!; var pixel = image.GetPixel(10, 10); Check(pixel.R < .1 && pixel.G > .9 && pixel.B > .9 && actor.Processes == frames && actor.TotalDelta > 0, "Actual filtered-window callbacks and cyan pixels");
                        if (Environment.GetEnvironmentVariable("ELECTRON2D_HOST_DELTA_CAPTURE") is { } capture) image.SavePNG(capture);
                    }
                };
            };
            Check(Engine.Run((window, index) =>
            {
                if (index == 1) { OS.DeltaSmoothing = false; Check(State() == default, "Owned startup resets manual-run history"); }
                if (index == 16) OS.DeltaSmoothing = true;
                if (index == 32) { frozen = State(); OS.DeltaSmoothing = false; }
                if (index == 48) OS.DeltaSmoothing = true;
                if (index == 64) { frozen = State(); DisplayServer.WindowSetVSyncMode(DisplayServer.VSyncMode.Disabled); effectiveDisabled = DisplayServer.WindowGetVSyncMode(); Check(effectiveDisabled is DisplayServer.VSyncMode.Disabled or DisplayServer.VSyncMode.Enabled, "Native VSync request or documented fallback"); }
                if (index == 80) DisplayServer.WindowSetVSyncMode(DisplayServer.VSyncMode.Enabled);
                if (index == 400) window.Tree!.Quit();
            }, root) == 0 && frames == 399 && nativeSamples && observedReady, "Real native owned loop consumes and converges its smoothing policy");
            var final = State(); Check(final.FPS >= 50 && Engine.FramesPerSecond > 0 && actor.TotalDelta > 0, "Real estimator confidence and host FPS");
            Console.WriteLine(JsonSerializer.Serialize(new { profile = "host-delta-smoothing", renderer, frames, nativeSamples, observedReady, effectiveDisabled, estimateFPS = final.FPS, remainder = final.Remainder, measuredFPS = Engine.FramesPerSecond }));
            var captured = new Window { Size = new(64, 48) }; var process = new Probe(); captured.AddChild(process);
            captured.Ready += _ => { Check(OS.DeltaSmoothing, "Next owned startup reapplies the changed effective project policy"); RenderingServer.SetDefaultClearColor(Colors.Red); DisplayServer.WindowSetVSyncMode(DisplayServer.VSyncMode.Enabled); };
            using var writer = new MovieWriterTests.Writer { Window = captured, FPS = 59, Limit = 8 }; MovieWriter.AddWriter(writer); OS.DeltaSmoothing = true;
            Check(Engine.Run((_, _) => Check(State() == default, "Fixed recording never samples the real-time filter"), captured, "host-delta.movieprobe", 59) == 17,
                "Fixed recording completes its ordinary writer lifecycle");
            Check(writer.Frames == 8 && process.Values.Count == 8 && process.Values.All(value => Math.Abs(value.Delta - 1d / 59) < 1e-12) && State() == default,
                "Enabled OS policy and classical VSync preserve exact movie process delta and untouched estimator");
        }
        finally { ProjectSettings.RemoveCustomFeature(feature); ProjectSettings.ClearFeatureOverride(ProjectSettings.DeltaSmoothing, feature); ProjectSettings.Set(ProjectSettings.DeltaSmoothing, projectSmooth); OS.DeltaSmoothing = smooth; Engine.MaxFPS = fps; ProjectSettings.Set(ProjectSettings.RenderingMethod, method); ProjectSettings.Set(ProjectSettings.RenderingFallback, fallback); }
    }
    private sealed class Actor : Entity
    {
        internal int Processes;
        internal double TotalDelta;
        internal Actor() { ProcessEnabled = true; }
        protected override void OnProcess(double delta) { Processes++; TotalDelta += delta; }
        protected override void OnDraw() => DrawRect(new(4, 4, 32, 32), Colors.Cyan);
    }
}
