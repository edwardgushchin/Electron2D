using Electron2D;
using System.Runtime.InteropServices;
using System.Text.Json;

internal static class AudioLimiterTests
{
    internal static void Run(bool native = false)
    {
        ResourceState(); ReferencePCM(); EdgesAndWarm(); PartialOverlap();
        if (native) Native();
        Console.WriteLine("Soft limiter: pinned curve, live controls, copies, finite edges and warmed CPU" + (native ? "/native PCM" : "") + " passed.");
    }
    private static void ResourceState()
    {
        using var effect = new AudioEffectLimiter();
        Check(effect.ThresholdDB == 0 && effect.CeilingDB == -.1f && effect.SoftClipDB == 2 && effect.SoftClipRatio == 10, "Four legacy defaults.");
        Check(effect.GetPropertyList().Count(p => p.IsStored && new[] { "ThresholdDB", "CeilingDB", "SoftClipDB", "SoftClipRatio" }.Contains(p.Name)) == 4, "Four typed stored settings.");
        Reject<ArgumentOutOfRangeException>(() => effect.ThresholdDB = float.NaN);
        Reject<ArgumentOutOfRangeException>(() => effect.CeilingDB = 1000);
        Reject<ArgumentOutOfRangeException>(() => effect.CeilingDB = float.NegativeInfinity);
        Reject<ArgumentOutOfRangeException>(() => effect.SoftClipDB = float.PositiveInfinity);
        Reject<ArgumentOutOfRangeException>(() => effect.SoftClipRatio = float.NaN);
        Check(effect.CeilingDB == -.1f && effect.ThresholdDB == 0, "Invalid authoring leaves prior settings.");
        var notifications = 0; effect.Changed += _ => notifications++;
        effect.SoftClipRatio = 10; effect.SoftClipRatio = -7; Check(notifications == 1, "Only changed settings notify, including inert ratio.");
        effect.ThresholdDB = -18; effect.CeilingDB = -9; effect.SoftClipDB = 6;
        using var copy = (AudioEffectLimiter)effect.Duplicate(true);
        Check(copy.Snapshot() == effect.Snapshot(), "Resource duplicate preserves all controls and concrete type.");
        copy.SoftClipDB = 4; Check(effect.SoftClipDB == 6, "Copies own independent controls.");
        effect.ResourceLocalToScene = true; using var holder = new Holder { Effect = effect }; using var packed = new PackedScene(); packed.Pack(holder);
        using var restored = (Holder)packed.Instantiate();
        Check(restored.Effect is not null && !ReferenceEquals(restored.Effect, effect) && restored.Effect.Snapshot() == effect.Snapshot(), "Scene-local concrete controls copied.");
        restored.Effect!.Dispose();
        Action<Resource> fail = _ => throw new ApplicationException("Limiter observer fixture."); effect.Changed += fail;
        Reject<ApplicationException>(() => effect.ThresholdDB = -12); Check(effect.ThresholdDB == -12, "Observer failure follows committed setting."); effect.Changed -= fail;
        using var instance = effect.Instantiate(); Check(!instance.ProcessSilence(), "No history or silent-input tail.");
        effect.Dispose(); Reject<ObjectDisposedException>(() => instance.Process([Vector2.Zero], new Vector2[1]));
        Reject<ObjectDisposedException>(() => effect.SoftClipRatio = 5);
    }
    private sealed record OracleCase(float ThresholdDB, float CeilingDB, float SoftClipDB, float SoftClipRatio, float[] Input, float[] PCM);
    private static void ReferencePCM()
    {
        using var resource = typeof(AudioLimiterTests).Assembly.GetManifestResourceStream("TestAudio.LimiterReference.json")!;
        var cases = JsonSerializer.Deserialize<OracleCase[]>(resource)!; Check(cases.Length == 8, "Eight independent C++ profiles.");
        var maxError = 0f;
        foreach (var item in cases)
        {
            using var effect = new AudioEffectLimiter { ThresholdDB = item.ThresholdDB, CeilingDB = item.CeilingDB, SoftClipDB = item.SoftClipDB, SoftClipRatio = item.SoftClipRatio };
            using var split = effect.Instantiate(); using var alias = effect.Instantiate();
            var input = new Vector2[item.Input.Length / 2]; var output = new Vector2[input.Length];
            for (var i = 0; i < input.Length; i++) input[i] = new(item.Input[2 * i], item.Input[2 * i + 1]);
            split.Process(input.AsSpan(0, 37), output.AsSpan(0, 37)); split.Process(input.AsSpan(37), output.AsSpan(37));
            var inPlace = (Vector2[])input.Clone(); alias.Process(inPlace, inPlace); Check(output.SequenceEqual(inPlace), "Split and aliased processing agree exactly.");
            Check(item.PCM.Length == input.Length * 2, "Every oracle frame accounted.");
            for (var i = 0; i < output.Length; i++) { maxError = Math.Max(maxError, Math.Abs(output[i].X - item.PCM[2 * i])); maxError = Math.Max(maxError, Math.Abs(output[i].Y - item.PCM[2 * i + 1])); }
        }
        Check(maxError < .000001f, $"C++ soft-limiter maximum PCM error {maxError}.");
        Console.WriteLine($"Soft limiter C++ oracle: 8 profiles/4096 channel samples; maximum error {maxError}.");
    }
    private static void EdgesAndWarm()
    {
        using var effect = new AudioEffectLimiter { ThresholdDB = -12, CeilingDB = -6, SoftClipDB = 4 };
        using var worker = effect.Instantiate(); using var other = effect.Instantiate();
        var input = new Vector2[] { Vector2.Zero, new(.01f, -.02f), new(1, -1), new(float.MaxValue, -float.MaxValue), new(float.Epsilon, -float.Epsilon) }; var output = new Vector2[input.Length];
        worker.Process(input, output); Check(output[0] == Vector2.Zero && output[1].DistanceTo(input[1] * Mathf.DBToLinear(6)) < .000001f, "Zero and sub-branch gain compensation.");
        Check(output[2] == new Vector2(Mathf.DBToLinear(-6), -Mathf.DBToLinear(-6)), "Strict signed sample cap without lookahead.");
        var previous = (Vector2[])output.Clone(); effect.SoftClipRatio = float.MaxValue; worker.Process(input, output); Check(output.SequenceEqual(previous), "Ratio has no PCM effect.");
        effect.ThresholdDB = 0; effect.CeilingDB = -20; worker.Process(input, output); other.Process(input, previous); Check(output.SequenceEqual(previous), "Existing instances share live edits without processing state.");
        effect.SoftClipDB = -5; worker.Process(input, output); Check(output.All(f => float.IsFinite(f.X) && float.IsFinite(f.Y)), "Raw singular coefficient above ceiling is harmless.");
        effect.CeilingDB = 6; effect.ThresholdDB = 6; effect.SoftClipDB = 0;
        var boundary = new Vector2[1]; worker.Process([new(1, MathF.BitIncrement(1))], boundary);
        Check(boundary[0].X == 1 && boundary[0].Y > 1.8f && boundary[0].Y < Mathf.DBToLinear(6), "Strict branch equality and legacy discontinuity are preserved per channel.");
        foreach (var ceiling in new float[] { -float.MaxValue, -1000, -20, -.1f, 0, 100, 700 })
            foreach (var threshold in new float[] { -float.MaxValue, -1000, 0, 1000, float.MaxValue })
                foreach (var soft in new float[] { -float.MaxValue, -1000, -25 - ceiling, -ceiling, 0, 1000, float.MaxValue })
                {
                    effect.CeilingDB = ceiling; effect.ThresholdDB = threshold; effect.SoftClipDB = soft;
                    worker.Process(input, output); var cap = Mathf.DBToLinear(ceiling);
                    Check(output.All(f => float.IsFinite(f.X) && float.IsFinite(f.Y) && Math.Abs(f.X) <= cap && Math.Abs(f.Y) <= cap) && output[0] == Vector2.Zero, "245 finite extreme configurations remain bounded and preserve silence.");
                }
        Reject<ArgumentException>(() => worker.Process([new(float.NaN, 0)], new Vector2[1]));
        Reject<ArgumentException>(() => worker.Process(input, output.AsSpan(1)));
        effect.CeilingDB = -.1f; effect.ThresholdDB = 0; effect.SoftClipDB = 2;
        var warmInput = new Vector2[512]; var warmOutput = new Vector2[512]; warmInput.AsSpan().Fill(new(1, -1.4f));
        for (var i = 0; i < 16; i++) worker.Process(warmInput, warmOutput);
        var bytes = GC.GetAllocatedBytesForCurrentThread(); for (var i = 0; i < 64; i++) worker.Process(warmInput, warmOutput);
        Check(GC.GetAllocatedBytesForCurrentThread() == bytes, "64 warmed active CPU blocks allocate zero bytes.");
        warmInput.AsSpan().Clear(); bytes = GC.GetAllocatedBytesForCurrentThread(); for (var i = 0; i < 64; i++) worker.Process(warmInput, warmOutput);
        Check(GC.GetAllocatedBytesForCurrentThread() == bytes, "64 warmed silent CPU blocks allocate zero bytes.");
    }
    private static void PartialOverlap()
    {
        using (var effect = new AudioEffectLimiter())
        using (var reference = effect.Instantiate())
        using (var overlap = effect.Instantiate())
        {
            var floats = Enumerable.Range(0, 10).Select(i => .03f * i).ToArray(); var expected = new Vector2[4];
            var source = MemoryMarshal.Cast<float, Vector2>(floats.AsSpan(0, 8));
            var destination = MemoryMarshal.Cast<float, Vector2>(floats.AsSpan(1, 8));
            reference.Process(source, expected); overlap.Process(source, destination);
            Check(destination.SequenceEqual(expected), "A one-float offset also preserves partially overlapping stereo frames.");
        }
        foreach (var effect in new AudioEffect[] { new AudioEffectLimiter(), new AudioEffectAmplify { VolumeDB = 6 }, new AudioEffectDelay() })
            using (effect)
                foreach (var offset in new[] { -1, 1 })
                {
                    var frames = Enumerable.Range(0, 9).Select(i => new Vector2(.03f * i, -.04f * i)).ToArray();
                    var sourceStart = offset < 0 ? 1 : 0; var destinationStart = offset > 0 ? 1 : 0;
                    var expected = new Vector2[8]; using var reference = effect.Instantiate(); using var overlap = effect.Instantiate();
                    reference.Process(frames.AsSpan(sourceStart, 8), expected);
                    overlap.Process(frames.AsSpan(sourceStart, 8), frames.AsSpan(destinationStart, 8));
                    Check(frames.AsSpan(destinationStart, 8).SequenceEqual(expected), "Partial overlap preserves original PCM for limiter and sibling kernels.");
                    var silence = new Vector2[8]; var actualTail = new Vector2[8]; var expectedTail = new Vector2[8];
                    overlap.Process(silence, actualTail); reference.Process(silence, expectedTail); Check(actualTail.SequenceEqual(expectedTail), "Partial overlap preserves subsequent stateful output.");
                    var bytes = GC.GetAllocatedBytesForCurrentThread();
                    for (var i = 0; i < 64; i++) { frames.AsSpan().Clear(); overlap.Process(frames.AsSpan(sourceStart, 8), frames.AsSpan(destinationStart, 8)); }
                    Check(GC.GetAllocatedBytesForCurrentThread() == bytes, "64 warmed partial-overlap blocks reuse prepared storage.");
                }
    }
    private static void Native()
    {
        var server = AudioServer.Service; server.CloseNative(); AudioServer.BusCount = 1;
        using var effect = new AudioEffectLimiter { ThresholdDB = -18, CeilingDB = -12, SoftClipDB = 24 };
        using var stream = AudioEffectTests.Constant(); var root = new Node(); var player = new AudioStreamPlayer { Stream = stream }; root.AddChild(player); using var tree = new SceneTree(root);
        try
        {
            AudioServer.AddBusEffect(0, effect); player.Play(); var native = server.Native; AudioEffectTests.Wait(native, 20);
            var borrowed = AudioServer.GetBusEffectInstance(0, 0);
            AudioEffectTests.CheckOutput(native, new(Mathf.DBToLinear(-12), -Mathf.DBToLinear(-12)));
            Reject<InvalidOperationException>(() => borrowed.Process([Vector2.Zero], new Vector2[1])); Reject<InvalidOperationException>(borrowed.Dispose);
            effect.CeilingDB = -6; AudioEffectTests.Wait(native, 8); AudioEffectTests.CheckOutput(native, new(Mathf.DBToLinear(-6), -Mathf.DBToLinear(-6)));
            AudioServer.SetBusEffectEnabled(0, 0, false); AudioEffectTests.CheckOutput(native, new(.2f, -.3f));
            AudioServer.SetBusEffectEnabled(0, 0, true); AudioEffectTests.Wait(native, 20);
            var bytes = native.MixManagedBytes; var calls = FAudioContext.AllocationCalls; AudioEffectTests.Wait(native, 64);
            Check(native.MixManagedBytes == bytes && FAudioContext.AllocationCalls == calls, "64 warmed native active passes allocate zero measured bytes/calls.");
            player.StreamPaused = true; AudioEffectTests.Wait(native, 20); bytes = native.MixManagedBytes; calls = FAudioContext.AllocationCalls; AudioEffectTests.Wait(native, 64);
            Check(native.MixManagedBytes == bytes && FAudioContext.AllocationCalls == calls, "64 warmed native paused passes allocate zero measured bytes/calls.");
            AudioServer.RemoveBusEffect(0, 0); Check(borrowed.IsDisposed && !effect.IsDisposed, "Removed bus state invalidates but resource survives.");
        }
        finally { tree.Dispose(); server.CloseNative(); while (AudioServer.GetBusEffectCount(0) > 0) AudioServer.RemoveBusEffect(0, 0); server.CloseNative(); }
    }
    internal static void RunHost()
    {
        var settings = ProjectSettings.Service; var prior = ProjectSettings.Get(ProjectSettings.RenderingMethod);
        var backend = Environment.GetEnvironmentVariable("ELECTRON2D_AUDIO_RENDERER") == "compatibility" ? "compatibility" : "gpu"; ProjectSettings.Set(ProjectSettings.RenderingMethod, backend);
        try
        {
            for (var run = 0; run < 2; run++)
            {
                using var effect = new AudioEffectLimiter { ThresholdDB = -18, CeilingDB = -12, SoftClipDB = 24 }; using var stream = AudioEffectTests.Constant(); using var capture = new AudioEffectCapture { BufferLength = .1f };
                var window = new Window { Size = new(160, 96) }; window.AddChild(new AudioStreamPlayer { Stream = stream, Autoplay = true }); var scenario = new HostScenario(capture); window.AddChild(scenario);
                var server = AudioServer.Service; AudioServer.AddBusEffect(0, effect); AudioServer.AddBusEffect(0, capture);
                try { Check(Engine.Run(window) == 0 && scenario.Completed && window.IsDisposed, "Public soft-limiter host processed and cleaned up."); }
                finally { if (!window.IsDisposed) window.Dispose(); while (AudioServer.GetBusEffectCount(0) > 0) AudioServer.RemoveBusEffect(0, 0); server.CloseNative(); }
                Console.WriteLine(JsonSerializer.Serialize(new { scenario = "soft-limiter-host", backend, run, scenario.Completed, cleaned = window.IsDisposed }));
            }
        }
        finally { ProjectSettings.Set(ProjectSettings.RenderingMethod, prior); }
    }
    private sealed class HostScenario(AudioEffectCapture capture) : Node
    {
        internal bool Completed; private double _elapsed;
        protected override void OnReady() { ProcessEnabled = true; }
        protected override void OnProcess(double delta)
        {
            _elapsed += delta; var available = capture.GetFramesAvailable(); var cap = Mathf.DBToLinear(-12);
            if (available > 0) Completed |= capture.GetBuffer(available).Any(f => Math.Abs(f.X - cap) < .0001f && Math.Abs(f.Y + cap) < .0001f);
            if (_elapsed < .3) return; Check(Completed, "Public Window host captured soft-limited stereo PCM."); Tree!.Quit();
        }
    }
    private sealed class Holder : Node
    {
        public AudioEffectLimiter? Effect { get; set; }
        private static readonly PropertyDescriptor[] Properties = [new PropertyDescriptor<Holder, AudioEffectLimiter?>(nameof(Effect), p => p.Effect, (p, v) => p.Effect = v, _ => null, stored: true)];
        protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors() => base.GetPropertyDescriptors().Concat(Properties);
        protected override Func<Node> CreateSceneInstanceFactory() => Create;
        private static Node Create() => new Holder();
    }
    private static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    private static void Reject<T>(Action action) where T : Exception { try { action(); } catch (T) { return; } throw new InvalidOperationException($"Expected {typeof(T).Name}."); }
}
