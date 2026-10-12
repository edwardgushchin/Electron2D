using Electron2D;

internal static class AnimationOptimizeTests
{
    private static readonly PropertyDescriptor<Probe, double> Scalar = new("Value", n => n.Value, (n, v) => n.Value = v);
    private static readonly PropertyDescriptor<Probe, float> Single = new("Single", n => n.Single, (n, v) => n.Single = v);
    private static readonly PropertyDescriptor<Probe, Vector2> Vector = new("Vector", n => n.Vector, (n, v) => n.Vector = v);
    private static readonly PropertyDescriptor<Probe, int> Integer = new("Integer", n => n.Integer, (n, v) => n.Integer = v);

    internal static void Run()
    {
        Reduction(); Tolerances(); PreservedCurves(); Integration(); Notifications();
        Console.WriteLine("Animation optimization: typed reduction, tolerances, protected curves, playback/cache invalidation and lifetime passed.");
    }
    private static void Reduction()
    {
        using var clip = Curve(0, 1, 2, 3, 4);
        clip.Length = 4;
        var vector = clip.AddTrack(Vector);
        var single = clip.AddTrack(Single);
        for (var i = 0; i < 5; i++) { clip.TrackInsertKey(vector, i, new Vector2(i * 3, i * -2)); clip.TrackInsertKey(single, i, (float)i); }
        using var original = (Animation)clip.Duplicate();
        clip.Optimize(0, 0, 15);
        Check(Enumerable.Range(0, 3).All(t => clip.TrackGetKeyCount(t) == 2), "All three built-in value families reduce.");
        for (var i = 0; i <= 64; i++)
        {
            var time = i / 16d;
            Near(clip.ValueTrackInterpolate<double>(0, time), original.ValueTrackInterpolate<double>(0, time));
            Check(clip.ValueTrackInterpolate<Vector2>(vector, time) == original.ValueTrackInterpolate<Vector2>(vector, time), "Vector samples survive reduction.");
            Near(clip.ValueTrackInterpolate<float>(single, time), original.ValueTrackInterpolate<float>(single, time));
        }
        Check(original.TrackGetKeyCount(0) == 5, "Duplicate containers remain independent.");
        using var constant = Curve(7, 7, 7, 7); constant.Optimize(); Check(constant.TrackGetKeyCount(0) == 1, "Constant becomes its first key.");
        using var irregular = new Animation(); var t = irregular.AddTrack(Scalar);
        irregular.TrackInsertKey(t, -2, -4d); irregular.TrackInsertKey(t, .5, 1d); irregular.TrackInsertKey(t, 100, 200d);
        irregular.Optimize(); Check(irregular.TrackGetKeyCount(t) == 2 && irregular.TrackGetKeyTime(t, 0) == -2 && irregular.TrackGetKeyTime(t, 1) == 100, "Time-aware speed and nonconstant endpoints.");
        using var angle = Curve(6.1, 0, Math.Tau - 6.1); angle.TrackSetInterpolationType(0, Animation.InterpolationType.LinearAngle);
        var before = angle.ValueTrackInterpolate<double>(0, .5); angle.Optimize(0, 0); Check(angle.TrackGetKeyCount(0) == 2, "Angle crosses the wrap."); Near(angle.ValueTrackInterpolate<double>(0, .5), before);
        using var halfTurn = Curve(0, Math.PI / 2, Math.PI); halfTurn.TrackSetInterpolationType(0, Animation.InterpolationType.LinearAngle); halfTurn.Optimize(); Check(halfTurn.TrackGetKeyCount(0) == 3, "Half-turn direction remains unambiguous.");
        using var wrappedConstant = Curve(0, Math.Tau); wrappedConstant.TrackSetInterpolationType(0, Animation.InterpolationType.LinearAngle); wrappedConstant.Optimize(); Check(wrappedConstant.TrackGetKeyCount(0) == 1, "Equivalent angle endpoints.");
    }
    private static void Tolerances()
    {
        using var strict = Curve(0, 1, 2.01); strict.Optimize(0, 0, 6); Check(strict.TrackGetKeyCount(0) == 3, "Strict speed tolerance.");
        strict.Optimize(.02, 0, 6); Check(strict.TrackGetKeyCount(0) == 2, "Relative speed tolerance.");
        using var precision = Curve(0, .0001, 0); precision.Optimize(0, 0, 6); Check(precision.TrackGetKeyCount(0) == 3, "Precision retains small excursion."); precision.Optimize(0, 0, 3); Check(precision.TrackGetKeyCount(0) == 1, "Absolute precision collapses near constant.");
        using var turn = new Animation(); var t = turn.AddTrack(Vector); turn.TrackInsertKey(t, 0, Vector2.Zero); turn.TrackInsertKey(t, 1, Vector2.Right); turn.TrackInsertKey(t, 2, new Vector2(1, 1));
        turn.Optimize(); Check(turn.TrackGetKeyCount(t) == 3, "Equal-speed right-angle corner must survive.");
        using var slight = new Animation(); t = slight.AddTrack(Vector); slight.TrackInsertKey(t, 0, Vector2.Zero); slight.TrackInsertKey(t, 1, Vector2.Right); slight.TrackInsertKey(t, 2, new Vector2(2, .01f));
        slight.Optimize(0, 0); Check(slight.TrackGetKeyCount(t) == 3, "Strict direction tolerance."); slight.Optimize(.01, .01); Check(slight.TrackGetKeyCount(t) == 2, "Explicit direction tolerance.");
        using var stopped = Curve(0, 0, 1); stopped.Optimize(); Check(stopped.TrackGetKeyCount(0) == 3, "Stationary to moving boundary.");
        using var reverse = Curve(0, 1, 0); reverse.Optimize(); Check(reverse.TrackGetKeyCount(0) == 3, "Velocity reversal.");
        foreach (var value in new[] { double.NaN, double.PositiveInfinity, -.01, 1.01 })
        { Reject<ArgumentOutOfRangeException>(() => reverse.Optimize(value)); Reject<ArgumentOutOfRangeException>(() => reverse.Optimize(allowedAngularError: value)); }
        Reject<ArgumentOutOfRangeException>(() => reverse.Optimize(precision: -1)); Reject<ArgumentOutOfRangeException>(() => reverse.Optimize(precision: 16));
        Check(reverse.TrackGetKeyCount(0) == 3, "Invalid arguments leave keys intact.");
    }
    private static void PreservedCurves()
    {
        using var discrete = Curve(2, 2, 2); discrete.ValueTrackSetUpdateMode(0, Animation.UpdateMode.Discrete); discrete.Optimize(); Check(discrete.TrackGetKeyCount(0) == 3, "Repeated discrete writes are events.");
        using var cubic = Curve(0, 1, 2); cubic.TrackSetInterpolationType(0, Animation.InterpolationType.Cubic); cubic.Optimize(); Check(cubic.TrackGetKeyCount(0) == 3, "Cubic neighbors are retained.");
        using var eased = Curve(0, 1, 2); eased.TrackSetKeyTransition(0, 1, 2); eased.Optimize(); Check(eased.TrackGetKeyCount(0) == 3, "Easing is retained in both directions.");
        using var custom = new Animation(); var t = custom.AddTrack(Scalar, interpolate: (a, b, w) => a + (b - a) * w * w);
        for (var i = 0; i < 3; i++) custom.TrackInsertKey(t, i, (double)i); custom.Optimize(); Check(custom.TrackGetKeyCount(t) == 3, "Custom interpolator is never invoked or presumed linear.");
        using var unsupported = new Animation(); t = unsupported.AddTrack(Integer); for (var i = 0; i < 3; i++) unsupported.TrackInsertKey(t, i, i); unsupported.Optimize(); Check(unsupported.TrackGetKeyCount(t) == 3, "Integer values keep typed semantics.");
        using var nearest = Curve(0, 1, 2); nearest.TrackSetInterpolationType(0, Animation.InterpolationType.Nearest); nearest.Optimize(); Check(nearest.TrackGetKeyCount(0) == 3, "Nearest steps retain boundaries.");
        using var held = Curve(2, 2, 2); held.TrackSetInterpolationType(0, Animation.InterpolationType.Nearest); held.Optimize(); Check(held.TrackGetKeyCount(0) == 1, "Nearest constant reduction.");
        using var nonfinite = Curve(0, double.NaN, 2); nonfinite.Optimize(); Check(nonfinite.TrackGetKeyCount(0) == 3, "Nonfinite authored values remain.");
        using var huge = Curve(-double.MaxValue, 0, double.MaxValue); huge.Optimize(); Check(huge.TrackGetKeyCount(0) == 3, "Overflow cannot authorize key loss.");
        using var hugeSingle = new Animation(); t = hugeSingle.AddTrack(Single); hugeSingle.TrackInsertKey(t, 0, -float.MaxValue); hugeSingle.TrackInsertKey(t, 1, 0f); hugeSingle.TrackInsertKey(t, 2, float.MaxValue); hugeSingle.Optimize(); Check(hugeSingle.TrackGetKeyCount(t) == 3, "Float endpoint difference must remain representable.");
        using var hugeVector = new Animation(); t = hugeVector.AddTrack(Vector); hugeVector.TrackInsertKey(t, 0, new Vector2(-float.MaxValue, 0)); hugeVector.TrackInsertKey(t, 1, Vector2.Zero); hugeVector.TrackInsertKey(t, 2, new Vector2(float.MaxValue, 0)); hugeVector.Optimize(); Check(hugeVector.TrackGetKeyCount(t) == 3, "Vector endpoint difference must remain representable.");
        using var empty = new Animation(); empty.Optimize(); t = empty.AddTrack(Scalar); empty.TrackInsertKey(t, 0, 1d); empty.Optimize(); Check(empty.TrackGetKeyCount(t) == 1, "Empty and singleton safe.");
    }
    private static void Integration()
    {
        using var clip = Curve(0, 1, 2); clip.Length = 2; clip.LoopMode = SpriteFrames.LoopMode.Linear;
        using var baseline = (Animation)clip.Duplicate();
        using var library = new AnimationLibrary(); library.AddAnimation("move", clip);
        using var root = new SubViewport(); using var tree = new SceneTree(root);
        var probe = new Probe(); var player = new AnimationPlayer { CallbackModeProcess = AnimationMixer.AnimationCallbackModeProcess.Manual };
        root.AddChild(probe); probe.AddChild(player); player.AddAnimationLibrary("", library); player.Play("move"); player.Advance(.25); Near(probe.Value, .25);
        clip.Optimize(); Check(clip.TrackGetKeyCount(0) == 2, "Active clip reduction.");
        for (var i = 0; i < 8; i++) { player.Advance(.125); Near(probe.Value, .375 + i * .125); }
        player.Seek(.5, update: true); Near(probe.Value, .5); player.Stop(); player.PlayBackwards("move"); player.Advance(.25); Near(probe.Value, 1.75);
        for (var i = -16; i <= 48; i++) { var time = i / 16d; Near(clip.ValueTrackInterpolate<double>(0, time), baseline.ValueTrackInterpolate<double>(0, time)); }
    }
    private static void Notifications()
    {
        using var clip = Curve(0, 1, 2); var changes = 0; clip.Changed += _ => changes++;
        clip.Optimize(); Check(changes == 1, "One notification per operation."); clip.Optimize(); Check(changes == 1, "No-op stays quiet.");
        using var broken = Curve(0, 1, 2); broken.Changed += _ => throw new InvalidOperationException("observer"); Reject<InvalidOperationException>(() => broken.Optimize()); Check(broken.TrackGetKeyCount(0) == 2, "Observer sees committed keys.");
        using var reentrant = Curve(0, 1, 2); reentrant.Changed += _ => reentrant.Optimize(); reentrant.Optimize(); Check(reentrant.TrackGetKeyCount(0) == 2, "Reentrant no-op terminates.");
        clip.Dispose(); Reject<ObjectDisposedException>(() => clip.Optimize());
    }
    private static Animation Curve(params double[] values)
    { var clip = new Animation(); var track = clip.AddTrack(Scalar); for (var i = 0; i < values.Length; i++) clip.TrackInsertKey(track, i, values[i]); return clip; }
    private sealed class Probe : Node { internal double Value; internal float Single; internal Vector2 Vector; internal int Integer; }
    private static void Near(double actual, double expected) => Check(Math.Abs(actual - expected) < 1e-8, $"Expected {expected}, got {actual}.");
    private static void Check(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
    private static void Reject<T>(Action action) where T : Exception { try { action(); } catch (T) { return; } throw new InvalidOperationException($"Expected {typeof(T).Name}."); }
}
