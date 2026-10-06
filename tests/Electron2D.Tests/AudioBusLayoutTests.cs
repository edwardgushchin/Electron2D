using Electron2D;
using System.Diagnostics;

internal static class AudioBusLayoutTests
{
    internal static void Run(bool native = false)
    {
        var folder = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "e2d-bus-layout-" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(folder);
        var defaultPath = ProjectSettings.Get(ProjectSettings.AudioBusesDefaultBusLayout);
        try
        {
            ProjectSettings.Set(ProjectSettings.AudioBusesDefaultBusLayout, "");
            DataAndArchives(folder); Boundaries();
            if (native) Native();
            Console.WriteLine("Audio bus layouts: graph archives, fresh-process defaults, copies, validation and native replacement passed.");
        }
        finally { Reset(); ProjectSettings.Set(ProjectSettings.AudioBusesDefaultBusLayout, defaultPath); Directory.Delete(folder, true); }
    }
    private static void Reset() { AudioServer.Service.CloseNative(); using var empty = new AudioBusLayout(); AudioServer.SetBusLayout(empty); }
    private static void DataAndArchives(string folder)
    {
        Reset(); using var gain = new AudioEffectAmplify { VolumeDB = -6 }; using var eq = new AudioEffectEQ10(); eq.SetBandGainDB(3, 7);
        using var layout = new AudioBusLayout();
        layout.SetSnapshot([new() { Name = "renamed master", Send = "ignored", VolumeDB = -2 }, new() { Name = "Effects", Send = "Master", Solo = true, Mute = true, Bypass = true, VolumeDB = -4, Effects = [new(gain, true), new(null, true), new(eq, false), new(gain, false)] }, new() { Name = "Music", Send = "Effects", VolumeDB = float.NegativeInfinity }]);
        AudioServer.SetBusLayout(layout);
        Check(AudioServer.BusCount == 3 && AudioServer.GetBusName(0) == "Master" && AudioServer.GetBusEffectCount(1) == 3 && AudioServer.IsBusMute(1) && AudioServer.IsBusSolo(1) && AudioServer.IsBusBypassingEffects(1) && !AudioServer.IsBusEffectEnabled(1, 1), "Complete bus data and null effect filtering.");
        using var captured = AudioServer.GenerateBusLayout(); using var shallow = (AudioBusLayout)captured.Duplicate(); using var deep = (AudioBusLayout)captured.DuplicateDeep(DeepDuplicateMode.All);
        Check(ReferenceEquals(shallow.Snapshot()[1].Effects[0].Resource, gain) && !ReferenceEquals(deep.Snapshot()[1].Effects[0].Resource, gain) && ReferenceEquals(deep.Snapshot()[1].Effects[0].Resource, deep.Snapshot()[1].Effects[2].Resource), "Shallow borrowing and deep alias preservation.");
        AudioServer.SetBusName(1, "Changed"); AudioServer.SetBusLayout(captured); Check(AudioServer.GetBusName(1) == "Effects" && AudioServer.GetBusSend(2) == "Effects", "Snapshot containers are independent.");
        var path = System.IO.Path.Combine(folder, "buses.e2dres");
        foreach (var flags in new[] { SaverFlags.None, SaverFlags.Compress | SaverFlags.SaveBigEndian, SaverFlags.BundleResources })
        {
            ResourceSaver.Save(captured, path, flags);
            using var loaded = ResourceLoader.Load<AudioBusLayout>(path, ResourceLoader.CacheMode.IgnoreDeep);
            var effect = (AudioEffectAmplify)loaded.Snapshot()[1].Effects[0].Resource!;
            Check(effect.VolumeDB == -6 && ((AudioEffectEQ10)loaded.Snapshot()[1].Effects[1].Resource!).GetBandGainDB(3) == 7 && ReferenceEquals(effect, loaded.Snapshot()[1].Effects[2].Resource), "Archive typed DSP controls and aliases.");
            AudioServer.SetBusLayout(loaded); loaded.Dispose(); Check(!effect.IsDisposed, "Applied graph retains owned file dependencies after root disposal.");
            using var retained = AudioServer.GenerateBusLayout(); Reset(); Check(!effect.IsDisposed, "Generated layout retains file dependencies after graph replacement."); retained.Dispose(); Check(effect.IsDisposed, "Last graph owner releases internal effects.");
        }
        using (var cached = ResourceLoader.Load<AudioBusLayout>(path))
        {
            ResourceSaver.Save(shallow, path);
            Check(ReferenceEquals(cached, ResourceLoader.Load<AudioBusLayout>(path, ResourceLoader.CacheMode.Replace)), "Layout cache replacement retains root identity.");
            var bytes = System.IO.File.ReadAllBytes(path); bytes[^1] ^= 1; System.IO.File.WriteAllBytes(path, bytes); Reject<InvalidDataException>(() => ResourceLoader.Load<AudioBusLayout>(path, ResourceLoader.CacheMode.Replace));
            Check(cached.Snapshot().Length == 3, "Corrupt replacement preserves prior snapshot.");
        }
        ResourceSaver.Save(captured, path); FreshProcess(path);
        // Every shipped concrete effect is saveable through the built-in direct factories.
        foreach (var type in typeof(AudioEffect).Assembly.GetExportedTypes().Where(t => typeof(AudioEffect).IsAssignableFrom(t) && !t.IsAbstract))
        {
            using var effect = (AudioEffect)Activator.CreateInstance(type)!; using var all = new AudioBusLayout(); all.SetSnapshot([new() { Name = "Master", Effects = [new(effect, true)] }]);
            ResourceSaver.Save(all, path); using var loaded = ResourceLoader.Load<AudioBusLayout>(path, ResourceLoader.CacheMode.IgnoreDeep); Check(loaded.Snapshot()[0].Effects[0].Resource!.GetType() == type, "Built-in effect archive " + type.Name);
        }
    }
    private static void Boundaries()
    {
        Reset(); var events = 0; AudioServer.BusLayoutChanged += Changed;
        try
        {
            Reject<ArgumentOutOfRangeException>(() => AudioServer.SetBusVolumeDB(0, float.MaxValue)); Check(AudioServer.GetBusVolumeDB(0) == 0, "Overflowing gain rejects before mutation.");
            using var layout = new AudioBusLayout(); AudioServer.SetBusLayout(layout); Check(events == 1, "One committed notification.");
            layout.SetSnapshot([]); Reject<ArgumentException>(() => AudioServer.SetBusLayout(layout));
            layout.SetSnapshot([new() { Name = "other" }, new() { Name = "Master" }]); Reject<ArgumentException>(() => AudioServer.SetBusLayout(layout));
            layout.SetSnapshot([new() { VolumeDB = float.MaxValue }]); Reject<ArgumentException>(() => AudioServer.SetBusLayout(layout));
            using var disposedEffect = new AudioEffectAmplify(); disposedEffect.Dispose(); layout.SetSnapshot([new() { Effects = [new(disposedEffect, false)] }]); Reject<ObjectDisposedException>(() => AudioServer.SetBusLayout(layout));
            Check(events == 1 && AudioServer.BusCount == 1, "All invalid data rejects before mutation/notification.");
            Reject<ArgumentNullException>(() => AudioServer.SetBusLayout(null!)); layout.Dispose(); Reject<ObjectDisposedException>(() => AudioServer.SetBusLayout(layout));
            Reject<InvalidOperationException>(() => Task.Run(() => { using var snapshot = AudioServer.GenerateBusLayout(); }).GetAwaiter().GetResult());
            using var bounds = new AudioBusLayout(); var count = bounds.GetPropertyList().OfType<PropertyDescriptor<AudioBusLayout, int>>().Single(p => p.Name == "BusCount"); Reject<ArgumentOutOfRangeException>(() => count.SetValue(bounds, 256)); Reject<ArgumentOutOfRangeException>(() => count.SetValue(bounds, -1));
            count.SetValue(bounds, 255); var buses = bounds.Snapshot(); for (var i = 0; i < buses.Length; i++) buses[i].Name = "Bus " + i; bounds.SetSnapshot(buses); AudioServer.SetBusLayout(bounds); Check(AudioServer.BusCount == 255, "Maximum bus count."); Reset();
            AudioServer.BusLayoutChanged += ThrowNotification;
            try { Reject<AggregateException>(() => AudioServer.SetBusLayout(bounds)); Check(AudioServer.BusCount == 255, "Notification failure occurs after commitment."); }
            finally { AudioServer.BusLayoutChanged -= ThrowNotification; Reset(); }
        }
        finally { AudioServer.BusLayoutChanged -= Changed; }
        void Changed() => events++;
        static void ThrowNotification() => throw new InvalidOperationException("Notification fixture");
    }
    private static void Native()
    {
        Reset(); using var stream = AudioEffectTests.Constant(); using var gain = new AudioEffectAmplify { VolumeDB = -6 }; using var layout = new AudioBusLayout(); layout.SetSnapshot([new() { Name = "Master" }, new() { Name = "Music", Send = "Master", Effects = [new(gain, true)] }]);
        var player = new AudioStreamPlayer { Stream = stream, Bus = "Music", MaxPolyphony = 1 }; var root = new Node(); root.AddChild(player); using var tree = new SceneTree(root);
        try
        {
            player.Play(); var playback = player.GetStreamPlayback(); var native = AudioServer.Service.Native; AudioServer.SetBusLayout(layout); AudioEffectTests.Wait(native, 20); AudioEffectTests.CheckOutput(native, new Vector2(.2f, -.3f) * Mathf.DBToLinear(-6)); Check(ReferenceEquals(playback, player.GetStreamPlayback()), "Live replacement retains playback identity and changes native PCM.");
            var instance = AudioServer.GetBusEffectInstance(1, 0); player.StreamPaused = true; AudioEffectTests.Wait(native, 8); var position = player.GetPlaybackPosition(); AudioServer.SetBusLayout(layout); Check(player.StreamPaused && player.GetPlaybackPosition() == position && instance.IsDisposed && !gain.IsDisposed, "Paused position is retained and old effect histories are released."); player.StreamPaused = false;
            using var failure = new FactoryEffect(); using var bad = new AudioBusLayout(); bad.SetSnapshot([new() { Name = "Master", Effects = [new(failure, true), new(failure, true)] }]); failure.FailAt = 2;
            var old = AudioServer.GetBusEffectInstance(1, 0); Reject<AggregateException>(() => AudioServer.SetBusLayout(bad)); Check(AudioServer.BusCount == 2 && !old.IsDisposed && failure.Last!.IsDisposed && player.IsPlaying(), "Factory failure cleans prepared siblings and preserves active graph.");
            failure.FailAt = int.MaxValue; failure.Callback = () => AudioServer.SetBusLayout(layout); Reject<AggregateException>(() => AudioServer.SetBusLayout(bad)); Check(AudioServer.BusCount == 2, "Factory configuration reentrancy rejects before commit.");
            failure.Callback = null; failure.ThrowCleanup = true; AudioServer.SetBusLayout(bad); var cleanupInstance = AudioServer.GetBusEffectInstance(0, 0);
            Reject<AggregateException>(() => AudioServer.SetBusLayout(layout)); Check(cleanupInstance.IsDisposed && AudioServer.BusCount == 2 && player.IsPlaying(), "Cleanup failure releases old instances after new graph commitment.");
            player.PlaybackType = AudioServer.PlaybackType.Sample; player.Play(); AudioServer.SetBusLayout(layout); AudioEffectTests.Wait(native, 20); AudioEffectTests.CheckOutput(native, new Vector2(.2f, -.3f) * Mathf.DBToLinear(-6)); Check(player.IsPlaying(), "Native sample replacement preserves transport.");
            var bytes = native.MixManagedBytes; var calls = FAudioContext.AllocationCalls; AudioEffectTests.Wait(native, 64); Check(native.MixManagedBytes == bytes && FAudioContext.AllocationCalls == calls, "64 warmed post-layout passes allocate zero measured managed bytes/native calls.");
            Console.WriteLine("Audio layout active/paused/sample FAudio replacement and factory rollback passed.");
        }
        finally { tree.Dispose(); Reset(); }
    }
    private sealed class FactoryEffect : AudioEffect
    {
        internal bool ThrowCleanup; internal int FailAt, Calls; internal AudioEffectInstance? Last; internal Action? Callback;
        protected override AudioEffectInstance OnInstantiate() { Callback?.Invoke(); if (++Calls == FailAt) throw new InvalidOperationException("Factory fixture"); return Last = new Pass(this); }
    }
    private sealed class Pass(FactoryEffect owner) : AudioEffectInstance { protected override void OnProcess(ReadOnlySpan<Vector2> source, Span<Vector2> destination) => source.CopyTo(destination); protected override void Dispose(bool disposing) { base.Dispose(disposing); if (disposing && owner.ThrowCleanup) throw new InvalidOperationException("Cleanup fixture"); } }
    private static void FreshProcess(string path)
    {
        var start = new ProcessStartInfo(Environment.ProcessPath!) { RedirectStandardOutput = true, RedirectStandardError = true };
        if (System.IO.Path.GetFileNameWithoutExtension(Environment.ProcessPath) == "dotnet") start.ArgumentList.Add(typeof(AudioBusLayoutTests).Assembly.Location);
        start.Environment["ELECTRON2D_TEST_BUS_LAYOUT_CHILD"] = path;
        using var process = Process.Start(start)!; var output = process.StandardOutput.ReadToEndAsync(); var error = process.StandardError.ReadToEndAsync();
        if (!process.WaitForExit(30000)) { process.Kill(true); throw new TimeoutException("Fresh audio layout process."); }
        Check(process.ExitCode == 0 && output.GetAwaiter().GetResult().Contains("Fresh audio layout process passed"), "Fresh-process layout default: " + error.GetAwaiter().GetResult());
    }
    internal static void RunChild(string path)
    {
        ProjectSettings.Set(ProjectSettings.AudioBusesDefaultBusLayout, path); using var loop = new VerifyLoop(); Engine.Start(loop); Check(loop.Initialized, "Default layout applied before OnInitialize."); Engine.Stop();
        using var snapshot = AudioServer.GenerateBusLayout(); Check(snapshot.Snapshot()[1].Effects[0].Resource is AudioEffectAmplify { VolumeDB: -6 }, "Default file dependencies survive temporary root cleanup."); Reset();
        ProjectSettings.Set(ProjectSettings.AudioBusesDefaultBusLayout, path + ".missing"); using var missingLoop = new VerifyLoop(false); Engine.Start(missingLoop); Engine.Stop();
        var wrongPath = path + ".wrong.e2dres"; using (var wrong = new Resource()) ResourceSaver.Save(wrong, wrongPath);
        ProjectSettings.Set(ProjectSettings.AudioBusesDefaultBusLayout, wrongPath); using var wrongLoop = new VerifyLoop(false); Engine.Start(wrongLoop); Engine.Stop();
        var corruptPath = path + ".corrupt.e2dres"; System.IO.File.WriteAllBytes(corruptPath, System.IO.File.ReadAllBytes(path)[..12]);
        ProjectSettings.Set(ProjectSettings.AudioBusesDefaultBusLayout, corruptPath); using var corruptLoop = new VerifyLoop(false); Reject<InvalidDataException>(() => Engine.Start(corruptLoop)); Check(!corruptLoop.Initialized && Engine.MainLoop is null, "Corrupt default rejects before initialization and releases engine reservation.");
        Console.WriteLine("Fresh audio layout process passed");
    }
    private sealed class VerifyLoop(bool expected = true) : MainLoop
    {
        internal bool Initialized;
        protected override void OnInitialize() { Check(AudioServer.BusCount == (expected ? 3 : 1), "Default/missing layout bus data before loop initialization."); Initialized = true; }
    }
    internal static void RunHost()
    {
        var folder = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "e2d-layout-host-" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(folder);
        var path = System.IO.Path.Combine(folder, "buses.e2dres"); var prior = ProjectSettings.Get(ProjectSettings.AudioBusesDefaultBusLayout); var renderer = ProjectSettings.Get(ProjectSettings.RenderingMethod);
        var backend = Environment.GetEnvironmentVariable("ELECTRON2D_AUDIO_RENDERER") == "compatibility" ? "compatibility" : "gpu";
        ProjectSettings.Set(ProjectSettings.RenderingMethod, backend);
        try
        {
            Reset(); using var gain = new AudioEffectAmplify { VolumeDB = -6 }; using var capture = new AudioEffectCapture { BufferLength = .2f };
            AudioServer.AddBus(); AudioServer.SetBusName(1, "Music"); AudioServer.AddBusEffect(1, gain); AudioServer.AddBusEffect(1, capture);
            using (var layout = AudioServer.GenerateBusLayout()) ResourceSaver.Save(layout, path);
            Reset(); ProjectSettings.Set(ProjectSettings.AudioBusesDefaultBusLayout, path);
            for (var run = 0; run < 2; run++)
            {
                using var stream = AudioEffectTests.Constant(); var window = new Window { Size = new(160, 96) }; var player = new AudioStreamPlayer { Stream = stream, Bus = "Music", Autoplay = true };
                var scenario = new HostScenario(player); window.AddChild(player); window.AddChild(scenario);
                try { Check(Engine.Run(window) == 0 && scenario.Completed && window.IsDisposed, "Public default layout host output and cleanup."); }
                finally { if (!window.IsDisposed) window.Dispose(); Reset(); }
                Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(new { scenario = "bus-layout-host", backend, run, scenario.Completed, cleaned = window.IsDisposed }));
            }
        }
        finally { Reset(); ProjectSettings.Set(ProjectSettings.AudioBusesDefaultBusLayout, prior); ProjectSettings.Set(ProjectSettings.RenderingMethod, renderer); Directory.Delete(folder, true); }
    }
    private sealed class HostScenario(AudioStreamPlayer player) : Node
    {
        internal bool Completed; private double _elapsed; private bool _applied;
        protected override void OnReady() { Check(AudioServer.BusCount == 2 && player.IsPlaying(), "Saved layout precedes autoplay."); ProcessEnabled = true; }
        protected override void OnProcess(double delta)
        {
            _elapsed += delta;
            if (!_applied && _elapsed > .1)
            {
                var playback = player.GetStreamPlayback(); using var layout = AudioServer.GenerateBusLayout(); AudioServer.SetBusLayout(layout); Check(ReferenceEquals(playback, player.GetStreamPlayback()), "Public host graph replacement keeps playback."); _applied = true;
            }
            var capture = (AudioEffectCapture)AudioServer.GetBusEffect(1, 1); var available = capture.GetFramesAvailable();
            if (_applied && available > 0) Completed |= capture.GetBuffer(available).Any(f => f.DistanceTo(new Vector2(.2f, -.3f) * Mathf.DBToLinear(-6)) < .0001f);
            if (_elapsed < .4) return; Check(Completed, "Public saved layout produces expected PCM after replacement."); Tree!.Quit();
        }
    }
    private static void Check(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
    private static void Reject<T>(Action action) where T : Exception { try { action(); } catch (T) { return; } throw new InvalidOperationException("Expected " + typeof(T).Name); }
}
