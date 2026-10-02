using Electron2D;

internal static class AudioEffectLevelTests
{
    internal static void Run(bool native = false)
    {
        ResourcesAndPCM();
        if (native) Native();
        Console.WriteLine("Audio level effects: resource copies, live block ramp, stereo pan, native bus PCM and warmed allocation passed.");
    }

    private static void ResourcesAndPCM()
    {
        using var gain = new AudioEffectAmplify(); using var pan = new AudioEffectPanner();
        Check(gain.VolumeDB == 0 && gain.VolumeLinear == 1 && pan.Pan == 0, "Defaults.");
        Check(gain.GetPropertyList().Any(p => p.Name == nameof(AudioEffectAmplify.VolumeDB) && !p.IsReadOnly) &&
              pan.GetPropertyList().Any(p => p.Name == nameof(AudioEffectPanner.Pan) && !p.IsReadOnly), "Typed authoring properties.");
        foreach (var bad in new[] { float.NaN, float.PositiveInfinity }) Reject<ArgumentOutOfRangeException>(() => gain.VolumeDB = bad);
        foreach (var bad in new[] { -1f, float.NaN, float.PositiveInfinity }) Reject<ArgumentOutOfRangeException>(() => gain.VolumeLinear = bad);
        foreach (var bad in new[] { float.NaN, float.PositiveInfinity, float.NegativeInfinity }) Reject<ArgumentOutOfRangeException>(() => pan.Pan = bad);
        using var amp = gain.Instantiate(); using var independent = gain.Instantiate();
        var input = new Vector2[4]; input.AsSpan().Fill(new(1, 0)); var output = new Vector2[4];
        gain.VolumeLinear = 2; amp.Process(input, output);
        for (var i = 0; i < 4; i++) Check(Math.Abs(output[i].X - (1 + i * .25f)) < .00001f, "Live gain ramps across the next block.");
        amp.Process(input, output); Check(output.All(f => Math.Abs(f.X - 2) < .00001f), "Steady gain after ramp.");
        independent.Process(input, output); Check(Math.Abs(output[0].X - 1) < .00001f && Math.Abs(output[3].X - 1.75f) < .00001f, "Instances retain independent prior gain.");
        gain.VolumeLinear = 0; Check(gain.VolumeDB == float.NegativeInfinity && gain.VolumeLinear == 0, "Zero linear gain is silence.");
        amp.Process(input, output); Check(Math.Abs(output[0].X - 2) < .00001f && Math.Abs(output[3].X - .5f) < .00001f, "Silence ramp.");
        amp.Process(input, output); Check(output.All(f => f == Vector2.Zero), "Silent steady block.");
        gain.VolumeDB = 6.0206f; amp.Process(input, output); Check(output[0] == Vector2.Zero, "Gain resumes from silence.");
        using var gainCopy = (AudioEffectAmplify)gain.Duplicate(true); Check(Math.Abs(gainCopy.VolumeLinear - 2) < .0001f, "Gain resource copy.");
        Action<Resource> fail = _ => throw new ApplicationException("Effect observer fixture."); gain.Changed += fail;
        Reject<ApplicationException>(() => gain.VolumeDB = 3); Check(gain.VolumeDB == 3, "Observer failure follows committed gain."); gain.Changed -= fail;
        pan.Pan = -1; using var panner = pan.Instantiate(); var frame = new[] { new Vector2(.2f, -.3f) }; panner.Process(frame, frame);
        Check(frame[0].DistanceTo(new(-.1f, 0)) < .00001f, "Full left pan folds both inputs.");
        pan.Pan = 1; panner.Process(frame, frame); Check(frame[0].DistanceTo(new(0, -.1f)) < .00001f, "Live right pan and aliasing.");
        pan.Pan = 3; using var panCopy = (AudioEffectPanner)pan.Duplicate(true); Check(panCopy.Pan == 3, "Out-of-range raw pan and copy.");
        gain.ResourceLocalToScene = true; pan.ResourceLocalToScene = true;
        using var holder = new Holder { Gain = gain, Pan = pan }; using var scene = new PackedScene(); scene.Pack(holder);
        using var restored = (Holder)scene.Instantiate();
        Check(restored.Gain is not null && restored.Pan is not null && !ReferenceEquals(restored.Gain, gain) && !ReferenceEquals(restored.Pan, pan) && restored.Gain.VolumeDB == 3 && restored.Pan.Pan == 3, "Scene-local concrete effects retain scalar configuration.");
        restored.Gain!.Dispose(); restored.Pan!.Dispose();
        frame[0] = new(.2f, -.3f); panner.Process(frame, frame); Check(frame[0].DistanceTo(new(0, -.1f)) < .00001f, "Processing clamps raw pan.");
        var block = new Vector2[128]; block.AsSpan().Fill(new(.1f, -.2f)); var destination = new Vector2[128];
        for (var i = 0; i < 20; i++) { gain.VolumeLinear = i % 2 == 0 ? .5f : 2; pan.Pan = i % 2 == 0 ? -.5f : .5f; amp.Process(block, destination); panner.Process(destination, destination); }
        var bytes = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < 64; i++) { gain.VolumeLinear = i % 2 == 0 ? .5f : 2; pan.Pan = i % 2 == 0 ? -.5f : .5f; amp.Process(block, destination); panner.Process(destination, destination); }
        Check(GC.GetAllocatedBytesForCurrentThread() == bytes, "64 warmed live-edit stereo blocks allocate no managed bytes.");
        gain.Dispose(); pan.Dispose(); Reject<ObjectDisposedException>(() => amp.Process(block, destination)); Reject<ObjectDisposedException>(() => panner.Process(block, destination));
    }

    private static void Native()
    {
        var server = AudioServer.Instance; server.CloseNative(); server.BusCount = 1;
        using var gain = new AudioEffectAmplify { VolumeLinear = 2 }; using var pan = new AudioEffectPanner();
        using var stream = AudioEffectTests.Constant(); var root = new Node(); var player = new AudioStreamPlayer { Stream = stream }; root.AddChild(player); using var tree = new SceneTree(root);
        try
        {
            server.AddBusEffect(0, gain); server.AddBusEffect(0, pan); player.Play(); var native = server.Native;
            AudioEffectTests.Wait(native, 20); AudioEffectTests.CheckOutput(native, new(.4f, -.6f));
            pan.Pan = -1; AudioEffectTests.CheckOutput(native, new(-.2f, 0));
            server.SetBusEffectEnabled(0, 1, false); AudioEffectTests.CheckOutput(native, new(.4f, -.6f));
            server.SetBusEffectEnabled(0, 1, true); AudioEffectTests.Wait(native, 20);
            var bytes = native.MixManagedBytes; var allocations = FAudioContext.AllocationCalls;
            AudioEffectTests.Wait(native, 64);
            Check(native.MixManagedBytes == bytes && FAudioContext.AllocationCalls == allocations, "64 active native passes allocate no measured bytes or custom FAudio calls.");
            player.StreamPaused = true; AudioEffectTests.Wait(native, 20); bytes = native.MixManagedBytes; allocations = FAudioContext.AllocationCalls;
            AudioEffectTests.Wait(native, 64); Check(native.MixManagedBytes == bytes && FAudioContext.AllocationCalls == allocations, "64 paused native passes allocate no measured bytes or custom FAudio calls.");
        }
        finally { tree.Dispose(); server.CloseNative(); while (server.GetBusEffectCount(0) > 0) server.RemoveBusEffect(0, 0); server.CloseNative(); }
    }

    internal static void RunHost()
    {
        var settings = ProjectSettings.Instance;
        var renderer = Environment.GetEnvironmentVariable("ELECTRON2D_AUDIO_RENDERER") == "compatibility" ? "compatibility" : "gpu";
        var previous = settings.Get(ProjectSettings.RenderingMethod);
        settings.Set(ProjectSettings.RenderingMethod, renderer);
        try
        {
            using var stream = AudioEffectTests.Constant(); using var gain = new AudioEffectAmplify { VolumeLinear = 2 };
            using var pan = new AudioEffectPanner { Pan = -1 }; using var capture = new AudioEffectCapture { BufferLength = .1f };
            var window = new Window { Size = new(160, 96), Title = "Electron2D bus level" };
            var player = new AudioStreamPlayer { Stream = stream, Autoplay = true }; var scenario = new HostScenario(capture);
            window.AddChild(player); window.AddChild(scenario);
            var server = AudioServer.Instance; server.AddBusEffect(0, gain); server.AddBusEffect(0, pan); server.AddBusEffect(0, capture);
            try
            {
                Check(Engine.Instance.Run(window) == 0 && scenario.Completed && window.IsDisposed, "Public native level host and cleanup.");
                Console.WriteLine($"Audio level host: {renderer}, {scenario.Frames} captured processed frames, cleanup passed.");
            }
            finally { if (!window.IsDisposed) window.Dispose(); while (server.GetBusEffectCount(0) > 0) server.RemoveBusEffect(0, 0); server.CloseNative(); }
        }
        finally { settings.Set(ProjectSettings.RenderingMethod, previous); }
    }

    private sealed class HostScenario(AudioEffectCapture capture) : Node
    {
        internal int Frames; internal bool Completed;
        private double _elapsed;
        protected override void OnReady() { capture.ClearBuffer(); ProcessEnabled = true; }
        protected override void OnProcess(double delta)
        {
            _elapsed += delta;
            var available = capture.GetFramesAvailable();
            if (available > 0)
            {
                var pcm = capture.GetBuffer(available);
                Frames += pcm.Length;
                Completed |= pcm.Any(frame => frame.DistanceTo(new(-.2f, 0)) < .0001f);
            }
            if (_elapsed < .3) return;
            Check(Completed && Frames > 0, "Public host captured amplified/panned PCM.");
            Tree!.Quit();
        }
    }

    private static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    private static void Reject<T>(Action action) where T : Exception { try { action(); } catch (T) { return; } throw new InvalidOperationException($"Expected {typeof(T).Name}."); }
    private sealed class Holder : Node
    {
        protected override Func<Node> CreateSceneInstanceFactory() => Create;
        private static Node Create() => new Holder();
        public AudioEffectAmplify? Gain { get; set; }
        public AudioEffectPanner? Pan { get; set; }
        private static readonly PropertyDescriptor[] Properties =
        [
            new PropertyDescriptor<Holder, AudioEffectAmplify?>(nameof(Gain), p => p.Gain, (p, v) => p.Gain = v, _ => null, stored: true),
            new PropertyDescriptor<Holder, AudioEffectPanner?>(nameof(Pan), p => p.Pan, (p, v) => p.Pan = v, _ => null, stored: true)
        ];
        protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors() => base.GetPropertyDescriptors().Concat(Properties);
    }
}
