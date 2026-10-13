using Electron2D;

internal static class CanvasViewportConfigurationTests
{
    private sealed class Paint : Entity
    {
        internal int Draws;
        internal Action? Drawing;
        protected override void OnDraw() { Draws++; Drawing?.Invoke(); DrawRect(new(0, 0, 8, 8), Colors.Lime); }
    }
    internal static void Run(string backend)
    {
        Reject<InvalidOperationException>(() => RenderingServer.ViewportSetCanvasCullMask(default, 0));
        ProjectSettings.Set(ProjectSettings.RenderingMethod, backend);
        ProjectSettings.Set(ProjectSettings.RenderingFallback, false);
        using var source = Image.CreateFromData(2, 1, false, Image.Format.Rgba8, [255, 0, 0, 255, 0, 0, 255, 255]);
        source.GenerateMIPMaps();
        using var texture = ImageTexture.CreateFromImage(source);
        var window = new Window { Size = new(128, 96), CanvasCullMask = 1 };
        var scene = new Paint { Position = new(64, 32) }; window.AddChild(scene);
        var second = new SubViewport { Size = new(128, 96), RenderTargetUpdateMode = ViewportUpdateMode.Always }; window.AddChild(second);
        var layer = new CanvasLayer { Layer = 1 }; var layerPaint = new Paint { Position = new(80, 32) }; layer.AddChild(layerPaint); window.AddChild(layer);
        RID owned = default, canvas = default, item = default, high = default, ownedTexture = default;
        using var rootTexture = window.GetTexture(); using var secondTexture = second.GetTexture();
        var frames = 0; long before = 0, active = 0, idle = 0; var warmed = 0; Vector2 lastSize = default;
        window.Ready += _ =>
        {
            RenderingServer.SetDefaultClearColor(Colors.Black);
            var root = window.GetViewportRID(); var other = second.GetViewportRID();
            owned = RenderingServer.ViewportCreate(); RenderingServer.ViewportSetSize(owned, 128, 96);
            RenderingServer.ViewportSetUpdateMode(owned, ViewportUpdateMode.Always); RenderingServer.ViewportSetActive(owned, true);
            ownedTexture = RenderingServer.ViewportGetTexture(owned);
            canvas = RenderingServer.CanvasCreate(); item = RenderingServer.CanvasItemCreate(); high = RenderingServer.CanvasItemCreate();
            foreach (var target in new[] { root, other, owned }) RenderingServer.ViewportAttachCanvas(target, canvas);
            RenderingServer.CanvasItemSetParent(item, canvas); RenderingServer.CanvasItemSetParent(high, canvas);
            RenderingServer.CanvasItemAddRect(item, new(0, 0, 8, 8), Colors.Red);
            RenderingServer.CanvasItemAddTextureRectRegion(item, new(0, 16, 32, 8), texture.GetRID(), new(0, 0, 4, 1), clipUV: false);
            RenderingServer.CanvasItemAddRect(high, new(40, 0, 8, 8), Colors.Blue);
            RenderingServer.CanvasItemSetVisibilityLayer(high, 0x80000000);
            foreach (var target in new[] { root, other, owned }) RenderingServer.ViewportSetDefaultCanvasItemTextureFilter(target, TextureFilter.Nearest);
            RenderingServer.ViewportSetGlobalCanvasTransform(other, new(0, new(8, 0)));
            RenderingServer.ViewportSetCanvasCullMask(owned, 0x80000000);
            Reject<ArgumentException>(() => RenderingServer.ViewportSetCanvasCullMask(canvas, 1));
            using (var foreign = new SubViewport()) Reject<InvalidOperationException>(() => RenderingServer.ViewportSetCanvasCullMask(foreign.GetViewportRID(), 1));
            Reject<InvalidOperationException>(() => Task.Run(() => RenderingServer.ViewportSetCanvasCullMask(root, 0)).GetAwaiter().GetResult());
            foreach (var invalid in new[] { -1, 0, (int)TextureFilter.Max, 99 }) Reject<ArgumentOutOfRangeException>(() => RenderingServer.ViewportSetDefaultCanvasItemTextureFilter(root, (TextureFilter)invalid));
            foreach (var invalid in new[] { -1, 0, (int)TextureRepeat.Max, 99 }) Reject<ArgumentOutOfRangeException>(() => RenderingServer.ViewportSetDefaultCanvasItemTextureRepeat(root, (TextureRepeat)invalid));
            var nan = Transform.Identity; nan.Origin.X = float.NaN;
            Reject<ArgumentException>(() => RenderingServer.ViewportSetGlobalCanvasTransform(root, nan));
            Check(window.GlobalCanvasTransform == Transform.Identity && window.CanvasCullMask == 1 && window.CanvasItemDefaultTextureFilter == Viewport.DefaultCanvasItemTextureFilter.Linear, "Server assignments preserve authored properties.");
            scene.Drawing = () => Reject<InvalidOperationException>(() => RenderingServer.ViewportSetCanvasCullMask(root, 0));
            RenderingServer.FramePostDraw += Verify;
            RenderingServer.FramePreDraw += () =>
            {
                if (frames < 14) return;
                before = GC.GetAllocatedBytesForCurrentThread();
                if (warmed < 96)
                {
                    var enabled = (warmed & 1) == 0;
                    RenderingServer.ViewportSetCanvasCullMask(root, enabled ? 1u : 0x80000000u);
                    RenderingServer.ViewportSetGlobalCanvasTransform(root, new(0, new(enabled ? 0 : 8, 0)));
                    RenderingServer.ViewportSetDefaultCanvasItemTextureFilter(root, TextureFilter.Nearest);
                    RenderingServer.ViewportSetDefaultCanvasItemTextureRepeat(root, enabled ? TextureRepeat.Disabled : TextureRepeat.Enabled);
                }
            };
            void Verify()
            {
                if (frames >= 14)
                {
                    var size = rootTexture.GetSize();
                    if (size != lastSize) { lastSize = size; warmed = 0; active = idle = 0; }
                    else if (warmed >= 32 && warmed < 96) active += GC.GetAllocatedBytesForCurrentThread() - before;
                    else if (warmed >= 96) idle += GC.GetAllocatedBytesForCurrentThread() - before;
                    if (++warmed == 160)
                    { Check(active == 0 && idle == 0, $"64 active and 64 idle frames: {active}/{idle} managed bytes."); RenderingServer.FreeRID(item); RenderingServer.FreeRID(high); RenderingServer.FreeRID(canvas); window.Tree!.Quit(); }
                    if (++frames > 2048) throw new Exception("Stable window size was not reached.");
                    return;
                }
                using var a = rootTexture.GetImage()!; using var b = secondTexture.GetImage()!; using var c = frames < 13 ? RenderingServer.Texture2DGet(ownedTexture) : null;
                switch (frames++)
                {
                    case 0:
                        Pixel(a, 4, 4, Colors.Red); Pixel(b, 12, 4, Colors.Red); Pixel(b, 4, 4, Colors.Black);
                        Pixel(c!, 44, 4, Colors.Blue); Pixel(c!, 4, 4, Colors.Black); Pixel(a, 20, 20, Colors.Blue);
                        RenderingServer.ViewportSetDefaultCanvasItemTextureRepeat(root, TextureRepeat.Enabled); break;
                    case 1:
                        Pixel(a, 20, 20, Colors.Red); Pixel(b, 28, 20, Colors.Blue);
                        RenderingServer.ViewportSetDefaultCanvasItemTextureFilter(root, TextureFilter.Linear); break;
                    case 2:
                        var mixed = a.GetPixel(7, 20); Check(mixed.R > .25f && mixed.R < .75f && mixed.B > .25f && mixed.B < .75f, $"Linear sampling mixes texels: {mixed}.");
                        RenderingServer.CanvasItemSetDefaultTextureFilter(item, TextureFilter.Nearest);
                        RenderingServer.CanvasItemSetDefaultTextureRepeat(item, TextureRepeat.Disabled); break;
                    case 3:
                        Pixel(a, 7, 20, Colors.Red); Pixel(a, 20, 20, Colors.Blue);
                        RenderingServer.CanvasItemSetDefaultTextureFilter(item, TextureFilter.ParentNode);
                        RenderingServer.CanvasItemSetDefaultTextureRepeat(item, TextureRepeat.ParentNode);
                        RenderingServer.ViewportSetCanvasTransform(root, canvas, new(0, Vector2.One * 2, 0, new(2, 0)));
                        RenderingServer.ViewportSetGlobalCanvasTransform(root, new(0, new(8, 8))); break;
                    case 4:
                        Pixel(a, 12, 12, Colors.Red); Pixel(a, 76, 44, Colors.Lime); Pixel(a, 92, 44, Colors.Lime);
                        Pixel(a, 4, 4, Colors.Black); Check(window.GetFinalTransform() == Transform.Identity, "Input/logical transform preserves authored state.");
                        RenderingServer.ViewportSetCanvasTransform(root, canvas, Transform.Identity);
                        window.GlobalCanvasTransform = Transform.Identity; window.CanvasItemDefaultTextureFilter = Viewport.DefaultCanvasItemTextureFilter.Nearest;
                        window.CanvasItemDefaultTextureRepeat = Viewport.DefaultCanvasItemTextureRepeat.Disabled; break;
                    case 5:
                        Pixel(a, 4, 4, Colors.Red); Pixel(a, 20, 20, Colors.Blue);
                        RenderingServer.ViewportSetCanvasCullMask(root, 0); break;
                    case 6:
                        Pixel(a, 4, 4, Colors.Black); Pixel(a, 68, 36, Colors.Black);
                        window.SetCanvasCullMaskBit(0, true); break;
                    case 7:
                        Pixel(a, 4, 4, Colors.Red); Pixel(a, 68, 36, Colors.Lime);
                        RenderingServer.ViewportSetCanvasCullMask(root, 0x80000000); break;
                    case 8:
                        Pixel(a, 44, 4, Colors.Blue); Pixel(a, 4, 4, Colors.Black); window.CanvasCullMask = 1;
                        RenderingServer.ViewportSetGlobalCanvasTransform(root, new(0, Vector2.Zero, 0, Vector2.Zero)); break;
                    case 9:
                        Pixel(a, 4, 4, Colors.Black); window.GlobalCanvasTransform = Transform.Identity;
                        RenderingServer.ViewportSetDefaultCanvasItemTextureFilter(root, TextureFilter.Nearest); break;
                    case 10:
                        using (var packed = new PackedScene()) { packed.Pack(second); using var restored = (SubViewport)packed.Instantiate(); Check(restored.GlobalCanvasTransform == Transform.Identity && restored.CanvasItemDefaultTextureFilter == Viewport.DefaultCanvasItemTextureFilter.Linear && restored.RenderingOwner is null, "Packed viewport retains authored values without session overrides."); }
                        Pixel(a, 4, 4, Colors.Red); var drawBaseline = scene.Draws; var layerBaseline = layerPaint.Draws;
                        RenderingServer.ViewportSetCanvasCullMask(root, 1);
                        RenderingServer.ViewportSetGlobalCanvasTransform(root, Transform.Identity);
                        RenderingServer.ViewportSetDefaultCanvasItemTextureFilter(root, TextureFilter.Nearest);
                        RenderingServer.ViewportSetDefaultCanvasItemTextureRepeat(root, TextureRepeat.Disabled);
                        RenderingServer.ViewportSetDefaultCanvasItemTextureFilter(root, TextureFilter.Linear);
                        window.CanvasItemDefaultTextureFilter = Viewport.DefaultCanvasItemTextureFilter.Nearest;
                        scene.PrepareCanvas(); layerPaint.PrepareCanvas();
                        Check(scene.Draws == drawBaseline && layerPaint.Draws == layerBaseline, "Server mutations preserve prepared retained commands.");
                        if (backend == "gpu") RenderingServer.ViewportSetDefaultCanvasItemTextureRepeat(root, TextureRepeat.Mirror); break;
                    case 11:
                        Pixel(a, 7, 20, Colors.Red);
                        if (backend == "gpu") { Pixel(a, 20, 20, Colors.Blue); RenderingServer.ViewportSetDefaultCanvasItemTextureFilter(root, TextureFilter.LinearWithMipmapsAnisotropic); }
                        break;
                    case 12:
                        Pixel(a, 4, 4, Colors.Red);
                        RenderingServer.FreeRID(owned); Reject<ArgumentException>(() => RenderingServer.ViewportSetCanvasCullMask(owned, 1)); break;
                    case 13:
                        RenderingServer.ViewportSetCanvasCullMask(root, 1); break;
                }
            }
        };
        Check(Engine.Run(window) == 0 && frames >= 174, "Native viewport configuration and warm lifecycle.");
        Check(window.IsDisposed && window.RenderingOwner is null && second.RenderingOwner is null, "Shutdown disposes scene nodes and removes the rendering session.");
        Overflow();
        if (backend == "compatibility") { Unsupported(texture, filter: true); Unsupported(texture, filter: false); }
        Console.WriteLine($"Viewport canvas configuration passed ({backend}); active={active}, idle={idle} managed bytes.");
    }
    private static void Overflow()
    {
        var window = new Window { Size = new(128, 96) }; using var target = window.GetTexture(); RID canvas = default;
        window.Ready += _ =>
        {
            canvas = RenderingServer.CanvasCreate(); RenderingServer.ViewportAttachCanvas(window.GetViewportRID(), canvas);
            var item = RenderingServer.CanvasItemCreate(); RenderingServer.CanvasItemSetParent(item, canvas);
            RenderingServer.CanvasItemAddRect(item, new(0, 0, 8, 8), Colors.Red);
            var large = new Transform(0, Vector2.One * float.MaxValue, 0, Vector2.Zero);
            RenderingServer.ViewportSetCanvasTransform(window.GetViewportRID(), canvas, large);
            RenderingServer.ViewportSetGlobalCanvasTransform(window.GetViewportRID(), large);
        };
        try { Engine.Run(window); throw new Exception("Expected overflowing canvas transform rejection."); }
        catch (InvalidOperationException error) { Check(error.Message.Contains("overflowed finite coordinates", StringComparison.Ordinal), "Overflow rejects before native submission."); }
        Reject<ArgumentException>(() => RenderingCanvasRegistry.Resolve(canvas));
        Check(!RenderingServer.IsAvailable && window.IsDisposed, "Overflow cleans native targets and identities.");
    }
    private static void Unsupported(Texture texture, bool filter)
    {
        var window = new Window { Size = new(128, 96) }; using var target = window.GetTexture(); RID item = default;
        window.Ready += _ =>
        {
            var canvas = RenderingServer.CanvasCreate(); RenderingServer.ViewportAttachCanvas(window.GetViewportRID(), canvas);
            item = RenderingServer.CanvasItemCreate(); RenderingServer.CanvasItemSetParent(item, canvas);
            RenderingServer.CanvasItemAddTextureRect(item, new(0, 0, 16, 16), texture.GetRID());
            if (filter) RenderingServer.ViewportSetDefaultCanvasItemTextureFilter(window.GetViewportRID(), TextureFilter.LinearWithMipmaps);
            else RenderingServer.ViewportSetDefaultCanvasItemTextureRepeat(window.GetViewportRID(), TextureRepeat.Mirror);
        };
        Reject<NotSupportedException>(() => Engine.Run(window));
        Reject<ArgumentException>(() => RenderingCanvasItemRegistry.Resolve(item));
        Check(!RenderingServer.IsAvailable, "Unsupported native sampling cleans the session.");
    }
    private static void Pixel(Image image, int x, int y, Color expected) { var actual = image.GetPixel(x, y); Check(MathF.Abs(actual.R - expected.R) < .03f && MathF.Abs(actual.G - expected.G) < .03f && MathF.Abs(actual.B - expected.B) < .03f, $"Pixel {x},{y}: {actual} expected {expected}."); }
    private static void Reject<T>(Action call) where T : Exception { try { call(); } catch (T) { return; } throw new Exception($"Expected {typeof(T).Name}."); }
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }
}
