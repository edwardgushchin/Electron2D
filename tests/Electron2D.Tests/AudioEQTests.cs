using Electron2D;
using System.Runtime.InteropServices;
using System.Text.Json;

internal static class AudioEQTests
{
    internal static void Run(bool native = false)
    {
        Resources(); ReferencePCM(); CPUAndWarm();
        if (native) Native();
        Console.WriteLine("Audio EQ: presets, indexed gain, copies, stereo filters, native PCM and warmed allocation passed.");
    }

    private static AudioEffectEQ[] Family() => [new AudioEffectEQ(), new AudioEffectEQ6(), new AudioEffectEQ10(), new AudioEffectEQ21()];

    private sealed record OracleCase(int Bands, float[] PCM);
    private static void ReferencePCM()
    {
        using var stream = typeof(AudioEQTests).Assembly.GetManifestResourceStream("TestAudio.EQReference.json")!;
        var cases = JsonSerializer.Deserialize<OracleCase[]>(stream)!;
        Check(cases.Length == 3, "Pinned EQ preset oracle census.");
        var input = new Vector2[256];
        for (var i = 0; i < input.Length; i++) input[i] = i == 0 ? new(.75f, -.5f) : new(((i * 37) % 101 - 50) / 128f, ((i * 37 + 11) % 101 - 50) / 128f);
        var output = new Vector2[input.Length]; var maxError = 0f;
        foreach (var item in cases)
        {
            using AudioEffectEQ eq = item.Bands switch { 6 => new AudioEffectEQ6(), 10 => new AudioEffectEQ10(), 21 => new AudioEffectEQ21(), _ => throw new InvalidOperationException("Unknown EQ oracle preset.") };
            eq.SetBandGainDB(0, 6); eq.SetBandGainDB(item.Bands - 1, -9);
            using var instance = eq.Instantiate();
            instance.Process(input.AsSpan(0, 113), output.AsSpan(0, 113)); instance.Process(input.AsSpan(113), output.AsSpan(113));
            Check(item.PCM.Length == 512, "Pinned EQ PCM length.");
            for (var i = 0; i < output.Length; i++)
            {
                maxError = Math.Max(maxError, Math.Abs(output[i].X - item.PCM[i * 2]));
                maxError = Math.Max(maxError, Math.Abs(output[i].Y - item.PCM[i * 2 + 1]));
            }
        }
        Check(maxError < .00003f, $"Pinned C++ EQ PCM max error {maxError}.");
        Console.WriteLine($"EQ C++ oracle: 3 presets/1536 channel samples, maximum absolute PCM error {maxError}.");
    }

    private static void Resources()
    {
        foreach (var eq in Family()) using (eq)
            {
                var count = eq is AudioEffectEQ21 ? 21 : eq is AudioEffectEQ10 ? 10 : 6;
                Check(eq.GetBandCount() == count, "Preset count.");
                for (var i = 0; i < count; i++) Check(eq.GetBandGainDB(i) == 0, "Band default.");
                Check(eq.GetPropertyList().Count(p => p.Name.StartsWith("BandDB/", StringComparison.Ordinal)) == count, "Indexed authoring fields.");
                Reject<ArgumentOutOfRangeException>(() => eq.GetBandGainDB(-1)); Reject<ArgumentOutOfRangeException>(() => eq.SetBandGainDB(count, 1));
                foreach (var bad in new[] { float.NaN, float.PositiveInfinity, float.MaxValue }) Reject<ArgumentOutOfRangeException>(() => eq.SetBandGainDB(0, bad));
                eq.SetBandGainDB(0, 12); eq.SetBandGainDB(count - 1, float.NegativeInfinity);
                using var copy = (AudioEffectEQ)eq.Duplicate(true);
                Check(copy.GetType() == eq.GetType() && copy.GetBandCount() == count && copy.GetBandGainDB(0) == 12 && copy.GetBandGainDB(count - 1) == float.NegativeInfinity, "Exact preset and gain copy.");
                Action<Resource> fail = _ => throw new ApplicationException("EQ observer fixture."); eq.Changed += fail;
                Reject<ApplicationException>(() => eq.SetBandGainDB(0, 6)); Check(eq.GetBandGainDB(0) == 6, "Observer failure follows committed gain."); eq.Changed -= fail;
                eq.ResourceLocalToScene = true; using var holder = new Holder { Effect = eq }; using var scene = new PackedScene(); scene.Pack(holder);
                using var restored = (Holder)scene.Instantiate();
                Check(restored.Effect is not null && !ReferenceEquals(restored.Effect, eq) && restored.Effect.GetType() == eq.GetType() && restored.Effect.GetBandGainDB(0) == 6, "Scene-local concrete EQ copy.");
                restored.Effect!.Dispose();
                using var instance = eq.Instantiate(); Check(!instance.ProcessSilence(), "No inactive silence processing.");
                eq.Dispose(); Reject<ObjectDisposedException>(() => instance.Process([Vector2.Zero], new Vector2[1]));
            }
    }

    private static void CPUAndWarm()
    {
        foreach (var eq in Family()) using (eq)
            {
                using var split = eq.Instantiate(); using var alias = eq.Instantiate();
                var input = Tone(44100, 100, 8000, 512); var output = new Vector2[input.Length];
                split.Process(input.AsSpan(0, 113), output.AsSpan(0, 113)); split.Process(input.AsSpan(113), output.AsSpan(113));
                var inPlace = (Vector2[])input.Clone(); alias.Process(inPlace, inPlace);
                Check(inPlace.SequenceEqual(output), "Split blocks and aliasing retain exact independent history.");
                Check(output.All(f => float.IsFinite(f.X) && float.IsFinite(f.Y)), "Default finite PCM.");
                using var silence = eq.Instantiate(); var quiet = new Vector2[128]; silence.Process(quiet, quiet);
                Check(quiet.All(f => f == Vector2.Zero), "Independent silent instance has no inherited history.");
                var baseline = RMS(output);
                var lowBand = eq.GetBandCount() switch { 10 => 2, 21 => 5, _ => 1 };
                var highBand = eq.GetBandCount() switch { 10 => 8, 21 => 17, _ => 5 };
                eq.SetBandGainDB(lowBand, 18); eq.SetBandGainDB(highBand, -18);
                for (var i = 0; i < 4; i++) split.Process(input, output);
                var tuned = RMS(output);
                Check(tuned.X > baseline.X * 1.5f && tuned.Y < baseline.Y * .9f, $"Frequency-selective live gain {eq.GetType().Name}: {baseline} -> {tuned}.");
                var bytes = GC.GetAllocatedBytesForCurrentThread();
                for (var i = 0; i < 64; i++) { eq.SetBandGainDB(lowBand, i % 2 == 0 ? 6 : 12); split.Process(input, output); }
                Check(GC.GetAllocatedBytesForCurrentThread() == bytes, "64 warmed live-edit EQ blocks allocate zero managed bytes.");
                input.AsSpan().Clear(); for (var i = 0; i < 20; i++) split.Process(input, output);
                bytes = GC.GetAllocatedBytesForCurrentThread(); for (var i = 0; i < 64; i++) split.Process(input, output);
                Check(GC.GetAllocatedBytesForCurrentThread() == bytes, "64 warmed silent EQ blocks allocate zero managed bytes.");
            }
        using var extreme = new AudioEffectEQ6();
        for (var i = 0; i < extreme.GetBandCount(); i++) extreme.SetBandGainDB(i, float.NegativeInfinity);
        using var extremeInstance = extreme.Instantiate(); var spike = new[] { new Vector2(10000, 0) }; var result = new Vector2[1];
        extremeInstance.Process(spike, result); Check(result[0] == Vector2.Zero, "All-silent bands produce exact silence.");
        extreme.SetBandGainDB(1, 760); Reject<ArithmeticException>(() => extremeInstance.Process(spike, result));
        extreme.SetBandGainDB(1, 0); extremeInstance.Process([new(.1f, -.2f)], result);
        Check(result[0].IsFinite(), "Overflow resets EQ histories and permits standalone recovery.");
    }

    private static void Native()
    {
        var server = AudioServer.Instance; server.CloseNative(); server.BusCount = 1;
        using var eq = new AudioEffectEQ6(); using var stream = Wave(); var root = new Node(); var player = new AudioStreamPlayer { Stream = stream }; root.AddChild(player); using var tree = new SceneTree(root);
        try
        {
            server.AddBusEffect(0, eq); player.Play(); var native = server.Native;
            AudioEffectTests.Wait(native, 20); var baseline = Measure(native);
            eq.SetBandGainDB(1, 18); eq.SetBandGainDB(5, -18); var tuned = Measure(native);
            Check(tuned.X > baseline.X * 1.5f && tuned.Y < baseline.Y * .9f, "Live native FAudio EQ changes distinct stereo frequencies.");
            server.SetBusEffectEnabled(0, 0, false); var bypass = Measure(native);
            Check(bypass.X < tuned.X * .8f && bypass.Y > tuned.Y, "Native EQ disable passes original PCM.");
            server.SetBusEffectEnabled(0, 0, true); AudioEffectTests.Wait(native, 20);
            var bytes = native.MixManagedBytes; var allocations = FAudioContext.AllocationCalls;
            AudioEffectTests.Wait(native, 64);
            Check(native.MixManagedBytes == bytes && FAudioContext.AllocationCalls == allocations, "64 active native EQ passes allocate no measured bytes/calls.");
            player.StreamPaused = true; AudioEffectTests.Wait(native, 20); bytes = native.MixManagedBytes; allocations = FAudioContext.AllocationCalls;
            AudioEffectTests.Wait(native, 64); Check(native.MixManagedBytes == bytes && FAudioContext.AllocationCalls == allocations, "64 paused native EQ passes allocate no measured bytes/calls.");
        }
        finally { tree.Dispose(); server.CloseNative(); while (server.GetBusEffectCount(0) > 0) server.RemoveBusEffect(0, 0); server.CloseNative(); }
    }

    internal static void RunHost()
    {
        var settings = ProjectSettings.Instance;
        var renderer = Environment.GetEnvironmentVariable("ELECTRON2D_AUDIO_RENDERER") == "compatibility" ? "compatibility" : "gpu";
        var previous = settings.Get(ProjectSettings.RenderingMethod);
        settings.Set(ProjectSettings.RenderingMethod, renderer);
        try
        {
            using var stream = Wave(); using var eq = new AudioEffectEQ6(); using var capture = new AudioEffectCapture { BufferLength = .1f };
            eq.SetBandGainDB(1, 18); eq.SetBandGainDB(5, -18);
            var window = new Window { Size = new(160, 96), Title = "Electron2D EQ" };
            window.AddChild(new AudioStreamPlayer { Stream = stream, Autoplay = true });
            var scenario = new HostScenario(capture); window.AddChild(scenario);
            var server = AudioServer.Instance; server.AddBusEffect(0, eq); server.AddBusEffect(0, capture);
            try
            {
                Check(Engine.Instance.Run(window) == 0 && scenario.Completed && window.IsDisposed, "Public EQ host and cleanup.");
                Console.WriteLine($"Audio EQ host: {renderer}, {scenario.Frames} captured processed frames, cleanup passed.");
            }
            finally { if (!window.IsDisposed) window.Dispose(); while (server.GetBusEffectCount(0) > 0) server.RemoveBusEffect(0, 0); server.CloseNative(); }
        }
        finally { settings.Set(ProjectSettings.RenderingMethod, previous); }
    }

    private sealed class HostScenario(AudioEffectCapture capture) : Node
    {
        internal int Frames; internal bool Completed;
        private double _elapsed, _left, _right;
        protected override void OnReady() { capture.ClearBuffer(); ProcessEnabled = true; }
        protected override void OnProcess(double delta)
        {
            _elapsed += delta;
            var available = capture.GetFramesAvailable();
            if (available > 0)
            {
                foreach (var frame in capture.GetBuffer(available)) { Frames++; _left += frame.X * frame.X; _right += frame.Y * frame.Y; }
            }
            if (_elapsed < .35) return;
            Completed = Frames > 0 && _left > _right * 2;
            Check(Completed, "Public host captured frequency-selective EQ output.");
            Tree!.Quit();
        }
    }

    private static AudioStreamWAV Wave()
    {
        var frames = Tone(44100, 100, 8000, 44100);
        var samples = new short[frames.Length * 2];
        for (var i = 0; i < frames.Length; i++) { samples[i * 2] = (short)(frames[i].X * 32768); samples[i * 2 + 1] = (short)(frames[i].Y * 32768); }
        return new AudioStreamWAV { SampleFormat = AudioStreamWAV.Format.PCM16, Stereo = true, MixRate = 44100, Data = MemoryMarshal.AsBytes(samples.AsSpan()).ToArray(), Loop = AudioStreamWAV.LoopMode.Forward, LoopEnd = frames.Length - 1 };
    }
    private static Vector2[] Tone(int rate, int leftHZ, int rightHZ, int frames)
    {
        var result = new Vector2[frames];
        for (var i = 0; i < frames; i++) result[i] = new((float)(Math.Sin(Math.Tau * leftHZ * i / rate) * .2), (float)(Math.Sin(Math.Tau * rightHZ * i / rate) * .2));
        return result;
    }
    private static Vector2 RMS(ReadOnlySpan<Vector2> frames)
    {
        double left = 0, right = 0;
        foreach (var frame in frames) { left += frame.X * frame.X; right += frame.Y * frame.Y; }
        return new((float)Math.Sqrt(left / frames.Length), (float)Math.Sqrt(right / frames.Length));
    }
    private static Vector2 Measure(FAudioContext native)
    {
        AudioEffectTests.Wait(native, 12); native.PrepareCapture(native.QuantumFrames * native.Channels * 25); AudioEffectTests.Wait(native, 27);
        var pcm = native.CapturedPCM(); double left = 0, right = 0; var count = pcm.Length / native.Channels;
        for (var i = 0; i < pcm.Length; i += native.Channels) { left += pcm[i] * pcm[i]; right += pcm[i + 1] * pcm[i + 1]; }
        return new((float)Math.Sqrt(left / count), (float)Math.Sqrt(right / count));
    }
    private sealed class Holder : Node
    {
        protected override Func<Node> CreateSceneInstanceFactory() => Create;
        private static Node Create() => new Holder();
        public AudioEffectEQ? Effect { get; set; }
        private static readonly PropertyDescriptor[] Properties = [new PropertyDescriptor<Holder, AudioEffectEQ?>(nameof(Effect), p => p.Effect, (p, v) => p.Effect = v, _ => null, stored: true)];
        protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors() => base.GetPropertyDescriptors().Concat(Properties);
    }
    private static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    private static void Reject<T>(Action action) where T : Exception { try { action(); } catch (T) { return; } throw new InvalidOperationException($"Expected {typeof(T).Name}."); }
}
