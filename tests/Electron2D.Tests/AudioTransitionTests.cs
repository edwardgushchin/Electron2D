using Electron2D;

internal static class AudioTransitionTests
{
    internal static void Run()
    {
        PreparedBlocks(); Native();
        Console.WriteLine("Audio transitions: lookahead, linear gain/pause/stop/seek/replacement, finite failures and warmed active/idle PCM passed.");
    }

    private static void PreparedBlocks()
    {
        using var native = new FAudioContext();
        lock (native.Gate)
        {
            using var playback = new ProbePlayback();
            using var voice = native.CreateStream(playback, native.Master);
            var count = native.QuantumFrames; var channels = native.Channels;
            var output = new float[count * channels];
            voice.SetVolume(1, 1); voice.Play(0); voice.MixBlock(output);
            for (var frame = 0; frame < count; frame++) Near(output[frame * channels], frame < 64 ? 0 : .25f, "Start preserves the full transient after 64 silent lookahead frames.");
            voice.MixBlock(output); voice.SetVolume(.5f, 1); voice.MixBlock(output);
            for (var frame = 0; frame < count; frame++) Near(output[frame * channels], .25f * (1 - .5f * frame / count), "Gain uses frame/count interpolation.");
            voice.MixBlock(output); Near(output[0], .125f, "Gain settles at the next block.");
            voice.Pause(true); var position = voice.Position; voice.Pause(true); Check(voice.Position == position, "Repeated pause does not mix another block.");
            voice.MixBlock(output);
            for (var frame = 0; frame < count; frame++) Near(output[frame * channels], .125f * (1 - (float)frame / count), "Pause drains one linear block.");
            voice.MixBlock(output); Check(output.All(v => v == 0) && voice.Position == position && voice.Paused, "Paused source cursor and output remain still.");
            voice.Pause(false); voice.MixBlock(output);
            for (var frame = 0; frame < count; frame++) Near(output[frame * channels], .125f * frame / count, "Resume ramps from zero while retaining lookahead.");
            voice.MixBlock(output); voice.Stop(); Check(!voice.Playing && playback.Stops == 1, "Stop hides the voice and invokes one owner callback.");
            voice.Stop(); voice.MixBlock(output);
            for (var frame = 0; frame < count; frame++) Near(output[frame * channels], .125f * (1 - (float)frame / count), "Stop drains its prepared PCM after synchronous source Stop.");
            voice.MixBlock(output); Check(output.All(v => v == 0), "Stop tail is consumed exactly once.");
            voice.Play(0); voice.MixBlock(output); voice.MixBlock(output); voice.Stop();
            using var replacement = new ProbePlayback { Sample = new(-.2f, .3f) };
            voice.ReplacePlayback(replacement); voice.Play(.5); voice.MixBlock(output);
            for (var frame = 0; frame < count; frame++) Near(output[frame * channels], .125f * (1 - (float)frame / count) + (frame < 64 ? 0 : -.1f), "Replacement retains outgoing fade independently of the disposed playback.");
            Check(playback.IsDisposed && voice.Position > .5, "Replacement cursor and old handle lifetime.");
            voice.Stop(); voice.MixBlock(output);
            replacement.Remaining = count - 32; voice.Play(0); voice.MixBlock(output);
            Check(voice.Finished && output[64 * channels] == -.1f && Math.Abs(output[(count - 1) * channels]) < .02f, "Finite end preserves attack and decays the final lookahead samples.");
            voice.Stop(); replacement.Remaining = int.MaxValue; voice.Play(0); voice.MixBlock(output);
            for (var i = 0; i < 20; i++) Cycle();
            var bytes = GC.GetAllocatedBytesForCurrentThread(); var calls = FAudioContext.AllocationCalls;
            for (var i = 0; i < 64; i++) Cycle();
            Check(GC.GetAllocatedBytesForCurrentThread() == bytes && FAudioContext.AllocationCalls == calls, "64 prepared gain/pause/resume/active/idle cycles allocate zero measured bytes/calls.");
            replacement.FailMix = true; Reject<AggregateException>(() => voice.Stop()); Check(!voice.Playing, "A final-block failure still stops logical/source state.");
            replacement.FailMix = false; voice.Play(0); voice.MixBlock(output); voice.MixBlock(output);
            using var survivorPlayback = new ProbePlayback(); using var survivor = native.CreateStream(survivorPlayback, native.Master);
            survivor.SetVolume(1, 1); survivor.Play(0); survivor.MixBlock(output); survivor.MixBlock(output);
            voice.Stop(); voice.MoveTailTo(survivor); survivor.MixBlock(output);
            Near(output[0], .15f, "Trimmed voice moves its outgoing -.1 gain tail into the surviving .25 voice.");
            voice.Play(0); replacement.Sample = new(float.NaN, 0); Reject<ArithmeticException>(() => voice.MixBlock(output));
            void Cycle() { voice.SetVolume(.25f, 1); voice.MixBlock(output); voice.Pause(true); voice.MixBlock(output); voice.MixBlock(output); voice.Pause(false); voice.SetVolume(.5f, 1); voice.MixBlock(output); }
        }
    }

    private static void Native()
    {
        var server = AudioServer.Instance; server.CloseNative(); server.BusCount = 1;
        using var stream = new ProbeStream(); var root = new Node(); var player = new AudioStreamPlayer { Stream = stream }; root.AddChild(player);
        using var tree = new SceneTree(root); player.Play(); var native = server.Native;
        try
        {
            AudioEffectTests.Wait(native, 20);
            Capture(() => player.VolumeLinear = .5f, value => value > .126f && value < .249f, "Actual FAudio gain ramp contains intermediate samples.");
            Capture(() => player.StreamPaused = true, value => value > .001f && value < .124f, "Actual FAudio pause drains a faded block.");
            var paused = player.GetPlaybackPosition(); AudioEffectTests.Wait(native, 8); Check(player.GetPlaybackPosition() == paused, "Actual native pause freezes after final-block preparation.");
            Capture(() => player.StreamPaused = false, value => value > .001f && value < .124f, "Actual FAudio resume ramps from silence.");
            Capture(() => player.Seek(.5), value => value > .126f && value < .249f, "Seek overlaps outgoing fade with the full new attack.");
            var old = player.GetStreamPlayback(); player.Play(); Check(old.IsDisposed, "Public oldest replacement disposes the old handle."); AudioEffectTests.Wait(native, 8);
            Capture(player.Stop, value => value > .001f && value < .124f, "Actual FAudio Stop drains retained PCM.");
            Check(!player.IsPlaying() && !player.HasStreamPlayback(), "Public Stop immediately hides transient output.");
            AudioEffectTests.CheckOutput(native, Vector2.Zero);
            player.Play(); AudioEffectTests.Wait(native, 20); var bytes = native.MixManagedBytes; var calls = FAudioContext.AllocationCalls;
            AudioEffectTests.Wait(native, 64); Check(native.MixManagedBytes == bytes && FAudioContext.AllocationCalls == calls, "64 warm active native passes allocate zero measured bytes/calls.");
            player.StreamPaused = true; AudioEffectTests.Wait(native, 20); bytes = native.MixManagedBytes; calls = FAudioContext.AllocationCalls;
            AudioEffectTests.Wait(native, 64); Check(native.MixManagedBytes == bytes && FAudioContext.AllocationCalls == calls, "64 warm idle native passes allocate zero measured bytes/calls.");
            Reject<ArgumentOutOfRangeException>(() => player.VolumeDB = float.MaxValue); Check(player.VolumeLinear == .5f, "Unrepresentable gain rejects without changing stored controls.");
            player.VolumeLinear = float.MaxValue; Check(float.IsFinite(player.VolumeLinear), "Largest finite linear gain remains representable after dB rounding."); player.VolumeLinear = .5f;
            lock (native.Gate)
            {
                player.StreamPaused = false; stream.MixHook = () => player.Stream = null;
                Reject<Exception>(() => player.StreamPaused = true); Check(ReferenceEquals(player.Stream, stream), "Owner final-block callbacks cannot reenter player mutation."); stream.MixHook = null;
                player.StreamPaused = false; stream.MixHook = () => server.BusCount = 2;
                Reject<Exception>(() => player.StreamPaused = true); Check(server.BusCount == 1, "Owner final-block callbacks cannot rebuild the native graph."); stream.MixHook = null;
            }
            player.Stop(); player.MaxPolyphony = 2; player.Play(); var oldest = stream.Last!; player.Play(); oldest.FailDispose = true; player.MaxPolyphony = 1;
            Reject<AggregateException>(() => player.Play()); Check(oldest.IsDisposed && player.HasStreamPlayback(), "Failed trim still removes disposed slots and retains the newest voice."); oldest.FailDispose = false; player.Play();
            player.MaxPolyphony = 2; player.Play();
            lock (native.Gate)
            {
                stream.MixHook = () => throw new ApplicationException("Injected multi-voice pause failure.");
                Reject<AggregateException>(() => player.StreamPaused = true); stream.MixHook = null;
                Check(player.StreamPaused && !player.IsPlaying(), "Failed pause attempts and freezes every active slot.");
            }
            void Capture(Action action, Func<float, bool> predicate, string message)
            {
                lock (native.Gate) { native.PrepareCapture(native.QuantumFrames * native.Channels * 8); action(); }
                AudioEffectTests.Wait(native, 8); var pcm = native.CapturedPCM(); var found = false;
                for (var i = 0; i < pcm.Length; i += native.Channels) found |= predicate(pcm[i]);
                Check(found, message);
            }
        }
        finally { tree.Dispose(); server.CloseNative(); }
    }

    internal static void RunHost()
    {
        var settings = ProjectSettings.Instance; var method = settings.Get(ProjectSettings.RenderingMethod);
        var renderer = Environment.GetEnvironmentVariable("ELECTRON2D_AUDIO_RENDERER") == "compatibility" ? "compatibility" : "gpu";
        settings.Set(ProjectSettings.RenderingMethod, renderer);
        try
        {
            for (var run = 0; run < 2; run++)
            {
                using var stream = new ProbeStream(); using var capture = new AudioEffectCapture { BufferLength = .2f };
                var window = new Window { Size = new(160, 96) };
                var player = new AudioStreamPlayer2D { Stream = stream, Autoplay = true, Position = new(80, 48) };
                var scenario = new HostScenario(player, capture); window.AddChild(player); window.AddChild(scenario);
                var server = AudioServer.Instance; server.AddBusEffect(0, capture);
                try
                {
                    Check(Engine.Instance.Run(window) == 0 && scenario.Completed && window.IsDisposed && !stream.IsDisposed, "Public spatial transition host completed and cleaned up.");
                    Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(new { scenario = "audio-transitions", renderer, run, spatial = true, cleaned = window.IsDisposed }));
                }
                finally { if (!window.IsDisposed) window.Dispose(); server.RemoveBusEffect(0, 0); server.CloseNative(); }
            }
        }
        finally { settings.Set(ProjectSettings.RenderingMethod, method); }
    }
    private sealed class HostScenario(AudioStreamPlayer2D player, AudioEffectCapture capture) : Node
    {
        internal bool Completed; private int _phase; private double _elapsed; private bool _ramp;
        private double _paused;
        protected override void OnReady() => ProcessEnabled = true;
        protected override void OnProcess(double delta)
        {
            _elapsed += delta;
            var available = capture.GetFramesAvailable();
            if (available > 0)
            {
                var samples = capture.GetBuffer(available);
                if (_phase > 0) _ramp |= samples.Any(v => v.X > .001f && v.X < .06f);
                if (_phase == 0 && !samples.Any(v => v.X > .12f)) return;
            }
            if (_elapsed < .15) return;
            if (_phase == 0) { capture.ClearBuffer(); player.VolumeLinear = .5f; _phase++; }
            else if (_phase == 1) { player.StreamPaused = true; _paused = player.GetPlaybackPosition(); _phase++; }
            else if (_phase == 2) { Check(player.StreamPaused && player.GetPlaybackPosition() == _paused, "Spatial pause freezes its prepared cursor."); player.StreamPaused = false; _phase++; }
            else if (_phase == 3) { player.Seek(.5); _phase++; }
            else { player.Stop(); Check(!player.IsPlaying() && !player.HasStreamPlayback() && _ramp, "Spatial shared gain/pause/resume/seek/stop path emits ramp PCM."); Completed = true; Tree!.Quit(); }
            _elapsed = 0;
        }
    }

    private sealed class ProbeStream : AudioStream
    {
        internal Action? MixHook;
        internal ProbePlayback? Last;
        protected override bool OnIsMonophonic() => false;
        protected override AudioStreamPlayback OnInstantiatePlayback() => Last = new ProbePlayback { Hook = () => MixHook?.Invoke() };
    }
    private sealed class ProbePlayback : AudioStreamPlayback
    {
        internal Vector2 Sample = new(.25f, -.5f);
        internal int Remaining = int.MaxValue, Stops;
        internal bool FailMix, FailDispose; internal Action? Hook;
        private bool _active; private double _position;
        protected override void Dispose(bool disposing) { base.Dispose(disposing); if (disposing && FailDispose) throw new ApplicationException("Injected trim cleanup failure."); }
        protected override void OnStart(double fromPosition) { _position = fromPosition; _active = true; }
        protected override void OnStop() { _active = false; Stops++; }
        protected override void OnSeek(double toPosition) => _position = toPosition;
        protected override bool OnIsPlaying() => _active;
        protected override double OnGetPlaybackPosition() => _position;
        protected override int OnMix(Span<Vector2> buffer, float rateScale)
        {
            Hook?.Invoke(); if (FailMix) throw new InvalidOperationException("Injected final-block mix failure.");
            var count = _active ? Math.Min(buffer.Length, Remaining) : 0;
            buffer[..count].Fill(Sample); Remaining -= count; _position += count * (double)rateScale / AudioServer.Instance.GetMixRate(); return count;
        }
    }
    private static void Near(float actual, float expected, string message) { Check(Math.Abs(actual - expected) < 1e-6f, $"{message} {actual} vs {expected}."); }
    private static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    private static void Reject<T>(Action action) where T : Exception { try { action(); } catch (T) { return; } throw new InvalidOperationException($"Expected {typeof(T).Name}."); }
}
