using Electron2D;
using System.Runtime.InteropServices;

internal static class AudioChorusTests
{
    internal static void Run(bool native = false)
    {
        ResourceState(); PCMAndWarm();
        if (native) { Native(); NativeTail(); }
        Console.WriteLine(native ? "Audio chorus: typed voices, stereo PCM, native tails and warmed allocation passed."
            : "Audio chorus: typed voices, stereo PCM and warmed CPU allocation passed.");
    }

    private static void ResourceState()
    {
        using var effect = new AudioEffectChorus();
        Check(effect.VoiceCount == 2 && effect.Dry == 1 && effect.Wet == .5f, "Chorus scalar defaults.");
        Check(effect.GetVoiceDelayMS(0) == 15 && effect.GetVoiceRateHZ(0) == .8f && effect.GetVoiceDepthMS(0) == 2 &&
              effect.GetVoiceLevelDB(0) == 0 && effect.GetVoiceCutoffHZ(0) == 8000 && effect.GetVoicePan(0) == -.5f &&
              effect.GetVoiceDelayMS(1) == 20 && effect.GetVoiceRateHZ(1) == 1.2f && effect.GetVoiceDepthMS(1) == 3 &&
              effect.GetVoiceCutoffHZ(1) == 8000 && effect.GetVoicePan(1) == .5f && effect.GetVoiceDelayMS(2) == 12 &&
              effect.GetVoiceRateHZ(2) == 1 && effect.GetVoiceDepthMS(2) == 0 && effect.GetVoiceCutoffHZ(2) == 16000,
              "All four voices expose source defaults, including inactive voices.");
        Check(effect.GetPropertyList().Count(p => p.IsStored && (p.Name.StartsWith("Voice/") || p.Name is "VoiceCount" or "Dry" or "Wet")) == 27,
            "Three scalar and 24 indexed voice fields are stored.");
        Reject<ArgumentOutOfRangeException>(() => effect.VoiceCount = 0);
        Reject<ArgumentOutOfRangeException>(() => effect.VoiceCount = 5);
        Reject<ArgumentOutOfRangeException>(() => effect.GetVoicePan(-1));
        Reject<ArgumentOutOfRangeException>(() => effect.SetVoicePan(4, 0));
        Reject<ArgumentOutOfRangeException>(() => effect.SetVoiceDelayMS(0, 51));
        Reject<ArgumentOutOfRangeException>(() => effect.SetVoiceDepthMS(0, -1));
        Reject<ArgumentOutOfRangeException>(() => effect.SetVoiceRateHZ(0, 0));
        Reject<ArgumentOutOfRangeException>(() => effect.SetVoiceCutoffHZ(0, float.NaN));
        Reject<ArgumentOutOfRangeException>(() => effect.SetVoiceLevelDB(0, float.PositiveInfinity));
        Reject<ArgumentOutOfRangeException>(() => effect.SetVoicePan(0, 2));
        Reject<ArgumentOutOfRangeException>(() => effect.Dry = float.NaN);
        Reject<ArgumentOutOfRangeException>(() => effect.Wet = float.PositiveInfinity);
        effect.VoiceCount = 4; effect.Dry = .2f; effect.Wet = .7f;
        effect.SetVoiceDelayMS(3, 50); effect.SetVoiceRateHZ(3, 20); effect.SetVoiceDepthMS(3, 20);
        effect.SetVoiceLevelDB(3, float.NegativeInfinity); effect.SetVoiceCutoffHZ(3, 0); effect.SetVoicePan(3, 1);
        using var copy = (AudioEffectChorus)effect.Duplicate(true);
        Check(copy.VoiceCount == 4 && copy.Dry == .2f && copy.Wet == .7f && copy.GetVoiceDelayMS(3) == 50 &&
              copy.GetVoiceRateHZ(3) == 20 && copy.GetVoiceDepthMS(3) == 20 && copy.GetVoiceLevelDB(3) == float.NegativeInfinity &&
              copy.GetVoiceCutoffHZ(3) == 0 && copy.GetVoicePan(3) == 1, "Concrete copy retains all voice controls.");
        effect.ResourceLocalToScene = true; using var holder = new Holder { Effect = effect }; using var packed = new PackedScene(); packed.Pack(holder);
        using var restored = (Holder)packed.Instantiate();
        Check(restored.Effect is AudioEffectChorus sceneCopy && !ReferenceEquals(sceneCopy, effect) && sceneCopy.GetVoiceDelayMS(3) == 50 && sceneCopy.VoiceCount == 4,
            "Scene-local copy retains indexed voice controls.");
        restored.Effect!.Dispose();
        Action<Resource> fail = _ => throw new ApplicationException("Chorus observer fixture."); effect.Changed += fail;
        Reject<ApplicationException>(() => effect.SetVoicePan(3, 0)); Check(effect.GetVoicePan(3) == 0, "Observer failure follows committed edit."); effect.Changed -= fail;
        using var instance = effect.Instantiate(); Check(instance.ProcessSilence(), "Chorus processes silent input for delayed tails.");
        effect.Dispose(); Reject<ObjectDisposedException>(() => instance.Process([Vector2.Zero], new Vector2[1]));
    }

    private static void PCMAndWarm()
    {
        var rate = AudioServer.GetMixRate();
        using var effect = new AudioEffectChorus { VoiceCount = 1, Dry = 0, Wet = 1 };
        effect.SetVoiceDelayMS(0, 15.25f * 1000 / rate); effect.SetVoiceDepthMS(0, 0);
        effect.SetVoiceCutoffHZ(0, 16000); effect.SetVoicePan(0, 0);
        var input = new Vector2[64]; input[0] = new(1, -.5f); var output = new Vector2[64];
        using var split = effect.Instantiate(); using var alias = effect.Instantiate();
        split.Process(input.AsSpan(0, 11), output.AsSpan(0, 11)); split.Process(input.AsSpan(11), output.AsSpan(11));
        Check(output[15].DistanceTo(input[0]) < 1e-6f && output.Where((_, i) => i != 15).All(v => v == Vector2.Zero),
            "One voice delays both channels by the exact prepared frame count.");
        var inPlace = (Vector2[])input.Clone(); alias.Process(inPlace, inPlace);
        Check(inPlace.SequenceEqual(output), "Aliased and split blocks agree for a static voice.");
        effect.SetVoiceDelayMS(0, 15.6f * 1000 / rate); using var rounded = effect.Instantiate(); rounded.Process(input, output);
        Check(output[15] == Vector2.Zero && output[16].DistanceTo(input[0]) < 1e-6f, "Fractional delay rounds to the nearest whole frame.");
        effect.SetVoiceDelayMS(0, 0); using var minimum = effect.Instantiate(); minimum.Process(input, output);
        Check(output[9] == Vector2.Zero && output[10].DistanceTo(input[0]) < 1e-6f, "Minimum read distance prevents zero-delay lookahead.");
        effect.SetVoiceDelayMS(0, 15.25f * 1000 / rate);
        effect.SetVoicePan(0, 1); using var right = effect.Instantiate(); right.Process(input, output);
        Check(output[15].X == 0 && Math.Abs(output[15].Y + .5f) < 1e-6f, "Pan isolates the right channel.");
        effect.SetVoiceCutoffHZ(0, 0); using var muted = effect.Instantiate(); muted.Process(input, output);
        Check(output.All(v => v == Vector2.Zero), "Zero cutoff omits a voice.");
        effect.SetVoiceCutoffHZ(0, 16000); effect.SetVoicePan(0, 0); effect.SetVoiceLevelDB(0, -6);
        using var gain = effect.Instantiate(); gain.Process(input, output);
        Check(Math.Abs(output[15].X - Mathf.DBToLinear(-6)) < 1e-6f, "Voice level is a decibel gain.");
        effect.SetVoiceLevelDB(0, 0); effect.SetVoiceCutoffHZ(0, 500); using var filtered = effect.Instantiate(); filtered.Process(input, output);
        Check(output[15].X > 0 && output[15].X < 1 && output[16].X > 0, "Voice low-pass retains history.");
        effect.SetVoiceCutoffHZ(0, 16000); effect.SetVoiceDepthMS(0, 1); effect.SetVoiceRateHZ(0, 20);
        var tones = new Vector2[4096]; for (var i = 0; i < tones.Length; i++) tones[i] = new((float)Math.Sin(i * .11), (float)Math.Cos(i * .13));
        var modulated = new Vector2[tones.Length]; using var lfo = effect.Instantiate(); lfo.Process(tones, modulated);
        effect.SetVoiceDepthMS(0, 0); var staticOutput = new Vector2[tones.Length]; using var fixedDelay = effect.Instantiate(); fixedDelay.Process(tones, staticOutput);
        Check(modulated.Zip(staticOutput).Any(pair => pair.First.DistanceTo(pair.Second) > .01f), "Depth and oscillator modulate audible delay.");
        effect.SetVoiceDepthMS(0, 1);
        using (var four = new AudioEffectChorus { VoiceCount = 4, Dry = 0, Wet = 1 })
        {
            for (var voice = 0; voice < 4; voice++)
            {
                four.SetVoiceDelayMS(voice, (11.25f + voice) * 1000 / rate);
                four.SetVoiceDepthMS(voice, 0); four.SetVoiceCutoffHZ(voice, 16000);
                four.SetVoicePan(voice, voice % 2 == 0 ? -1 : 1);
            }
            using var fourInstance = four.Instantiate();
            fourInstance.Process(input.AsSpan(0, 8), output.AsSpan(0, 8));
            fourInstance.Process(input.AsSpan(8), output.AsSpan(8));
            for (var voice = 0; voice < 4; voice++)
            {
                var frame = output[11 + voice];
                Check(voice % 2 == 0 ? Math.Abs(frame.X - 1) < 1e-6f && frame.Y == 0
                    : frame.X == 0 && Math.Abs(frame.Y + .5f) < 1e-6f, "All four voices retain independent stereo timing and pan.");
            }
            using var growing = four.Instantiate(); four.VoiceCount = 1;
            growing.Process(input.AsSpan(0, 8), output.AsSpan(0, 8)); four.VoiceCount = 4;
            growing.Process(input.AsSpan(8), output.AsSpan(8));
            Check(Math.Abs(output[14].Y + .5f) < 1e-6f, "A live voice-count edit retains stored voice settings and pending input.");
        }
        using (var unsafeEffect = new AudioEffectChorus { Dry = float.MaxValue, Wet = 0 })
        using (var unsafeInstance = unsafeEffect.Instantiate())
        {
            Reject<ArithmeticException>(() => unsafeInstance.Process([new(2, 0)], new Vector2[1]));
            unsafeEffect.Dry = 0; var recovered = new Vector2[1]; unsafeInstance.Process([new(.1f, -.2f)], recovered);
            Check(recovered[0].IsFinite(), "Overflow clears chorus history for standalone recovery.");
        }
        var silence = new Vector2[256]; var result = new Vector2[256];
        for (var i = 0; i < 16; i++) lfo.Process(silence, result);
        var bytes = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < 64; i++) lfo.Process(tones.AsSpan(0, 256), result);
        Check(GC.GetAllocatedBytesForCurrentThread() == bytes, "64 warmed active chorus blocks allocate no managed bytes.");
        bytes = GC.GetAllocatedBytesForCurrentThread(); for (var i = 0; i < 64; i++) lfo.Process(silence, result);
        Check(GC.GetAllocatedBytesForCurrentThread() == bytes, "64 warmed silent chorus blocks allocate no managed bytes.");
    }

    private static void Native()
    {
        var server = AudioServer.Service; server.CloseNative(); AudioServer.BusCount = 1;
        using var effect = new AudioEffectChorus { VoiceCount = 1, Dry = 0, Wet = 1 };
        effect.SetVoiceDelayMS(0, 5); effect.SetVoiceDepthMS(0, 0); effect.SetVoiceCutoffHZ(0, 16000); effect.SetVoicePan(0, 1);
        using var stream = AudioEffectTests.Constant(); var root = new Node(); var player = new AudioStreamPlayer { Stream = stream }; root.AddChild(player); using var tree = new SceneTree(root);
        try
        {
            AudioServer.AddBusEffect(0, effect); player.Play(); var native = server.Native; AudioEffectTests.Wait(native, 20);
            AudioEffectTests.CheckOutput(native, new(0, -.3f));
            if (native.Channels > 2)
            {
                var pcm = native.CapturedPCM();
                for (var frame = 0; frame < pcm.Length; frame += native.Channels)
                    for (var channel = 2; channel < native.Channels; channel++)
                        Check(Math.Abs(pcm[frame + channel]) < .0001f, "Chorus does not leak the front pair into unrelated output pairs.");
            }
            effect.SetVoicePan(0, -1); AudioEffectTests.Wait(native, 20); AudioEffectTests.CheckOutput(native, new(.2f, 0));
            AudioServer.SetBusEffectEnabled(0, 0, false); AudioEffectTests.CheckOutput(native, new(.2f, -.3f));
            AudioServer.SetBusEffectEnabled(0, 0, true); AudioEffectTests.Wait(native, 20);
            var bytes = native.MixManagedBytes; var calls = FAudioContext.AllocationCalls; AudioEffectTests.Wait(native, 64);
            Check(native.MixManagedBytes == bytes && FAudioContext.AllocationCalls == calls, "64 warmed native active blocks allocate no measured bytes or custom calls.");
            player.StreamPaused = true; AudioEffectTests.Wait(native, 20); bytes = native.MixManagedBytes; calls = FAudioContext.AllocationCalls;
            AudioEffectTests.Wait(native, 64); Check(native.MixManagedBytes == bytes && FAudioContext.AllocationCalls == calls,
                "64 warmed native paused blocks allocate no measured bytes or custom calls.");
        }
        finally { tree.Dispose(); server.CloseNative(); while (AudioServer.GetBusEffectCount(0) > 0) AudioServer.RemoveBusEffect(0, 0); server.CloseNative(); }
    }

    private static void NativeTail()
    {
        var server = AudioServer.Service; server.CloseNative(); AudioServer.BusCount = 1;
        using var effect = new AudioEffectChorus { VoiceCount = 1, Dry = 0, Wet = 1 };
        effect.SetVoiceDelayMS(0, 50); effect.SetVoiceDepthMS(0, 0); effect.SetVoiceCutoffHZ(0, 16000); effect.SetVoicePan(0, 0);
        var samples = new short[512]; samples[0] = 16384; samples[1] = -16384;
        using var stream = new AudioStreamWAV { SampleFormat = AudioStreamWAV.Format.PCM16, Stereo = true, MixRate = (int)AudioServer.GetMixRate(), Data = MemoryMarshal.AsBytes(samples.AsSpan()).ToArray() };
        var root = new Node(); var player = new AudioStreamPlayer { Stream = stream }; root.AddChild(player); using var tree = new SceneTree(root);
        try
        {
            AudioServer.AddBusEffect(0, effect); var native = server.Native; native.PrepareCapture(native.QuantumFrames * native.Channels * 70);
            player.Play(); AudioEffectTests.Wait(native, 70); var pcm = native.CapturedPCM(); var first = -1;
            for (var i = 0; i < pcm.Length; i += native.Channels)
                if (Math.Abs(pcm[i]) > .2f || Math.Abs(pcm[i + 1]) > .2f) { first = i / native.Channels; break; }
            Check(first >= .04f * AudioServer.GetMixRate() && first <= .07f * AudioServer.GetMixRate(), $"Finite source yields delayed native chorus tail; first={first}.");
            tree.ProcessFrame(.01); Check(!player.IsPlaying(), "Source finishes before chorus tail.");
        }
        finally { tree.Dispose(); server.CloseNative(); while (AudioServer.GetBusEffectCount(0) > 0) AudioServer.RemoveBusEffect(0, 0); server.CloseNative(); }
    }

    internal static void RunHost()
    {
        var settings = ProjectSettings.Service; var previous = ProjectSettings.Get(ProjectSettings.RenderingMethod);
        ProjectSettings.Set(ProjectSettings.RenderingMethod, Environment.GetEnvironmentVariable("ELECTRON2D_AUDIO_RENDERER") == "compatibility" ? "compatibility" : "gpu");
        try
        {
            using var effect = new AudioEffectChorus { VoiceCount = 1, Dry = 0, Wet = 1 };
            effect.SetVoiceDelayMS(0, 5); effect.SetVoiceDepthMS(0, 0); effect.SetVoiceCutoffHZ(0, 16000); effect.SetVoicePan(0, 1);
            using var stream = AudioEffectTests.Constant(); using var capture = new AudioEffectCapture { BufferLength = .1f };
            var window = new Window { Size = new(160, 96) }; window.AddChild(new AudioStreamPlayer { Stream = stream, Autoplay = true });
            var scenario = new HostScenario(capture); window.AddChild(scenario);
            var server = AudioServer.Service; AudioServer.AddBusEffect(0, effect); AudioServer.AddBusEffect(0, capture);
            try { Check(Engine.Run(window) == 0 && scenario.Completed && window.IsDisposed, "Public chorus host processed and cleaned up."); }
            finally { if (!window.IsDisposed) window.Dispose(); while (AudioServer.GetBusEffectCount(0) > 0) AudioServer.RemoveBusEffect(0, 0); server.CloseNative(); }
        }
        finally { ProjectSettings.Set(ProjectSettings.RenderingMethod, previous); }
    }
    private sealed class HostScenario(AudioEffectCapture capture) : Node
    {
        internal bool Completed;
        private double _elapsed;
        protected override void OnReady() { ProcessEnabled = true; }
        protected override void OnProcess(double delta)
        {
            _elapsed += delta; var available = capture.GetFramesAvailable();
            if (available > 0) Completed |= capture.GetBuffer(available).Any(f => f.DistanceTo(new(0, -.3f)) < .0001f);
            if (_elapsed < .25) return;
            Check(Completed, "Public Window host captured stereo chorus PCM."); Tree!.Quit();
        }
    }

    private sealed class Holder : Node
    {
        public AudioEffectChorus? Effect { get; set; }
        private static readonly PropertyDescriptor[] Properties = [new PropertyDescriptor<Holder, AudioEffectChorus?>(nameof(Effect), p => p.Effect, (p, v) => p.Effect = v, _ => null, stored: true)];
        protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors() => base.GetPropertyDescriptors().Concat(Properties);
        protected override Func<Node> CreateSceneInstanceFactory() => Create;
        private static Node Create() => new Holder();
    }
    private static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    private static void Reject<T>(Action action) where T : Exception { try { action(); } catch (T) { return; } throw new InvalidOperationException($"Expected {typeof(T).Name}."); }
}
