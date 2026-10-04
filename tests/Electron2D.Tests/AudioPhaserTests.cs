using Electron2D;
using System.Runtime.InteropServices;
using System.Text.Json;

internal static class AudioPhaserTests
{
    internal static void Run(bool native = false)
    {
        ResourceState(); ReferencePCM(); EdgesAndWarm();
        if (native) { Native(); NativeTail(); }
        Console.WriteLine(native ? "Phaser: pinned six-stage PCM, native output and allocation passed."
            : "Phaser: pinned six-stage PCM and warmed CPU allocation passed.");
    }

    private static void ResourceState()
    {
        using var effect = new AudioEffectPhaser();
        Check(effect.RangeMinHZ == 440 && effect.RangeMaxHZ == 1600 && effect.RateHZ == .5f &&
              effect.Feedback == .7f && effect.Depth == 1, "Phaser defaults.");
        Check(effect.GetPropertyList().Count(p => p.IsStored && p.Name is "RangeMinHZ" or "RangeMaxHZ" or "RateHZ" or "Feedback" or "Depth") == 5,
            "Five stored typed controls.");
        Reject<ArgumentOutOfRangeException>(() => effect.RangeMinHZ = 9);
        Reject<ArgumentOutOfRangeException>(() => effect.RangeMinHZ = float.NaN);
        Reject<ArgumentOutOfRangeException>(() => effect.RangeMaxHZ = 10001);
        Reject<ArgumentOutOfRangeException>(() => effect.RangeMaxHZ = float.PositiveInfinity);
        Reject<ArgumentOutOfRangeException>(() => effect.RateHZ = 0);
        Reject<ArgumentOutOfRangeException>(() => effect.RateHZ = 21);
        Reject<ArgumentOutOfRangeException>(() => effect.Feedback = 0);
        Reject<ArgumentOutOfRangeException>(() => effect.Feedback = 1);
        Reject<ArgumentOutOfRangeException>(() => effect.Depth = 0);
        Reject<ArgumentOutOfRangeException>(() => effect.Depth = float.NegativeInfinity);
        Check(effect.RangeMinHZ == 440 && effect.RangeMaxHZ == 1600 && effect.RateHZ == .5f &&
              effect.Feedback == .7f && effect.Depth == 1, "Rejected controls leave settings unchanged.");
        effect.RangeMinHZ = 8500; effect.RangeMaxHZ = 100; effect.RateHZ = 20; effect.Feedback = .9f; effect.Depth = 4;
        using var copy = (AudioEffectPhaser)effect.Duplicate(true);
        Check(copy.RangeMinHZ == 8500 && copy.RangeMaxHZ == 100 && copy.RateHZ == 20 && copy.Feedback == .9f && copy.Depth == 4,
            "Resource copy retains all controls, including reversed endpoints.");
        effect.ResourceLocalToScene = true; using var holder = new Holder { Effect = effect }; using var packed = new PackedScene(); packed.Pack(holder);
        using var restored = (Holder)packed.Instantiate();
        Check(restored.Effect is AudioEffectPhaser sceneCopy && !ReferenceEquals(sceneCopy, effect) &&
              sceneCopy.RangeMinHZ == 8500 && sceneCopy.RangeMaxHZ == 100 && sceneCopy.RateHZ == 20 &&
              sceneCopy.Feedback == .9f && sceneCopy.Depth == 4, "Scene-local copy retains phaser controls.");
        restored.Effect!.Dispose();
        Action<Resource> fail = _ => throw new ApplicationException("Phaser observer fixture."); effect.Changed += fail;
        Reject<ApplicationException>(() => effect.Depth = 2); Check(effect.Depth == 2, "Observer error follows committed edit."); effect.Changed -= fail;
        using var instance = effect.Instantiate(); Check(instance.ProcessSilence(), "All-pass feedback continues over silent input.");
        effect.Dispose(); Reject<ObjectDisposedException>(() => instance.Process([Vector2.Zero], new Vector2[1]));
    }

    private sealed record OracleCase(int Rate, int Profile, float[] Input, float[] PCM);
    private static void ReferencePCM()
    {
        var rate = (int)AudioServer.GetMixRate();
        using var stream = typeof(AudioPhaserTests).Assembly.GetManifestResourceStream("TestAudio.PhaserReference.json")!;
        var cases = JsonSerializer.Deserialize<OracleCase[]>(stream)!.Where(item => item.Rate == rate).ToArray();
        Check(cases.Length == 2, "Two pinned C++ phaser profiles at the output rate.");
        var maximum = 0f;
        foreach (var item in cases)
        {
            using var effect = item.Profile == 0 ? new AudioEffectPhaser() : new AudioEffectPhaser
            { RangeMinHZ = 100, RangeMaxHZ = 8500, RateHZ = 20, Feedback = .9f, Depth = 2 };
            using var split = effect.Instantiate(); using var alias = effect.Instantiate();
            var input = new Vector2[item.Input.Length / 2]; var output = new Vector2[input.Length];
            for (var i = 0; i < input.Length; i++) input[i] = new(item.Input[2 * i], item.Input[2 * i + 1]);
            split.Process(input.AsSpan(0, 37), output.AsSpan(0, 37));
            split.Process(input.AsSpan(37, 91), output.AsSpan(37, 91));
            split.Process(input.AsSpan(128), output.AsSpan(128));
            var inPlace = (Vector2[])input.Clone(); alias.Process(inPlace, inPlace);
            Check(inPlace.SequenceEqual(output), "Split and aliased processing preserve independent stereo histories.");
            Check(item.PCM.Length == output.Length * 2, "Pinned phaser channel count.");
            for (var i = 0; i < output.Length; i++)
            {
                maximum = MathF.Max(maximum, MathF.Abs(output[i].X - item.PCM[2 * i]));
                maximum = MathF.Max(maximum, MathF.Abs(output[i].Y - item.PCM[2 * i + 1]));
            }
        }
        Check(maximum < .0005f, $"Pinned C++ phaser maximum absolute PCM error {maximum}.");
        Console.WriteLine($"Phaser C++ oracle: 2 profiles/16384 channel samples at {rate} Hz, maximum absolute error {maximum}.");
    }

    private static void EdgesAndWarm()
    {
        using var effect = new AudioEffectPhaser { RangeMinHZ = 1000, RangeMaxHZ = 1000, RateHZ = .01f, Feedback = .1f };
        using var processor = effect.Instantiate(); var output = new Vector2[1];
        processor.Process([new(1, 0)], output);
        var d = 1000f / (AudioServer.GetMixRate() * .5f);
        var a = (1 - d) / (1 + d);
        Check(Math.Abs(output[0].X - (1 + MathF.Pow(a, 6))) < .0001f && Math.Abs(output[0].Y) < .00001f,
            "Six cascaded all-pass stages have the expected first impulse gain without cross-channel leakage.");
        var silence = new Vector2[512]; var tail = new Vector2[512]; processor.Process(silence, tail);
        Check(tail.Any(frame => Math.Abs(frame.X) > .0001f) && tail.All(frame => Math.Abs(frame.Y) < .00001f),
            "Six-stage and feedback history produce an isolated left-channel silent tail.");
        effect.RangeMinHZ = 8500; effect.RangeMaxHZ = 100; effect.RateHZ = 20; effect.Feedback = .9f; effect.Depth = 4;
        processor.Process([new(.2f, -.3f)], output);
        Check(output[0].IsFinite(), "Live reversed sweep and feedback edits remain finite.");
        using var unsafeEffect = new AudioEffectPhaser { Depth = 4 };
        using var unsafeInstance = unsafeEffect.Instantiate();
        Reject<ArithmeticException>(() => unsafeInstance.Process([new(float.MaxValue, 0)], new Vector2[1]));
        unsafeEffect.Depth = 1; var recovered = new Vector2[1]; unsafeInstance.Process([new(.1f, -.2f)], recovered);
        using var fresh = unsafeEffect.Instantiate(); fresh.Process([new(.1f, -.2f)], output);
        Check(recovered[0] == output[0], "Overflow clears all stages, feedback and oscillator phase.");
        var input = new Vector2[128]; input.AsSpan().Fill(new(.2f, -.3f)); var result = new Vector2[128];
        using var warm = effect.Instantiate(); for (var i = 0; i < 16; i++) warm.Process(input, result);
        var bytes = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < 64; i++) warm.Process(input, result);
        Check(GC.GetAllocatedBytesForCurrentThread() == bytes, "64 warmed active blocks allocate zero managed bytes.");
        input.AsSpan().Clear(); bytes = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < 64; i++) warm.Process(input, result);
        Check(GC.GetAllocatedBytesForCurrentThread() == bytes, "64 warmed silent blocks allocate zero managed bytes.");
    }

    private static void Native()
    {
        var server = AudioServer.Service; server.CloseNative(); AudioServer.BusCount = 1;
        using var effect = new AudioEffectPhaser { RateHZ = 20, Feedback = .9f, Depth = 4 };
        using var stream = AudioEffectTests.Constant(); var root = new Node(); var player = new AudioStreamPlayer { Stream = stream }; root.AddChild(player); using var tree = new SceneTree(root);
        try
        {
            AudioServer.AddBusEffect(0, effect); var borrowed = AudioServer.GetBusEffectInstance(0, 0); player.Play(); var native = server.Native;
            AudioEffectTests.Wait(native, 20); native.PrepareCapture(native.QuantumFrames * native.Channels * 8); AudioEffectTests.Wait(native, 10);
            var pcm = native.CapturedPCM(); var changed = false;
            for (var frame = 0; frame < pcm.Length; frame += native.Channels)
            {
                changed |= Math.Abs(pcm[frame] - .2f) > .03f && Math.Abs(pcm[frame + 1] + .3f) > .03f;
                for (var channel = 2; channel < native.Channels; channel++)
                    Check(Math.Abs(pcm[frame + channel]) < .0001f, "Phaser does not leak into unrelated output pairs.");
            }
            Check(changed, "Native FAudio output contains a distinct stereo phaser signal.");
            AudioServer.SetBusEffectEnabled(0, 0, false); AudioEffectTests.CheckOutput(native, new(.2f, -.3f));
            AudioServer.SetBusEffectEnabled(0, 0, true); AudioEffectTests.Wait(native, 20);
            var bytes = native.MixManagedBytes; var calls = FAudioContext.AllocationCalls; AudioEffectTests.Wait(native, 64);
            Check(native.MixManagedBytes == bytes && FAudioContext.AllocationCalls == calls, "64 warmed native active passes allocate no measured bytes/calls.");
            player.StreamPaused = true; AudioEffectTests.Wait(native, 20); bytes = native.MixManagedBytes; calls = FAudioContext.AllocationCalls;
            AudioEffectTests.Wait(native, 64);
            Check(native.MixManagedBytes == bytes && FAudioContext.AllocationCalls == calls, "64 warmed native paused passes allocate no measured bytes/calls.");
            AudioServer.RemoveBusEffect(0, 0); Check(borrowed.IsDisposed && !effect.IsDisposed, "Removal releases only the borrowed instance.");
        }
        finally { tree.Dispose(); server.CloseNative(); while (AudioServer.GetBusEffectCount(0) > 0) AudioServer.RemoveBusEffect(0, 0); server.CloseNative(); }
    }

    private static void NativeTail()
    {
        var server = AudioServer.Service; server.CloseNative(); AudioServer.BusCount = 1;
        using var effect = new AudioEffectPhaser { RangeMinHZ = 10, RangeMaxHZ = 10, RateHZ = .01f, Feedback = .9f, Depth = 4 };
        var samples = new short[512]; samples[0] = 16384; samples[1] = -16384;
        using var stream = new AudioStreamWAV
        {
            SampleFormat = AudioStreamWAV.Format.PCM16,
            Stereo = true,
            MixRate = (int)AudioServer.GetMixRate(),
            Data = MemoryMarshal.AsBytes(samples.AsSpan()).ToArray()
        };
        var root = new Node(); var player = new AudioStreamPlayer { Stream = stream }; root.AddChild(player); using var tree = new SceneTree(root);
        try
        {
            AudioServer.AddBusEffect(0, effect); var native = server.Native;
            native.PrepareCapture(native.QuantumFrames * native.Channels * 70); player.Play(); AudioEffectTests.Wait(native, 70);
            var pcm = native.CapturedPCM(); var found = false;
            for (var frame = 512; frame < pcm.Length / native.Channels; frame++)
                found |= Math.Abs(pcm[frame * native.Channels]) > .00001f;
            Check(found, "Finite source leaves a native phaser feedback tail after voice completion.");
            tree.ProcessFrame(.01); Check(!player.IsPlaying(), "Finite voice ends before the phaser tail.");
        }
        finally { tree.Dispose(); server.CloseNative(); while (AudioServer.GetBusEffectCount(0) > 0) AudioServer.RemoveBusEffect(0, 0); server.CloseNative(); }
    }

    internal static void RunHost()
    {
        var settings = ProjectSettings.Service; var previous = ProjectSettings.Get(ProjectSettings.RenderingMethod);
        ProjectSettings.Set(ProjectSettings.RenderingMethod, Environment.GetEnvironmentVariable("ELECTRON2D_AUDIO_RENDERER") == "compatibility" ? "compatibility" : "gpu");
        try
        {
            using var effect = new AudioEffectPhaser { RateHZ = 20, Feedback = .9f, Depth = 4 };
            using var stream = AudioEffectTests.Constant(); using var capture = new AudioEffectCapture { BufferLength = .1f };
            var window = new Window { Size = new(160, 96) }; window.AddChild(new AudioStreamPlayer { Stream = stream, Autoplay = true });
            var scenario = new HostScenario(capture); window.AddChild(scenario);
            var server = AudioServer.Service; AudioServer.AddBusEffect(0, effect); AudioServer.AddBusEffect(0, capture);
            try { Check(Engine.Run(window) == 0 && scenario.Completed && window.IsDisposed, "Public phaser host processed and cleaned up."); }
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
            if (available > 0) Completed |= capture.GetBuffer(available).Any(frame =>
                Math.Abs(frame.X - .2f) > .03f && Math.Abs(frame.Y + .3f) > .03f);
            if (_elapsed < .3) return;
            Check(Completed, "Public Window host captured processed stereo phaser PCM."); Tree!.Quit();
        }
    }
    private sealed class Holder : Node
    {
        public AudioEffectPhaser? Effect { get; set; }
        private static readonly PropertyDescriptor[] Properties = [new PropertyDescriptor<Holder, AudioEffectPhaser?>(nameof(Effect), p => p.Effect, (p, v) => p.Effect = v, _ => null, stored: true)];
        protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors() => base.GetPropertyDescriptors().Concat(Properties);
        protected override Func<Node> CreateSceneInstanceFactory() => Create;
        private static Node Create() => new Holder();
    }
    private static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    private static void Reject<T>(Action action) where T : Exception { try { action(); } catch (T) { return; } throw new InvalidOperationException($"Expected {typeof(T).Name}."); }
}
