using Electron2D;
using System.Runtime.InteropServices;
using F = Electron2D.FAudioBindings.FAudio;
using Path = System.IO.Path;

internal static class AudioEffectTests
{
    internal static void Run(bool native = false)
    {
        ResourceAndRing(); HooksAndConcurrency();
        if (native) { NativePairs(); NativeGraph(); NativeFailuresAndActivity(); }
        Console.WriteLine("Audio effects: capture FIFO/counters, hooks, concurrency, ordered native chains and lifetime passed.");
    }
    private static void ResourceAndRing()
    {
        using var settings = new ProjectSettingsRegistry(Directory.GetCurrentDirectory(), Path.Combine(Path.GetTempPath(), "e2d-effect-settings")); Check(settings.Get(ProjectSettings.AudioBusesChannelDisableTime) == 2 && settings.Get(ProjectSettings.AudioBusesChannelDisableThresholdDB) == -60, "Typed bus activity defaults."); Reject<ArgumentOutOfRangeException>(() => settings.Set(ProjectSettings.AudioBusesChannelDisableTime, -1)); Reject<ArgumentException>(() => settings.Set(ProjectSettings.AudioBusesChannelDisableThresholdDB, float.NaN));
        using var capture = new AudioEffectCapture(); Check(capture.BufferLength == .1f && capture.GetBufferLengthFrames() == 0 && capture.GetFramesAvailable() == 0 && capture.CanGetBuffer(0), "Uninitialized capture defaults.");
        Reject<ArgumentOutOfRangeException>(() => capture.GetBuffer(-1)); Reject<ArgumentOutOfRangeException>(() => capture.CanGetBuffer(-1));
        foreach (var value in new[] { 0f, -1, float.NaN, float.PositiveInfinity }) Reject<ArgumentOutOfRangeException>(() => capture.BufferLength = value);
        capture.BufferLength = 128 / AudioServer.GetMixRate(); using var instance = capture.Instantiate();
        Check(capture.GetBufferLengthFrames() == 256 && instance.ProcessSilence(), "Strictly larger power-of-two capture with silence processing.");
        var source = Enumerable.Range(0, 128).Select(i => new Vector2(i, -i)).ToArray(); var output = new Vector2[128]; instance.Process(source, output); Check(output.SequenceEqual(source), "Capture preserves exact finite PCM, including bus values beyond unity.");
        instance.Process(source, output); Check(capture.GetFramesAvailable() == 128 && capture.GetPushedFrames() == 128 && capture.GetDiscardedFrames() == 128, "Whole-block overflow retains earlier data.");
        Check(capture.GetBuffer(int.MaxValue).Length == 0 && capture.GetBuffer(0).Length == 0 && capture.GetFramesAvailable() == 128, "Shortage/empty queries never consume.");
        Check(capture.GetBuffer(64).SequenceEqual(source.Take(64)), "FIFO first half."); instance.Process(source.AsSpan(0, 64), output.AsSpan(0, 64)); Check(capture.GetBuffer(128).SequenceEqual(source.Skip(64).Concat(source.Take(64))), "Wrapped FIFO.");
        source.AsSpan().Fill(new(.4f, -.2f)); instance.Process(source, source); Check(source[0] == new Vector2(.4f, -.2f), "Aliased pass-through.");
        capture.ClearBuffer(); Check(capture.GetPushedFrames() == 320 && capture.GetDiscardedFrames() == 128 && capture.GetFramesAvailable() == 0, "Clear retains counters.");
        capture.BufferLength = 1; using var second = capture.Instantiate(); Check(capture.GetBufferLengthFrames() == 256 && capture.GetPushedFrames() == 320, "Repeated Instantiate retains capacity/counters.");
        using var copy = (AudioEffectCapture)capture.Duplicate(true); Check(copy.BufferLength == 1 && copy.GetBufferLengthFrames() == 0 && copy.GetPushedFrames() == 0, "Configuration-only copy.");
        var full = new Vector2[255]; full.AsSpan().Fill(new(.7f, -.4f)); instance.Process(full, full); Check(capture.GetFramesAvailable() == 255 && capture.GetBuffer(256).Length == 0 && capture.GetBuffer(255).SequenceEqual(full), "Exactly full usable ring remains readable without modulo ambiguity.");
        var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic; typeof(AudioEffectCapture).GetField("_pushed", flags)!.SetValue(capture, long.MaxValue - 1); typeof(AudioEffectCapture).GetField("_discarded", flags)!.SetValue(capture, long.MaxValue - 1); instance.Process(source, output); instance.Process(source, output); Check(capture.GetPushedFrames() == long.MaxValue && capture.GetDiscardedFrames() == long.MaxValue, "Lifetime counters saturate without wrapping negative.");
        Check(capture.GetPropertyList().Any(p => p.Name == nameof(AudioEffectCapture.BufferLength) && !p.IsReadOnly), "Typed stored capture field.");
        for (var i = 0; i < 20; i++) { capture.ClearBuffer(); instance.Process(source, output); }
        var bytes = GC.GetAllocatedBytesForCurrentThread(); for (var i = 0; i < 64; i++) { capture.ClearBuffer(); instance.Process(source, output); _ = capture.CanGetBuffer(128); _ = capture.GetPushedFrames(); }
        Check(GC.GetAllocatedBytesForCurrentThread() == bytes, "64 prepared capture writes/queries allocate zero managed bytes.");
        capture.Dispose(); Reject<ObjectDisposedException>(() => instance.Process(source, output)); Reject<ObjectDisposedException>(capture.ClearBuffer);
        using var tiny = new AudioEffectCapture { BufferLength = .000001f }; using var zero = tiny.Instantiate(); Check(tiny.GetBufferLengthFrames() == 1, "Zero usable capacity."); zero.Process(source.AsSpan(0, 1), output.AsSpan(0, 1)); Check(tiny.GetDiscardedFrames() == 1, "Tiny ring discards whole block.");
        using var large = new AudioEffectCapture { BufferLength = float.MaxValue }; Reject<ArgumentOutOfRangeException>(() => large.Instantiate()); Check(large.GetBufferLengthFrames() == 0, "Oversized initialization is transactional.");
    }
    private static void HooksAndConcurrency()
    {
        using var effect = new ArithmeticEffect(2, new(.1f, -.1f)); using var instance = effect.Instantiate();
        var input = new Vector2[] { new(.2f, -.3f) }; var output = new Vector2[1]; instance.Process(input, output); Check(output[0].DistanceTo(new(.5f, -.7f)) < .00001f && !instance.ProcessSilence(), "Executable custom hooks/default silence policy.");
        Reject<ArgumentException>(() => instance.Process(input, [])); Reject<ArgumentException>(() => instance.Process([new(float.NaN, 0)], output));
        effect.FailOutput = true; Reject<ArgumentException>(() => instance.Process(input, output)); effect.FailOutput = false;
        effect.ReenterInstance = true; Reject<InvalidOperationException>(() => instance.Process(input, output)); effect.ReenterInstance = false;
        effect.ReenterLock = true; Reject<InvalidOperationException>(() => instance.Process(input, output)); effect.ReenterLock = false;
        effect.FailFactory = true; Reject<ApplicationException>(() => effect.Instantiate()); effect.FailFactory = false;
        effect.InvalidFactory = 1; Reject<InvalidOperationException>(() => effect.Instantiate()); effect.InvalidFactory = 2; Reject<InvalidOperationException>(() => effect.Instantiate()); effect.InvalidFactory = 0;
        using var capture = new AudioEffectCapture { BufferLength = .1f }; using var a = capture.Instantiate(); using var b = capture.Instantiate(); var block = new Vector2[64];
        Parallel.Invoke(() => { for (var i = 0; i < 300; i++) a.Process(block, block); }, () => { var other = new Vector2[64]; for (var i = 0; i < 300; i++) b.Process(other, other); }, () => { for (var i = 0; i < 1000; i++) { _ = capture.GetBuffer(64); if (i % 4 == 0) capture.ClearBuffer(); } });
        Check(capture.GetPushedFrames() + capture.GetDiscardedFrames() == 600 * 64, "Concurrent shared capture accounts every whole block.");
    }
    private static unsafe void NativePairs()
    {
        var activity = new FAudioBusEffect.Activity(2, 128, .001f); activity.Begin(64); activity.Use(0); activity.Finish(new float[128], 2); activity.Begin(64); activity.Finish(new float[128], 2); Check(activity.Active[0], "Unused silent bus retains its activity tail."); activity.Begin(128); activity.Finish(new float[256], 2); Check(!activity.Active[0], "Unused silent bus expires only after the prepared timeout.");
        Check(sizeof(F.FAPOBase) == 224 && Marshal.OffsetOf<F.FAPOBase>(nameof(F.FAPOBase.Destructor)).ToInt64() == 112 && sizeof(F.FAPORegistrationProperties) == 1068 && sizeof(F.FAPOProcessBufferParameters) == 16, "Linux x64 pinned C FAPO ABI.");
        foreach (var channels in new[] { 2, 4, 6, 8 })
        {
            using var effect = new ArithmeticEffect(2, new(.1f, -.1f)); using var adapter = new FAudioBusEffect(channels, 16, effect);
            var samples = new float[16 * channels]; for (var i = 0; i < samples.Length; i++) samples[i] = (i % channels + 1) * .02f;
            fixed (float* data = samples)
            {
                var args = new F.FAPOProcessBufferParameters { pBuffer = (nint)data, ValidFrameCount = 16, BufferFlags = F.FAPOBufferFlags.FAPO_BUFFER_SILENT }; var result = args;
                var native = (F.FAPO*)adapter.DangerousGetHandle(); var process = (delegate* unmanaged[Cdecl]<nint, uint, F.FAPOProcessBufferParameters*, uint, F.FAPOProcessBufferParameters*, int, void>)native->Process;
                process(adapter.DangerousGetHandle(), 1, &args, 1, &result, 1);
                for (var c = 0; c < channels; c++) Check(Math.Abs(samples[c] - ((c + 1) * .04f + (c % 2 == 0 ? .1f : -.1f))) < .00001f, "All native interleaved stereo pairs process independently, ignoring incomplete silent hint.");
                Check(adapter.Instances.Distinct().Count() == channels / 2, "Independent pair instances.");
            }
            adapter.Dispose(); adapter.ReleaseInstances();
        }
    }
    private static void NativeGraph()
    {
        var server = AudioServer.Service; server.CloseNative(); AudioServer.BusCount = 1;
        while (AudioServer.GetBusEffectCount(0) > 0) AudioServer.RemoveBusEffect(0, 0);
        AudioServer.AddBus(); AudioServer.SetBusName(1, "Captured");
        using var scale = new ArithmeticEffect(2, Vector2.Zero); using var offset = new ArithmeticEffect(1, new(.1f, .05f)); using var capture = new AudioEffectCapture { BufferLength = .2f };
        var root = new Node(); using var stream = Constant(); var player = new AudioStreamPlayer { Stream = stream, Bus = "Captured" }; root.AddChild(player); using var tree = new SceneTree(root);
        try
        {
            AudioServer.AddBusEffect(1, scale); AudioServer.AddBusEffect(1, capture, int.MaxValue); AudioServer.AddBusEffect(1, offset, 1); Check(AudioServer.GetBusEffectCount(1) == 3 && ReferenceEquals(AudioServer.GetBusEffect(1, 1), offset), "Ordered insertion and append policy.");
            var instance = AudioServer.GetBusEffectInstance(1, 0); Reject<InvalidOperationException>(instance.Dispose); Reject<InvalidOperationException>(() => instance.Process([], [])); Reject<ArgumentOutOfRangeException>(() => AudioServer.GetBusEffectInstance(1, 0, AudioServer.GetBusChannels(1)));
            Reject<ArgumentOutOfRangeException>(() => AudioServer.GetBusEffect(-1, 0)); Reject<ArgumentOutOfRangeException>(() => AudioServer.RemoveBusEffect(1, 3)); Reject<ArgumentNullException>(() => AudioServer.AddBusEffect(1, null!));
            Task.Run(() => Reject<InvalidOperationException>(() => AudioServer.SetBusEffectEnabled(1, 0, false))).GetAwaiter().GetResult();
            player.Play(); var native = server.Native; Wait(native, 20); AudioServer.SetBusVolumeLinear(1, .5f);
            CheckCapture(capture, native, new(.5f, -.55f)); CheckOutput(native, new(.25f, -.275f)); Check(AudioServer.GetBusPeakVolumeLeftDB(1, 0) > -14, "Meter follows public effects and final gain.");
            AudioServer.SetBusMute(1, true); CheckCapture(capture, native, new(.5f, -.55f)); CheckOutput(native, Vector2.Zero); AudioServer.SetBusMute(1, false);
            AudioServer.SetBusBypassEffects(1, true); capture.ClearBuffer(); Wait(native, 8); Check(capture.GetFramesAvailable() == 0 && AudioServer.IsBusEffectEnabled(1, 0) && AudioServer.IsBusBypassingEffects(1), "Bypass suppresses capture without changing enable flags."); CheckOutput(native, new(.1f, -.15f));
            AudioServer.SetBusEffectEnabled(1, 0, false); AudioServer.SetBusBypassEffects(1, false); CheckCapture(capture, native, new(.3f, -.25f)); AudioServer.SetBusEffectEnabled(1, 0, true); Check(ReferenceEquals(instance, AudioServer.GetBusEffectInstance(1, 0)), "Enable/bypass retain instance identity.");
            AudioServer.SetBusSend(1, "Missing"); Check(ReferenceEquals(instance, AudioServer.GetBusEffectInstance(1, 0)), "Routing graph rebuild retains effect identity.");
            AudioServer.SwapBusEffects(1, 0, 1); Check(instance.IsDisposed && !scale.IsDisposed, "Structural edits invalidate old instances, retain borrowed resources."); CheckCapture(capture, native, new(.6f, -.5f));
            using var bad = new ArithmeticEffect(1, Vector2.Zero) { FailFactory = true }; var before = AudioServer.GetBusEffectInstance(1, 0); Reject<ApplicationException>(() => AudioServer.AddBusEffect(1, bad)); Check(AudioServer.GetBusEffectCount(1) == 3 && ReferenceEquals(before, AudioServer.GetBusEffectInstance(1, 0)), "Factory failure preserves live chain.");
            bad.FailFactory = false; bad.ReenterServer = true; Reject<InvalidOperationException>(() => AudioServer.AddBusEffect(1, bad)); Check(AudioServer.GetBusEffectCount(1) == 3, "Factory reentrancy rejects before configuration mutation.");
            Wait(native, 20); var bytes = native.MixManagedBytes; var allocations = FAudioContext.AllocationCalls; Wait(native, 64); Check(native.MixManagedBytes == bytes && FAudioContext.AllocationCalls == allocations, "64 warmed active native effect passes allocate zero measured bytes/calls.");
            AudioServer.SetBusEffectEnabled(1, 0, false); AudioServer.SetBusEffectEnabled(1, 1, false); player.StreamPaused = true; Wait(native, 8); capture.ClearBuffer(); Wait(native, 20); Check(capture.GetFramesAvailable() > 0 && capture.GetBuffer(capture.GetFramesAvailable()).All(f => f == Vector2.Zero), "Capture processes silent/paused bus."); bytes = native.MixManagedBytes; allocations = FAudioContext.AllocationCalls; Wait(native, 64); Check(native.MixManagedBytes == bytes && FAudioContext.AllocationCalls == allocations, "64 paused effect passes allocate zero measured bytes/calls."); player.StreamPaused = false; AudioServer.SetBusEffectEnabled(1, 0, true); AudioServer.SetBusEffectEnabled(1, 1, true);
            scale.FailProcess = true; Wait(native, 4); Reject<AggregateException>(() => tree.ProcessFrame(.01)); tree.ProcessFrame(.01); CheckOutput(native, Vector2.Zero); scale.FailProcess = false;
            AudioServer.RemoveBusEffect(1, 1); CheckCapture(capture, native, new(.3f, -.25f));
            var borrowed = AudioServer.GetBusEffectInstance(1, 0); AudioServer.RemoveBus(1); Check(borrowed.IsDisposed && !capture.IsDisposed && player.IsPlaying(), "Bus removal destroys instances and reroutes source.");
            AudioServer.AddBusEffect(0, capture); CheckCapture(capture, native, new(.2f, -.3f)); var master = AudioServer.GetBusEffectInstance(0, 0); server.CloseNative(); Check(master.IsDisposed && AudioServer.GetBusEffectCount(0) == 1 && !capture.IsDisposed, "Engine closure retains configuration/borrowed resource.");
            var reopened = AudioServer.GetBusEffectInstance(0, 0); Check(!reopened.IsDisposed && !ReferenceEquals(reopened, master), "Cold query prepares fresh instance after output closure."); AudioServer.RemoveBusEffect(0, 0);
        }
        finally { tree.Dispose(); server.CloseNative(); AudioServer.BusCount = 1; while (AudioServer.GetBusEffectCount(0) > 0) AudioServer.RemoveBusEffect(0, 0); server.CloseNative(); }
    }
    private static void CheckCapture(AudioEffectCapture capture, FAudioContext native, Vector2 expected)
    {
        capture.ClearBuffer(); Wait(native, 8); var result = capture.GetBuffer(capture.GetFramesAvailable()); Check(result.Length > 0 && result.Any(f => f.DistanceTo(expected) < .0001f), $"Native capture expected {expected}; got {result.FirstOrDefault()}.");
    }
    private static void NativeFailuresAndActivity()
    {
        var server = AudioServer.Service; var settings = ProjectSettings.Service; var oldTime = ProjectSettings.Get(ProjectSettings.AudioBusesChannelDisableTime); ProjectSettings.Set(ProjectSettings.AudioBusesChannelDisableTime, .03f);
        var root = new Node(); using var silent = Constant(); silent.Data = new byte[44100 * 4]; var player = new AudioStreamPlayer { Stream = silent }; root.AddChild(player); using var tree = new SceneTree(root); using var effect = new ArithmeticEffect(1, Vector2.Zero);
        try
        {
            AudioServer.AddBusEffect(0, effect); var native = server.Native; Wait(native, 4); Check(effect.ProcessCalls == 0, "Inactive silent bus skips default hooks."); player.Play(); Wait(native, 8); Check(effect.ProcessCalls > 0, "Active silent source still invokes hooks.");
            player.StreamPaused = true; Wait(native, 12); var count = effect.ProcessCalls; Wait(native, 8); Check(effect.ProcessCalls == count, "Stopped source tail expires using actual mix frames and configured timeout.");
            player.StreamPaused = false; effect.FailSilence = true; player.Stop(); Wait(native, 12); Reject<AggregateException>(() => tree.ProcessFrame(.01)); effect.FailSilence = false; AudioServer.RemoveBusEffect(0, 0);
            using var reused = new ArithmeticEffect(1, Vector2.Zero); AudioServer.AddBusEffect(0, reused); var borrowed = AudioServer.GetBusEffectInstance(0, 0); reused.Reuse = borrowed; Reject<InvalidOperationException>(() => AudioServer.AddBusEffect(0, reused)); Check(AudioServer.GetBusEffectCount(0) == 1 && !borrowed.IsDisposed, "Reused attached factory state rejects without disposing live instance."); reused.Reuse = null;
            reused.FailDispose = true; Reject<AggregateException>(() => AudioServer.RemoveBusEffect(0, 0)); Check(borrowed.IsDisposed && AudioServer.GetBusEffectCount(0) == 0, "Disposal-hook failure completes removal and logical cleanup.");
            using var disposed = new AudioEffectCapture(); AudioServer.AddBusEffect(0, disposed); disposed.Dispose(); Wait(native, 4); Reject<AggregateException>(() => tree.ProcessFrame(.01)); AudioServer.RemoveBusEffect(0, 0);
            AudioServer.SetBusVolumeDB(0, float.MaxValue); Wait(native, 4); Reject<AggregateException>(() => tree.ProcessFrame(.01)); AudioServer.SetBusVolumeDB(0, 0); Wait(native, 4); tree.ProcessFrame(.01);
        }
        finally { tree.Dispose(); server.CloseNative(); while (AudioServer.GetBusEffectCount(0) > 0) AudioServer.RemoveBusEffect(0, 0); server.CloseNative(); ProjectSettings.Set(ProjectSettings.AudioBusesChannelDisableTime, oldTime); }
    }
    internal static void RunHost()
    {
        var backend = Environment.GetEnvironmentVariable("ELECTRON2D_AUDIO_RENDERER") == "compatibility" ? "compatibility" : "gpu"; var settings = ProjectSettings.Service; var previous = ProjectSettings.Get(ProjectSettings.RenderingMethod); ProjectSettings.Set(ProjectSettings.RenderingMethod, backend); var fps = Engine.MaxFPS; Engine.MaxFPS = 60;
        try
        {
            for (var run = 0; run < 3; run++)
            {
                using var stream = Constant(); using var capture = new AudioEffectCapture { BufferLength = .1f }; var window = new Window { Size = new(160, 96), Title = "Electron2D bus capture" }; var player = new AudioStreamPlayer { Stream = stream, Autoplay = true }; var scenario = new HostScenario(player, capture, run == 2); window.AddChild(player); window.AddChild(scenario);
                AudioServer.AddBusEffect(0, capture);
                try
                {
                    if (run == 2) { var failed = false; try { Engine.Run(window); } catch (AggregateException error) { failed = error.Flatten().InnerExceptions.Any(e => e.Message == "Effect host failure fixture."); } Check(failed && window.IsDisposed, "Failed public host cleanup."); }
                    else Check(Engine.Run(window) == 0 && scenario.Completed && window.IsDisposed && !stream.IsDisposed, "Public effect/capture host lifecycle.");
                    Check(scenario.Instance!.IsDisposed && !capture.IsDisposed, "Host releases bus instances, borrows capture resource.");
                    Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(new { scenario = "audio-effect-host", backend, run, scenario.Frames, cleaned = window.IsDisposed }));
                }
                finally { if (!window.IsDisposed) window.Dispose(); AudioServer.RemoveBusEffect(0, 0); AudioServer.Service.CloseNative(); }
            }
        }
        finally { Engine.MaxFPS = fps; ProjectSettings.Set(ProjectSettings.RenderingMethod, previous); }
    }
    private sealed class HostScenario(AudioStreamPlayer player, AudioEffectCapture capture, bool fail) : Node
    {
        internal bool Completed; internal int Frames; internal AudioEffectInstance? Instance; private double _elapsed;
        protected override void OnReady() { Instance = AudioServer.GetBusEffectInstance(0, 0); ProcessEnabled = true; }
        protected override void OnProcess(double delta)
        {
            _elapsed += delta; var available = capture.GetFramesAvailable(); if (available > 0) { var pcm = capture.GetBuffer(available); Frames += pcm.Length; Completed |= pcm.Any(f => f.DistanceTo(new(.2f, -.3f)) < .0001f); }
            if (_elapsed < .3) return; Check(player.IsPlaying() && Completed && Frames > 0, "Public raw capture receives playing source PCM."); if (fail) throw new ApplicationException("Effect host failure fixture."); Tree!.Quit();
        }
    }
    internal static void CheckOutput(FAudioContext native, Vector2 expected)
    {
        Wait(native, 4); native.PrepareCapture(native.QuantumFrames * native.Channels * 8); Wait(native, 10); var pcm = native.CapturedPCM(); Check(pcm.Length > 0, "Actual native output captured.");
        for (var i = 0; i < pcm.Length; i += native.Channels) Check(new Vector2(pcm[i], pcm[i + 1]).DistanceTo(expected) < .0001f, $"Native output expected {expected}; got {pcm[i]}, {pcm[i + 1]}.");
    }
    internal static AudioStreamWAV Constant()
    {
        var frames = new Vector2[44100]; frames.AsSpan().Fill(new(.2f, -.3f));
        return new AudioStreamWAV { SampleFormat = AudioStreamWAV.Format.PCM16, Stereo = true, MixRate = 44100, Data = PCM16(frames), Loop = AudioLoopMode.Forward, LoopEnd = frames.Length - 1 };
    }
    private static byte[] PCM16(Vector2[] frames)
    {
        var values = new short[frames.Length * 2]; for (var i = 0; i < frames.Length; i++) { values[i * 2] = (short)(frames[i].X * 32768); values[i * 2 + 1] = (short)(frames[i].Y * 32768); }
        return MemoryMarshal.AsBytes(values.AsSpan()).ToArray();
    }
    internal static void Wait(FAudioContext native, int passes)
    {
        var target = native.MixPasses + passes; var start = System.Diagnostics.Stopwatch.GetTimestamp(); while (native.MixPasses < target) { if (System.Diagnostics.Stopwatch.GetElapsedTime(start).TotalSeconds > 5) throw new InvalidOperationException("Effect native mix deadline."); Thread.Sleep(1); }
    }
    private sealed class ArithmeticEffect(float scale, Vector2 offset) : AudioEffect
    {
        internal bool FailFactory, FailProcess, FailOutput, ReenterInstance, ReenterServer, ReenterLock, FailSilence, FailDispose;
        internal int ProcessCalls;
        internal int InvalidFactory;
        internal AudioEffectInstance? Reuse;
        protected override AudioEffectInstance OnInstantiate() { if (FailFactory) throw new ApplicationException("Effect factory fixture."); if (ReenterServer) AudioServer.AddBus(); if (InvalidFactory == 1) return null!; var instance = Reuse ?? new ArithmeticInstance(this, scale, offset); if (InvalidFactory == 2) instance.Dispose(); return instance; }
        protected override Resource CreateDuplicateInstance() => new ArithmeticEffect(scale, offset);
    }
    private sealed class ArithmeticInstance(ArithmeticEffect source, float scale, Vector2 offset) : AudioEffectInstance
    {
        protected override void OnProcess(ReadOnlySpan<Vector2> input, Span<Vector2> output)
        {
            Interlocked.Increment(ref source.ProcessCalls);
            if (source.FailProcess) throw new ApplicationException("Effect process fixture."); if (source.ReenterInstance) ProcessSilence();
            if (source.ReenterLock) AudioServer.Unlock();
            for (var i = 0; i < input.Length; i++) output[i] = source.FailOutput ? new(float.NaN, 0) : input[i] * scale + offset;
        }
        protected override bool OnProcessSilence() { if (source.FailSilence) throw new ApplicationException("Effect silence fixture."); return false; }
        protected override void Dispose(bool disposing) { base.Dispose(disposing); if (source.FailDispose) throw new ApplicationException("Effect disposal fixture."); }
    }
    private static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    private static void Reject<T>(Action action) where T : Exception { try { action(); } catch (T) { return; } throw new InvalidOperationException($"Expected {typeof(T).Name}."); }
}
