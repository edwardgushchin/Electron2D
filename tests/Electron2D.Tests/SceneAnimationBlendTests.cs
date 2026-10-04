using Electron2D;
using System.Runtime.CompilerServices;
using Mathf = Electron2D.Mathf;

internal static class SceneAnimationBlendTests
{
    private static readonly PropertyDescriptor<Probe, double> X = new("X", n => n.X, (n, v) => { n.X = v; n.Writes++; n.Hook?.Invoke(); });
    private static readonly PropertyDescriptor<Probe, double> Y = new("Y", n => n.Y, (n, v) => n.Y = v);
    internal static void Run() { Arithmetic(); Transitions(); Capture(); Modes(); Lifetime(); CaptureCleanup(); Warm(); Console.WriteLine("Typed animation crossfades/capture/RESET/discrete modes/postprocess, scalar/vector/array/string arithmetic and warmed allocations passed."); }
    private static void Arithmetic()
    {
        static T Blend<T>(T rest, T a, T b, double wa = .5, double wb = .5) { var math = AnimationBlendValue<T>.Create()!; math.Begin(rest); math.Add(a, wa, false); math.Add(b, wb, false); return math.Finish(wa + wb); }
        Near(Blend(0d, 10d, 30d), 20); Check(Blend(0, 0, 1) == 1 && Blend(false, false, true), "Promoted integer/bool rounds at final output.");
        Check(Blend(0L, long.MaxValue - 1, long.MaxValue) == long.MaxValue && Blend(0L, long.MinValue, long.MaxValue) == -1, "Int64 uses exact promoted decimal arithmetic.");
        Check(Blend(default(Vector2i), new(0, 0), new(1, -1)) == new Vector2i(1, -1), "Integer vector midpoint.");
        Check(Blend(default(Vector4i), new(int.MaxValue, 0, -2, 2), new(int.MaxValue, 1, 1, 1)) == new Vector4i(int.MaxValue, 1, -1, 2), "Integer components have no intermediate rounding or float overflow.");
        var transform = Blend(Transform.Identity, new Transform(0, new(2, 2), 0, new(10, 0)), new Transform(0, new(4, 4), 0, new(20, 0))); Near(transform.Origin.X, 20); Near(transform.Scale.X, 3.75);
        Check(Blend(Array.Empty<int>(), new[] { 0, 2 }, new[] { 1 }).SequenceEqual([1, 1]), "Typed array missing elements use zero and length uses current maximum.");
        Check(Blend(Array.Empty<Vector2>(), new[] { new Vector2(2, 4) }, new[] { new Vector2(4, 8) })[0] == new Vector2(3, 6), "Vector arrays blend by element.");
        Check(Blend("", "AA", "CC") == "BB", "String blending uses Unicode scalar codes.");
        Check(Blend(Array.Empty<string>(), new[] { "AA" }, new[] { "CC" })[0] == "BB", "Typed string arrays.");
        var angle = AnimationBlendValue<double>.Create()!; angle.Begin(0); angle.Add(Math.Tau - .2, .5, true); angle.Add(.2, .5, true); Near(angle.Finish(), 0);
    }
    private static void Transitions()
    {
        using var a = Clip(10); using var b = Clip(30); using var c = Clip(50); using var scope = new Setup(a, b, c); var p = scope.Player;
        Check(p.PlaybackDefaultBlendTime == 0 && p.PlaybackAutoCapture && p.PlaybackAutoCaptureDuration == -1 && p.CallbackModeDiscrete == AnimationMixer.AnimationCallbackModeDiscrete.Recessive && !p.Deterministic, "Blend/capture defaults.");
        p.Play("a"); p.Advance(0); Near(scope.Target.X, 10); p.SetBlendTime("a", "b", 1); Near(p.GetBlendTime("a", "b"), 1); p.Play("b"); p.Advance(0); Near(scope.Target.X, 10); p.Advance(.25); Near(scope.Target.X, 10); p.Advance(.25); Near(scope.Target.X, 50d / 3); p.Advance(.25); Near(scope.Target.X, 70d / 3); p.Advance(.25); Near(scope.Target.X, (30 * .75 + 10 * 1e-5) / (.75 + 1e-5)); p.Advance(0); Near(scope.Target.X, 30);
        p.Play("a", 0); p.Advance(0); p.Play("b", 1); p.Advance(.25); p.Play("c", 1); p.Advance(0); Near(scope.Target.X, 15); p.Advance(.25); Near(scope.Target.X, (10 * .5 + 30 * 1e-5) / (.5 + 1e-5)); p.Advance(.25); Near(scope.Target.X, 110d / 3);
        p.Stop(true); p.SetBlendTime("a", "b", 0); p.PlaybackDefaultBlendTime = .2; p.Play("a", 0); p.Advance(0); p.Play("b"); p.Advance(.2); p.Advance(0); Near(scope.Target.X, 30);
        Reject<ArgumentOutOfRangeException>(() => p.PlaybackDefaultBlendTime = -1); Reject<ArgumentOutOfRangeException>(() => p.SetBlendTime("a", "b", double.NaN)); Reject<KeyNotFoundException>(() => p.SetBlendTime("missing", "b", 1)); p.SetBlendTime("a", "b", 0); Near(p.GetBlendTime("a", "b"), 0);
        using var sparse = Clip(80, Y); scope.Library.AddAnimation("sparse", sparse); p.Stop(true); scope.Target.X = 99; scope.Target.Y = 99; p.Deterministic = false; p.Play("sparse", 0); p.Advance(0); Near(scope.Target.X, 99); Near(scope.Target.Y, 80);
        using var reset = Clip(3); var ry = reset.AddTrack(Y); reset.TrackSetPath(ry, "target:Y"); reset.TrackInsertKey(ry, 0, 4d); scope.Library.AddAnimation("RESET", reset);
        p.Deterministic = true; p.Play("a", 0); p.Advance(0); Near(scope.Target.X, 10); Near(scope.Target.Y, 4); p.Play("sparse", 1); p.Advance(.5); Near(scope.Target.X, 6.5); Near(scope.Target.Y, 4); p.Advance(.5); p.Advance(0); Near(scope.Target.X, 3); Near(scope.Target.Y, 80);
    }
    private static void Capture()
    {
        using var a = Clip(20); a.ValueTrackSetUpdateMode(0, Animation.UpdateMode.Capture); using var scope = new Setup(a); var p = scope.Player;
        Check(a.CaptureIncluded, "CaptureIncluded reflects disabled as well as active capture metadata."); scope.Target.X = 100; p.PlayWithCapture("a", 1); p.Advance(0); Near(scope.Target.X, 100); p.Advance(.25); Near(scope.Target.X, 80); p.Advance(.75); Near(scope.Target.X, 20);
        scope.Target.X = 100; p.PlayWithCapture("a", 1, transitionType: Tween.TransitionType.Quad); p.Advance(.5); Near(scope.Target.X, 40);
        p.Stop(true); p.SpeedScale = 2; scope.Target.X = 100; p.PlayWithCapture("a", 1, customSpeed: 0); p.Advance(.25); Near(scope.Target.X, 60); Check(p.IsPlaying() && p.CurrentAnimationPosition == 0, "Capture clock uses SpeedScale independently of custom speed."); p.SpeedScale = 0; p.Advance(5); Near(scope.Target.X, 60); p.SpeedScale = 1;
        p.Pause(); p.Advance(.5); Near(scope.Target.X, 60); p.Play("a", 0); p.Advance(.5); Near(scope.Target.X, 20);
        p.Stop(true); a.TrackSetKeyTime(0, 0, .5); scope.Target.X = 100; p.Play("a"); p.Advance(.25); Near(scope.Target.X, 60); p.Advance(.25); Near(scope.Target.X, 20);
        p.PlaybackAutoCapture = false; scope.Target.X = 100; p.Play("a", 0); p.Advance(0); Near(scope.Target.X, 20); p.Capture("a", 1); scope.Target.X = 999; p.Advance(0); Near(scope.Target.X, 20);
        p.ClearCaches(); scope.Target.X = 70; p.Capture("a", 1); p.Advance(.5); Near(scope.Target.X, 45);
        Reject<ArgumentOutOfRangeException>(() => p.Capture("a", 0)); Reject<ArgumentOutOfRangeException>(() => p.PlayWithCapture("a", double.NaN)); p.Active = false; Reject<InvalidOperationException>(() => p.Capture("a", 1)); p.Active = true;
        a.ValueTrackSetUpdateMode(0, Animation.UpdateMode.Capture); using var copied = new Animation(); a.CopyTrack(0, copied); Check(copied.CaptureIncluded, "Copied capture tracks update cached metadata."); copied.TrackSetEnabled(0, false); Check(copied.CaptureIncluded, "Disabled capture metadata is still included."); copied.RemoveTrack(0); Check(!copied.CaptureIncluded, "Removing the last capture track clears cached metadata.");
        a.ValueTrackSetUpdateMode(0, Animation.UpdateMode.Continuous); Check(!a.CaptureIncluded, "Capture flag recalculates on mode changes.");
    }
    private static void Modes()
    {
        using var discrete = Clip(10); discrete.ValueTrackSetUpdateMode(0, Animation.UpdateMode.Discrete); discrete.TrackInsertKey(0, .5, 14d); using var continuous = Clip(30); using var scope = new Setup(discrete, continuous); var p = scope.Player;
        p.Play("b"); p.Advance(0); p.Play("a", 1); p.CallbackModeDiscrete = AnimationMixer.AnimationCallbackModeDiscrete.Dominant; p.Advance(.25); p.Advance(.25); Near(scope.Target.X, 14);
        p.Stop(true); p.CallbackModeDiscrete = AnimationMixer.AnimationCallbackModeDiscrete.Recessive; p.Play("b", 0); p.Advance(0); p.Play("a", 1); p.Advance(.25); p.Advance(.25); Near(scope.Target.X, 20);
        p.Stop(true); p.CallbackModeDiscrete = AnimationMixer.AnimationCallbackModeDiscrete.ForceContinuous; p.Play("b", 0); p.Advance(0); p.Play("a", 1); p.Advance(.25); p.Advance(.25); Near(scope.Target.X, (14 * .25 + 30 * .5) / .75);
        var before = scope.Target.Writes; p.Advance(0); Check(scope.Target.Writes > before, "ForceContinuous updates even without crossed keys.");
    }
    private static void Lifetime()
    {
        using var a = Clip(10); using var b = Clip(30); var target = new Probe { Name = "target" }; var player = new PostProcess { CallbackModeProcess = AnimationMixer.AnimationCallbackModeProcess.Manual }; var root = new Node(); root.AddChild(target); root.AddChild(player); using var tree = new SceneTree(root); using var library = new AnimationLibrary(); library.AddAnimation("a", a); library.AddAnimation("b", b); player.AddAnimationLibrary("", library);
        player.Play("a"); player.Advance(0); Near(target.X, 11); Check(player.LastID == target.InstanceID && player.LastSubIndex == -1, "Typed postprocess receives the actual target identity."); player.Play("b", 1); player.Advance(.25); Near(target.X, 11);
        target.Hook = () => player.Stop(true); player.Advance(.25); Check(!player.IsPlaying(), "Reentrant setter stops without writing stale blend results."); target.Hook = null;
        player.Play("a", 0); player.KeyHook = () => player.Stop(); player.Advance(.1); Check(!player.IsPlaying() && player.CurrentAnimationPosition == 0, "Postprocess Stop is guarded against recursive reset sampling."); player.KeyHook = null;
        player.Play("b", 1); target.Hook = () => throw new ApplicationException("Expected blend setter failure."); Reject<ApplicationException>(() => player.Advance(.1)); Check(!player.IsPlaying(), "Blend setter failure pauses unchanged playback."); target.Hook = null;
        player.Play("a", 0); player.Advance(0); a.ValueTrackSetUpdateMode(0, Animation.UpdateMode.Capture); target.X = 100; player.PlayWithCapture("a", 1); root.RemoveChild(target); target.Dispose(); var replacement = new Probe { Name = "target" }; root.AddChild(replacement); player.Advance(.25); Near(replacement.X, 11);
        library.RemoveAnimation("a"); player.Advance(.1); Check(!player.IsPlaying(), "Removed current source stops."); player.Play("b", 0); player.Advance(0); b.Dispose(); player.Advance(.1); Check(!player.IsPlaying(), "Disposed current source stops.");
    }
    private static void CaptureCleanup()
    {
        using var clip = Clip(20); clip.ValueTrackSetUpdateMode(0, Animation.UpdateMode.Capture); var root = new Node(); var target = new Probe { Name = "target", X = 100 }; var player = new PostProcess(); root.AddChild(target); root.AddChild(player); using var tree = new SceneTree(root); using var library = new AnimationLibrary(); library.AddAnimation("a", clip); player.AddAnimationLibrary("", library); player.PlayWithCapture("a", 1); player.Advance(0);
        var snapshot = player.LastAnimation!; Check(snapshot != clip && snapshot.GetTrackCount() == 1, "Capture owns a separate typed snapshot resource."); snapshot.Disposed += _ => throw new ApplicationException("Expected snapshot cleanup failure."); Reject<AggregateException>(() => player.Dispose()); Check(player.IsDisposed && player.Parent is null && snapshot.IsDisposed && !clip.IsDisposed && !library.IsDisposed && !target.IsDisposed, "Snapshot cleanup failure continues node cleanup without disposing borrowed assets.");
    }
    private static void Warm()
    {
        using var a = Clip(10); using var b = Clip(30); a.LoopMode = b.LoopMode = SpriteFrames.LoopMode.Linear; using var scope = new Setup(a, b); var p = scope.Player; p.Play("a"); p.Advance(0); p.Play("b", 1000); for (var i = 0; i < 20; i++) scope.Tree.ProcessFrame(.01); var before = GC.GetAllocatedBytesForCurrentThread(); for (var i = 0; i < 256; i++) scope.Tree.ProcessFrame(.01); Check(GC.GetAllocatedBytesForCurrentThread() == before, "256 warmed crossfade tree passes allocate zero bytes.");
        a.ValueTrackSetUpdateMode(0, Animation.UpdateMode.Capture); p.PlayWithCapture("a", 1000, 1000); for (var i = 0; i < 20; i++) p.Advance(.01); before = GC.GetAllocatedBytesForCurrentThread(); for (var i = 0; i < 256; i++) p.Advance(.01); Check(GC.GetAllocatedBytesForCurrentThread() == before, "256 warmed capture/crossfade passes allocate zero bytes.");
        p.RenameAnimationLibrary("", "motion"); p.Play("motion/a", 0); p.Advance(0); p.Play("motion/b", 1000); for (var i = 0; i < 20; i++) p.Advance(.01); before = GC.GetAllocatedBytesForCurrentThread(); for (var i = 0; i < 256; i++) p.Advance(.01); Check(GC.GetAllocatedBytesForCurrentThread() == before, "256 warmed qualified-name blend passes allocate zero bytes.");
    }
    internal static void RunHost()
    {
        var backend = Environment.GetEnvironmentVariable("ELECTRON2D_ANIMATION_RENDERER") ?? "gpu"; var settings = ProjectSettings.Service; var prior = ProjectSettings.Get(ProjectSettings.RenderingMethod); ProjectSettings.Set(ProjectSettings.RenderingMethod, backend);
        var descriptor = new PropertyDescriptor<Entity, Vector2>(nameof(Entity.Position), n => n.Position, (n, v) => n.Position = v);
        try
        {
            for (var run = 0; run < 2; run++)
            {
                using var a = new Animation { Length = 4 }; var ta = a.AddTrack(descriptor); a.TrackSetPath(ta, "box:Position"); a.TrackInsertKey(ta, 0, new Vector2(16, 16));
                using var b = new Animation { Length = 4 }; var tb = b.AddTrack(descriptor); b.TrackSetPath(tb, "box:Position"); b.TrackInsertKey(tb, 0, new Vector2(48, 16)); b.ValueTrackSetUpdateMode(tb, Animation.UpdateMode.Capture);
                using var library = new AnimationLibrary(); library.AddAnimation("a", a); library.AddAnimation("b", b);
                var window = new Window { Size = new(96, 64), Title = "Electron2D blended scene animation" }; var box = new Box { Name = "box" }; var player = new AnimationPlayer { PlaybackAutoCapture = false, CallbackModeProcess = AnimationMixer.AnimationCallbackModeProcess.Manual }; player.AddAnimationLibrary("", library); window.AddChild(box); window.AddChild(player); var stage = 0;
                box.Start = () =>
                {
                    var renderer = RenderingServer.Service!; RenderingServer.SetDefaultClearColor(Colors.Black); player.Play("a"); player.Advance(0);
                    RenderingServer.FramePostDraw += () =>
                    {
                        using var pixels = renderer.Readback(); var expected = stage switch { 0 or 1 or 2 => 16, 3 => 37, 4 or 7 => 48, 5 => 72, 6 => 60, _ => throw new InvalidOperationException("Unexpected blend stage.") };
                        Check(pixels.GetPixel(expected, 16).R > .9f && pixels.GetPixel(expected, 16).G < .1f, "Weighted/captured position reaches rendered pixels."); if (stage >= 3) Check(pixels.GetPixel(16, 16).R < .1f, "Old pose is cleared.");
                        switch (stage++)
                        {
                            case 0: player.Play("b", 1); player.Advance(0); break;
                            case 1: player.Advance(.5); break;
                            case 2: player.Advance(.25); break;
                            case 3: player.Advance(.25); break;
                            case 4: box.Position = new(72, 16); player.Capture("b", 1); player.Advance(0); break;
                            case 5: player.Advance(.5); break;
                            case 6: player.Advance(.5); break;
                            case 7: window.Tree!.Quit(); break;
                        }
                    };
                };
                Check(Engine.Run(window) == 0 && stage == 8 && window.IsDisposed && !a.IsDisposed && !b.IsDisposed && !library.IsDisposed, "Blend host lifecycle and borrowed resources."); Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(new { scenario = "scene-blend-host", backend, run, stages = stage, cleaned = window.IsDisposed }));
            }
        }
        finally { ProjectSettings.Set(ProjectSettings.RenderingMethod, prior); }
    }
    private sealed class Box : Entity { internal Action? Start; protected override void OnReady() => Start?.Invoke(); protected override void OnDraw() => DrawRect(new(-3, -3, 6, 6), Colors.Red); }
    private static Animation Clip(double value, PropertyDescriptor<Probe, double>? property = null) { property ??= X; var a = new Animation { Length = 4 }; var i = a.AddTrack(property); a.TrackSetPath(i, "target:" + property.Name); a.TrackInsertKey(i, 0, value); return a; }
    private sealed class Setup : IDisposable
    {
        internal readonly Probe Target = new() { Name = "target" }; internal readonly AnimationPlayer Player = new(); internal readonly AnimationLibrary Library = new(); internal readonly SceneTree Tree;
        internal Setup(params Animation[] animations) { for (var i = 0; i < animations.Length; i++) Library.AddAnimation(((char)('a' + i)).ToString(), animations[i]); var root = new Node(); root.AddChild(Target); root.AddChild(Player); Tree = new(root); Player.AddAnimationLibrary("", Library); }
        public void Dispose() { Tree.Dispose(); Library.Dispose(); }
    }
    private sealed class Probe : Node { internal double X, Y; internal int Writes; internal Action? Hook; }
    private sealed class PostProcess : AnimationPlayer
    { internal ulong LastID; internal int LastSubIndex; internal Animation? LastAnimation; internal Action? KeyHook; protected override T OnPostProcessKeyValue<T>(Animation animation, int track, T value, ulong objectID, int objectSubIndex = -1) { LastID = objectID; LastSubIndex = objectSubIndex; LastAnimation = animation; KeyHook?.Invoke(); if (typeof(T) == typeof(double)) { var result = Unsafe.As<T, double>(ref value) + 1; return Unsafe.As<double, T>(ref result); } return value; } }
    private static void Near(double a, double b) => Check(Math.Abs(a - b) < 1e-5, $"Expected {b}, got {a}.");
    private static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    private static void Reject<T>(Action action) where T : Exception { try { action(); } catch (T) { return; } throw new InvalidOperationException("Expected " + typeof(T).Name); }
}
