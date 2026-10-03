using Electron2D;
using System.Runtime.InteropServices;
using System.Text.Json;

internal static class AudioCompressorTests
{
    internal static void Run(bool native = false)
    {
        ResourceState(); ReferencePCM(); EdgesAndWarm();
        if (native) Native();
        Console.WriteLine("Compressor: pinned linked envelope, controls/copies, finite edges, warmed CPU" + (native ? ", ordered named native sidechain" : "") + " passed.");
    }
    private static void ResourceState()
    {
        using var effect = new AudioEffectCompressor();
        Check(effect.Threshold == 0 && effect.Ratio == 4 && effect.Gain == 0 && effect.AttackUS == 20 && effect.ReleaseMS == 250 && effect.Mix == 1 && effect.Sidechain == "", "Seven default controls.");
        Check(effect.GetPropertyList().Count(p => p.IsStored && new[] { "Threshold", "Ratio", "Gain", "AttackUS", "ReleaseMS", "Mix", "Sidechain" }.Contains(p.Name)) == 7, "Seven stored typed descriptors.");
        Reject<ArgumentOutOfRangeException>(() => effect.Ratio = 0); Reject<ArgumentOutOfRangeException>(() => effect.Mix = float.NaN);
        Reject<ArgumentOutOfRangeException>(() => effect.AttackUS = -1); Reject<ArgumentOutOfRangeException>(() => effect.ReleaseMS = float.NaN);
        Reject<ArgumentOutOfRangeException>(() => effect.Threshold = float.PositiveInfinity); Reject<ArgumentOutOfRangeException>(() => effect.Gain = 1000);
        Reject<ArgumentNullException>(() => effect.Sidechain = null!);
        var notifications = 0; effect.Changed += _ => notifications++; effect.Sidechain = ""; effect.Sidechain = "Dialog"; Check(notifications == 1, "Only changed name notifies.");
        effect.Threshold = -18; effect.Ratio = .5f; effect.Gain = -3; effect.AttackUS = 0; effect.ReleaseMS = 0; effect.Mix = .4f;
        using var copy = (AudioEffectCompressor)effect.Duplicate(true); Check(copy.Snapshot() == effect.Snapshot(), "Exact concrete settings copy."); copy.Threshold = 0; Check(effect.Threshold == -18, "Copy controls independent.");
        effect.ResourceLocalToScene = true; using var holder = new Holder { Effect = effect }; using var packed = new PackedScene(); packed.Pack(holder); using var restored = (Holder)packed.Instantiate();
        Check(restored.Effect is not null && !ReferenceEquals(restored.Effect, effect) && restored.Effect.Snapshot() == effect.Snapshot(), "Scene-local concrete settings copy."); restored.Effect!.Dispose();
        Action<Resource> fail = _ => throw new ApplicationException("Compressor observer fixture."); effect.Changed += fail;
        Reject<ApplicationException>(() => effect.Mix = .5f); Check(effect.Mix == .5f, "Observer failure follows committed edit."); effect.Changed -= fail;
        using var callback = new CallbackEffect(() => effect.Sidechain = "Reentrant"); using var invocation = callback.Instantiate();
        Reject<InvalidOperationException>(() => invocation.Process([Vector2.Zero], new Vector2[1])); Check(effect.Sidechain == "Dialog", "Audio callback name edit rejects before mutation.");
        using var instance = effect.Instantiate(); Check(!instance.ProcessSilence(), "Inactive silence needs no output processing.");
        effect.Dispose(); Reject<ObjectDisposedException>(() => instance.Process([Vector2.Zero], new Vector2[1]));
    }
    private sealed record OracleCase(int Rate, int Profile, float Threshold, float Ratio, float Gain, float AttackUS, float ReleaseMS, float Mix, string Sidechain, float[] Input, float[] Detector, float[] PCM);
    private static void ReferencePCM()
    {
        using var resource = typeof(AudioCompressorTests).Assembly.GetManifestResourceStream("TestAudio.CompressorReference.json")!;
        var cases = JsonSerializer.Deserialize<OracleCase[]>(resource)!.Where(c => c.Rate == AudioServer.Instance.GetMixRate()).ToArray(); Check(cases.Length == 7, "Seven C++ profiles at the current rate.");
        var maxError = 0f;
        foreach (var item in cases)
        {
            using var effect = new AudioEffectCompressor { Threshold = item.Threshold, Ratio = item.Ratio, Gain = item.Gain, AttackUS = item.AttackUS, ReleaseMS = item.ReleaseMS, Mix = item.Mix, Sidechain = item.Sidechain };
            using var worker = effect.Instantiate(); using var alias = effect.Instantiate();
            var input = Vectors(item.Input); var detector = Vectors(item.Detector); var output = new Vector2[input.Length];
            if (item.Sidechain.Length == 0)
            {
                worker.Process(input.AsSpan(0, 127), output.AsSpan(0, 127)); worker.Process(input.AsSpan(127), output.AsSpan(127));
                var inPlace = (Vector2[])input.Clone(); alias.Process(inPlace, inPlace); Check(output.SequenceEqual(inPlace), "Split and aliased linked envelope agree.");
            }
            else ((AudioEffectCompressorInstance)worker).ProcessBlock(input, output, detector, effect.Snapshot());
            Check(item.PCM.Length == input.Length * 2, "Every oracle frame accounted.");
            for (var i = 0; i < output.Length; i++) { maxError = Math.Max(maxError, Math.Abs(output[i].X - item.PCM[2 * i])); maxError = Math.Max(maxError, Math.Abs(output[i].Y - item.PCM[2 * i + 1])); }
        }
        Check(maxError < .00001f, $"Compressor C++ maximum PCM error {maxError}."); Console.WriteLine($"Compressor C++ oracle: 7 profiles/14336 channel samples at {AudioServer.Instance.GetMixRate()} Hz; maximum error {maxError}.");
    }
    private static Vector2[] Vectors(float[] values) { var result = new Vector2[values.Length / 2]; for (var i = 0; i < result.Length; i++) result[i] = new(values[i * 2], values[i * 2 + 1]); return result; }
    private static void EdgesAndWarm()
    {
        using var effect = new AudioEffectCompressor { Threshold = -18, AttackUS = 0, ReleaseMS = 0 }; using var worker = effect.Instantiate(); var output = new Vector2[4];
        var input = new Vector2[] { new(.2f, -.4f), Vector2.Zero, new(.01f, -.02f), new(1, -.5f) };
        worker.Process(input, output); Check(output[0].X < .2f && Math.Abs(output[0].X / output[0].Y + .5f) < .000001f && output[1] == Vector2.Zero && output[2] == input[2], "Linked channels, exact silence and immediate release below threshold.");
        effect.Ratio = 1; worker.Process(input, output); Check(output.SequenceEqual(input), "Ratio one gives exact unity without makeup.");
        effect.Ratio = 4; effect.Mix = 0; effect.Threshold = -float.MaxValue; worker.Process(input, output); Check(output.SequenceEqual(input), "Dry bypass remains exact with underflowed detector threshold.");
        effect.AttackUS = -0f; effect.ReleaseMS = -0f; worker.Process(input, output); Check(output.SequenceEqual(input), "Both signs of zero timing give immediate finite envelopes.");
        effect.Mix = 1; effect.Ratio = 1; worker.Process(input, output); Check(output.SequenceEqual(input), "Underflowed threshold cannot contaminate unity ratio.");
        effect.Ratio = 4; effect.Gain = float.NegativeInfinity; worker.Process(input, output); Check(output.All(f => f == Vector2.Zero), "Zero makeup silences only wet component.");
        effect.Gain = 0; effect.Threshold = -18; effect.AttackUS = 2000; effect.ReleaseMS = 100;
        using var envelope = effect.Instantiate(); var loud = new Vector2[1024]; loud.AsSpan().Fill(new(1, -.5f)); var contour = new Vector2[1024]; envelope.Process(loud, contour);
        Check(contour[0].X > contour[^1].X * 2, "Attack retains initial transient."); loud.AsSpan().Fill(new(.01f, -.005f)); envelope.Process(loud, contour); Check(contour[^1].X > contour[0].X, "Release restores quiet gain.");
        effect.Ratio = .000001f; effect.AttackUS = 0; Reject<ArithmeticException>(() => worker.Process(input, output)); Check(output.All(f => f == Vector2.Zero), "Expansion overflow clears complete standalone block.");
        effect.Ratio = 4; using var fresh = effect.Instantiate(); var expected = new Vector2[4]; worker.Process(input, output); fresh.Process(input, expected); Check(output.SequenceEqual(expected), "Failed envelope resets for recovery.");
        Reject<ArgumentException>(() => worker.Process([new(float.NaN, 0)], new Vector2[1]));
        loud.AsSpan().Fill(new(.8f, -.6f)); for (var i = 0; i < 16; i++) worker.Process(loud, contour);
        var bytes = GC.GetAllocatedBytesForCurrentThread(); for (var i = 0; i < 64; i++) worker.Process(loud, contour); Check(GC.GetAllocatedBytesForCurrentThread() == bytes, "64 warmed active compressor blocks allocate zero bytes.");
        loud.AsSpan().Clear(); bytes = GC.GetAllocatedBytesForCurrentThread(); for (var i = 0; i < 64; i++) worker.Process(loud, contour); Check(GC.GetAllocatedBytesForCurrentThread() == bytes, "64 warmed silent compressor blocks allocate zero bytes.");
    }
    private static float Reduction(float detector, float threshold = -12) => Mathf.DBToLinear(-2.08136898f * Math.Max(0, Mathf.LinearToDB(detector / Mathf.DBToLinear(threshold))) * .75f);
    private static void Native()
    {
        var server = AudioServer.Instance; server.CloseNative(); server.BusCount = 3; server.SetBusName(1, "Music"); server.SetBusName(2, "Voice"); server.SetBusVolumeDB(2, -6);
        using var compressor = new AudioEffectCompressor { Threshold = -12, AttackUS = 0, ReleaseMS = 0, Sidechain = "Voice" }; using var capture = new AudioEffectCapture { BufferLength = .2f };
        using var music = AudioEffectTests.Constant(); using var voice = Constant(new(.9f, -.6f));
        var root = new Node(); var musicPlayer = new AudioStreamPlayer { Name = "MusicPlayer", Stream = music, Bus = "Music" }; var voicePlayer = new AudioStreamPlayer { Name = "VoicePlayer", Stream = voice, Bus = "Voice" }; root.AddChild(musicPlayer); root.AddChild(voicePlayer); using var tree = new SceneTree(root);
        try
        {
            server.AddBusEffect(1, compressor); server.AddBusEffect(1, capture); musicPlayer.Play(); voicePlayer.Play(); var native = server.Native; AudioEffectTests.Wait(native, 20);
            var borrowed = server.GetBusEffectInstance(1, 0); CheckCapture(native, capture, new Vector2(.2f, -.3f) * Reduction(.9f * Mathf.DBToLinear(-6)), "Earlier bus is post-effects/gain detector.");
            Reject<InvalidOperationException>(borrowed.Dispose);
            server.MoveBus(1, -1); Check(ReferenceEquals(borrowed, server.GetBusEffectInstance(2, 0)), "Routing reorder preserves envelope identity.");
            CheckCapture(native, capture, new Vector2(.2f, -.3f) * Reduction(.9f), "Later bus is direct raw detector before its gain.");
            server.SetBusName(1, "Dialog"); voicePlayer.Bus = "Dialog";
            CheckCapture(native, capture, new(.2f, -.3f), "Missing old name reads currently silent Master, not own input.");
            compressor.Sidechain = "Dialog"; CheckCapture(native, capture, new Vector2(.2f, -.3f) * Reduction(.9f), "Live name sees renamed later bus.");
            using var silence = new AudioEffectAmplify { VolumeDB = float.NegativeInfinity }; server.AddBusEffect(1, silence);
            CheckCapture(native, capture, new Vector2(.2f, -.3f) * Reduction(.9f), "Later bus detector precedes its public effects.");
            server.MoveBus(1, -1); CheckCapture(native, capture, new(.2f, -.3f), "Earlier bus detector includes its silencing effect.");
            server.RemoveBusEffect(2, 0); server.SetBusVolumeDB(2, 0); server.SetBusSend(2, "Music");
            compressor.Sidechain = "Music"; CheckCapture(native, capture, new Vector2(1.1f, -.9f) * Reduction(1.1f), "Self detector includes upstream sends before this effect.");
            compressor.Sidechain = ""; CheckCapture(native, capture, new Vector2(1.1f, -.9f) * Reduction(1.1f), "Empty sidechain uses the exact current effect input.");
            server.SetBusBypassEffects(1, true); CheckCapture(native, capture, new Vector2(1.1f, -.9f) * Reduction(1.1f), "Bypassed capture does not consume frames while bus continues.", bypassed: true); server.SetBusBypassEffects(1, false);
            var bytes = native.MixManagedBytes; var calls = FAudioContext.AllocationCalls; AudioEffectTests.Wait(native, 64); Check(native.MixManagedBytes == bytes && FAudioContext.AllocationCalls == calls, "64 warmed native sidechain passes allocate zero bytes/calls.");
            musicPlayer.StreamPaused = true; voicePlayer.StreamPaused = true; AudioEffectTests.Wait(native, 20); bytes = native.MixManagedBytes; calls = FAudioContext.AllocationCalls; AudioEffectTests.Wait(native, 64); Check(native.MixManagedBytes == bytes && FAudioContext.AllocationCalls == calls, "64 warmed paused native passes allocate zero bytes/calls.");
            server.RemoveBus(2); Check(!borrowed.IsDisposed, "Removing detector bus leaves target envelope alive.");
        }
        finally { tree.Dispose(); server.CloseNative(); while (server.GetBusEffectCount(1) > 0) server.RemoveBusEffect(1, 0); server.CloseNative(); server.BusCount = 1; }
    }
    private static void CheckCapture(FAudioContext native, AudioEffectCapture capture, Vector2 expected, string message, bool bypassed = false)
    {
        AudioEffectTests.Wait(native, 8); lock (native.Gate) capture.ClearBuffer(); AudioEffectTests.Wait(native, 8); var available = capture.GetFramesAvailable();
        if (bypassed) { Check(available == 0, message); return; }
        Check(available > 0, "Native target bus capture contains PCM."); var frames = capture.GetBuffer(available);
        for (var i = 0; i < frames.Length; i++)
        {
            var pair = i / native.QuantumFrames % (native.Channels / 2); var target = pair == 0 ? expected : Vector2.Zero;
            Check(frames[i].DistanceTo(target) < .00015f, message + $" Pair {pair}: expected {target}, got {frames[i]}.");
        }
    }
    private static AudioStreamWAV Constant(Vector2 value)
    {
        var data = new short[44100 * 2]; for (var i = 0; i < data.Length; i += 2) { data[i] = (short)(value.X * 32768); data[i + 1] = (short)(value.Y * 32768); }
        return new AudioStreamWAV { SampleFormat = AudioStreamWAV.Format.PCM16, Stereo = true, MixRate = 44100, Data = MemoryMarshal.AsBytes(data.AsSpan()).ToArray(), Loop = AudioLoopMode.Forward, LoopEnd = 44099 };
    }
    internal static void RunHost()
    {
        var settings = ProjectSettings.Instance; var prior = settings.Get(ProjectSettings.RenderingMethod); var backend = Environment.GetEnvironmentVariable("ELECTRON2D_AUDIO_RENDERER") == "compatibility" ? "compatibility" : "gpu"; settings.Set(ProjectSettings.RenderingMethod, backend);
        try
        {
            for (var run = 0; run < 2; run++)
            {
                var server = AudioServer.Instance; server.BusCount = 3; server.SetBusName(1, "Music"); server.SetBusName(2, "Voice");
                using var compressor = new AudioEffectCompressor { Threshold = -12, AttackUS = 0, ReleaseMS = 0, Sidechain = "Voice" }; using var capture = new AudioEffectCapture { BufferLength = .2f }; using var music = AudioEffectTests.Constant(); using var voice = Constant(new(.9f, -.6f));
                var window = new Window { Size = new(160, 96) }; window.AddChild(new AudioStreamPlayer { Name = "MusicPlayer", Stream = music, Bus = "Music", Autoplay = true }); window.AddChild(new AudioStreamPlayer { Name = "VoicePlayer", Stream = voice, Bus = "Voice", Autoplay = true }); var scenario = new HostScenario(capture); window.AddChild(scenario); server.AddBusEffect(1, compressor); server.AddBusEffect(1, capture);
                try { Check(Engine.Instance.Run(window) == 0 && scenario.Completed && window.IsDisposed, "Public compressor host processed and cleaned up."); }
                finally { if (!window.IsDisposed) window.Dispose(); server.CloseNative(); while (server.GetBusEffectCount(1) > 0) server.RemoveBusEffect(1, 0); server.CloseNative(); server.BusCount = 1; }
                Console.WriteLine(JsonSerializer.Serialize(new { scenario = "compressor-host", backend, run, scenario.Completed, cleaned = window.IsDisposed }));
            }
        }
        finally { settings.Set(ProjectSettings.RenderingMethod, prior); }
    }
    private sealed class HostScenario(AudioEffectCapture capture) : Node
    {
        internal bool Completed; private double _elapsed;
        protected override void OnReady() { ProcessEnabled = true; }
        protected override void OnProcess(double delta)
        {
            _elapsed += delta; var available = capture.GetFramesAvailable(); var expected = new Vector2(.2f, -.3f) * Reduction(.9f);
            if (available > 0) Completed |= capture.GetBuffer(available).Any(f => f.DistanceTo(expected) < .00015f);
            if (_elapsed < .3) return; Check(Completed, "Public Window host captured sidechain-ducked PCM."); Tree!.Quit();
        }
    }
    private sealed class Holder : Node
    {
        public AudioEffectCompressor? Effect { get; set; }
        private static readonly PropertyDescriptor[] Properties = [new PropertyDescriptor<Holder, AudioEffectCompressor?>(nameof(Effect), p => p.Effect, (p, v) => p.Effect = v, _ => null, stored: true)];
        protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors() => base.GetPropertyDescriptors().Concat(Properties);
        protected override Func<Node> CreateSceneInstanceFactory() => Create;
        private static Node Create() => new Holder();
    }
    private sealed class CallbackEffect(Action callback) : AudioEffect
    {
        protected override AudioEffectInstance OnInstantiate() => new CallbackInstance(callback);
    }
    private sealed class CallbackInstance(Action callback) : AudioEffectInstance
    {
        protected override void OnProcess(ReadOnlySpan<Vector2> source, Span<Vector2> destination) { callback(); source.CopyTo(destination); }
    }
    private static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    private static void Reject<T>(Action action) where T : Exception { try { action(); } catch (T) { return; } throw new InvalidOperationException($"Expected {typeof(T).Name}."); }
}
