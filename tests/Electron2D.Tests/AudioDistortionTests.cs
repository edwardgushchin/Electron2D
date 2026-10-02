using Electron2D;
using System.Runtime.InteropServices;
using System.Text.Json;

internal static class AudioDistortionTests
{
    internal static void Run(bool native = false)
    {
        ResourceState(); ReferencePCM(); CPUAndWarm();
        if (native) { Native(); NativeTail(); }
        Console.WriteLine(native ? "Audio distortion: five pinned curves, typed state, native PCM and warmed allocation passed."
            : "Audio distortion: five pinned curves, typed state and warmed CPU allocation passed.");
    }

    private static void ResourceState()
    {
        using var effect = new AudioEffectDistortion();
        Check(effect.DistortionMode == AudioEffectDistortion.Mode.Clip && effect.PreGain == 0 && effect.PostGain == 0 &&
              effect.KeepHFHZ == 16000 && effect.Drive == 0, "Distortion defaults.");
        Check(Enum.GetValues<AudioEffectDistortion.Mode>().Select(value => (int)value).SequenceEqual([0, 1, 2, 3, 4]), "All five mode ordinals.");
        Check(effect.GetPropertyList().Count(p => p.IsStored && new[] { "DistortionMode", "PreGain", "PostGain", "KeepHFHZ", "Drive" }.Contains(p.Name)) == 5,
            "Five typed stored controls.");
        Reject<ArgumentOutOfRangeException>(() => effect.DistortionMode = (AudioEffectDistortion.Mode)5);
        Reject<ArgumentOutOfRangeException>(() => effect.Drive = -.01f);
        Reject<ArgumentOutOfRangeException>(() => effect.Drive = 1.01f);
        Reject<ArgumentOutOfRangeException>(() => effect.KeepHFHZ = 0);
        Reject<ArgumentOutOfRangeException>(() => effect.KeepHFHZ = float.PositiveInfinity);
        Reject<ArgumentOutOfRangeException>(() => effect.PreGain = float.NaN);
        Reject<ArgumentOutOfRangeException>(() => effect.PostGain = float.PositiveInfinity);
        effect.DistortionMode = AudioEffectDistortion.Mode.WaveShape; effect.Drive = 1;
        effect.PreGain = -6; effect.PostGain = float.NegativeInfinity; effect.KeepHFHZ = 20500;
        using var copy = (AudioEffectDistortion)effect.Duplicate(true);
        Check(copy.DistortionMode == AudioEffectDistortion.Mode.WaveShape && copy.Drive == 1 && copy.PreGain == -6 &&
              copy.PostGain == float.NegativeInfinity && copy.KeepHFHZ == 20500, "Concrete copy retains all settings.");
        effect.ResourceLocalToScene = true; using var holder = new Holder { Effect = effect }; using var packed = new PackedScene(); packed.Pack(holder);
        using var restored = (Holder)packed.Instantiate();
        Check(restored.Effect is AudioEffectDistortion sceneCopy && !ReferenceEquals(sceneCopy, effect) &&
              sceneCopy.DistortionMode == AudioEffectDistortion.Mode.WaveShape && sceneCopy.KeepHFHZ == 20500,
              "Scene-local copy retains concrete curve and frequency controls.");
        restored.Effect!.Dispose();
        Action<Resource> fail = _ => throw new ApplicationException("Distortion observer fixture."); effect.Changed += fail;
        Reject<ApplicationException>(() => effect.Drive = .5f); Check(effect.Drive == .5f, "Throwing observer follows committed edit."); effect.Changed -= fail;
        using var instance = effect.Instantiate(); Check(instance.ProcessSilence(), "Filter history continues on silent blocks.");
        effect.Dispose(); Reject<ObjectDisposedException>(() => instance.Process([Vector2.Zero], new Vector2[1]));
    }

    private sealed record OracleCase(int Rate, int Mode, float Drive, float[] Input, float[] PCM);
    private static void ReferencePCM()
    {
        var rate = AudioServer.Instance.GetMixRate();
        using var resource = typeof(AudioDistortionTests).Assembly.GetManifestResourceStream("TestAudio.DistortionReference.json")!;
        var cases = JsonSerializer.Deserialize<OracleCase[]>(resource)!.Where(item => item.Rate == rate).ToArray();
        Check(cases.Length == 5, "Five pinned C++ modes at the current output rate.");
        var maxError = 0f;
        foreach (var item in cases)
        {
            using var effect = new AudioEffectDistortion
            {
                DistortionMode = (AudioEffectDistortion.Mode)item.Mode,
                Drive = item.Drive,
                PreGain = 3,
                PostGain = -2,
                KeepHFHZ = 4000
            };
            using var instance = effect.Instantiate();
            var input = new Vector2[item.Input.Length / 2]; var output = new Vector2[input.Length];
            for (var i = 0; i < input.Length; i++) input[i] = new(item.Input[i * 2], item.Input[i * 2 + 1]);
            instance.Process(input.AsSpan(0, 37), output.AsSpan(0, 37)); instance.Process(input.AsSpan(37, 69), output.AsSpan(37, 69));
            instance.Process(input.AsSpan(106), output.AsSpan(106));
            Check(item.PCM.Length == input.Length * 2, "Pinned distortion sample count.");
            for (var i = 0; i < output.Length; i++)
            {
                maxError = Math.Max(maxError, Math.Abs(output[i].X - item.PCM[i * 2]));
                maxError = Math.Max(maxError, Math.Abs(output[i].Y - item.PCM[i * 2 + 1]));
            }
        }
        Check(maxError < .0001f, $"Pinned C++ distortion PCM maximum absolute error {maxError}.");
        Console.WriteLine($"Distortion C++ oracle: 5 modes/2560 channel samples at {rate} Hz, maximum absolute PCM error {maxError}.");
    }

    private static void CPUAndWarm()
    {
        using var effect = new AudioEffectDistortion { KeepHFHZ = 1_000_000 };
        using var split = effect.Instantiate(); using var alias = effect.Instantiate();
        var input = new Vector2[256]; for (var i = 0; i < input.Length; i++) input[i] = new((i % 17 - 8) / 16f, (i % 13 - 6) / 14f);
        var output = new Vector2[input.Length]; split.Process(input.AsSpan(0, 43), output.AsSpan(0, 43)); split.Process(input.AsSpan(43), output.AsSpan(43));
        var inPlace = (Vector2[])input.Clone(); alias.Process(inPlace, inPlace);
        Check(inPlace.SequenceEqual(output), "Aliased and split processing preserve independent channel histories.");
        var one = new Vector2[1]; split.Process([new(.5f, -.5f)], one);
        Check(one[0].X < .5f && one[0].X > .49f && one[0].Y > -.5f && one[0].Y < -.49f,
            "Zero-drive clip still follows its shallow power curve.");
        effect.Drive = 1; split.Process([new(.2f, -.3f)], one);
        Check(one[0].X > .99f && one[0].Y < -.99f, "Live drive edit saturates both polarities.");
        effect.DistortionMode = AudioEffectDistortion.Mode.LoFi; split.Process([new(.2f, -.3f)], one);
        Check(one[0].DistanceTo(new(.25f, -.25f)) < .0001f, "Live mode edit quantizes at 2-bit endpoint.");
        effect.DistortionMode = AudioEffectDistortion.Mode.Overdrive;
        using var asymmetric = effect.Instantiate(); asymmetric.Process([new(.5f, -.5f)], one);
        Check(one[0].X > 0 && one[0].Y < 0 && Math.Abs(one[0].X + one[0].Y) > .0001f,
            "The pinned overdrive transfer retains its actual asymmetry.");
        effect.DistortionMode = AudioEffectDistortion.Mode.WaveShape; effect.Drive = 1;
        using var wave = effect.Instantiate(); wave.Process([new(.2f, -.2f)], one);
        Check(one[0].X > .99f && one[0].Y < -.99f && one[0].IsFinite(), "Waveshape endpoint stays finite.");
        effect.DistortionMode = AudioEffectDistortion.Mode.Clip; effect.Drive = .5f; effect.KeepHFHZ = 100;
        using var tail = effect.Instantiate(); var impulse = new Vector2[512]; impulse[0] = new(.8f, -.4f);
        tail.Process(impulse.AsSpan(0, 256), output);
        var continuation = new Vector2[256]; tail.Process(impulse.AsSpan(256), continuation);
        Check(continuation.Any(v => Math.Abs(v.X) > .00001f), "Low-pass history yields a finite-source silent-input tail.");
        using var unsafeEffect = new AudioEffectDistortion { DistortionMode = AudioEffectDistortion.Mode.LoFi, PreGain = 700, KeepHFHZ = 1_000_000 };
        using var unsafeInstance = unsafeEffect.Instantiate();
        Reject<ArithmeticException>(() => unsafeInstance.Process([new(1e10f, 0)], new Vector2[1]));
        unsafeEffect.PreGain = 0; unsafeInstance.Process([new(.1f, -.2f)], one);
        Check(one[0].IsFinite(), "Overflow clears filter history for standalone recovery.");
        effect.KeepHFHZ = 4000; effect.DistortionMode = AudioEffectDistortion.Mode.ATan; effect.Drive = .8f;
        using var warm = effect.Instantiate(); var silence = new Vector2[128]; var result = new Vector2[128];
        for (var i = 0; i < 16; i++) warm.Process(silence, result);
        var bytes = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < 64; i++) warm.Process(input.AsSpan(0, 128), result);
        Check(GC.GetAllocatedBytesForCurrentThread() == bytes, "64 warmed active distortion blocks allocate zero managed bytes.");
        bytes = GC.GetAllocatedBytesForCurrentThread(); for (var i = 0; i < 64; i++) warm.Process(silence, result);
        Check(GC.GetAllocatedBytesForCurrentThread() == bytes, "64 warmed silent-tail distortion blocks allocate zero managed bytes.");
    }

    private static void Native()
    {
        var server = AudioServer.Instance; server.CloseNative(); server.BusCount = 1;
        using var effect = new AudioEffectDistortion { DistortionMode = AudioEffectDistortion.Mode.Clip, Drive = 1, KeepHFHZ = 1_000_000 };
        using var stream = AudioEffectTests.Constant(); var root = new Node(); var player = new AudioStreamPlayer { Stream = stream }; root.AddChild(player); using var tree = new SceneTree(root);
        try
        {
            server.AddBusEffect(0, effect); player.Play(); var native = server.Native; AudioEffectTests.Wait(native, 20);
            AudioEffectTests.CheckOutput(native, new((float)Math.Pow(.2, .0001), -(float)Math.Pow(.3, .0001)));
            effect.DistortionMode = AudioEffectDistortion.Mode.LoFi; AudioEffectTests.Wait(native, 20);
            AudioEffectTests.CheckOutput(native, new(.25f, -.25f));
            if (native.Channels > 2)
            {
                var pcm = native.CapturedPCM();
                for (var frame = 0; frame < pcm.Length; frame += native.Channels)
                    for (var channel = 2; channel < native.Channels; channel++)
                        Check(Math.Abs(pcm[frame + channel]) < .0001f, "Distortion does not leak into unrelated output pairs.");
            }
            server.SetBusEffectEnabled(0, 0, false); AudioEffectTests.CheckOutput(native, new(.2f, -.3f));
            server.SetBusEffectEnabled(0, 0, true); AudioEffectTests.Wait(native, 20);
            var bytes = native.MixManagedBytes; var calls = FAudioContext.AllocationCalls; AudioEffectTests.Wait(native, 64);
            Check(native.MixManagedBytes == bytes && FAudioContext.AllocationCalls == calls, "64 warmed native active passes allocate no measured bytes/calls.");
            player.StreamPaused = true; AudioEffectTests.Wait(native, 20); bytes = native.MixManagedBytes; calls = FAudioContext.AllocationCalls;
            AudioEffectTests.Wait(native, 64); Check(native.MixManagedBytes == bytes && FAudioContext.AllocationCalls == calls,
                "64 warmed native paused passes allocate no measured bytes/calls.");
        }
        finally { tree.Dispose(); server.CloseNative(); while (server.GetBusEffectCount(0) > 0) server.RemoveBusEffect(0, 0); server.CloseNative(); }
    }

    private static void NativeTail()
    {
        var server = AudioServer.Instance; server.CloseNative(); server.BusCount = 1;
        using var effect = new AudioEffectDistortion { Drive = .5f, KeepHFHZ = 100 };
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
            native.PrepareCapture(native.QuantumFrames * native.Channels * 70); player.Play(); AudioEffectTests.Wait(native, 70);
            var pcm = native.CapturedPCM(); var found = false;
            for (var frame = 512; frame < pcm.Length / native.Channels; frame++)
                found |= Math.Abs(pcm[frame * native.Channels]) > .00001f;
            Check(found, "Finite source leaves an actual filtered distortion tail after stopping.");
            tree.ProcessFrame(.01); Check(!player.IsPlaying(), "Finite source stops before its filtered bus tail.");
        }
        finally { tree.Dispose(); server.CloseNative(); while (server.GetBusEffectCount(0) > 0) server.RemoveBusEffect(0, 0); server.CloseNative(); }
    }

    internal static void RunHost()
    {
        var settings = ProjectSettings.Instance; var previous = settings.Get(ProjectSettings.RenderingMethod);
        settings.Set(ProjectSettings.RenderingMethod, Environment.GetEnvironmentVariable("ELECTRON2D_AUDIO_RENDERER") == "compatibility" ? "compatibility" : "gpu");
        try
        {
            using var effect = new AudioEffectDistortion { Drive = 1, KeepHFHZ = 1_000_000 };
            using var stream = AudioEffectTests.Constant(); using var capture = new AudioEffectCapture { BufferLength = .1f };
            var window = new Window { Size = new(160, 96) }; window.AddChild(new AudioStreamPlayer { Stream = stream, Autoplay = true });
            var scenario = new HostScenario(capture); window.AddChild(scenario);
            var server = AudioServer.Instance; server.AddBusEffect(0, effect); server.AddBusEffect(0, capture);
            try { Check(Engine.Instance.Run(window) == 0 && scenario.Completed && window.IsDisposed, "Public distortion host processed and cleaned up."); }
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
            if (available > 0) Completed |= capture.GetBuffer(available).Any(f => f.X > .99f && f.Y < -.99f);
            if (_elapsed < .25) return;
            Check(Completed, "Public Window host captured saturated distortion PCM."); Tree!.Quit();
        }
    }
    private sealed class Holder : Node
    {
        public AudioEffectDistortion? Effect { get; set; }
        private static readonly PropertyDescriptor[] Properties = [new PropertyDescriptor<Holder, AudioEffectDistortion?>(nameof(Effect), p => p.Effect, (p, v) => p.Effect = v, _ => null, stored: true)];
        protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors() => base.GetPropertyDescriptors().Concat(Properties);
        protected override Func<Node> CreateSceneInstanceFactory() => Create;
        private static Node Create() => new Holder();
    }
    private static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    private static void Reject<T>(Action action) where T : Exception { try { action(); } catch (T) { return; } throw new InvalidOperationException($"Expected {typeof(T).Name}."); }
}
