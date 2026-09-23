using Electron2D;

internal static class CanvasPixelSnapTests
{
    internal static void Run()
    {
        Check(CanvasGeometry.Snap(new(-1.5f, 0.5f)) == new Vector2(-1, 1), "Half values round toward positive infinity.");
        using var image = Image.CreateFromData(3, 1, false, Image.Format.Rgba8, new byte[] { 255, 0, 0, 0, 255, 0, 0, 255, 255, 0, 0, 255 });
        using var texture = ImageTexture.CreateFromImage(image);
        var root = new TestViewport();
        Check(!root.SnapTransformsToPixel && !root.SnapVerticesToPixel, "Viewport snapping defaults off.");
        root.SnapTransformsToPixel = true;
        var sprite = new Sprite { Texture = texture };
        root.AddChild(sprite);
        Check(sprite.GetRect() == new Rect(-1.5f, -0.5f, 3, 1), "Detached sprite has no active viewport policy.");
        using (var tree = new SceneTree(root))
        {
            var draws = 0; var rectEvents = 0;
            sprite.Draw += _ => draws++; sprite.ItemRectChanged += _ => rectEvents++;
            var vertices = new List<CanvasVertex>(); var batches = new List<CanvasBatch>();
            Vector2 First()
            {
                sprite.PrepareCanvas(); vertices.Clear(); batches.Clear(); sprite.AppendCanvas(vertices, batches, sprite.GetGlobalTransform());
                return vertices[0].Position;
            }
            Check(sprite.GetRect() == new Rect(-1, 0, 3, 1) && !sprite.IsPixelOpaque(new(-0.4f, 0.2f)), "Attached snapping adjusts bounds and source alpha origin.");
            Check(First() == new Vector2(-1, 0), "Sprite records the snapped local offset.");
            root.SnapTransformsToPixel = false;
            Check(sprite.GetRect() == new Rect(-1.5f, -0.5f, 3, 1) && sprite.IsPixelOpaque(new(-0.4f, 0.2f)), "Live queries use the current policy.");
            Check(First() == new Vector2(-1, 0) && draws == 1 && rectEvents == 0, "Viewport policy does not silently invalidate retained commands or emit rectangle events.");
            sprite.QueueRedraw(); Check(First() == new Vector2(-1.5f, -0.5f) && draws == 2, "Explicit redraw refreshes the offset.");
            root.SnapVerticesToPixel = true;
            Check(First() == new Vector2(-1, 0) && draws == 2 && sprite.GetRect().Position == new Vector2(-1.5f, -0.5f), "Vertex snapping updates replay without changing local bounds.");
            Check(sprite.Position == Vector2.Zero && sprite.GetTransform() == Transform.Identity, "Rendering policies never mutate logical transforms.");
            Reject<InvalidOperationException>(() => Task.Run(() => root.SnapTransformsToPixel = true).GetAwaiter().GetResult());
            Reject<InvalidOperationException>(() => Task.Run(() => root.SnapVerticesToPixel = false).GetAwaiter().GetResult());
            root.RemoveChild(sprite); sprite.Dispose();
        }
        Reject<ObjectDisposedException>(() => root.SnapTransformsToPixel = false);
        Reject<ObjectDisposedException>(() => _ = root.SnapVerticesToPixel);
        using var window = new Window { SnapTransformsToPixel = true, SnapVerticesToPixel = true };
        using var packed = new PackedScene(); packed.Pack(window);
        using var copy = (Window)packed.Instantiate();
        Check(copy.SnapTransformsToPixel && copy.SnapVerticesToPixel, "PackedScene stores both viewport policies.");
        VerifyGeometry(); VerifySettings();
        Console.WriteLine("Canvas pixel snapping managed checks passed.");
    }

    private static void VerifyGeometry()
    {
        using var image = Image.CreateEmpty(2, 2, false, Image.Format.Rgba8);
        using var texture = ImageTexture.CreateFromImage(image);
        var vertices = new List<CanvasVertex>();
        var transform = new Transform(new(1.8f, 0.6f), new(-0.4f, 1.6f), new(0.2f, 0.2f));
        var command = new CanvasCommand(false, Vector2.Zero, new(2, 2), Colors.White, 0, false, Transform.Identity, texture, new(0, 0, 1, 1), ClipUV: true);
        CanvasGeometry.Append(vertices, command, transform, Colors.White, snapVertices: true);
        // Rounded primitive corners are (0,0), (4,1), (3,5), (-1,3), a non-parallelogram.
        Check(vertices.Any(v => v.Position.IsEqualApprox(new(0.75f, 1.25f))) &&
            vertices.Any(v => v.Position.IsEqualApprox(new(2.75f, 1.75f))), "UV clipping interpolates inside snapped triangles instead of snapping artificial cuts.");
        var area = 0f;
        for (var i = 0; i < vertices.Count; i += 3)
            area += System.MathF.Abs((vertices[i + 1].Position - vertices[i].Position).Cross(vertices[i + 2].Position - vertices[i].Position)) / 2;
        Check(System.MathF.Abs(area - 15.5f) < 0.0001f && vertices.All(v => v.UV.X is >= 0.25f and <= 0.75f && v.UV.Y is >= 0.25f and <= 0.75f), "Clipped triangles cover the snapped primitive exactly and preserve source borders.");
        var fill = new CanvasStroke(); fill.SetRect(new(0, 0, 2, 2), Colors.White, true, -1, true);
        var outline = new CanvasStroke(); outline.SetRect(new(0, 0, 2, 2), Colors.White, false, 1.2f, true);
        var shapes = new[]
        {
            command with { Texture = null, Stroke = fill },
            command with { Texture = null, Stroke = outline },
            command with { Texture = null, Line = true, Width = -1, Antialiased = true },
            command with { Transpose = true, B = new(-2, 2) },
        };
        foreach (var shape in shapes)
        {
            vertices.Clear(); CanvasGeometry.Append(vertices, shape, transform, Colors.White, snapVertices: true);
            Check(vertices.Count > 0 && vertices.All(v => v.Position.IsFinite() && v.UV.IsFinite()), "Lines, outlines, antialiasing, flipped and transposed textures execute.");
            if (shape.Texture is null) Check(vertices.All(v => v.Position == v.Position.Floor()), "Every final untextured vertex is snapped.");
        }
        vertices.Clear(); CanvasGeometry.Append(vertices, command with { B = new(0.1f, 0.1f) }, Transform.Identity, Colors.White, snapVertices: true);
        Check(vertices.Count == 0, "Collapsed snapped textures emit no geometry.");
        for (var i = 0; i < 1000; i++) { vertices.Clear(); CanvasGeometry.Append(vertices, command, transform, Colors.White, snapVertices: true); }
        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < 1000; i++) { vertices.Clear(); CanvasGeometry.Append(vertices, command, transform, Colors.White, snapVertices: true); }
        Check(GC.GetAllocatedBytesForCurrentThread() == before, "Warmed clipped snapping geometry allocates zero managed bytes.");
    }

    private static void VerifySettings()
    {
        var settings = ProjectSettings.Instance;
        using var original = new Window();
        settings.AddCustomFeature("pixel-snap-test");
        try
        {
            settings.SetFeatureOverride(ProjectSettings.SnapTransformsToPixel, "pixel-snap-test", true);
            settings.SetFeatureOverride(ProjectSettings.SnapVerticesToPixel, "pixel-snap-test", true);
            using var configured = new Window();
            Check(configured.SnapTransformsToPixel && configured.SnapVerticesToPixel && !original.SnapTransformsToPixel && !original.SnapVerticesToPixel,
                "Window construction samples active project overrides without mutating existing windows.");
        }
        finally
        {
            settings.ClearFeatureOverride(ProjectSettings.SnapTransformsToPixel, "pixel-snap-test");
            settings.ClearFeatureOverride(ProjectSettings.SnapVerticesToPixel, "pixel-snap-test");
            settings.RemoveCustomFeature("pixel-snap-test");
        }
    }

    private sealed class TestViewport : Viewport { public override Rect GetVisibleRect() => new(0, 0, 64, 64); }
    private static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    private static void Reject<T>(Action action) where T : Exception
    {
        try { action(); } catch (T) { return; }
        throw new InvalidOperationException("Expected " + typeof(T).Name);
    }
}
