using Electron2D;

internal static class AnimationSpecialTrackTests
{
    private static readonly PropertyDescriptor<Probe, double> X = new("X", n => n.X, (n, v) => n.X = v);
    private static readonly Action<Probe, int> Mark = static (n, v) => { n.Calls.Add(v); n.Hook?.Invoke(); };
    internal static void Run() { Bezier(); Methods(); MixedKeysAndFailures(); LoopsAndGraph(); FilteredMixing(); Failures(); Warm(); Console.WriteLine("Typed Bézier/method tracks: authoring, curves, traversal/loops/seeks/sections, deferred/immediate/capacity, graph/filter/callback lifetime and warm allocation checks passed."); }
    private static void Bezier()
    {
        using var clip = new Animation { Length = 2 }; var track = clip.AddBezierTrack(X); Check(clip.TrackGetType(track) == Animation.TrackType.Bezier && clip.FindTrack("", Animation.TrackType.Value) == -1, "Bézier identity and FindTrack kind filtering."); clip.TrackSetPath(track, "target:X"); clip.BezierTrackInsertKey(track, 0, 0, new(1, 2), new(-1, 3)); clip.BezierTrackInsertKey(track, 2, 20); Check(clip.BezierTrackGetKeyInHandle(track, 0).X == 0 && clip.BezierTrackGetKeyOutHandle(track, 0).X == 0, "Handle time signs clamp.");
        clip.BezierTrackSetKeyOutHandle(track, 0, new(2f / 3, 0)); clip.BezierTrackSetKeyInHandle(track, 1, new(-2f / 3, 0)); Near(clip.BezierTrackInterpolate(track, 1), 10); Near(clip.BezierTrackInterpolate(track, .5), 3.125); Near(clip.BezierTrackInterpolate(track, -1), 0); Near(clip.BezierTrackInterpolate(track, 3), 20);
        var key = clip.TrackGetKeyValue<AnimationBezierKey>(track, 0); clip.TrackSetKeyValue(track, 0, key with { Value = 4 }); Near(clip.BezierTrackGetKeyValue(track, 0), 4); clip.TrackSetKeyTime(track, 0, .25); Check(clip.TrackGetKeyTime(track, 0) == .25, "Typed key movement."); using var copy = (Animation)clip.Duplicate(); clip.BezierTrackSetKeyValue(track, 0, 0); Near(copy.BezierTrackGetKeyValue(track, 0), 4); Reject<InvalidOperationException>(() => clip.ValueTrackSetUpdateMode(track, Animation.UpdateMode.Discrete)); Reject<ArgumentOutOfRangeException>(() => clip.BezierTrackSetKeyOutHandle(track, 0, new(float.NaN, 0)));
        using var setup = new Setup(clip); setup.Player.Play("a"); setup.Player.Seek(1, true); Near(setup.Target.X, clip.BezierTrackInterpolate(track, 1));
        using var wrong = new Animation(); var raw = wrong.AddTrack(new PropertyDescriptor<Probe, AnimationBezierKey>("raw", _ => default, (_, _) => { })); Reject<InvalidOperationException>(() => wrong.BezierTrackInsertKey(raw, 0, 1));
        using var outside = new Animation { Length = 1 }; var t = outside.AddBezierTrack(X); outside.BezierTrackInsertKey(t, 2, 10); Near(outside.BezierTrackInterpolate(t, 3), 0);
    }
    private static void Methods()
    {
        using var clip = Events(); using var setup = new Setup(clip); setup.Player.CallbackModeMethod = AnimationMixer.AnimationCallbackModeMethod.Immediate; setup.Player.Play("a"); setup.Player.Advance(.5); Sequence(setup.Target.Calls, 0, 1, 2); Check(clip.MethodTrackGetName(0, 1) == "mark" && clip.MethodTrackGetParams<int>(0, 1) == 1, "Typed callback metadata."); Reject<InvalidCastException>(() => clip.MethodTrackGetParams<string>(0, 1));
        setup.Target.Calls.Clear(); setup.Player.Advance(0); Sequence(setup.Target.Calls); setup.Player.Seek(.4, true); Sequence(setup.Target.Calls, 1); setup.Target.Calls.Clear(); setup.Player.Seek(.8, true, true); Sequence(setup.Target.Calls); setup.Player.Stop(); Sequence(setup.Target.Calls);
        setup.Player.PlayBackwards("a"); setup.Player.Advance(.5); Sequence(setup.Target.Calls, 4, 3, 2); setup.Target.Calls.Clear(); setup.Player.Stop(true); setup.Player.PlaySection("a", .25, .75); setup.Player.Advance(.5); Sequence(setup.Target.Calls, 1, 2, 3);
        setup.Target.Calls.Clear(); setup.Player.CallbackModeMethod = AnimationMixer.AnimationCallbackModeMethod.Deferred; setup.Player.Stop(true); setup.Player.Play("a"); setup.Player.Advance(.5); Sequence(setup.Target.Calls); setup.Scene.FlushDeferred(); Sequence(setup.Target.Calls, 0, 1, 2);
    }
    private static void MixedKeysAndFailures()
    {
        using var clip = Events(); clip.TrackSetKeyValue(0, 1, new AnimationMethodKey<Probe, string>("text", static (n, v) => n.Calls.Add(v.Length), "hello")); clip.TrackSetKeyValue(0, 2, new AnimationMethodKey<Probe, (int A, int B)>("sum", static (n, v) => n.Calls.Add(v.A + v.B), (7, 8)));
        Check(clip.TrackGetKeyValue<AnimationMethodKey<Probe, string>>(0, 1).Arguments == "hello" && clip.MethodTrackGetParams<(int A, int B)>(0, 2) == (7, 8), "Mixed exact payload signatures in one receiver track."); Reject<InvalidCastException>(() => clip.TrackGetKeyValue<int>(0, 1)); Reject<InvalidCastException>(() => clip.TrackGetKeyValue<object>(0, 1)); Reject<ArgumentException>(() => clip.TrackSetPath(0, "target:X"));
        using var copy = (Animation)clip.Duplicate(true); using var setup = new Setup(copy); setup.Player.Play("a"); setup.Player.Advance(.5); copy.TrackSetKeyValue(0, 1, new AnimationMethodKey<Probe, int>("edited", Mark, 99)); setup.Player.ClearCaches(); setup.Scene.FlushDeferred(); Sequence(setup.Target.Calls, 0, 5, 15);
        setup.Target.Calls.Clear(); setup.Player.CallbackModeMethod = AnimationMixer.AnimationCallbackModeMethod.Immediate; setup.Target.Hook = () => copy.Clear(); setup.Player.Stop(true); setup.Player.Play("a"); setup.Player.Advance(1); Sequence(setup.Target.Calls, 0); setup.Target.Hook = null;
        using var failingClip = Events(); using var failing = new Setup(failingClip); failing.Target.Hook = () => throw new InvalidOperationException("user callback"); failing.Player.Play("a"); failing.Player.Advance(.5); Reject<AggregateException>(() => failing.Scene.FlushDeferred()); failing.Target.Hook = null; failing.Target.Calls.Clear(); failing.Player.Stop(true); failing.Player.Play("a"); failing.Player.Advance(.5); failing.Scene.FlushDeferred(); Sequence(failing.Target.Calls, 0, 1, 2);
        failing.Target.Calls.Clear(); failing.Player.CallbackModeMethod = AnimationMixer.AnimationCallbackModeMethod.Immediate; failing.Target.Hook = () => throw new InvalidOperationException("user callback"); failing.Player.Stop(true); failing.Player.Play("a"); Reject<InvalidOperationException>(() => failing.Player.Advance(.5)); Check(!failing.Player.IsPlaying(), "Immediate callback failure pauses unchanged playback.");
    }
    private static void LoopsAndGraph()
    {
        using var clip = Events(); clip.LoopMode = SpriteFrames.LoopMode.Linear; using var setup = new Setup(clip); setup.Player.CallbackModeMethod = AnimationMixer.AnimationCallbackModeMethod.Immediate; setup.Player.Play("a"); setup.Player.Advance(2.25); Sequence(setup.Target.Calls, 0, 1, 2, 3, 4, 0, 1, 2, 3, 4, 0, 1);
        setup.Target.Calls.Clear(); clip.LoopMode = SpriteFrames.LoopMode.PingPong; setup.Player.Stop(true); setup.Player.Play("a"); setup.Player.Advance(1.5); Sequence(setup.Target.Calls, 0, 1, 2, 3, 4, 3, 2);
        using var leaf = new AnimationNodeAnimation { Animation = "a" }; setup.Player.Active = false; var tree = new AnimationTree { TreeRoot = leaf, CallbackModeMethod = AnimationMixer.AnimationCallbackModeMethod.Immediate, CallbackModeProcess = AnimationMixer.AnimationCallbackModeProcess.Manual }; tree.AddAnimationLibrary("", setup.Library); setup.Root.AddChild(tree); setup.Target.Calls.Clear(); tree.Advance(0); tree.Advance(.75); tree.Advance(.5); Sequence(setup.Target.Calls, 0, 1, 2, 3, 4, 3); tree.Dispose();
    }
    private static void FilteredMixing()
    {
        using var first = new Animation { Length = 1 }; var a = first.AddBezierTrack(X); first.TrackSetPath(a, "target:X"); first.BezierTrackInsertKey(a, 0, 10);
        using var second = Events(); var b = second.AddBezierTrack(X); second.TrackSetPath(b, "target:X"); second.BezierTrackInsertKey(b, 0, 30); using var setup = new Setup(first); setup.Library.AddAnimation("b", second); setup.Player.Active = false;
        using var graph = new AnimationNodeBlendTree(); using var mix = new AnimationNodeBlend2 { Sync = true, FilterEnabled = true }; mix.SetFilterPath("target", true); using var left = new AnimationNodeAnimation { Animation = "a" }; using var right = new AnimationNodeAnimation { Animation = "b" }; graph.AddNode("mix", mix); graph.AddNode("a", left); graph.AddNode("b", right); graph.ConnectNode("mix", 0, "a"); graph.ConnectNode("mix", 1, "b"); graph.ConnectNode("output", 0, "mix"); var tree = new AnimationTree { TreeRoot = graph, CallbackModeProcess = AnimationMixer.AnimationCallbackModeProcess.Manual, CallbackModeMethod = AnimationMixer.AnimationCallbackModeMethod.Immediate }; tree.AddAnimationLibrary("", setup.Library); setup.Root.AddChild(tree);
        tree.Advance(0); tree.Advance(.25); Sequence(setup.Target.Calls); Near(setup.Target.X, 10); tree.SetParameter("mix", AnimationNodeBlend2.BlendAmount, 1); tree.Advance(.25); Sequence(setup.Target.Calls, 2); Near(setup.Target.X, 10);
        mix.SetFilterPath("target:X", true); tree.SetParameter("mix", AnimationNodeBlend2.BlendAmount, .5); setup.Target.Calls.Clear(); tree.Advance(.25); Near(setup.Target.X, 20); Sequence(setup.Target.Calls, 3); tree.Dispose();
    }
    private static void Failures()
    {
        using (var disabled = Events()) { disabled.TrackSetEnabled(0, false); using var delayed = new Setup(disabled); delayed.Player.CallbackModeMethod = AnimationMixer.AnimationCallbackModeMethod.Immediate; delayed.Player.Play("a"); delayed.Player.Advance(.5); Sequence(delayed.Target.Calls); disabled.TrackSetEnabled(0, true); delayed.Player.Advance(.25); Sequence(delayed.Target.Calls, 3); }
        using var clip = Events(); clip.LoopMode = SpriteFrames.LoopMode.Linear; using var setup = new Setup(clip); setup.Player.Play("a"); Reject<InvalidOperationException>(() => setup.Player.Advance(5)); setup.Scene.FlushDeferred(); setup.Player.PrepareMethodCallbacks(64); setup.Player.Play("a"); setup.Player.Seek(0); setup.Target.Calls.Clear(); setup.Player.Advance(5); setup.Scene.FlushDeferred(); Check(setup.Target.Calls.Count == 26, "Prepared larger multi-loop burst.");
        setup.Player.CallbackModeMethod = AnimationMixer.AnimationCallbackModeMethod.Immediate; setup.Target.Calls.Clear(); setup.Target.Hook = () => setup.Player.Stop(true); setup.Player.Play("a"); setup.Player.Advance(1); Sequence(setup.Target.Calls, 0); setup.Target.Hook = null;
        setup.Player.CallbackModeMethod = AnimationMixer.AnimationCallbackModeMethod.Deferred; setup.Player.Play("a"); setup.Player.Advance(.5); setup.Target.Dispose(); setup.Scene.FlushDeferred();
    }
    private static void Warm()
    {
        using var clip = Events(); clip.LoopMode = SpriteFrames.LoopMode.Linear; using var setup = new Setup(clip); setup.Target.Calls.Capacity = 2048; setup.Player.PrepareMethodCallbacks(32); setup.Player.Play("a"); for (var i = 0; i < 20; i++) setup.Scene.ProcessFrame(.25); var before = GC.GetAllocatedBytesForCurrentThread(); for (var i = 0; i < 256; i++) setup.Scene.ProcessFrame(.25); Check(GC.GetAllocatedBytesForCurrentThread() == before, "Prepared deferred event frames allocate zero managed bytes.");
        using var curve = new Animation { Length = 1, LoopMode = SpriteFrames.LoopMode.Linear }; var track = curve.AddBezierTrack(X); curve.TrackSetPath(track, "target:X"); curve.BezierTrackInsertKey(track, 0, 0, outHandle: new(.2f, .5f)); curve.BezierTrackInsertKey(track, 1, 1, inHandle: new(-.2f, -.5f)); using var other = new Setup(curve); other.Player.Play("a"); for (var i = 0; i < 20; i++) other.Scene.ProcessFrame(.01); before = GC.GetAllocatedBytesForCurrentThread(); for (var i = 0; i < 256; i++) other.Scene.ProcessFrame(.01); Check(GC.GetAllocatedBytesForCurrentThread() == before, "Warmed Bézier scalar frames allocate zero managed bytes.");
    }
    internal static void RunHost()
    {
        var backend = Environment.GetEnvironmentVariable("ELECTRON2D_ANIMATION_RENDERER") ?? "gpu"; var settings = ProjectSettings.Service; var prior = ProjectSettings.Get(ProjectSettings.RenderingMethod); ProjectSettings.Set(ProjectSettings.RenderingMethod, backend);
        var descriptor = new PropertyDescriptor<Box, float>("X", n => n.Position.X, (n, v) => n.Position = new(v, 16));
        try
        {
            for (var run = 0; run < 2; run++)
            {
                using var clip = new Animation { Length = 1 }; var scalar = clip.AddBezierTrack(descriptor); clip.TrackSetPath(scalar, "box:X"); clip.BezierTrackInsertKey(scalar, 0, 16, outHandle: new(1f / 3, 0)); clip.BezierTrackInsertKey(scalar, 1, 80, inHandle: new(-1f / 3, 0)); var calls = clip.AddMethodTrack<Box>(); clip.TrackSetPath(calls, "box"); clip.TrackInsertKey(calls, 0, new AnimationMethodKey<Box, Color>("color", static (n, v) => { n.Fill = v; n.QueueRedraw(); }, Colors.Red)); clip.TrackInsertKey(calls, .5, new AnimationMethodKey<Box, Color>("color", static (n, v) => { n.Fill = v; n.QueueRedraw(); }, Colors.Green));
                using var library = new AnimationLibrary(); library.AddAnimation("curve", clip); using var leaf = new AnimationNodeAnimation { Animation = "curve" }; var window = new Window { Size = new(112, 48), Title = "Electron2D Bézier and method tracks" }; var box = new Box { Name = "box" }; var tree = new AnimationTree { TreeRoot = leaf, CallbackModeProcess = AnimationMixer.AnimationCallbackModeProcess.Manual }; tree.AddAnimationLibrary("", library); window.AddChild(box); window.AddChild(tree); var stage = 0; int[] positions = [16, 26, 48, 70, 80];
                box.Start = () => { var renderer = RenderingServer.Service!; RenderingServer.SetDefaultClearColor(Colors.Black); tree.Advance(0); RenderingServer.FramePostDraw += () => { using var pixels = renderer.Readback(); var color = pixels.GetPixel(positions[stage], 16); Check(stage < 2 ? color.R > .9f && color.G < .1f : color.G > .4f && color.R < .1f, "Bézier scalar pose and deferred method color reach real pixels."); if (stage > 0) Check(pixels.GetPixel(positions[stage - 1], 16).R < .1f && pixels.GetPixel(positions[stage - 1], 16).G < .1f, "Prior pose clears."); stage++; if (stage == 5) window.Tree!.Quit(); else { tree.Advance(.25); } }; };
                Check(Engine.Run(window) == 0 && stage == 5 && window.IsDisposed && !clip.IsDisposed && !leaf.IsDisposed && !library.IsDisposed, "Special-track host cleanup preserves borrowed resources."); Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(new { scenario = "animation-special-tracks-host", backend, run, stages = stage, cleaned = window.IsDisposed }));
            }
        }
        finally { ProjectSettings.Set(ProjectSettings.RenderingMethod, prior); }
    }
    private sealed class Box : Entity { internal Action? Start; internal Color Fill; protected override void OnReady() => Start?.Invoke(); protected override void OnDraw() => DrawRect(new(-3, -3, 6, 6), Fill); }
    private static Animation Events() { var clip = new Animation { Length = 1 }; var track = clip.AddMethodTrack<Probe>(); clip.TrackSetPath(track, "target"); for (var i = 0; i < 5; i++) clip.TrackInsertKey(track, i * .25, new AnimationMethodKey<Probe, int>("mark", Mark, i)); return clip; }
    private sealed class Setup : IDisposable
    {
        internal readonly Probe Target = new() { Name = "target" }; internal readonly Node Root = new(); internal readonly AnimationPlayer Player = new() { PlaybackAutoCapture = false }; internal readonly AnimationLibrary Library = new(); internal readonly SceneTree Scene;
        internal Setup(Animation clip) { Library.AddAnimation("a", clip); Root.AddChild(Target); Root.AddChild(Player); Scene = new(Root); Player.AddAnimationLibrary("", Library); }
        public void Dispose() { Scene.Dispose(); Library.Dispose(); }
    }
    private sealed class Probe : Node { internal double X; internal readonly List<int> Calls = []; internal Action? Hook; }
    private static void Sequence(List<int> actual, params int[] expected) => Check(actual.SequenceEqual(expected), "Expected [" + string.Join(',', expected) + "], got [" + string.Join(',', actual) + "].");
    private static void Near(double value, double expected) => Check(Math.Abs(value - expected) < 1e-5, $"Expected {expected}, got {value}.");
    private static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    private static void Reject<T>(Action action) where T : Exception { try { action(); } catch (T) { return; } throw new InvalidOperationException("Expected " + typeof(T).Name); }
}
