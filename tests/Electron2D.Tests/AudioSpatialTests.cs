using Electron2D;

internal static class AudioSpatialTests
{
    internal static void Run(bool native = false)
    {
        Resources();
        if (native) Native();
        Console.WriteLine("Spatial audio: listener ownership, typed scene state, panning, attenuation and Area bus routing passed.");
    }

    private static void Resources()
    {
        Check(typeof(AudioStreamPlayer).BaseType == typeof(Node) && typeof(AudioStreamEmitter).BaseType == typeof(Entity), "Player/emitter retain neutral and spatial inheritance roles.");
        using var listener = new AudioListener(); Check(!listener.IsCurrent(), "Detached listener default.");
        listener.MakeCurrent(); Check(listener.Current, "Detached listener request."); listener.ClearCurrent(); Check(!listener.Current, "Detached clear.");
        using var area = new Area(); Check(!area.AudioBusOverride && area.AudioBusName == "Master", "Area bus defaults.");
        Reject<ArgumentNullException>(() => area.AudioBusName = null!); area.AudioBusName = "Effects"; area.AudioBusOverride = true;
        using var settings = new ProjectSettingsRegistry(Directory.GetCurrentDirectory(), System.IO.Path.Combine(System.IO.Path.GetTempPath(), "e2d-spatial-settings"));
        Check(settings.Get(ProjectSettings.AudioGeneral2DPanningStrength) == .5f, "Spatial project setting default.");
        Reject<ArgumentException>(() => settings.Set(ProjectSettings.AudioGeneral2DPanningStrength, float.NaN));
        Reject<ArgumentOutOfRangeException>(() => settings.Set(ProjectSettings.AudioGeneral2DPanningStrength, -1));
        using var player = new AudioStreamEmitter();
        Check(player.GetChildCount() == 0 && player.GetChildCount(includeInternal: true) == 1, "Prepared playback is an internal child.");
        Check(player.MaxDistance == 2000 && player.Attenuation == 1 && player.PanningStrength == 1 && player.AreaMask == 0 && player.Bus == "Master", "Spatial defaults.");
        Reject<ArgumentOutOfRangeException>(() => player.MaxDistance = 0); Reject<ArgumentOutOfRangeException>(() => player.Attenuation = float.NaN);
        Reject<ArgumentOutOfRangeException>(() => player.Attenuation = -1);
        Reject<ArgumentOutOfRangeException>(() => player.PanningStrength = -1); Reject<ArgumentNullException>(() => player.Bus = null!);
        player.MaxDistance = 300; player.Attenuation = 2; player.PanningStrength = 1.5f; player.AreaMask = 7; player.Bus = "Effects";
        using var root = new Window(); root.AddChild(listener); root.AddChild(area); root.AddChild(player);
        listener.Owner = root; area.Owner = root; player.Owner = root;
        using var scene = new PackedScene(); scene.Pack(root);
        using var copied = (Window)scene.Instantiate();
        var cloned = copied.GetChildren().OfType<AudioStreamEmitter>().Single(); var clonedArea = copied.GetChildren().OfType<Area>().Single(); var clonedListener = copied.GetChildren().OfType<AudioListener>().Single();
        Check(cloned.MaxDistance == 300 && cloned.Attenuation == 2 && cloned.PanningStrength == 1.5f && cloned.AreaMask == 7 && clonedArea.AudioBusOverride && clonedArea.AudioBusName == "Effects" && !clonedListener.Current, "Scene state and exact public children.");
        Check(cloned.GetChildCount() == 0 && cloned.GetChildCount(includeInternal: true) == 1, "Packed player reconstructs one internal playback child.");
    }

    private static void Native()
    {
        var server = AudioServer.Service; server.CloseNative(); AudioServer.BusCount = 1;
        var settings = ProjectSettings.Service; var previous = ProjectSettings.Get(ProjectSettings.RenderingMethod); var fps = Engine.MaxFPS;
        ProjectSettings.Set(ProjectSettings.RenderingMethod, Environment.GetEnvironmentVariable("ELECTRON2D_AUDIO_RENDERER") == "compatibility" ? "compatibility" : "gpu"); Engine.MaxFPS = 60;
        using var stream = AudioEffectTests.Constant(); using var shape = new RectangleShape { Size = new(40, 40) }; using var capture = new AudioEffectCapture { BufferLength = .2f };
        var window = new Window { Size = new(160, 96) }; var player = new AudioStreamEmitter { Stream = stream, Position = new(80, 48) }; window.AddChild(player);
        var listener = new AudioListener { Position = new(160, 48) }; window.AddChild(listener);
        var area = new Area { Position = new(160, 48), AudioBusName = "Effects", CollisionLayer = 1 }; area.AddChild(new CollisionShape { Shape = shape }); window.AddChild(area);
        var scenario = new HostScenario(window, player, listener, area, capture); window.AddChild(scenario);
        try
        {
            AudioServer.AddBus(); AudioServer.SetBusName(1, "Effects"); AudioServer.AddBusEffect(1, capture);
            Check(Engine.Run(window) == 0 && scenario.Completed && window.IsDisposed, "Spatial native host completed and released its scene.");
        }
        finally { if (!window.IsDisposed) window.Dispose(); server.CloseNative(); AudioServer.BusCount = 1; Engine.MaxFPS = fps; ProjectSettings.Set(ProjectSettings.RenderingMethod, previous); }
    }

    private sealed class HostScenario(Window window, AudioStreamEmitter player, AudioListener listener, Area area, AudioEffectCapture capture) : Node
    {
        internal bool Completed;
        private AudioStreamEmitter? _autoplay;
        private int _phase, _frames;
        protected override void OnReady()
        {
            Check(window.AudioListenerEnable2D && window.GetAudioListener2D() is null, "Root default listening center.");
            player.Play(); ProcessEnabled = true;
        }
        protected override void OnProcess(double delta)
        {
            if (++_frames < 3) return;
            _frames = 0;
            var native = AudioServer.Service.Native;
            switch (_phase++)
            {
                case 0:
                    var size = window.GetVisibleRect().Size;
                    player.Position = size * .5f; listener.Position = area.Position = new(size.X, size.Y * .5f);
                    player.RefreshSpatial(queryArea: true);
                    AudioEffectTests.CheckOutput(native, new(.1f, -.15f));
                    player.Attenuation = 0; player.Position = listener.Position;
                    break;
                case 1:
                    var edgeSize = window.GetVisibleRect().Size;
                    player.Position = listener.Position = area.Position = new(edgeSize.X, edgeSize.Y * .5f);
                    player.RefreshSpatial(queryArea: true);
                    AudioEffectTests.CheckOutput(native, new(.075f, -.1875f));
                    listener.MakeCurrent(); Check(ReferenceEquals(window.GetAudioListener2D(), listener), "Current explicit listener owns root viewport.");
                    break;
                case 2:
                    AudioEffectTests.CheckOutput(native, new(.1f, -.15f));
                    if (native.Channels > 2)
                    {
                        var pcm = native.CapturedPCM();
                        for (var frame = 0; frame < pcm.Length; frame += native.Channels)
                            for (var channel = 2; channel < native.Channels; channel++)
                                Check(Math.Abs(pcm[frame + channel]) < .0001f, "Spatial source uses the front stereo pair on a multichannel output.");
                    }
                    player.MaxDistance = 10; player.Position = window.GetVisibleRect().Size * .5f;
                    break;
                case 3:
                    AudioEffectTests.CheckOutput(native, Vector2.Zero);
                    player.Position = listener.Position; player.MaxDistance = 2000; player.AreaMask = 1; area.AudioBusOverride = true;
                    break;
                case 4:
                    capture.ClearBuffer(); AudioEffectTests.Wait(native, 8);
                    Check(capture.GetFramesAvailable() > 0 && capture.GetBuffer(capture.GetFramesAvailable()).Any(f => f.DistanceTo(new(.1f, -.15f)) < .0001f), "Area point query routes actual spatial PCM into selected bus.");
                    for (var i = 0; i < 20; i++) player.RefreshSpatial(queryArea: true);
                    var bytes = GC.GetAllocatedBytesForCurrentThread(); var allocations = FAudioContext.AllocationCalls;
                    for (var i = 0; i < 64; i++) player.RefreshSpatial(queryArea: true);
                    var managedDelta = GC.GetAllocatedBytesForCurrentThread() - bytes; var nativeDelta = FAudioContext.AllocationCalls - allocations;
                    Check(managedDelta == 0 && nativeDelta == 0, $"Warmed spatial area query and voice matrix update allocate zero measured bytes/calls; managed {managedDelta}, native {nativeDelta}.");
                    area.AudioBusOverride = false;
                    break;
                case 5:
                    capture.ClearBuffer(); AudioEffectTests.Wait(native, 8);
                    var after = capture.GetBuffer(capture.GetFramesAvailable());
                    Check(after.All(f => f == Vector2.Zero), $"Disabling Area override restores authored routing; first sample {after.FirstOrDefault()}.");
                    player.Stop(); listener.ClearCurrent(); Check(window.GetAudioListener2D() is null, "Explicit listener release.");
                    listener.MakeCurrent(); window.RemoveChild(listener); Check(listener.Current, "Current request survives scene exit.");
                    window.AddChild(listener); Check(ReferenceEquals(window.GetAudioListener2D(), listener), "Current request restores on reentry."); listener.ClearCurrent();
                    _autoplay = new AudioStreamEmitter { Name = "AutoplaySpatial", Stream = player.Stream, Autoplay = true, Position = window.GetVisibleRect().Size * .5f }; window.AddChild(_autoplay);
                    break;
                case 6:
                    _autoplay!.Position = window.GetVisibleRect().Size * .5f;
                    _autoplay.RefreshSpatial(queryArea: true);
                    Check(_autoplay.IsPlaying(), "Spatial autoplay begins on the first fixed step.");
                    AudioEffectTests.CheckOutput(native, new(.1f, -.15f));
                    window.AudioListenerEnable2D = false;
                    break;
                case 7:
                    AudioEffectTests.CheckOutput(native, Vector2.Zero);
                    window.AudioListenerEnable2D = true;
                    break;
                case 8:
                    _autoplay!.Position = window.GetVisibleRect().Size * .5f;
                    _autoplay.RefreshSpatial(queryArea: true);
                    AudioEffectTests.CheckOutput(native, new(.1f, -.15f));
                    _autoplay!.Stop(); Completed = true; Tree!.Quit();
                    break;
            }
        }
    }

    private static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    private static void Reject<T>(Action action) where T : Exception { try { action(); } catch (T) { return; } throw new InvalidOperationException($"Expected {typeof(T).Name}."); }
}
