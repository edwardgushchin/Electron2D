using System.Buffers.Binary;
using System.Diagnostics;
using Electron2D;
using Path = System.IO.Path;
using static MovieWriterTests;

internal static class ProjectRecordingTests
{
    private const string ExtensionID = "test:project-recording";
    private const string LoopClass = "test:recording-loop";
    private const int FPS = 7;
    private const int MixRate = 48000;

    internal static void Run(string backend)
    {
        ProjectSettings.Set(ProjectSettings.RenderingMethod, backend);
        ProjectSettings.Set(ProjectSettings.RenderingFallback, false);
        ProjectSettings.Set(ProjectSettings.MovieWriterDisableVSync, true);
        ProjectSettings.Set(ProjectSettings.MovieWriterMixRate, MixRate);
        ProjectSettings.Set(ProjectSettings.MovieWriterSpeakerMode, AudioServer.SpeakerMode.Stereo);
        ProjectSettings.Set(ProjectSettings.LowProcessorMode, true);
        ProjectSettings.Set(ProjectSettings.MovieWriterFile, "ignored.png");
        ProjectSettings.Set(ProjectSettings.MovieWriterFPS, 29);
        Engine.PhysicsTicksPerSecond = 60;

        using var folder = DirAccess.CreateTemp("e2d-record-project-");
        using var source = Source();
        var root = folder.GetCurrentDir();
        using (var window = NewWindow())
        {
            Reject<ArgumentException>(() => Engine.RunProject(window, " "));
            Reject<ArgumentOutOfRangeException>(() => Engine.RunProjectAsync(window, "valid.png", 0));
            if (SynchronizationContext.Current is null)
                Reject<InvalidOperationException>(() => Engine.RunProjectAsync(window, "valid.png", FPS));
            Check(!window.IsDisposed && Engine.MainLoop is null && Engine.GetWriteMoviePath() == "", "Rejected startup preserves the detached window.");
        }

        ExtensionManager.LoadExtensionFromFunction(ExtensionID, OpenExtension);
        using var extension = ExtensionManager.GetExtension(ExtensionID)!;
        try
        {
            ProjectSettings.Set(ProjectSettings.MainLoopType, LoopClass);
            ProjectSettings.Set(ProjectSettings.MainScene, Path.Combine(root, "missing.e2dscene"));
            RunCustom(root, source, "custom-sync", asynchronous: false);
            RunCustom(root, source, "custom-async", asynchronous: true);
            RunCustom(root, source, "custom-failure", asynchronous: false, fail: true);
        }
        finally { ExtensionManager.UnloadExtension(ExtensionID); }

        ProjectSettings.Reset(ProjectSettings.MainLoopType);
        using var scene = new ColorRect { Name = "RecordedScene", Size = new(64, 48), Color = Colors.Green };
        var scenePath = Path.Combine(root, "main.e2dscene");
        ResourceSaver.SaveScene(scene, scenePath);
        ProjectSettings.Set(ProjectSettings.MainScene, scenePath);
        var path = Path.Combine(root, "scene.png");
        using (var window = NewWindow())
        {
            window.AddChild(new AudioStreamPlayer { Stream = source, Autoplay = true });
            window.Ready += _ =>
            {
                var tree = window.Tree!;
                Check(tree.CurrentScene?.Name == scene.Name && Engine.GetWriteMoviePath() == path, "The configured scene and explicit path are visible during Ready.");
                tree.ProcessFrameStarted += static current => current.Quit(31);
            };
            Check(Engine.RunProject(window, path, FPS) == 31, "SceneTree quit code is retained.");
            CheckRetired(window);
        }
        VerifyOutput(path, 1, Colors.Green);
        Console.WriteLine($"Project recording {backend}: sync/async custom loop, final frame, exact native PCM, failure cleanup and configured scene passed.");
    }

    private static RecordingLoop CreateLoop() => new();

    private static ExtensionInitialization? OpenExtension(Extension module) =>
        new(ExtensionInitializationLevel.Scene, level =>
        {
            if (level == ExtensionInitializationLevel.Scene)
                module.RegisterClass(LoopClass, CreateLoop, static _ => { }, static _ => { });
        }, static _ => { });

    private static Window NewWindow() => new()
    {
        Size = new(64, 48),
        ContentScaleMode = ContentScaleMode.Viewport,
        ContentScaleSize = new(64, 48),
        Borderless = true,
    };

    private static void RunCustom(string folder, AudioStream source, string name, bool asynchronous, bool fail = false)
    {
        using var window = NewWindow();
        var path = Path.Combine(folder, name + ".png");
        RecordingLoop.Configure(window, source, path, fail);
        window.Ready += _ => throw new InvalidOperationException("A custom loop must not enter the window tree.");
        if (fail) Reject<ArithmeticException>(() => Engine.RunProject(window, path, FPS));
        else
        {
            var code = asynchronous ? RunAsync(window, path) : Engine.RunProject(window, path, FPS);
            Check(code == 0, "Custom loop stop returns zero.");
        }

        var loop = RecordingLoop.Last!;
        Check(loop.IsDisposed && loop.Finalized && loop.Processed == (fail ? 2 : FPS), "The custom loop retires after rendering or failure.");
        if (!fail) Check(loop.Physics == 60, "Seven frames execute exactly sixty fixed ticks.");
        VerifyOutput(path, fail ? 1 : FPS, fail ? Colors.Red : Colors.Blue);
        CheckRetired(window);
    }

    private static int RunAsync(Window window, string path)
    {
        var previous = SynchronizationContext.Current;
        var context = new PumpContext();
        SynchronizationContext.SetSynchronizationContext(context);
        try
        {
            var pending = Engine.RunProjectAsync(window, path, FPS);
            var start = Stopwatch.GetTimestamp();
            while (!pending.IsCompleted)
            {
                if (Stopwatch.GetElapsedTime(start) > TimeSpan.FromSeconds(15)) throw new TimeoutException("The project recording did not complete.");
                context.Pump();
            }
            return pending.GetAwaiter().GetResult();
        }
        finally { SynchronizationContext.SetSynchronizationContext(previous); }
    }

    private static void VerifyOutput(string path, int count, Color expected)
    {
        var prefix = path[..^4];
        var framePattern = Path.GetFileName(prefix) + "????????.png";
        Check(Directory.EnumerateFiles(Path.GetDirectoryName(path)!, framePattern).Count() == count, "Only committed simulation frames reach the encoder.");
        using var image = Image.LoadFromFile(prefix + (count - 1).ToString("D8", System.Globalization.CultureInfo.InvariantCulture) + ".png");
        Check(image.Size == new Vector2i(64, 48) && image.GetPixel(5, 5).IsEqualApprox(expected), "Decoded PNG contains the final canvas state.");
        var wav = File.ReadAllBytes(prefix + ".wav");
        var length = count * MixRate / FPS * 8;
        Check(BinaryPrimitives.ReadInt32LittleEndian(wav.AsSpan(24)) == MixRate &&
            BinaryPrimitives.ReadInt32LittleEndian(wav.AsSpan(40)) == length && wav.Length == 44 + length, "WAV has the exact cumulative PCM duration.");
        var audible = false;
        for (var i = 44; i < wav.Length; i += 4)
            audible |= BinaryPrimitives.ReadInt32LittleEndian(wav.AsSpan(i)) > 100_000_000;
        Check(audible, "The actual native mixer delivers the sample tone.");
    }

    private static void CheckRetired(Window window) => Check(window.IsDisposed && Engine.MainLoop is null &&
        !RenderingServer.IsAvailable && !DisplayServer.IsAvailable && Engine.GetWriteMoviePath() == "", "Owned services and movie path retire.");

    private sealed class RecordingLoop : MainLoop
    {
        private static Window _window = null!;
        private static AudioStream _source = null!;
        private static string _path = "";
        private static bool _fail;
        private AudioStreamPlayback? _playback;
        private RID _item;
        internal static RecordingLoop? Last;
        internal int Physics, Processed;
        internal bool Finalized;

        internal static void Configure(Window window, AudioStream source, string path, bool fail)
        {
            (_window, _source, _path, _fail) = (window, source, path, fail);
            Last = null;
        }

        protected override void OnInitialize()
        {
            Last = this;
            Check(_window.Tree is null && ReferenceEquals(Engine.MainLoop, this) &&
                Engine.GetWriteMoviePath() == _path && AudioServer.GetMixRate() == MixRate, "The movie graph precedes custom initialization.");
            var canvas = RenderingServer.CanvasCreate();
            _item = RenderingServer.CanvasItemCreate();
            RenderingServer.CanvasItemSetParent(_item, canvas);
            RenderingServer.ViewportAttachCanvas(_window.GetViewportRID(), canvas);
            _playback = _source.InstantiatePlayback();
            _playback.SetSamplePlayback(new AudioSamplePlayback(_source));
            _playback.Start();
        }

        protected override bool OnPhysicsProcess(double delta)
        {
            Check(Math.Abs(delta - 1d / 60) < 1e-12, "Fixed custom physics delta.");
            Physics++;
            return false;
        }

        protected override bool OnProcess(double delta)
        {
            Check(Math.Abs(delta - 1d / FPS) < 1e-12, "The explicit recording cadence reaches the custom loop.");
            Processed++;
            if (_fail && Processed == 2) throw new ArithmeticException("Injected custom process failure.");
            if (Processed == 1 || Processed == FPS)
            {
                RenderingServer.CanvasItemClear(_item);
                RenderingServer.CanvasItemAddRect(_item, new(2, 2, 12, 12), Processed == FPS ? Colors.Blue : Colors.Red);
            }
            return Processed == FPS;
        }

        protected override void OnFinalize()
        {
            Check(Engine.GetWriteMoviePath() == _path && RenderingServer.IsAvailable, "The renderer and output path survive finalization.");
            _playback?.Dispose();
            Finalized = true;
        }
    }
}
