using Electron2D;
using RateMode = Electron2D.AudioStreamGenerator.AudioStreamGeneratorMixRate;

internal static class AudioGeneratorTests
{
    internal static void Run(bool native = false)
    {
        VerifyResource(); VerifyQueue(); VerifyResampling(); VerifyConcurrency(); VerifyWarm();
        if (native) VerifyNative();
        Console.WriteLine("Generator resource, bounded FIFO, partial-read/wrap, controls, resampling, concurrency and warm checks passed.");
    }
    private static AudioStreamGeneratorPlayback Playback(AudioStreamGenerator stream) => (AudioStreamGeneratorPlayback)stream.InstantiatePlayback();
    private static void VerifyResource()
    {
        using var stream = new AudioStreamGenerator(); Check(stream.MixRate == 44100 && stream.BufferLength == .5f && stream.MixRateMode == RateMode.Custom && stream.GetLength() == 0 && stream.IsMonophonic(), "Generator defaults/duration/monophony.");
        using var playback = Playback(stream); Check(playback.GetFramesAvailable() == 32767, "Default strictly larger power-of-two storage, one reserved slot.");
        foreach (var count in new[] { 0, 1, 127, 128, 129 })
        {
            stream.MixRate = 128; stream.BufferLength = count == 0 ? .001f : count / 128f;
            using var p = Playback(stream); Check(p.GetFramesAvailable() == (count == 0 ? 0 : count == 1 ? 1 : count == 127 ? 127 : 255), "Exact power-of-two sizing boundary.");
        }
        foreach (var value in new[] { 0f, -1, float.NaN, float.PositiveInfinity }) { Reject<ArgumentOutOfRangeException>(() => stream.MixRate = value); Reject<ArgumentOutOfRangeException>(() => stream.BufferLength = value); }
        Reject<ArgumentOutOfRangeException>(() => stream.MixRateMode = RateMode.Max); Check(stream.MixRateMode == RateMode.Custom, "Invalid mode rejects before commit.");
        stream.MixRate = float.MaxValue; Reject<ArgumentOutOfRangeException>(() => stream.InstantiatePlayback()); stream.MixRate = 1 << 24; stream.BufferLength = 1; Reject<ArgumentOutOfRangeException>(() => stream.InstantiatePlayback());
        stream.MixRate = 22050; stream.BufferLength = .1f; stream.ResourceLocalToScene = true;
        using var copy = (AudioStreamGenerator)stream.Duplicate(true); Check(copy.MixRate == 22050 && copy.BufferLength == .1f, "Exact generator configuration copying.");
        using var player = new AudioStreamPlayer { Stream = stream }; using var scene = new PackedScene(); scene.Pack(player); using var restored = (AudioStreamPlayer)scene.Instantiate();
        var local = (AudioStreamGenerator)restored.Stream!; Check(!ReferenceEquals(local, stream) && local.MixRate == 22050 && local.BufferLength == .1f, "Packed scene-local producer configuration."); local.Dispose();
        stream.MixRateMode = RateMode.Output; stream.MixRate = 1; using var output = Playback(stream); Check(output.GetFramesAvailable() == 8191, "Output mode ignores custom rate at current 44100 Hz baseline.");
        Action<Resource> fail = _ => throw new ApplicationException("Generator observer failure"); stream.Changed += fail;
        Reject<ApplicationException>(() => stream.MixRate = 32000); Check(stream.MixRate == 32000, "Configuration commits before Changed failure."); stream.Changed -= fail;
        stream.Dispose(); Reject<ObjectDisposedException>(() => output.Start()); Reject<ObjectDisposedException>(() => output.PushFrame(Vector2.Zero)); output.Stop();
    }
    private static void VerifyQueue()
    {
        using var source = new AudioStreamGenerator { MixRate = 128, BufferLength = 1 }; using var p = Playback(source);
        var input = Enumerable.Range(0, 255).Select(i => new Vector2(i, -i)).ToArray(); var scratch = new Vector2[128];
        Check(p.CanPushBuffer(255) && !p.CanPushBuffer(256) && p.CanPushBuffer(0), "Capacity and empty availability."); Reject<ArgumentOutOfRangeException>(() => p.CanPushBuffer(-1));
        Check(p.PushBuffer(input) && p.GetFramesAvailable() == 0 && !p.PushFrame(Vector2.Zero) && !p.PushBuffer([Vector2.Zero]), "Full queue rejects whole writes.");
        Reject<ArgumentException>(() => p.PushBuffer([Vector2.Zero, new(float.NaN, 0)])); Reject<ArgumentException>(() => p.PushFrame(new(0, float.PositiveInfinity)));
        p.Start(123); Check(p.GetFramesAvailable() == 0 && p.GetPlaybackPosition() == 0 && p.GetSkips() == 0, "First Start ignores position and retains queued frames.");
        Reject<InvalidOperationException>(p.ClearBuffer);
        // Direct protected callback probing separates source FIFO behavior from the shared 128-frame cubic history.
        var mix = typeof(AudioStreamGeneratorPlayback).GetMethod("OnMixResampled", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!.CreateDelegate<SourceMix>(p);
        Check(mix(scratch) == 128 && scratch[0] == new Vector2(0, 0) && scratch[127] == new Vector2(127, -127), "FIFO first segment.");
        Check(p.GetFramesAvailable() == 128 && !p.PushBuffer(input), "Overflow leaves remaining FIFO intact.");
        Check(p.PushBuffer(input.AsSpan(0, 128)), "Wrapped second write."); input.AsSpan(0, 128).Fill(new(-999, -999));
        mix(scratch); Check(scratch[0] == new Vector2(128, -128) && scratch[126] == new Vector2(254, -254) && scratch[127] == Vector2.Zero, "Wrapped read order and copied input.");
        mix(scratch); Check(scratch[0] == new Vector2(1, -1) && scratch[126] == new Vector2(127, -127) && scratch[127] == Vector2.Zero && p.GetSkips() == 1, "Partial final source block zero-fills once and remains active.");
        var position = p.GetPlaybackPosition(); p.Seek(-999); Check(p.GetPlaybackPosition() == position && p.IsPlaying(), "Generated stream seek is a no-op.");
        p.Stop(); var skips = p.GetSkips(); p.ClearBuffer(); Check(p.GetFramesAvailable() == 255 && p.GetPlaybackPosition() == 0 && p.GetSkips() == skips, "Stopped clear resets queue/time, preserving skips.");
        p.Start(); Check(p.GetSkips() == 0, "Start resets underrun count.");
        typeof(AudioStreamGeneratorPlayback).GetField("_skips", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!.SetValue(p, int.MaxValue - 1);
        mix(scratch); mix(scratch); Check(p.GetSkips() == int.MaxValue, "Underrun counter saturates without signed overflow.");
        p.Stop(); scratch.AsSpan().Fill(Vector2.One); Check(p.MixInto(scratch, 1) == 0 && scratch.All(v => v == Vector2.Zero), "Stopped engine mixing clears output and reports inactive.");
        Reject<ArgumentOutOfRangeException>(() => p.Start(double.NaN)); Reject<ArgumentOutOfRangeException>(() => p.Seek(double.PositiveInfinity));
        p.Dispose(); Reject<ObjectDisposedException>(() => p.GetFramesAvailable()); Reject<ObjectDisposedException>(() => p.PushBuffer([])); Reject<ObjectDisposedException>(p.ClearBuffer);
    }
    private delegate int SourceMix(Span<Vector2> output);
    private static void VerifyResampling()
    {
        using var source = new AudioStreamGenerator { MixRate = 44100, BufferLength = .1f }; using var p = Playback(source);
        var feed = new Vector2[600]; for (var i = 0; i < feed.Length; i++) feed[i] = new(i / 1000f, -i / 1000f);
        p.PushBuffer(feed); p.Start(); var output = p.MixAudio(1, 390);
        Check(output.Length == 390 && output.Take(130).All(v => v == Vector2.Zero), "Generated silence/history starts without a false stream end.");
        for (var i = 130; i < output.Length; i++) Check(output[i] == feed[i - 130], "Independent unit-rate cubic history/order oracle.");
        Check(Math.Abs(p.GetPlaybackPosition() - 384 / 44100d) < 1e-12 && p.GetSkips() == 0, "Position counts generated source blocks including prefetch.");
        p.Stop(); p.ClearBuffer(); p.PushBuffer(feed); p.Start(); var faster = p.MixAudio(2, 260);
        Check(faster[80] == feed[30] && faster.Length == 260, "Local pitch consumes two source frames per output frame.");
        p.Stop(); p.ClearBuffer(); p.PushBuffer(feed); source.MixRate = 22050; p.Start(); var slow = p.MixAudio(1, 390); Check(Math.Abs(slow[300].X - feed[20].X) < 1e-6, "Live source frequency changes resampling ratio.");
        var held = p.GetPlaybackPosition(); p.MixAudio(0, 260); Check(p.GetPlaybackPosition() == held, "Zero pitch freezes source demand.");
        var free = p.GetFramesAvailable(); p.Stop(); p.Start(); Check(p.GetFramesAvailable() == free && p.GetPlaybackPosition() == 0, "Nonzero-time restart retains queue and resets position.");
        source.BufferLength = .2f; Check(p.GetFramesAvailable() == free, "Existing capacity remains fixed after resource duration edits.");
        p.Stop(); p.ClearBuffer(); p.Start(); Check(p.MixAudio(1, 2048).Length == 2048 && p.IsPlaying() && p.GetSkips() > 0, "Persistent underrun returns silence, not natural completion.");
    }
    private static void VerifyConcurrency()
    {
        using var source = new AudioStreamGenerator { MixRate = 44100, BufferLength = .1f }; using var p = Playback(source); p.Start();
        var producer = Task.Run(() => { for (var i = 0; i < 2000; i++) p.PushFrame(new(.1f, -.1f)); });
        var consumer = Task.Run(() => { var frames = new Vector2[64]; for (var i = 0; i < 1000; i++) { p.MixInto(frames, 1); Check(frames.All(v => v.IsFinite()), "Concurrent producer PCM remains finite."); } });
        var reset = Task.Run(() => { for (var i = 0; i < 200; i++) p.BeginResample(); });
        Task.WaitAll(producer, consumer, reset); Check(p.GetFramesAvailable() is >= 0 and <= 8191, "Shared queue/history serialization preserves bounds.");
    }
    private static void VerifyWarm()
    {
        using var source = new AudioStreamGenerator { MixRate = 44100, BufferLength = .1f }; using var p = Playback(source);
        var feed = new Vector2[128]; feed.AsSpan().Fill(new(.2f, -.2f)); var output = new Vector2[128]; p.Start();
        for (var i = 0; i < 20; i++) { p.PushBuffer(feed); p.MixInto(output, 1); }
        var before = GC.GetAllocatedBytesForCurrentThread(); for (var i = 0; i < 64; i++) { Check(p.PushBuffer(feed), "Prepared producer capacity."); p.MixInto(output, 1); p.GetFramesAvailable(); p.GetSkips(); }
        Check(GC.GetAllocatedBytesForCurrentThread() == before, "64 active push/resample/query cycles allocate zero bytes.");
        p.Stop(); before = GC.GetAllocatedBytesForCurrentThread(); for (var i = 0; i < 64; i++) p.MixInto(output, 1);
        Check(GC.GetAllocatedBytesForCurrentThread() == before, "64 stopped generator mixes allocate zero bytes.");
    }
    private static void VerifyNative()
    {
        using var stream = new AudioStreamGenerator { MixRateMode = RateMode.Output, BufferLength = .1f }; var root = new Node(); var player = new AudioStreamPlayer { Stream = stream }; root.AddChild(player); using var tree = new SceneTree(root);
        player.Play(); var p = (AudioStreamGeneratorPlayback)player.GetStreamPlayback(); var native = AudioServer.Service.Native;
        var feed = new Vector2[512]; feed.AsSpan().Fill(new(.2f, -.15f));
        void Refill() { while (p.CanPushBuffer(feed.Length)) p.PushBuffer(feed); }
        Refill(); Wait(native, 20, Refill); native.PrepareCapture(16000); Wait(native, 20, Refill);
        var pcm = native.CapturedPCM(); Check(pcm.Where((_, i) => i % native.Channels == 0).Any(v => Math.Abs(v - .2f) < .01f) && pcm.Where((_, i) => i % native.Channels == 1).Any(v => Math.Abs(v + .15f) < .01f), "Actual FAudio output has distinct generated stereo PCM.");
        var bytes = native.MixManagedBytes; var calls = FAudioContext.AllocationCalls; Wait(native, 64, Refill);
        Check(native.MixManagedBytes == bytes && FAudioContext.AllocationCalls == calls, "64 active FAudio passes allocate zero measured managed/native calls.");
        Wait(native, 30); tree.ProcessFrame(.01); Check(player.IsPlaying() && p.GetSkips() > 0, "Native underrun does not finish the generator."); Refill(); Wait(native, 20, Refill);
        player.StreamPaused = true; Wait(native, 20); bytes = native.MixManagedBytes; calls = FAudioContext.AllocationCalls; Wait(native, 64);
        Check(native.MixManagedBytes == bytes && FAudioContext.AllocationCalls == calls, "64 paused FAudio passes allocate zero measured bytes/calls.");
        player.Stop(); p.ClearBuffer(); player.Play(); Check(p.IsDisposed && !stream.IsDisposed, "Repeated Play replaces borrowed handle and retains stream.");
    }
    private static void Wait(FAudioContext native, long passes, Action? refill = null)
    {
        var target = native.MixPasses + passes; var started = System.Diagnostics.Stopwatch.GetTimestamp();
        while (native.MixPasses < target) { refill?.Invoke(); if (System.Diagnostics.Stopwatch.GetElapsedTime(started).TotalSeconds > 5) throw new InvalidOperationException("Native generator mix deadline."); Thread.Sleep(1); }
    }
    internal static void RunHost()
    {
        var backend = Environment.GetEnvironmentVariable("ELECTRON2D_AUDIO_RENDERER") == "compatibility" ? "compatibility" : "gpu";
        var settings = ProjectSettings.Service; var previous = ProjectSettings.Get(ProjectSettings.RenderingMethod); ProjectSettings.Set(ProjectSettings.RenderingMethod, backend);
        var fps = Engine.MaxFPS; Engine.MaxFPS = 60;
        try
        {
            for (var run = 0; run < 2; run++)
            {
                using var stream = new AudioStreamGenerator { MixRate = 22050, BufferLength = .1f }; var window = new Window { Size = new(160, 96), Title = "Electron2D generated audio" };
                var player = new AudioStreamPlayer { Stream = stream, Autoplay = true, VolumeDB = -24 }; var scenario = new Scenario(player); window.AddChild(player); window.AddChild(scenario);
                Check(Engine.Run(window) == 0 && scenario.Completed && window.IsDisposed && !stream.IsDisposed, "Public generator host lifecycle.");
                Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(new { scenario = "audio-generator-host", backend, run, scenario.Position, cleaned = window.IsDisposed }));
            }
        }
        finally { Engine.MaxFPS = fps; ProjectSettings.Set(ProjectSettings.RenderingMethod, previous); }
    }
    private sealed class Scenario(AudioStreamPlayer player) : Node
    {
        private readonly Vector2[] _feed = new Vector2[256]; private AudioStreamGeneratorPlayback? _playback; private double _elapsed, _phase;
        internal bool Completed; internal double Position;
        protected override void OnReady() { _playback = (AudioStreamGeneratorPlayback)player.GetStreamPlayback(); ProcessEnabled = true; Fill(); }
        private void Fill()
        {
            while (_playback!.CanPushBuffer(_feed.Length))
            {
                for (var i = 0; i < _feed.Length; i++) { _feed[i] = new((float)Math.Sin(_phase), (float)Math.Sin(_phase)); _phase = (_phase + Math.Tau * 440 / 22050) % Math.Tau; }
                _playback.PushBuffer(_feed);
            }
        }
        protected override void OnProcess(double delta) { Fill(); _elapsed += delta; if (_elapsed < .3) return; Position = player.GetPlaybackPosition(); Completed = Position > .1 && player.IsPlaying(); Tree!.Quit(); }
    }
    private static void Reject<T>(Action action) where T : Exception { try { action(); } catch (T) { return; } throw new InvalidOperationException("Expected " + typeof(T).Name); }
    private static void Check(bool value, string text) { if (!value) throw new InvalidOperationException(text); }
}
