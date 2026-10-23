using Electron2D;
using System.Diagnostics;
using Path = System.IO.Path;

internal static class AnimationResetSaveTests
{
    private static readonly PropertyDescriptor<Box, double> Gain = new(nameof(Box.Gain), n => n.Gain, (n, v) => n.Gain = v, stored: true);
    private static bool _registered;
    private static bool _failReset;
    private static int _ready;
    private static int _events, _constructed;
    private static Action<Box>? _hook;
    private static readonly Action<Box, int> Event = static (_, _) => _events++;
    private static int _localSetup;
    private static Memo CreateMemo() => new();
    private sealed class Memo : Resource
    {
        internal int Value;
        protected override Resource CreateDuplicateInstance() => new Memo();
        protected override void CopyCustomStateTo(Resource target, bool deep, DeepDuplicateMode mode, Func<Resource?, Resource?> duplicate, Func<Resource?, Resource?> force) => ((Memo)target).Value = Value;
        protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors() => base.GetPropertyDescriptors().Append(new PropertyDescriptor<Memo, int>("Value", n => n.Value, (n, v) => n.Value = v, stored: true));
        protected override void OnSetupLocalToScene() => _localSetup++;
    }
    private static Box CreateBox() => new();
    private static void Register()
    {
        if (_registered) return;
        ResourceFileTypes.RegisterNode("test.reset.Box", CreateBox); ResourceFileTypes.RegisterResource("test.reset.Memo", CreateMemo); ResourceFileTypes.RegisterAnimationProperty("test.reset.gain", Gain); ResourceFileTypes.RegisterAnimationMethod("test.reset.event", Event); _registered = true;
    }
    private sealed class Box : Entity
    {
        internal Box() => _constructed++;
        private double _gain;
        internal Memo? State;
        internal double Gain { get => _gain; set { if (_failReset && value == 16) throw new ApplicationException("RESET setter fixture"); if (value == 16) _hook?.Invoke(this); _gain = value; if (State is not null) State.Value = (int)value; Position = new((float)value, 16); } }
        protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors() => base.GetPropertyDescriptors().Append(AnimationResetSaveTests.Gain).Append(new PropertyDescriptor<Box, Memo?>("State", n => n.State, (n, v) => n.State = v, stored: true));
        protected override Func<Node> CreateSceneInstanceFactory() => CreateBox;
        protected override void OnReady() => _ready++;
        protected override void OnDraw() => DrawRect(new(0, 0, 6, 6), Colors.Red);
    }
    private static (Node Root, Box Target, AnimationMixer Mixer, Animation Reset, AnimationLibrary Library) Make(int kind)
    {
        var root = new Node(); var target = new Box { Name = "target", State = new Memo { Value = 40, ResourceLocalToScene = true }, Gain = 40 }; AnimationMixer mixer = kind switch { 1 => new AnimationPlayer(), 2 => new AnimationTree(), _ => new AnimationMixer() }; mixer.Name = "mixer"; mixer.CallbackModeProcess = AnimationMixer.AnimationCallbackModeProcess.Manual;
        root.AddChild(target); root.AddChild(mixer); target.Owner = mixer.Owner = root;
        var reset = new Animation(); var track = reset.AddTrack(Gain); reset.TrackSetPath(track, "target:Gain"); reset.TrackInsertKey(track, 0, 16d); var bezier = reset.AddBezierTrack(Gain); reset.TrackSetPath(bezier, "target:Gain"); reset.BezierTrackInsertKey(bezier, 0, 16); var eventTrack = reset.AddMethodTrack<Box>(); reset.TrackSetPath(eventTrack, "target"); reset.TrackInsertKey<AnimationMethodKey<Box>>(eventTrack, 0, new AnimationMethodKey<Box, int>("event", Event, 1)); var library = new AnimationLibrary(); library.AddAnimation("RESET", reset); mixer.AddAnimationLibrary("", library);
        return (root, target, mixer, reset, library);
    }
    internal static void Run()
    {
        Register(); var ready = _ready; var events = _events;
        for (var kind = 0; kind < 3; kind++)
        {
            var f = Make(kind); using var root = f.Root; using var memo = f.Target.State; using var reset = f.Reset; using var library = f.Library; using var packed = new PackedScene();
            Check(f.Mixer.ResetOnSave, "Default RESET save policy."); packed.Pack(root); using (var actual = packed.Instantiate()) Check(((Box)actual.GetNode("target")).Gain == 40, "Ordinary Pack preserves actual pose.");
            var setupBefore = _localSetup; packed.PackForSave(root); Check(_localSetup == setupBefore && f.Target.State!.Value == 40, "Authoring isolates mutable resources and skips local setup callbacks."); Check(f.Target.Gain == 40 && f.Target.Position == new Vector2(40, 16), "Preview and coupled position are unchanged.");
            using (var saved = packed.Instantiate()) Check(((Box)saved.GetNode("target")).Gain == 16 && ((Box)saved.GetNode("target")).Position == new Vector2(16, 16) && ((Box)saved.GetNode("target")).State!.Value == 16, $"RESET kind={kind}, gain={((Box)saved.GetNode("target")).Gain}, position={((Box)saved.GetNode("target")).Position}, state={((Box)saved.GetNode("target")).State!.Value}.");
            f.Mixer.ResetOnSave = false; var constructed = _constructed; packed.PackForSave(root); Check(_constructed == constructed, "Disabled save skips reconstruction."); using (var saved = packed.Instantiate()) Check(((Box)saved.GetNode("target")).Gain == 40 && !((AnimationMixer)saved.GetNode("mixer")).ResetOnSave, "Disabled policy executes and stores.");
            f.Mixer.ResetOnSave = true; _failReset = true; try { Reject<ApplicationException>(() => packed.PackForSave(root)); } finally { _failReset = false; }
            using (var old = packed.Instantiate()) Check(((Box)old.GetNode("target")).Gain == 40, "Failed preparation preserves prior packed data."); Check(f.Target.Gain == 40, "Failed setter preserves live preview.");
        }
        Check(_events == events, "RESET save suppresses method event tracks.");
        Guards();
        Check(_ready == ready, "Authoring never activates scene lifecycle.");
        var directory = Path.Combine(Path.GetTempPath(), "e2d-reset-" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(directory);
        try { var path = Path.Combine(directory, "scene.e2dscene"); WriteFixture(path); Child(path); }
        finally { Directory.Delete(directory, true); }
        Console.WriteLine("RESET save pose: all controller families, coupled properties, ordinary/disabled policies, failure atomicity, detached authoring and fresh files passed.");
    }
    private static void Guards()
    {
        var f = Make(1); using var root = f.Root; using var memo = f.Target.State; using var reset = f.Reset; using var library = f.Library; using var packed = new PackedScene();
        packed.Pack(root); _hook = _ => root.Name = "changed";
        try { Reject<InvalidOperationException>(() => packed.PackForSave(root)); } finally { _hook = null; }
        Check(root.Name != "changed" && f.Target.Gain == 40, "Source hierarchy guard survives callback attempts.");
        _hook = n => n.Name = "changed";
        try { packed.PackForSave(root); } finally { _hook = null; }
        using (var renamed = packed.Instantiate()) Check(renamed.GetNode("changed") is Box && f.Target.Name == "target", "Coupled name changes persist while preview identity remains unchanged.");
        _hook = n => { var extra = new Node { Name = "extra" }; n.Parent!.AddChild(extra); extra.Owner = n.Parent; };
        try { Reject<InvalidOperationException>(() => packed.PackForSave(root)); } finally { _hook = null; }
        Check(f.Target.Name == "target" && root.GetChildCount() == 2, "Temporary topology mutation rejects without altering preview.");
        using var moving = new Animation { Length = 2 }; var t = moving.AddTrack(Gain); moving.TrackSetPath(t, "target:Gain"); moving.TrackInsertKey(t, 0, 40d); moving.TrackInsertKey(t, 2, 80d); library.AddAnimation("move", moving);
        var player = (AnimationPlayer)f.Mixer; player.Play("move"); player.Advance(.5); player.Queue("move"); var preview = f.Target.Gain; var clock = player.CurrentAnimationPosition; var queued = player.GetQueue(); packed.PackForSave(root);
        Check(f.Target.Gain == preview && player.IsPlaying() && player.CurrentAnimationPosition == clock && player.GetQueue().SequenceEqual(queued), "Preview playback, clock and queue remain intact.");
        f.Target.Gain = 40;
        f.Mixer.Active = false; packed.PackForSave(root); using (var saved = packed.Instantiate()) Check(((Box)saved.GetNode("target")).Gain == 16 && !((AnimationMixer)saved.GetNode("mixer")).Active, "Inactive controller still supplies authored RESET pose.");
        library.RemoveAnimation("RESET"); var count = _constructed; packed.PackForSave(root); Check(_constructed == count, "Missing RESET bypasses reconstruction.");
        using (var saved = packed.Instantiate()) Check(((Box)saved.GetNode("target")).Gain == 40, "Missing RESET preserves actual pose.");
    }
    internal static void WriteFixture(string path)
    {
        Register(); var f = Make(1); using var root = f.Root; using var memo = f.Target.State; using var reset = f.Reset; using var library = f.Library; using var packed = new PackedScene(); packed.PackForSave(root); root.Dispose(); library.Dispose(); reset.Dispose(); memo!.Dispose();
        using (var saved = packed.Instantiate()) Check(((Box)saved.GetNode("target")).Gain == 16, "Prepared scene owns isolated assets after preview cleanup.");
        ResourceSaver.Save(packed, path, SaverFlags.BundleResources | SaverFlags.SaveBigEndian);
    }
    private static void Child(string path)
    {
        var start = new ProcessStartInfo(Environment.ProcessPath!) { RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false }; if (System.IO.Path.GetFileNameWithoutExtension(start.FileName).Equals("dotnet", StringComparison.OrdinalIgnoreCase)) start.ArgumentList.Add(typeof(AnimationResetSaveTests).Assembly.Location); start.Environment.Remove("ELECTRON2D_TEST_RESET_SAVE"); start.Environment["ELECTRON2D_TEST_RESET_SAVE_CHILD"] = path;
        using var child = Process.Start(start)!; var output = child.StandardOutput.ReadToEndAsync(); var error = child.StandardError.ReadToEndAsync(); if (!child.WaitForExit(30000)) { child.Kill(true); throw new TimeoutException("RESET save child."); }
        Check(child.ExitCode == 0, output.GetAwaiter().GetResult() + error.GetAwaiter().GetResult());
    }
    internal static void RunChild(string path)
    {
        Register(); using var packed = ResourceLoader.Load<PackedScene>(path, ResourceLoader.CacheMode.IgnoreDeep); using var root = packed.Instantiate(); Check(((Box)root.GetNode("target")).Gain == 16 && ((Box)root.GetNode("target")).Position == new Vector2(16, 16), "Fresh process saved RESET/coupled pose.");
    }
    internal static void RunNative(string path)
    {
        Register(); var backend = Environment.GetEnvironmentVariable("ELECTRON2D_ANIMATION_RENDERER") ?? "gpu"; var prior = ProjectSettings.Get(ProjectSettings.RenderingMethod); ProjectSettings.Set(ProjectSettings.RenderingMethod, backend);
        try
        {
            using var packed = ResourceLoader.Load<PackedScene>(path, ResourceLoader.CacheMode.IgnoreDeep); var root = packed.Instantiate(); var window = new Window { Size = new(80, 64), Title = "Electron2D RESET save pose" }; window.AddChild(root); var checkedPixels = false;
            root.Ready += _ => { Check(RenderingServer.GetCurrentRenderingMethod() == backend, "Actual renderer."); RenderingServer.SetDefaultClearColor(Colors.Black); RenderingServer.FramePostDraw += () => { using var image = RenderingServer.Service!.Readback(); Check(image.GetPixel(16, 16).R > .9f && image.GetPixel(40, 16).R < .1f, "Saved RESET pose reaches pixels, preview pose absent."); checkedPixels = true; window.Tree!.Quit(); }; };
            Check(Engine.Run(window) == 0 && checkedPixels && window.IsDisposed, "Saved RESET host and cleanup."); Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(new { scenario = "saved-reset-pose", backend, checkedPixels, cleaned = window.IsDisposed }));
        }
        finally { ProjectSettings.Set(ProjectSettings.RenderingMethod, prior); }
    }
    private static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    private static void Reject<T>(Action action) where T : Exception { try { action(); } catch (T) { return; } throw new InvalidOperationException("Expected " + typeof(T).Name); }
}
