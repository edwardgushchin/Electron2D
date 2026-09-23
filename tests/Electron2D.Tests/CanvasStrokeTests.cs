using MathF = Electron2D.MathF;
using Electron2D;

internal static class CanvasStrokeTests
{
    internal static void Run()
    {
        using var node = new Painter(); var vertices = new List<CanvasVertex>(); var batches = new List<CanvasBatch>();
        Reject<InvalidOperationException>(() => node.DrawCircle(Vector2.Zero, 3, Colors.Red));
        void Record(Action<Painter> action, Transform? transform = null)
        {
            node.Paint = action; node.InvalidateCanvas(); node.PrepareCanvas(); vertices.Clear(); batches.Clear(); node.AppendCanvas(vertices, batches, transform ?? Transform.Identity);
        }
        Record(n => n.DrawPolyline([new(0, 0), new(10, 0), new(10, 10)], Colors.White, 4));
        Check(vertices.Count == 12 && vertices.Any(v => v.Position.IsEqualApprox(new Vector2(12, -2))) && vertices.Any(v => v.Position.IsEqualApprox(new Vector2(8, 2))), "Joined strip shares a miter at the corner.");
        Record(n => n.DrawPolyline([new(0, 0), new(10, 0), new(0.1f, 0.01f)], Colors.White, 4));
        Check(vertices.All(v => v.Position.DistanceTo(new Vector2(10, 0)) < 20), "Near-reversal miter is bounded.");
        Record(n => n.DrawPolyline([new(0, 0), new(0, 0), new(10, 0), new(10, 10), new(0, 0)], Colors.White, 2, true));
        Check(vertices.Count > 0 && vertices.All(v => v.Position.IsFinite()), "Repeated and closed endpoints produce finite feathers.");
        Vector2[] points = [new(0, 0), new(10, 0), new(20, 0)]; Color[] colors = [Colors.Red, Colors.Blue];
        Record(n => n.DrawPolylineColors(points, colors, 2));
        Check(vertices[^1].Color == Colors.Blue && vertices[0].Color == Colors.Red, "Missing polyline colors repeat the last supplied color.");
        Array.Fill(points, new Vector2(500, 500)); Array.Fill(colors, Colors.Green);
        vertices.Clear(); node.AppendCanvas(vertices, batches, Transform.Identity); Check(vertices[0].Color == Colors.Red && vertices[^1].Position.X == 20, "Stroke data is copied.");
        Record(n => n.DrawPolylineColors([Vector2.Zero, Vector2.One], [], 2));
        Check(vertices.All(v => v.Color == Colors.White), "Empty polyline colors use white.");
        Record(n => n.DrawPolylineColors([Vector2.Zero, Vector2.One], [Colors.Red, Colors.Blue, new(float.NaN, 0, 0)], 2));
        Check(vertices.All(v => v.Color.IsFinite()), "Unused extra polyline colors are ignored.");
        Record(n => n.DrawMultilineColors([new(0, 0), new(10, 0), new(20, 0), new(30, 0)], [Colors.Red, Colors.Blue], 2));
        Check(vertices.Count == 12 && vertices.Take(6).All(v => v.Color == Colors.Red) && vertices.Skip(6).All(v => v.Color == Colors.Blue), "Multiline colors belong to segments.");
        Record(n => n.DrawLine(new(0, 0), new(10, 0), Colors.White, 2, true));
        Check(vertices.Min(v => v.Position.Y) == -1.75f && vertices.Max(v => v.Position.Y) == 1.75f, "Antialias compensation: width 2 has width 1 core and 1.25 feather.");
        Record(n => n.DrawLine(new(0, 0), new(10, 0), Colors.White, 1, true), new Transform(0, new Vector2(2, 3), 0, Vector2.Zero));
        Check(vertices.Max(v => v.Position.Y) == 2.625f, "Feathers scale in local coordinates.");
        Record(n => n.DrawPolyline([new(0, 0), new(10, 0)], Colors.White, -1, true), new Transform(0, new Vector2(4, 4), 0, Vector2.Zero));
        Check(vertices.Count == 6 && vertices.Max(v => v.Position.Y) - vertices.Min(v => v.Position.Y) == 1, "Thin polyline ignores AA and stays one pixel wide.");
        Record(n => n.DrawDashedLine(new(0, 0), new(11, 0), Colors.White, 2, 2, true));
        Check(vertices.Count == 18 && vertices.Min(v => v.Position.X) == 0 && vertices.Max(v => v.Position.X) == 11, "Aligned dash sequence includes both endpoints.");
        Record(n => n.DrawDashedLine(new(0, 0), new(11, 0), Colors.White, 2, 2, false));
        Check(vertices.Max(v => v.Position.X) == 10, "Unaligned dash leaves the tail.");
        Record(n => n.DrawArc(Vector2.Zero, 10, 0, MathF.Tau * 4, 9, Colors.Red, 2));
        var once = vertices.ToArray(); Record(n => n.DrawArc(Vector2.Zero, 10, 0, MathF.Tau, 9, Colors.Red, 2));
        Check(vertices.SequenceEqual(once), "Arc sweep clamps at one turn.");
        Record(n => n.DrawEllipse(Vector2.Zero, 10, 4, Colors.Red));
        Check(vertices.Count == 192 && vertices.Max(v => v.Position.X) == 10 && vertices.Max(v => v.Position.Y) == 4, "Ellipse uses 64 fan segments and separate radii.");
        Record(n => n.DrawCircle(Vector2.Zero, 4, Colors.Red, false, 8));
        Check(vertices.Count == 192 && vertices.Max(v => v.Position.X) == 8, "Wide circle outline becomes expanded filled circle.");
        Record(n => { n.DrawPolyline([Vector2.Zero, Vector2.One], Colors.White, 0, true); n.DrawMultiline([Vector2.Zero, Vector2.One], Colors.White, 0, true); });
        Check(vertices.Count == 0, "Zero width emits no geometry.");
        node.Paint = n =>
        {
            Reject<ArgumentException>(() => n.DrawPolyline([], Colors.White));
            Reject<ArgumentException>(() => n.DrawMultiline([Vector2.Zero, Vector2.One, new(2, 2)], Colors.White));
            Reject<ArgumentException>(() => n.DrawMultilineColors([Vector2.Zero, Vector2.One], []));
            Reject<ArgumentException>(() => n.DrawPolyline([Vector2.Zero, new(float.NaN, 0)], Colors.White));
            Reject<ArgumentException>(() => n.DrawCircle(Vector2.Zero, float.NaN, Colors.White));
            Reject<ArgumentException>(() => n.DrawEllipseArc(Vector2.Zero, 2, 3, 0, float.PositiveInfinity, 3, Colors.White));
            Reject<ArgumentOutOfRangeException>(() => n.DrawArc(Vector2.Zero, 2, 0, 1, 1, Colors.White));
            Reject<ArgumentOutOfRangeException>(() => n.DrawDashedLine(Vector2.Zero, Vector2.One, Colors.White, dash: 0));
            Reject<ArgumentOutOfRangeException>(() => n.DrawDashedLine(Vector2.Zero, Vector2.One, Colors.White, dash: float.Epsilon));
            n.DrawCircle(Vector2.Zero, 3, Colors.Red);
            throw new ApplicationException();
        };
        node.InvalidateCanvas(); Reject<ApplicationException>(node.PrepareCanvas); vertices.Clear(); node.AppendCanvas(vertices, batches, Transform.Identity);
        Check(vertices.Count == 0, "Failure discards partial strokes.");
        node.Paint = n =>
        {
            n.DrawPolyline([new(0, 0), new(5, 0), new(5, 5)], Colors.Red, 3, true);
            n.DrawMultiline([new(0, 0), new(3, 1)], Colors.White, 1, true);
            n.DrawArc(Vector2.Zero, 3, 0, 2, 8, Colors.Green, 1, true);
            n.DrawEllipse(Vector2.Zero, 5, 2, Colors.Blue, antialiased: true);
            n.DrawDashedLine(Vector2.Zero, new(30, 10), Colors.White, 2, 3, true, true);
            n.DrawLine(Vector2.Zero, new(30, 10), Colors.White, 2, true);
        };
        void Replay() { node.InvalidateCanvas(); node.PrepareCanvas(); vertices.Clear(); batches.Clear(); node.AppendCanvas(vertices, batches, Transform.Identity); }
        for (var i = 0; i < 30; i++) Replay();
        var before = GC.GetAllocatedBytesForCurrentThread(); for (var i = 0; i < 1000; i++) Replay();
        Check(GC.GetAllocatedBytesForCurrentThread() == before, "Warmed curved/stroked redraw and replay are allocation-free.");
        using var tree = new SceneTree(node);
        Check(Task.Run(() => { try { node.DrawCircle(Vector2.Zero, 2, Colors.White); return false; } catch (InvalidOperationException) { return true; } }).Result, "Owner thread guard.");
        tree.Dispose(); Reject<ObjectDisposedException>(() => node.DrawPolyline([], Colors.White));
        Console.WriteLine("Canvas stroke geometry, joins, feathers, arcs, dashes, snapshots, guards and zero-allocation checks passed.");
    }
    private sealed class Painter : Entity { internal Action<Painter>? Paint; protected override void OnDraw() => Paint?.Invoke(this); }
    private static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    private static void Reject<T>(Action action) where T : Exception { try { action(); } catch (T) { return; } throw new InvalidOperationException($"Expected {typeof(T).Name}."); }
}
