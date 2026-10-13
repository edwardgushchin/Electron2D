using Electron2D;

internal static class CanvasBackBufferTests
{
    internal static void RunManaged()
    {
        Reject<InvalidOperationException>(() => RenderingServer.CanvasItemSetCopyToBackBuffer(default, true, default));
        Reject<ArgumentException>(() => RenderingServer.CanvasItemSetCopyToBackBuffer(default, false, new(float.NaN, 0, 1, 1)));
        using var ordinary = new Entity(); Check(ordinary.RenderCopyRect is null, "Ordinary items do not copy by default.");
        using var copy = new BackBufferCopy { Position = new(4, 8), Rect = new(16, 8, -16, -8) };
        var state = copy.ServerState = new(null!, false) { Copy = (false, default) };
        Check(copy.RenderCopyRect is null && copy.CopyMode == BackBufferCopyMode.Rect, "A disabled override preserves source getters.");
        copy.Rect = copy.Rect; Check(state.Copy is null && copy.RenderCopyRect == copy.Rect, "An equal source rectangle republishes the copy policy.");
        state.Copy = (true, default); copy.CopyMode = copy.CopyMode; Check(state.Copy is null, "An equal source mode republishes the copy policy.");
        state.Copy = (false, default); Action<ElectronObject> fail = _ => throw new InvalidOperationException("Observer failure."); copy.PropertyListChanged += fail;
        Reject<InvalidOperationException>(() => copy.CopyMode = BackBufferCopyMode.Viewport); copy.PropertyListChanged -= fail;
        Check(state.Copy is null && copy.RenderCopyRect == default(Rect2), "Source publication commits before observers.");
        state.Copy = (true, new(90, 50, 1, 1));
        using var packed = new PackedScene(); packed.Pack(copy); using var restored = (BackBufferCopy)packed.Instantiate();
        Check(restored.ServerState is null && restored.Rect == copy.Rect && restored.CopyMode == copy.CopyMode && restored.Position == copy.Position, "Scene storage contains authored values and no session copy state.");
        var signed = new Transform(.4f, new(-2, 3), .1f, new(8, 4)) * new Rect2(16, 8, -16, -8);
        Check(signed.IsFinite() && signed.HasArea(), "Signed local copy bounds transform to finite screen bounds.");
        CanvasCompositionTests.Run();
        Console.WriteLine("Retained backbuffer guards, source publication, observer commitment and scene storage passed.");
    }
    private sealed class Probe : Entity
    {
        internal Action? Drawing;
        protected override void OnDraw() => Drawing?.Invoke();
    }
    private sealed class TestViewport : Viewport { public override Rect2 GetVisibleRect() => new(0, 0, 96, 64); }
    internal static void RunNative(string profile)
    {
        var backend = profile.StartsWith("gpu", StringComparison.Ordinal) ? "gpu" : "compatibility";
        var language = profile.Contains("glsl", StringComparison.Ordinal) ? "Glsl" : "Hlsl";
        var msaa = profile.Contains("msaa", StringComparison.Ordinal) ? Viewport.MSAA.Samples4X : Viewport.MSAA.Disabled;
        ProjectSettings.Set(ProjectSettings.RenderingMethod, backend); ProjectSettings.Set(ProjectSettings.RenderingFallback, false);
        ProjectSettings.Set(ProjectSettings.MSAA2D, msaa); Engine.MaxFPS = 120;
        using var shader = backend == "gpu" ? Load(language) : null;
        using var material = shader is null ? null : new ShaderMaterial { Shader = shader };
        material?.SetShaderParameter("tint", Colors.White); material?.SetShaderParameter("offset", Vector2.Zero);
        var window = new Window { Size = new(160, 96), CanvasCullMask = 1 };
        var other = new SubViewport { Size = new(96, 64), CanvasCullMask = 1, MSAA2D = msaa, RenderTargetUpdateMode = ViewportUpdateMode.Always }; window.AddChild(other);
        var source = new BackBufferCopy { CopyMode = BackBufferCopyMode.Disabled, Position = new(40, 8) }; window.AddChild(source);
        var probe = new Probe(); window.AddChild(probe);
        var particles = new CPUParticles { Amount = 1, Emitting = false, LocalCoords = false, Position = new(64, 8) }; window.AddChild(particles);
        using var rootTexture = window.GetTexture(); using var otherTexture = other.GetTexture();
        RID canvas = default, background = default, copy = default, foreground = default, reader = default, clip = default;
        var phase = 0; var warm = 0; var frames = 0; long before = 0, active = 0, idle = 0; Vector2 lastSize = default, frameSize = default;
        window.Ready += _ =>
        {
            Check(RenderingServer.GetCurrentRenderingMethod() == backend, "Exact requested backend."); RenderingServer.SetDefaultClearColor(Colors.Black);
            canvas = RenderingServer.CanvasCreate(); background = RenderingServer.CanvasItemCreate(); copy = RenderingServer.CanvasItemCreate(); foreground = RenderingServer.CanvasItemCreate(); reader = RenderingServer.CanvasItemCreate(); clip = RenderingServer.CanvasItemCreate();
            foreach (var view in new[] { window.GetViewportRID(), other.GetViewportRID() }) RenderingServer.ViewportAttachCanvas(view, canvas);
            foreach (var item in new[] { background, copy, foreground, reader, clip, source.GetCanvasItem(), particles.GetCanvasItem() }) RenderingServer.CanvasItemSetParent(item, canvas);
            RenderingServer.CanvasItemSetZIndex(particles.GetCanvasItem(), 1); RenderingServer.CanvasItemSetZIndex(copy, 1); RenderingServer.CanvasItemSetZIndex(source.GetCanvasItem(), 1); RenderingServer.CanvasItemSetZIndex(foreground, 2); RenderingServer.CanvasItemSetZIndex(reader, 3);
            RenderingServer.CanvasItemAddRect(background, new(0, 0, 96, 64), Colors.Red); RenderingServer.CanvasItemAddRect(foreground, new(0, 0, 96, 64), Colors.Blue);
            RenderingServer.CanvasItemSetTransform(copy, new(0, new(8, 8))); RenderingServer.CanvasItemSetCopyToBackBuffer(copy, true, new(0, 0, 16, 16));
            if (material is not null) { RenderingServer.CanvasItemSetMaterial(reader, material.GetRID()); RenderingServer.CanvasItemAddRect(reader, new(0, 0, 96, 64), Colors.White); }
            Reject<ArgumentException>(() => RenderingServer.CanvasItemSetCopyToBackBuffer(canvas, true, default));
            Reject<InvalidOperationException>(() => Task.Run(() => RenderingServer.CanvasItemSetCopyToBackBuffer(copy, false, default)).GetAwaiter().GetResult());
            var state = RenderingCanvasItemRegistry.Resolve(copy).ServerState!; var saved = state.Copy;
            Reject<ArgumentException>(() => RenderingServer.CanvasItemSetCopyToBackBuffer(copy, false, new(0, 0, float.PositiveInfinity, 1)));
            Check(state.Copy == saved, "Rejected copy inputs preserve committed policy.");
            using (var foreign = new SceneTree(new TestViewport()))
            {
                var item = new Entity(); foreign.Root.AddChild(item);
                Reject<InvalidOperationException>(() => RenderingServer.CanvasItemSetCopyToBackBuffer(item.GetCanvasItem(), true, default));
            }
            var capture = source.BeginSceneCapture();
            try { Reject<InvalidOperationException>(() => source.Rect = default); } finally { Node.EndSceneCapture(capture); }
            probe.Drawing = () => { RenderingServer.CanvasItemSetCopyToBackBuffer(copy, true, new(0, 0, 16, 16)); probe.Drawing = null; }; probe.QueueRedraw();
            RenderingServer.FramePreDraw += () =>
            {
                if (phase < 16) return; frameSize = rootTexture.GetSize(); before = GC.GetAllocatedBytesForCurrentThread();
                if (warm < 96) RenderingServer.CanvasItemSetCopyToBackBuffer(source.GetCanvasItem(), true, (warm & 1) == 0 ? default : new(0, 0, 16, 16));
            };
            RenderingServer.FramePostDraw += () =>
            {
                if (phase == 16)
                {
                    var size = rootTexture.GetSize(); if (size != lastSize || size != frameSize) { warm = 0; active = idle = 0; lastSize = size; }
                    else if (warm >= 32 && warm < 96) active += GC.GetAllocatedBytesForCurrentThread() - before;
                    else if (warm >= 96) idle += GC.GetAllocatedBytesForCurrentThread() - before;
                    if (++warm == 160) { Check(active == 0 && idle == 0, $"64 active/64 idle copy intervals: {active}/{idle} bytes."); RenderingServer.FreeRID(copy); Reject<ArgumentException>(() => RenderingServer.CanvasItemSetCopyToBackBuffer(copy, true, default)); window.Tree!.Quit(); }
                    if (++frames > 2048) throw new Exception("Copy allocation warmup did not stabilize."); return;
                }
                var renderer = RenderingServer.Service!;
                using var screen = renderer.ReadbackBackBuffer(window); using var second = renderer.ReadbackBackBuffer(other);
                using var pixels = rootTexture.GetImage()!; using var secondPixels = otherTexture.GetImage()!;
                void Snapshot(int x, int y, Color color) { Pixel(screen, x, y, color); Pixel(second, x, y, color); if (material is not null) { Pixel(pixels, x, y, color); Pixel(secondPixels, x, y, color); } }
                void Disabled() { if (material is not null) { Pixel(pixels, 12, 12, Colors.Blue); Pixel(secondPixels, 12, 12, Colors.Blue); } }
                switch (phase++)
                {
                    case 0: Snapshot(12, 12, Colors.Red); RenderingServer.CanvasItemSetCopyToBackBuffer(copy, true, default); RenderingServer.CanvasItemAddRect(copy, new(0, 0, 8, 8), Colors.Lime); break;
                    case 1: Snapshot(72, 48, Colors.Red); RenderingServer.CanvasItemSetCopyToBackBuffer(copy, true, new(16, 16, -16, -16)); break;
                    case 2: Snapshot(12, 12, Colors.Red); RenderingServer.CanvasItemClear(copy); Check(state.Copy is not null, "Clearing commands preserves copy state."); RenderingServer.CanvasItemClear(background); RenderingServer.CanvasItemAddRect(background, new(0, 0, 96, 64), Colors.Lime); break;
                    case 3: Snapshot(12, 12, Colors.Lime); RenderingServer.CanvasItemSetCopyToBackBuffer(copy, false, default); break;
                    case 4:
                        Disabled(); RenderingServer.CanvasItemSetTransform(clip, new(0, new(8, 8))); RenderingServer.CanvasItemSetCustomRect(clip, true, new(0, 0, 16, 16)); RenderingServer.CanvasItemSetClip(clip, true);
                        RenderingServer.CanvasItemSetParent(copy, clip); RenderingServer.CanvasItemSetTransform(copy, Transform.Identity); RenderingServer.CanvasItemSetCopyToBackBuffer(copy, true, new(0, 0, 32, 32)); break;
                    case 5: Snapshot(12, 12, Colors.Lime); RenderingServer.CanvasItemSetVisible(copy, false); break;
                    case 6: Disabled(); RenderingServer.CanvasItemSetVisible(copy, true); RenderingServer.CanvasItemSetVisibilityLayer(copy, 2); break;
                    case 7: Disabled(); RenderingServer.CanvasItemSetVisibilityLayer(copy, 1); RenderingServer.CanvasItemSetParent(copy, canvas); RenderingServer.CanvasItemSetZIndex(copy, -1); RenderingServer.CanvasItemSetCopyToBackBuffer(copy, true, default); break;
                    case 8: Snapshot(12, 12, Colors.Black); RenderingServer.CanvasItemSetCopyToBackBuffer(copy, false, default); RenderingServer.CanvasItemSetCopyToBackBuffer(source.GetCanvasItem(), true, new(0, 0, 16, 16)); break;
                    case 9: Snapshot(44, 12, Colors.Lime); Check(source.CopyMode == BackBufferCopyMode.Disabled, "Borrowed copy preserves source getter."); source.Rect = source.Rect; Check(source.ServerState!.Copy is null, "Source rectangle republishes disabled mode."); break;
                    case 10: Disabled(); source.CopyMode = BackBufferCopyMode.Viewport; break;
                    case 11: Snapshot(72, 48, Colors.Lime); RenderingServer.CanvasItemSetCopyToBackBuffer(source.GetCanvasItem(), false, default); source.CopyMode = source.CopyMode; Check(source.ServerState!.Copy is null, "Equal source mode republishes full copy."); break;
                    case 12: Snapshot(72, 48, Colors.Lime); probe.Drawing = null; RenderingServer.CanvasItemSetCopyToBackBuffer(source.GetCanvasItem(), true, new(0, 0, 0, 16)); break;
                    case 13: RenderingServer.CanvasItemSetCopyToBackBuffer(source.GetCanvasItem(), true, new(10000, 10000, 8, 8)); break;
                    case 14:
                        RenderingServer.CanvasItemClear(background); RenderingServer.CanvasItemAddRect(background, new(0, 0, 96, 64), Colors.Red);
                        RenderingServer.CanvasItemSetCopyToBackBuffer(source.GetCanvasItem(), false, default); RenderingServer.CanvasItemSetCopyToBackBuffer(particles.GetCanvasItem(), true, new(0, 0, 16, 16)); break;
                    case 15: Snapshot(68, 12, Colors.Red); particles.QueueFree(); break;
                }
            };
        };
        Check(Engine.Run(window) == 0 && phase == 16 && warm == 160 && source.ServerState is null, "Copy host lifetime and borrowed session cleanup.");
        var overflow = new Window { Size = new(96, 64) };
        overflow.Ready += _ =>
        {
            var canvasRID = RenderingServer.CanvasCreate(); var item = RenderingServer.CanvasItemCreate(); RenderingServer.ViewportAttachCanvas(overflow.GetViewportRID(), canvasRID);
            RenderingServer.CanvasItemSetParent(item, canvasRID); RenderingServer.CanvasItemSetTransform(item, new(0, new(float.MaxValue, 1), 0, Vector2.Zero));
            RenderingServer.CanvasItemSetCopyToBackBuffer(item, true, new(0, 0, 2, 1));
        };
        try { Engine.Run(overflow); throw new Exception("Expected computed copy bounds to overflow."); }
        catch (InvalidOperationException error) { Check(error.Message.Contains("Canvas copy bounds overflowed finite coordinates", StringComparison.Ordinal), "The render failure reports derived copy bounds."); }
        Check(overflow.IsDisposed, "Derived copy-bound overflow rejects rendering and tears down the session.");
        Console.WriteLine($"Retained backbuffer pixels passed ({profile}, two views), warm={active}/{idle} bytes.");
    }
    private static Shader Load(string language)
    { using var input = typeof(CanvasCompositionTests).Assembly.GetManifestResourceStream("TestShaders.Screen" + language + ".spv")!; using var memory = new MemoryStream(); input.CopyTo(memory); return Shader.CreateFromSPIRV(memory.ToArray()); }
    private static void Pixel(Image image, int x, int y, Color expected) { var value = image.GetPixel(x, y); Check(MathF.Abs(value.R - expected.R) < .03f && MathF.Abs(value.G - expected.G) < .03f && MathF.Abs(value.B - expected.B) < .03f, $"Pixel {x},{y}: {value}, expected {expected}."); }
    private static void Reject<T>(Action call) where T : Exception { try { call(); } catch (T) { return; } throw new Exception($"Expected {typeof(T).Name}."); }
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }
}
