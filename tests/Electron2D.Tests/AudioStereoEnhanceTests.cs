using Electron2D;
using System.Runtime.InteropServices;

internal static class AudioStereoEnhanceTests
{
    internal static void Run(bool native = false)
    {
        ResourceState(); StereoPCM(); DelayAndEdges(); Warm();
        if (native) { Native(); NativeTail(); }
        Console.WriteLine(native ? "Stereo enhance: typed state, PCM, native output/tail and allocation passed."
            : "Stereo enhance: typed state, PCM and CPU allocation passed.");
    }

    private static void ResourceState()
    {
        using var effect = new AudioEffectStereoEnhance();
        Check(effect.PanPullout == 1 && effect.TimePulloutMS == 0 && effect.Surround == 0, "Stereo enhancement defaults.");
        Check(effect.GetPropertyList().Count(p => p.IsStored && p.Name is "PanPullout" or "TimePulloutMS" or "Surround") == 3,
            "Three typed stored controls.");
        Reject<ArgumentOutOfRangeException>(() => effect.PanPullout = -.01f);
        Reject<ArgumentOutOfRangeException>(() => effect.PanPullout = 4.01f);
        Reject<ArgumentOutOfRangeException>(() => effect.PanPullout = float.NaN);
        Reject<ArgumentOutOfRangeException>(() => effect.TimePulloutMS = -.01f);
        Reject<ArgumentOutOfRangeException>(() => effect.TimePulloutMS = 50.01f);
        Reject<ArgumentOutOfRangeException>(() => effect.TimePulloutMS = float.PositiveInfinity);
        Reject<ArgumentOutOfRangeException>(() => effect.Surround = -.01f);
        Reject<ArgumentOutOfRangeException>(() => effect.Surround = 1.01f);
        Reject<ArgumentOutOfRangeException>(() => effect.Surround = float.NegativeInfinity);
        Check(effect.PanPullout == 1 && effect.TimePulloutMS == 0 && effect.Surround == 0, "Rejected edits leave state unchanged.");
        effect.PanPullout = 4; effect.TimePulloutMS = 50; effect.Surround = 1;
        using var copy = (AudioEffectStereoEnhance)effect.Duplicate(true);
        Check(copy.PanPullout == 4 && copy.TimePulloutMS == 50 && copy.Surround == 1,
            "Resource copy retains all stereo controls.");
        effect.ResourceLocalToScene = true; using var holder = new Holder { Effect = effect }; using var packed = new PackedScene(); packed.Pack(holder);
        using var restored = (Holder)packed.Instantiate();
        Check(restored.Effect is AudioEffectStereoEnhance sceneCopy && !ReferenceEquals(sceneCopy, effect) &&
              sceneCopy.PanPullout == 4 && sceneCopy.TimePulloutMS == 50 && sceneCopy.Surround == 1,
            "Scene-local copy retains configuration without instance history.");
        restored.Effect!.Dispose();
        Action<Resource> fail = _ => throw new ApplicationException("Stereo observer fixture."); effect.Changed += fail;
        Reject<ApplicationException>(() => effect.PanPullout = 2); Check(effect.PanPullout == 2, "Observer failure follows committed edit."); effect.Changed -= fail;
        using var instance = effect.Instantiate(); Check(instance.ProcessSilence(), "Delayed samples continue on silent blocks.");
        effect.Dispose(); Reject<ObjectDisposedException>(() => instance.Process([Vector2.Zero], new Vector2[1]));
    }

    private static void StereoPCM()
    {
        using var effect = new AudioEffectStereoEnhance(); using var processor = effect.Instantiate();
        var result = new Vector2[1]; var input = new Vector2(.2f, -.3f);
        processor.Process([input], result); Check(result[0] == input, "Neutral default passes finite PCM bit-identically.");
        effect.PanPullout = 0; processor.Process([input], result);
        Near(result[0], new(-.05f, -.05f), "Zero width downmixes to the common center.");
        effect.PanPullout = 4; processor.Process([input], result);
        Near(result[0], new(.95f, -1.05f), "Maximum side gain widens both polarities.");
        effect.PanPullout = 1; effect.Surround = 1; processor.Process([input], result);
        Near(result[0], new(.15f, -.25f), "Zero-delay surround adds center to left and subtracts it from right.");
        processor.Process([new(.4f, .4f)], result);
        Near(result[0], new(.8f, 0), "Zero-delay surround pans mono input fully left.");
        effect.Surround = 0; processor.Process([input], result);
        Check(result[0] == input, "Live surround disable restores the direct right channel.");
    }

    private static void DelayAndEdges()
    {
        var rate = AudioServer.Instance.GetMixRate();
        var delay = (int)(50 / 1000.0 * rate);
        using var effect = new AudioEffectStereoEnhance { TimePulloutMS = 50 };
        using var split = effect.Instantiate(); using var alias = effect.Instantiate();
        var input = new Vector2[delay * 3 + 17]; input[0] = new(.2f, -.3f); input[delay + 7] = new(-.4f, .6f);
        var output = new Vector2[input.Length]; split.Process(input.AsSpan(0, 37), output.AsSpan(0, 37));
        split.Process(input.AsSpan(37, delay + 3), output.AsSpan(37, delay + 3));
        split.Process(input.AsSpan(delay + 40), output.AsSpan(delay + 40));
        var inPlace = (Vector2[])input.Clone(); alias.Process(inPlace, inPlace);
        Check(inPlace.SequenceEqual(output), "Aliased and split blocks preserve delay history.");
        Near(output[0], new(.2f, 0), "A delayed right impulse starts silent.");
        Near(output[delay], new(0, -.3f), "Maximum-delay right impulse arrives at the exact output frame.");
        Near(output[delay + 7], new(-.4f, 0), "Second left impulse is immediate.");
        Near(output[delay * 2 + 7], new(0, .6f), "Wrapped ring preserves a second right impulse.");
        effect.Surround = 1;
        using var surrounded = effect.Instantiate(); var impulse = new Vector2[delay + 1]; impulse[0] = new(.4f, .4f);
        var stereo = new Vector2[impulse.Length]; surrounded.Process(impulse, stereo);
        Near(stereo[0], new(.4f, .4f), "Delayed surround center does not affect the initial frame.");
        Near(stereo[delay], new(.4f, -.4f), "Delayed center contributes with opposite channel polarity.");
        effect.Surround = 0; using var live = effect.Instantiate(); var one = new Vector2[1];
        live.Process([new(.3f, .7f)], one); Near(one[0], new(.3f, 0), "Initial right sample enters the delay ring.");
        effect.TimePulloutMS = 0; live.Process([new(.2f, .4f)], one);
        Near(one[0], new(.2f, .4f), "Live zero-delay edit reads the current right sample.");
        effect.TimePulloutMS = 50; var history = new Vector2[delay - 1];
        live.Process(new Vector2[history.Length], history);
        Near(history[^1], new(0, .7f), "Live delay edits retain earlier ring history.");
        effect.Surround = 1; using var switched = effect.Instantiate();
        switched.Process([new(.1f, .3f)], one);
        effect.Surround = 0; var transition = new Vector2[delay];
        switched.Process(new Vector2[delay], transition);
        Near(transition[^1], new(0, .2f), "Mode switch retains the shared center/right ring history.");
        using var unsafeEffect = new AudioEffectStereoEnhance { Surround = 1 };
        using var unsafeInstance = unsafeEffect.Instantiate();
        Reject<ArithmeticException>(() => unsafeInstance.Process([new(float.MaxValue, float.MaxValue)], new Vector2[1]));
        unsafeEffect.Surround = 0; var recovered = new Vector2[1]; unsafeInstance.Process([new(.1f, -.2f)], recovered);
        Near(recovered[0], new(.1f, -.2f), "Overflow clears delayed history before recovery.");
        using var max = new AudioEffectStereoEnhance { PanPullout = 0 }; using var maxInstance = max.Instantiate();
        maxInstance.Process([new(float.MaxValue, float.MaxValue)], recovered);
        Check(recovered[0] == new Vector2(float.MaxValue, float.MaxValue), "Finite center math accepts maximum finite equal channels.");
    }

    private static void Warm()
    {
        using var effect = new AudioEffectStereoEnhance { PanPullout = 1.5f, TimePulloutMS = 12, Surround = .25f };
        using var instance = effect.Instantiate(); var input = new Vector2[128]; input.AsSpan().Fill(new(.2f, -.3f)); var result = new Vector2[128];
        for (var i = 0; i < 16; i++) instance.Process(input, result);
        var bytes = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < 64; i++) instance.Process(input, result);
        Check(GC.GetAllocatedBytesForCurrentThread() == bytes, "64 warmed active blocks allocate zero managed bytes.");
        var empty = Array.Empty<Vector2>(); bytes = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < 64; i++) instance.Process(empty, empty);
        Check(GC.GetAllocatedBytesForCurrentThread() == bytes, "64 warmed empty blocks allocate zero managed bytes.");
        input.AsSpan().Clear(); bytes = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < 64; i++) instance.Process(input, result);
        Check(GC.GetAllocatedBytesForCurrentThread() == bytes, "64 warmed silent-tail blocks allocate zero managed bytes.");
    }

    private static void Native()
    {
        var server = AudioServer.Instance; server.CloseNative(); server.BusCount = 1;
        using var effect = new AudioEffectStereoEnhance { PanPullout = 0 };
        using var stream = AudioEffectTests.Constant(); var root = new Node(); var player = new AudioStreamPlayer { Stream = stream }; root.AddChild(player); using var tree = new SceneTree(root);
        try
        {
            server.AddBusEffect(0, effect); var borrowed = server.GetBusEffectInstance(0, 0); player.Play(); var native = server.Native;
            AudioEffectTests.CheckOutput(native, new(-.05f, -.05f));
            effect.PanPullout = 1; effect.Surround = 1; AudioEffectTests.CheckOutput(native, new(.15f, -.25f));
            if (native.Channels > 2)
            {
                var pcm = native.CapturedPCM();
                for (var frame = 0; frame < pcm.Length; frame += native.Channels)
                    for (var channel = 2; channel < native.Channels; channel++)
                        Check(Math.Abs(pcm[frame + channel]) < .0001f, "Unrelated output pairs remain silent.");
            }
            server.SetBusEffectEnabled(0, 0, false); AudioEffectTests.CheckOutput(native, new(.2f, -.3f));
            server.SetBusEffectEnabled(0, 0, true); AudioEffectTests.Wait(native, 20);
            var bytes = native.MixManagedBytes; var calls = FAudioContext.AllocationCalls; AudioEffectTests.Wait(native, 64);
            Check(native.MixManagedBytes == bytes && FAudioContext.AllocationCalls == calls, "64 warmed native active passes allocate no measured bytes or calls.");
            player.StreamPaused = true; AudioEffectTests.Wait(native, 20); bytes = native.MixManagedBytes; calls = FAudioContext.AllocationCalls;
            AudioEffectTests.Wait(native, 64);
            Check(native.MixManagedBytes == bytes && FAudioContext.AllocationCalls == calls, "64 warmed native paused passes allocate no measured bytes or calls.");
            server.RemoveBusEffect(0, 0); Check(borrowed.IsDisposed && !effect.IsDisposed, "Removal releases the borrowed instance only.");
        }
        finally { tree.Dispose(); server.CloseNative(); while (server.GetBusEffectCount(0) > 0) server.RemoveBusEffect(0, 0); server.CloseNative(); }
    }

    private static void NativeTail()
    {
        var server = AudioServer.Instance; server.CloseNative(); server.BusCount = 1;
        using var effect = new AudioEffectStereoEnhance { TimePulloutMS = 20 };
        var samples = new short[512]; samples[0] = 16384; samples[1] = 8192;
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
            for (var frame = 400; frame < pcm.Length / native.Channels; frame++)
                found |= Math.Abs(pcm[frame * native.Channels + 1]) > .01f && Math.Abs(pcm[frame * native.Channels]) < .01f;
            Check(found, "Finite source produces a delayed right-channel tail on the native bus.");
            tree.ProcessFrame(.01); Check(!player.IsPlaying(), "Finite source stops before its effect tail.");
        }
        finally { tree.Dispose(); server.CloseNative(); while (server.GetBusEffectCount(0) > 0) server.RemoveBusEffect(0, 0); server.CloseNative(); }
    }

    internal static void RunHost()
    {
        var settings = ProjectSettings.Instance; var previous = settings.Get(ProjectSettings.RenderingMethod);
        settings.Set(ProjectSettings.RenderingMethod, Environment.GetEnvironmentVariable("ELECTRON2D_AUDIO_RENDERER") == "compatibility" ? "compatibility" : "gpu");
        try
        {
            using var effect = new AudioEffectStereoEnhance { PanPullout = 0 };
            using var stream = AudioEffectTests.Constant(); using var capture = new AudioEffectCapture { BufferLength = .1f };
            var window = new Window { Size = new(160, 96) }; window.AddChild(new AudioStreamPlayer { Stream = stream, Autoplay = true });
            var scenario = new HostScenario(capture); window.AddChild(scenario);
            var server = AudioServer.Instance; server.AddBusEffect(0, effect); server.AddBusEffect(0, capture);
            try { Check(Engine.Instance.Run(window) == 0 && scenario.Completed && window.IsDisposed, "Public stereo host processed and cleaned up."); }
            finally { if (!window.IsDisposed) window.Dispose(); while (server.GetBusEffectCount(0) > 0) server.RemoveBusEffect(0, 0); server.CloseNative(); }
        }
        finally { settings.Set(ProjectSettings.RenderingMethod, previous); }
    }
    private sealed class HostScenario(AudioEffectCapture capture) : Node
    {
        internal bool Completed; private double _elapsed;
        protected override void OnReady() { ProcessEnabled = true; }
        protected override void OnProcess(double delta)
        {
            _elapsed += delta; var available = capture.GetFramesAvailable();
            if (available > 0) Completed |= capture.GetBuffer(available).Any(frame => frame.DistanceTo(new(-.05f, -.05f)) < .0001f);
            if (_elapsed < .25) return;
            Check(Completed, "Public Window host captured mono center from the stereo bus."); Tree!.Quit();
        }
    }
    private sealed class Holder : Node
    {
        public AudioEffectStereoEnhance? Effect { get; set; }
        private static readonly PropertyDescriptor[] Properties = [new PropertyDescriptor<Holder, AudioEffectStereoEnhance?>(nameof(Effect), p => p.Effect, (p, v) => p.Effect = v, _ => null, stored: true)];
        protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors() => base.GetPropertyDescriptors().Concat(Properties);
        protected override Func<Node> CreateSceneInstanceFactory() => Create;
        private static Node Create() => new Holder();
    }
    private static void Near(Vector2 actual, Vector2 expected, string message) => Check(actual.DistanceTo(expected) < .00001f, message);
    private static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    private static void Reject<T>(Action action) where T : Exception { try { action(); } catch (T) { return; } throw new InvalidOperationException($"Expected {typeof(T).Name}."); }
}
