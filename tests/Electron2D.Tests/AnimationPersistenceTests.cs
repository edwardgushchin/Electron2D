using Electron2D;
using System.Diagnostics;
using Path = System.IO.Path;

internal static class AnimationPersistenceTests
{
    private static readonly PropertyDescriptor<Entity, Vector2> Position = new(nameof(Entity.Position), n => n.Position, (n, v) => n.Position = v);
    private static readonly PropertyDescriptor<Box, double> Value = new("Value", n => n.Value, (n, v) => n.Value = v);
    private static readonly PropertyDescriptor<Box, float> Height = new("Height", n => n.Height, (n, v) => n.Height = v);
    private static readonly PropertyDescriptor<Box, Gradient?> Gradient = new("Gradient", n => n.Gradient, (n, v) => n.Gradient = v);
    private static readonly Func<double, double, double, double> Interpolate = Curved;
    private static double Curved(double a, double b, double w) => a + (b - a) * w * w;
    private static bool _registered;
    private static void Register()
    {
        if (_registered) return;
        ResourceFileTypes.RegisterAnimationProperty("test.animation.position", Position);
        ResourceFileTypes.RegisterAnimationProperty("test.animation.value", Value, Interpolate);
        ResourceFileTypes.RegisterAnimationProperty("test.animation.height", Height);
        ResourceFileTypes.RegisterAnimationProperty("test.animation.gradient", Gradient);
        _registered = true;
    }
    internal static void Run()
    {
        Register(); var directory = Path.Combine(Path.GetTempPath(), "e2d-animation-" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(directory);
        try
        {
            var path = Path.Combine(directory, "motion.e2dres"); using var gradient = new Gradient(); using var clip = Create(gradient); using var library = new AnimationLibrary();
            library.AddAnimation("move", clip); library.AddAnimation("alias", clip);
            ResourceSaver.Save(library, path, SaverFlags.BundleResources | SaverFlags.SaveBigEndian);
            using (var loaded = ResourceLoader.Load<AnimationLibrary>(path, ResourceLoader.CacheMode.IgnoreDeep)) Verify(loaded);
            Child(path, "verify"); Child(path, "unknown");
            using (var cached = ResourceLoader.Load<AnimationLibrary>(path))
            {
                Check(ReferenceEquals(cached, ResourceLoader.Load<AnimationLibrary>(path)), "Reuse keeps root identity.");
                clip.TrackSetKeyValue(0, 1, new Vector2(40, 16)); ResourceSaver.Save(library, path, SaverFlags.BundleResources);
                Check(ReferenceEquals(cached, ResourceLoader.Load<AnimationLibrary>(path, ResourceLoader.CacheMode.ReplaceDeep)), "Replace keeps library identity.");
                Check(cached.GetAnimation("move").TrackGetKeyValue<Vector2>(0, 1).X == 40 && ReferenceEquals(cached.GetAnimation("move"), cached.GetAnimation("alias")), "Replace restores changed keys and aliases.");
            }
            var prior = File.ReadAllBytes(path);
            using var unsupported = new Animation(); unsupported.AddAnimationTrack(); Reject<AggregateException>(() => ResourceSaver.Save(unsupported, path));
            Check(File.ReadAllBytes(path).SequenceEqual(prior), "Unsupported tracks cannot overwrite the asset.");
            using var unregistered = new Animation(); var descriptor = new PropertyDescriptor<Box, double>("Unregistered", n => n.Value, (n, v) => n.Value = v); unregistered.AddTrack(descriptor);
            Reject<AggregateException>(() => ResourceSaver.Save(unregistered, path)); Check(File.ReadAllBytes(path).SequenceEqual(prior), "Unregistered property cannot overwrite the asset.");
            using var copy = (Animation)unregistered.Duplicate(); Check(copy.GetTrackCount() == 1, "Unregistered runtime clips still duplicate.");
            using var empty = new AnimationLibrary(); var emptyPath = Path.Combine(directory, "empty.e2dres"); ResourceSaver.Save(empty, emptyPath);
            using var emptyLoaded = ResourceLoader.Load<AnimationLibrary>(emptyPath, ResourceLoader.CacheMode.Ignore); emptyLoaded.AddAnimation("new", clip); ResourceSaver.Save(emptyLoaded, emptyPath, SaverFlags.BundleResources);
            using var nonempty = ResourceLoader.Load<AnimationLibrary>(emptyPath, ResourceLoader.CacheMode.IgnoreDeep); Check(nonempty.HasAnimation("new"), "An empty loaded library can grow and save.");
            Validation(clip);
        }
        finally { Directory.Delete(directory, true); }
        Console.WriteLine("Animation resource persistence, fresh-process playback, aliases, replacement, validation and atomic failures passed.");
    }
    private static Animation Create(Gradient gradient)
    {
        var clip = new Animation { Length = 1, Step = .125, LoopMode = SpriteFrames.LoopMode.PingPong, ResourceName = "motion" };
        var track = clip.AddTrack(Position); clip.TrackSetPath(track, "target:Position"); clip.TrackSetImported(track, true); clip.TrackSetInterpolationLoopWrap(track, false);
        for (var key = 0; key <= 32; key++) clip.TrackInsertKey(track, key / 32d, new Vector2(16 + key, 16));
        clip.Optimize(0, 0); Check(clip.TrackGetKeyCount(track) == 2, "Dense curve reduced before saving.");
        track = clip.AddTrack(Value, interpolate: Interpolate); clip.TrackSetPath(track, "target:Value"); clip.TrackInsertKey(track, 0, 0d); clip.TrackInsertKey(track, 1, 8d); clip.TrackSetKeyTransition(track, 0, 2);
        track = clip.AddBezierTrack(Height); clip.TrackSetPath(track, "target:Height"); clip.BezierTrackInsertKey(track, 0, 2, outHandle: new(1f / 3, 1)); clip.BezierTrackInsertKey(track, 1, 4, inHandle: new(-1f / 3, -1));
        track = clip.AddTrack(Gradient); clip.TrackSetPath(track, "target:Gradient"); clip.ValueTrackSetUpdateMode(track, Animation.UpdateMode.Discrete); clip.TrackInsertKey(track, 0, gradient); clip.TrackInsertKey(track, 1, gradient); clip.TrackSetEnabled(track, false);
        clip.AddMarker("scene/start", -1); clip.SetMarkerColor("scene/start", Colors.Red); clip.AddMarker("end:pose", 2); clip.SetMarkerColor("end:pose", Colors.Blue);
        return clip;
    }
    private static void Verify(AnimationLibrary library)
    {
        var clip = library.GetAnimation("move"); Check(ReferenceEquals(clip, library.GetAnimation("alias")), "Library aliases survive.");
        Check(clip.GetTrackCount() == 4 && clip.Length == 1 && clip.Step == .125 && clip.LoopMode == SpriteFrames.LoopMode.PingPong && clip.ResourceName == "motion", "Clip timing and identity metadata survive.");
        Check(clip.TrackGetKeyCount(0) == 2 && clip.TrackIsImported(0) && !clip.TrackGetInterpolationLoopWrap(0), "Track metadata survives.");
        Check(clip.TrackGetPath(0) == "target:Position" && clip.TrackGetKeyTime(0, 1) == 1 && clip.TrackGetKeyTransition(1, 0) == 2, "Path, time and transition survive.");
        Check(clip.TrackGetType(2) == Animation.TrackType.Bezier && clip.BezierTrackGetKeyOutHandle(2, 0).Y == 1, "Typed Bézier handles survive.");
        Check(!clip.TrackIsEnabled(3) && clip.ValueTrackGetUpdateMode(3) == Animation.UpdateMode.Discrete && ReferenceEquals(clip.TrackGetKeyValue<Gradient?>(3, 0), clip.TrackGetKeyValue<Gradient?>(3, 1)), "Resource keys and disabled/discrete metadata survive.");
        Check(clip.GetMarkerTime("scene/start") == -1 && clip.GetMarkerColor("scene/start") == Colors.Red && clip.GetMarkerTime("end:pose") == 2, "Marker names, times and colors survive.");
        Check(clip.ValueTrackInterpolate<double>(1, .5) == .5, "Registered custom interpolation and easing execute after loading.");
        var root = new Node(); var target = new Box { Name = "target" }; var player = new AnimationPlayer { CallbackModeProcess = AnimationMixer.AnimationCallbackModeProcess.Manual, PlaybackAutoCapture = false };
        root.AddChild(target); root.AddChild(player); using var tree = new SceneTree(root); player.AddAnimationLibrary("", library); player.Play("move"); player.Advance(.5);
        Check(target.Position == new Vector2(32, 16) && Math.Abs(target.Value - .5) < 1e-8 && target.Height > 2 && target.Height < 4, "Loaded property and Bézier tracks execute together.");
    }
    private static void Validation(Animation clip)
    {
        Reject<InvalidOperationException>(() => ResourceFileTypes.RegisterAnimationProperty("test.animation.position", Position));
        Reject<InvalidOperationException>(() => ResourceFileTypes.RegisterAnimationProperty("test.animation.other", Position));
        Reject<ArgumentException>(() => ResourceFileTypes.RegisterAnimationProperty("bad\nID", Position));
        Reject<ArgumentException>(() => ResourceFileTypes.RegisterAnimationProperty("readonly", new PropertyDescriptor<Box, double>("ReadOnly", n => n.Value)));
        var schemas = (PropertyDescriptor<Animation, string[]>)clip.GetPropertyList().Single(p => p.Name == "TrackSchemas");
        Reject<InvalidDataException>(() => schemas.SetValue(clip, ["0:missing"])); Reject<InvalidDataException>(() => schemas.SetValue(clip, ["6:test.animation.position"])); Check(clip.GetTrackCount() == 4, "Bad schema is transactional.");
        var times = (PropertyDescriptor<Animation, double[]>)clip.GetPropertyList().Single(p => p.Name == "Track[0].Times");
        Reject<InvalidDataException>(() => times.SetValue(clip, [0, 0])); Reject<InvalidDataException>(() => times.SetValue(clip, [0, double.NaN])); Check(clip.TrackGetKeyCount(0) == 2, "Bad times preserve keys.");
        var transitions = (PropertyDescriptor<Animation, double[]>)clip.GetPropertyList().Single(p => p.Name == "Track[0].Transitions");
        Reject<InvalidDataException>(() => transitions.SetValue(clip, [1])); Reject<InvalidDataException>(() => transitions.SetValue(clip, [1, double.NaN]));
    }
    private static void Child(string path, string mode)
    {
        var start = new ProcessStartInfo("dotnet") { RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false }; start.ArgumentList.Add(typeof(AnimationPersistenceTests).Assembly.Location);
        start.Environment["ELECTRON2D_TEST_ANIMATION_PERSISTENCE_CHILD"] = path; start.Environment["ELECTRON2D_TEST_ANIMATION_PERSISTENCE_MODE"] = mode; start.Environment.Remove("ELECTRON2D_TEST_ANIMATION_PERSISTENCE");
        using var child = Process.Start(start)!; var output = child.StandardOutput.ReadToEndAsync(); var error = child.StandardError.ReadToEndAsync();
        if (!child.WaitForExit(30000)) { child.Kill(true); throw new TimeoutException("Animation file child."); }
        Check(child.ExitCode == 0, "Fresh file process: " + output.GetAwaiter().GetResult() + error.GetAwaiter().GetResult());
    }
    internal static void RunChild(string path)
    {
        if (Environment.GetEnvironmentVariable("ELECTRON2D_TEST_ANIMATION_PERSISTENCE_MODE") == "unknown")
        { Reject<InvalidDataException>(() => ResourceLoader.Load<AnimationLibrary>(path, ResourceLoader.CacheMode.IgnoreDeep)); Console.WriteLine("Unknown binding rejected in fresh process."); return; }
        Register(); using var library = ResourceLoader.Load<AnimationLibrary>(path, ResourceLoader.CacheMode.IgnoreDeep); Verify(library); Console.WriteLine("Fresh animation graph restored and executed.");
    }
    internal static void RunNative(string path)
    {
        Register(); var backend = Environment.GetEnvironmentVariable("ELECTRON2D_ANIMATION_RENDERER") ?? "gpu"; var prior = ProjectSettings.Get(ProjectSettings.RenderingMethod); ProjectSettings.Set(ProjectSettings.RenderingMethod, backend);
        try
        {
            using var library = ResourceLoader.Load<AnimationLibrary>(path, ResourceLoader.CacheMode.IgnoreDeep);
            var window = new Window { Size = new(80, 64), Title = "Electron2D loaded animation" }; var target = new Box { Name = "target" }; var player = new AnimationPlayer { CallbackModeProcess = AnimationMixer.AnimationCallbackModeProcess.Manual, PlaybackAutoCapture = false };
            player.AddAnimationLibrary("", library); window.AddChild(target); window.AddChild(player); var stage = 0;
            target.Start = () => { Check(RenderingServer.GetCurrentRenderingMethod() == backend, "Actual renderer without fallback."); RenderingServer.SetDefaultClearColor(Colors.Black); player.Play("move"); player.Advance(0); RenderingServer.FramePostDraw += () => { using var image = RenderingServer.Service!.Readback(); var x = stage == 0 ? 16 : stage == 1 ? 32 : 48; Check(image.GetPixel(x, 16).R > .9f && image.GetPixel(x, 16).G < .1f, "Loaded pose reaches real pixels."); stage++; if (stage == 3) window.Tree!.Quit(); else player.Advance(.5); }; };
            Check(Engine.Run(window) == 0 && stage == 3 && window.IsDisposed && !library.IsDisposed, "Loaded host cleanup and borrowing.");
            Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(new { scenario = "loaded-animation", backend, stages = stage, cleaned = window.IsDisposed }));
        }
        finally { ProjectSettings.Set(ProjectSettings.RenderingMethod, prior); }
    }
    internal static void WriteFixture(string path) { Register(); using var gradient = new Gradient(); using var clip = Create(gradient); using var library = new AnimationLibrary(); library.AddAnimation("move", clip); library.AddAnimation("alias", clip); ResourceSaver.Save(library, path, SaverFlags.BundleResources); }
    private sealed class Box : Entity { internal double Value; internal float Height; internal Gradient? Gradient; internal Action? Start; protected override void OnReady() => Start?.Invoke(); protected override void OnDraw() => DrawRect(new(-3, -3, 6, 6), Colors.Red); }
    private static void Check(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
    private static void Reject<T>(Action action) where T : Exception { try { action(); } catch (T) { return; } throw new InvalidOperationException($"Expected {typeof(T).Name}."); }
}
