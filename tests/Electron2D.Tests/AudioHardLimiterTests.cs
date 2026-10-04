using Electron2D;
using System.Text.Json;

internal static class AudioHardLimiterTests
{
    internal static void Run(bool native = false)
    {
        ResourceState(); ReferencePCM(); EdgesAndWarm();
        if (native) Native();
        Console.WriteLine(native ? "Hard limiter: pinned PCM, peak safety, native output and allocation passed."
            : "Hard limiter: pinned PCM, peak safety and CPU allocation passed.");
    }

    private static void ResourceState()
    {
        using var effect = new AudioEffectHardLimiter();
        Check(effect.CeilingDB == -.3f && effect.PreGainDB == 0 && effect.Release == .1f, "Hard limiter defaults.");
        Check(effect.GetPropertyList().Count(p => p.IsStored && p.Name is "CeilingDB" or "PreGainDB" or "Release") == 3,
            "Three typed stored controls.");
        Reject<ArgumentOutOfRangeException>(() => effect.CeilingDB = float.NaN);
        Reject<ArgumentOutOfRangeException>(() => effect.CeilingDB = float.PositiveInfinity);
        Reject<ArgumentOutOfRangeException>(() => effect.PreGainDB = float.PositiveInfinity);
        Reject<ArgumentOutOfRangeException>(() => effect.Release = 0);
        Reject<ArgumentOutOfRangeException>(() => effect.Release = float.NaN);
        effect.CeilingDB = -9; effect.PreGainDB = 6; effect.Release = .015f;
        using var copy = (AudioEffectHardLimiter)effect.Duplicate(true);
        Check(copy.CeilingDB == -9 && copy.PreGainDB == 6 && copy.Release == .015f, "Resource copy retains settings only.");
        effect.ResourceLocalToScene = true; using var holder = new Holder { Effect = effect }; using var packed = new PackedScene(); packed.Pack(holder);
        using var restored = (Holder)packed.Instantiate();
        Check(restored.Effect is AudioEffectHardLimiter sceneCopy && !ReferenceEquals(sceneCopy, effect) &&
              sceneCopy.CeilingDB == -9 && sceneCopy.PreGainDB == 6 && sceneCopy.Release == .015f,
            "Scene-local copy retains settings.");
        restored.Effect!.Dispose();
        Action<Resource> fail = _ => throw new ApplicationException("Hard limiter observer fixture."); effect.Changed += fail;
        Reject<ApplicationException>(() => effect.CeilingDB = -6);
        Check(effect.CeilingDB == -6, "Observer failure follows committed edit."); effect.Changed -= fail;
        using var instance = effect.Instantiate(); Check(instance.ProcessSilence(), "Lookahead continues through silent blocks.");
        effect.Dispose(); Reject<ObjectDisposedException>(() => instance.Process([Vector2.Zero], new Vector2[1]));
    }

    private sealed record OracleCase(int Rate, int Profile, float[] Input, float[] PCM);
    private static void ReferencePCM()
    {
        var rate = (int)AudioServer.GetMixRate();
        using var resource = typeof(AudioHardLimiterTests).Assembly.GetManifestResourceStream("TestAudio.HardLimiterReference.json")!;
        var cases = JsonSerializer.Deserialize<OracleCase[]>(resource)!.Where(item => item.Rate == rate).ToArray();
        Check(cases.Length == 2, "Two pinned limiter profiles at the output rate.");
        var maxError = 0f;
        foreach (var item in cases)
        {
            using var effect = item.Profile == 0 ? new AudioEffectHardLimiter() : new AudioEffectHardLimiter { PreGainDB = 6, CeilingDB = -9, Release = .015f };
            using var split = effect.Instantiate(); using var alias = effect.Instantiate();
            var input = new Vector2[item.Input.Length / 2]; var output = new Vector2[input.Length];
            for (var i = 0; i < input.Length; i++) input[i] = new(item.Input[i * 2], item.Input[i * 2 + 1]);
            split.Process(input.AsSpan(0, 127), output.AsSpan(0, 127));
            split.Process(input.AsSpan(127, 313), output.AsSpan(127, 313));
            split.Process(input.AsSpan(440), output.AsSpan(440));
            var inPlace = (Vector2[])input.Clone(); alias.Process(inPlace, inPlace);
            Check(inPlace.SequenceEqual(output), "Aliased and split processing have identical histories.");
            Check(item.PCM.Length == input.Length * 2, "Pinned limiter sample count.");
            var ceiling = Mathf.DBToLinear(effect.CeilingDB);
            for (var i = 0; i < output.Length; i++)
            {
                maxError = MathF.Max(maxError, MathF.Abs(output[i].X - item.PCM[i * 2]));
                maxError = MathF.Max(maxError, MathF.Abs(output[i].Y - item.PCM[i * 2 + 1]));
                Check(MathF.Abs(output[i].X) <= ceiling && MathF.Abs(output[i].Y) <= ceiling,
                    "Corrected limiter never emits a sample beyond its ceiling.");
            }
            var delay = (int)Math.Ceiling(rate * .002) + 1;
            Check(output.AsSpan(0, delay).IndexOfAnyExcept(Vector2.Zero) < 0 && output[delay] != Vector2.Zero,
                "Prepared two-millisecond lookahead has an exact initial silent interval.");
        }
        Check(maxError < .0003f, $"Pinned C++ PCM maximum absolute error after ceiling correction {maxError}.");
        Console.WriteLine($"Hard limiter C++ oracle: 2 profiles/16384 channel samples at {rate} Hz; maximum error {maxError}.");
    }

    private static void EdgesAndWarm()
    {
        using var effect = new AudioEffectHardLimiter { CeilingDB = -12, PreGainDB = 12, Release = .02f };
        using var instance = effect.Instantiate();
        var input = new Vector2[512]; input.AsSpan().Fill(new(.2f, -.3f)); var output = new Vector2[input.Length];
        for (var i = 0; i < 12; i++) instance.Process(input, output);
        var ceiling = Mathf.DBToLinear(-12);
        Check(output.Any(f => f.X > .05f && f.Y < -.05f) && output.All(f => MathF.Abs(f.X) <= ceiling && MathF.Abs(f.Y) <= ceiling),
            "Linked stereo reduction preserves sign and enforces peak ceiling.");
        Check(MathF.Abs(output[^1].X / output[^1].Y + 2f / 3f) < .001f,
            "One detector gain preserves the steady stereo channel ratio.");
        effect.CeilingDB = -24; effect.PreGainDB = 18; effect.Release = .01f;
        instance.Process(input, output); ceiling = Mathf.DBToLinear(-24);
        Check(output.All(f => MathF.Abs(f.X) <= ceiling && MathF.Abs(f.Y) <= ceiling), "Live settings take effect at the next block.");
        using var fresh = effect.Instantiate(); var freshOutput = new Vector2[512]; fresh.Process(input, freshOutput);
        Check(freshOutput[0] == Vector2.Zero && output[0] != Vector2.Zero, "New instance has an independent lookahead line.");
        input.AsSpan().Clear();
        for (var i = 0; i < 16; i++) instance.Process(input, output);
        Check(output.All(f => f == Vector2.Zero), "Silent blocks flush finite delayed source PCM.");

        using var fast = new AudioEffectHardLimiter { CeilingDB = -24, Release = .000001f };
        using var fastInstance = fast.Instantiate();
        fastInstance.Process([new(1, -1)], new Vector2[1]);
        fastInstance.Process(new Vector2[2048], new Vector2[2048]);
        var quiet = new Vector2[256]; quiet.AsSpan().Fill(new(.02f, -.02f));
        fastInstance.Process(quiet, quiet);
        Check(quiet[^1].DistanceTo(new(.02f, -.02f)) < .000001f,
            "Positive sub-sample release recovers full gain after the sustain window.");
        using (var silentCeiling = new AudioEffectHardLimiter { CeilingDB = float.NegativeInfinity })
        using (var silentInstance = silentCeiling.Instantiate())
        {
            input.AsSpan().Fill(new(.5f, -.5f));
            silentInstance.Process(input, output);
            Check(output.All(f => f == Vector2.Zero), "A zero-linear ceiling produces silence.");
        }

        using var overflow = new AudioEffectHardLimiter { PreGainDB = 24 };
        using var damaged = overflow.Instantiate(); using var pristine = overflow.Instantiate();
        Reject<ArithmeticException>(() => damaged.Process([new(float.MaxValue, 0)], new Vector2[1]));
        var single = new Vector2[1]; var expected = new Vector2[1];
        damaged.Process([new(.25f, -.25f)], single); pristine.Process([new(.25f, -.25f)], expected);
        Check(single.SequenceEqual(expected), "Nonfinite pre-gain resets the instance for the next source block.");

        using var warm = new AudioEffectHardLimiter(); using var worker = warm.Instantiate();
        input.AsSpan().Fill(new(.6f, -.7f)); for (var i = 0; i < 16; i++) worker.Process(input, output);
        var bytes = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < 64; i++) worker.Process(input, output);
        Check(GC.GetAllocatedBytesForCurrentThread() == bytes, "64 warmed active limiter blocks allocate zero managed bytes.");
        input.AsSpan().Clear(); bytes = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < 64; i++) worker.Process(input, output);
        Check(GC.GetAllocatedBytesForCurrentThread() == bytes, "64 warmed silent limiter blocks allocate zero managed bytes.");
    }

    private static void Native()
    {
        var server = AudioServer.Service; server.CloseNative(); AudioServer.BusCount = 1;
        using var effect = new AudioEffectHardLimiter { CeilingDB = -12, PreGainDB = 12, Release = .02f };
        using var stream = AudioEffectTests.Constant(); var root = new Node(); var player = new AudioStreamPlayer { Stream = stream }; root.AddChild(player); using var tree = new SceneTree(root);
        try
        {
            AudioServer.AddBusEffect(0, effect); player.Play(); var native = server.Native; AudioEffectTests.Wait(native, 30);
            var borrowed = AudioServer.GetBusEffectInstance(0, 0);
            native.PrepareCapture(native.QuantumFrames * native.Channels * 8); AudioEffectTests.Wait(native, 10);
            var pcm = native.CapturedPCM(); var ceiling = Mathf.DBToLinear(-12);
            Check(pcm.Length > 0, "Native limiter PCM captured.");
            for (var i = 0; i < pcm.Length; i += native.Channels)
            {
                Check(pcm[i] > .02f && pcm[i + 1] < -.02f && MathF.Abs(pcm[i]) <= ceiling && MathF.Abs(pcm[i + 1]) <= ceiling,
                    "Native front pair contains linked, bounded stereo PCM.");
                for (var channel = 2; channel < native.Channels; channel++) Check(MathF.Abs(pcm[i + channel]) < .0001f, "Other output pairs stay silent.");
            }
            AudioServer.SetBusEffectEnabled(0, 0, false); AudioEffectTests.CheckOutput(native, new(.2f, -.3f));
            AudioServer.SetBusEffectEnabled(0, 0, true); AudioEffectTests.Wait(native, 20);
            var bytes = native.MixManagedBytes; var calls = FAudioContext.AllocationCalls; AudioEffectTests.Wait(native, 64);
            Check(native.MixManagedBytes == bytes && FAudioContext.AllocationCalls == calls, "64 warmed native active passes allocate no measured bytes/calls.");
            player.StreamPaused = true; AudioEffectTests.Wait(native, 20); bytes = native.MixManagedBytes; calls = FAudioContext.AllocationCalls;
            AudioEffectTests.Wait(native, 64);
            Check(native.MixManagedBytes == bytes && FAudioContext.AllocationCalls == calls, "64 warmed native paused passes allocate no measured bytes/calls.");
            AudioServer.RemoveBusEffect(0, 0); Check(borrowed.IsDisposed && !effect.IsDisposed, "Bus removal releases borrowed state but retains resource.");
        }
        finally { tree.Dispose(); server.CloseNative(); while (AudioServer.GetBusEffectCount(0) > 0) AudioServer.RemoveBusEffect(0, 0); server.CloseNative(); }
    }

    internal static void RunHost()
    {
        var settings = ProjectSettings.Service; var previous = ProjectSettings.Get(ProjectSettings.RenderingMethod);
        ProjectSettings.Set(ProjectSettings.RenderingMethod, Environment.GetEnvironmentVariable("ELECTRON2D_AUDIO_RENDERER") == "compatibility" ? "compatibility" : "gpu");
        try
        {
            using var effect = new AudioEffectHardLimiter { CeilingDB = -12, PreGainDB = 12 };
            using var stream = AudioEffectTests.Constant(); using var capture = new AudioEffectCapture { BufferLength = .1f };
            var window = new Window { Size = new(160, 96) }; window.AddChild(new AudioStreamPlayer { Stream = stream, Autoplay = true });
            var scenario = new HostScenario(capture); window.AddChild(scenario);
            var server = AudioServer.Service; AudioServer.AddBusEffect(0, effect); AudioServer.AddBusEffect(0, capture);
            try { Check(Engine.Run(window) == 0 && scenario.Completed && window.IsDisposed, "Public hard-limiter host processed and cleaned up."); }
            finally { if (!window.IsDisposed) window.Dispose(); while (AudioServer.GetBusEffectCount(0) > 0) AudioServer.RemoveBusEffect(0, 0); server.CloseNative(); }
        }
        finally { ProjectSettings.Set(ProjectSettings.RenderingMethod, previous); }
    }
    private sealed class HostScenario(AudioEffectCapture capture) : Node
    {
        internal bool Completed; private double _elapsed;
        protected override void OnReady() { ProcessEnabled = true; }
        protected override void OnProcess(double delta)
        {
            _elapsed += delta; var available = capture.GetFramesAvailable();
            if (available > 0)
                Completed |= capture.GetBuffer(available).Any(f => f.X > .02f && f.Y < -.02f &&
                    MathF.Abs(f.X) <= Mathf.DBToLinear(-12) && MathF.Abs(f.Y) <= Mathf.DBToLinear(-12));
            if (_elapsed < .3) return;
            Check(Completed, "Public Window host captured limited stereo PCM."); Tree!.Quit();
        }
    }
    private sealed class Holder : Node
    {
        public AudioEffectHardLimiter? Effect { get; set; }
        private static readonly PropertyDescriptor[] Properties = [new PropertyDescriptor<Holder, AudioEffectHardLimiter?>(nameof(Effect), p => p.Effect, (p, v) => p.Effect = v, _ => null, stored: true)];
        protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors() => base.GetPropertyDescriptors().Concat(Properties);
        protected override Func<Node> CreateSceneInstanceFactory() => Create;
        private static Node Create() => new Holder();
    }
    private static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    private static void Reject<T>(Action action) where T : Exception { try { action(); } catch (T) { return; } throw new InvalidOperationException($"Expected {typeof(T).Name}."); }
}
