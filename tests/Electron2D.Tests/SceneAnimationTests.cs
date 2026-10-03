using Electron2D;
using Mathf = Electron2D.Mathf;

internal static class SceneAnimationTests
{
    private static readonly PropertyDescriptor<Entity, Vector2> Position = new(nameof(Entity.Position), n => n.Position, (n, v) => n.Position = v);
    private static readonly PropertyDescriptor<Probe, double> Value = new("Value", n => n.Value, (n, v) => { n.Value = v; n.Writes++; n.Hook?.Invoke(); });
    private static readonly PropertyDescriptor<Probe, string> Text = new("Text", n => n.Text, (n, v) => n.Text = v);
    internal static void Run()
    {
        Keys(); Libraries(); Playback(); Discrete(); Lifecycle(); Warm();
        Console.WriteLine("Scene animation typed keys, curves, markers, libraries, forward/reverse/sections/queues, lifetime and zero warmed allocation passed.");
    }
    private static void Keys()
    {
        using var a = new Animation(); Check(a.Length == 1 && a.LoopMode == SpriteFrames.LoopMode.None && a.Step == .033333335, "Timing defaults.");
        var t = a.AddTrack(Value); a.TrackSetPath(t, ".:Value"); Check(a.TrackGetType(t) == Animation.TrackType.Value && a.TrackGetPath(t) == ".:Value", "Typed descriptor and path.");
        Reject<ArgumentException>(() => a.TrackSetPath(t, ".:Other")); Reject<ArgumentException>(() => a.AddTrack(new PropertyDescriptor<Probe, double>("ReadOnly", n => n.Value)));
        Reject<ArgumentOutOfRangeException>(() => a.Length = double.NaN); a.Length = -4; Check(a.Length == .001, "Duration clamp."); a.Length = 2;
        a.TrackInsertKey(t, 1, 10d); a.TrackInsertKey(t, 0, 0d); a.TrackInsertKey(t, 2, 20d); Check(a.TrackInsertKey(t, 1, 12d) == 1 && a.TrackGetKeyCount(t) == 3, "Sorted replacement.");
        Near(a.ValueTrackInterpolate<double>(t, .5), 6); Reject<InvalidCastException>(() => a.TrackGetKeyValue<float>(t, 0)); Reject<ArgumentOutOfRangeException>(() => a.TrackInsertKey(t, double.PositiveInfinity, 0d));
        Check(a.TrackFindKey(t, .5, Animation.FindMode.Nearest) == 0 && a.TrackFindKey(t, .5, Animation.FindMode.Nearest, backward: true) == 1 && a.TrackFindKey(t, 1, Animation.FindMode.Exact) == 1 && a.TrackFindKey(t, .5) == -1, "Directional key lookup.");
        a.TrackSetKeyTime(t, 1, 2); Check(a.TrackGetKeyCount(t) == 2 && a.TrackGetKeyValue<double>(t, 1) == 12, "Key move replaces destination.");
        a.TrackSetKeyValue(t, 1, 20d); a.TrackSetKeyTransition(t, 0, 2); Near(a.ValueTrackInterpolate<double>(t, 1), 5); a.TrackSetKeyTransition(t, 0, 1);
        a.TrackSetInterpolationType(t, Animation.InterpolationType.Cubic); for (var i = 0; i < 20; i++) { var w = i / 20d; Near(a.ValueTrackInterpolate<double>(t, w * 2), Mathf.CubicInterpolateInTime(0, 20, 0, 20, w, 2, 0, 2)); }
        a.TrackSetInterpolationType(t, Animation.InterpolationType.LinearAngle); a.TrackSetKeyValue(t, 0, Math.PI * .95); a.TrackSetKeyValue(t, 1, -Math.PI * .95); Near(a.ValueTrackInterpolate<double>(t, 1), Math.PI);
        a.TrackSetInterpolationType(t, Animation.InterpolationType.CubicAngle); Near(a.ValueTrackInterpolate<double>(t, 1), Mathf.CubicInterpolateAngleInTime(Math.PI * .95, -Math.PI * .95, Math.PI * .95, -Math.PI * .95, .5, 2, 0, 2));
        a.TrackSetInterpolationType(t, Animation.InterpolationType.Linear); a.TrackSetKeyValue(t, 0, 0d); a.TrackSetKeyValue(t, 1, 20d); a.ValueTrackSetUpdateMode(t, Animation.UpdateMode.Discrete); Near(a.ValueTrackInterpolate<double>(t, 1), 0); Near(a.ValueTrackInterpolate<double>(t, 1, true), 20);
        var text = a.AddTrack(Text); a.TrackInsertKey(text, 0, "start"); a.TrackInsertKey(text, 1, "end"); Check(a.ValueTrackInterpolate<string>(text, .5) == "start", "Non-numeric nearest keys."); Reject<NotSupportedException>(() => a.TrackSetInterpolationType(text, Animation.InterpolationType.Cubic));
        a.AddMarker("start", .2); a.SetMarkerColor("start", Colors.Red); a.AddMarker("replace", .2); a.AddMarker("end", 1.8); Check(!a.HasMarker("start") && a.GetMarkerColor("replace") == new Color(1, 1, 1) && a.GetMarkerNames().SequenceEqual(["replace", "end"]), "Marker replacement/color/order."); Check(a.GetPrevMarker(.2) == "replace" && a.GetNextMarker(.2) == "end" && a.GetMarkerTime("absent") == -1, "Marker boundaries.");
        using var copy = (Animation)a.Duplicate(); copy.TrackSetKeyValue(t, 0, 4d); Near(a.TrackGetKeyValue<double>(t, 0), 0); using var destination = new Animation(); a.CopyTrack(text, destination); a.TrackRemoveKey(text, 0); Check(destination.TrackGetKeyCount(0) == 2, "Independent duplicated/copied containers.");
        a.TrackMoveDown(text); Check(a.FindTrack(".:Value") == 1, "Track order down."); a.TrackMoveUp(0); Check(a.FindTrack(".:Value") == 0, "Track order up."); a.TrackMoveTo(0, 2); Check(a.FindTrack(".:Value") == 1, "Insertion-boundary move."); a.TrackSwap(0, 1); a.Clear(); Check(a.GetTrackCount() == 0 && a.GetMarkerNames().Length == 2 && a.Length == 1, "Clear restores length/loop and retains markers.");
        using var loop = Timeline(Value, "target:Value", .2, .8); loop.LoopMode = SpriteFrames.LoopMode.Linear; Near(loop.ValueTrackInterpolate<double>(0, .9), .65); Near(loop.ValueTrackInterpolate<double>(0, .1), .35); loop.TrackSetInterpolationLoopWrap(0, false); Near(loop.ValueTrackInterpolate<double>(0, .9), .8);
    }
    private static void Libraries()
    {
        using var a = new Animation(); using var b = new Animation(); using var library = new AnimationLibrary(); var events = new List<string>(); library.AnimationAdded += n => events.Add("+" + n); library.AnimationRemoved += n => events.Add("-" + n); library.AnimationChanged += n => events.Add("=" + n); library.AnimationRenamed += (a, b) => events.Add(a + ">" + b);
        library.AddAnimation("walk", a); library.AddAnimation("walk", b); a.Length = 2; b.Length = 3; library.RenameAnimation("walk", "run"); b.Length = 4; Check(events.SequenceEqual(["+walk", "-walk", "+walk", "=walk", "walk>run", "=run"]), "Replacement and renamed subscriptions.");
        Reject<ArgumentException>(() => library.AddAnimation("bad/name", a)); Reject<ArgumentException>(() => library.RenameAnimation("run", "run")); using var shallow = (AnimationLibrary)library.Duplicate(); Check(shallow.GetAnimation("run") == b, "Shallow aliases."); using var deep = (AnimationLibrary)library.Duplicate(true); Check(deep.GetAnimation("run") != b && deep.GetAnimation("run").Length == 4, "Deep resource copy."); deep.GetAnimation("run").Dispose();
        library.RemoveAnimation("run"); var count = events.Count; b.Length = 5; Check(events.Count == count && library.GetAnimationListSize() == 0, "Removed source detached."); library.Dispose(); Check(!a.IsDisposed && !b.IsDisposed, "Borrowed resource lifetime.");
        using var reentrant = new AnimationLibrary(); reentrant.AddAnimation("a", a); reentrant.AnimationRemoved += _ => { if (reentrant.HasAnimation("a")) reentrant.RemoveAnimation("a"); }; reentrant.AddAnimation("a", b); Check(!reentrant.HasAnimation("a"), "Replacement observer may remove the committed replacement without leaked membership.");
    }
    private static void Playback()
    {
        using var a = Timeline(Value, "target:Value"); using var b = Timeline(Value, "target:Value"); b.TrackSetKeyValue(0, 1, 20d); using var library = new AnimationLibrary(); library.AddAnimation("a", a); library.AddAnimation("b", b);
        var root = new Node(); var target = new Probe { Name = "target" }; var player = new AnimationPlayer(); root.AddChild(target); root.AddChild(player); using var tree = new SceneTree(root); player.AddAnimationLibrary("", library);
        Check(player.GetAnimationList().SequenceEqual(["a", "b"]) && player.FindAnimation(a) == "a" && player.FindAnimationLibrary(a) == "", "Default namespace.");
        player.Play("a"); Near(target.Value, 0); tree.ProcessFrame(.25); Near(target.Value, 2.5); tree.Paused = true; tree.ProcessFrame(.25); Near(target.Value, 2.5); tree.Paused = false;
        player.Pause(); tree.ProcessFrame(.1); Near(player.CurrentAnimationPosition, .25); player.Play(); player.Advance(.25); Near(target.Value, 5); player.Seek(.7); Near(target.Value, 5); player.Advance(0); Near(target.Value, 7); player.Seek(2, true); Near(target.Value, 10);
        var finished = 0; player.AnimationFinished += _ => finished++; player.Play(); player.Advance(1); Check(finished == 1 && !player.IsPlaying(), "Forward restart and single completion."); player.Advance(1); Check(finished == 1, "Completed controller stays stopped.");
        player.PlayBackwards("a"); player.Advance(.25); Near(target.Value, 7.5); player.Advance(.75); Near(target.Value, 0); Check(finished == 2 && !player.IsPlaying(), "Reverse completion.");
        a.AddMarker("from", .2); a.AddMarker("to", .8); player.PlaySectionWithMarkers("a", "from", "to"); player.Advance(.3); Near(target.Value, 5); player.Advance(.3); Near(target.Value, 8); Check(!player.IsPlaying() && player.HasSection(), "Marker section completion."); player.ResetSection(); Check(!player.HasSection(), "Section reset."); Reject<ArgumentException>(() => player.SetSection(.8, .2));
        player.Play("a"); player.Queue("b"); player.Queue("a"); player.Advance(1); Check(player.CurrentAnimation == "b" && player.GetQueue().SequenceEqual(["a"]), "Queued transition preserves later queue."); player.Advance(1); Check(player.CurrentAnimation == "a", "Second queue transition."); player.ClearQueue(); player.AnimationSetNext("a", "b"); player.Advance(1); Check(player.CurrentAnimation == "b", "Configured next transition."); player.AnimationSetNext("a", "");
        a.LoopMode = SpriteFrames.LoopMode.Linear; player.Play("a"); player.Seek(.9); player.Advance(2.3); Near(player.CurrentAnimationPosition, .2); a.LoopMode = SpriteFrames.LoopMode.PingPong; player.Seek(.9); player.Advance(.3); Near(player.CurrentAnimationPosition, .8); Check(player.GetPlayingSpeed() < 0, "Ping-pong direction."); player.Advance(.9); Near(player.CurrentAnimationPosition, .1); Check(player.GetPlayingSpeed() > 0, "Ping-pong reverse boundary.");
        player.SpeedScale = 0; player.Advance(10); Near(player.CurrentAnimationPosition, .1); Check(player.IsPlaying(), "Zero rate keeps enabled."); player.SpeedScale = 1; player.CallbackModeProcess = AnimationMixer.AnimationCallbackModeProcess.Manual; tree.ProcessFrame(.1); Near(player.CurrentAnimationPosition, .1); player.CallbackModeProcess = AnimationMixer.AnimationCallbackModeProcess.Physics; tree.ProcessFrame(.1); Near(player.CurrentAnimationPosition, .1); tree.PhysicsFrame(.1); Near(player.CurrentAnimationPosition, .2);
        player.Active = false; player.Advance(.2); Near(player.CurrentAnimationPosition, .2); player.Active = true; Reject<NotSupportedException>(() => player.Play("a", .5)); Reject<ArgumentOutOfRangeException>(() => player.Advance(double.NaN)); Reject<InvalidOperationException>(() => Task.Run(() => player.Seek(.5)).GetAwaiter().GetResult());
        player.Stop(true); Near(target.Value, 2); player.Stop(); Near(target.Value, 0); Check(player.AssignedAnimation == "a" && player.CurrentAnimation == "", "Stopped selection and reset state.");
        player.AssignedAnimation = "b"; Check(!player.IsPlaying() && player.CurrentAnimationPosition == 0, "Stopped assignment rewinds without starting."); player.Play("b"); player.Advance(.1); player.CurrentAnimation = "b"; Near(player.CurrentAnimationPosition, .1); player.AssignedAnimation = "a"; Check(player.CurrentAnimation == "a", "Active assignment switches."); player.Advance(.2); player.CurrentAnimation = "[stop]"; Check(player.IsPlaying(), "Current stop is deferred while attached."); tree.FlushDeferred(); Check(!player.IsPlaying() && player.CurrentAnimationPosition == 0, "Deferred current stop resets.");
        Reject<ArgumentException>(() => player.AddAnimationLibrary("named", library)); using var namedLibrary = new AnimationLibrary(); namedLibrary.AddAnimation("a", a); player.AddAnimationLibrary("named", namedLibrary); Check(player.GetAnimation("named/a") == a, "Qualified namespace."); player.RenameAnimationLibrary("named", "other"); Check(player.HasAnimation("other/a") && !player.HasAnimation("named/a"), "Namespace rename."); player.RemoveAnimationLibrary("other");
    }
    private static void Discrete()
    {
        using var a = Timeline(Value, "target:Value"); a.ValueTrackSetUpdateMode(0, Animation.UpdateMode.Discrete);
        a.TrackInsertKey(0, .25, 2.5); a.TrackInsertKey(0, .5, 5d); a.TrackInsertKey(0, .75, 7.5);
        using var library = new AnimationLibrary(); library.AddAnimation("a", a); var root = new Node(); var target = new Probe { Name = "target" }; var player = new AnimationPlayer(); root.AddChild(target); root.AddChild(player); using var tree = new SceneTree(root); player.AddAnimationLibrary("", library);
        var values = new List<double>(); target.Hook = () => values.Add(target.Value); player.Play("a"); player.Advance(.8); Check(values.SequenceEqual([0, 2.5, 5, 7.5]), "Discrete update writes each crossed key in forward order."); values.Clear(); player.Advance(.1); Check(values.Count == 0, "Discrete update does not rewrite an uncrossed key.");
        player.PlayBackwards("a"); player.Seek(1); values.Clear(); player.Advance(.8); Check(values.SequenceEqual([10, 7.5, 5, 2.5]), "Discrete reverse order.");
        a.LoopMode = SpriteFrames.LoopMode.Linear; player.Stop(true); player.Play("a"); player.Advance(0); values.Clear(); player.Advance(2.3); Check(values.SequenceEqual([2.5, 5, 7.5, 10, 0, 2.5, 5, 7.5, 10, 0, 2.5]), "Discrete multiple-loop order preserves every crossed key and seam.");
        target.Hook = null; for (var i = 0; i < 20; i++) player.Advance(.11); var before = GC.GetAllocatedBytesForCurrentThread(); for (var i = 0; i < 256; i++) player.Advance(.11); Check(GC.GetAllocatedBytesForCurrentThread() == before, "256 warmed discrete/loop updates allocate zero bytes.");
        player.Stop(true); target.Hook = () => player.Pause(); player.Play("a"); player.Advance(.8); Check(!player.IsPlaying() && target.Value == 0, "Reentrant discrete setter abandons later crossed keys.");
        player.Stop(true); var priorWrites = target.Writes; target.Hook = () => a.TrackSetEnabled(0, false); player.Play("a"); player.Advance(.8); Check(target.Writes == priorWrites + 1, "Resource edit during a discrete setter abandons later crossed keys.");
    }
    private static void Lifecycle()
    {
        using var a = Timeline(Value, "target:Value"); using var library = new AnimationLibrary(); library.AddAnimation("a", a); var root = new Node(); var target = new Probe { Name = "target" }; var player = new AnimationPlayer { Autoplay = "a" }; player.AddAnimationLibrary("", library); root.AddChild(target); root.AddChild(player); using var tree = new SceneTree(root); Check(player.IsPlaying(), "Ready-time autoplay."); tree.ProcessFrame(.1); Near(target.Value, 1);
        root.RemoveChild(target); var replacement = new Probe { Name = "target" }; root.AddChild(replacement); tree.ProcessFrame(.1); Near(replacement.Value, 2); Near(target.Value, 1); target.Dispose();
        a.TrackSetKeyValue(0, 1, 20d); tree.ProcessFrame(.1); Near(replacement.Value, 6); player.ClearCaches(); tree.ProcessFrame(.1); Near(replacement.Value, 8);
        replacement.Hook = () => player.Play("a"); player.Advance(.1); Check(player.IsPlaying(), "Reentrant setter playback remains live."); replacement.Hook = () => throw new ApplicationException("Expected setter failure."); Reject<ApplicationException>(() => player.Advance(.1)); Check(!player.IsPlaying(), "Setter failure pauses controller."); replacement.Hook = null;
        player.Play("a"); library.RemoveAnimation("a"); player.Advance(.1); Check(!player.IsPlaying(), "Removed playback source pauses."); library.AddAnimation("a", a); player.Play("a"); a.Dispose(); player.Advance(.1); Check(!player.IsPlaying(), "Disposed source pauses."); Check(!library.IsDisposed, "Controller does not own libraries.");
        using var packed = new PackedScene(); using var packRoot = new Node(); var packedPlayer = new AnimationPlayer(); packRoot.AddChild(packedPlayer); packedPlayer.Owner = packRoot; Reject<NotSupportedException>(() => packed.Pack(packRoot));
    }
    private static void Warm()
    {
        using var a = Timeline(Value, "target:Value"); a.LoopMode = SpriteFrames.LoopMode.Linear; using var library = new AnimationLibrary(); library.AddAnimation("a", a); var root = new Node(); var target = new Probe { Name = "target" }; var player = new AnimationPlayer(); root.AddChild(target); root.AddChild(player); using var tree = new SceneTree(root); player.AddAnimationLibrary("", library); player.Play("a"); for (var i = 0; i < 16; i++) tree.ProcessFrame(.01);
        var before = GC.GetAllocatedBytesForCurrentThread(); for (var i = 0; i < 256; i++) tree.ProcessFrame(.01); Check(GC.GetAllocatedBytesForCurrentThread() == before, "256 warmed scene updates allocate zero bytes."); a.TrackSetInterpolationType(0, Animation.InterpolationType.CubicAngle); player.Advance(.01); before = GC.GetAllocatedBytesForCurrentThread(); for (var i = 0; i < 256; i++) player.Advance(.01); Check(GC.GetAllocatedBytesForCurrentThread() == before, "256 warmed angular-cubic updates allocate zero bytes.");
    }
    private static Animation Timeline<TOwner>(PropertyDescriptor<TOwner, double> descriptor, string path, double start = 0, double end = 1) where TOwner : Node
    { var a = new Animation(); var t = a.AddTrack(descriptor); a.TrackSetPath(t, path); a.TrackInsertKey(t, start, (start == 0 ? 0d : start)); a.TrackInsertKey(t, end, (end == 1 ? 10d : end)); return a; }
    internal static void RunHost()
    {
        var backend = Environment.GetEnvironmentVariable("ELECTRON2D_ANIMATION_RENDERER") ?? "gpu"; var settings = ProjectSettings.Instance; var prior = settings.Get(ProjectSettings.RenderingMethod); settings.Set(ProjectSettings.RenderingMethod, backend);
        try
        {
            for (var run = 0; run < 2; run++)
            {
                using var animation = new Animation(); var track = animation.AddTrack(Position); animation.TrackSetPath(track, "box:Position"); animation.TrackInsertKey(track, 0, new Vector2(16, 16)); animation.TrackInsertKey(track, 1, new Vector2(48, 16)); using var library = new AnimationLibrary(); library.AddAnimation("move", animation);
                var window = new Window { Size = new(80, 64), Title = "Electron2D scene animation" }; var box = new Box { Name = "box" }; var player = new AnimationPlayer { CallbackModeProcess = AnimationMixer.AnimationCallbackModeProcess.Manual }; player.AddAnimationLibrary("", library); window.AddChild(box); window.AddChild(player); var stage = 0;
                box.Start = () => { var renderer = RenderingServer.Instance!; renderer.SetDefaultClearColor(Colors.Black); player.Play("move"); player.Advance(0); renderer.FramePostDraw += () => { using var image = renderer.Readback(); var x = stage == 0 ? 16 : stage == 1 ? 32 : 48; Check(image.GetPixel(x, 16).R > .9f && image.GetPixel(x, 16).G < .1f, "Animated position reaches rendered pixels."); if (stage > 0) Check(image.GetPixel(16, 16).R < .1f, "Previous position cleared."); stage++; if (stage == 3) window.Tree!.Quit(); else player.Advance(.5); }; };
                Check(Engine.Instance.Run(window) == 0 && stage == 3 && window.IsDisposed && !animation.IsDisposed && !library.IsDisposed, "Rendered host and borrowed resources cleaned up."); Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(new { scenario = "scene-animation-host", backend, run, stages = stage, cleaned = window.IsDisposed }));
            }
        }
        finally { settings.Set(ProjectSettings.RenderingMethod, prior); }
    }
    private sealed class Box : Entity { internal Action? Start; protected override void OnReady() => Start?.Invoke(); protected override void OnDraw() => DrawRect(new(-3, -3, 6, 6), Colors.Red); }
    private sealed class Probe : Node { internal double Value; internal string Text = ""; internal int Writes; internal Action? Hook; }
    private static void Near(double a, double b) => Check(Math.Abs(a - b) < 1e-8, $"Expected {b}, got {a}.");
    private static void Check(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
    private static void Reject<T>(Action action) where T : Exception { try { action(); } catch (T) { return; } throw new InvalidOperationException("Expected " + typeof(T).Name); }
}
