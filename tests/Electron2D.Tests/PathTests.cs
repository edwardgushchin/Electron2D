using Mathf = Electron2D.Mathf;
using Electron2D;
using ScenePath = Electron2D.Path;

internal static class PathTests
{
    internal static void Run()
    {
        Sampling(); LifetimesAndCopies(); FailureAndThreads();
        Console.WriteLine("Scene path and follower checks passed.");
    }

    private static PathCurve Line(Vector2 end)
    {
        var curve = new PathCurve(); curve.AddPoint(Vector2.Zero, outHandle: end / 3); curve.AddPoint(end, inHandle: -end / 3); return curve;
    }

    private static void Sampling()
    {
        using var curve = Line(new(30, 0)); using var path = new ScenePath { Curve = curve, Position = new(3, 4) };
        var f = new PathFollow(); path.AddChild(f); var child = new Entity { Position = new(2, 3) }; f.AddChild(child);
        Check(f.Progress == 0 && f.ProgressRatio == 0 && f.HOffset == 0 && f.VOffset == 0 && f.Rotates && f.CubicInterp && f.Loop, "Follower defaults.");
        f.Progress = 45; Near(f.Position, Vector2.Zero); Reject<InvalidOperationException>(() => f.ProgressRatio = .5f);
        using var tree = new SceneTree(path); Near(f.Position, new(30, 0)); Near(f.Progress, 45); Near(f.ProgressRatio, 1.5f);
        f.Progress = 45; Near(f.Progress, 15); Near(f.Position, new(15, 0)); Near(child.GlobalPosition, new(20, 7));
        f.Progress = 60; Near(f.Progress, 30); f.Progress = -60; Near(f.Progress, 30); f.Progress = -5; Near(f.Progress, 25);
        f.Progress = 0; Near(f.Progress, 0); f.ProgressRatio = .5f; Near(f.Position, new(15, 0));
        f.Loop = false; f.Progress = -10; Near(f.Progress, 0); f.Progress = 90; Near(f.Progress, 30);
        f.ProgressRatio = 2; Near(f.ProgressRatio, 1); f.ProgressRatio = -1; Near(f.ProgressRatio, 0);
        f.HOffset = 2; f.VOffset = 3; f.Progress = 15; Near(f.Position, new(17, 3));
        using var vertical = Line(new(0, 30)); path.Curve = vertical; Near(f.Position, new(-3, 17)); Near(f.Rotation, Mathf.Pi / 2);
        f.Scale = new(2, 3); f.Skew = .2f; f.Rotates = false; Near(f.Position, new(2, 18)); Near(f.Rotation, Mathf.Pi / 2); Near(f.Scale, new(2, 3)); Near(f.Skew, .2f);
        f.Rotation = .7f; f.Progress = 10; Near(f.Rotation, .7f); f.Rotates = true; Near(f.Rotation, Mathf.Pi / 2); Near(f.Scale, new(2, 3)); Near(f.Skew, .2f);
        f.HOffset = f.VOffset = 0; f.Progress = 25;
        using var shortCurve = Line(new(0, 10)); path.Curve = shortCurve; Near(f.Progress, 25); Near(f.ProgressRatio, 2.5f); Near(f.Position, new(0, 10));
        f.Loop = true; Near(f.Progress, 25); f.Progress = f.Progress; Near(f.Progress, 5);
        var position = f.Position; path.Curve = null; f.Progress = 90; Near(f.Position, position); Near(f.ProgressRatio, 0);
        using var empty = new PathCurve(); path.Curve = empty; f.Progress = 5; Near(f.Progress, 0); Near(f.Position, position); Reject<InvalidOperationException>(() => f.ProgressRatio = 0);
        path.Curve = curve; f.Rotates = false; f.CubicInterp = false; f.Progress = 0;
        f.CreateTween().TweenProperty(f, n => n.Progress, (n, value) => n.Progress = value, 30f, 1);
        tree.ProcessFrame(.5); Near(f.Progress, 15); Near(f.Position, new(15, 0));
        tree.ProcessFrame(.5); Near(f.Progress, 30);
        // Policy changes do not resample a manually placed node; a later progress edit does.
        f.Position = new(7, 9); f.CubicInterp = true; f.Loop = false; Near(f.Position, new(7, 9)); f.Progress = 15; Near(f.Position, new(15, 0));
        Reject<ArgumentOutOfRangeException>(() => f.Progress = float.NaN); Reject<ArgumentOutOfRangeException>(() => f.ProgressRatio = float.PositiveInfinity);
        Reject<ArgumentOutOfRangeException>(() => f.HOffset = float.NaN); Reject<ArgumentOutOfRangeException>(() => f.VOffset = float.NegativeInfinity);
        f.Position = new(7, 9); path.Curve = curve; Near(f.Position, new(15, 0));
        using var bent = new PathCurve { BakeInterval = 20 }; bent.AddPoint(Vector2.Zero, outHandle: new(0, 30)); bent.AddPoint(new(30, 0), inHandle: new(0, 30));
        path.Curve = bent; f.CubicInterp = false; f.Progress = 7.3f; var linear = f.Position;
        Near(linear, bent.SampleBaked(7.3f, false)); f.CubicInterp = true; Near(f.Position, linear); f.Progress = 7.3f;
        Near(f.Position, bent.SampleBaked(7.3f, true)); Check(f.Position.DistanceTo(linear) > .1f, "Cubic policy changes actual curved-path sampling on the next update.");
        path.Curve = curve; f.Rotates = true;
        for (var i = 0; i < 100; i++) f.Progress = i % 30;
        var allocated = GC.GetAllocatedBytesForCurrentThread(); for (var i = 0; i < 1000; i++) f.Progress = i % 30;
        Check(GC.GetAllocatedBytesForCurrentThread() == allocated, "Warm progress updates allocate nothing.");
    }

    private static void LifetimesAndCopies()
    {
        using var a = Line(new(30, 0)); using var b = Line(new(0, 30)); using var root = new Node();
        var p = new ScenePath { Name = "A", Curve = a }; var q = new ScenePath { Name = "B", Curve = b }; var f = new PathFollow { Name = "Follower", Progress = 15 };
        root.AddChild(p); root.AddChild(q); p.AddChild(f); using var tree = new SceneTree(root);
        f.Reparent(q, false); Near(f.Position, new(0, 15)); p.Curve = b; Near(f.Position, new(0, 15));
        f.Reparent(root, false); var old = f.Position; f.Progress = 24; Near(f.Position, old); Near(f.ProgressRatio, 0);
        f.Reparent(p, false); Near(f.Position, new(0, 24));
        // Only direct followers bind: neutral intermediates and detached parents do not.
        var neutral = new Node(); p.AddChild(neutral); f.Reparent(neutral, false); f.Progress = 10; Near(f.Position, new(0, 24));
        f.Reparent(p, false); Near(f.Position, new(0, 10)); p.Curve = a; f.Progress = 12; f.HOffset = 2; f.VOffset = 3; f.Rotates = false; f.CubicInterp = false; f.Loop = false;
        var beforeReparent = f.GlobalPosition; f.Reparent(q, true); Near(f.GlobalPosition, beforeReparent); f.Progress = 12; Near(f.Position, new(2, 15)); f.Reparent(p, false);
        f.Owner = p; using var packed = new PackedScene(); packed.Pack(p);
        using var shared = (ScenePath)packed.Instantiate(); Check(ReferenceEquals(shared.Curve, a), "Packed curves are borrowed by default.");
        var sf = (PathFollow)shared.GetChild(0); Near(sf.Progress, 12); Check(sf.HOffset == 2 && sf.VOffset == 3 && !sf.Rotates && !sf.CubicInterp && !sf.Loop, "Stored follower properties survive packing.");
        Check(!sf.GetPropertyList().Single(x => x.Name == nameof(PathFollow.ProgressRatio)).IsStored, "Derived ratio is not stored.");
        a.ResourceLocalToScene = true; packed.Pack(p); using var local = (ScenePath)packed.Instantiate(); var localCurve = local.Curve!; Check(!ReferenceEquals(a, localCurve), "Scene-local curve duplicates.");
        using (var localTree = new SceneTree(local)) { var lf = (PathFollow)local.GetChild(0); Near(lf.Position, new(14, 3)); localCurve.SetPointPosition(1, new(30, 10)); Check(lf.Position != new Vector2(14, 3), "Local curve drives its copied follower."); }
        Check(localCurve.IsDisposed && !a.IsDisposed, "Root owns local curve copies only.");
        f.Dispose(); a.SetPointPosition(1, new(20, 0)); Check(!a.IsDisposed, "Disposed followers disconnect through parent membership.");
        using var detached = new ScenePath { Curve = a }; detached.Dispose(); a.SetPointPosition(1, new(21, 0));
        using var dead = new PathCurve(); dead.Dispose(); Reject<ObjectDisposedException>(() => p.Curve = dead);
    }

    private static void FailureAndThreads()
    {
        using var curve = Line(new(30, 0)); using var root = new Node(); var path = new ScenePath { Curve = curve }; root.AddChild(path);
        var first = new PathFollow { Name = "first", Progress = 15 }; var second = new PathFollow { Name = "second", Progress = 15 }; path.AddChild(first); path.AddChild(second); using var tree = new SceneTree(root);
        first.NotifyLocalTransformChanges = second.NotifyLocalTransformChanges = true;
        var owner = Environment.CurrentManagedThreadId; var events = 0; second.LocalTransformChanged += _ => { Check(Environment.CurrentManagedThreadId == owner, "Scene events stay on the owner thread."); events++; };
        Task.Run(() => curve.SetPointPosition(1, new(30, 15))).GetAwaiter().GetResult(); Check(events == 0, "Worker curve edits defer transforms.");
        tree.FlushDeferred(); Check(events > 0 && second.Position.Y > 0, "Deferred worker update reaches followers.");
        Task.Run(() => Reject<InvalidOperationException>(() => first.Progress = 1)).GetAwaiter().GetResult();
        Action<CanvasItem> fail = _ => throw new ApplicationException("path observer"); first.LocalTransformChanged += fail;
        Reject<AggregateException>(() => curve.SetPointPosition(1, new(30, 30))); Check(second.Position.Y > 5, "One failed follower does not skip siblings."); first.LocalTransformChanged -= fail;
        var switched = false;
        first.LocalTransformChanged += _ => { if (switched) return; switched = true; first.Progress = 7; };
        using var vertical = Line(new(0, 30)); path.Curve = vertical; Near(first.Progress, 7); Near(first.Position, new(0, 7));
        // Deferred callbacks from a prior resource or membership cannot override later placement.
        Task.Run(() => vertical.SetPointPosition(1, new(0, 40))).GetAwaiter().GetResult(); path.Curve = curve; second.Position = new(99, 99); tree.FlushDeferred(); Near(second.Position, new(99, 99));
        Task.Run(() => curve.SetPointPosition(1, new(40, 40))).GetAwaiter().GetResult(); root.RemoveChild(path); root.AddChild(path); second.Position = new(98, 98); tree.FlushDeferred(); Near(second.Position, new(98, 98));
        first.Dispose(); second.Dispose(); path.Dispose(); Task.Run(() => curve.SetPointPosition(1, new(20, 20))).GetAwaiter().GetResult(); tree.FlushDeferred();
        Reject<ObjectDisposedException>(() => first.Progress = 0);
    }

    private static void Near(float actual, float expected) => Check(Math.Abs(actual - expected) < .001f, $"Expected {expected}, got {actual}.");
    private static void Near(Vector2 actual, Vector2 expected) { Near(actual.X, expected.X); Near(actual.Y, expected.Y); }
    private static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    private static void Reject<T>(Action action) where T : Exception { try { action(); } catch (T) { return; } throw new InvalidOperationException("Expected " + typeof(T).Name); }
}
