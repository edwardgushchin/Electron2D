using Electron2D;
using static TextureArrayTests;

internal static class ViewportPresentationTests
{
    internal static void Run(string profile)
    {
        var method = ProjectSettings.Get(ProjectSettings.RenderingMethod); var fallback = ProjectSettings.Get(ProjectSettings.RenderingFallback);
        var backend = profile == "compatibility" ? profile : "gpu"; var hdr = profile is "hdr" or "hdr-msaa";
        try
        {
            ProjectSettings.Set(ProjectSettings.RenderingMethod, backend); ProjectSettings.Set(ProjectSettings.RenderingFallback, false);
            var window = new Window { Size = new(64, 48), Title = "Electron2D viewport presentation" };
            var driver = new Driver(window, hdr, profile == "hdr-msaa"); window.AddChild(driver);
            window.Ready += _ =>
            {
                Check(RenderingServer.GetCurrentRenderingMethod() == backend, "Requested backend without fallback.");
                RenderingServer.RenderLoopEnabled = false; RenderingServer.SetDefaultClearColor(Colors.Blue);
            };
            try { Check(Engine.Run(window) == 0 && driver.Completed, "Viewport presentation session completed."); }
            finally { if (!window.IsDisposed) window.Dispose(); }
            Reject<ArgumentException>(() => Viewport.ResolveViewportRID(driver.Freed));
            var empty = new Window { Size = new(64, 48), ContentScaleSize = new(80, 80), ContentScaleMode = ContentScaleMode.Viewport, ContentScaleFactor = 1000 };
            var cleared = false;
            empty.Ready += _ => RenderingServer.FramePostDraw += () =>
            {
                using var pixels = RenderingServer.Service!.Readback();
                Check(pixels.Size == new Vector2i(64, 48) && empty.GetTexture().GetImage() is null, "An initially zero root raster still presents a client-sized image.");
                Pixel(pixels, 8, 8, Colors.Black); cleared = true; empty.Tree!.Quit();
            };
            try { Check(Engine.Run(empty) == 0 && cleared, "Initially zero root raster clears without a source allocation."); }
            finally { if (!empty.IsDisposed) empty.Dispose(); }
            Console.WriteLine($"Viewport native presentation passed: {profile}; warmed owner bytes={driver.WarmBytes}.");
        }
        finally { ProjectSettings.Set(ProjectSettings.RenderingMethod, method); ProjectSettings.Set(ProjectSettings.RenderingFallback, fallback); }
    }
    private sealed class Driver(Window window, bool hdr, bool msaa) : Node
    {
        internal bool Completed; internal long WarmBytes; internal RID Freed;
        protected override void OnReady() => ProcessEnabled = true;
        protected override void OnProcess(double delta)
        {
            if (Completed) return;
            var root = window.GetViewportRID();
            Check(RenderingServer.ViewportGetUpdateMode(root) == ViewportUpdateMode.WhenVisible, "Root visibility drives its initial screen attachment.");
            RenderingServer.ForceDraw(); Screen(8, 8, Colors.Blue);
            var view = RenderingServer.ViewportCreate(); var other = RenderingServer.ViewportCreate();
            var canvas = RenderingServer.CanvasCreate(); var item = RenderingServer.CanvasItemCreate();
            RenderingServer.CanvasItemSetParent(item, canvas);
            foreach (var rid in new[] { view, other })
            {
                RenderingServer.ViewportSetSize(rid, 32, 32); RenderingServer.ViewportSetActive(rid, true);
                RenderingServer.ViewportAttachCanvas(rid, canvas); RenderingServer.ViewportSetUseHDR2D(rid, hdr);
                if (msaa) RenderingServer.ViewportSetMSAA2D(rid, Viewport.MSAA.Samples4X);
            }
            var texture = RenderingServer.ViewportGetTexture(view);
            RenderingServer.ViewportAttachToScreen(root); // Empty root rect removes the normal visibility trigger.
            RenderingServer.ViewportAttachToScreen(view, new(0, 0, 32, 32));
            Draw(item, Colors.Red); RenderingServer.ForceDraw(); Screen(8, 8, Colors.Red); Screen(48, 40, Colors.Black);
            using (var pixels = window.GetTexture().GetImage()!) Pixel(pixels, 8, 8, Colors.Blue);
            Check(RenderingServer.ViewportGetTexture(view) == texture, "Screen attachment preserves texture identity.");
            foreach (var rect in new[] { new Rect2(float.NaN, 0, 1, 1), new Rect2(0, 0, float.PositiveInfinity, 1), new Rect2(0, 0, -1, 1), new Rect2(float.MaxValue, 0, float.MaxValue, 1) })
                Reject<ArgumentException>(() => RenderingServer.ViewportAttachToScreen(view, rect));
            Reject<ArgumentOutOfRangeException>(() => RenderingServer.ViewportAttachToScreen(view, screen: -2));
            Reject<NotSupportedException>(() => RenderingServer.ViewportAttachToScreen(view, screen: 1));
            Reject<ArgumentException>(() => RenderingServer.ViewportAttachToScreen(default));
            using (var foreign = new SubViewport()) Reject<InvalidOperationException>(() => RenderingServer.ViewportAttachToScreen(foreign.GetViewportRID()));
            Task.Run(() => Reject<InvalidOperationException>(() => RenderingServer.ViewportAttachToScreen(view))).GetAwaiter().GetResult();
            Draw(item, Colors.Green); RenderingServer.ForceDraw(false); Screen(8, 8, Colors.Red);
            using (var pixels = RenderingServer.Texture2DGet(texture)!) Pixel(pixels, 8, 8, Colors.Green);
            RenderingServer.ForceDraw(); Screen(8, 8, Colors.Green);
            RenderingServer.ViewportSetActive(view, false); Draw(item, Colors.Red); RenderingServer.ForceDraw(); Screen(8, 8, Colors.Green);
            RenderingServer.ViewportSetActive(view, true); RenderingServer.ViewportSetUpdateMode(view, ViewportUpdateMode.Disabled);
            RenderingServer.ForceDraw(); Screen(8, 8, Colors.Green);
            RenderingServer.ViewportSetUpdateMode(view, ViewportUpdateMode.Once); RenderingServer.ForceDraw(); Screen(8, 8, Colors.Red);
            Check(RenderingServer.ViewportGetUpdateMode(view) == ViewportUpdateMode.Disabled, "Attached Once transitions only after target draw.");
            RenderingServer.ViewportSetUpdateMode(view, ViewportUpdateMode.WhenVisible);
            RenderingServer.ViewportAttachToScreen(view); Draw(item, Colors.Green); RenderingServer.ForceDraw(); Screen(8, 8, Colors.Red);
            using (var pixels = RenderingServer.Texture2DGet(texture)!) Pixel(pixels, 8, 8, Colors.Red);
            RenderingServer.ViewportSetUpdateMode(view, ViewportUpdateMode.Always); RenderingServer.ForceDraw(); Screen(8, 8, Colors.Green); Screen(40, 8, Colors.Black);
            RenderingServer.ViewportAttachToScreen(view, screen: -1); Draw(item, Colors.Red); RenderingServer.ForceDraw(); Screen(8, 8, Colors.Green);
            RenderingServer.ViewportSetUpdateMode(view, ViewportUpdateMode.WhenParentVisible);
            RenderingServer.ViewportAttachToScreen(view, new(-16, 0, 32, 32));
            RenderingServer.CanvasItemClear(item); RenderingServer.CanvasItemAddRect(item, new(0, 0, 16, 32), Colors.Red); RenderingServer.CanvasItemAddRect(item, new(16, 0, 16, 32), Colors.Blue);
            RenderingServer.ForceDraw(); Screen(8, 8, Colors.Blue); Screen(24, 8, Colors.Black);
            RenderingServer.ViewportAttachToScreen(view, new(.25f, .25f, 31.5f, 31.5f));
            RenderingServer.ForceDraw(); Screen(8, 8, Colors.Red); Screen(24, 8, Colors.Blue);
            RenderingServer.ViewportSetUpdateMode(view, ViewportUpdateMode.Once); RenderingServer.ViewportSetSize(view, 1, 32);
            RenderingServer.ForceDraw(); Screen(8, 8, Colors.Red);
            Check(RenderingServer.ViewportGetUpdateMode(view) == ViewportUpdateMode.Once, "Suspended attached targets preserve Once.");
            RenderingServer.ViewportSetSize(view, 32, 32); RenderingServer.ForceDraw();
            RenderingServer.ViewportSetUpdateMode(view, ViewportUpdateMode.WhenVisible);
            RenderingServer.ViewportSetTransparentBackground(view, true); Draw(item, new(1, 0, 0, .5f));
            RenderingServer.ViewportAttachToScreen(view, new(0, 0, 32, 32)); RenderingServer.ForceDraw();
            using (var pixels = RenderingServer.Texture2DGet(texture)!)
            { var expected = pixels.GetPixel(8, 8); Screen(8, 8, hdr ? expected.LinearToSRGB() : expected); }
            RenderingServer.ViewportSetTransparentBackground(view, false);
            RenderingServer.ViewportAttachToScreen(view, new(0, 0, 64, 48));
            RenderingServer.ViewportSetActive(other, false); RenderingServer.ViewportSetActive(other, true);
            Draw(item, Colors.Red); RenderingServer.ViewportAttachToScreen(other, new(32, 0, 32, 32));
            RenderingServer.ViewportSetDisable2D(other, true); RenderingServer.ForceDraw(); Screen(8, 8, Colors.Red); Screen(40, 8, Colors.Blue);
            RenderingServer.ViewportSetActive(view, false); RenderingServer.ViewportSetActive(view, true); RenderingServer.ForceDraw(); Screen(40, 8, Colors.Red);
            RenderingServer.ViewportSetActive(other, true); RenderingServer.ForceDraw(); Screen(40, 8, Colors.Red);
            RenderingServer.ViewportSetActive(other, false); RenderingServer.ViewportSetActive(other, true); RenderingServer.ForceDraw(); Screen(40, 8, Colors.Blue);
            RenderingServer.ViewportSetParentViewport(other, view); RenderingServer.ForceDraw(); Screen(40, 8, Colors.Red);
            RenderingServer.ViewportAttachToScreen(root, new(0, 0, 64, 48)); RenderingServer.ViewportSetParentViewport(view, root);
            RenderingServer.ForceDraw(); Screen(8, 8, Colors.Blue);
            RenderingServer.ViewportAttachToScreen(root, screen: -1); RenderingServer.ViewportAttachToScreen(other, screen: -1);
            RenderingServer.ViewportSetParentViewport(view, default); RenderingServer.ViewportSetUpdateMode(view, ViewportUpdateMode.WhenVisible);
            Draw(item, new(.5f, .5f, .5f, 1)); RenderingServer.ForceDraw(); Screen(8, 8, new(.5f, .5f, .5f, 1));
            using (var pixels = RenderingServer.Texture2DGet(texture)!)
            { Check(pixels.PixelFormat == (hdr ? Image.Format.Rgbah : Image.Format.Rgba8), "Native source format remains independent of presentation."); Pixel(pixels, 8, 8, hdr ? new Color(.214041f, .214041f, .214041f, 1) : new(.5f, .5f, .5f, 1)); }
            RenderingServer.ViewportAttachToScreen(view, new(0, 0, 32, 32));
            var twin = new SubViewport { Size = new(16, 16) }; window.AddChild(twin);
            RenderingServer.ViewportAttachToScreen(twin.GetViewportRID(), new(48, 32, 16, 16));
            var scene = new SubViewport { Size = new(16, 16) }; window.AddChild(scene);
            RenderingServer.ViewportAttachCanvas(scene.GetViewportRID(), canvas);
            RenderingServer.ViewportAttachToScreen(scene.GetViewportRID(), new(48, 32, 16, 16)); RenderingServer.ForceDraw();
            Screen(56, 40, new(.5f, .5f, .5f, 1));
            window.MoveChild(twin, window.GetChildCount() - 1); RenderingServer.ForceDraw(); Screen(56, 40, new(.5f, .5f, .5f, 1));
            scene.Dispose(); RenderingServer.ForceDraw(); Screen(56, 40, Colors.Blue);
            twin.Dispose(); RenderingServer.ForceDraw(); Screen(56, 40, Colors.Black);
            var probe = new Guard(view); window.AddChild(probe);
            RenderingServer.ViewportSetUpdateMode(root, ViewportUpdateMode.Always); RenderingServer.ForceDraw(); Check(probe.Checked, "Capture mutation guard executes."); probe.Dispose();
            RenderingServer.ViewportSetUpdateMode(root, ViewportUpdateMode.WhenVisible);
            Action pre = () => RenderingServer.ViewportAttachToScreen(view, new(0, 0, 64, 48));
            RenderingServer.FramePreDraw += pre; RenderingServer.ForceDraw(); RenderingServer.FramePreDraw -= pre;
            for (var i = 0; i < 8; i++) RenderingServer.ForceDraw();
            var bytes = GC.GetAllocatedBytesForCurrentThread(); for (var i = 0; i < 64; i++) RenderingServer.ForceDraw(); WarmBytes = GC.GetAllocatedBytesForCurrentThread() - bytes;
            Check(WarmBytes == 0, $"Prepared attached presentation allocates {WarmBytes} owner-thread bytes.");
            RenderingServer.FreeRID(view); Freed = view; RenderingServer.ForceDraw();
            RenderingServer.FreeRID(other); RenderingServer.FreeRID(item); RenderingServer.FreeRID(canvas);
            Completed = true; Tree!.Quit();
        }
    }
    private sealed class Guard(RID view) : Entity
    {
        internal bool Checked;
        protected override void OnDraw() { Reject<InvalidOperationException>(() => RenderingServer.ViewportAttachToScreen(view)); Checked = true; }
    }
    private static void Draw(RID item, Color color)
    { RenderingServer.CanvasItemClear(item); RenderingServer.CanvasItemAddRect(item, new(0, 0, 32, 32), color); }
    private static void Screen(int x, int y, Color expected) { using var pixels = RenderingServer.Service!.Readback(); Pixel(pixels, x, y, expected); }
    private static void Pixel(Image image, int x, int y, Color expected)
    { var actual = image.GetPixel(x, y); Check(Math.Abs(actual.R - expected.R) < .02 && Math.Abs(actual.G - expected.G) < .02 && Math.Abs(actual.B - expected.B) < .02 && Math.Abs(actual.A - expected.A) < .02, $"({x},{y}) expected {expected}, got {actual}."); }
}
