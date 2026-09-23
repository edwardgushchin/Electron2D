using Electron2D;

internal static class CanvasTimingTests
{
    internal static void Run()
    {
        using var node = new Painter(); var vertices = new List<CanvasVertex>(); var batches = new List<CanvasBatch>();
        void Record(Action<Painter> paint) { node.Paint = paint; node.InvalidateCanvas(); node.PrepareCanvas(); }
        void Replay(double time = 0, Transform? transform = null) { vertices.Clear(); batches.Clear(); node.AppendCanvas(vertices, batches, transform ?? Transform.Identity, time); }
        Reject<InvalidOperationException>(() => node.DrawAnimationSlice(1, 0, 1));
        Reject<InvalidOperationException>(node.DrawEndAnimation);
        Record(n =>
        {
            n.DrawAnimationSlice(2, 0, 1, 0.25);
            n.DrawSetTransform(new(10, 0)); n.DrawRect(new(0, 0, 2, 2), Colors.Red);
            n.DrawAnimationSlice(2, 1, 2, 0.25);
            n.DrawRect(new(0, 0, 2, 2), Colors.Green);
            n.DrawEndAnimation(); n.DrawRect(new(20, 0, 2, 2), Colors.Blue);
        });
        Replay(0.25); Check(vertices.Count == 12 && vertices[0].Color == Colors.Red && vertices[0].Position.X == 10 && vertices[6].Position.X == 30, "Inclusive begin and end command preserve executed transform.");
        Replay(1.25); Check(vertices.Count == 12 && vertices[0].Color == Colors.Green && vertices[0].Position.X == 0 && vertices[6].Position.X == 20, "Exclusive end; skipped transform does not affect later interval or end.");
        Replay(0); Check(vertices[0].Color == Colors.Green, "Positive modulo handles time before offset.");
        Replay(2.25); Check(vertices[0].Color == Colors.Red, "Period wraps without recording.");
        Check(node.Draws == 1, "Interval replay does not invoke OnDraw.");
        Record(n => { n.DrawAnimationSlice(0, -1, 1); n.DrawRect(new(0, 0, 2, 2), Colors.Red); n.DrawEndAnimation(); n.DrawCircle(Vector2.Zero, 2, Colors.Blue); });
        Replay(); Check(vertices.Count == 192 && vertices.All(v => v.Color == Colors.Blue), "Zero period hides; end restores all command families.");
        Record(n => { n.DrawAnimationSlice(-2, -1, 0); n.DrawRect(new(0, 0, 2, 2), Colors.Green); });
        Replay(1.5); Check(vertices.Count == 6, "Signed period preserves negative phase.");
        Replay(0.5); Check(vertices.Count == 0, "Signed period does not use absolute length.");
        Record(n => { n.DrawAnimationSlice(2, 1.5, 0.5); n.DrawRect(new(0, 0, 2, 2), Colors.Red); });
        Replay(); Check(vertices.Count == 0, "Reversed interval boundaries do not implicitly wrap.");
        Record(n => { n.DrawAnimationSlice(2, -1, 3); n.DrawRect(new(0, 0, 2, 2), Colors.White); });
        Replay(1.9); Check(vertices.Count == 6, "Out-of-period bounds remain meaningful.");
        Record(n => n.DrawRect(new(0, 0, 2, 2), Colors.White)); Replay();
        Check(vertices.Count == 6 && vertices[0].Position == Vector2.Zero, "Rerecord resets interval and transform state.");
        using var pixels = Image.CreateEmpty(1, 1, false, Image.Format.Rgba8);
        using var texture = ImageTexture.CreateFromImage(pixels);
        Record(n => { n.DrawAnimationSlice(2, 1, 2); n.DrawTexture(texture, Vector2.Zero); });
        texture.Dispose(); Replay(0); Check(vertices.Count == 0 && batches.Count == 0, "Hidden commands do not access disposed texture or material state.");
        Reject<ObjectDisposedException>(() => Replay(1));
        Record(n =>
        {
            n.DrawRect(new(0, 0, 2, 2), Colors.Red);
            Reject<ArgumentException>(() => n.DrawAnimationSlice(double.NaN, 0, 1));
            Reject<ArgumentException>(() => n.DrawAnimationSlice(1, double.PositiveInfinity, 1));
            Reject<ArgumentException>(() => n.DrawAnimationSlice(1, 0, double.NaN));
            Reject<ArgumentException>(() => n.DrawAnimationSlice(1, 0, 1, double.NegativeInfinity));
            Reject<ArgumentException>(() => n.DrawRect(new(float.MaxValue, 0, float.MaxValue, 2), Colors.White));
            n.DrawRect(new(3, 0, 2, 2), Colors.Blue);
        });
        Replay(); Check(vertices.Count == 12 && vertices[0].Color == Colors.Red && vertices[6].Color == Colors.Blue, "Caught validation failures preserve already recorded commands and pool slots.");
        node.Paint = n => { n.DrawAnimationSlice(1, 0, 0); n.DrawSetTransform(new(100, 0)); throw new ApplicationException(); };
        node.InvalidateCanvas(); Reject<ApplicationException>(node.PrepareCanvas); Replay(); Check(vertices.Count == 0, "Callback failure discards state commands too.");
        Record(n => n.DrawRect(new(0, 0, 2, 2), Colors.White)); Replay(); Check(vertices[0].Position == Vector2.Zero, "Retry starts with default state.");
        Record(n => n.DrawRect(new(10, 10, -8, -6), Colors.Red)); Replay();
        Check(vertices.Min(v => v.Position.X) == 2 && vertices.Max(v => v.Position.Y) == 10, "Negative rectangle dimensions normalize.");
        Record(n => n.DrawRect(new(0, 0, 10, 6), Colors.White, antialiased: true)); Replay();
        Check(vertices.Count == 54 && vertices[0].Position == new Vector2(0.3125f, 0.3125f) && vertices.Min(v => v.Position.X) == -0.9375f, "Filled rectangle compensates its core and uses local side/corner feathers.");
        Replay(transform: new Transform(0, new(2, 3), 0, Vector2.Zero));
        Check(vertices.Min(v => v.Position.Y) == -2.8125f, "Filled feather scales with geometry.");
        Record(n => n.DrawRect(new(0, 0, 0.8f, 1), Colors.White, antialiased: true)); Replay();
        Check(Mathf.IsEqualApprox(vertices.Min(v => v.Position.X), 0.093749985f), "Small adjusted cores scale down their feather.");
        Record(n => n.DrawRect(new(0, 0, 0, 10), Colors.White, false, 4)); Replay();
        Check(vertices.Count == 6 && vertices.Min(v => v.Position.X) == -2 && vertices.Max(v => v.Position.Y) == 12, "A wide outline fills even a degenerate original rectangle.");
        Record(n => n.DrawRect(new(0, 0, 12, 8), Colors.White, false, 2, true)); Replay(); var rectangle = vertices.ToArray();
        Record(n => n.DrawPolyline([new(0, 0), new(12, 0), new(12, 8), new(0, 8), new(0, 0)], Colors.White, 2, true)); Replay();
        Check(vertices.SequenceEqual(rectangle), "Rectangle outline uses exactly the closed polyline geometry.");
        Record(n => n.DrawRect(new(0, 0, 12, 8), Colors.White, false, -1, true)); Replay(); var thin = vertices.ToArray();
        Record(n => n.DrawRect(new(0, 0, 12, 8), Colors.White, false, -1, false)); Replay(); Check(vertices.SequenceEqual(thin), "Thin rectangular outlines ignore AA.");
        Record(n => n.DrawRect(new(0, 0, 12, 8), Colors.White, false, 0, true)); Replay(); Check(vertices.Count == 0, "Zero-width nondegenerate outline has no geometry.");
        node.Paint = n => { n.DrawAnimationSlice(2, 0, 1); n.DrawSetTransform(new(2, 3)); n.DrawRect(new(0, 0, 12, 8), Colors.White, false, 2, true); n.DrawEndAnimation(); n.DrawRect(new(16, 0, 12, 8), Colors.Red, antialiased: true); };
        void Warm() { node.InvalidateCanvas(); node.PrepareCanvas(); Replay(0.5); Replay(1.5); }
        for (var i = 0; i < 30; i++) Warm(); var before = GC.GetAllocatedBytesForCurrentThread(); for (var i = 0; i < 1000; i++) Warm();
        Check(GC.GetAllocatedBytesForCurrentThread() == before, "Warmed rectangle and interval recording/replay allocate zero bytes.");
        using var tree = new SceneTree(node);
        Check(Task.Run(() => { try { node.DrawAnimationSlice(1, 0, 1); return false; } catch (InvalidOperationException) { return true; } }).Result, "Interval owner-thread guard.");
        tree.Dispose(); Reject<ObjectDisposedException>(node.DrawEndAnimation);
        var settings = ProjectSettings.Instance;
        Check(settings.Get(ProjectSettings.RenderingTimeRolloverSeconds) == 3600, "Render-clock wrap default.");
        Reject<ArgumentOutOfRangeException>(() => settings.Set(ProjectSettings.RenderingTimeRolloverSeconds, 0));
        Reject<ArgumentException>(() => settings.Set(ProjectSettings.RenderingTimeRolloverSeconds, double.NaN));
        Console.WriteLine("Canvas interval, ordered transform, rectangle, validation and zero-allocation checks passed.");
    }
    private sealed class Painter : Entity { internal Action<Painter>? Paint; internal int Draws; protected override void OnDraw() { Draws++; Paint?.Invoke(this); } }
    private static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    private static void Reject<T>(Action action) where T : Exception { try { action(); } catch (T) { return; } throw new InvalidOperationException($"Expected {typeof(T).Name}."); }
}
