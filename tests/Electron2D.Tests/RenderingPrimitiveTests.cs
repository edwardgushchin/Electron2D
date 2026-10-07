using Electron2D;

internal static class RenderingPrimitiveTests
{
    private static readonly Vector2[] Points = [new(0, 0), new(8, 0), new(8, 8), new(0, 8)];
    private static readonly int[] Indices = [0, 1, 2, 0, 2, 3];
    internal static void Run()
    {
        using var item = new RenderingServerCanvasItem(); var vertices = new List<CanvasVertex>(256); var batches = new List<CanvasBatch>(32);
        using (item.StartServerDrawing())
        {
            var points = Points.ToArray(); var indices = Indices.ToArray(); item.RecordServerTriangles(indices, points, [Colors.Red], default, default, default, null, 1); points[0] = new(100, 100); indices[0] = 3;
        }
        item.AppendCanvas(vertices, batches, Transform.Identity); Check(vertices.Count == 3 && vertices[0].Position == Vector2.Zero && vertices[0].Color == Colors.Red, "Copied indexed prefix.");
        var before = item.RenderBounds(); Reject<ArgumentOutOfRangeException>(() => { using var scope = item.StartServerDrawing(); item.RecordServerTriangles([9, 1, 2], Points, [], default, default, default, null, -1); }); Check(item.RenderBounds() == before, "Invalid indices preserve prior commands.");
        item.ClearServerCommands(); using (item.StartServerDrawing()) { item.DrawSetTransformMatrix(new(0, new(4, 5))); item.RecordServerClipIgnore(true); item.DrawRect(new(0, 0, 8, 8), Colors.Green); item.RecordServerClipIgnore(false); item.DrawRect(new(20, 0, 8, 8), Colors.Blue); }
        vertices.Clear(); batches.Clear(); item.AppendCanvas(vertices, batches, Transform.Identity, clip: new(0, 0, 0, 0)); Check(vertices.Count != 0 && vertices.Min(v => v.Position.X) == 4 && batches.All(b => b.Clip is null), "Ignore clipping resurrects commands and retains earlier drawing transform.");
        item.ClearServerCommands(); using (item.StartServerDrawing()) item.RecordServerStroke([new(0, 0), new(8, 0)], [Colors.Red, Colors.Blue], 4, false, false); vertices.Clear(); batches.Clear(); item.AppendCanvas(vertices, batches, Transform.Identity); Check(vertices.Any(v => v.Color.R > .9 && v.Color.B < .1) && vertices.Any(v => v.Color.B > .9 && v.Color.R < .1), "Wide multiline endpoints retain separate colors.");
        void Replay() { item.ClearServerCommands(); using (item.StartServerDrawing()) { item.RecordServerTriangles(Indices, Points, [Colors.White], default, default, default, null, -1); item.RecordServerStroke([new(0, 0), new(8, 0)], [Colors.Red, Colors.Blue], 4, true, false); } vertices.Clear(); batches.Clear(); item.AppendCanvas(vertices, batches, Transform.Identity); }
        for (var i = 0; i < 64; i++) Replay(); var allocated = GC.GetAllocatedBytesForCurrentThread(); for (var i = 0; i < 128; i++) Replay(); Check(allocated == GC.GetAllocatedBytesForCurrentThread(), "128 warmed indexed/gradient clear/record/replay intervals.");
        SkinChannels();
        Console.WriteLine("Copied triangle prefix, guards, clip state, gradient endpoints and 128 warmed intervals passed with 0 managed bytes.");
    }
    private static void SkinChannels()
    {
        using var root = new Entity(); var rig = new Skeleton { Name = "Rig" }; var bone = new Bone { Name = "Bone", Rest = Transform.Identity }; bone.SetAutocalculateLengthAndAngle(false); bone.SetLength(8); rig.AddChild(bone); var item = new Entity { Name = "Consumer" }; root.AddChild(rig); root.AddChild(item); using var tree = new SceneTree(root); item.AttachSkeleton(rig.GetSkeleton()); bone.Position = new(12, 0);
        var weights = new float[16]; for (var i = 0; i < 4; i++) weights[i * 4] = 1; var bones = new int[16]; var vertices = new List<CanvasVertex>(); var batches = new List<CanvasBatch>();
        using (item.StartServerDrawing()) item.RecordServerTriangles(Indices, Points, [], default, default, weights, null, -1); Array.Clear(weights); item.AppendCanvas(vertices, batches, Transform.Identity); Check(vertices[0].Position.IsEqualApprox(new(12, 0)), "Independent weights default to bone zero and copy input.");
        item.ClearServerCommands(); using (item.StartServerDrawing()) item.RecordServerTriangles(Indices, Points, [], default, bones, default, null, -1); vertices.Clear(); batches.Clear(); item.AppendCanvas(vertices, batches, Transform.Identity); Check(vertices.All(v => v.Position == Vector2.Zero), "Independent bones use zero default weights.");
        item.ClearServerCommands(); for (var i = 0; i < 4; i++) weights[i * 4] = 1f / 65535; using (item.StartServerDrawing()) item.RecordServerTriangles(Indices, Points, [], default, bones, weights, null, -1); vertices.Clear(); batches.Clear(); item.AppendCanvas(vertices, batches, Transform.Identity); Check(Math.Abs(vertices[0].Position.X - 12f / 65535) < .000001, "First UNORM16 quantum remains unnormalized.");
        bone.Scale = Vector2.Zero; rig.Dispose(); vertices.Clear(); batches.Clear(); item.AppendCanvas(vertices, batches, Transform.Identity); Check(vertices[1].Position == Points[1], "Released scene palette restores unskinned raw triangles.");
    }
    private sealed class Paint : Entity { internal Color Fill = Colors.Green; protected override void OnDraw() => DrawRect(new(0, 0, 8, 8), Fill); }
    internal static void RunHost()
    {
        Run(); var backend = Environment.GetEnvironmentVariable("ELECTRON2D_RENDER_BACKEND") ?? "gpu"; ProjectSettings.Set(ProjectSettings.RenderingMethod, backend); ProjectSettings.Set(ProjectSettings.RenderingFallback, false); Engine.MaxFPS = 60;
        var window = new Window { Size = new(160, 128) }; var source = new Paint { Name = "Source", Position = new(112, 8) }; window.AddChild(source); var otherSource = new Paint { Name = "OtherSource", Position = new(112, 8), Fill = Colors.Blue }; window.AddChild(otherSource); RID canvas = default, root = default, child = default, sibling = default, palette = default, materialItem = default; var phase = 0;
        var group = new CanvasGroup { Name = "Group", Position = new(112, 96) }; group.AddChild(new Paint { Name = "Grouped", Fill = Colors.Red }); window.AddChild(group);
        using var inheritedMaterial = new CanvasItemMaterial { BlendMode = BlendMode.Add }; var materialParent = new Paint { Name = "MaterialParent", Position = new(128, 88), Material = inheritedMaterial }; window.AddChild(materialParent);
        using var pixels = Image.CreateEmpty(2, 2, false, Image.Format.Rgba8); pixels.Fill(Colors.White); using var texture = ImageTexture.CreateFromImage(pixels);
        window.Ready += _ =>
        {
            Check(RenderingServer.GetCurrentRenderingMethod() == backend, "Actual requested backend."); RenderingServer.SetDefaultClearColor(Colors.Black); canvas = RenderingServer.CanvasCreate(); RenderingServer.ViewportAttachCanvas(window.GetViewportRID(), canvas); root = RenderingServer.CanvasItemCreate(); child = RenderingServer.CanvasItemCreate(); RenderingServer.CanvasItemSetParent(root, canvas); RenderingServer.CanvasItemSetParent(child, root);
            RenderingServer.CanvasItemAddRect(root, new(8, 8, 8, 8), Colors.Red); RenderingServer.CanvasItemAddLine(root, new(24, 12), new(40, 12), Colors.Lime, 4); RenderingServer.CanvasItemAddPolyline(root, [new(48, 12), new(56, 12), new(56, 20)], [Colors.Blue], 4); RenderingServer.CanvasItemAddMultiline(root, [new(64, 12), new(80, 12)], [Colors.Red, Colors.Blue], 4);
            RenderingServer.CanvasItemAddCircle(root, new(12, 36), 5, Colors.Cyan); RenderingServer.CanvasItemAddEllipse(root, new(32, 36), 8, 4, Colors.Yellow); RenderingServer.CanvasItemAddPolygon(root, [new(48, 32), new(56, 32), new(56, 40), new(48, 40)], [Colors.Magenta]); RenderingServer.CanvasItemAddPrimitive(root, [new(64, 32), new(72, 32), new(72, 40), new(64, 40)], [Colors.Lime], [], default);
            RenderingServer.CanvasItemAddTextureRect(root, new(8, 56, 8, 8), texture.GetRID(), modulate: Colors.Red); RenderingServer.CanvasItemAddTextureRectRegion(root, new(24, 56, 8, 8), texture.GetRID(), new(0, 0, 2, 2), Colors.Blue);
            RenderingServer.CanvasItemAddTriangleArray(root, Indices, [new(48, 56), new(56, 56), new(56, 64), new(48, 64)], [Colors.Yellow]);
            RenderingServer.CanvasItemAddSetTransform(root, new(0, new(72, 56))); RenderingServer.CanvasItemAddRect(root, new(0, 0, 8, 8), Colors.White); RenderingServer.CanvasItemAddSetTransform(root, Transform.Identity);
            RenderingServer.CanvasItemAddAnimationSlice(root, 1, 0, 0); RenderingServer.CanvasItemAddRect(root, new(96, 56, 8, 8), Colors.Red); RenderingServer.CanvasItemAddAnimationSlice(root, 1, 0, 2); RenderingServer.CanvasItemAddRect(root, new(112, 56, 8, 8), Colors.Lime);
            RenderingServer.CanvasItemAddRect(child, new(8, 8, 8, 8), Colors.Blue); sibling = RenderingServer.CanvasItemCreate(); RenderingServer.CanvasItemSetParent(sibling, root); RenderingServer.CanvasItemSetDrawIndex(sibling, -1); RenderingServer.CanvasItemAddRect(sibling, new(8, 8, 8, 8), Colors.Red);
            materialItem = RenderingServer.CanvasItemCreate(); RenderingServer.CanvasItemSetParent(materialItem, materialParent.GetCanvasItem()); RenderingServer.CanvasItemSetUseParentMaterial(materialItem, true); RenderingServer.CanvasItemAddRect(materialItem, new(0, 0, 8, 8), Colors.Blue);
            RenderingServer.CanvasItemSetCustomRect(group.GetCanvasItem(), true, new(-1000, -1000, 1, 1));
            palette = RenderingServer.SkeletonCreate(); RenderingServer.SkeletonAllocateData(palette, 1); RenderingServer.SkeletonBoneSetTransform2D(palette, 0, new(0, new(12, 0))); RenderingServer.CanvasItemAttachSkeleton(root, palette);
            var bones = new int[16]; var weights = new float[16]; for (var i = 0; i < 4; i++) weights[i * 4] = 1;
            RenderingServer.CanvasItemAddTriangleArray(root, Indices, [new(8, 80), new(16, 80), new(16, 88), new(8, 88)], [Colors.Cyan], bones: bones, weights: weights);
            Reject<ArgumentException>(() => RenderingServer.CanvasItemAddPolygon(root, [new(float.NaN, 0), Vector2.One, Vector2.Zero], [])); Reject<ArgumentOutOfRangeException>(() => RenderingServer.CanvasItemAddTriangleArray(root, Indices, Points, [], count: 3)); Reject<InvalidOperationException>(() => Task.Run(() => RenderingServer.CanvasItemAddRect(root, default, Colors.Red)).GetAwaiter().GetResult());
            RenderingServer.FramePostDraw += () =>
            {
                using var image = window.GetTexture().GetImage()!;
                try
                {
                    if (phase == 0)
                    {
                        Pixel(image, 113, 97, Colors.Red); Pixel(image, 129, 89, Colors.Cyan); Pixel(image, 9, 9, Colors.Blue); Pixel(image, 28, 12, Colors.Lime); Pixel(image, 52, 12, Colors.Blue); Check(image.GetPixel(65, 12).R > .7f && image.GetPixel(79, 12).B > .7f, "Gradient native endpoints."); Pixel(image, 12, 36, Colors.Cyan); Pixel(image, 32, 36, Colors.Yellow); Pixel(image, 49, 33, Colors.Magenta); Pixel(image, 65, 33, Colors.Lime); Pixel(image, 9, 57, Colors.Red); Pixel(image, 25, 57, Colors.Blue); Pixel(image, 49, 57, Colors.Yellow); Pixel(image, 73, 57, Colors.White); Pixel(image, 97, 57, Colors.Black); Pixel(image, 113, 57, Colors.Lime); Pixel(image, 21, 81, Colors.Cyan); Pixel(image, 9, 81, Colors.Black);
                        image.SavePNG("/tmp/e2d-primitives-" + backend + ".png"); RenderingServer.CanvasItemSetDrawIndex(child, -2);
                    }
                    else if (phase == 1) { Pixel(image, 9, 9, Colors.Red); RenderingServer.CanvasItemSetVisibilityLayer(root, 2); window.CanvasCullMask = 1; }
                    else if (phase == 2) { Pixel(image, 9, 9, Colors.Black); RenderingServer.CanvasItemSetVisibilityLayer(root, 1); RenderingServer.CanvasItemSetCustomRect(root, true, new(0, 0, 4, 4)); RenderingServer.CanvasItemSetClip(root, true); RenderingServer.CanvasItemClear(child); RenderingServer.CanvasItemAddClipIgnore(child, true); RenderingServer.CanvasItemAddRect(child, new(8, 8, 8, 8), Colors.Blue); }
                    else if (phase == 3) { Pixel(image, 9, 9, Colors.Blue); RenderingServer.CanvasItemAddClipIgnore(child, false); RenderingServer.CanvasItemAddRect(child, new(24, 8, 8, 8), Colors.Red); RenderingServer.CanvasItemSetDrawIndex(source.GetCanvasItem(), int.MaxValue); Check(source.GetIndex() == 0, "Native ordering preserves authored Node index."); }
                    else if (phase == 4) { Pixel(image, 113, 9, Colors.Green); Pixel(image, 25, 9, Colors.Black); source.VisibilityLayer = 1; RenderingServer.CanvasItemSetVisibilityLayer(source.GetCanvasItem(), 2); Check(source.VisibilityLayer == 1, "Native mask preserves authored getter."); }
                    else if (phase == 5) { Pixel(image, 113, 9, Colors.Blue); source.VisibilityLayer = 1; source.MoveToFront(); window.MoveChild(source, 0); Check(source.ServerState?.DrawIndex is null, "Scene structural order republishes native index."); RenderingServer.CanvasItemSetDrawIndex(child, 0); RenderingServer.CanvasItemSetClip(root, false); RenderingServer.CanvasItemSetCustomRect(root, false); }
                    else { Pixel(image, 113, 9, Colors.Blue); Check(source.ServerState?.VisibilityLayer is null, "Source mask setter republishes native field."); RenderingServer.FreeRID(canvas); RenderingServer.FreeRID(root); RenderingServer.FreeRID(child); RenderingServer.FreeRID(sibling); RenderingServer.FreeRID(palette); RenderingServer.FreeRID(materialItem); window.Tree!.Quit(); }
                }
                catch (Exception error) { throw new InvalidOperationException("Primitive phase " + phase + " / " + backend, error); }
                phase++;
            };
        };
        Check(Engine.Run(window) == 0 && phase == 7, "Primitive host lifecycle."); Console.WriteLine("Seventeen canvas primitive/order/culling/material operations passed native pixels: " + backend);
    }
    private static void Pixel(Image image, int x, int y, Color expected) { var actual = image.GetPixel(x, y); Check(Math.Abs(actual.R - expected.R) < .04 && Math.Abs(actual.G - expected.G) < .04 && Math.Abs(actual.B - expected.B) < .04, "Pixel " + x + "," + y + ": " + actual + " expected " + expected); }
    private static void Check(bool value, string text) { if (!value) throw new InvalidOperationException(text); }
    private static void Reject<T>(Action action) where T : Exception { try { action(); } catch (T) { return; } throw new InvalidOperationException("Expected " + typeof(T).Name); }
}
