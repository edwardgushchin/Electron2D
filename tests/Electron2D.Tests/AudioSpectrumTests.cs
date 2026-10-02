using Electron2D;

internal static class AudioSpectrumTests
{
    internal static void Run(bool native = false)
    {
        ResourceState(); FrequencyProfiles(); WarmAndRecovery();
        if (native) Native();
        Console.WriteLine(native ? "Audio spectrum: five FFT sizes, stereo queries, native PCM and warmed allocation passed."
            : "Audio spectrum: five FFT sizes, stereo queries and warmed CPU allocation passed.");
    }

    private static void ResourceState()
    {
        using var effect = new AudioEffectSpectrumAnalyzer();
        Check(effect.BufferLength == 2 && effect.FFTSize == AudioFFTSize.Size1024, "Analyzer defaults.");
        Check(Enum.GetValues<AudioFFTSize>().Select(value => (int)value).SequenceEqual([0, 1, 2, 3, 4, 5]), "Shared FFT enum numeric identity.");
        Check(effect.GetPropertyList().Count(p => p.IsStored && p.Name is "BufferLength" or "FFTSize") == 2, "Two typed stored controls.");
        Reject<ArgumentOutOfRangeException>(() => effect.BufferLength = .09f);
        Reject<ArgumentOutOfRangeException>(() => effect.BufferLength = 4.1f);
        Reject<ArgumentOutOfRangeException>(() => effect.BufferLength = float.NaN);
        Reject<ArgumentOutOfRangeException>(() => effect.FFTSize = AudioFFTSize.Max);
        Reject<ArgumentOutOfRangeException>(() => effect.FFTSize = (AudioFFTSize)(-1));
        effect.BufferLength = .3f; effect.FFTSize = AudioFFTSize.Size4096;
        using var copy = (AudioEffectSpectrumAnalyzer)effect.Duplicate(true);
        Check(copy.BufferLength == .3f && copy.FFTSize == AudioFFTSize.Size4096, "Concrete resource copy retains both controls.");
        effect.ResourceLocalToScene = true; using var holder = new Holder { Effect = effect }; using var packed = new PackedScene(); packed.Pack(holder);
        using var restored = (Holder)packed.Instantiate();
        Check(restored.Effect is AudioEffectSpectrumAnalyzer sceneCopy && !ReferenceEquals(sceneCopy, effect) &&
              sceneCopy.BufferLength == .3f && sceneCopy.FFTSize == AudioFFTSize.Size4096, "Scene-local analyzer copy.");
        restored.Effect!.Dispose();
        Action<Resource> fail = _ => throw new ApplicationException("Analyzer observer fixture."); effect.Changed += fail;
        Reject<ApplicationException>(() => effect.BufferLength = .5f); Check(effect.BufferLength == .5f, "Observer failure follows committed edit."); effect.Changed -= fail;
        using var instance = (AudioEffectSpectrumAnalyzerInstance)effect.Instantiate();
        Check(instance.ProcessSilence() && instance.GetMagnitudeForFrequencyRange(0, 1000) == Vector2.Zero,
            "New analyzer processes silence and exposes zero before its first complete transform.");
        Reject<ArgumentOutOfRangeException>(() => instance.GetMagnitudeForFrequencyRange(float.NaN, 1000));
        Reject<ArgumentOutOfRangeException>(() => instance.GetMagnitudeForFrequencyRange(0, 1000, (AudioEffectSpectrumAnalyzerInstance.MagnitudeMode)2));
        effect.Dispose(); Reject<ObjectDisposedException>(() => instance.Process([Vector2.Zero], new Vector2[1]));
        Reject<ObjectDisposedException>(() => instance.GetMagnitudeForFrequencyRange(0, 1000));
    }

    private static void FrequencyProfiles()
    {
        var rate = AudioServer.Instance.GetMixRate();
        for (var preset = 0; preset < 5; preset++)
        {
            using var effect = new AudioEffectSpectrumAnalyzer { FFTSize = (AudioFFTSize)preset, BufferLength = .1f };
            using var instance = (AudioEffectSpectrumAnalyzerInstance)effect.Instantiate();
            using var alias = (AudioEffectSpectrumAnalyzerInstance)effect.Instantiate();
            var bins = 256 << preset; var frames = bins * 2;
            var input = new Vector2[frames]; var output = new Vector2[frames];
            for (var i = 0; i < frames; i++)
                input[i] = new(.8f * (float)Math.Sin(Math.Tau * 13 * i / frames), .4f * (float)Math.Sin(Math.Tau * 27 * i / frames));
            instance.Process(input.AsSpan(0, 73), output.AsSpan(0, 73));
            instance.Process(input.AsSpan(73, 147), output.AsSpan(73, 147));
            instance.Process(input.AsSpan(220), output.AsSpan(220));
            Check(output.SequenceEqual(input), "Analyzer leaves finite stereo PCM bit-identical.");
            var inPlace = (Vector2[])input.Clone(); alias.Process(inPlace, inPlace);
            Check(inPlace.SequenceEqual(input), "Analyzer supports aliased input and output.");
            var leftHZ = (13.25f * rate) / frames; var rightHZ = (27.25f * rate) / frames;
            var left = instance.GetMagnitudeForFrequencyRange(leftHZ, leftHZ);
            var right = instance.GetMagnitudeForFrequencyRange(rightHZ, rightHZ);
            Check(left.X is > .39f and < .41f && left.Y < .001f && right.Y is > .19f and < .21f && right.X < .001f,
                $"Hann-normalized stereo peaks at {bins} bins: left={left}, right={right}.");
            Check(alias.GetMagnitudeForFrequencyRange(leftHZ, leftHZ).DistanceTo(left) < .00001f,
                "Split and aliased transforms publish the same spectrum.");
            var low = 10.25f * rate / frames; var high = 16.25f * rate / frames;
            var max = instance.GetMagnitudeForFrequencyRange(low, high);
            var average = instance.GetMagnitudeForFrequencyRange(high, low, AudioEffectSpectrumAnalyzerInstance.MagnitudeMode.Average);
            Check(max.X >= left.X && average.X > 0 && average.X < max.X,
                "Inclusive maximum and reversed-range average use the requested bins.");
            Check(instance.GetMagnitudeForFrequencyRange(-100, -1).IsFinite() &&
                  instance.GetMagnitudeForFrequencyRange(rate, rate * 2).IsFinite(), "Out-of-band finite frequencies clamp to edge bins.");
            effect.FFTSize = AudioFFTSize.Size256; effect.BufferLength = 4;
            var silence = new Vector2[frames]; instance.Process(silence, silence);
            Check(instance.GetMagnitudeForFrequencyRange(leftHZ, leftHZ) == Vector2.Zero,
                "Existing prepared transform keeps its creation-time size and replaces old PCM after a silent window.");
        }
    }

    private static void WarmAndRecovery()
    {
        using var effect = new AudioEffectSpectrumAnalyzer { FFTSize = AudioFFTSize.Size256, BufferLength = .1f };
        using var instance = (AudioEffectSpectrumAnalyzerInstance)effect.Instantiate();
        var input = new Vector2[128]; var output = new Vector2[128];
        input.AsSpan().Fill(new(.2f, -.3f));
        for (var i = 0; i < 16; i++) instance.Process(input, output);
        var bytes = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < 64; i++) { instance.Process(input, output); _ = instance.GetMagnitudeForFrequencyRange(0, 1000); }
        Check(GC.GetAllocatedBytesForCurrentThread() == bytes, "64 warmed active transforms and queries allocate zero managed bytes.");
        input.AsSpan().Clear(); bytes = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < 64; i++) { instance.Process(input, output); _ = instance.GetMagnitudeForFrequencyRange(0, 1000); }
        Check(GC.GetAllocatedBytesForCurrentThread() == bytes, "64 warmed silent transforms and queries allocate zero managed bytes.");
        var overflow = new Vector2[512]; overflow.AsSpan().Fill(new(float.MaxValue, 0));
        Reject<ArithmeticException>(() => instance.Process(overflow, overflow));
        var recovery = new Vector2[512]; recovery.AsSpan().Fill(new(.1f, 0));
        instance.Process(recovery, recovery);
        Check(instance.GetMagnitudeForFrequencyRange(0, 0).X is > .09f and < .11f,
            "Spectrum history resets after hidden transform overflow and accepts the next complete block.");
    }

    private static void Native()
    {
        var server = AudioServer.Instance; server.CloseNative(); server.BusCount = 1;
        using var effect = new AudioEffectSpectrumAnalyzer { FFTSize = AudioFFTSize.Size256, BufferLength = .1f };
        using var stream = AudioEffectTests.Constant(); var root = new Node(); var player = new AudioStreamPlayer { Stream = stream }; root.AddChild(player); using var tree = new SceneTree(root);
        try
        {
            server.AddBusEffect(0, effect); player.Play(); var native = server.Native; AudioEffectTests.Wait(native, 30);
            var borrowed = (AudioEffectSpectrumAnalyzerInstance)server.GetBusEffectInstance(0, 0);
            var dc = borrowed.GetMagnitudeForFrequencyRange(0, 0);
            Check(dc.X is > .19f and < .21f && dc.Y is > .29f and < .31f, $"Borrowed native bus instance exposes stereo DC spectrum: {dc}.");
            AudioEffectTests.CheckOutput(native, new(.2f, -.3f));
            if (native.Channels > 2)
            {
                var pcm = native.CapturedPCM();
                for (var frame = 0; frame < pcm.Length; frame += native.Channels)
                    for (var channel = 2; channel < native.Channels; channel++)
                        Check(Math.Abs(pcm[frame + channel]) < .0001f, "Analyzer does not leak front-pair PCM into unrelated pairs.");
                for (var pair = 1; pair < native.Channels / 2; pair++)
                    Check(((AudioEffectSpectrumAnalyzerInstance)server.GetBusEffectInstance(0, 0, pair)).GetMagnitudeForFrequencyRange(0, 0) == Vector2.Zero,
                        "Each output stereo pair owns an independent zero spectrum when no source feeds it.");
            }
            var bytes = native.MixManagedBytes; var calls = FAudioContext.AllocationCalls; AudioEffectTests.Wait(native, 64);
            Check(native.MixManagedBytes == bytes && FAudioContext.AllocationCalls == calls, "64 warmed native active FFT passes allocate no measured bytes/calls.");
            player.StreamPaused = true; AudioEffectTests.Wait(native, 30);
            Check(borrowed.GetMagnitudeForFrequencyRange(0, 0) == Vector2.Zero, "Silent native windows replace the prior live spectrum.");
            bytes = native.MixManagedBytes; calls = FAudioContext.AllocationCalls; AudioEffectTests.Wait(native, 64);
            Check(native.MixManagedBytes == bytes && FAudioContext.AllocationCalls == calls, "64 warmed native paused FFT passes allocate no measured bytes/calls.");
            server.RemoveBusEffect(0, 0);
            Reject<ObjectDisposedException>(() => borrowed.GetMagnitudeForFrequencyRange(0, 0));
        }
        finally { tree.Dispose(); server.CloseNative(); while (server.GetBusEffectCount(0) > 0) server.RemoveBusEffect(0, 0); server.CloseNative(); }
    }

    internal static void RunHost()
    {
        var settings = ProjectSettings.Instance; var previous = settings.Get(ProjectSettings.RenderingMethod);
        settings.Set(ProjectSettings.RenderingMethod, Environment.GetEnvironmentVariable("ELECTRON2D_AUDIO_RENDERER") == "compatibility" ? "compatibility" : "gpu");
        try
        {
            using var effect = new AudioEffectSpectrumAnalyzer { FFTSize = AudioFFTSize.Size256, BufferLength = .1f };
            using var stream = AudioEffectTests.Constant(); using var capture = new AudioEffectCapture { BufferLength = .1f };
            var window = new Window { Size = new(160, 96) }; window.AddChild(new AudioStreamPlayer { Stream = stream, Autoplay = true });
            var scenario = new HostScenario(capture); window.AddChild(scenario);
            var server = AudioServer.Instance; server.AddBusEffect(0, effect); server.AddBusEffect(0, capture);
            try { Check(Engine.Instance.Run(window) == 0 && scenario.Completed && window.IsDisposed, "Public analyzer host queried and cleaned up."); }
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
            var server = AudioServer.Instance;
            var available = capture.GetFramesAvailable();
            var pcm = available > 0 ? capture.GetBuffer(available) : [];
            var magnitude = ((AudioEffectSpectrumAnalyzerInstance)server.GetBusEffectInstance(0, 0)).GetMagnitudeForFrequencyRange(0, 0);
            Completed |= pcm.Any(f => f.DistanceTo(new(.2f, -.3f)) < .0001f) && magnitude.X is > .19f and < .21f && magnitude.Y is > .29f and < .31f;
            if (_elapsed < .3) return;
            Check(Completed, "Public Window host captured pass-through PCM and queried stereo magnitude."); Tree!.Quit();
        }
    }
    private sealed class Holder : Node
    {
        public AudioEffectSpectrumAnalyzer? Effect { get; set; }
        private static readonly PropertyDescriptor[] Properties = [new PropertyDescriptor<Holder, AudioEffectSpectrumAnalyzer?>(nameof(Effect), p => p.Effect, (p, v) => p.Effect = v, _ => null, stored: true)];
        protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors() => base.GetPropertyDescriptors().Concat(Properties);
        protected override Func<Node> CreateSceneInstanceFactory() => Create;
        private static Node Create() => new Holder();
    }
    private static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    private static void Reject<T>(Action action) where T : Exception { try { action(); } catch (T) { return; } throw new InvalidOperationException($"Expected {typeof(T).Name}."); }
}
