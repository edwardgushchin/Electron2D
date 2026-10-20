using Electron2D;
using static TextureArrayTests;

internal static class PolygonAntialiasTests
{
    private static readonly Vector2[] Square = [new(0, 0), new(20, 0), new(20, 20), new(0, 20)];
    internal static void Managed()
    {
        PolygonTests.Run(); CanvasPolygonTests.Run();
        using var polygon = new Polygon { Vertices = Square };
        Check(!polygon.Antialiased, "Polygon edge smoothing defaults to disabled.");
        var property = polygon.GetPropertyList().Single(p => p.Name == nameof(Polygon.Antialiased));
        var vertices = new List<CanvasVertex>(256); var batches = new List<CanvasBatch>(8);
        void Replay(Transform transform) { polygon.PrepareCanvas(); vertices.Clear(); batches.Clear(); polygon.AppendCanvas(vertices, batches, transform); }
        Replay(Transform.Identity); Check(vertices.Count == 6, "Disabled smoothing preserves the original two triangles.");
        polygon.Antialiased = true; Replay(Transform.Identity);
        Check(property.CanRevert(polygon) && vertices.Count == 30 && vertices.Count(v => v.Color.A == 0) == 12, "Only the four boundary edges receive a transparent fringe.");
        foreach (var scale in new[] { new Vector2(2, 3), new Vector2(-2, 3), new Vector2(.25f, .5f) })
        {
            Replay(new Transform(0, scale, 0, Vector2.Zero));
            var left = Math.Min(0, 20 * scale.X); var right = Math.Max(0, 20 * scale.X);
            Check(vertices.Min(v => v.Position.X) == left - 1 && vertices.Max(v => v.Position.X) == right + 1, "The fringe remains one framebuffer pixel under scaling and reflection.");
        }
        Replay(new Transform(0, Vector2.Zero, 0, Vector2.Zero)); Check(vertices.Count == 6 && vertices.All(v => v.Position == Vector2.Zero), "Collapsed geometry emits no invalid fringe.");
        polygon.Polygons = [[0, 1, 2], [0, 2, 3]]; Replay(Transform.Identity);
        Check(vertices.Count == 30, "Adjacent explicit contours cancel their shared diagonal.");
        polygon.Vertices = [.. Square, new(float.MaxValue, 1)]; Replay(new Transform(0, new Vector2(2, 2), 0, Vector2.Zero)); Check(vertices.Count == 30, "Unused contour vertices do not affect bounds or overflow transformed smoothing.");
        polygon.Polygons = []; polygon.Vertices = Square.Reverse().ToArray(); Replay(Transform.Identity);
        Check(vertices.Count == 30 && vertices.Min(v => v.Position.X) == -1, "Reverse winding retains outward smoothing.");
        polygon.Vertices = [new(0, 0), new(30, 0), new(30, 10), new(10, 10), new(10, 30), new(0, 30)]; Replay(Transform.Identity);
        Check(vertices.Count == 48 && vertices.All(v => v.Position.IsFinite()), "Concave outlines smooth only their boundary.");
        polygon.Vertices = [.. Square, Square[0]]; Replay(Transform.Identity);
        Check(vertices.Where(v => v.Color.A == 0).All(v => v.Position.X <= 0 || v.Position.X >= 20 || v.Position.Y <= 0 || v.Position.Y >= 20), "An explicitly closed contour has no interior fringe seam.");
        foreach (var reverse in new[] { false, true })
        {
            polygon.Vertices = reverse ? Square.Reverse().ToArray() : Square; polygon.InvertEnabled = true; polygon.InvertBorder = 10; Replay(Transform.Identity);
            var fringe = vertices.Where(v => v.Color.A == 0).Select(v => v.Position).Distinct().ToArray();
            Check(fringe.Length == 8 && fringe.Contains(new(1, 1)) && fringe.Contains(new(-11, -11)), "Inverted smoothing follows its hole and outer rectangle without the triangulation slit.");
        }
        polygon.InvertEnabled = false; polygon.Vertices = Square; polygon.Polygons = [[0, 1]]; Replay(Transform.Identity); Check(vertices.Count == 0, "Only short explicit contours remain an empty drawing.");
        polygon.InvertEnabled = false; polygon.Vertices = Square; polygon.Polygons = [[0, 1, 20]];
        Reject<ArgumentOutOfRangeException>(() => Replay(Transform.Identity)); polygon.Polygons = []; Replay(Transform.Identity);
        Check(vertices.Count == 30, "A failed contour can retry through pooled storage.");
        using var packed = new PackedScene(); packed.Pack(polygon); using var copy = (Polygon)packed.Instantiate();
        var path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "e2d-polygon-aa-" + Guid.NewGuid().ToString("N") + ".e2dscene");
        try { ResourceSaver.Save(packed, path); using var loaded = ResourceLoader.Load<PackedScene>(path, ResourceLoader.CacheMode.Ignore); using var restored = (Polygon)loaded.Instantiate(); Check(restored.Antialiased, "Typed file restoration preserves the property."); } finally { File.Delete(path); }

        Check(copy.Antialiased, "Scene storage restores the authored smoothing property.");
        for (var i = 0; i < 32; i++) { polygon.QueueRedraw(); Replay(Transform.Identity); }
        var before = GC.GetAllocatedBytesForCurrentThread(); for (var i = 0; i < 64; i++) { polygon.QueueRedraw(); Replay(Transform.Identity); }
        Check(GC.GetAllocatedBytesForCurrentThread() == before, "Prepared triangulation, boundary rebuilding and replay allocate zero owner bytes.");
        polygon.Antialiased = false; Replay(Transform.Identity); Check(vertices.Count == 6, "Disabling smoothing clears pooled fringe state.");
        using (var host = new Node())
        {
            var rig = new Skeleton { Name = "Rig" }; var a = new Bone { Name = "A", Rest = Transform.Identity }; var b = new Bone { Name = "B", Rest = Transform.Identity }; a.SetAutocalculateLengthAndAngle(false); b.SetAutocalculateLengthAndAngle(false); rig.AddChild(a); rig.AddChild(b); host.AddChild(rig);
            var skin = new Polygon { Vertices = Square, Polygons = [[2, 3, 0], [0, 1, 2]], Skeleton = "../Rig", Antialiased = true }; skin.AddBone("A", [1, 0, 0, 1]); skin.AddBone("B", [0, 1, 1, 0]); host.AddChild(skin); using var skinTree = new SceneTree(host); b.Position = new(10, 0);
            skin.PrepareCanvas(); vertices.Clear(); batches.Clear(); skin.AppendCanvas(vertices, batches, Transform.Identity);
            Check(vertices.Min(v => v.Position.X) == -1 && vertices.Max(v => v.Position.X) == 31, "Compacted contour indices retain their original asymmetric skin weights.");
        }
        using var tree = new SceneTree(polygon); Task.Run(() => Reject<InvalidOperationException>(() => polygon.Antialiased = true)).GetAwaiter().GetResult();
        tree.Dispose(); Reject<ObjectDisposedException>(() => polygon.Antialiased = true);
        Console.WriteLine("Polygon smoothing managed topology, winding, transforms, inversion, persistence, recovery and zero prepared owner bytes passed.");
    }

    internal static void Native(string backend)
    {
        var method = ProjectSettings.Get(ProjectSettings.RenderingMethod); var fallback = ProjectSettings.Get(ProjectSettings.RenderingFallback); var fps = Engine.MaxFPS;
        try
        {
            ProjectSettings.Set(ProjectSettings.RenderingMethod, backend); ProjectSettings.Set(ProjectSettings.RenderingFallback, false); Engine.MaxFPS = 0;
            using var shader = Environment.GetEnvironmentVariable("ELECTRON2D_POLYGON_AA_SHADER") is { } shaderName ? LoadShader(shaderName) : null;
            using var material = shader is null ? null : new ShaderMaterial { Shader = shader };
            using var root = new Window { Size = new(256, 192), Borderless = true };
            var polygon = new Polygon { Vertices = Square, Position = new(20.25f, 20.25f), Color = new(1, 0, 0, .5f) }; root.AddChild(polygon);
            var inverted = new Polygon { Vertices = Square, Position = new(80.25f, 40.25f), Color = new(0, 1, 0, .5f), InvertEnabled = true, InvertBorder = 10, Antialiased = true }; root.AddChild(inverted);
            var rig = new Skeleton { Name = "Rig" }; var bone = new Bone { Name = "Bone", Rest = Transform.Identity }; bone.SetAutocalculateLengthAndAngle(false); rig.AddChild(bone); root.AddChild(rig);
            var skin = new Polygon { Vertices = Square, Position = new(120.25f, 20.25f), Skeleton = "../Rig", Color = Colors.Blue, Antialiased = true }; skin.AddBone("Bone", [1, 1, 1, 1]); root.AddChild(skin);
            using var image = Image.CreateEmpty(1, 1, false, Image.Format.Rgba8); image.Fill(new(0, 1, 0, .5f)); using var texture = ImageTexture.CreateFromImage(image);
            var textured = new Polygon { Vertices = Square, Texture = texture, Position = new(20.25f, 90.25f), Antialiased = true }; root.AddChild(textured);
            var diagonal = new Polygon { Vertices = [new(0, 0), new(24, 0), new(0, 24)], Position = new(160.25f, 90.25f), Color = new(1, 1, 1, .5f), Antialiased = true }; root.AddChild(diagonal);
            var repeated = new Polygon { Vertices = Square, Position = new(-20.25f, 140.25f), Color = Colors.Red, Antialiased = true }; root.AddChild(repeated);
            foreach (var item in new[] { polygon, inverted, skin, textured, diagonal, repeated }) item.Material = material;
            root.Ready += _ =>
            {
                RenderingServer.CanvasSetItemRepeat(repeated.GetCanvasItem(), new(1000, 0), 1);
                RenderingServer.RenderLoopEnabled = false; RenderingServer.SetDefaultClearColor(Colors.Black); RenderingServer.ForceDraw();
                using var sharp = RenderingServer.Service!.Readback(); Check(sharp.GetPixel(19, 30).R < .01f, "Disabled smoothing has no outside edge samples.");
                polygon.Antialiased = true; RenderingServer.ForceDraw();
                using var smooth = RenderingServer.Service.Readback();
                Near(smooth.GetPixel(0, 150).R, .25f, "Repeated offscreen fill preserves its visible framebuffer fringe.");
                Near(smooth.GetPixel(19, 30).R, .125f, "A fractional vertical edge has analytic interpolated fringe alpha.");
                Near(smooth.GetPixel(25, 30).R, .5f, "Translucent interior is unchanged by the fringe.");
                Near(smooth.GetPixel(19, 100).G, .125f, "Texture alpha multiplies the outside fringe.");
                Near(smooth.GetPixel(172, 102).R, .3232233f, "A diagonal edge retains framebuffer-normal fringe coverage.");

                Near(smooth.GetPixel(80, 50).G, .375f, "The inverted hole receives inward fringe coverage.");
                Near(smooth.GetPixel(100, 65).G, .5f, "The inverted triangulation slit has no antialias seam.");
                polygon.Polygons = [[0, 1, 2], [0, 2, 3]]; RenderingServer.ForceDraw(); using var adjacent = RenderingServer.Service.Readback();
                Check(adjacent.GetData().SequenceEqual(smooth.GetData()), "Subdividing the polygon into adjacent contours preserves every output pixel.");
                bone.Position = new(10, 0); RenderingServer.ForceDraw(); using var moved = RenderingServer.Service.Readback();
                Check(moved.GetPixel(119, 30).B < .01f, "The old deformed edge is retired."); Near(moved.GetPixel(129, 30).B, .25f, "The one-pixel fringe follows the deformed skeleton edge.");
                if (Environment.GetEnvironmentVariable("ELECTRON2D_POLYGON_AA_OUTPUT") is { } output) { Directory.CreateDirectory(output); smooth.SavePNG(System.IO.Path.Combine(output, backend + "-edges.png")); moved.SavePNG(System.IO.Path.Combine(output, backend + "-skin.png")); }
                using var premultiplied = new CanvasItemMaterial { BlendMode = BlendMode.PremultAlpha }; polygon.Material = premultiplied; polygon.Color = new(.5f, 0, 0, .5f); RenderingServer.ForceDraw(); using (var premul = RenderingServer.Service.Readback()) { Near(premul.GetPixel(19, 30).R, .125f, "Premultiplied fringe fades RGB together with alpha."); Near(premul.GetPixel(25, 30).R, .5f, "Premultiplied interior remains unchanged."); }
                foreach (var mode in new[] { BlendMode.Mul }) { using var incompatible = new CanvasItemMaterial { BlendMode = mode }; polygon.Material = incompatible; Reject<NotSupportedException>(() => RenderingServer.ForceDraw()); }
                polygon.Material = material; polygon.Color = new(1, 0, 0, .5f); RenderingServer.ForceDraw();
                root.SnapVerticesToPixel = true; RenderingServer.ForceDraw(); using (var snapped = RenderingServer.Service.Readback()) Near(snapped.GetPixel(19, 30).R, .25f, "Pixel snapping precedes framebuffer fringe expansion."); root.SnapVerticesToPixel = false;
                if (backend == "gpu")
                {
                    root.UseHDR2D = true; RenderingServer.ForceDraw(); using (var linear = root.GetTexture().GetImage()!) { Near(linear.GetPixel(19, 30).R, .125f, "HDR keeps fringe alpha in linear color."); Near(linear.GetPixel(25, 30).R, .5f, "HDR preserves translucent interior energy."); }
                    root.UseHDR2D = false; root.MSAA2D = Viewport.MSAA.Samples4X; RenderingServer.ForceDraw(); using (var multisample = RenderingServer.Service.Readback()) Check(multisample.GetPixel(19, 30).R is > .01f and < .4f, "MSAA composes the same fringe geometry."); root.MSAA2D = Viewport.MSAA.Disabled;
                }
                for (var i = 0; i < 32; i++) { bone.Position = new(10 + (i & 1), 0); polygon.Antialiased = (i & 1) == 0; RenderingServer.ForceDraw(); }
                var before = GC.GetAllocatedBytesForCurrentThread(); for (var i = 0; i < 64; i++) { bone.Position = new(10 + (i & 1), 0); polygon.Antialiased = (i & 1) == 0; RenderingServer.ForceDraw(); }
                Check(GC.GetAllocatedBytesForCurrentThread() == before, "Prepared active native redraw and deformation allocate zero owner bytes."); root.Tree!.Quit();
            };
            Check(Engine.Run(root) == 0 && root.IsDisposed, "Native smoothing session retires cleanly.");
        }
        finally { Engine.MaxFPS = fps; ProjectSettings.Set(ProjectSettings.RenderingMethod, method); ProjectSettings.Set(ProjectSettings.RenderingFallback, fallback); }
        Console.WriteLine(backend + ": native polygon edge/alpha/seam/skinning pixels and zero prepared owner bytes passed.");
    }

    private static Shader LoadShader(string name)
    {
        using var input = typeof(PolygonAntialiasTests).Assembly.GetManifestResourceStream("TestShaders." + name + ".spv")!;
        using var memory = new MemoryStream(); input.CopyTo(memory); return Shader.CreateFromSPIRV(memory.ToArray());
    }

    private static void Near(float actual, float expected, string message) => Check(Math.Abs(actual - expected) < .025f, message + $" Actual {actual}, expected {expected}.");
}
