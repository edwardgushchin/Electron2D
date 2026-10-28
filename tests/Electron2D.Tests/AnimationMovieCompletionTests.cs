using System.Buffers.Binary;
using Electron2D;
using Path = System.IO.Path;
using static MovieWriterTests;

internal static class AnimationMovieCompletionTests
{
    private const string CompletionMessage = "Movie recording is ending after animation finished:";
    private static readonly PropertyDescriptor<Entity, Color> Modulate = new(nameof(Entity.Modulate), n => n.Modulate, (n, v) => n.Modulate = v);

    private sealed class Scene : IDisposable
    {
        internal readonly Window Window = new() { Name = "MovieRoot", Size = new(320, 96), Borderless = true, Title = "Animation movie completion" };
        internal readonly Entity Canvas = new() { Name = "Canvas" };
        internal readonly AnimationPlayer Player = new() { Name = "Player", Autoplay = "a", MovieQuitOnFinish = true, PlaybackAutoCapture = false };
        internal readonly AnimationLibrary Library = new();
        internal readonly Animation First = Clip(Colors.Red, Colors.Blue, .15);
        internal readonly Animation Second = Clip(Colors.Blue, Colors.Green, .1);
        private readonly AudioStreamWAV _audio = Source();
        internal int Finished;
        internal Scene()
        {
            Canvas.Draw += item => item.DrawRect(new(0, 0, 320, 96), Colors.White);
            Window.AddChild(Canvas); Window.AddChild(Player);
            Library.AddAnimation("a", First); Library.AddAnimation("b", Second); Player.AddAnimationLibrary("", Library);
            Window.AddChild(new AudioStreamPlayer { Stream = _audio, Autoplay = true });
            Window.Ready += _ => RenderingServer.SetDefaultClearColor(Colors.Black);
            Player.AnimationFinished += _ => Finished++;
        }
        public void Dispose()
        {
            Window.Dispose(); Player.Dispose(); Library.Dispose(); First.Dispose(); Second.Dispose(); _audio.Dispose();
        }
    }

    private static Animation Clip(Color from, Color to, double duration)
    {
        var animation = new Animation { Length = duration };
        var track = animation.AddTrack(Modulate); animation.TrackSetPath(track, "Canvas:Modulate");
        animation.TrackInsertKey(track, 0, from); animation.TrackInsertKey(track, duration, to);
        return animation;
    }

    private sealed class Writer : MovieWriter
    {
        internal int Frames, Begins, Ends, Limit = 8;
        internal long Samples;
        internal double Magnitude;
        internal Color LastColor;
        internal bool FailEnd;
        internal Action? FrameHook;
        protected override string[] OnGetSupportedExtensions() => ["animationmovie"];
        protected override bool OnHandlesFile(string path) => path.EndsWith(".animationmovie", StringComparison.Ordinal);
        protected override void OnWriteBegin(Vector2i size, int fps, string path)
        { Check(fps == 20 && size.X >= 320 && size.Y >= 96, "Actual recording geometry and cadence."); Frames = 0; Samples = 0; Magnitude = 0; Begins++; }
        protected override void OnWriteFrame(Image image, ReadOnlySpan<int> pcm)
        {
            LastColor = image.GetPixel(5, 5); Frames++; Samples += pcm.Length / 2;
            foreach (var sample in pcm) Magnitude += Math.Abs((double)sample / int.MaxValue);
            FrameHook?.Invoke();
            if (Frames >= Limit) ((SceneTree)Engine.MainLoop!).Quit(91);
        }
        protected override void OnWriteEnd() { Ends++; if (FailEnd) throw new IOException("end failure"); }
    }

    private sealed class Messages : Logger
    {
        internal int Completions;
        protected override void OnLogMessage(string message, bool error)
        { if (message.StartsWith(CompletionMessage, StringComparison.Ordinal)) Completions++; }
    }

    internal static void Portable()
    {
        using var root = new Node(); using var player = new AnimationPlayer { Name = "Player" };
        Check(!player.MovieQuitOnFinish && !player.IsAnimationActive(), "Movie completion defaults off and there is no playback selection.");
        player.MovieQuitOnFinish = true; root.AddChild(player); player.Owner = root;
        using var packed = new PackedScene(); packed.Pack(root);
        using (var copy = packed.Instantiate()) Check(copy.GetNode<AnimationPlayer>("Player").MovieQuitOnFinish, "Authored flag survives in-memory scene storage.");
        var folder = Path.Combine(Path.GetTempPath(), "electron2d-animation-movie-" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(folder);
        try
        {
            var path = Path.Combine(folder, "scene.e2dscene"); ResourceSaver.Save(packed, path, SaverFlags.BundleResources);
            using var loaded = ResourceLoader.Load<PackedScene>(path, ResourceLoader.CacheMode.IgnoreDeep);
            using var copy = loaded.Instantiate(); Check(copy.GetNode<AnimationPlayer>("Player").MovieQuitOnFinish, "Authored flag survives scene-file storage.");
        }
        finally { Directory.Delete(folder, true); }
        using var library = new AnimationLibrary(); using var clip = new Animation { Length = .1 }; library.AddAnimation("a", clip); player.AddAnimationLibrary("", library);
        using var tree = new SceneTree(root);
        Task.Run(() => Reject<InvalidOperationException>(() => player.MovieQuitOnFinish = false)).GetAwaiter().GetResult();
        player.Play("a"); player.Pause(); Check(player.IsAnimationActive() && !player.IsPlaying(), "Pause retains a valid playback selection.");
        player.Active = false; Check(player.IsAnimationActive(), "Mixer evaluation eligibility does not clear selection."); player.Active = true;
        var stopped = 0; player.CurrentAnimationChanged += name => { if (name.Length == 0) stopped++; };
        player.Stop(); Check(!player.IsAnimationActive() && stopped == 1, "Stop clears selection and publishes its empty state.");
        var events = new List<string>(); player.CurrentAnimationChanged += name => events.Add("changed:" + name); player.AnimationFinished += name => events.Add("finished:" + name);
        player.Play("a"); player.Advance(.2); Check(events.SequenceEqual(new[] { "finished:a", "changed:" }), "Terminal selection notification follows the completion event."); Check(player.IsAnimationActive() && !player.IsPlaying() && player.CurrentAnimationPosition == .1, "Finite completion retains its selected endpoint."); Check(!tree.Process(0), "Ordinary attached completion does not quit without recording.");
        player.Seek(.1, update: true); Check(!tree.Process(0), "Seeking is not movie completion.");
        root.RemoveChild(player); player.Play("a"); player.Advance(.2);
        Check(!tree.Process(0), "Detached completion cannot quit its former tree.");
        player.Dispose(); Reject<ObjectDisposedException>(() => _ = player.MovieQuitOnFinish); Reject<ObjectDisposedException>(() => player.MovieQuitOnFinish = false);
        Console.WriteLine("Animation movie portable: defaults, scene/file storage, owner/disposal and non-recording/detached completion passed.");
    }

    internal static void Native(string backend, string folder)
    {
        Directory.CreateDirectory(folder); ProjectSettings.Set(ProjectSettings.RenderingMethod, backend); ProjectSettings.Set(ProjectSettings.RenderingFallback, false);
        using var writer = new Writer(); MovieWriter.AddWriter(writer); using var messages = new Messages(); OS.AddLogger(messages);
        var only = Environment.GetEnvironmentVariable("ELECTRON2D_ANIMATION_MOVIE_CASE");
        foreach (var name in new[] { "forward", "reverse", "section", "queue", "next", "restart", "change-restart", "stop", "ready", "physics", "worker-rejected", "scale", "pause", "enable", "disabled", "loop", "pingpong", "seek", "detach", "explicit-quit", "callback-failure", "end-failure", "foreign", "ordinary", "builtin" })
        {
            if (only is not null && only != name) continue;
            if (name == "builtin") { Builtin(folder); Console.WriteLine(name + " passed"); continue; }
            using var scene = new Scene(); Engine.TimeScale = 1; writer.Limit = 8; writer.FailEnd = false; writer.FrameHook = null; messages.Completions = 0;
            var expectedCode = 0; var expectedFrames = 3; var expectedColor = Colors.Blue; var expectedFinished = 1;
            switch (name)
            {
                case "reverse": scene.Window.Ready += _ => scene.Player.PlayBackwards("a"); expectedColor = Colors.Red; break;
                case "section": scene.Window.Ready += _ => scene.Player.PlaySection("a", .05, .1); expectedFrames = 1; expectedColor = Colors.Red.Lerp(Colors.Blue, 2f / 3); break;
                case "queue": scene.Window.Ready += _ => scene.Player.Queue("b"); expectedFrames = 5; expectedFinished = 2; expectedColor = Colors.Green; break;
                case "next": scene.Player.AnimationSetNext("a", "b"); expectedFrames = 5; expectedFinished = 2; expectedColor = Colors.Green; break;
                case "restart": scene.Player.AnimationFinished += _ => { if (scene.Finished == 1) scene.Player.Play("a"); }; expectedFrames = 6; expectedFinished = 2; break;
                case "change-restart": scene.Player.CurrentAnimationChanged += value => { if (value.Length == 0 && scene.Finished == 1) scene.Player.Play("a"); }; expectedFrames = 6; expectedFinished = 2; break;
                case "stop": scene.Player.AnimationFinished += _ => scene.Player.Stop(keepState: true); expectedCode = 91; expectedFrames = writer.Limit; break;
                case "ready": scene.Window.Ready += _ => scene.Player.Advance(.2); expectedFrames = 0; break;
                case "physics": scene.Player.CallbackModeProcess = AnimationMixer.AnimationCallbackModeProcess.Physics; break;
                case "worker-rejected":
                    var group = new Entity { Name = "WorkerGroup", ProcessThreadGroup = Node.ProcessThreadGroupMode.SubThread };
                    scene.Window.RemoveChild(scene.Canvas); scene.Window.RemoveChild(scene.Player);
                    group.AddChild(scene.Canvas); group.AddChild(scene.Player); scene.Window.AddChild(group); break;
                case "scale": Engine.TimeScale = 2; expectedFrames = 2; break;
                case "pause": scene.Window.Ready += _ => scene.Window.Tree!.Paused = true; writer.FrameHook = () => { if (writer.Frames == 2) scene.Window.Tree!.Paused = false; }; expectedFrames = 5; break;
                case "enable": scene.Player.MovieQuitOnFinish = false; scene.Player.AnimationFinished += _ => scene.Player.MovieQuitOnFinish = true; break;
                case "disabled": scene.Player.AnimationFinished += _ => scene.Player.MovieQuitOnFinish = false; expectedCode = 91; expectedFrames = writer.Limit; break;
                case "loop": scene.First.LoopMode = SpriteFrames.LoopMode.Linear; expectedCode = 91; expectedFrames = writer.Limit; expectedFinished = 0; break;
                case "pingpong": scene.First.LoopMode = SpriteFrames.LoopMode.PingPong; expectedCode = 91; expectedFrames = writer.Limit; expectedFinished = 0; break;
                case "seek": scene.Player.CallbackModeProcess = AnimationMixer.AnimationCallbackModeProcess.Manual; scene.Window.Ready += _ => scene.Player.Seek(.15, update: true); expectedCode = 91; expectedFrames = writer.Limit; expectedFinished = 0; break;
                case "detach": scene.Player.AnimationFinished += _ => scene.Window.RemoveChild(scene.Player); expectedCode = 91; expectedFrames = writer.Limit; break;
                case "explicit-quit": scene.Player.AnimationFinished += _ => ((SceneTree)Engine.MainLoop!).Quit(37); expectedCode = 37; break;
                case "callback-failure": scene.Player.AnimationFinished += _ => throw new FormatException("completion failure"); break;
                case "end-failure": writer.FailEnd = true; break;
                case "foreign": Foreign(scene, writer, folder); Console.WriteLine(name + " passed"); continue;
                case "ordinary": Ordinary(scene, writer); Console.WriteLine(name + " passed"); continue;
            }
            var beforeEnds = writer.Ends;
            if (name == "worker-rejected")
            {
                try { Engine.Run(scene.Window, Path.Combine(folder, name + ".animationmovie"), 20); throw new InvalidOperationException("The owner-thread animation guard was lost."); }
                catch (AggregateException error) { Check(error.Flatten().InnerExceptions.Count == 1 && error.Flatten().InnerExceptions[0] is InvalidOperationException { Message: "SceneTree mutation and execution must run on its owner thread." }, "Animation stepping retains its owner-thread contract."); }
                Check(writer.Frames == 0 && scene.Finished == 0 && messages.Completions == 0 && writer.Ends == beforeEnds + 1, "A rejected worker step finalizes an empty recording without a false completion.");
            }
            else if (name == "callback-failure")
            {
                try { Engine.Run(scene.Window, Path.Combine(folder, name + ".animationmovie"), 20); throw new InvalidOperationException("The completion failure was lost."); }
                catch (AggregateException error) { Check(error.Flatten().InnerExceptions.Count == 1 && error.Flatten().InnerExceptions[0] is FormatException { Message: "completion failure" }, "Only the expected scene callback failure propagates."); }
                Check(writer.Frames == 2 && writer.Ends == beforeEnds + 1, "Callback failure finalizes only already successful frames.");
            }
            else if (name == "end-failure")
            {
                Reject<IOException>(() => Engine.Run(scene.Window, Path.Combine(folder, name + ".animationmovie"), 20));
                Check(writer.Frames == 3 && writer.Ends == beforeEnds + 1, "Writer finalization failure retains its final submitted frame and releases runtime.");
            }
            else
            {
                var code = Engine.Run(scene.Window, Path.Combine(folder, name + ".animationmovie"), 20);
                Check(code == expectedCode, name + ": completion must request its expected quit code; got " + code);
                Check(writer.Frames == expectedFrames && scene.Finished == expectedFinished, name + ": exact successful frame/completion counts; got " + writer.Frames + "/" + scene.Finished);
                if (name is not ("loop" or "pingpong" or "ready")) Near(writer.LastColor, expectedColor);
                Check(writer.Samples == (long)writer.Frames * 48000 / 20 && (writer.Frames == 0 || writer.Magnitude > 1), "Final rational PCM block and real audio reach the writer.");
                Check(writer.Ends == beforeEnds + 1 && messages.Completions == (expectedCode == 0 ? 1 : 0), "Single finalization and completion diagnostic.");
            }
            Check(scene.Window.IsDisposed && Engine.MainLoop is null && Engine.GetWriteMoviePath() == "", "Completion and failures release window, main loop and movie identity.");
            Console.WriteLine(name + " passed: " + writer.Frames + " successful frames");
        }
        Engine.TimeScale = 1;
        Console.WriteLine("Animation movie native " + backend + " passed.");
    }

    private static void Foreign(Scene scene, Writer writer, string folder)
    {
        scene.Player.MovieQuitOnFinish = false; writer.Limit = 5;
        using var root = new Node(); using var player = new AnimationPlayer { MovieQuitOnFinish = true, CallbackModeProcess = AnimationMixer.AnimationCallbackModeProcess.Manual };
        using var library = new AnimationLibrary(); using var clip = new Animation { Length = .1 }; library.AddAnimation("a", clip); player.AddAnimationLibrary("", library);
        root.AddChild(player); using var tree = new SceneTree(root); player.Play("a");
        writer.FrameHook = () => { if (writer.Frames == 1) { player.Advance(.2); Check(!tree.Process(0), "A foreign manual tree cannot finish another tree's recording."); } };
        try { Check(Engine.Run(scene.Window, Path.Combine(folder, "foreign.animationmovie"), 20) == 91 && writer.Frames == 5, "The recording survives a foreign player's completion."); }
        finally { writer.FrameHook = null; }
    }

    private static void Ordinary(Scene scene, Writer writer)
    {
        scene.Player.CallbackModeProcess = AnimationMixer.AnimationCallbackModeProcess.Manual; writer.Frames = 0;
        var code = Engine.Run((window, frame) =>
        {
            if (frame == 1) scene.Player.Advance(.2);
            if (frame == 2) { using var image = window.GetTexture().GetImage()!; Near(image.GetPixel(5, 5), Colors.Blue); }
            if (frame == 4) ((SceneTree)Engine.MainLoop!).Quit(91);
        }, scene.Window);
        Check(code == 91 && scene.Finished == 1 && writer.Frames == 0, "Ordinary rendered playback ignores the authored movie-only flag.");
    }

    private static void Builtin(string folder)
    {
        ProjectSettings.Set(ProjectSettings.MovieWriterMixRate, 48000); ProjectSettings.Set(ProjectSettings.MovieWriterSpeakerMode, AudioServer.SpeakerMode.Stereo);
        foreach (var extension in new[] { ".png", ".avi", ".ogv" })
        {
            using var scene = new Scene();
            var path = Path.Combine(folder, "terminal" + extension);
            var code = Engine.Run((_, frame) => { if (frame > 8) ((SceneTree)Engine.MainLoop!).Quit(91); }, scene.Window, path, 20);
            Check(code == 0 && scene.Finished == 1, "Built-in " + extension + " recording finishes normally.");
            if (extension != ".png") { using var file = File.Open(path, FileMode.Open, System.IO.FileAccess.Read, FileShare.None); Check(file.Length > 0, "The native encoded movie is closed and published."); continue; }
            var images = Directory.GetFiles(folder, "terminal????????.png").Order(StringComparer.Ordinal).ToArray();
            Check(code == 0 && images.Length == 3, "Built-in recording ends on the actual clip endpoint.");
            using var image = Image.LoadFromFile(images[^1]); Near(image.GetPixel(5, 5), Colors.Blue);
            var wav = File.ReadAllBytes(Path.ChangeExtension(path, ".wav"));
            Check(wav.Length == 44 + 3 * 48000 / 20 * 2 * 4 && BinaryPrimitives.ReadUInt32LittleEndian(wav.AsSpan(40)) == wav.Length - 44, "Built-in WAV finalization publishes every frame's PCM length.");
        }
    }

    private static void Near(Color actual, Color expected)
    { Check(Math.Abs(actual.R - expected.R) < .03 && Math.Abs(actual.G - expected.G) < .03 && Math.Abs(actual.B - expected.B) < .03, "Final recorded color differs: " + actual + " vs " + expected); }
}
