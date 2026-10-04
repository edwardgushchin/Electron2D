using Electron2D;
using System.Text.Json;

internal static class AudioOutputTests
{
    internal static void Run()
    {
        var server = AudioServer.Service; server.CloseNative(); AudioServer.BusCount = 1;
        Check(AudioServer.OutputDevice == "Default", "Default output selector."); Reject<ArgumentNullException>(() => AudioServer.OutputDevice = null!);
        using var stream = AudioEffectTests.Constant(); using var effect = new AudioEffectAmplify { VolumeDB = -6 };
        var root = new Node(); var player = new AudioStreamPlayer { Stream = stream }; root.AddChild(player); using var tree = new SceneTree(root);
        try
        {
            AudioServer.AddBusEffect(0, effect); player.Play(); var native = server.Native; AudioEffectTests.Wait(native, 20);
            var playback = player.GetStreamPlayback(); var instance = AudioServer.GetBusEffectInstance(0, 0); var names = AudioServer.GetOutputDeviceList();
            var selected = names.FirstOrDefault(n => n != "Default") ?? throw new InvalidOperationException("Output device enumeration lacks a physical selector.");
            var rate = AudioServer.GetMixRate(); var channels = AudioServer.GetBusChannels(0); var latency = AudioServer.GetOutputLatency(); Check(latency > 0 && double.IsFinite(latency), "Actual driver buffer/queue snapshot is finite and positive.");
            Reject<ArgumentException>(() => AudioServer.OutputDevice = "Missing Electron2D output fixture"); Check(AudioServer.OutputDevice == "Default" && player.IsPlaying(), "Unavailable selector preserves output/name/playback.");
            Task.Run(() => Reject<InvalidOperationException>(() => AudioServer.OutputDevice = selected)).GetAwaiter().GetResult();
            for (var run = 0; run < 3; run++)
            {
                AudioServer.Lock(); try { AudioServer.OutputDevice = selected; Check(AudioServer.GetOutputLatency() > 0, "Lock-held selection/query completes without callback lock inversion."); } finally { AudioServer.Unlock(); }
                AudioEffectTests.Wait(native, 12); AudioEffectTests.CheckOutput(native, new Vector2(.2f, -.3f) * Mathf.DBToLinear(-6));
                Check(ReferenceEquals(playback, player.GetStreamPlayback()) && ReferenceEquals(instance, AudioServer.GetBusEffectInstance(0, 0)) && AudioServer.GetMixRate() == rate && AudioServer.GetBusChannels(0) == channels, "Switch retains playback/effect identity and mix format.");
                player.StreamPaused = true; var position = player.GetPlaybackPosition(); AudioServer.OutputDevice = "Default"; AudioEffectTests.Wait(native, 12); Check(player.StreamPaused && Math.Abs(player.GetPlaybackPosition() - position) < .001, "Paused state/cursor survives switch."); player.StreamPaused = false;
            }
            player.PlaybackType = AudioServer.PlaybackType.Sample; player.Play(); AudioEffectTests.Wait(native, 20); AudioServer.OutputDevice = selected; AudioEffectTests.Wait(native, 12); AudioEffectTests.CheckOutput(native, new Vector2(.2f, -.3f) * Mathf.DBToLinear(-6)); Check(player.IsPlaying(), "Native sample transport survives selected-device switch.");
            var bytes = native.MixManagedBytes; var calls = FAudioContext.AllocationCalls; AudioEffectTests.Wait(native, 64); Check(native.MixManagedBytes == bytes && FAudioContext.AllocationCalls == calls, "64 warmed switched native passes allocate zero measured bytes/calls.");
            bytes = GC.GetAllocatedBytesForCurrentThread(); for (var i = 0; i < 128; i++) _ = AudioServer.GetOutputLatency(); Check(GC.GetAllocatedBytesForCurrentThread() == bytes, "Cached live latency queries allocate zero bytes.");
            server.CloseNative(); Check(AudioServer.OutputDevice == selected && instance.IsDisposed, "Selected preference survives closure and prior processing state invalidates.");
            Check(AudioServer.GetOutputLatency() > 0 && AudioServer.OutputDevice == selected, "Cold output query reapplies selected preference.");
            Console.WriteLine(JsonSerializer.Serialize(new { scenario = "output-device", rate, channels, latency, selected, preserved = true }));
        }
        finally { tree.Dispose(); AudioServer.OutputDevice = "Default"; server.CloseNative(); while (AudioServer.GetBusEffectCount(0) > 0) AudioServer.RemoveBusEffect(0, 0); server.CloseNative(); }
    }
    internal static void RunHost()
    {
        var settings = ProjectSettings.Service; var prior = ProjectSettings.Get(ProjectSettings.RenderingMethod); var backend = Environment.GetEnvironmentVariable("ELECTRON2D_AUDIO_RENDERER") == "compatibility" ? "compatibility" : "gpu"; ProjectSettings.Set(ProjectSettings.RenderingMethod, backend);
        try
        {
            for (var run = 0; run < 2; run++)
            {
                using var stream = AudioEffectTests.Constant(); using var capture = new AudioEffectCapture { BufferLength = .2f };
                var window = new Window { Size = new(160, 96) }; var player = new AudioStreamPlayer { Stream = stream, Autoplay = true }; window.AddChild(player); var scenario = new HostScenario(capture, player); window.AddChild(scenario);
                var server = AudioServer.Service; AudioServer.AddBusEffect(0, capture);
                try { Check(Engine.Run(window) == 0 && scenario.Completed && window.IsDisposed, "Actual output-switch host processed and cleaned up."); }
                finally { if (!window.IsDisposed) window.Dispose(); AudioServer.OutputDevice = "Default"; server.CloseNative(); while (AudioServer.GetBusEffectCount(0) > 0) AudioServer.RemoveBusEffect(0, 0); server.CloseNative(); }
                Console.WriteLine(JsonSerializer.Serialize(new { scenario = "output-host", backend, run, scenario.Completed, cleaned = window.IsDisposed }));
            }
        }
        finally { ProjectSettings.Set(ProjectSettings.RenderingMethod, prior); }
    }
    private sealed class HostScenario(AudioEffectCapture capture, AudioStreamPlayer player) : Node
    {
        internal bool Completed; private double _elapsed; private bool _switched;
        protected override void OnReady() { ProcessEnabled = true; }
        protected override void OnProcess(double delta)
        {
            _elapsed += delta; var server = AudioServer.Service;
            if (!_switched && _elapsed > .1) { var selected = AudioServer.GetOutputDeviceList().First(n => n != "Default"); AudioServer.OutputDevice = selected; Check(AudioServer.GetOutputLatency() > 0 && player.IsPlaying(), "Host device switch retains active playback."); _switched = true; capture.ClearBuffer(); }
            var available = capture.GetFramesAvailable(); if (_switched && available > 0) Completed |= capture.GetBuffer(available).Any(f => f.DistanceTo(new(.2f, -.3f)) < .0001f);
            if (_elapsed < .4) return; Check(Completed, "Public Window host produced PCM after output switch."); Tree!.Quit();
        }
    }
    private static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    private static void Reject<T>(Action action) where T : Exception { try { action(); } catch (T) { return; } throw new InvalidOperationException($"Expected {typeof(T).Name}."); }
}
