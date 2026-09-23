using Electron2D;

internal static class CanvasPolygonTests
{
    private static readonly Vector2[] Contour = [new(0, 0), new(12, 0), new(12, 4), new(4, 4), new(4, 12), new(0, 12)];

    internal static void Run()
    {
        using var node = new Painter();
        Reject<InvalidOperationException>(() => node.DrawPolygon(Contour, []));
        var vertices = new List<CanvasVertex>(); var batches = new List<CanvasBatch>();
        foreach (var reverse in new[] { false, true })
        {
            var points = reverse ? Contour.Reverse().ToArray() : (Vector2[])Contour.Clone();
            var colors = points.Select(p => new Color(p.X / 12, p.Y / 12, 0)).ToArray();
            var uvs = points.Select(p => p / 12).ToArray();
            node.Paint = n => n.DrawPolygon(points, colors, uvs);
            node.InvalidateCanvas(); node.PrepareCanvas();
            Array.Fill(points, new Vector2(100, 100)); Array.Fill(colors, Colors.White); Array.Fill(uvs, Vector2.One);
            vertices.Clear(); batches.Clear(); node.AppendCanvas(vertices, batches, Transform.Identity);
            Check(vertices.Count == 12 && batches.Count == 1, "A six-vertex concave polygon produces four retained triangles.");
            double area = 0;
            for (var i = 0; i < vertices.Count; i += 3)
            {
                var a = vertices[i].Position; var b = vertices[i + 1].Position; var c = vertices[i + 2].Position;
                area += Math.Abs((b - a).Cross(c - a)) / 2;
            }
            Check(area == 80, "Either winding covers exactly the concave contour area.");
            foreach (var v in vertices)
                Check(v.UV == v.Position / 12 && v.Color.R == v.UV.X && v.Color.G == v.UV.Y, "Positions, colors and UVs are copied together.");
        }
        node.Paint = n =>
        {
            Reject<ArgumentException>(() => n.DrawPolygon([], []));
            Reject<ArgumentException>(() => n.DrawPolygon(Contour, [Colors.Red, Colors.Blue]));
            Reject<ArgumentException>(() => n.DrawPolygon(Contour, [], [Vector2.One]));
            Reject<ArgumentException>(() => n.DrawPolygon([new(float.NaN, 0), Vector2.One, Vector2.Zero], []));
            Reject<ArgumentException>(() => n.DrawColoredPolygon(Contour, new Color(float.NaN, 0, 0)));
            Reject<ArgumentException>(() => n.DrawPrimitive([], [], []));
            Reject<ArgumentException>(() => n.DrawPrimitive(Contour, [], []));
            Reject<ArgumentException>(() => n.DrawPolygon([new(0, 0), new(8, 8), new(0, 8), new(8, 0)], []));
            n.DrawColoredPolygon([new(0, 0), new(4, 0), new(8, 0), new(8, 8), new(0, 8), new(0, 0)], Colors.White);
            n.DrawPrimitive([new(20, 20), new(24, 20), new(20, 24)], [Colors.Red, Colors.Blue], [new(0.1f, 0.2f)]);
        };
        node.InvalidateCanvas(); node.PrepareCanvas(); vertices.Clear(); batches.Clear(); node.AppendCanvas(vertices, batches, Transform.Identity);
        Check(vertices[^3].Color == Colors.Red && vertices[^2].Color == Colors.Blue && vertices[^1].Color == Colors.Red && vertices[^1].UV == Vector2.Zero, "Primitive partial attributes use first color and zero UV.");
        Atlas(node, vertices, batches);
        using var callbackTexture = new SizeCallbackTexture();
        using var callbackAtlas = new AtlasTexture { Atlas = callbackTexture, Region = new(0, 0, 1, 1) };
        callbackTexture.Query = () => node.DrawColoredPolygon(Contour, Colors.Red);
        node.Paint = n => n.DrawColoredPolygon(Contour, Colors.Blue, Contour, callbackAtlas);
        node.InvalidateCanvas(); node.PrepareCanvas(); vertices.Clear(); batches.Clear(); node.AppendCanvas(vertices, batches, Transform.Identity);
        Check(vertices.Count == 24 && vertices[0].Color == Colors.Red && vertices[12].Color == Colors.Blue, "Custom texture callbacks cannot overwrite an uncommitted pooled polygon.");
        using (var disposable = new Painter())
        {
            callbackTexture.Query = disposable.Dispose;
            disposable.Paint = n => n.DrawPolygon(Contour, [], Contour, callbackAtlas);
            Reject<ObjectDisposedException>(disposable.PrepareCanvas);
        }
        node.Paint = n => n.DrawColoredPolygon(Contour, Colors.White);
        node.InvalidateCanvas(); node.PrepareCanvas(); vertices.Clear(); batches.Clear();
        var placement = new Transform(0, new Vector2(-2, 3), 0, new Vector2(0.25f, 0.75f));
        node.AppendCanvas(vertices, batches, placement);
        Check(vertices.Any(v => v.Position == placement * Contour[2]), "Nonuniform and reflected item transforms affect retained polygon vertices.");
        Reject<InvalidOperationException>(() => node.AppendCanvas(vertices, batches, new Transform(0, new Vector2(float.MaxValue, 1), 0, Vector2.Zero)));
        node.Paint = n => { n.DrawPolygon(Contour, []); n.DrawColoredPolygon(Contour, Colors.Blue); n.DrawPrimitive([new(2, 2)], [], []); };
        for (var i = 0; i < 30; i++) { node.InvalidateCanvas(); node.PrepareCanvas(); vertices.Clear(); batches.Clear(); node.AppendCanvas(vertices, batches, Transform.Identity); }
        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < 1000; i++) { node.InvalidateCanvas(); node.PrepareCanvas(); vertices.Clear(); batches.Clear(); node.AppendCanvas(vertices, batches, Transform.Identity); }
        Check(GC.GetAllocatedBytesForCurrentThread() == before, "Warmed polygon redraw, triangulation and replay allocate no managed bytes.");
        node.Paint = n => { n.DrawColoredPolygon(Contour, Colors.Red); throw new ApplicationException(); };
        node.InvalidateCanvas(); Reject<ApplicationException>(node.PrepareCanvas); vertices.Clear(); batches.Clear(); node.AppendCanvas(vertices, batches, Transform.Identity);
        Check(vertices.Count == 0, "Recording failure clears partial polygons.");
        node.Paint = n => n.DrawColoredPolygon(Contour, Colors.Blue); node.PrepareCanvas(); vertices.Clear(); node.AppendCanvas(vertices, batches, Transform.Identity);
        Check(vertices.Count == 12 && vertices.All(v => v.Color == Colors.Blue), "Recording retries with clean reused polygon storage.");
        using var tree = new SceneTree(node);
        var wrongThread = Task.Run(() => { try { node.PrepareCanvas(); return false; } catch (InvalidOperationException) { return true; } }).Result;
        Check(wrongThread, "Attached recording rejects foreign threads.");
        tree.Dispose(); Reject<ObjectDisposedException>(() => node.DrawPrimitive([Vector2.Zero], [], []));
        Console.WriteLine("Canvas polygon triangulation, attributes, snapshots, recording guards and zero-allocation redraw passed.");
    }

    private static void Atlas(Painter node, List<CanvasVertex> vertices, List<CanvasBatch> batches)
    {
        using var image = Image.CreateEmpty(4, 4, false, Image.Format.Rgba8); image.Fill(Colors.Red);
        using var source = ImageTexture.CreateFromImage(image);
        using var atlas = new AtlasTexture { Atlas = source, Region = new(1, 2, 2, 1), Margin = new(5, 5, 5, 5), FilterClip = true };
        node.Paint = n => { n.DrawPolygon(Contour, [], Contour.Select(p => p / 12).ToArray(), atlas); n.DrawPrimitive([Vector2.Zero, Vector2.One, new(2, 0)], [], [Vector2.One], atlas); };
        node.InvalidateCanvas(); node.PrepareCanvas(); atlas.Atlas = null;
        vertices.Clear(); batches.Clear(); node.AppendCanvas(vertices, batches, Transform.Identity);
        Check(batches.Count == 1 && batches[0].Texture == source, "Atlas target identity is captured at recording for both commands.");
        for (var i = 0; i < 12; i++) Check(vertices[i].UV == new Vector2(0.25f, 0.5f) + vertices[i].Position / 12 * new Vector2(0.5f, 0.25f), "Polygon atlas region remaps supplied UVs and ignores margins.");
        Check(vertices[^3].UV == Vector2.One, "Primitive atlas UVs sample the full source.");
        source.Dispose();
        node.Paint = n => n.DrawPolygon(Contour, [], texture: source); node.InvalidateCanvas(); Reject<ObjectDisposedException>(node.PrepareCanvas);
    }

    private sealed class SizeCallbackTexture : Texture
    {
        internal Action? Query;
        public override Vector2 GetSize() { Query?.Invoke(); return Vector2.One; }
        public override int GetWidth() => 1;
        public override int GetHeight() => 1;
    }

    private sealed class Painter : Entity
    {
        internal Action<Painter>? Paint;
        protected override void OnDraw() => Paint?.Invoke(this);
    }
    private static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    private static void Reject<T>(Action action) where T : Exception { try { action(); } catch (T) { return; } throw new InvalidOperationException($"Expected {typeof(T).Name}."); }
}
