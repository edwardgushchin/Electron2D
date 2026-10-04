using System.Buffers.Binary;
using Electron2D;

internal static class AudioPlaylistTests
{
    internal static void Run(bool native = false) { Resources(); Mixing(); Failures(); Warm(); if (native) Native(); Console.WriteLine("Playlist order/metadata/boundaries/fades/loops/copies/ownership and warmed CPU" + (native ? "/native output" : "") + " passed."); }
    private static void Resources()
    {
        using var source = new AudioStreamPlaylist(); Check(AudioStreamPlaylist.MaxStreams == 64 && source.StreamCount == 0 && source.FadeTime == .3 && !source.Shuffle && source.Loop && source.IsMetaStream() && source.IsMonophonic() && !source.CanBeSampled() && source.GetLength() == 0 && source.GetBPM() == 0, "Default resource contract.");
        using var a = new Probe(new(.1f, .2f)) { Duration = 2 }; using var b = new Probe(new(.2f, .1f)) { Duration = 9, BPM = 120, Beats = 2 };
        source.SetListStream(63, b); source.SetListStream(0, a); source.SetListStream(1, b); source.StreamCount = 2; Check(source.GetLength() == 3 && source.GetBPM() == 120 && source.GetListStream(63) == b, "Musical duration, first tempo and hidden slots.");
        Reject<ArgumentOutOfRangeException>(() => source.StreamCount = 65); Reject<ArgumentOutOfRangeException>(() => source.GetListStream(-1)); Reject<ArgumentOutOfRangeException>(() => source.FadeTime = double.NaN); Reject<ArgumentOutOfRangeException>(() => source.FadeTime = -1); Reject<InvalidOperationException>(() => source.SetListStream(2, source));
        var notifications = 0; source.PropertyListChanged += _ => notifications++; source.StreamCount = 2; Check(notifications == 1, "Equal count notifies without a cohort rebuild.");
        source.FadeTime = 2; source.Shuffle = true; source.Loop = false; using var shallow = (AudioStreamPlaylist)source.Duplicate(); using var deep = (AudioStreamPlaylist)source.DuplicateDeep(DeepDuplicateMode.All); Check(shallow.GetListStream(0) == a && !ReferenceEquals(deep.GetListStream(0), a) && shallow.GetListStream(63) is null && deep.FadeTime == 2 && deep.Shuffle && !deep.Loop, "Copies use active prefix, alias policy and typed configuration.");
        using var window = new Window(); var player = new AudioStreamPlayer { Stream = source }; window.AddChild(player); player.Owner = window; using var packed = new PackedScene(); packed.Pack(window); using var instance = (Window)packed.Instantiate(); Check(instance.GetChildren().OfType<AudioStreamPlayer>().Single().Stream == source, "PackedScene borrows the playlist.");
        using var sync = new AudioStreamSynchronized { StreamCount = 1 }; sync.SetSyncStream(0, source); Reject<InvalidOperationException>(() => source.SetListStream(0, sync));
    }
    private static void Mixing()
    {
        var rate = AudioServer.GetMixRate(); using var a = new Probe(new(.1f, .2f)) { Duration = 3d / rate }; using var b = new Probe(new(.2f, .1f)) { Duration = 4d / rate }; using var source = new AudioStreamPlaylist { StreamCount = 2, FadeTime = 2d / rate, Loop = false }; source.SetListStream(0, a); source.SetListStream(1, b); using var playback = source.InstantiatePlayback(); var output = new Vector2[12];
        Check(playback.MixInto(output, 1) == 0, "Inactive request."); playback.Start(); playback.MixInto([], 9); Check(playback.IsPlaying(), "Empty demand does not advance."); Check(playback.MixInto(output, 9) == 12 && !playback.IsPlaying(), "Finite playlist reports padded full demand and ends.");
        for (var i = 0; i < 3; i++) Near(output[i], new(.1f, .2f)); Near(output[3], new(.3f, .3f)); Near(output[4], new(.25f, .2f)); Near(output[5], new(.2f, .1f)); Check(output.Skip(7).All(v => v == Vector2.Zero) && a.Last!.Rate == 1 && b.Last!.Rate == 1 && !a.Last.Active && !b.Last.Active, "Exact in-block transition, outgoing fade, zero padding, child rate one and cleanup.");
        Near(playback.GetPlaybackPosition(), 7d / rate); playback.Seek(4d / rate); Near(b.Last!.StartedAt, 1d / rate); Near(playback.GetPlaybackPosition(), 4d / rate); playback.Stop(); playback.Start(-1); Near(a.Last!.StartedAt, 0); Near(playback.GetPlaybackPosition(), 0);
        source.FadeTime = 0; playback.Start(); playback.MixInto(output, 1); Near(output[3], new(.2f, .1f)); Check(output.All(v => v.IsFinite()), "Zero fade has no old first frame or nonfinite division.");
        source.Loop = true; playback.Start(7d / rate + 1d / rate); Near(a.Last.StartedAt, 1d / rate); playback.MixInto(output, 1); Check(playback.IsPlaying() && playback.GetLoopCount() >= 1 && playback.GetPlaybackPosition() < 7d / rate, "Beyond-end start wraps and cycles count independently.");
        source.StreamCount = 0; Check(!playback.IsPlaying() && a.Last.IsDisposed && b.Last!.IsDisposed, "Count edit closes full previous cohort."); playback.Start(); Check(!playback.IsPlaying(), "Empty looping resource never modulo-divides zero.");
        source.StreamCount = 1; a.Duration = 0; playback.Start(); Check(!playback.IsPlaying(), "All zero duration tracks stay stopped."); a.Duration = 3d / rate; playback.Start(); var old = a.Last; source.SetListStream(63, b); Check(old!.IsDisposed && !playback.IsPlaying(), "Hidden structural writes also rebuild registered ownership.");
        using var one = new AudioStreamPlaylist { StreamCount = 1, FadeTime = 0 }; one.SetListStream(0, a); a.Loop = true; using var single = one.InstantiatePlayback(); single.Start(); var starts = a.Last!.Starts; single.MixAudio(1, 10); Check(a.Last.Starts == starts && single.GetLoopCount() == 3, "Same looping child continues instead of restart.");
        using var order = new AudioStreamPlaylist { StreamCount = 4, Shuffle = true, Loop = false, FadeTime = 0 }; var probes = new Probe[4]; for (var i = 0; i < probes.Length; i++) { probes[i] = new Probe(new(i + 1, i + 1)) { Duration = 1d / rate }; order.SetListStream(i, probes[i]); }
        try { using var shuffled = order.InstantiatePlayback(); var seen = new HashSet<string>(); for (var run = 0; run < 20; run++) { shuffled.Start(); var data = shuffled.MixAudio(1, 4); Check(data.Select(v => v.X).Order().SequenceEqual(new float[] { 1, 2, 3, 4 }), "Every shuffled cycle is a permutation."); seen.Add(string.Join(',', data.Select(v => v.X))); } Check(seen.Count > 1, "Starts generate fresh shuffle orders."); } finally { foreach (var probe in probes) probe.Dispose(); }
    }
    private static void Failures()
    {
        using var a = new Probe(Vector2.One) { Duration = 1 }; using var bad = new Probe(Vector2.One) { FactoryFailure = true }; using var source = new AudioStreamPlaylist { StreamCount = 1 }; source.SetListStream(0, a); using var first = source.InstantiatePlayback(); using var second = source.InstantiatePlayback(); first.Start(); second.Start(); var old = a.Last;
        Reject<ApplicationException>(() => source.SetListStream(0, bad)); Check(first.IsPlaying() && second.IsPlaying() && old is { IsDisposed: false } && source.GetListStream(0) == a, "Factory failure preserves every cohort and configuration.");
        a.FactoryHook = () => source.StreamCount = 0; Reject<InvalidOperationException>(() => source.InstantiatePlayback()); a.FactoryHook = null;
        a.MixHook = () => source.Dispose(); Reject<InvalidOperationException>(() => first.MixAudio(1, 256)); Check(!source.IsDisposed && !first.IsPlaying(), "Source disposal callback rejects and failed mixer stops."); a.MixHook = null;
        first.Start(); a.Nonfinite = true; var output = new Vector2[256]; Reject<ArithmeticException>(() => first.MixInto(output, 1)); Check(output.All(v => v == Vector2.Zero), "Nonfinite data cannot escape the aggregate."); a.Nonfinite = false;
        a.StopFailure = true; Reject<Exception>(() => source.SetListStream(0, bad)); Check(source.GetListStream(0) == a, "Preparation failure precedes any old cleanup."); a.StopFailure = false;
        using var good = new Probe(Vector2.One); a.StopFailure = true; Reject<Exception>(() => source.SetListStream(0, good)); Check(source.GetListStream(0) == good && !first.IsPlaying() && !second.IsPlaying(), "Cleanup failure reports after committed new stopped cohorts."); a.StopFailure = false;
        using var shared = new Probe(Vector2.One) { Reuse = true, Duration = 1 }; using var aliases = new AudioStreamPlaylist { StreamCount = 1 }; aliases.SetListStream(0, shared); using var owner = aliases.InstantiatePlayback(); Reject<InvalidOperationException>(() => aliases.InstantiatePlayback()); Check(!shared.Last!.IsDisposed, "Shared factory rejects without disposing the prior owning cohort.");
    }
    private static void Warm()
    {
        var rate = AudioServer.GetMixRate(); using var source = new AudioStreamPlaylist { StreamCount = 64, FadeTime = .001 }; using var child = new Probe(new(.01f, -.01f)) { Duration = 8d / rate }; for (var i = 0; i < 64; i++) source.SetListStream(i, child); using var playback = source.InstantiatePlayback(); var output = new Vector2[257]; playback.Start(); for (var i = 0; i < 20; i++) playback.MixInto(output, 1); var before = GC.GetAllocatedBytesForCurrentThread(); for (var i = 0; i < 64; i++) playback.MixInto(output, 1); Check(GC.GetAllocatedBytesForCurrentThread() == before, "64 warmed 64-track transition/fade passes allocate zero managed bytes."); playback.Stop(); before = GC.GetAllocatedBytesForCurrentThread(); for (var i = 0; i < 64; i++) playback.MixInto(output, 1); Check(GC.GetAllocatedBytesForCurrentThread() == before, "64 stopped passes allocate zero bytes.");
    }
    private static void Native()
    {
        using var a = Tone(); using var b = Tone(); using var source = new AudioStreamPlaylist { StreamCount = 2, Loop = false, FadeTime = .02 }; source.SetListStream(0, a); source.SetListStream(1, b); var root = new Node(); var player = new AudioStreamPlayer { Stream = source, PlaybackType = AudioServer.PlaybackType.Sample }; root.AddChild(player); using var tree = new SceneTree(root); player.Play(); var native = AudioServer.Service.Native; Check(player.GetStreamPlayback() is AudioStreamPlaybackPlaylist && player.GetStreamPlayback().GetSamplePlayback() is null, "Playlist meta Sample request falls back to its stream mixer."); Wait(native, 12); native.PrepareCapture(native.QuantumFrames * native.Channels * 8); Wait(native, 10); Check(native.CapturedPCM().Any(v => Math.Abs(v) > .03), "Real playlist PCM reaches FAudio."); player.StreamPaused = true; var position = player.GetPlaybackPosition(); Wait(native, 5); Check(player.GetPlaybackPosition() == position, "Playlist pause freezes output-clock cursor."); player.StreamPaused = false; var ended = 0; player.Finished += () => ended++; Wait(native, 80); tree.ProcessFrame(.01); Check(!player.IsPlaying() && ended == 1, "Finite native sequence completes once.");
        source.Loop = true; player.Play(); Wait(native, 20); var bytes = native.MixManagedBytes; var calls = FAudioContext.AllocationCalls; Wait(native, 64); Check(native.MixManagedBytes == bytes && FAudioContext.AllocationCalls == calls, "64 warmed looping native playlist passes allocate zero measured callback bytes/custom calls.");
        player.StreamPaused = true; Wait(native, 20); bytes = native.MixManagedBytes; calls = FAudioContext.AllocationCalls; Wait(native, 64); Check(native.MixManagedBytes == bytes && FAudioContext.AllocationCalls == calls, "64 warmed paused native playlist passes allocate zero measured callback bytes/custom calls."); player.Stop();
        using var mic = new AudioStreamMicrophone(); using var input = new TimedInput(mic); using var recording = new AudioStreamPlaylist { StreamCount = 1, Loop = false, FadeTime = 0 }; recording.SetListStream(0, input); var settings = ProjectSettings.Service; var enabled = ProjectSettings.Get(ProjectSettings.AudioDriverEnableInput); ProjectSettings.Set(ProjectSettings.AudioDriverEnableInput, true);
        try { player.Stream = recording; player.Play(); Wait(native, 5); Check(player.IsPlaying(), "Playlist activates prepared input on owner start."); Wait(native, 20); tree.ProcessFrame(.01); Check(!player.IsPlaying(), "Finite timed input playlist stops on its declared boundary."); } finally { ProjectSettings.Set(ProjectSettings.AudioDriverEnableInput, enabled); }
    }
    internal static void RunHost()
    {
        var settings = ProjectSettings.Service; var prior = ProjectSettings.Get(ProjectSettings.RenderingMethod); var backend = Environment.GetEnvironmentVariable("ELECTRON2D_AUDIO_RENDERER") == "compatibility" ? "compatibility" : "gpu"; ProjectSettings.Set(ProjectSettings.RenderingMethod, backend);
        try { for (var i = 0; i < 2; i++) { using var wave = Tone(); using var source = new AudioStreamPlaylist { StreamCount = 2, Loop = false, FadeTime = .02 }; source.SetListStream(0, wave); source.SetListStream(1, wave); var window = new Window { Title = "Electron2D playlist", Size = new(240, 120) }; var player = new AudioStreamEmitter { Stream = source, Position = new(120, 60), VolumeDB = -24 }; var host = new Host(player); window.AddChild(player); window.AddChild(host); Check(Engine.Run(window) == 0 && host.Completed && window.IsDisposed, "Public playlist host lifecycle."); Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(new { scenario = "playlist-host", backend, run = i, host.Completed, cleaned = window.IsDisposed })); } } finally { ProjectSettings.Set(ProjectSettings.RenderingMethod, prior); }
    }
    private sealed class Host(AudioStreamEmitter player) : Node
    {
        private double _time; private bool _finished; private int _phase; internal bool Completed;
        protected override void OnReady() { player.Finished += () => _finished = true; player.Play(); ProcessEnabled = true; }
        protected override void OnProcess(double delta) { _time += delta; if (_phase == 0 && _time > .1) { player.StreamPaused = true; _phase++; } else if (_phase == 1 && _time > .2) { player.StreamPaused = false; player.Seek(.15); _phase++; } else if (_finished) { Check(!player.IsPlaying(), "Playlist completed before quit."); Completed = true; Tree!.Quit(); } if (_time > 4) throw new InvalidOperationException("Playlist host timeout."); }
    }
    private sealed class TimedInput(AudioStreamMicrophone source) : AudioStream
    {
        protected override AudioStreamPlayback OnInstantiatePlayback() => source.InstantiatePlayback();
        protected override double OnGetLength() => .1;
    }
    private sealed class Probe(Vector2 value) : AudioStream
    {
        internal double Duration, BPM; internal int Beats; internal bool Loop, FactoryFailure, StopFailure, Nonfinite, Reuse; internal Action? FactoryHook, MixHook; internal Playback? Last;
        protected override AudioStreamPlayback OnInstantiatePlayback() { FactoryHook?.Invoke(); if (FactoryFailure) throw new ApplicationException("Factory failure."); return Reuse && Last is not null ? Last : Last = new(this, value); }
        protected override double OnGetLength() => Duration;
        protected override double OnGetBPM() => BPM;
        protected override int OnGetBeatCount() => Beats;
        protected override bool OnHasLoop() => Loop;
        protected override Resource CreateDuplicateInstance() => new Probe(value);
        protected override void CopyCustomStateTo(Resource target, bool deep, DeepDuplicateMode mode, Func<Resource?, Resource?> duplicate, Func<Resource?, Resource?> force) { var p = (Probe)target; p.Duration = Duration; p.BPM = BPM; p.Beats = Beats; p.Loop = Loop; }
        internal sealed class Playback(Probe source, Vector2 value) : AudioStreamPlayback
        {
            internal bool Active; internal int Starts; internal double StartedAt; internal float Rate;
            protected override void OnStart(double position) { Active = true; StartedAt = position; Starts++; }
            protected override void OnStop() { Active = false; if (source.StopFailure) throw new ApplicationException("Stop failure."); }
            protected override bool OnIsPlaying() => Active;
            protected override double OnGetPlaybackPosition() => StartedAt;
            protected override void OnSeek(double time) => StartedAt = time;
            protected override int OnMix(Span<Vector2> buffer, float rateScale) { source.MixHook?.Invoke(); Rate = rateScale; buffer.Fill(source.Nonfinite ? new(float.NaN, 0) : value); return Active ? buffer.Length : 0; }
        }
    }
    private static AudioStreamWAV Tone() { var data = new byte[22050]; for (var i = 0; i < data.Length / 2; i++) BinaryPrimitives.WriteInt16LittleEndian(data.AsSpan(i * 2), (short)(Math.Sin(i * Math.PI * 2 * 440 / 44100) * 8000)); return new AudioStreamWAV { Data = data, SampleFormat = AudioStreamWAV.Format.PCM16 }; }
    private static void Wait(FAudioContext context, int passes) { var end = context.MixPasses + passes; var start = System.Diagnostics.Stopwatch.GetTimestamp(); while (context.MixPasses < end) { if (System.Diagnostics.Stopwatch.GetElapsedTime(start) > TimeSpan.FromSeconds(5)) throw new InvalidOperationException("Native mix timeout."); Thread.Sleep(1); } }
    private static void Near(Vector2 a, Vector2 b) { if ((a - b).Length() > 1e-5) throw new InvalidOperationException($"PCM {a} != {b}"); }
    private static void Near(double a, double b) { if (Math.Abs(a - b) > 1e-9) throw new InvalidOperationException($"Time {a} != {b}"); }
    private static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    private static void Reject<T>(Action action) where T : Exception { try { action(); } catch (T) { return; } throw new InvalidOperationException("Expected " + typeof(T).Name); }
}
