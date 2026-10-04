using Electron2D;
using System.Text.Json;

internal static class AudioPitchShiftTests
{
    internal static void Run(bool native = false)
    {
        ResourceState(); ReferencePCM(); FrequencyAndEdges(); Warm();
        if (native) Native();
        Console.WriteLine(native ? "Pitch shift: pinned PCM, stereo frequency, native output and allocation passed."
            : "Pitch shift: pinned PCM, stereo frequency and CPU allocation passed.");
    }

    private static void ResourceState()
    {
        using var effect = new AudioEffectPitchShift();
        Check(effect.PitchScale == 1 && effect.Oversampling == 4 && effect.FFTSize == AudioFFTSize.Size2048, "Pitch defaults.");
        Check(effect.GetPropertyList().Count(p => p.IsStored && p.Name is "PitchScale" or "Oversampling" or "FFTSize") == 3,
            "Three typed stored controls.");
        Reject<ArgumentOutOfRangeException>(() => effect.PitchScale = 0);
        Reject<ArgumentOutOfRangeException>(() => effect.PitchScale = float.NaN);
        Reject<ArgumentOutOfRangeException>(() => effect.PitchScale = float.PositiveInfinity);
        Reject<ArgumentOutOfRangeException>(() => effect.Oversampling = 3);
        Reject<ArgumentOutOfRangeException>(() => effect.Oversampling = 33);
        Reject<ArgumentOutOfRangeException>(() => effect.FFTSize = AudioFFTSize.Max);
        effect.PitchScale = .75f; effect.Oversampling = 8; effect.FFTSize = AudioFFTSize.Size512;
        using var copy = (AudioEffectPitchShift)effect.Duplicate(true);
        Check(copy.PitchScale == .75f && copy.Oversampling == 8 && copy.FFTSize == AudioFFTSize.Size512,
            "Resource copy retains settings.");
        effect.ResourceLocalToScene = true; using var holder = new Holder { Effect = effect }; using var packed = new PackedScene(); packed.Pack(holder);
        using var restored = (Holder)packed.Instantiate();
        Check(restored.Effect is AudioEffectPitchShift sceneCopy && !ReferenceEquals(sceneCopy, effect) &&
              sceneCopy.PitchScale == .75f && sceneCopy.Oversampling == 8 && sceneCopy.FFTSize == AudioFFTSize.Size512,
            "Scene-local copy retains settings without instance state.");
        restored.Effect!.Dispose();
        Action<Resource> fail = _ => throw new ApplicationException("Pitch observer fixture."); effect.Changed += fail;
        Reject<ApplicationException>(() => effect.PitchScale = 2);
        Check(effect.PitchScale == 2, "Observer error follows committed edit."); effect.Changed -= fail;
        using var instance = effect.Instantiate(); Check(instance.ProcessSilence(), "Pitch state flushes over silent input.");
        effect.Dispose(); Reject<ObjectDisposedException>(() => instance.Process([Vector2.Zero], new Vector2[1]));
    }

    private sealed record OracleCase(int Rate, int Size, int Overlap, float Scale, float[] Input, float[] PCM);
    private static void ReferencePCM()
    {
        var rate = (int)AudioServer.GetMixRate();
        using var resource = typeof(AudioPitchShiftTests).Assembly.GetManifestResourceStream("TestAudio.PitchShiftReference.json")!;
        var cases = JsonSerializer.Deserialize<OracleCase[]>(resource)!.Where(item => item.Rate == rate).ToArray();
        Check(cases.Length == 2, "Two pinned phase-vocoder profiles at the output rate.");
        var maxError = 0f;
        foreach (var item in cases)
        {
            using var effect = new AudioEffectPitchShift
            {
                PitchScale = item.Scale,
                Oversampling = item.Overlap,
                FFTSize = (AudioFFTSize)System.Numerics.BitOperations.Log2((uint)(item.Size / 256))
            };
            using var split = effect.Instantiate(); using var alias = effect.Instantiate();
            var input = new Vector2[item.Input.Length / 2]; var output = new Vector2[input.Length];
            for (var i = 0; i < input.Length; i++) input[i] = new(item.Input[2 * i], item.Input[2 * i + 1]);
            split.Process(input.AsSpan(0, 37), output.AsSpan(0, 37));
            split.Process(input.AsSpan(37, 91), output.AsSpan(37, 91));
            split.Process(input.AsSpan(128), output.AsSpan(128));
            var inPlace = (Vector2[])input.Clone(); alias.Process(inPlace, inPlace);
            Check(inPlace.SequenceEqual(output), "Aliased and split pitch processing share exact state evolution.");
            Check(item.PCM.Length == output.Length * 2, "Pinned pitch sample count.");
            for (var i = 0; i < output.Length; i++)
            {
                maxError = MathF.Max(maxError, MathF.Abs(output[i].X - item.PCM[2 * i]));
                maxError = MathF.Max(maxError, MathF.Abs(output[i].Y - item.PCM[2 * i + 1]));
            }
        }
        Check(maxError < .003f, $"Pinned pitch PCM maximum absolute error {maxError}.");
        Console.WriteLine($"Pitch C++ oracle: 2 stereo profiles at {rate} Hz; maximum absolute PCM error {maxError}.");
    }

    private static void FrequencyAndEdges()
    {
        var rate = (int)AudioServer.GetMixRate();
        var input = SineFrames(rate, 16384); var output = new Vector2[input.Length];
        using var effect = new AudioEffectPitchShift { PitchScale = 2, FFTSize = AudioFFTSize.Size512, Oversampling = 8 };
        using var instance = effect.Instantiate(); instance.Process(input, output);
        Check(Magnitude(output, 0, 880, rate) > Magnitude(output, 0, 440, rate) * 2 &&
              Magnitude(output, 1, 1320, rate) > Magnitude(output, 1, 660, rate) * 2,
            "Independent stereo tones shift one octave without changing output frame count.");
        var silence = new Vector2[4096]; instance.Process(silence, silence);
        Check(silence.Any(frame => frame != Vector2.Zero) && silence[^1] == Vector2.Zero,
            "Silent processing flushes the finite source tail.");
        effect.Oversampling = 32; var next = new Vector2[1024]; instance.Process(input.AsSpan(0, 1024), next);
        using (var freshOverlap = effect.Instantiate())
        {
            var expected = new Vector2[1024]; freshOverlap.Process(input.AsSpan(0, 1024), expected);
            Check(next.SequenceEqual(expected), "Live overlap edit resets to fresh compatible history.");
        }
        effect.PitchScale = 1; instance.Process(input.AsSpan(0, 1024), next);
        Check(next.SequenceEqual(input.AsSpan(0, 1024).ToArray()), "Unit pitch bypasses spectral latency bit-for-bit.");
        effect.PitchScale = 1.0000005f; instance.Process(input.AsSpan(0, 1024), next);
        Check(next.SequenceEqual(input.AsSpan(0, 1024).ToArray()), "Near-unit pitch uses the same exact bypass boundary.");
        effect.PitchScale = .5f; instance.Process(input.AsSpan(0, 1024), next);
        using (var freshResume = effect.Instantiate())
        {
            var expected = new Vector2[1024]; freshResume.Process(input.AsSpan(0, 1024), expected);
            Check(next.SequenceEqual(expected), "Resuming pitch shift starts a fresh spectral window.");
        }
        for (var size = AudioFFTSize.Size256; size < AudioFFTSize.Max; size++)
        {
            using var preset = new AudioEffectPitchShift { FFTSize = size, PitchScale = 1.3f };
            using var state = preset.Instantiate(); var count = (256 << (int)size) * 3;
            var frames = SineFrames(rate, count); var result = new Vector2[count]; state.Process(frames, result);
            Check(result.Any(frame => frame != Vector2.Zero) && result.All(frame => frame.IsFinite()),
                $"FFT preset {size} produces finite, nonempty resynthesis.");
        }
        using (var oddOverlap = new AudioEffectPitchShift { FFTSize = AudioFFTSize.Size256, PitchScale = 1.3f, Oversampling = 5 })
        using (var oddState = oddOverlap.Instantiate())
        {
            var frames = SineFrames(rate, 2048); var result = new Vector2[frames.Length]; oddState.Process(frames, result);
            Check(result.Any(frame => frame != Vector2.Zero) && result.All(frame => frame.IsFinite()),
                "A non-power-of-two overlap factor retains a positive transform step.");
        }
        using (var prepared = new AudioEffectPitchShift { FFTSize = AudioFFTSize.Size256, PitchScale = 1.5f })
        using (var oldSize = prepared.Instantiate())
        {
            prepared.FFTSize = AudioFFTSize.Size4096;
            var frames = SineFrames(rate, 1024); var oldOutput = new Vector2[frames.Length]; oldSize.Process(frames, oldOutput);
            using var newSize = prepared.Instantiate(); var newOutput = new Vector2[frames.Length]; newSize.Process(frames, newOutput);
            Check(oldOutput.Any(frame => frame != Vector2.Zero) && newOutput.All(frame => frame == Vector2.Zero),
                "FFT-size edits affect new instances without resizing live histories.");
        }
        using var extreme = new AudioEffectPitchShift { FFTSize = AudioFFTSize.Size256, PitchScale = 2 };
        using var damaged = extreme.Instantiate(); using var pristine = extreme.Instantiate();
        Reject<ArithmeticException>(() => damaged.Process(Enumerable.Repeat(new Vector2(float.MaxValue, 0), 256).ToArray(), new Vector2[256]));
        var recovery = SineFrames(rate, 512); var recovered = new Vector2[512]; var fresh = new Vector2[512];
        damaged.Process(recovery, recovered); pristine.Process(recovery, fresh);
        Check(recovered.SequenceEqual(fresh), "Overflow clears both channel histories for recovery.");
    }

    private static void Warm()
    {
        using var effect = new AudioEffectPitchShift { FFTSize = AudioFFTSize.Size256, PitchScale = 1.5f };
        using var instance = effect.Instantiate(); var input = SineFrames((int)AudioServer.GetMixRate(), 256); var output = new Vector2[256];
        for (var i = 0; i < 16; i++) instance.Process(input, output);
        var bytes = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < 64; i++) instance.Process(input, output);
        Check(GC.GetAllocatedBytesForCurrentThread() == bytes, "64 warmed active phase-vocoder blocks allocate zero managed bytes.");
        input.AsSpan().Clear(); bytes = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < 64; i++) instance.Process(input, output);
        Check(GC.GetAllocatedBytesForCurrentThread() == bytes, "64 warmed silent phase-vocoder blocks allocate zero managed bytes.");
    }

    private static void Native()
    {
        var server = AudioServer.Service; server.CloseNative(); AudioServer.BusCount = 1;
        using var effect = new AudioEffectPitchShift { PitchScale = 2, FFTSize = AudioFFTSize.Size256, Oversampling = 4 };
        using var stream = SineStream(); var root = new Node(); var player = new AudioStreamPlayer { Stream = stream }; root.AddChild(player); using var tree = new SceneTree(root);
        try
        {
            AudioServer.AddBusEffect(0, effect); player.Play(); var native = server.Native; AudioEffectTests.Wait(native, 30);
            var borrowed = AudioServer.GetBusEffectInstance(0, 0);
            native.PrepareCapture(native.QuantumFrames * native.Channels * 20); AudioEffectTests.Wait(native, 22);
            var pcm = native.CapturedPCM(); var rate = native.MixRate;
            Check(pcm.Length > 0 && Magnitude(pcm, native.Channels, 0, 880, rate) > Magnitude(pcm, native.Channels, 0, 440, rate) * 2 &&
                  Magnitude(pcm, native.Channels, 1, 1320, rate) > Magnitude(pcm, native.Channels, 1, 660, rate) * 2,
                "Native front stereo pair shifts distinct tones upward.");
            for (var i = 0; i < pcm.Length; i += native.Channels)
                for (var channel = 2; channel < native.Channels; channel++) Check(MathF.Abs(pcm[i + channel]) < .0001f, "Rear speaker pairs remain silent.");
            AudioServer.SetBusEffectEnabled(0, 0, false); AudioEffectTests.Wait(native, 10);
            native.PrepareCapture(native.QuantumFrames * native.Channels * 10); AudioEffectTests.Wait(native, 12);
            pcm = native.CapturedPCM();
            Check(Magnitude(pcm, native.Channels, 0, 440, rate) > Magnitude(pcm, native.Channels, 0, 880, rate) * 2,
                "Bus effect disable restores original pitch.");
            AudioServer.SetBusEffectEnabled(0, 0, true); AudioEffectTests.Wait(native, 20);
            var bytes = native.MixManagedBytes; var calls = FAudioContext.AllocationCalls; AudioEffectTests.Wait(native, 64);
            Check(native.MixManagedBytes == bytes && FAudioContext.AllocationCalls == calls, "64 warmed native active passes allocate no measured bytes/calls.");
            player.StreamPaused = true; AudioEffectTests.Wait(native, 20); bytes = native.MixManagedBytes; calls = FAudioContext.AllocationCalls;
            AudioEffectTests.Wait(native, 64);
            Check(native.MixManagedBytes == bytes && FAudioContext.AllocationCalls == calls, "64 warmed native paused passes allocate no measured bytes/calls.");
            AudioServer.RemoveBusEffect(0, 0); Check(borrowed.IsDisposed && !effect.IsDisposed, "Removal invalidates borrowed pitch state.");
        }
        finally { tree.Dispose(); server.CloseNative(); while (AudioServer.GetBusEffectCount(0) > 0) AudioServer.RemoveBusEffect(0, 0); server.CloseNative(); }
    }

    internal static void RunHost()
    {
        var settings = ProjectSettings.Service; var previous = ProjectSettings.Get(ProjectSettings.RenderingMethod);
        ProjectSettings.Set(ProjectSettings.RenderingMethod, Environment.GetEnvironmentVariable("ELECTRON2D_AUDIO_RENDERER") == "compatibility" ? "compatibility" : "gpu");
        try
        {
            using var effect = new AudioEffectPitchShift { PitchScale = 2, FFTSize = AudioFFTSize.Size256 };
            using var stream = SineStream(); using var capture = new AudioEffectCapture { BufferLength = .1f };
            var window = new Window { Size = new(160, 96) }; window.AddChild(new AudioStreamPlayer { Stream = stream, Autoplay = true });
            var scenario = new HostScenario(capture); window.AddChild(scenario);
            var server = AudioServer.Service; AudioServer.AddBusEffect(0, effect); AudioServer.AddBusEffect(0, capture);
            try { Check(Engine.Run(window) == 0 && scenario.Completed && window.IsDisposed, "Public pitch host processed and cleaned up."); }
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
            if (available > 1024)
            {
                var pcm = capture.GetBuffer(available);
                var rate = (int)AudioServer.GetMixRate();
                Completed |= Magnitude(pcm, 0, 880, rate) > Magnitude(pcm, 0, 440, rate) * 2;
            }
            if (_elapsed < .35) return;
            Check(Completed, "Public Window host captured octave-shifted PCM."); Tree!.Quit();
        }
    }
    private sealed class Holder : Node
    {
        public AudioEffectPitchShift? Effect { get; set; }
        private static readonly PropertyDescriptor[] Properties = [new PropertyDescriptor<Holder, AudioEffectPitchShift?>(nameof(Effect), p => p.Effect, (p, v) => p.Effect = v, _ => null, stored: true)];
        protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors() => base.GetPropertyDescriptors().Concat(Properties);
        protected override Func<Node> CreateSceneInstanceFactory() => Create;
        private static Node Create() => new Holder();
    }
    private static Vector2[] SineFrames(int rate, int frames)
    {
        var result = new Vector2[frames];
        for (var i = 0; i < frames; i++) result[i] = new(.4f * (float)Math.Sin(Math.Tau * 440 * i / rate), .3f * (float)Math.Sin(Math.Tau * 660 * i / rate));
        return result;
    }
    private static AudioStreamWAV SineStream()
    {
        const int rate = 44100; var frames = SineFrames(rate, rate); var samples = new float[frames.Length * 2];
        for (var i = 0; i < frames.Length; i++) { samples[2 * i] = frames[i].X; samples[2 * i + 1] = frames[i].Y; }
        return new AudioStreamWAV
        {
            SampleFormat = AudioStreamWAV.Format.PCM16,
            Stereo = true,
            MixRate = rate,
            Data = AudioPCMCodec.Encode(samples, AudioStreamWAV.Format.PCM16, 2, rate),
            Loop = AudioLoopMode.Forward,
            LoopEnd = rate - 1
        };
    }
    private static double Magnitude(Vector2[] frames, int channel, double hz, int rate)
    {
        double real = 0, imaginary = 0; var begin = frames.Length / 2;
        for (var i = begin; i < frames.Length; i++)
        {
            var sample = channel == 0 ? frames[i].X : frames[i].Y; var phase = Math.Tau * hz * i / rate;
            real += sample * Math.Cos(phase); imaginary += sample * Math.Sin(phase);
        }
        return Math.Sqrt(real * real + imaginary * imaginary) / (frames.Length - begin);
    }
    private static double Magnitude(float[] pcm, int channels, int channel, double hz, int rate)
    {
        double real = 0, imaginary = 0; var frames = pcm.Length / channels; var begin = frames / 2;
        for (var i = begin; i < frames; i++)
        {
            var phase = Math.Tau * hz * i / rate; var sample = pcm[i * channels + channel];
            real += sample * Math.Cos(phase); imaginary += sample * Math.Sin(phase);
        }
        return Math.Sqrt(real * real + imaginary * imaginary) / (frames - begin);
    }
    private static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    private static void Reject<T>(Action action) where T : Exception { try { action(); } catch (T) { return; } throw new InvalidOperationException($"Expected {typeof(T).Name}."); }
}
