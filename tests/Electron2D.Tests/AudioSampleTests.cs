using System.Buffers.Binary;
using Electron2D;

internal static class AudioSampleTests
{
    internal static void Run(bool native = false)
    {
        Snapshots(); Ownership(); if (native) { Native(); Edges(); }
        Console.WriteLine("Sample generation, typed PCM/loops and ownership" + (native ? "/actual FAudio" : "") + " checks passed.");
    }
    private static void Snapshots()
    {
        using var stream = Tone(22050, 1); Check(stream.CanBeSampled(), "WAV is sample-capable."); using var sample = stream.GenerateSample();
        Check(sample.NumChannels == 1 && sample.SampleRate == 22050 && sample.LoopMode == AudioLoopMode.Disabled && sample.Data.Length == 22050 && ReferenceEquals(sample.Stream, stream), "Copied sample metadata/data.");
        var copy = sample.Data; copy[10] = 999; Check(sample.Data[10] != 999, "Data inspection copies."); var original = sample.Data[10]; stream.Data = new byte[64]; Check(sample.Data[10] == original, "Resource replacement retains generated snapshot.");
        foreach (var mode in new[] { AudioLoopMode.Forward, AudioLoopMode.PingPong, AudioLoopMode.Backward })
        {
            using var traversal = new AudioSample(stream, [0, 1, 2, 3, 4, 5], 1, 44100, mode, 1, 5); var prepared = traversal.PrepareNative();
            var expected = mode == AudioLoopMode.Forward ? new float[] { 0, 1, 2, 3, 4, 5 } : mode == AudioLoopMode.PingPong ? new float[] { 0, 1, 2, 3, 4, 3, 2 } : new float[] { 0, 4, 3, 2, 1 };
            Check(prepared.Data.SequenceEqual(expected) && prepared.Begin == 1 && prepared.Length == (mode == AudioLoopMode.PingPong ? 6 : 4), "Exact prepared forward/reflected/reversed loop frames.");
        }
        using var mic = new AudioStreamMicrophone(); Check(!mic.CanBeSampled(), "Recording is not finite sample PCM."); Reject<NotSupportedException>(() => mic.GenerateSample());
        Reject<ArgumentException>(() => new AudioSample(stream, [float.NaN], 1, 44100)); Reject<ArgumentException>(() => new AudioSample(stream, [1, 2, 3], 2, 44100)); Reject<ArgumentOutOfRangeException>(() => new AudioSample(stream, [1], 1, 0)); Reject<ArgumentOutOfRangeException>(() => new AudioSample(stream, [1], 1, 44100, AudioLoopMode.Forward, 0, 2));
        using var mp3 = AudioStreamMP3.LoadFromFile(Fixture("tone.mp3")); using var ogg = AudioStreamOggVorbis.LoadFromFile(Fixture("tone.ogg"));
        mp3.Loop = ogg.Loop = true; mp3.LoopOffset = ogg.LoopOffset = .05;
        foreach (var resource in new AudioStream[] { mp3, ogg }) { Check(resource.CanBeSampled(), "Compressed sampling capability."); using var s = resource.GenerateSample(); Check(s.NumChannels == 2 && s.SampleRate > 0 && s.LoopMode == AudioLoopMode.Forward && s.LoopBegin == (int)(.05 * s.SampleRate) && s.Data.Any(v => Math.Abs(v) > .01), "Compressed decoded PCM and seconds-to-frame loop offset."); }
        using var mono = AudioStreamMP3.LoadFromFile(Fixture("mono.mp3")); using var ms = mono.GenerateSample(); Check(ms.NumChannels == 1, "Mono compressed channels.");
        stream.Dispose(); Check(!sample.IsDisposed, "Sample snapshot owns PCM and borrows source.");
    }
    private static void Ownership()
    {
        using var stream = Tone(44100, .1); using var playback = stream.InstantiatePlayback(); using var second = stream.InstantiatePlayback(); var request = new AudioSamplePlayback(stream);
        Check(playback.GetSamplePlayback() is null, "Ordinary playback has no sample request."); playback.SetSamplePlayback(request); Check(ReferenceEquals(playback.GetSamplePlayback(), request), "Typed association.");
        Reject<InvalidOperationException>(() => second.SetSamplePlayback(request)); Reject<InvalidOperationException>(() => request.Dispose());
        playback.SetSamplePlayback(null); Check(request.IsDisposed, "Replacing association consumes old ownership.");
        var server = AudioServer.Service; Check(!AudioServer.IsStreamRegisteredAsSample(stream), "No implicit registration from construction."); AudioServer.RegisterStreamAsSample(stream); Check(AudioServer.IsStreamRegisteredAsSample(stream), "Prepared registration.");
        using var invalid = new SampleFactory(_ => null!); Reject<InvalidOperationException>(() => AudioServer.RegisterStreamAsSample(invalid)); Check(!AudioServer.IsStreamRegisteredAsSample(invalid), "Failed generation publishes no cache entry.");
        using var reentrant = new SampleFactory(_ => { AudioServer.AddBus(); return stream.GenerateSample(); }); Reject<InvalidOperationException>(() => AudioServer.RegisterStreamAsSample(reentrant)); Check(AudioServer.BusCount == 1, "Sample factory cannot mutate the audio graph.");
        var fail = false; using var mutable = new SampleFactory(source => fail ? throw new FormatException("Injected sample failure.") : new AudioSample(source, [0, 1], 1, 44100)); AudioServer.RegisterStreamAsSample(mutable); fail = true; Reject<FormatException>(() => AudioServer.RegisterStreamAsSample(mutable)); Check(AudioServer.IsStreamRegisteredAsSample(mutable), "Failed re-registration preserves the previous prepared snapshot.");
        using var generator = new AudioStreamGenerator(); Reject<NotSupportedException>(() => AudioServer.RegisterStreamAsSample(generator));
        Task.Run(() => Reject<InvalidOperationException>(() => AudioServer.RegisterStreamAsSample(stream))).GetAwaiter().GetResult();
        using var p = new AudioSamplePlayback(stream); Reject<ArgumentOutOfRangeException>(() => p.Offset = double.NaN); Reject<ArgumentOutOfRangeException>(() => p.PitchScale = 0); Reject<ArgumentException>(() => p.VolumeVector = []); Reject<ArgumentOutOfRangeException>(() => p.VolumeVector = [Vector2.One, new(-1, 0), Vector2.One, Vector2.One]);
        var gains = p.VolumeVector; gains[0] = Vector2.Zero; Check(p.VolumeVector[0] == Vector2.One, "Request gains copy out.");
    }
    private static void Native()
    {
        using var stream = Tone(22050, 1); var root = new Node(); var player = new AudioStreamPlayer { Stream = stream, PlaybackType = AudioServer.PlaybackType.Sample, MaxPolyphony = 2 }; root.AddChild(player); using var tree = new SceneTree(root);
        player.Play(.1); var native = AudioServer.Service.Native; var handle = player.GetStreamPlayback(); Check(handle.GetSamplePlayback() is not null, "Sample player creates typed native association."); Reject<InvalidOperationException>(() => handle.Dispose());
        Wait(native, 12); native.PrepareCapture(native.QuantumFrames * native.Channels * 8); Wait(native, 10); Check(native.CapturedPCM().Any(v => Math.Abs(v) > .1), "Complete sample buffer reaches actual FAudio.");
        var position = player.GetPlaybackPosition(); Check(position > .1 && position < .9, "Native sample cursor advances from offset.");
        player.StreamPaused = true; position = player.GetPlaybackPosition(); Wait(native, 8); Check(player.GetPlaybackPosition() == position, "Native sample pause freezes cursor."); player.StreamPaused = false;
        Reject<NotSupportedException>(() => player.PitchScale = 2048); Check(player.PitchScale == 1, "Rejected native pitch preserves player configuration.");
        player.PitchScale = 2; player.VolumeDB = -6; Wait(native, 8); Check(player.GetPlaybackPosition() > position, "Live native gain/pitch.");
        player.Stop(); player.Play(); player.Play(); player.MaxPolyphony = 1; player.Play(); Wait(native, 8); Check(player.GetStreamPlayback().GetSamplePlayback() is not null && player.GetPlaybackPosition() < .4 && handle.IsDisposed, "Cold replay and shrinking sample polyphony replace borrowed bindings.");
        player.Stop(); player.PitchScale = 1; player.VolumeDB = 0; player.Play();
        var finished = 0; player.Finished += () => finished++; Wait(native, 115); tree.ProcessFrame(.01); Check(!player.IsPlaying() && finished == 1, "Finite sample completion signals once.");
        Check(Electron2D.FAudioBindings.FAudio.FAudioLinkedVersion() == Electron2D.FAudioBindings.FAudio.FAUDIO_COMPILED_VERSION, "Native and internal binding release pins agree.");
        using var loop = Tone(44100, .1); loop.Loop = AudioLoopMode.Forward; loop.LoopBegin = 0; loop.LoopEnd = 4409; player.Stream = loop; player.Play(); Wait(native, 25); Check(player.IsPlaying() && player.GetPlaybackPosition() < .11, "Whole-sample native loop.");
        for (var i = 0; i < 20; i++) { player.VolumeDB = i % 2 == 0 ? 0 : -6; player.PitchScale = i % 2 == 0 ? 1 : 2; Wait(native, 1); }
        player.PitchScale = 1f / 1024; Wait(native, 4); player.PitchScale = 1024; Wait(native, 2); player.PitchScale = 1; Wait(native, 4);
        var before = native.MixManagedBytes; var calls = FAudioContext.AllocationCalls; for (var i = 0; i < 64; i++) { player.VolumeDB = i % 2 == 0 ? 0 : -6; player.PitchScale = i % 2 == 0 ? 1 : 2; Wait(native, 1); }
        Check(native.MixManagedBytes == before && FAudioContext.AllocationCalls == calls, "64 warmed native sample gain/pitch passes allocate zero measured bytes/calls.");
        player.StreamPaused = true; Wait(native, 20); before = native.MixManagedBytes; calls = FAudioContext.AllocationCalls; Wait(native, 64); Check(native.MixManagedBytes == before && FAudioContext.AllocationCalls == calls, "64 warmed paused sample passes allocate zero measured bytes/calls.");
        player.StreamPaused = false; using var mp3 = AudioStreamMP3.LoadFromFile(Fixture("tone.mp3")); using var ogg = AudioStreamOggVorbis.LoadFromFile(Fixture("tone.ogg"));
        foreach (var codec in new AudioStream[] { mp3, ogg }) { player.Stream = codec; player.PitchScale = 1; player.VolumeDB = 0; player.Play(); native.PrepareCapture(native.QuantumFrames * native.Channels * 8); Wait(native, 10); Check(native.CapturedPCM().Any(v => Math.Abs(v) > .03), "Compressed sample playback reaches native output."); }
        player.Stop(); using var standalone = stream.InstantiatePlayback(); standalone.SetSamplePlayback(new AudioSamplePlayback(stream)); standalone.Start(.2); Wait(native, 10); Check(standalone.GetPlaybackPosition() > .2, "Standalone typed sample association executes."); standalone.Seek(.1); Wait(native, 2); Check(standalone.GetPlaybackPosition() >= .1 && standalone.GetPlaybackPosition() < .2, "Standalone seek resets its native counter origin."); standalone.Stop();
        player.Play(); ogg.Dispose(); Wait(native, 5); Reject<Exception>(() => tree.ProcessFrame(.01)); Check(!player.IsPlaying(), "Disposed sample source stops native output and reports on the owner.");
    }
    private static void Edges()
    {
        var server = AudioServer.Service; using var stream = Tone(44100, .1); stream.Loop = AudioLoopMode.Forward; stream.LoopBegin = 10; stream.LoopEnd = 4000;
        var root = new Node(); var player = new AudioStreamPlayer { Stream = stream, PlaybackType = AudioServer.PlaybackType.Sample }; root.AddChild(player); using var tree = new SceneTree(root); player.Play(); var native = server.Native;
        var request = player.GetStreamPlayback().GetSamplePlayback()!; request.Offset = double.MaxValue; native.PrepareCapture(native.QuantumFrames * native.Channels * 8); Wait(native, 10); Check(player.GetStreamPlayback().IsPlaying() && native.CapturedPCM().Any(v => Math.Abs(v) > .1), "Looped offset beyond the loop wraps before native submit.");
        AudioServer.AddBus(); AudioServer.SetBusName(1, "sample-route"); player.Bus = "sample-route"; Wait(native, 5); AudioServer.SetBusMute(1, true); native.PrepareCapture(native.QuantumFrames * native.Channels * 8); Wait(native, 10); Check(native.CapturedPCM().All(v => Math.Abs(v) < 1e-6), "Sample bus mute."); AudioServer.SetBusMute(1, false);
        using var standalone = stream.InstantiatePlayback(); standalone.SetSamplePlayback(new AudioSamplePlayback(stream) { Bus = "sample-route" }); standalone.Start(); AudioServer.AddBus(); Wait(native, 5); Check(standalone.IsPlaying(), "Standalone sample reroutes across real bus graph reconstruction.");
        standalone.Stop(); standalone.Start(.03); Wait(native, 2); Check(standalone.GetPlaybackPosition() >= 0 && standalone.GetPlaybackPosition() < .11, "Repeated native starts keep bounded loop positions.");
        var settings = ProjectSettings.Service; var previous = ProjectSettings.Get(ProjectSettings.AudioGeneralDefaultPlaybackType); ProjectSettings.Set(ProjectSettings.AudioGeneralDefaultPlaybackType, AudioDefaultPlaybackType.Sample);
        try { player.PlaybackType = AudioServer.PlaybackType.Default; player.Play(); Check(player.GetStreamPlayback().GetSamplePlayback() is not null, "Typed project default selects samples."); }
        finally { ProjectSettings.Set(ProjectSettings.AudioGeneralDefaultPlaybackType, previous); }
        player.Play(); Check(player.GetStreamPlayback().GetSamplePlayback() is null, "Changing the project default recreates pooled native transport.");
        var previousInput = ProjectSettings.Get(ProjectSettings.AudioDriverEnableInput); player.Stream = new AudioStreamMicrophone(); ProjectSettings.Set(ProjectSettings.AudioDriverEnableInput, true); try { player.PlaybackType = AudioServer.PlaybackType.Sample; player.Play(); Check(player.GetStreamPlayback().GetSamplePlayback() is null, "A sample request falls back for a nonsampleable recording stream."); player.Stop(); } finally { ProjectSettings.Set(ProjectSettings.AudioDriverEnableInput, previousInput); player.Stream!.Dispose(); player.Stream = null; }
        standalone.Stop();
        var speed = AudioServer.PlaybackSpeedScale; AudioServer.PlaybackSpeedScale = 2; try { player.Stream = stream; player.Play(); Wait(native, 3); Reject<NotSupportedException>(() => AudioServer.PlaybackSpeedScale = 2048); Check(AudioServer.PlaybackSpeedScale == 2, "Unsupported global native ratio preserves global configuration."); } finally { AudioServer.PlaybackSpeedScale = speed; }
        foreach (var mode in new[] { AudioLoopMode.PingPong, AudioLoopMode.Backward })
        {
            stream.Loop = mode; AudioServer.RegisterStreamAsSample(stream); player.Stream = stream; player.PlaybackType = AudioServer.PlaybackType.Sample; player.Play(); Wait(native, 15);
            var playback = player.GetStreamPlayback(); Check(playback.IsPlaying() && playback.GetPlaybackPosition() < .1, "Native reflected/reversed loops retain source-time positions.");
            playback.LoopingOverride = false; Wait(native, 15); tree.ProcessFrame(.01); Check(!player.IsPlaying(), "Disabling a transformed loop plays the original finite PCM through completion.");
        }
        player.Play(); player.StreamPaused = true; player.GetStreamPlayback().LoopingOverride = false; var paused = player.GetPlaybackPosition(); Wait(native, 5); Check(player.StreamPaused && player.GetPlaybackPosition() == paused, "Live looping updates preserve pause."); player.Stop();
        using var empty = new AudioStreamWAV(); player.Stream = empty; var completions = 0; player.Finished += () => completions++; player.Play(); tree.ProcessFrame(.01); Check(!player.IsPlaying() && completions == 1, "Empty finite native samples complete once without submitting zero bytes.");
        using var unsupportedRate = Tone(999, .1); player.Stream = unsupportedRate; Reject<NotSupportedException>(() => player.Play()); Check(!player.IsPlaying(), "Unsupported native source rates reject before active publication.");
        AudioServer.BusCount = 1;
    }
    internal static void RunHost()
    {
        var backend = Environment.GetEnvironmentVariable("ELECTRON2D_AUDIO_RENDERER") == "compatibility" ? "compatibility" : "gpu"; var settings = ProjectSettings.Service; var previous = ProjectSettings.Get(ProjectSettings.RenderingMethod); var fps = Engine.MaxFPS;
        ProjectSettings.Set(ProjectSettings.RenderingMethod, backend); Engine.MaxFPS = 60;
        try
        {
            for (var i = 0; i < 2; i++)
            {
                using var stream = Tone(22050, 1); var window = new Window { Title = "Electron2D native samples", Size = new(240, 120) }; var player = new AudioStreamPlayer { Stream = stream, Autoplay = true, PlaybackType = AudioServer.PlaybackType.Sample, VolumeDB = -24 }; var emitter = new AudioStreamEmitter { Stream = stream, Position = new(120, 60), Autoplay = true, PlaybackType = AudioServer.PlaybackType.Sample, VolumeDB = -24 }; var scenario = new Host(player, emitter); window.AddChild(player); window.AddChild(emitter); window.AddChild(scenario);
                if (Engine.Run(window) != 0 || !scenario.Completed || !window.IsDisposed || stream.IsDisposed) throw new InvalidOperationException("Sample host lifecycle failed.");
                Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(new { scenario = "audio-sample-host", backend, run = i, scenario.Frozen, completed = scenario.Completed, cleaned = window.IsDisposed }));
            }
        }
        finally { ProjectSettings.Set(ProjectSettings.RenderingMethod, previous); Engine.MaxFPS = fps; }
    }
    private sealed class Host(AudioStreamPlayer player, AudioStreamEmitter emitter) : Node
    {
        private double _elapsed, _at; private int _phase; private bool _finished, _emitterFinished; internal bool Completed; internal double Frozen;
        protected override void OnReady() { ProcessEnabled = true; player.Finished += () => _finished = true; emitter.Finished += () => _emitterFinished = true; if (!player.IsPlaying() || player.GetStreamPlayback().GetSamplePlayback() is null) throw new InvalidOperationException("Native sample autoplay."); }
        protected override void OnProcess(double delta)
        {
            _elapsed += delta;
            if (_phase == 0 && _elapsed > .15) { if (!emitter.IsPlaying() || emitter.GetStreamPlayback().GetSamplePlayback() is null) throw new InvalidOperationException("Sample emitter autoplay at fixed-step entry."); player.StreamPaused = emitter.StreamPaused = true; Frozen = player.GetPlaybackPosition(); _at = _elapsed; _phase = 1; }
            else if (_phase == 1 && _elapsed - _at > .1) { if (player.GetPlaybackPosition() != Frozen) throw new InvalidOperationException("Native sample paused cursor moved."); player.StreamPaused = emitter.StreamPaused = false; player.Seek(.3); emitter.Seek(.3); player.VolumeDB = emitter.VolumeDB = -30; player.PitchScale = emitter.PitchScale = 2; _phase = 2; }
            else if (_phase == 2 && _finished && _emitterFinished) { if (player.IsPlaying() || emitter.IsPlaying()) throw new InvalidOperationException("Sample completed while player remained active."); Completed = true; Tree!.Quit(); }
            if (_elapsed > 5) throw new InvalidOperationException("Native sample host timed out.");
        }
    }
    private sealed class SampleFactory(Func<AudioStream, AudioSample> create) : AudioStream
    {
        public override bool CanBeSampled() => true;
        public override AudioSample GenerateSample() => create(this);
        protected override AudioStreamPlayback OnInstantiatePlayback() => throw new NotSupportedException();
    }
    private static AudioStreamWAV Tone(int rate, double seconds) { var count = (int)(rate * seconds); var bytes = new byte[count * 2]; for (var i = 0; i < count; i++) BinaryPrimitives.WriteInt16LittleEndian(bytes.AsSpan(i * 2), (short)(Math.Sin(i * 2 * Math.PI * 440 / rate) * 8000)); return new AudioStreamWAV { SampleFormat = AudioStreamWAV.Format.PCM16, MixRate = rate, Data = bytes }; }
    private static string Fixture(string name) => System.IO.Path.Combine("tests", "Electron2D.Tests", "Fixtures", "Audio", name);
    private static void Wait(FAudioContext context, long count) { var end = context.MixPasses + count; var watch = System.Diagnostics.Stopwatch.StartNew(); while (context.MixPasses < end) { if (watch.Elapsed > TimeSpan.FromSeconds(5)) throw new InvalidOperationException("Native sample mixing did not advance."); Thread.Sleep(1); } }
    private static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    private static void Reject<T>(Action action) where T : Exception { try { action(); } catch (T) { return; } throw new InvalidOperationException("Expected " + typeof(T).Name); }
}
