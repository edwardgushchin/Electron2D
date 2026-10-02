using Electron2D;
using System.Runtime.InteropServices;
using System.Text.Json;

internal static class AudioReverbTests
{
    internal static void Run(bool native = false)
    {
        ResourceState(); ReferencePCM(); CPUAndWarm();
        if (native) { Native(); NativeTail(); }
        Console.WriteLine(native ? "Audio reverb: typed state, pinned PCM, stereo rooms, native tails and warmed allocation passed."
            : "Audio reverb: typed state, pinned PCM, stereo rooms and warmed CPU allocation passed.");
    }

    private static void ResourceState()
    {
        using var effect = new AudioEffectReverb();
        Check(effect.PredelayMSEC == 150 && effect.PredelayFeedback == .4f && effect.RoomSize == .8f &&
              effect.Damping == .5f && effect.Spread == 1 && effect.Hipass == 0 && effect.Dry == 1 && effect.Wet == .5f,
              "Reverb resource defaults.");
        var fields = new[] { nameof(AudioEffectReverb.PredelayMSEC), nameof(AudioEffectReverb.PredelayFeedback),
            nameof(AudioEffectReverb.RoomSize), nameof(AudioEffectReverb.Damping), nameof(AudioEffectReverb.Spread),
            nameof(AudioEffectReverb.Hipass), nameof(AudioEffectReverb.Dry), nameof(AudioEffectReverb.Wet) };
        Check(effect.GetPropertyList().Count(p => p.IsStored && fields.Contains(p.Name)) == 8, "Eight typed stored reverb settings.");
        Reject<ArgumentOutOfRangeException>(() => effect.PredelayMSEC = -1);
        Reject<ArgumentOutOfRangeException>(() => effect.PredelayFeedback = float.NaN);
        Reject<ArgumentOutOfRangeException>(() => effect.RoomSize = float.NaN);
        Reject<ArgumentOutOfRangeException>(() => effect.Damping = float.PositiveInfinity);
        Reject<ArgumentOutOfRangeException>(() => effect.Spread = -1);
        Reject<ArgumentOutOfRangeException>(() => effect.Spread = 2);
        Reject<ArgumentOutOfRangeException>(() => effect.Hipass = float.NaN);
        Reject<ArgumentOutOfRangeException>(() => effect.Dry = float.PositiveInfinity);
        Reject<ArgumentOutOfRangeException>(() => effect.Wet = float.NaN);
        effect.PredelayMSEC = 0; effect.PredelayFeedback = 2; Check(effect.PredelayFeedback == .98f, "Feedback request clamps to stable bound.");
        effect.PredelayFeedback = -1; Check(effect.PredelayFeedback == 0, "Negative feedback request clamps to zero.");
        effect.RoomSize = 1.5f; effect.Damping = -.2f; effect.Spread = 0; effect.Hipass = 2; effect.Dry = -.5f; effect.Wet = 1.5f;
        using var copy = (AudioEffectReverb)effect.Duplicate(true);
        Check(copy.PredelayMSEC == 0 && copy.PredelayFeedback == 0 && copy.RoomSize == 1.5f && copy.Damping == -.2f &&
              copy.Spread == 0 && copy.Hipass == 2 && copy.Dry == -.5f && copy.Wet == 1.5f, "Concrete copy retains all controls.");
        effect.ResourceLocalToScene = true; using var holder = new Holder { Effect = effect }; using var packed = new PackedScene(); packed.Pack(holder);
        using var restored = (Holder)packed.Instantiate();
        Check(restored.Effect is AudioEffectReverb sceneCopy && !ReferenceEquals(sceneCopy, effect) && sceneCopy.Wet == 1.5f && sceneCopy.Hipass == 2,
            "Scene-local copy retains concrete reverb controls."); restored.Effect!.Dispose();
        Action<Resource> fail = _ => throw new ApplicationException("Reverb observer fixture."); effect.Changed += fail;
        Reject<ApplicationException>(() => effect.Wet = 1); Check(effect.Wet == 1, "Observer failure follows committed edit."); effect.Changed -= fail;
        using var instance = effect.Instantiate(); Check(instance.ProcessSilence(), "Reverb continues processing silent input for tails.");
        effect.Dispose(); Reject<ObjectDisposedException>(() => instance.Process([Vector2.Zero], new Vector2[1]));
    }

    private sealed record OracleCase(int Profile, float[] PCM);
    private static void ReferencePCM()
    {
        using var resource = typeof(AudioReverbTests).Assembly.GetManifestResourceStream("TestAudio.ReverbReference.json")!;
        var cases = JsonSerializer.Deserialize<OracleCase[]>(resource)!; Check(cases.Length == 3, "Pinned reverb profile census.");
        var input = new Vector2[4096];
        for (var i = 0; i < input.Length; i++) input[i] = i == 0 ? new(.75f, -.5f) : new(((i * 37) % 101 - 50) / 512f, ((i * 37 + 11) % 101 - 50) / 512f);
        var output = new Vector2[input.Length]; var maxError = 0f;
        foreach (var item in cases)
        {
            using var effect = new AudioEffectReverb
            {
                RoomSize = item.Profile == 1 ? .9f : item.Profile == 2 ? 0 : .8f,
                Damping = item.Profile == 1 ? .2f : item.Profile == 2 ? 1 : .5f,
                Wet = item.Profile == 0 ? .5f : 1,
                Dry = item.Profile == 0 ? 1 : 0,
                PredelayMSEC = item.Profile == 1 ? 20 : item.Profile == 2 ? 500 : 150,
                PredelayFeedback = item.Profile == 1 ? .8f : item.Profile == 2 ? 0 : .4f,
                Hipass = item.Profile == 1 ? .5f : item.Profile == 2 ? 1 : 0,
                Spread = item.Profile == 1 ? 0 : 1
            };
            using var instance = effect.Instantiate();
            for (var offset = 0; offset < input.Length;)
            {
                var count = Math.Min(input.Length - offset, offset == 0 ? 113 : offset < 1024 ? 257 : 1024);
                instance.Process(input.AsSpan(offset, count), output.AsSpan(offset, count)); offset += count;
            }
            Check(item.PCM.Length == input.Length * 2, "Pinned reverb sample count.");
            for (var i = 0; i < output.Length; i++)
            {
                maxError = Math.Max(maxError, Math.Abs(output[i].X - item.PCM[i * 2]));
                maxError = Math.Max(maxError, Math.Abs(output[i].Y - item.PCM[i * 2 + 1]));
            }
        }
        Check(maxError < .0001f, $"Pinned C++ reverb PCM maximum absolute error {maxError}.");
        Console.WriteLine($"Reverb C++ oracle: 3 profiles/24576 channel samples, maximum absolute PCM error {maxError}.");
    }

    private static void CPUAndWarm()
    {
        using var effect = new AudioEffectReverb { Wet = 0, Dry = 1 };
        using var instance = effect.Instantiate(); using var alias = effect.Instantiate();
        var input = new Vector2[4096];
        for (var i = 0; i < input.Length; i++) input[i] = new((i % 31 - 15) / 128f, (i % 23 - 11) / 128f);
        var output = new Vector2[input.Length]; instance.Process(input.AsSpan(0, 113), output.AsSpan(0, 113)); instance.Process(input.AsSpan(113), output.AsSpan(113));
        Check(output.SequenceEqual(input), "Dry-only reverb preserves exact PCM.");
        var inPlace = (Vector2[])input.Clone(); alias.Process(inPlace, inPlace); Check(inPlace.SequenceEqual(output), "Aliased processing matches split blocks.");
        effect.Dry = 0; effect.Wet = 1; effect.PredelayFeedback = 0; effect.Spread = 0;
        using var centered = effect.Instantiate(); var impulse = new Vector2[5000]; impulse[0] = new(1, 1); var tail = new Vector2[impulse.Length];
        centered.Process(impulse, tail);
        var first = Array.FindIndex(tail, f => Math.Abs(f.X) > .000001f || Math.Abs(f.Y) > .000001f);
        var expectedFirst = (int)MathF.Round(.025306122448979593f * AudioServer.Instance.GetMixRate(), MidpointRounding.ToEven);
        Check(first == expectedFirst && tail.All(f => f.X == f.Y) && tail.Skip(first).Any(f => Math.Abs(f.X) > .001f),
            "First comb reflection occurs at its tuned frame and zero spread keeps equal channels.");
        effect.Spread = 1; using var wide = effect.Instantiate(); wide.Process(impulse, tail);
        Check(tail.Skip(first).Any(f => Math.Abs(f.X - f.Y) > .0001f), "Full spread creates distinct stereo tail timing.");
        var silence = new Vector2[128]; var quiet = new Vector2[128];
        for (var i = 0; i < 20; i++) wide.Process(silence, quiet);
        var bytes = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < 64; i++) { effect.RoomSize = i % 2 == 0 ? .2f : .9f; wide.Process(input.AsSpan(0, 128), quiet); }
        Check(GC.GetAllocatedBytesForCurrentThread() == bytes, "64 warmed reverb edits and active blocks allocate zero managed bytes.");
        bytes = GC.GetAllocatedBytesForCurrentThread(); for (var i = 0; i < 64; i++) wide.Process(silence, quiet);
        Check(GC.GetAllocatedBytesForCurrentThread() == bytes, "64 warmed silent-tail reverb blocks allocate zero managed bytes.");
        effect.Wet = 0; effect.Dry = float.MaxValue; Reject<ArithmeticException>(() => wide.Process([new(2, 0)], new Vector2[1]));
        effect.Dry = 1; var recovered = new Vector2[1]; wide.Process([new(.1f, -.2f)], recovered);
        Check(recovered[0].DistanceTo(new(.1f, -.2f)) < .000001f, "Overflow resets all reverb histories for standalone recovery.");
        using var hidden = new AudioEffectReverb { Dry = 0, Wet = 0, PredelayMSEC = 0, PredelayFeedback = .98f };
        using var hiddenInstance = hidden.Instantiate(); var extremeInput = Enumerable.Repeat(new Vector2(float.MaxValue, 0), 11).ToArray();
        Reject<ArithmeticException>(() => hiddenInstance.Process(extremeInput, new Vector2[extremeInput.Length]));
        hidden.PredelayFeedback = 0; hidden.Dry = 1; hiddenInstance.Process([new(.1f, -.2f)], recovered);
        Check(recovered[0].DistanceTo(new(.1f, -.2f)) < .000001f, "Hidden wet-path overflow resets histories even with zero output gain.");
    }

    private static void Native()
    {
        var server = AudioServer.Instance; server.CloseNative(); server.BusCount = 1;
        using var effect = new AudioEffectReverb { Wet = 0, Dry = 1 }; using var stream = AudioEffectTests.Constant();
        var root = new Node(); var player = new AudioStreamPlayer { Stream = stream }; root.AddChild(player); using var tree = new SceneTree(root);
        try
        {
            server.AddBusEffect(0, effect); player.Play(); var native = server.Native; AudioEffectTests.Wait(native, 20);
            AudioEffectTests.CheckOutput(native, new(.2f, -.3f));
            effect.Wet = 1; effect.Dry = 0; AudioEffectTests.Wait(native, 80);
            native.PrepareCapture(native.QuantumFrames * native.Channels * 10); AudioEffectTests.Wait(native, 12);
            var pcm = native.CapturedPCM(); Check(pcm.Length > 0, "Native reverb output captured.");
            Check(Enumerable.Range(0, pcm.Length / native.Channels).Any(i => Math.Abs(pcm[i * native.Channels]) > .01f && Math.Abs(pcm[i * native.Channels] - .2f) > .01f),
                "Live reverb emits actual processed FAudio PCM.");
            if (native.Channels > 2)
                for (var frame = 0; frame < pcm.Length; frame += native.Channels)
                    for (var channel = 2; channel < native.Channels; channel++)
                        Check(Math.Abs(pcm[frame + channel]) < .0001f, "Front-pair reverb does not leak into unrelated speaker pairs.");
            server.SetBusEffectEnabled(0, 0, false); AudioEffectTests.CheckOutput(native, new(.2f, -.3f));
            server.SetBusEffectEnabled(0, 0, true); AudioEffectTests.Wait(native, 20);
            var bytes = native.MixManagedBytes; var allocations = FAudioContext.AllocationCalls; AudioEffectTests.Wait(native, 64);
            Check(native.MixManagedBytes == bytes && FAudioContext.AllocationCalls == allocations, "64 active native reverb passes allocate zero measured bytes/calls.");
            player.StreamPaused = true; AudioEffectTests.Wait(native, 20); bytes = native.MixManagedBytes; allocations = FAudioContext.AllocationCalls;
            AudioEffectTests.Wait(native, 64); Check(native.MixManagedBytes == bytes && FAudioContext.AllocationCalls == allocations,
                "64 paused native reverb-tail passes allocate zero measured bytes/calls.");
        }
        finally { tree.Dispose(); server.CloseNative(); while (server.GetBusEffectCount(0) > 0) server.RemoveBusEffect(0, 0); server.CloseNative(); }
    }

    private static void NativeTail()
    {
        var server = AudioServer.Instance; server.CloseNative(); server.BusCount = 1;
        using var effect = new AudioEffectReverb { Dry = 0, Wet = 1, PredelayFeedback = 0 };
        var samples = new short[512]; samples[0] = 16384; samples[1] = -16384;
        using var stream = new AudioStreamWAV
        {
            SampleFormat = AudioStreamWAV.Format.PCM16,
            Stereo = true,
            MixRate = (int)server.GetMixRate(),
            Data = MemoryMarshal.AsBytes(samples.AsSpan()).ToArray()
        };
        var root = new Node(); var player = new AudioStreamPlayer { Stream = stream }; root.AddChild(player); using var tree = new SceneTree(root);
        try
        {
            server.AddBusEffect(0, effect); var native = server.Native;
            native.PrepareCapture(native.QuantumFrames * native.Channels * 130); player.Play(); AudioEffectTests.Wait(native, 130);
            var pcm = native.CapturedPCM(); var first = -1;
            for (var i = 0; i < pcm.Length; i += native.Channels)
                if (Math.Abs(pcm[i]) > .005f || Math.Abs(pcm[i + 1]) > .005f) { first = i / native.Channels; break; }
            Check(first >= .02f * server.GetMixRate() && first <= .1f * server.GetMixRate(),
                $"Finite source produces native wet reverb only after the first comb reflection; first frame {first}.");
            tree.ProcessFrame(.01); Check(!player.IsPlaying(), "Finite source ended before the captured reverb tail.");
        }
        finally { tree.Dispose(); server.CloseNative(); while (server.GetBusEffectCount(0) > 0) server.RemoveBusEffect(0, 0); server.CloseNative(); }
    }

    internal static void RunHost()
    {
        var settings = ProjectSettings.Instance; var previous = settings.Get(ProjectSettings.RenderingMethod);
        settings.Set(ProjectSettings.RenderingMethod, Environment.GetEnvironmentVariable("ELECTRON2D_AUDIO_RENDERER") == "compatibility" ? "compatibility" : "gpu");
        try
        {
            using var effect = new AudioEffectReverb { Dry = 0, Wet = 1 };
            using var stream = AudioEffectTests.Constant(); using var capture = new AudioEffectCapture { BufferLength = .1f };
            var window = new Window { Size = new(160, 96) }; window.AddChild(new AudioStreamPlayer { Stream = stream, Autoplay = true });
            var scenario = new HostScenario(capture); window.AddChild(scenario);
            var server = AudioServer.Instance; server.AddBusEffect(0, effect); server.AddBusEffect(0, capture);
            try { Check(Engine.Instance.Run(window) == 0 && scenario.Completed && window.IsDisposed, "Public reverb host processed and cleaned up."); }
            finally { if (!window.IsDisposed) window.Dispose(); while (server.GetBusEffectCount(0) > 0) server.RemoveBusEffect(0, 0); server.CloseNative(); }
        }
        finally { settings.Set(ProjectSettings.RenderingMethod, previous); }
    }
    private sealed class HostScenario(AudioEffectCapture capture) : Node
    {
        internal bool Completed;
        private double _elapsed;
        protected override void OnReady() { ProcessEnabled = true; }
        protected override void OnProcess(double delta)
        {
            _elapsed += delta; var available = capture.GetFramesAvailable();
            if (available > 0) Completed |= capture.GetBuffer(available).Any(f => float.IsFinite(f.X) && Math.Abs(f.X) > .01f && Math.Abs(f.X - .2f) > .01f);
            if (_elapsed < .4) return;
            Check(Completed, "Public Window host captured nontrivial reverb PCM."); Tree!.Quit();
        }
    }
    private sealed class Holder : Node
    {
        public AudioEffectReverb? Effect { get; set; }
        private static readonly PropertyDescriptor[] Properties = [new PropertyDescriptor<Holder, AudioEffectReverb?>(nameof(Effect), p => p.Effect, (p, v) => p.Effect = v, _ => null, stored: true)];
        protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors() => base.GetPropertyDescriptors().Concat(Properties);
        protected override Func<Node> CreateSceneInstanceFactory() => Create;
        private static Node Create() => new Holder();
    }
    private static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    private static void Reject<T>(Action action) where T : Exception { try { action(); } catch (T) { return; } throw new InvalidOperationException($"Expected {typeof(T).Name}."); }
}
