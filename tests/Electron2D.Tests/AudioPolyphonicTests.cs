using System.Buffers.Binary;
using Electron2D;

internal static class AudioPolyphonicTests
{
    internal static void Run(bool native = false)
    {
        CPU(); Failures(); Warm(); if (native) { Native(); Nested(); }
        Console.WriteLine("Dynamic polyphonic IDs, streamed PCM/ramps, ownership/copies/failures and warmed CPU" + (native ? "/native sample/stream controls" : "") + " passed.");
    }
    private static void CPU()
    {
        using var source = new AudioStreamPolyphonic(); Check(source.Polyphony == 32 && source.IsMonophonic() && source.IsMetaStream() && source.GetLength() == 0 && !source.CanBeSampled() && source.GetParameterList().Length == 0, "Resource defaults/meta policy.");
        Reject<ArgumentOutOfRangeException>(() => source.Polyphony = 129); Reject<ArgumentOutOfRangeException>(() => source.Polyphony = -1);
        using var copied = (AudioStreamPolyphonic)source.Duplicate(); Check(copied.Polyphony == 32, "Copied configuration."); source.Polyphony = 2; using var playback = (AudioStreamPlaybackPolyphonic)source.InstantiatePlayback(); source.Polyphony = 0;
        using var empty = (AudioStreamPlaybackPolyphonic)source.InstantiatePlayback(); empty.Start(); Check(empty.IsPlaying() && empty.PlayStream(source) == -1, "Captured zero capacity is active indefinite silence.");
        using var a = new Probe(new(.2f, .4f)); using var b = new Probe(new(.1f, .3f)); var output = new Vector2[256];
        var first = playback.PlayStream(a, 3); var second = playback.PlayStream(b, 2, (float)Mathf.LinearToDB(.5), 2); Check(first == 1 && second == ((1L << 32) | 2) && playback.PlayStream(a) == -1, "Parent-local encoded IDs and captured capacity.");
        Check(playback.PlayStream(null) == -1 && a.Last!.Starts == 0 && playback.MixInto(output, 1) == 0, "Inactive parent defers streamed starts."); playback.Start(-5); playback.MixInto([], 1); Check(a.Last!.Starts == 0, "Zero mix does not consume pending start."); playback.MixInto(output, 3);
        Near(output[0], new(.25f, .55f)); Check(a.Last.Offset == 3 && b.Last!.Offset == 2 && b.Last.Rate == 6 && playback.GetLoopCount() == 0 && playback.GetPlaybackPosition() == 0, "Offsets/rates and indefinite parent queries."); playback.Seek(99); Check(a.Last.Offset == 3, "Parent seek is ignored.");
        playback.SetStreamVolume(first, float.NegativeInfinity); playback.MixInto(output, 1); Near(output[0], new(.25f, .55f)); Near(output[255], new(.05f + .2f / 256, .15f + .4f / 256)); playback.MixInto(output, 1); Near(output[0], new(.05f, .15f));
        playback.StopStream(second); Check(!playback.IsStreamPlaying(second), "Stop invalidates ID synchronously."); playback.MixInto(output, 1); Near(output[0], new(.05f, .15f)); Check(!b.Last!.Active && playback.PlayStream(b) != second && !playback.IsStreamPlaying(second), "Final fade stops child; recycled slot rejects stale ID.");
        var current = b.Last!; playback.Stop(); Check(!playback.IsPlaying() && a.Last.IsDisposed && current.IsDisposed && !a.IsDisposed && !b.IsDisposed, "Parent stop consumes children and borrows resources.");
        playback.Start(); a.Frames = 3; var finite = playback.PlayStream(a); playback.MixInto(output, 1); Check(!playback.IsStreamPlaying(finite) && playback.IsPlaying() && output.Skip(3).All(v => v == Vector2.Zero), "Short read clears child, pads remainder and retains parent active.");
        var pending = playback.PlayStream(b); playback.StopStream(pending); var starts = b.Last!.Starts; playback.MixInto(output, 1); Check(b.Last.Starts == starts, "A pending stopped stream never starts.");
        playback.SetStreamVolume(-1, float.NaN); playback.SetStreamPitchScale(-1, float.NaN); playback.StopStream(long.MaxValue); Check(!playback.IsStreamPlaying(long.MinValue), "Invalid/malformed IDs no-op.");
        playback.Stop(); playback.Start(); typeof(AudioStreamPlaybackPolyphonic).GetField("_generation", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!.SetValue(playback, uint.MaxValue);
        var wrappedA = playback.PlayStream(a); var wrappedB = playback.PlayStream(b); Check(wrappedA == uint.MaxValue && wrappedB == (1L << 32) && playback.IsStreamPlaying(wrappedB), "UInt32 generation wrap retains valid zero generations and slot encoding.");
        using var window = new Window(); var player = new AudioStreamPlayer { Stream = copied, Owner = null }; window.AddChild(player); player.Owner = window; using var packed = new PackedScene(); packed.Pack(window); using var instance = (Window)packed.Instantiate(); Check(ReferenceEquals(instance.GetChildren().OfType<AudioStreamPlayer>().Single().Stream, copied), "PackedScene borrows meta resource; resource snapshots own only capacity.");
    }
    private static void Failures()
    {
        using var source = new AudioStreamPolyphonic { Polyphony = 2 }; using var playback = (AudioStreamPlaybackPolyphonic)source.InstantiatePlayback(); using var probe = new Probe(Vector2.One); var output = new Vector2[256]; playback.Start();
        Reject<InvalidOperationException>(() => playback.PlayStream(source)); probe.Factory = () => playback.PlayStream(probe); Reject<InvalidOperationException>(() => playback.PlayStream(probe)); probe.Factory = null;
        var id = playback.PlayStream(probe); Reject<ArgumentOutOfRangeException>(() => playback.SetStreamVolume(id, 1000)); Reject<ArgumentOutOfRangeException>(() => playback.SetStreamPitchScale(id, float.NaN));
        probe.Hook = () => playback.Dispose(); Reject<InvalidOperationException>(() => playback.MixInto(output, 1)); Check(!playback.IsDisposed && !playback.IsPlaying() && !probe.Last!.Active, "Mix reentry rejects and silences/stops every voice."); probe.Hook = null;
        playback.Stop(); playback.Start(); id = playback.PlayStream(probe); probe.Hook = () => source.Dispose(); Reject<InvalidOperationException>(() => playback.MixInto(output, 1)); Check(!source.IsDisposed, "Source callback disposal rejects before logical disposal."); probe.Hook = null; playback.Stop();
        probe.Nonfinite = true; playback.Start(); playback.PlayStream(probe); Reject<ArithmeticException>(() => playback.MixInto(output, 1)); Check(output.All(v => v == Vector2.Zero), "Nonfinite PCM cannot escape the failed aggregate."); probe.Nonfinite = false; playback.Stop();
        probe.FailStop = true; playback.Start(); playback.PlayStream(probe); playback.MixInto(output, 1); Reject<Exception>(() => playback.Stop()); Check(probe.Last!.IsDisposed && !playback.IsPlaying(), "Stop callback failure still consumes owned child."); probe.FailStop = false;
        var shared = new Probe(Vector2.One) { Reuse = true }; using (shared) { playback.Start(); playback.PlayStream(shared); Reject<InvalidOperationException>(() => playback.PlayStream(shared)); Check(!shared.Last!.IsDisposed, "Duplicate factory state rejects without consuming the first owner."); }
        playback.Stop();
    }
    private static void Warm()
    {
        using var source = new AudioStreamPolyphonic { Polyphony = 128 }; using var probe = new Probe(new(.001f, -.001f)); using var playback = (AudioStreamPlaybackPolyphonic)source.InstantiatePlayback(); var ids = new long[128]; var output = new Vector2[257]; playback.Start(); for (var i = 0; i < ids.Length; i++) ids[i] = playback.PlayStream(probe);
        for (var i = 0; i < 20; i++) playback.MixInto(output, 1); var before = GC.GetAllocatedBytesForCurrentThread(); for (var i = 0; i < 64; i++) { playback.SetStreamVolume(ids[i], i % 2 == 0 ? 0 : -6); playback.SetStreamPitchScale(ids[i], 1); playback.MixInto(output, 1); }
        Check(GC.GetAllocatedBytesForCurrentThread() == before, "128 voices/64 warmed scalar and 257-frame mix operations allocate zero managed bytes.");
        playback.Stop(); playback.Start(); for (var i = 0; i < 20; i++) playback.MixInto(output, 1); before = GC.GetAllocatedBytesForCurrentThread(); for (var i = 0; i < 64; i++) playback.MixInto(output, 1); Check(GC.GetAllocatedBytesForCurrentThread() == before, "64 warmed active-empty mixer passes allocate zero managed bytes.");
    }
    private static void Native()
    {
        using var source = new AudioStreamPolyphonic { Polyphony = 2 }; using var wave = Tone(); var root = new Node(); var player = new AudioStreamPlayer { Stream = source }; root.AddChild(player); using var tree = new SceneTree(root); player.Play(); var playback = (AudioStreamPlaybackPolyphonic)player.GetStreamPlayback(); Reject<InvalidOperationException>(() => playback.Dispose());
        var id = playback.PlayStream(wave, playbackType: AudioServer.PlaybackType.Sample); var native = AudioServer.Service.Native; Wait(native, 12); Check(Capture(native).Any(v => Math.Abs(v) > .03), "Native child sample PCM.");
        playback.SetStreamVolume(id, float.NegativeInfinity); Check(Capture(native).All(v => Math.Abs(v) < 1e-6), "Native child gain control."); playback.SetStreamVolume(id, 0); player.StreamPaused = true; Check(Capture(native).All(v => Math.Abs(v) < 1e-6), "Parent pause stops native child output."); player.StreamPaused = false; player.VolumeDB = float.NegativeInfinity; Check(Capture(native).All(v => Math.Abs(v) < 1e-6), "Parent gain reaches native child matrix."); player.VolumeDB = 0;
        playback.SetStreamPitchScale(id, 2); Reject<NotSupportedException>(() => playback.SetStreamPitchScale(id, 2048)); Check(playback.IsStreamPlaying(id), "Native ratio rejection preserves active state.");
        for (var i = 0; i < 20; i++) { playback.SetStreamVolume(id, i % 2 == 0 ? 0 : -6); playback.SetStreamPitchScale(id, i % 2 == 0 ? 1 : 2); Wait(native, 1); }
        var bytes = native.MixManagedBytes; var calls = FAudioContext.AllocationCalls; var ownerBytes = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < 64; i++) { playback.SetStreamVolume(id, i % 2 == 0 ? 0 : -6); playback.SetStreamPitchScale(id, i % 2 == 0 ? 1 : 2); Wait(native, 1); }
        Check(native.MixManagedBytes == bytes && FAudioContext.AllocationCalls == calls && GC.GetAllocatedBytesForCurrentThread() == ownerBytes, "64 warmed native scalar/output passes allocate zero measured owner/callback bytes and custom native calls.");
        playback.StopStream(id); Check(!playback.IsStreamPlaying(id) && Capture(native).All(v => Math.Abs(v) < 1e-6), "Native stop releases audible output and ID."); id = playback.PlayStream(wave, playbackType: AudioServer.PlaybackType.Stream); Check(Capture(native).Any(v => Math.Abs(v) > .03), "Streamed child PCM through the same parent.");
        using var mic = new AudioStreamMicrophone(); var settings = ProjectSettings.Service; var previous = ProjectSettings.Get(ProjectSettings.AudioDriverEnableInput); ProjectSettings.Set(ProjectSettings.AudioDriverEnableInput, true); try { var input = playback.PlayStream(mic, playbackType: AudioServer.PlaybackType.Sample); Wait(native, 6); Check(playback.IsStreamPlaying(input), "Nonsampleable input uses prepared queued streamed start."); playback.StopStream(input); Wait(native, 3); } finally { ProjectSettings.Set(ProjectSettings.AudioDriverEnableInput, previous); }
        player.Stop(); wave.Loop = AudioLoopMode.Disabled; AudioServer.RegisterStreamAsSample(wave); player.Play(); playback = (AudioStreamPlaybackPolyphonic)player.GetStreamPlayback(); var defaultType = ProjectSettings.Get(ProjectSettings.AudioGeneralDefaultPlaybackType); ProjectSettings.Set(ProjectSettings.AudioGeneralDefaultPlaybackType, AudioDefaultPlaybackType.Sample);
        try { var finite = playback.PlayStream(wave); Wait(native, 20); Check(!playback.IsStreamPlaying(finite), "Default native child retires on finite completion without another PlayStream."); playback.SetStreamVolume(finite, float.NaN); }
        finally { ProjectSettings.Set(ProjectSettings.AudioGeneralDefaultPlaybackType, defaultType); }
        player.Stop(); Check(!playback.IsPlaying() && !playback.IsStreamPlaying(id), "Parent stop retires both transports.");
    }
    private static void Nested()
    {
        using var childSource = new AudioStreamPolyphonic(); using var wave = Tone(); using var relay = new Relay(childSource); using var sync = new AudioStreamSynchronized { StreamCount = 1 }; sync.SetSyncStream(0, relay);
        var root = new Node(); var player = new AudioStreamPlayer { Stream = sync }; root.AddChild(player); using var tree = new SceneTree(root); player.Play(); var nested = relay.Last!; var id = nested.PlayStream(wave, playbackType: AudioServer.PlaybackType.Sample); var native = AudioServer.Service.Native;
        Check(Capture(native).Any(v => Math.Abs(v) > .03), "Nested synchronized native child output."); player.StreamPaused = true; Check(Capture(native).All(v => Math.Abs(v) < 1e-6), "Nested native pause propagates."); player.StreamPaused = false;
        sync.StreamCount = 0; Check(nested.IsDisposed && Capture(native).All(v => Math.Abs(v) < 1e-6), "Structural cohort edit releases nested native ownership.");
        using var interactive = new AudioStreamInteractive { ClipCount = 1 }; interactive.SetClipStream(0, relay); player.Stream = interactive; player.Play(); nested = relay.Last!; id = nested.PlayStream(wave, playbackType: AudioServer.PlaybackType.Sample);
        Check(Capture(native).Any(v => Math.Abs(v) > .03) && nested.IsStreamPlaying(id), "Queued interactive start resumes its prepared native child on the output thread."); player.Stop(); Check(!nested.IsStreamPlaying(id) && Capture(native).All(v => Math.Abs(v) < 1e-6), "Nested parent stop retires native output.");
    }
    private sealed class Relay(AudioStreamPolyphonic source) : AudioStream
    {
        internal AudioStreamPlaybackPolyphonic? Last;
        protected override AudioStreamPlayback OnInstantiatePlayback() => Last = (AudioStreamPlaybackPolyphonic)source.InstantiatePlayback();
    }
    internal static void RunHost()
    {
        var settings = ProjectSettings.Service; var previous = ProjectSettings.Get(ProjectSettings.RenderingMethod); var backend = Environment.GetEnvironmentVariable("ELECTRON2D_AUDIO_RENDERER") == "compatibility" ? "compatibility" : "gpu"; ProjectSettings.Set(ProjectSettings.RenderingMethod, backend);
        try { for (var i = 0; i < 2; i++) { using var source = new AudioStreamPolyphonic(); using var wave = Tone(); var window = new Window { Size = new(240, 120), Title = "Electron2D dynamic polyphony" }; var emitter = new AudioStreamEmitter { Stream = source, Position = new(120, 60), VolumeDB = -24 }; var host = new Host(emitter, wave); window.AddChild(emitter); window.AddChild(host); Check(Engine.Run(window) == 0 && host.Completed && window.IsDisposed, "Polyphonic public emitter host lifecycle."); Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(new { scenario = "polyphonic-host", backend, run = i, host.Completed, cleaned = window.IsDisposed })); } } finally { ProjectSettings.Set(ProjectSettings.RenderingMethod, previous); }
    }
    private sealed class Host(AudioStreamEmitter emitter, AudioStreamWAV wave) : Node
    {
        private double _time; private int _phase; private AudioStreamPlaybackPolyphonic? _playback; private long _sample, _stream; internal bool Completed;
        protected override void OnReady() { emitter.Play(); _playback = (AudioStreamPlaybackPolyphonic)emitter.GetStreamPlayback(); _sample = _playback.PlayStream(wave, playbackType: AudioServer.PlaybackType.Sample); _stream = _playback.PlayStream(wave, volumeDB: -6, playbackType: AudioServer.PlaybackType.Stream); ProcessEnabled = true; }
        protected override void OnProcess(double delta) { _time += delta; if (_phase == 0 && _time > .15) { emitter.StreamPaused = true; _phase++; } else if (_phase == 1 && _time > .25) { emitter.StreamPaused = false; emitter.Position = new(180, 60); _playback!.SetStreamVolume(_sample, -6); _playback.SetStreamPitchScale(_stream, 2); _phase++; } else if (_phase == 2 && _time > .4) { _playback!.StopStream(_sample); _playback.StopStream(_stream); Check(!_playback.IsStreamPlaying(_sample) && !_playback.IsStreamPlaying(_stream), "Public host IDs invalidate."); emitter.Stop(); Completed = true; Tree!.Quit(); } if (_time > 3) throw new InvalidOperationException("Polyphonic host timeout."); }
    }
    private sealed class Probe(Vector2 value) : AudioStream
    {
        internal int Frames = int.MaxValue; internal bool FailStop, Nonfinite, Reuse; internal Action? Factory, Hook; internal Playback? Last;
        protected override AudioStreamPlayback OnInstantiatePlayback() { Factory?.Invoke(); return Reuse && Last is not null ? Last : Last = new(this, value); }
        internal sealed class Playback(Probe source, Vector2 value) : AudioStreamPlayback
        {
            internal bool Active; internal int Starts, Remaining; internal double Offset; internal float Rate;
            protected override void OnStart(double position) { Active = true; Starts++; Offset = position; Remaining = source.Frames; }
            protected override void OnStop() { Active = false; if (source.FailStop) throw new ApplicationException("Stop failure"); }
            protected override bool OnIsPlaying() => Active;
            protected override double OnGetPlaybackPosition() => Offset;
            protected override void OnSeek(double time) => Offset = time;
            protected override int OnMix(Span<Vector2> buffer, float rateScale) { source.Hook?.Invoke(); Rate = rateScale; var count = Active ? Math.Min(Remaining, buffer.Length) : 0; buffer.Fill(source.Nonfinite ? new(float.NaN, 0) : value); Remaining -= count; return count; }
        }
    }
    private static AudioStreamWAV Tone() { var data = new byte[8820]; for (var i = 0; i < data.Length / 2; i++) BinaryPrimitives.WriteInt16LittleEndian(data.AsSpan(i * 2), (short)(Math.Sin(i * Math.PI * 2 * 440 / 44100) * 8000)); return new AudioStreamWAV { Data = data, SampleFormat = AudioStreamWAV.Format.PCM16, Loop = AudioLoopMode.Forward, LoopBegin = 0, LoopEnd = 4409 }; }
    private static float[] Capture(FAudioContext context) { Wait(context, 4); context.PrepareCapture(context.QuantumFrames * context.Channels * 6); Wait(context, 8); return context.CapturedPCM(); }
    private static void Wait(FAudioContext context, int count) { var end = context.MixPasses + count; var started = System.Diagnostics.Stopwatch.GetTimestamp(); while (context.MixPasses < end) { if (System.Diagnostics.Stopwatch.GetElapsedTime(started) > TimeSpan.FromSeconds(5)) throw new InvalidOperationException("Native mixing timeout"); Thread.Sleep(1); } }
    private static void Near(Vector2 a, Vector2 b) { if (Math.Abs(a.X - b.X) > 1e-5 || Math.Abs(a.Y - b.Y) > 1e-5) throw new InvalidOperationException($"PCM {a} != {b}"); }
    private static void Check(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
    private static void Reject<T>(Action action) where T : Exception { try { action(); } catch (T) { return; } throw new InvalidOperationException("Expected " + typeof(T).Name); }
}
