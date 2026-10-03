using Electron2D;

internal static class AudioRecordTests
{
    internal static void Run(bool native = false)
    {
        ResourceAndFormats(); RestartAndCopies();
        if (native) Native();
        Console.WriteLine(native ? "Audio record: formats, restart, native PCM and allocation passed." : "Audio record: formats and restart passed.");
    }

    private static void ResourceAndFormats()
    {
        using var record = new AudioEffectRecord();
        Check(record.Format == AudioStreamWAV.Format.PCM16 && !record.IsRecordingActive() && record.GetRecording() is null, "Record defaults and absent sample.");
        Check(record.GetPropertyList().Count(p => p.IsStored && p.Name == "Format") == 1, "Stored typed format.");
        Reject<ArgumentOutOfRangeException>(() => record.Format = (AudioStreamWAV.Format)(-1));
        Reject<ArgumentOutOfRangeException>(() => record.Format = (AudioStreamWAV.Format)4);
        Reject<InvalidOperationException>(() => record.SetRecordingActive(true));
        using var instance = record.Instantiate();
        Check(instance.ProcessSilence(), "Record accepts silent bus blocks.");
        var frames = new Vector2[1024]; frames.AsSpan().Fill(new(.2f, -.3f));
        var output = new Vector2[frames.Length];
        foreach (var format in Enum.GetValues<AudioStreamWAV.Format>())
        {
            record.Format = format; record.SetRecordingActive(true);
            instance.Process(frames, output); Check(output.SequenceEqual(frames), "Record passes PCM through unchanged.");
            using (var live = record.GetRecording()) Check(live is not null && live.Stereo && live.SampleFormat == format && live.MixRate == (int)AudioServer.Instance.GetMixRate() && live.Loop == AudioLoopMode.Disabled,
                $"Active {format} snapshot owns correct metadata.");
            record.SetRecordingActive(false);
            using var result = record.GetRecording();
            Check(result is not null && !record.IsRecordingActive() && result.SampleFormat == format && result.Data.Length > 0, $"Stopped {format} sample is encoded.");
            var decoded = result!.PreparePCM().Samples;
            Check(decoded.Length >= frames.Length * 2 && decoded[512] is > .1f and < .3f && decoded[513] is < -.2f and > -.4f,
                $"{format} roundtrip has distinct stereo PCM.");
        }
        record.SetRecordingActive(true);
        Check(record.GetRecording() is null, "Restart removes prior sample.");
        record.SetRecordingActive(false);
        var alias = (Vector2[])frames.Clone(); record.SetRecordingActive(true); instance.Process(alias, alias); record.SetRecordingActive(false);
        Check(alias.SequenceEqual(frames), "Aliased processing passes PCM through.");
        var tooLarge = new Vector2[1 << 18]; tooLarge.AsSpan().Fill(new(.2f, -.3f));
        record.SetRecordingActive(true); instance.Process(tooLarge, tooLarge);
        Check(!record.IsRecordingActive(), "Whole-block ring overflow stops recording without changing output.");
        Reject<InvalidOperationException>(() => record.GetRecording());
        record.SetRecordingActive(true); instance.Process(frames, output); record.SetRecordingActive(false);
        using (var recovered = record.GetRecording()) Check(recovered is not null && recovered.GetLength() > 0, "Restart clears overflow failure.");
        record.Dispose(); Reject<ObjectDisposedException>(() => record.GetRecording());
        Reject<ObjectDisposedException>(() => instance.Process(frames, output));
    }

    private static void RestartAndCopies()
    {
        using var record = new AudioEffectRecord { Format = AudioStreamWAV.Format.QOA };
        using var first = record.Instantiate();
        record.SetRecordingActive(true);
        var frames = new Vector2[512]; frames.AsSpan().Fill(new(.25f, -.25f)); first.Process(frames, frames);
        using var second = record.Instantiate();
        Check(record.IsRecordingActive(), "Fresh instance inherits active recording state.");
        second.Process(frames, frames); record.SetRecordingActive(false);
        using var sample = record.GetRecording(); Check(sample is not null && sample.GetLength() > 0, "New instance publishes its sample.");
        using var copy = (AudioEffectRecord)record.Duplicate(true);
        Check(copy.Format == AudioStreamWAV.Format.QOA && copy.GetRecording() is null && !copy.IsRecordingActive(), "Copy keeps format but no capture state.");
        record.ResourceLocalToScene = true; using var holder = new Holder { Effect = record }; using var packed = new PackedScene(); packed.Pack(holder);
        using var restored = (Holder)packed.Instantiate();
        Check(restored.Effect is AudioEffectRecord sceneCopy && sceneCopy.Format == record.Format && sceneCopy.GetRecording() is null,
            "Scene-local copy keeps only configuration.");
        restored.Effect!.Dispose();
        Action<Resource> fail = _ => throw new ApplicationException("Record observer fixture."); record.Changed += fail;
        Reject<ApplicationException>(() => record.Format = AudioStreamWAV.Format.PCM8);
        Check(record.Format == AudioStreamWAV.Format.PCM8, "Observer error follows committed format edit."); record.Changed -= fail;
    }

    private static void Native()
    {
        var server = AudioServer.Instance; server.CloseNative(); server.BusCount = 1;
        using var record = new AudioEffectRecord(); using var stream = AudioEffectTests.Constant();
        var root = new Node(); var player = new AudioStreamPlayer { Stream = stream }; root.AddChild(player); using var tree = new SceneTree(root);
        try
        {
            server.AddBusEffect(0, record); player.Play(); var native = server.Native; AudioEffectTests.Wait(native, 8);
            var borrowed = server.GetBusEffectInstance(0, 0);
            record.SetRecordingActive(true); AudioEffectTests.Wait(native, 30);
            Check(record.IsRecordingActive(), "Native bus record is active.");
            using (var failed = new FailingEffect())
            {
                Reject<ApplicationException>(() => server.AddBusEffect(0, failed));
                Check(record.IsRecordingActive() && ReferenceEquals(server.GetBusEffectInstance(0, 0), borrowed),
                    "Failed chain preparation preserves the active front-pair recorder.");
            }
            using (var live = record.GetRecording()) Check(live is not null && live.GetLength() > 0, "Active native snapshot contains PCM.");
            AudioEffectTests.CheckOutput(native, new(.2f, -.3f));
            record.SetRecordingActive(false);
            using (var result = record.GetRecording())
            {
                Check(result is not null && result.GetLength() > .05, "Stopped native sample has time.");
                var pcm = result!.PreparePCM().Samples;
                Check(pcm.Length > 1000 && pcm[0] is > .19f and < .21f && pcm[1] is < -.29f and > -.31f, "Front stereo pair recorded distinct live channels.");
            }
            if (native.Channels > 2)
                for (var pair = 1; pair < native.Channels / 2; pair++)
                    Check(!ReferenceEquals(server.GetBusEffectInstance(0, 0, pair), borrowed), "Each pair keeps an independent borrowed instance.");
            record.SetRecordingActive(true); AudioEffectTests.Wait(native, 8);
            var bytes = native.MixManagedBytes; var calls = FAudioContext.AllocationCalls; AudioEffectTests.Wait(native, 64);
            Check(native.MixManagedBytes == bytes && FAudioContext.AllocationCalls == calls, "64 warmed native recording passes allocate no measured callback bytes/calls.");
            player.StreamPaused = true; AudioEffectTests.Wait(native, 8); bytes = native.MixManagedBytes; calls = FAudioContext.AllocationCalls;
            AudioEffectTests.Wait(native, 64);
            Check(native.MixManagedBytes == bytes && FAudioContext.AllocationCalls == calls, "64 warmed silent recording passes allocate no measured callback bytes/calls.");
            record.SetRecordingActive(false);
            server.RemoveBusEffect(0, 0); Check(borrowed.IsDisposed && record.GetRecording() is null, "Removing bus instance invalidates current sample.");
        }
        finally { tree.Dispose(); server.CloseNative(); while (server.GetBusEffectCount(0) > 0) server.RemoveBusEffect(0, 0); server.CloseNative(); }
    }

    internal static void RunHost()
    {
        var settings = ProjectSettings.Instance; var previous = settings.Get(ProjectSettings.RenderingMethod);
        settings.Set(ProjectSettings.RenderingMethod, Environment.GetEnvironmentVariable("ELECTRON2D_AUDIO_RENDERER") == "compatibility" ? "compatibility" : "gpu");
        try
        {
            using var record = new AudioEffectRecord(); using var stream = AudioEffectTests.Constant();
            var window = new Window { Size = new(160, 96) }; window.AddChild(new AudioStreamPlayer { Stream = stream, Autoplay = true });
            var scenario = new HostScenario(record); window.AddChild(scenario); var server = AudioServer.Instance; server.AddBusEffect(0, record);
            try { Check(Engine.Instance.Run(window) == 0 && scenario.Completed && window.IsDisposed, "Public record host queried and cleaned up."); }
            finally { if (!window.IsDisposed) window.Dispose(); while (server.GetBusEffectCount(0) > 0) server.RemoveBusEffect(0, 0); server.CloseNative(); }
        }
        finally { settings.Set(ProjectSettings.RenderingMethod, previous); }
    }
    private sealed class HostScenario(AudioEffectRecord record) : Node
    {
        internal bool Completed; private double _elapsed;
        protected override void OnReady() { record.SetRecordingActive(true); ProcessEnabled = true; }
        protected override void OnProcess(double delta)
        {
            _elapsed += delta; if (_elapsed < .3) return;
            record.SetRecordingActive(false); using var sample = record.GetRecording();
            Completed = sample is not null && sample.GetLength() > .05 && sample.PreparePCM().Samples.Any(v => Math.Abs(v) > .1f);
            Check(Completed, "Public Window host recorded nonzero PCM."); Tree!.Quit();
        }
    }
    private sealed class Holder : Node
    {
        public AudioEffectRecord? Effect { get; set; }
        private static readonly PropertyDescriptor[] Properties = [new PropertyDescriptor<Holder, AudioEffectRecord?>(nameof(Effect), p => p.Effect, (p, v) => p.Effect = v, _ => null, stored: true)];
        protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors() => base.GetPropertyDescriptors().Concat(Properties);
        protected override Func<Node> CreateSceneInstanceFactory() => Create;
        private static Node Create() => new Holder();
    }
    private sealed class FailingEffect : AudioEffect
    {
        protected override AudioEffectInstance OnInstantiate() => throw new ApplicationException("Record chain failure fixture.");
        protected override Resource CreateDuplicateInstance() => new FailingEffect();
    }
    private static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    private static void Reject<T>(Action action) where T : Exception { try { action(); } catch (T) { return; } throw new InvalidOperationException($"Expected {typeof(T).Name}."); }
}
