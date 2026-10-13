using Electron2D;

internal static class CanvasServerGroupTests
{
    internal static void Run(string backend)
    {
        using (var authored = new CanvasGroup())
        {
            authored.ServerState = new(null!, false) { Group = new(RenderingServer.CanvasGroupMode.Disabled, 0, false, 0, false) };
            authored.FitMargin = 1; Check(authored.ServerState.Group is null, "Group fit setter republishes.");
            authored.ServerState.Group = new(RenderingServer.CanvasGroupMode.Disabled, 0, false, 0, false); authored.ClearMargin = 1; Check(authored.ServerState.Group is null, "Group clear setter republishes.");
            authored.ServerState.Group = new(RenderingServer.CanvasGroupMode.Disabled, 0, false, 0, false); authored.UseMIPMaps = true; Check(authored.ServerState.Group is null, "Group mipmap setter republishes.");
        }
        using (var authored = new Entity()) { authored.ServerState = new(null!, false) { Group = new(RenderingServer.CanvasGroupMode.Transparent, 0, true, 0, false) }; authored.ClipChildren = ClipChildrenMode.Only; Check(authored.ServerState.Group is null, "Mask source setter republishes."); }
        ProjectSettings.Set(ProjectSettings.RenderingMethod, backend); ProjectSettings.Set(ProjectSettings.RenderingFallback, false);
        var window = new Window { Size = new(128, 96) }; var view = new SubViewport { Size = new(128, 96), RenderTargetUpdateMode = ViewportUpdateMode.Always }; window.AddChild(view);
        RID canvas = default, root = default, child = default, second = default; var phase = 0; long before = 0, allocated = 0;
        window.Ready += _ =>
        {
            RenderingServer.SetDefaultClearColor(Colors.Black);
            canvas = RenderingServer.CanvasCreate(); RenderingServer.ViewportAttachCanvas(window.GetViewportRID(), canvas); RenderingServer.ViewportAttachCanvas(view.GetViewportRID(), canvas);
            root = RenderingServer.CanvasItemCreate(); child = RenderingServer.CanvasItemCreate(); second = RenderingServer.CanvasItemCreate();
            RenderingServer.CanvasItemSetParent(root, canvas); RenderingServer.CanvasItemSetParent(child, root); RenderingServer.CanvasItemSetParent(second, root);
            RenderingServer.CanvasItemSetTransform(root, new(0, new(32, 8))); RenderingServer.CanvasItemAddRect(root, new(0, 0, 16, 16), Colors.White);
            RenderingServer.CanvasItemAddRect(child, new(4, 0, 24, 16), Colors.Red); RenderingServer.CanvasItemAddRect(second, new(12, 0, 24, 16), Colors.Red);
            RenderingServer.CanvasItemSetSelfModulate(root, new(1, 1, 1, .5f)); RenderingServer.CanvasItemSetCanvasGroupMode(root, RenderingServer.CanvasGroupMode.Disabled);
            Reject<ArgumentOutOfRangeException>(() => RenderingServer.CanvasItemSetCanvasGroupMode(root, (RenderingServer.CanvasGroupMode)99));
            Reject<ArgumentOutOfRangeException>(() => RenderingServer.CanvasItemSetCanvasGroupMode(root, RenderingServer.CanvasGroupMode.Transparent, float.NaN));
            Reject<ArgumentOutOfRangeException>(() => RenderingServer.CanvasItemSetCanvasGroupMode(root, RenderingServer.CanvasGroupMode.Transparent, fitMargin: -1));
            Reject<ArgumentException>(() => RenderingServer.CanvasItemSetCanvasGroupMode(canvas, RenderingServer.CanvasGroupMode.Disabled));
            Reject<InvalidOperationException>(() => Task.Run(() => RenderingServer.CanvasItemSetCanvasGroupMode(root, RenderingServer.CanvasGroupMode.Disabled)).GetAwaiter().GetResult());
            RenderingServer.FramePreDraw += () => { if (phase >= 16) { before = GC.GetAllocatedBytesForCurrentThread(); RenderingServer.CanvasItemSetCanvasGroupMode(root, (phase & 1) == 0 ? RenderingServer.CanvasGroupMode.Transparent : RenderingServer.CanvasGroupMode.ClipOnly, clearMargin: 3, fitEmpty: true, fitMargin: 2); } };
            RenderingServer.FramePostDraw += () =>
            {
                if (phase >= 20) { allocated += GC.GetAllocatedBytesForCurrentThread() - before; if (++phase == 84) { Check(allocated == 0, $"64 warmed group mutation/render intervals allocate zero managed bytes: {allocated}."); RenderingServer.FreeRID(child); RenderingServer.FreeRID(second); RenderingServer.FreeRID(root); RenderingServer.FreeRID(canvas); window.Tree!.Quit(); } return; }
                using var image = window.GetTexture().GetImage()!; using var other = view.GetTexture().GetImage()!;
                switch (phase++)
                {
                    case 0: Pixel(image, 33, 12, new(.5f, .5f, .5f)); Pixel(image, 37, 12, Colors.Red); Pixel(image, 61, 12, Colors.Red); RenderingServer.CanvasItemSetCanvasGroupMode(root, RenderingServer.CanvasGroupMode.Transparent); break;
                    case 1: Pixel(image, 37, 12, new(.5f, 0, 0)); Pixel(image, 45, 12, new(.5f, 0, 0)); Pixel(image, 61, 12, Colors.Black); Pixel(other, 45, 12, new(.5f, 0, 0)); RenderingServer.CanvasItemSetSelfModulate(root, Colors.White); RenderingServer.CanvasItemSetCanvasGroupMode(root, RenderingServer.CanvasGroupMode.ClipOnly); break;
                    case 2: Pixel(image, 33, 12, Colors.Black); Pixel(image, 37, 12, Colors.Red); Pixel(image, 53, 12, Colors.Black); RenderingServer.CanvasItemSetCanvasGroupMode(root, RenderingServer.CanvasGroupMode.ClipAndDraw); break;
                    case 3: Pixel(image, 33, 12, Colors.White); Pixel(image, 37, 12, Colors.Red); Pixel(image, 53, 12, Colors.Black); RenderingServer.CanvasItemClear(root); RenderingServer.CanvasItemSetCanvasGroupMode(root, RenderingServer.CanvasGroupMode.Transparent, clearMargin: 3, fitEmpty: false); break;
                    case 4: Pixel(image, 45, 12, Colors.Black); RenderingServer.CanvasItemSetSelfModulate(root, new(1, 1, 1, .5f)); RenderingServer.CanvasItemSetCanvasGroupMode(root, RenderingServer.CanvasGroupMode.Transparent, clearMargin: 3, fitEmpty: true, fitMargin: 2); break;
                    case 5: Pixel(image, 45, 12, new(.5f, 0, 0)); Pixel(image, 61, 12, new(.5f, 0, 0)); using (var back = RenderingServer.Service!.ReadbackBackBuffer(window)) { Pixel(back, 35, 12, Colors.Black); Check(back.GetPixel(35, 12).A < .05f, "Fit and clear margins prepare transparent border."); } RenderingServer.CanvasItemSetCanvasGroupMode(root, RenderingServer.CanvasGroupMode.ClipOnly, fitEmpty: true, blurMIPMaps: backend == "gpu"); break;
                    case 6: Pixel(image, 45, 12, new(.5f, 0, 0)); RenderingServer.CanvasItemSetCanvasGroupMode(root, RenderingServer.CanvasGroupMode.ClipAndDraw, fitEmpty: true); break;
                    case 7: RenderingServer.CanvasItemSetCanvasGroupMode(root, RenderingServer.CanvasGroupMode.Transparent, fitEmpty: true, blurMIPMaps: backend == "gpu"); break;
                    case 8: Pixel(image, 45, 12, new(.5f, 0, 0)); RenderingServer.CanvasItemSetCanvasGroupMode(root, RenderingServer.CanvasGroupMode.Disabled); break;
                    case 9: Pixel(image, 45, 12, Colors.Red); RenderingServer.CanvasItemSetParent(second, canvas); RenderingServer.CanvasItemSetCanvasGroupMode(root, RenderingServer.CanvasGroupMode.Transparent, fitEmpty: true); break;
                    case 10: Pixel(image, 13, 12, Colors.Red); Pixel(image, 45, 12, new(.5f, 0, 0)); RenderingServer.CanvasItemSetParent(second, root); break;
                    case 11: RenderingServer.CanvasItemSetVisible(child, false); RenderingServer.CanvasItemSetVisible(second, false); RenderingServer.CanvasItemAddRect(root, new(0, 0, 16, 16), Colors.White); RenderingServer.CanvasItemSetCanvasGroupMode(root, RenderingServer.CanvasGroupMode.ClipOnly); break;
                    case 12: Pixel(image, 33, 12, Colors.Black); RenderingServer.CanvasItemSetCanvasGroupMode(root, RenderingServer.CanvasGroupMode.ClipAndDraw); break;
                    case 13: Pixel(image, 33, 12, new(.5f, .5f, .5f)); RenderingServer.CanvasItemSetVisible(child, true); RenderingServer.CanvasItemSetVisible(second, true); RenderingServer.CanvasItemClear(root); RenderingServer.CanvasItemSetCanvasGroupMode(root, RenderingServer.CanvasGroupMode.Transparent, fitEmpty: true); break;
                }
            };
        };
        Check(Engine.Run(window) == 0 && phase == 84 && !RenderingServer.IsAvailable, "Group host teardown.");
        RejectRender(nested: true);
        if (backend == "compatibility") RejectRender(nested: false);
        Console.WriteLine("Retained group modes, alpha masks/one-pass opacity, fit/clear margins, empty/disabled groups, two viewports, mipmaps, reparenting and 64 zero-byte mutation/render intervals passed: " + backend);
    }
    private static void RejectRender(bool nested)
    {
        var window = new Window { Size = new(128, 96) };
        window.Ready += _ =>
        {
            var canvas = RenderingServer.CanvasCreate(); RenderingServer.ViewportAttachCanvas(window.GetViewportRID(), canvas);
            var parent = RenderingServer.CanvasItemCreate(); var child = RenderingServer.CanvasItemCreate();
            RenderingServer.CanvasItemSetParent(parent, canvas); RenderingServer.CanvasItemSetParent(child, parent);
            RenderingServer.CanvasItemAddRect(child, new(0, 0, 16, 16), Colors.Red);
            RenderingServer.CanvasItemSetCanvasGroupMode(parent, RenderingServer.CanvasGroupMode.Transparent, fitEmpty: true, blurMIPMaps: !nested);
            if (nested)
            {
                RenderingServer.CanvasItemSetCanvasGroupMode(child, RenderingServer.CanvasGroupMode.Transparent, fitEmpty: true);
                var leaf = RenderingServer.CanvasItemCreate(); RenderingServer.CanvasItemSetParent(leaf, child); RenderingServer.CanvasItemAddRect(leaf, new(0, 0, 16, 16), Colors.Red);
            }
        };
        Reject<NotSupportedException>(() => Engine.Run(window)); Check(window.IsDisposed && !RenderingServer.IsAvailable, "Unsupported retained group tears down its complete owned graph.");
    }
    private static void Pixel(Image image, int x, int y, Color expected) { var actual = image.GetPixel(x, y); Check(MathF.Abs(actual.R - expected.R) < .06f && MathF.Abs(actual.G - expected.G) < .06f && MathF.Abs(actual.B - expected.B) < .06f, $"Pixel {x},{y}: {actual} expected {expected}"); }
    private static void Check(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
    private static void Reject<T>(Action action) where T : Exception { try { action(); } catch (T) { return; } throw new InvalidOperationException(typeof(T).Name + " required."); }
}
