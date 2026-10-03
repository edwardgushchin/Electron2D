using Electron2D;
using System.Runtime.InteropServices;
using System.Text.Json;
using Slope = Electron2D.AudioEffectFilter.FilterDB;

internal static class AudioFilterTests
{
    internal static void Run(bool native = false)
    {
        Resources(); ReferencePCM(); ResponsesAndEdges(); HistoryAndWarm(); Concurrency();
        if (native) Native();
        Console.WriteLine("Audio filters: typed resources, pinned C++ PCM, all responses/stages, stable edges, history, native integration and warmed allocations passed.");
    }
    private static AudioEffectFilter[] Family() => [new AudioEffectFilter(), new AudioEffectLowPassFilter(), new AudioEffectHighPassFilter(), new AudioEffectBandPassFilter(), new AudioEffectNotchFilter(), new AudioEffectBandLimitFilter(), new AudioEffectLowShelfFilter(), new AudioEffectHighShelfFilter()];
    private static void Resources()
    {
        foreach (var filter in Family()) using (filter)
            {
                Check(filter.CutoffHZ == 2000 && filter.Resonance == .5f && filter.Gain == 1 && filter.DB == Slope.Filter6DB, "Family defaults.");
                foreach (var value in new[] { float.NaN, float.PositiveInfinity, float.NegativeInfinity }) { Reject<ArgumentOutOfRangeException>(() => filter.CutoffHZ = value); Reject<ArgumentOutOfRangeException>(() => filter.Resonance = value); Reject<ArgumentOutOfRangeException>(() => filter.Gain = value); }
                Reject<ArgumentOutOfRangeException>(() => filter.DB = (Slope)4); Check(filter.DB == Slope.Filter6DB, "Selector rejection is transactional.");
                filter.CutoffHZ = -123; Check(filter.CutoffHZ == 1, "Finite cutoff floor."); filter.CutoffHZ = 30000; filter.Resonance = -.5f; filter.Gain = -.3f; filter.DB = Slope.Filter24DB;
                using var copy = (AudioEffectFilter)filter.Duplicate(true); Check(copy.GetType() == filter.GetType() && copy.CutoffHZ == 30000 && copy.Resonance == -.5f && copy.Gain == -.3f && copy.DB == Slope.Filter24DB, "Exact concrete configuration copy, including signed raw controls.");
                filter.ResourceLocalToScene = true; using var holder = new Holder { Filter = filter }; using var scene = new PackedScene(); scene.Pack(holder); using var restored = (Holder)scene.Instantiate(); var local = restored.Filter!; Check(!ReferenceEquals(local, filter) && local.GetType() == filter.GetType() && local.CutoffHZ == filter.CutoffHZ && local.DB == filter.DB, "Typed scene-local filter configuration."); local.Dispose();
                var fields = filter.GetPropertyList(); Check(fields.Any(p => p.Name == nameof(AudioEffectFilter.CutoffHZ) && !p.IsReadOnly), "Typed cutoff authoring.");
                var hasGain = fields.Any(p => p.Name == nameof(AudioEffectFilter.Gain)); Check(hasGain == (filter.GetType() == typeof(AudioEffectFilter) || filter is AudioEffectNotchFilter or AudioEffectBandLimitFilter or AudioEffectLowShelfFilter or AudioEffectHighShelfFilter), "Concrete pass family hides unused Gain metadata; base retains it.");
                Action<Resource> fail = _ => throw new ApplicationException("Filter observer fixture."); filter.Changed += fail; Reject<ApplicationException>(() => filter.CutoffHZ = 2500); Check(filter.CutoffHZ == 2500, "Changed failure follows committed value."); filter.Changed -= fail;
                using var first = filter.Instantiate(); using var second = filter.Instantiate(); Check(!ReferenceEquals(first, second) && !first.ProcessSilence(), "Independent histories/default inactive-silence policy."); filter.Dispose(); Reject<ObjectDisposedException>(() => first.Process([Vector2.Zero], new Vector2[1])); Reject<ObjectDisposedException>(() => filter.Instantiate());
            }
    }
    private sealed record OracleCase(string Kind, int Stages, float Cutoff, float Resonance, float Gain, double[] Coefficients, float[] PCM);
    private static void ReferencePCM()
    {
        using var stream = typeof(AudioFilterTests).Assembly.GetManifestResourceStream("TestAudio.FilterReference.json")!; var cases = JsonSerializer.Deserialize<OracleCase[]>(stream)!; Check(cases.Length == 72, "Independent pinned C++ fixture census.");
        var input = new Vector2[256]; for (var i = 0; i < input.Length; i++) input[i] = i == 0 ? new(.75f, -.5f) : new(((i * 37) % 101 - 50) / 128f, ((i * 37 + 11) % 101 - 50) / 128f);
        var output = new Vector2[256]; var maxError = 0f;
        foreach (var item in cases)
        {
            using var filter = item.Kind switch { "LowPass" => new AudioEffectLowPassFilter(), "HighPass" => new AudioEffectHighPassFilter(), "BandPass" => new AudioEffectBandPassFilter(), "Notch" => new AudioEffectNotchFilter(), "LowShelf" => new AudioEffectLowShelfFilter(), "HighShelf" => (AudioEffectFilter)new AudioEffectHighShelfFilter(), _ => throw new InvalidOperationException("Unknown fixture kind.") };
            filter.CutoffHZ = item.Cutoff; filter.Resonance = item.Resonance; filter.Gain = item.Gain; filter.DB = (Slope)(item.Stages - 1);
            var coefficients = AudioFilterKernel.Prepare(filter.Snapshot(), 44100); var actual = new[] { coefficients.B0, coefficients.B1, coefficients.B2, coefficients.A1, coefficients.A2 };
            for (var i = 0; i < actual.Length; i++) Check(Math.Abs(actual[i] - item.Coefficients[i]) < 1e-12, "Coefficient oracle matches ordinary regimes.");
            using var instance = filter.Instantiate(); instance.Process(input.AsSpan(0, 113), output.AsSpan(0, 113)); instance.Process(input.AsSpan(113), output.AsSpan(113));
            for (var i = 0; i < output.Length; i++) { maxError = Math.Max(maxError, Math.Abs(output[i].X - item.PCM[i * 2])); maxError = Math.Max(maxError, Math.Abs(output[i].Y - item.PCM[i * 2 + 1])); }
            using var alias = filter.Instantiate(); var inPlace = (Vector2[])input.Clone(); alias.Process(inPlace, inPlace); Check(inPlace.SequenceEqual(output), "Arbitrary block split and aliased span preserve exact history.");
        }
        Check(maxError < .000003f, $"Pinned PCM max error {maxError}."); Console.WriteLine($"Filter C++ oracle: 72 cases/36864 channel samples, maximum absolute PCM error {maxError}.");
    }
    private static double Response(AudioEffectFilter filter, float frequency, float rate = 44100)
    {
        var c = AudioFilterKernel.Prepare(filter.Snapshot(), rate); var z = System.Numerics.Complex.FromPolarCoordinates(1, -Math.Tau * frequency / rate);
        var value = (c.B0 + c.B1 * z + c.B2 * z * z) / (1 - c.A1 * z - c.A2 * z * z); return Math.Pow(value.Magnitude, (int)filter.DB + 1);
    }
    private static void ResponsesAndEdges()
    {
        using var low = new AudioEffectLowPassFilter(); using var high = new AudioEffectHighPassFilter(); using var band = new AudioEffectBandPassFilter(); using var notch = new AudioEffectNotchFilter(); using var limit = new AudioEffectBandLimitFilter(); using var shelfLow = new AudioEffectLowShelfFilter { Gain = 2 }; using var shelfHigh = new AudioEffectHighShelfFilter { Gain = 2 };
        Check(Response(low, 20) > .99 && Response(low, 18000) < .005 && Response(high, 20) < .001 && Response(high, 18000) > .99, "Real low/high-pass response.");
        Check(Response(band, 2000) > 1 && Response(notch, 2000) < .00001 && Response(band, 10) < .01, "Band-pass center gain and notch rejection.");
        Check(Response(limit, 1000.25f) < .00001 && Response(limit, .00001f) > .99 && Response(limit, 22049) > .99 && Response(limit, 1000) < Response(notch, 1000), "BandLimit rejects its broad band and preserves outside limits; fixes pass-band numerator.");
        Check(Response(shelfLow, 10) > 3.99 && Response(shelfLow, 22000) < 1.001 && Response(shelfHigh, 10) < 1.001 && Response(shelfHigh, 22000) > 3.99, "Shelf gain is the coefficient squared for one stage.");
        var impulse = new Vector2[512]; impulse[0] = new(.25f, -.1f); var output = new Vector2[512];
        foreach (var filter in Family()) using (filter)
                foreach (var db in Enum.GetValues<Slope>()) foreach (var cutoff in new[] { 1f, 22050, 30000, float.MaxValue }) foreach (var resonance in new[] { -1f, 0, .5f, 2000, float.MaxValue })
                        {
                            filter.DB = db; filter.CutoffHZ = cutoff; filter.Resonance = resonance; filter.Gain = .5f;
                            var c = AudioFilterKernel.Prepare(filter.Snapshot(), 44100); Check(Math.Abs(c.A2) < 1 && 1 - c.A1 - c.A2 > 0 && 1 + c.A1 - c.A2 > 0, "All boundary coefficients have poles inside the unit circle.");
                            using var instance = filter.Instantiate(); instance.Process(impulse, output); for (var pass = 0; pass < 32; pass++) { impulse[0] = Vector2.Zero; instance.Process(impulse, output); }
                            impulse[0] = new(.25f, -.1f);
                            Check(output.All(f => float.IsFinite(f.X) && float.IsFinite(f.Y)), "Boundary processing remains finite.");
                        }
        using var overflow = new AudioEffectLowShelfFilter { Gain = float.MaxValue }; using var overflowing = overflow.Instantiate(); Reject<ArithmeticException>(() => overflowing.Process([new(float.MaxValue, 0)], new Vector2[1])); overflow.Gain = 1; overflowing.Process([new(.2f, -.3f)], output.AsSpan(0, 1)); Check(output[0].DistanceTo(new(.2f, -.3f)) < .00001, "Overflow clears contaminated history and permits standalone recovery.");
    }
    private static void HistoryAndWarm()
    {
        using var filter = new AudioEffectLowPassFilter { DB = Slope.Filter24DB }; using var a = filter.Instantiate(); using var b = filter.Instantiate(); var input = new Vector2[128]; input[0] = new(1, 0); var output = new Vector2[128]; a.Process(input, output); input.AsSpan().Clear(); b.Process(input, output); Check(output.All(f => f == Vector2.Zero), "Factories do not share channel/instance histories."); a.Process(input, output); Check(output.Any(f => f.X != 0) && output.All(f => f.Y == 0), "Active silence processes tails without stereo cross-talk.");
        filter.DB = Slope.Filter6DB; a.Process(input, output); filter.DB = Slope.Filter24DB; a.Process(input, output); Check(output.Any(f => f.X != 0), "Temporarily unused stages retain history across preset edits.");
        input.AsSpan().Fill(new(.1f, -.05f)); for (var i = 0; i < 20; i++) { filter.CutoffHZ = i % 2 == 0 ? 1000 : 3000; a.Process(input, output); }
        var bytes = GC.GetAllocatedBytesForCurrentThread(); for (var i = 0; i < 64; i++) { filter.CutoffHZ = i % 2 == 0 ? 1000 : 3000; a.Process(input, output); _ = filter.Gain; }
        Check(GC.GetAllocatedBytesForCurrentThread() == bytes, "64 live-edit/process/query cycles allocate zero managed bytes.");
        input.AsSpan().Clear(); for (var i = 0; i < 20; i++) a.Process(input, output); bytes = GC.GetAllocatedBytesForCurrentThread(); for (var i = 0; i < 64; i++) a.Process(input, output); Check(GC.GetAllocatedBytesForCurrentThread() == bytes, "64 silent prepared filter blocks allocate zero managed bytes.");
    }
    private static void Concurrency()
    {
        using var filter = new AudioEffectHighShelfFilter(); using var instance = filter.Instantiate(); var frames = new Vector2[64]; frames.AsSpan().Fill(new(.05f, -.1f));
        Parallel.Invoke(() => { for (var i = 0; i < 300; i++) { filter.CutoffHZ = i % 2 == 0 ? 2000 : 3000; filter.Resonance = i % 2 == 0 ? .5f : .8f; filter.DB = (Slope)(i % 4); } }, () => { var output = new Vector2[64]; for (var i = 0; i < 500; i++) instance.Process(frames, output); });
        Check(!filter.IsDisposed, "Resource snapshots and independent processing gates serialize live edits.");
    }
    private static void Native()
    {
        var server = AudioServer.Instance; server.CloseNative(); server.BusCount = 1; using var filter = new AudioEffectLowPassFilter(); using var capture = new AudioEffectCapture { BufferLength = .2f }; using var source = Tone(48000); var root = new Node(); var player = new AudioStreamPlayer { Stream = source }; root.AddChild(player); using var tree = new SceneTree(root);
        try
        {
            server.AddBusEffect(0, filter); server.AddBusEffect(0, capture); player.Play(); var native = server.Native; Wait(native, 20); var filtered = Measure(native); Check(filtered.X > .2f && filtered.Y < .02f, "Actual native filter preserves low tone and attenuates high tone.");
            filter.CutoffHZ = 16000; var open = Measure(native); Check(open.Y > .15f, "Live cutoff updates audible native PCM."); server.SetBusEffectEnabled(0, 0, false); var bypass = Measure(native); Check(bypass.X > .2f && bypass.Y > .2f, "Disabling passes distinct stereo tones."); server.SetBusEffectEnabled(0, 0, true); filter.CutoffHZ = 2000;
            Wait(native, 20); var bytes = native.MixManagedBytes; var calls = FAudioContext.AllocationCalls; Wait(native, 64, () => filter.CutoffHZ = native.MixPasses % 2 == 0 ? 1000 : 3000); Check(native.MixManagedBytes == bytes && FAudioContext.AllocationCalls == calls, "64 warmed native live-filter passes allocate zero managed bytes/custom allocator calls.");
            player.StreamPaused = true; Wait(native, 20); bytes = native.MixManagedBytes; calls = FAudioContext.AllocationCalls; Wait(native, 64); Check(native.MixManagedBytes == bytes && FAudioContext.AllocationCalls == calls, "64 paused native filter passes allocate zero measured bytes/calls.");
            var borrowed = server.GetBusEffectInstance(0, 0); server.RemoveBusEffect(0, 0); Check(borrowed.IsDisposed && !filter.IsDisposed, "Filter instance teardown borrows resource."); server.RemoveBusEffect(0, 0);
            player.StreamPaused = false; var reference = Measure(native);
            foreach (var concrete in Family()) using (concrete)
                {
                    concrete.Gain = 1.5f; concrete.DB = Slope.Filter12DB; server.AddBusEffect(0, concrete); var actual = Measure(native);
                    Check(Math.Abs(actual.X / reference.X - Response(concrete, 20, native.MixRate)) < .03 && Math.Abs(actual.Y / reference.Y - Response(concrete, 8000, native.MixRate)) < .03, $"Native {concrete.GetType().Name}: RMS ratios {actual.X / reference.X}/{actual.Y / reference.Y}, expected {Response(concrete, 20, native.MixRate)}/{Response(concrete, 8000, native.MixRate)} at {native.MixRate} Hz."); server.RemoveBusEffect(0, 0);
                }
        }
        finally { tree.Dispose(); server.CloseNative(); while (server.GetBusEffectCount(0) > 0) server.RemoveBusEffect(0, 0); server.CloseNative(); }
    }
    private static Vector2 Measure(FAudioContext native)
    {
        Wait(native, 12); native.PrepareCapture(native.QuantumFrames * native.Channels * 25); Wait(native, 27); var pcm = native.CapturedPCM(); double left = 0, right = 0; var count = pcm.Length / native.Channels;
        for (var i = 0; i < pcm.Length; i += native.Channels) { left += pcm[i] * pcm[i]; right += pcm[i + 1] * pcm[i + 1]; }
        return new((float)Math.Sqrt(left / count), (float)Math.Sqrt(right / count));
    }
    private static AudioStreamWAV Tone(int rate = 44100)
    {
        var samples = new short[rate * 2]; for (var i = 0; i < rate; i++) { samples[i * 2] = (short)(Math.Sin(Math.Tau * 20 * i / rate) * 10000); samples[i * 2 + 1] = (short)(Math.Sin(Math.Tau * 8000 * i / rate) * 10000); }
        return new AudioStreamWAV { SampleFormat = AudioStreamWAV.Format.PCM16, Stereo = true, MixRate = rate, Data = MemoryMarshal.AsBytes(samples.AsSpan()).ToArray(), Loop = AudioLoopMode.Forward, LoopEnd = rate - 1 };
    }
    private static void Wait(FAudioContext native, int passes, Action? action = null)
    {
        var target = native.MixPasses + passes; var start = System.Diagnostics.Stopwatch.GetTimestamp(); while (native.MixPasses < target) { action?.Invoke(); if (System.Diagnostics.Stopwatch.GetElapsedTime(start).TotalSeconds > 5) throw new InvalidOperationException("Native filter mix deadline."); Thread.Sleep(1); }
    }
    internal static void RunHost()
    {
        var backend = Environment.GetEnvironmentVariable("ELECTRON2D_AUDIO_RENDERER") == "compatibility" ? "compatibility" : "gpu"; var settings = ProjectSettings.Instance; var previous = settings.Get(ProjectSettings.RenderingMethod); settings.Set(ProjectSettings.RenderingMethod, backend); var fps = Engine.Instance.MaxFPS; Engine.Instance.MaxFPS = 60;
        try
        {
            for (var run = 0; run < 3; run++)
            {
                using var source = Tone(); using var filter = new AudioEffectLowPassFilter(); using var capture = new AudioEffectCapture { BufferLength = .1f }; var window = new Window { Size = new(160, 96), Title = "Electron2D stereo filter" }; var player = new AudioStreamPlayer { Stream = source, Autoplay = true }; var scenario = new HostScenario(player, filter, capture, run == 2); window.AddChild(player); window.AddChild(scenario);
                var server = AudioServer.Instance; server.AddBusEffect(0, filter); server.AddBusEffect(0, capture);
                try
                {
                    if (run == 2) { var failed = false; try { Engine.Instance.Run(window); } catch (AggregateException error) { failed = error.Flatten().InnerExceptions.Any(e => e.Message == "Filter host failure fixture."); } Check(failed && window.IsDisposed, "Failed filter host cleanup."); }
                    else Check(Engine.Instance.Run(window) == 0 && scenario.Completed && window.IsDisposed, "Public filter host lifecycle.");
                    Check(scenario.Instance!.IsDisposed && !filter.IsDisposed && !source.IsDisposed, "Host closes instances and borrows resources."); Console.WriteLine(JsonSerializer.Serialize(new { scenario = "audio-filter-host", backend, run, scenario.Frames, leftRMS = scenario.RMS.X, rightRMS = scenario.RMS.Y, cleaned = window.IsDisposed }));
                }
                finally { if (!window.IsDisposed) window.Dispose(); while (server.GetBusEffectCount(0) > 0) server.RemoveBusEffect(0, 0); server.CloseNative(); }
            }
        }
        finally { Engine.Instance.MaxFPS = fps; settings.Set(ProjectSettings.RenderingMethod, previous); }
    }
    private sealed class HostScenario(AudioStreamPlayer player, AudioEffectFilter filter, AudioEffectCapture capture, bool fail) : Node
    {
        internal int Frames; internal bool Completed; internal Vector2 RMS; internal AudioEffectInstance? Instance; private double _elapsed, _left, _right;
        protected override void OnReady() { Instance = AudioServer.Instance.GetBusEffectInstance(0, 0); capture.ClearBuffer(); ProcessEnabled = true; }
        protected override void OnProcess(double delta)
        {
            _elapsed += delta; var available = capture.GetFramesAvailable(); if (available > 0) foreach (var frame in capture.GetBuffer(available)) { Frames++; _left += frame.X * frame.X; _right += frame.Y * frame.Y; }
            if (_elapsed < .3) return; RMS = new((float)Math.Sqrt(_left / Frames), (float)Math.Sqrt(_right / Frames)); Completed = player.IsPlaying() && Frames > 0 && RMS.X > .15f && RMS.Y < .03f; Check(Completed && filter.CutoffHZ == 2000, "Public filtered PCM reaches raw capture."); if (fail) throw new ApplicationException("Filter host failure fixture."); Tree!.Quit();
        }
    }
    private sealed class Holder : Node
    {
        public Holder() { }
        protected override Func<Node> CreateSceneInstanceFactory() => Create;
        private static Node Create() => new Holder();
        public AudioEffectFilter? Filter { get; set; }
        private static readonly PropertyDescriptor[] Properties = [new PropertyDescriptor<Holder, AudioEffectFilter?>(nameof(Filter), p => p.Filter, (p, v) => p.Filter = v, _ => null, stored: true)];
        protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors() => base.GetPropertyDescriptors().Concat(Properties);
    }
    private static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    private static void Reject<T>(Action action) where T : Exception { try { action(); } catch (T) { return; } throw new InvalidOperationException($"Expected {typeof(T).Name}."); }
}
