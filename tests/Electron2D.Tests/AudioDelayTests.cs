using Electron2D;
using System.Runtime.InteropServices;

internal static class AudioDelayTests
{
    internal static void Run(bool native = false)
    {
        ResourceState(); ImpulsesAndWarm();
        if (native) { Native(); NativeTail(); }
        Console.WriteLine(native ? "Audio delay: typed state, stereo taps, feedback, native PCM and warmed allocation passed."
            : "Audio delay: typed state, stereo taps, feedback and warmed CPU allocation passed.");
    }

    private static void ResourceState()
    {
        using var effect = new AudioEffectDelay();
        Check(effect.Dry == 1 && effect.Tap1Active && effect.Tap1DelayMS == 250 && effect.Tap1LevelDB == -6 && effect.Tap1Pan == .2f &&
              effect.Tap2Active && effect.Tap2DelayMS == 500 && effect.Tap2LevelDB == -12 && effect.Tap2Pan == -.4f &&
              !effect.FeedbackActive && effect.FeedbackDelayMS == 340 && effect.FeedbackLevelDB == -6 && effect.FeedbackLowpass == 16000, "Delay defaults.");
        Check(effect.GetPropertyList().Count(p => p.IsStored && new[] { nameof(AudioEffectDelay.Dry), nameof(AudioEffectDelay.Tap1Active), nameof(AudioEffectDelay.Tap1DelayMS), nameof(AudioEffectDelay.Tap1LevelDB), nameof(AudioEffectDelay.Tap1Pan), nameof(AudioEffectDelay.Tap2Active), nameof(AudioEffectDelay.Tap2DelayMS), nameof(AudioEffectDelay.Tap2LevelDB), nameof(AudioEffectDelay.Tap2Pan), nameof(AudioEffectDelay.FeedbackActive), nameof(AudioEffectDelay.FeedbackDelayMS), nameof(AudioEffectDelay.FeedbackLevelDB), nameof(AudioEffectDelay.FeedbackLowpass) }.Contains(p.Name)) == 13, "All 13 typed fields are stored.");
        Reject<ArgumentOutOfRangeException>(() => effect.Tap1DelayMS = -1);
        Reject<ArgumentOutOfRangeException>(() => effect.Tap2DelayMS = 1501);
        Reject<ArgumentOutOfRangeException>(() => effect.FeedbackDelayMS = float.NaN);
        Reject<ArgumentOutOfRangeException>(() => effect.FeedbackLowpass = 0);
        Reject<ArgumentOutOfRangeException>(() => effect.Dry = float.PositiveInfinity);
        Reject<ArgumentOutOfRangeException>(() => effect.Tap1LevelDB = float.NaN);
        Reject<ArgumentOutOfRangeException>(() => effect.FeedbackLevelDB = float.PositiveInfinity);
        effect.Dry = .25f; effect.Tap1Active = false; effect.Tap1DelayMS = 0; effect.Tap1LevelDB = float.NegativeInfinity; effect.Tap1Pan = 2;
        effect.Tap2Active = false; effect.Tap2DelayMS = 1500; effect.Tap2LevelDB = 0; effect.Tap2Pan = -2;
        effect.FeedbackActive = true; effect.FeedbackDelayMS = 0; effect.FeedbackLevelDB = -3; effect.FeedbackLowpass = 1;
        using var copy = (AudioEffectDelay)effect.Duplicate(true);
        Check(copy.Dry == .25f && !copy.Tap1Active && copy.Tap1DelayMS == 0 && copy.Tap1LevelDB == float.NegativeInfinity && copy.Tap1Pan == 2 &&
              !copy.Tap2Active && copy.Tap2DelayMS == 1500 && copy.Tap2LevelDB == 0 && copy.Tap2Pan == -2 && copy.FeedbackActive &&
              copy.FeedbackDelayMS == 0 && copy.FeedbackLevelDB == -3 && copy.FeedbackLowpass == 1, "Concrete duplicate copies every field.");
        effect.ResourceLocalToScene = true; using var holder = new Holder { Effect = effect }; using var packed = new PackedScene(); packed.Pack(holder);
        using var restored = (Holder)packed.Instantiate();
        Check(restored.Effect is AudioEffectDelay sceneCopy && !ReferenceEquals(sceneCopy, effect) && sceneCopy.FeedbackActive && sceneCopy.Tap2DelayMS == 1500, "Scene-local delay resource copy.");
        restored.Effect!.Dispose();
        Action<Resource> fail = _ => throw new ApplicationException("Delay observer fixture."); effect.Changed += fail;
        Reject<ApplicationException>(() => effect.Dry = .5f); Check(effect.Dry == .5f, "Observer failure follows committed state."); effect.Changed -= fail;
        using var instance = effect.Instantiate(); Check(instance.ProcessSilence(), "Delay requests silent input for audible tails.");
        effect.Dispose(); Reject<ObjectDisposedException>(() => instance.Process([Vector2.Zero], new Vector2[1]));
    }

    private static void ImpulsesAndWarm()
    {
        var rate = AudioServer.Instance.GetMixRate();
        using var effect = new AudioEffectDelay
        {
            Dry = 0,
            Tap1DelayMS = 2.1f * 1000 / rate,
            Tap1LevelDB = 0,
            Tap1Pan = 0,
            Tap2DelayMS = 4.1f * 1000 / rate,
            Tap2LevelDB = 0,
            Tap2Pan = 0
        };
        using var split = effect.Instantiate(); using var alias = effect.Instantiate();
        var input = new Vector2[16]; input[0] = new(1, -.5f); var output = new Vector2[16];
        split.Process(input.AsSpan(0, 3), output.AsSpan(0, 3)); split.Process(input.AsSpan(3), output.AsSpan(3));
        Check(output[2].DistanceTo(input[0]) < .000001f && output[4].DistanceTo(input[0]) < .000001f &&
              output.Where((_, i) => i != 2 && i != 4).All(v => v == Vector2.Zero), "Two taps have exact independent delay positions.");
        var inPlace = (Vector2[])input.Clone(); alias.Process(inPlace, inPlace); Check(inPlace.SequenceEqual(output), "In-place and split processing preserve the same history.");
        using var immediate = new AudioEffectDelay { Dry = 0, Tap1DelayMS = 0, Tap1LevelDB = 0, Tap1Pan = 0, Tap2Active = false };
        using var immediateInstance = immediate.Instantiate(); var immediateOutput = new Vector2[1];
        immediateInstance.Process([input[0]], immediateOutput); Check(immediateOutput[0] == input[0], "Zero-delay tap reads the current input frame.");
        using var live = effect.Instantiate(); effect.Tap1Active = true; effect.Tap2Active = true;
        live.Process(input.AsSpan(0, 3), output.AsSpan(0, 3)); effect.Tap1Active = false;
        live.Process(input.AsSpan(3), output.AsSpan(3)); Check(output[4].DistanceTo(input[0]) < .000001f, "Live tap edit retains pending second tap.");
        effect.Tap1Active = false; effect.Tap2Active = false; effect.Dry = 1; effect.FeedbackActive = true;
        effect.FeedbackDelayMS = 3.1f * 1000 / rate; effect.FeedbackLevelDB = -6; effect.FeedbackLowpass = 16000;
        using var feedback = effect.Instantiate(); feedback.Process(input, output);
        var expected = Mathf.DBToLinear(-6) * (1 - (float)Math.Exp(-Math.Tau * effect.FeedbackLowpass / rate));
        Check(output[0] == input[0] && Math.Abs(output[3].X - expected) < .00001f && Math.Abs(output[3].Y + expected * .5f) < .00001f, "Filtered feedback repeats after the configured frame count.");
        using var released = effect.Instantiate(); var firstFrame = new Vector2[1]; released.Process([input[0]], firstFrame);
        effect.FeedbackActive = false; var retainedTail = new Vector2[4]; released.Process(retainedTail, retainedTail);
        Check(retainedTail[2].X > .1f && retainedTail[2].Y < -.05f, "Disabling feedback preserves already queued output."); effect.FeedbackActive = true;
        effect.FeedbackDelayMS = 0; using var zero = effect.Instantiate(); zero.Process(input, output);
        Check(Math.Abs(output[1].X - expected) < .00001f, "Zero feedback delay uses previous feedback sample.");
        var silence = new Vector2[128]; var result = new Vector2[128];
        for (var i = 0; i < 20; i++) feedback.Process(silence, result);
        var bytes = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < 64; i++) { effect.Tap1Pan = i % 2 == 0 ? -1 : 1; feedback.Process(input, output); }
        Check(GC.GetAllocatedBytesForCurrentThread() == bytes, "64 warmed live-edited delay blocks allocate zero managed bytes.");
        bytes = GC.GetAllocatedBytesForCurrentThread(); for (var i = 0; i < 64; i++) feedback.Process(silence, result);
        Check(GC.GetAllocatedBytesForCurrentThread() == bytes, "64 warmed silent-tail delay blocks allocate zero managed bytes.");
        using var extreme = new AudioEffectDelay { Tap1Active = false, Tap2Active = false, FeedbackActive = true, FeedbackLevelDB = 760 };
        using var unsafeInstance = extreme.Instantiate(); Reject<ArithmeticException>(() => unsafeInstance.Process([new(10000, 0)], new Vector2[1]));
        extreme.FeedbackLevelDB = float.NegativeInfinity; var recovered = new Vector2[1]; unsafeInstance.Process([new(.1f, -.2f)], recovered);
        Check(recovered[0].IsFinite(), "Overflow resets delay history for standalone recovery.");
        using var boundary = new AudioEffectDelay { Dry = 0, Tap1DelayMS = 1500, Tap1LevelDB = 0, Tap1Pan = 0, Tap2Active = false };
        using var boundaryInstance = boundary.Instantiate(); var delayedFrames = (int)(boundary.Tap1DelayMS / 1000f * rate);
        var longInput = new Vector2[delayedFrames + 2]; var longOutput = new Vector2[longInput.Length]; longInput[0] = new(.5f, -.25f);
        boundaryInstance.Process(longInput, longOutput);
        Check(longOutput[delayedFrames].DistanceTo(longInput[0]) < .000001f && longOutput[delayedFrames - 1] == Vector2.Zero,
            "Maximum documented tap delay retains the full history without wrap aliasing.");
    }

    private static void Native()
    {
        var server = AudioServer.Instance; server.CloseNative(); server.BusCount = 1;
        using var effect = new AudioEffectDelay { Dry = 0, Tap1DelayMS = 5, Tap1LevelDB = 0, Tap1Pan = 1, Tap2Active = false };
        using var stream = AudioEffectTests.Constant(); var root = new Node(); var player = new AudioStreamPlayer { Stream = stream }; root.AddChild(player); using var tree = new SceneTree(root);
        try
        {
            server.AddBusEffect(0, effect); player.Play(); var native = server.Native; AudioEffectTests.Wait(native, 20);
            AudioEffectTests.CheckOutput(native, new(0, -.3f));
            if (native.Channels > 2)
            {
                var pcm = native.CapturedPCM();
                for (var frame = 0; frame < pcm.Length; frame += native.Channels)
                    for (var channel = 2; channel < native.Channels; channel++)
                        Check(Math.Abs(pcm[frame + channel]) < .0001f, "Delay does not leak the front-pair signal into unrelated output pairs.");
            }
            effect.Tap1Pan = -1; AudioEffectTests.Wait(native, 20); AudioEffectTests.CheckOutput(native, new(.2f, 0));
            server.SetBusEffectEnabled(0, 0, false); AudioEffectTests.CheckOutput(native, new(.2f, -.3f));
            server.SetBusEffectEnabled(0, 0, true); AudioEffectTests.Wait(native, 20);
            var bytes = native.MixManagedBytes; var allocations = FAudioContext.AllocationCalls; AudioEffectTests.Wait(native, 64);
            Check(native.MixManagedBytes == bytes && FAudioContext.AllocationCalls == allocations, "64 native active delay passes allocate zero measured bytes/calls.");
            player.StreamPaused = true; AudioEffectTests.Wait(native, 20); bytes = native.MixManagedBytes; allocations = FAudioContext.AllocationCalls;
            AudioEffectTests.Wait(native, 64); Check(native.MixManagedBytes == bytes && FAudioContext.AllocationCalls == allocations, "64 native paused delay passes allocate zero measured bytes/calls.");
        }
        finally { tree.Dispose(); server.CloseNative(); while (server.GetBusEffectCount(0) > 0) server.RemoveBusEffect(0, 0); server.CloseNative(); }
    }

    private static void NativeTail()
    {
        var server = AudioServer.Instance; server.CloseNative(); server.BusCount = 1;
        using var effect = new AudioEffectDelay { Dry = 0, Tap1DelayMS = 100, Tap1LevelDB = 0, Tap1Pan = 0, Tap2Active = false };
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
                if (Math.Abs(pcm[i]) > .2f || Math.Abs(pcm[i + 1]) > .2f) { first = i / native.Channels; break; }
            Check(first >= .08f * server.GetMixRate() && first <= .14f * server.GetMixRate() &&
                Math.Abs(pcm[first * native.Channels] + pcm[first * native.Channels + 1]) < .0001f,
                $"Finite source produces an actual delayed native tail after voice completion; first frame {first}.");
            tree.ProcessFrame(.01); Check(!player.IsPlaying(), "Finite source stopped before its delayed bus tail.");
        }
        finally { tree.Dispose(); server.CloseNative(); while (server.GetBusEffectCount(0) > 0) server.RemoveBusEffect(0, 0); server.CloseNative(); }
    }

    internal static void RunHost()
    {
        var settings = ProjectSettings.Instance; var previous = settings.Get(ProjectSettings.RenderingMethod);
        settings.Set(ProjectSettings.RenderingMethod, Environment.GetEnvironmentVariable("ELECTRON2D_AUDIO_RENDERER") == "compatibility" ? "compatibility" : "gpu");
        try
        {
            using var effect = new AudioEffectDelay { Dry = 0, Tap1DelayMS = 5, Tap1LevelDB = 0, Tap1Pan = 1, Tap2Active = false };
            using var stream = AudioEffectTests.Constant(); using var capture = new AudioEffectCapture { BufferLength = .1f };
            var window = new Window { Size = new(160, 96) }; var player = new AudioStreamPlayer { Stream = stream, Autoplay = true };
            var scenario = new HostScenario(capture); window.AddChild(player); window.AddChild(scenario);
            var server = AudioServer.Instance; server.AddBusEffect(0, effect); server.AddBusEffect(0, capture);
            try { Check(Engine.Instance.Run(window) == 0 && scenario.Completed && window.IsDisposed, "Public delay host processes and cleans up."); }
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
            _elapsed += delta;
            var available = capture.GetFramesAvailable();
            if (available > 0) Completed |= capture.GetBuffer(available).Any(f => f.DistanceTo(new(0, -.3f)) < .0001f);
            if (_elapsed < .25) return;
            Check(Completed, "Public host captured stereo delayed output."); Tree!.Quit();
        }
    }
    private sealed class Holder : Node
    {
        public AudioEffectDelay? Effect { get; set; }
        private static readonly PropertyDescriptor[] Properties = [new PropertyDescriptor<Holder, AudioEffectDelay?>(nameof(Effect), p => p.Effect, (p, v) => p.Effect = v, _ => null, stored: true)];
        protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors() => base.GetPropertyDescriptors().Concat(Properties);
        protected override Func<Node> CreateSceneInstanceFactory() => Create;
        private static Node Create() => new Holder();
    }
    private static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    private static void Reject<T>(Action action) where T : Exception { try { action(); } catch (T) { return; } throw new InvalidOperationException($"Expected {typeof(T).Name}."); }
}
