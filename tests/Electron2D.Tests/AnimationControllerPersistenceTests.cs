using Electron2D;
using System.Diagnostics;
using Path = System.IO.Path;

internal static class AnimationControllerPersistenceTests
{
    private static readonly PropertyDescriptor<Entity, Vector2> Position = new(nameof(Entity.Position), n => n.Position, (n, v) => n.Position = v);
    private static bool _registered;
    private static void Register()
    {
        if (_registered) return;
        ResourceFileTypes.RegisterAnimationProperty("test.controllers.position", Position);
        ResourceFileTypes.RegisterNode("test.controllers.Box", CreateBox); _registered = true;
    }
    private static Box CreateBox() => new();
    private sealed class Box : Entity
    {
        internal Action? Start;
        protected override Func<Node> CreateSceneInstanceFactory() => CreateBox;
        protected override void OnReady() => Start?.Invoke();
        protected override void OnDraw() => DrawRect(new(0, 0, 8, 8), Colors.Red);
    }
    private sealed class DerivedPlayer : AnimationPlayer { }
    private sealed class CaptureProbe(AnimationPlayer player) : Node
    {
        private readonly AnimationPlayer _player = player;
        protected override Func<Node> CreateSceneInstanceFactory() => static () => new Node();
        protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors()
        {
            foreach (var p in base.GetPropertyDescriptors()) yield return p;
            yield return new PropertyDescriptor<CaptureProbe, int>("Guard", n => { n._player.Advance(0); return 0; }, (_, _) => { }, stored: true);
        }
    }
    internal static void Run()
    {
        Register(); var dir = Path.Combine(Path.GetTempPath(), "e2d-controller-" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(dir);
        try
        {
            var path = Path.Combine(dir, "scene.e2dscene"); WriteFixture(path);
            using var scene = ResourceLoader.Load<PackedScene>(path, ResourceLoader.CacheMode.IgnoreDeep); Verify(scene);
            Child(path, false); Child(path, true); Validation();
        }
        finally { Directory.Delete(dir, true); }
        Console.WriteLine("Controller files: scene state, aliases, scheduling, transitions, fresh processes, guards and ownership passed.");
    }
    internal static void WriteFixture(string path)
    {
        Register(); using var a = Clip(new(16, 16), new(32, 16)); using var b = Clip(new(32, 16), new(48, 16));
        using var library = new AnimationLibrary(); library.AddAnimation("a", a); library.AddAnimation("alias", a); library.AddAnimation("b", b);
        using var root = new Node { Name = "root" }; var target = new Box { Name = "target" }; var player = new AnimationPlayer { Name = "player", Autoplay = "a", PlaybackAutoCapture = false, PlaybackDefaultBlendTime = .25, PlaybackAutoCaptureDuration = .33, PlaybackAutoCaptureTransitionType = Tween.TransitionType.Sine, PlaybackAutoCaptureEaseType = Tween.EaseType.Out, SpeedScale = 2, CallbackModeMethod = AnimationMixer.AnimationCallbackModeMethod.Immediate, AudioMaxPolyphony = 8, Deterministic = true };
        var mixer = new AnimationMixer { Name = "mixer", Active = false, RootNode = "../target", CallbackModeProcess = AnimationMixer.AnimationCallbackModeProcess.Physics, CallbackModeDiscrete = AnimationMixer.AnimationCallbackModeDiscrete.Dominant };
        player.AddAnimationLibrary("", library); mixer.AddAnimationLibrary("shared", library); player.AnimationSetNext("a", "b"); player.SetBlendTime("a", "b", .5);
        root.AddChild(target); root.AddChild(player); root.AddChild(mixer); target.Owner = player.Owner = mixer.Owner = root;
        player.PlaySection("a", .1, .9); player.Advance(.2); player.Queue("alias");
        using var packed = new PackedScene(); packed.Pack(root);
        Check(player.IsPlaying() && player.GetQueue().Length == 1 && player.CurrentAnimationPosition == .5, "Capture preserves live playback and queue.");
        ResourceSaver.Save(packed, path, SaverFlags.BundleResources | SaverFlags.SaveBigEndian);
    }
    private static Animation Clip(Vector2 from, Vector2 to)
    {
        var a = new Animation { Length = 1 }; var t = a.AddTrack(Position); a.TrackSetPath(t, "target:Position"); a.TrackInsertKey(t, 0, from); a.TrackInsertKey(t, 1, to); return a;
    }
    private static void Verify(PackedScene packed)
    {
        using var root = packed.Instantiate(); var p = (AnimationPlayer)root.GetNode("player"); var m = (AnimationMixer)root.GetNode("mixer"); var target = (Box)root.GetNode("target");
        Check(!p.IsPlaying() && p.AssignedAnimation == "" && p.GetQueue().Length == 0 && !p.HasSection(), "Transient playback is not stored.");
        Check(ReferenceEquals(p.GetAnimationLibrary(""), m.GetAnimationLibrary("shared")) && ReferenceEquals(p.GetAnimation("a"), p.GetAnimation("alias")), "Scene resource aliases survive.");
        Check(p.Autoplay == "a" && p.SpeedScale == 2 && p.PlaybackDefaultBlendTime == .25 && p.PlaybackAutoCaptureDuration == .33 && p.PlaybackAutoCaptureTransitionType == Tween.TransitionType.Sine && p.PlaybackAutoCaptureEaseType == Tween.EaseType.Out && !p.PlaybackAutoCapture && p.AudioMaxPolyphony == 8 && p.Deterministic && p.CallbackModeMethod == AnimationMixer.AnimationCallbackModeMethod.Immediate, "Player authored options survive.");
        Check(!m.Active && m.RootNode == "../target" && m.CallbackModeProcess == AnimationMixer.AnimationCallbackModeProcess.Physics && m.CallbackModeDiscrete == AnimationMixer.AnimationCallbackModeDiscrete.Dominant, "Mixer authored options survive.");
        Check(p.AnimationGetNext("a") == "b" && p.GetBlendTime("a", "b") == .5, "Next and transition durations survive.");
        using var tree = new SceneTree(root); Check(p.IsPlaying() && p.AssignedAnimation == "a", "Loaded Ready starts autoplay.");
        tree.ProcessFrame(.25); Check(target.Position == new Vector2(24, 16), "Loaded idle schedule reaches target.");
        p.CallbackModeProcess = AnimationMixer.AnimationCallbackModeProcess.Manual; tree.ProcessFrame(.25); Check(target.Position == new Vector2(24, 16), "Manual schedule stays put.");
        p.Advance(.25); Check(p.AssignedAnimation == "b", "Loaded next chain executes."); p.Advance(.25); p.Advance(.25); Check(target.Position.X >= 32, "Loaded named transition advances.");
        var borrowed = p.GetAnimationLibrary(""); tree.Dispose(); Check(!borrowed.IsDisposed, "Loaded template retains its resource graph after instance teardown.");
    }
    private static void Validation()
    {
        using var a = Clip(default, Vector2.One); using var lib = new AnimationLibrary(); lib.AddAnimation("a", a); using var p = new AnimationPlayer();
        var notices = 0; p.PropertyListChanged += _ => notices++; p.AddAnimationLibrary("", lib); p.RenameAnimationLibrary("", "renamed"); p.RenameAnimationLibrary("renamed", "");
        Check(notices == 3, "Library edits notify dynamic stored schema."); p.SetBlendTime("a", "a", .1); p.SetBlendTime("a", "a", 0); Check(notices == 5, "Blend edits notify indexed stored schema.");
        var names = (PropertyDescriptor<AnimationMixer, string[]>)p.GetPropertyList().Single(d => d.Name == "LibraryNames");
        Reject<InvalidDataException>(() => names.SetValue(p, ["x", "x"])); Reject<InvalidDataException>(() => names.SetValue(p, ["bad/name"])); Check(ReferenceEquals(p.GetAnimationLibrary(""), lib), "Invalid library names preserve state.");
        var next = (PropertyDescriptor<AnimationPlayer, string[]>)p.GetPropertyList().Single(d => d.Name == "NextAnimations");
        Reject<InvalidDataException>(() => next.SetValue(p, ["a"])); Reject<InvalidDataException>(() => next.SetValue(p, ["a", "a", "a", "a"]));
        using var packed = new PackedScene(); using var derived = new DerivedPlayer(); Reject<NotSupportedException>(() => packed.Pack(derived));
        using var root = new Node(); var guarded = new AnimationPlayer(); root.AddChild(guarded); guarded.Owner = root; var probe = new CaptureProbe(guarded); root.AddChild(probe); probe.Owner = root;
        Reject<InvalidOperationException>(() => packed.Pack(root)); Check(!packed.CanInstantiate(), "Playback cannot mutate captured hierarchy."); guarded.Advance(0);
        packed.Pack(p); var data = packed.FileData;
        foreach (var fault in new[] { "NextAnimations", "BlendPairs", "LibraryNames" })
        {
            var properties = data.Nodes[0].Properties.Select(v => v.Name == fault ? new ScenePropertyData(fault, new StoredPropertyValue<string[]>(fault == "NextAnimations" ? ["a", "missing"] : fault == "BlendPairs" ? ["a", "a"] : ["", "missing"])) : v).ToArray();
            using var bad = new PackedScene(); bad.LoadFileData(new([data.Nodes[0] with { Properties = properties }]));
            Reject<InvalidDataException>(() => bad.Instantiate()); Check(!lib.IsDisposed, "Failed restoration preserves borrowed library.");
        }
        var blend = (PropertyDescriptor<AnimationPlayer, string[]>)p.GetPropertyList().Single(d => d.Name == "BlendPairs");
        Reject<InvalidDataException>(() => blend.SetValue(p, ["b", "a", "a", "b"]));
        blend.SetValue(p, ["a", "a"]); var duration = (PropertyDescriptor<AnimationPlayer, double>)p.GetPropertyList().Single(d => d.Name == "Blend[0]");
        Reject<InvalidDataException>(() => duration.SetValue(p, double.NaN)); Reject<InvalidDataException>(() => duration.SetValue(p, 0)); duration.SetValue(p, .5);
        lib.RemoveAnimation("a"); Reject<KeyNotFoundException>(() => packed.Pack(p)); Check(!packed.CanInstantiate(), "Stale blend reference cannot capture a non-loadable scene.");
        using var treeNode = new AnimationTree(); packed.Pack(treeNode); using var emptyTree = packed.Instantiate(); Check(emptyTree is AnimationTree, "Empty graph controller packs with its authored schema.");
    }
    private static void Child(string path, bool unknown)
    {
        var start = new ProcessStartInfo(Environment.ProcessPath!) { RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false }; if (System.IO.Path.GetFileNameWithoutExtension(start.FileName).Equals("dotnet", StringComparison.OrdinalIgnoreCase)) start.ArgumentList.Add(typeof(AnimationControllerPersistenceTests).Assembly.Location);
        start.Environment.Remove("ELECTRON2D_TEST_CONTROLLER_FILE"); start.Environment["ELECTRON2D_TEST_CONTROLLER_FILE_CHILD"] = path; start.Environment["ELECTRON2D_TEST_CONTROLLER_UNKNOWN"] = unknown ? "1" : "0";
        using var child = Process.Start(start)!; var output = child.StandardOutput.ReadToEndAsync(); var error = child.StandardError.ReadToEndAsync();
        if (!child.WaitForExit(30000)) { child.Kill(true); throw new TimeoutException("Controller file child."); }
        Check(child.ExitCode == 0, output.GetAwaiter().GetResult() + error.GetAwaiter().GetResult());
    }
    internal static void RunChild(string path)
    {
        if (Environment.GetEnvironmentVariable("ELECTRON2D_TEST_CONTROLLER_UNKNOWN") == "1") { Reject<InvalidDataException>(() => ResourceLoader.Load<PackedScene>(path, ResourceLoader.CacheMode.IgnoreDeep)); return; }
        Register(); using var scene = ResourceLoader.Load<PackedScene>(path, ResourceLoader.CacheMode.IgnoreDeep); Verify(scene);
    }
    internal static void RunNative(string path)
    {
        Register(); var backend = Environment.GetEnvironmentVariable("ELECTRON2D_ANIMATION_RENDERER") ?? "gpu"; var prior = ProjectSettings.Get(ProjectSettings.RenderingMethod); ProjectSettings.Set(ProjectSettings.RenderingMethod, backend);
        try
        {
            using var packed = ResourceLoader.Load<PackedScene>(path, ResourceLoader.CacheMode.IgnoreDeep); var root = packed.Instantiate(); var p = (AnimationPlayer)root.GetNode("player"); var target = (Box)root.GetNode("target");
            p.CallbackModeProcess = AnimationMixer.AnimationCallbackModeProcess.Manual; var window = new Window { Size = new(80, 64), Title = "Electron2D loaded scene controllers" }; window.AddChild(root); var stage = 0;
            target.Start = () => { Check(RenderingServer.GetCurrentRenderingMethod() == backend, "Actual requested renderer."); RenderingServer.SetDefaultClearColor(Colors.Black); };
            root.Ready += _ => { Check(p.IsPlaying(), "Loaded scene autoplays without host reconstruction."); p.Advance(0); RenderingServer.FramePostDraw += () => { using var image = RenderingServer.Service!.Readback(); var x = stage == 0 ? 16 : stage == 1 ? 24 : 32; Check(image.GetPixel(x, 16).R > .9f, "Loaded controller pose reaches pixels."); stage++; if (stage == 3) window.Tree!.Quit(); else p.Advance(.25); }; };
            Check(Engine.Run(window) == 0 && stage == 3 && window.IsDisposed, "Loaded scene host cleanup."); Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(new { scenario = "loaded-controller-scene", backend, stages = stage, cleaned = window.IsDisposed }));
        }
        finally { ProjectSettings.Set(ProjectSettings.RenderingMethod, prior); }
    }
    private static void Check(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
    private static void Reject<T>(Action action) where T : Exception { try { action(); } catch (T) { return; } throw new InvalidOperationException("Expected " + typeof(T).Name); }
}
