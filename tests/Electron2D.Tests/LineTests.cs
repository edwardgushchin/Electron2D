using Electron2D;

internal static class LineTests
{
    internal static void Run()
    {
        using var line = new Line();
        Check(line is Entity && line.Points.Length == 0 && line.Width == 10 && line.DefaultColor == Colors.White &&
            !line.Closed && line.RoundPrecision == 8 && line.SharpLimit == 2 && !line.Antialiased &&
            line.JointMode == Line.LineJointMode.Sharp && line.TextureMode == Line.LineTextureMode.None,
            "Line defaults and hierarchy.");
        line.AddPoint(new(0, 0)); line.AddPoint(new(20, 0)); line.AddPoint(new(10, 10), 1);
        Check(line.GetPointCount() == 3 && line.GetPointPosition(1) == new Vector2(10, 10), "Indexed insertion.");
        line.SetPointPosition(1, new(10, 0)); line.RemovePoint(1);
        Check(line.Points.SequenceEqual([new(0, 0), new(20, 0)]), "Point edit and removal.");
        var copy = line.Points; copy[0] = new(100, 100);
        Check(line.GetPointPosition(0) == Vector2.Zero, "Points are value snapshots.");
        Reject<ArgumentException>(() => line.Points = [new(float.NaN, 0)]);
        Reject<ArgumentOutOfRangeException>(() => line.RemovePoint(3));
        line.Width = -1; line.SharpLimit = -1; line.RoundPrecision = 0;
        Check(line.Width == 0 && line.SharpLimit == 0 && line.RoundPrecision == 1, "Numeric clamping.");
        line.Width = 10; line.RoundPrecision = 8; line.DefaultColor = Colors.Red;
        var vertices = new List<CanvasVertex>(); var batches = new List<CanvasBatch>();
        line.PrepareCanvas(); line.AppendCanvas(vertices, batches, Transform.Identity);
        Check(vertices.Count == 6 && batches.Count == 1 && vertices.All(v => v.Color == Colors.Red), "Segment becomes one colored quad.");
        line.BeginCapMode = Line.LineCapMode.Round; line.EndCapMode = Line.LineCapMode.Round;
        line.PrepareCanvas(); vertices.Clear(); batches.Clear(); line.AppendCanvas(vertices, batches, Transform.Identity);
        Check(vertices.Count > 6 && vertices.Any(v => v.Position.X < 0) && vertices.Any(v => v.Position.X > 20), "Rounded endpoint geometry.");
        line.Closed = true; line.Points = [new(0, 0), new(20, 0), new(20, 20), new(0, 20)];
        line.JointMode = Line.LineJointMode.Bevel; line.PrepareCanvas(); vertices.Clear(); batches.Clear(); line.AppendCanvas(vertices, batches, Transform.Identity);
        Check(vertices.Count >= 24 && batches.Count == 1, "Closed line draws every edge and joints.");
        line.Antialiased = true; line.PrepareCanvas(); vertices.Clear(); batches.Clear(); line.AppendCanvas(vertices, batches, Transform.Identity);
        Check(vertices.Count > 48 && batches.Count == 1 && vertices.Any(v => v.Color.A == 0), "Antialias fringe is retained with transparent outer vertices.");
        line.Closed = false; line.Antialiased = false; line.JointMode = Line.LineJointMode.Sharp; line.SharpLimit = 2;
        line.Points = [new(0, 0), new(20, 0), new(20, -20)];
        line.PrepareCanvas(); vertices.Clear(); batches.Clear(); line.AppendCanvas(vertices, batches, Transform.Identity);
        Check(vertices[2].Position == new Vector2(15, -5) && vertices[5].Position == new Vector2(25, 5),
            "A right-turn sharp joint preserves the two sides of the strip.");
        line.Closed = false; line.Antialiased = false; line.Points = [new(0, 0), new(20, 0), new(0, 1)];
        line.PrepareCanvas(); vertices.Clear(); batches.Clear(); line.AppendCanvas(vertices, batches, Transform.Identity);
        Check(vertices.Count > 0 && vertices.All(v => v.Position.IsFinite() && v.Position.X is > -20 and < 40 && v.Position.Y is > -20 and < 20),
            "A hairpin bend must not create an unbounded interior miter.");
        line.Closed = true; line.Points = [new(0, 0), new(20, 0), new(20, 20), new(0, 20)];
        using var scene = new PackedScene(); scene.Pack(line);
        var state = scene.GetState();
        var pointsIndex = Enumerable.Range(0, state.GetNodePropertyCount(0)).Single(i => state.GetNodePropertyName(0, i) == nameof(Line.Points));
        var storedPoints = state.GetNodePropertyValue<Vector2[]>(0, pointsIndex);
        storedPoints[0] = new(500, 500);
        Check(state.GetNodePropertyValue<Vector2[]>(0, pointsIndex)[0] == Vector2.Zero, "SceneState returns a copied point array.");
        using var instance = (Line)scene.Instantiate();
        Check(instance.GetType() == typeof(Line) && instance.Points.SequenceEqual(line.Points) && instance.Closed &&
            instance.JointMode == line.JointMode && instance.BeginCapMode == line.BeginCapMode, "Packed line state.");
        using var gradient = new Gradient { Colors = [Colors.Red, Colors.Blue] };
        using var colored = new Line { Points = [Vector2.Zero, new(20, 0)], Gradient = gradient };
        colored.PrepareCanvas(); vertices.Clear(); batches.Clear(); colored.AppendCanvas(vertices, batches, Transform.Identity);
        Check(vertices[0].Color == Colors.Red && vertices[2].Color == Colors.Blue, "Gradient follows normalized line length.");
        colored.Points = [new(0, 0), new(20, 0), new(40, 0)]; colored.BeginCapMode = Line.LineCapMode.Box;
        colored.PrepareCanvas(); vertices.Clear(); batches.Clear(); colored.AppendCanvas(vertices, batches, Transform.Identity);
        Check(vertices.Any(v => v.Position.X == 20 && v.Color == gradient.Sample(25f / 45f)),
            "A beginning cap contributes to normalized gradient distance at an interior point.");
        Task.Run(() => gradient.SetColor(0, Colors.Green)).GetAwaiter().GetResult();
        colored.PrepareCanvas(); vertices.Clear(); batches.Clear(); colored.AppendCanvas(vertices, batches, Transform.Identity);
        Check(vertices[0].Color == Colors.Green, "Worker resource edit invalidates retained line geometry.");
        using var image = Image.CreateFromData(2, 1, false, Image.Format.Rgba8, [255, 0, 0, 255, 0, 0, 255, 255]);
        using var source = ImageTexture.CreateFromImage(image);
        using var atlas = new AtlasTexture { Atlas = source, Region = new(1, 0, 1, 1) };
        using var textured = new Line { Points = [Vector2.Zero, new(20, 0)], Texture = atlas, TextureMode = Line.LineTextureMode.Stretch };
        textured.PrepareCanvas(); vertices.Clear(); batches.Clear(); textured.AppendCanvas(vertices, batches, Transform.Identity);
        Check(batches.Count == 1 && batches[0].Texture == source && vertices.All(v => v.UV.X is >= 0.5f and <= 1),
            "Atlas line samples the resolved source region.");
        using var tiled = new Line
        {
            Points = [Vector2.Zero, new(20, 0)],
            Width = 8,
            Texture = source,
            TextureMode = Line.LineTextureMode.Tile,
            BeginCapMode = Line.LineCapMode.Round,
            EndCapMode = Line.LineCapMode.Round,
        };
        tiled.PrepareCanvas(); vertices.Clear(); batches.Clear(); tiled.AppendCanvas(vertices, batches, Transform.Identity);
        Check(vertices.Any(v => v.Position.X < 0 && v.UV.X < vertices[0].UV.X) &&
            vertices.Any(v => v.Position.X > 20 && v.UV.X > vertices[2].UV.X),
            "Round caps map texture columns beyond both body endpoints.");
        using var zeroWidthCurve = new Curve();
        tiled.WidthCurve = zeroWidthCurve;
        tiled.PrepareCanvas(); vertices.Clear(); batches.Clear(); tiled.AppendCanvas(vertices, batches, Transform.Identity);
        Check(vertices.All(v => v.Position.IsFinite() && v.UV.IsFinite()), "Zero-radius round caps do not record invalid geometry.");
        line.ClearPoints(); Check(line.Points.Length == 0, "ClearPoints.");
        line.Dispose(); Reject<ObjectDisposedException>(() => line.AddPoint(Vector2.Zero));
        Console.WriteLine("Line managed geometry, state and packing passed.");
    }

    private static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    private static void Reject<T>(Action action) where T : Exception { try { action(); } catch (T) { return; } throw new InvalidOperationException($"Expected {typeof(T).Name}."); }
}
