using Electron2D;

internal static class RenderingCanvasTests
{
    private sealed class Paint : Entity { protected override void OnDraw() => DrawRect(new(0, 0, 4, 4), Colors.Blue); }
    internal static void Run()
    {
        using var parent = new Entity { Position = new(4, 8) }; var child = new Paint { Name = "Child", Position = new(2, 3) }; parent.AddChild(child);
        child.ServerState = new(null!, false) { Transform = new Transform(0, new(40, 50)), Modulate = Colors.Red, Visible = false, Z = 17, Filter = TextureFilter.Nearest };
        Check(child.Position == new Vector2(2, 3) && child.Modulate == Colors.White && child.Visible && child.ZIndex == 0 && child.TextureFilter == TextureFilter.ParentNode, "Server edits preserve authored getters.");
        Check(child.RenderGlobal(1).Origin == new Vector2(44, 58) && !child.RenderVisible && child.RenderZ == 17, "Render projections apply separate state.");
        child.Position = new(6, 7); child.Modulate = Colors.Green; child.ZIndex = 5; child.TextureFilter = TextureFilter.Linear;
        Check(child.ServerState.Transform is null && child.ServerState.Modulate is null && child.ServerState.Z is null && child.ServerState.Filter is null && child.ServerState.Visible == false, "Only matching source setters republish.");
        child.ServerState.Visible = null; child.PrepareCanvas(); Check(child.RenderBounds() == new Rect2(0, 0, 4, 4), "Retained source bounds.");
        child.ServerState.CustomRect = new(1, 2, 3, 4); Check(child.RenderBounds() == new Rect2(1, 2, 3, 4), "Custom bounds override retained geometry.");
        using var authored = new Entity { Modulate = Colors.Green, Position = new(3, 4) }; authored.ServerState = new(null!, false) { Modulate = Colors.Red, Transform = new(0, new(99, 99)) };
        using var packed = new PackedScene(); packed.Pack(authored); using var restored = (Entity)packed.Instantiate(); Check(restored.Position == new Vector2(3, 4) && restored.Modulate == Colors.Green && restored.ServerState is null, "Packed scenes retain authored fields without native overrides.");
        using var layer = new CanvasLayer(); var rid = layer.GetCanvas(); Check(rid.IsValid() && layer.GetCanvas() == rid, "Stable borrowed layer canvas before startup."); layer.Dispose(); Check(RenderingCanvasRegistry.ResolveOrNull(rid) is null, "Layer disposal releases weak canvas identity.");
        using var geometry = new ArrayMesh(); geometry.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, new MeshSurfaceData { Vertices = [new(0, 0), new(8, 0), new(0, 8)] });
        using var repeated = new MultiMesh { Mesh = geometry, InstanceCount = 1 }; repeated.SetInstanceTransform2D(0, Transform.Identity);
        using var recorder = new RenderingServerCanvasItem { ServerState = new(null!, true) }; var vertices = new List<CanvasVertex>(64); var batches = new List<CanvasBatch>(8);
        void Replay() { recorder.ClearServerCommands(); recorder.AppendServerMesh(geometry, null, Transform.Identity, Colors.White); recorder.AppendServerMultiMesh(repeated, null); vertices.Clear(); batches.Clear(); recorder.AppendCanvas(vertices, batches, Transform.Identity); }
        for (var i = 0; i < 64; i++) Replay(); var before = GC.GetAllocatedBytesForCurrentThread(); for (var i = 0; i < 128; i++) Replay(); Check(GC.GetAllocatedBytesForCurrentThread() == before && vertices.Count == 6, "128 warmed clear/record/replay cycles allocate zero managed bytes.");
        recorder.ClearServerCommands(); Check(recorder.RenderBounds() == default, "Clear removes commands and borrowed geometry references.");
        Console.WriteLine("128 owned canvas command cycles allocate zero managed bytes.");
        Console.WriteLine("Canvas render projections, source republishing, bounds and borrowed identities passed.");
    }
    internal static void RunHost()
    {
        var backend = Environment.GetEnvironmentVariable("ELECTRON2D_RENDER_BACKEND") ?? "gpu";
        Engine.MaxFPS = 60; ProjectSettings.Set(ProjectSettings.RenderingMethod, backend); ProjectSettings.Set(ProjectSettings.RenderingFallback, false);
        using var mesh = new ArrayMesh(); mesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, new MeshSurfaceData { Vertices = [new(0, 0), new(8, 0), new(8, 8), new(0, 8)], Indices = [0, 1, 2, 0, 2, 3] });
        using var skinned = MeshSkinTests.Mesh(8, 5);
        using var multi = new MultiMesh { Mesh = mesh }; multi.InstanceCount = 1; multi.SetInstanceTransform2D(0, Transform.Identity);
        var window = new Window { Size = new(96, 64) }; var sub = new SubViewport { Name = "OtherView", Size = new(96, 64), RenderTargetUpdateMode = ViewportUpdateMode.Always }; window.AddChild(sub);
        var source = new Paint { Name = "Source", Position = new(64, 32) }; window.AddChild(source);
        RID canvas = default, root = default, child = default, instances = default; var phase = 0; long before = 0, mutation = 0, active = 0, idle = 0; RID cleanup = default, skinItem = default, palette = default, patch = default;
        window.Ready += _ =>
        {
            Check(RenderingServer.GetCurrentRenderingMethod() == backend, "Requested native backend is active."); RenderingServer.SetDefaultClearColor(Colors.Black);
            canvas = RenderingServer.CanvasCreate(); root = RenderingServer.CanvasItemCreate(); child = RenderingServer.CanvasItemCreate(); instances = RenderingServer.CanvasItemCreate();
            RenderingServer.ViewportAttachCanvas(window.GetViewportRID(), canvas); RenderingServer.ViewportAttachCanvas(sub.GetViewportRID(), canvas);
            RenderingServer.ViewportSetCanvasTransform(sub.GetViewportRID(), canvas, new(0, new(24, 0))); RenderingServer.ViewportSetCanvasStacking(window.GetViewportRID(), canvas, 1, -2);
            RenderingServer.CanvasItemSetParent(root, canvas); RenderingServer.CanvasItemSetParent(child, root); RenderingServer.CanvasItemSetParent(instances, canvas);
            RenderingServer.CanvasItemSetTransform(root, new(0, new(8, 8))); RenderingServer.CanvasItemSetTransform(child, new(0, new(4, 0))); RenderingServer.CanvasItemSetTransform(instances, new(0, new(8, 40)));
            RenderingServer.CanvasItemAddMesh(root, mesh.GetRID(), modulate: Colors.Red); RenderingServer.CanvasItemAddMesh(child, mesh.GetRID(), modulate: Colors.Blue); RenderingServer.CanvasItemAddMultiMesh(instances, multi.GetRID());
            patch = RenderingServer.CanvasItemCreate(); RenderingServer.CanvasItemSetParent(patch, canvas); RenderingServer.CanvasItemSetTransform(patch, new(0, new(8, 56)));
            RenderingServer.CanvasItemAddNinePatch(patch, new(0, 0, 8, 8), default, default, Vector2.Zero, Vector2.Zero, AxisStretchMode.TileFit, AxisStretchMode.Tile, modulate: Colors.Green);
            Reject<ArgumentOutOfRangeException>(() => RenderingServer.CanvasItemAddNinePatch(patch, new(0, 0, 8, 8), default, default, Vector2.Zero, Vector2.Zero, (AxisStretchMode)3));
            Check(RenderingServer.DebugCanvasItemGetRect(patch) == new Rect2(0, 0, 8, 8), "Server nine-patch retained bounds.");
            palette = RenderingServer.SkeletonCreate(); RenderingServer.SkeletonAllocateData(palette, 8); RenderingServer.SkeletonBoneSetTransform2D(palette, 5, new(0, new(12, 0)));
            skinItem = RenderingServer.CanvasItemCreate(); RenderingServer.CanvasItemSetParent(skinItem, canvas); RenderingServer.CanvasItemSetTransform(skinItem, new(0, new(56, 8))); RenderingServer.CanvasItemAttachSkeleton(skinItem, palette); RenderingServer.CanvasItemAddMesh(skinItem, skinned.GetRID(), modulate: Colors.Blue);
            Check(RenderingServer.DebugCanvasItemGetRect(root) == new Rect2(0, 0, 8, 8), "Server mesh command bounds.");
            Reject<ArgumentException>(() => RenderingServer.CanvasItemSetParent(root, child)); Reject<ArgumentException>(() => RenderingServer.CanvasItemSetParent(root, mesh.GetRID())); Reject<ArgumentOutOfRangeException>(() => RenderingServer.CanvasItemSetZIndex(root, 4097)); Reject<ArgumentOutOfRangeException>(() => RenderingServer.CanvasItemSetDefaultTextureFilter(root, TextureFilter.Max)); Reject<ArgumentOutOfRangeException>(() => RenderingServer.CanvasItemSetDefaultTextureRepeat(root, TextureRepeat.Max)); Reject<ArgumentException>(() => RenderingServer.CanvasItemSetTransform(root, new(float.NaN, Vector2.Zero))); Reject<InvalidOperationException>(() => RenderingServer.FreeRID(source.GetCanvasItem())); Reject<InvalidOperationException>(() => RenderingServer.FreeRID(source.GetCanvas()));
            RenderingServer.CanvasItemSetDefaultTextureFilter(root, TextureFilter.Nearest); RenderingServer.CanvasItemSetDefaultTextureRepeat(root, TextureRepeat.Disabled);
            Reject<InvalidOperationException>(() => Task.Run(() => RenderingServer.CanvasItemSetVisible(child, true)).GetAwaiter().GetResult());
            var chain = new RID[1026]; chain[0] = RenderingServer.CanvasItemCreate();
            for (var i = 1; i <= 1025; i++) { chain[i] = RenderingServer.CanvasItemCreate(); if (i < 1025) RenderingServer.CanvasItemSetParent(chain[i], chain[i - 1]); }
            Reject<InvalidOperationException>(() => RenderingServer.CanvasItemSetParent(chain[1025], chain[1024]));
            Reject<InvalidOperationException>(() => RenderingServer.CanvasItemSetParent(chain[0], root)); Check(RenderingCanvasItemRegistry.Resolve(chain[0]).RenderParent is null, "Rejected deep-subtree reparent preserves root.");
            for (var i = 0; i <= 1025; i++) RenderingServer.FreeRID(chain[i]);
            window.Tree!.ProcessFrameStarted += _ => { before = GC.GetAllocatedBytesForCurrentThread(); if (phase is >= 16 and < 144) { RenderingServer.CanvasItemSetTransform(child, new(0, new(8 + (phase & 1), 8))); RenderingServer.CanvasItemSetModulate(child, Colors.White); mutation = GC.GetAllocatedBytesForCurrentThread() - before; } };
            RenderingServer.FramePreDraw += () => before = GC.GetAllocatedBytesForCurrentThread();
            RenderingServer.FramePostDraw += () =>
            {
                if (phase >= 16)
                {
                    if (phase == 16) { using var pixels = window.GetTexture().GetImage()!; Pixel(pixels, 25, 9, Colors.Blue); Pixel(pixels, 9, 9, Colors.Black); window.GlobalCanvasTransform = Transform.Identity; }
                    if (phase is >= 80 and < 144) active += GC.GetAllocatedBytesForCurrentThread() - before + mutation;
                    if (phase >= 144) idle += GC.GetAllocatedBytesForCurrentThread() - before;
                    if (++phase == 208) { RenderingServer.FreeRID(child); RenderingServer.FreeRID(instances); cleanup = RenderingServer.CanvasItemCreate(); window.Tree!.Quit(); }
                    return;
                }
                using var image = window.GetTexture().GetImage()!; using var other = sub.GetTexture().GetImage()!;
                try
                {
                    if (phase < 10) { Pixel(image, 9, 9, phase is 1 or 9 ? new(.5f, 0, 0) : Colors.Red); Pixel(other, 33, 9, phase is 1 or 9 ? new(.5f, 0, 0) : phase == 5 ? Colors.Black : Colors.Red); }
                    if (phase < 15) Pixel(image, 9, 41, phase == 1 ? new(.5f, 1, 1) : Colors.White);
                    switch (phase)
                    {
                        case 0: Pixel(image, 9, 57, Colors.Green); Pixel(image, 69, 9, Colors.Blue); Pixel(image, 57, 9, Colors.Black); Pixel(image, 13, 9, Colors.Blue); Pixel(other, 37, 9, Colors.Blue); RenderingServer.CanvasSetModulate(canvas, new(.5f, 1, 1)); RenderingServer.CanvasItemClear(patch); RenderingServer.CanvasItemAddNinePatch(patch, new(0, 0, 8, 8), default, default, Vector2.Zero, Vector2.Zero, drawCenter: false, modulate: Colors.Green); break;
                        case 1: Pixel(image, 9, 57, Colors.Black); Pixel(image, 13, 9, Colors.Blue); RenderingServer.CanvasItemClear(patch); RenderingServer.CanvasItemAddNinePatch(patch, new(0, 0, 8, 8), default, default, Vector2.Zero, Vector2.Zero, modulate: Colors.Green); RenderingServer.CanvasSetModulate(canvas, Colors.White); RenderingServer.CanvasItemSetDrawBehindParent(child, true); break;
                        case 2: Pixel(image, 13, 9, Colors.Red); RenderingServer.CanvasItemSetZIndex(child, 1); break;
                        case 3: Pixel(image, 13, 9, Colors.Blue); RenderingServer.CanvasItemSetZAsRelativeToParent(child, false); RenderingServer.CanvasItemSetZIndex(child, -1); break;
                        case 4: Pixel(image, 13, 9, Colors.Red); RenderingServer.ViewportRemoveCanvas(sub.GetViewportRID(), canvas); break;
                        case 5: Pixel(other, 37, 9, Colors.Black); RenderingServer.ViewportAttachCanvas(sub.GetViewportRID(), canvas); RenderingServer.CanvasItemSetDrawBehindParent(child, false); RenderingServer.CanvasItemSetZIndex(child, 0); RenderingServer.CanvasItemSetCustomRect(root, true, new(0, 0, 4, 8)); RenderingServer.CanvasItemSetClip(root, true); break;
                        case 6: Pixel(image, 13, 9, Colors.Black); RenderingServer.CanvasItemSetClip(root, false); RenderingServer.CanvasItemSetCustomRect(root, false); RenderingServer.CanvasSetItemMirroring(canvas, root, new(16, 0)); break;
                        case 7: Pixel(image, 25, 9, Colors.Red); Pixel(image, 29, 9, Colors.Blue); RenderingServer.CanvasSetItemMirroring(canvas, root, Vector2.Zero); RenderingServer.CanvasItemSetSortChildrenByY(root, true); RenderingServer.CanvasItemSetTransform(child, new(0, new(4, -1))); break;
                        case 8: Pixel(image, 13, 9, Colors.Red); RenderingServer.CanvasItemSetSortChildrenByY(root, false); RenderingServer.CanvasItemSetTransform(child, new(0, new(4, 0))); RenderingServer.CanvasItemSetSelfModulate(root, new(1, 1, 1, .5f)); break;
                        case 9: Pixel(image, 13, 9, Colors.Blue); Pixel(image, 9, 9, new(.5f, 0, 0)); RenderingServer.FreeRID(root); break;
                        case 10: Pixel(image, 13, 9, Colors.Black); Check(RenderingServer.DebugCanvasItemGetRect(child) == new Rect2(0, 0, 8, 8), "Free parent preserves independent child."); RenderingServer.CanvasItemSetParent(child, canvas); RenderingServer.CanvasItemSetTransform(child, new(0, new(8, 8))); RenderingServer.CanvasItemSetVisible(child, false); break;
                        case 11: Pixel(image, 9, 9, Colors.Black); RenderingServer.CanvasItemSetVisible(child, true); RenderingServer.CanvasItemSetModulate(source.GetCanvasItem(), Colors.Red); Check(source.Modulate == Colors.White, "Low-level scene tint does not write getter."); break;
                        case 12: Pixel(image, 9, 9, Colors.Blue); Pixel(image, 65, 33, Colors.Black); source.Modulate = Colors.White; RenderingServer.CanvasItemClear(source.GetCanvasItem()); RenderingServer.CanvasItemAddMesh(source.GetCanvasItem(), mesh.GetRID(), modulate: Colors.Green); break;
                        case 13: Pixel(image, 65, 33, Colors.Green); source.QueueRedraw(); break;
                        case 14: Pixel(image, 65, 33, Colors.Blue); image.SavePNG("/tmp/e2d-owned-canvas-" + backend + ".png"); RenderingServer.CanvasItemSetCustomRect(source.GetCanvasItem(), true, new(-100, -100, 4, 4)); RenderingServer.FreeRID(canvas); break;
                        case 15: Pixel(image, 65, 33, Colors.Black); RenderingServer.CanvasItemSetCustomRect(source.GetCanvasItem(), false); Pixel(image, 9, 9, Colors.Black); Check(RenderingServer.DebugCanvasItemGetRect(child) == new Rect2(0, 0, 8, 8), "Free canvas preserves item resources."); canvas = RenderingServer.CanvasCreate(); RenderingServer.ViewportAttachCanvas(window.GetViewportRID(), canvas); RenderingServer.CanvasItemSetParent(child, canvas); RenderingServer.CanvasItemSetParent(instances, canvas); window.GlobalCanvasTransform = new(0, new(16, 0)); break;
                    }
                }
                catch (Exception error) { throw new InvalidOperationException("Owned canvas phase=" + phase + " backend=" + backend, error); }
                phase++;
            };
        };
        Check(Engine.Run(window) == 0 && phase == 208, "Canvas native lifecycle."); Check(RenderingCanvasRegistry.ResolveOrNull(canvas) is null && RenderingCanvasItemRegistry.ResolveOrNull(child) is null && RenderingCanvasItemRegistry.ResolveOrNull(cleanup) is null && RenderingCanvasItemRegistry.ResolveOrNull(skinItem) is null && RenderingCanvasItemRegistry.ResolveOrNull(patch) is null && !RenderingSkeletonRegistry.Contains(palette), "Explicit release and renderer teardown.");
        Console.WriteLine("Owned canvas multi-viewport, graph, order, tint, clip, mirroring, commands, orphan lifetime and source republishing passed, 64 mutation+render + 64 render intervals allocate " + active + "/" + idle + " managed bytes: " + backend);
    }
    private static void Pixel(Image image, int x, int y, Color expected) { var actual = image.GetPixel(x, y); Check(Math.Abs(actual.R - expected.R) < .02 && Math.Abs(actual.G - expected.G) < .02 && Math.Abs(actual.B - expected.B) < .02, "Pixel " + x + "," + y + " actual=" + actual + " expected=" + expected); }
    private static void Check(bool value, string text) { if (!value) throw new InvalidOperationException(text); }
    private static void Reject<T>(Action action) where T : Exception { try { action(); } catch (T) { return; } throw new InvalidOperationException("Expected " + typeof(T).Name); }
}
