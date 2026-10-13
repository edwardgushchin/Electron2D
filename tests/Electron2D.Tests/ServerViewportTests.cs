using System.Text.Json;
using Electron2D;
using static TextureArrayTests;

internal static class ServerViewportTests
{
    internal static void RunManaged()
    {
        Reject<InvalidOperationException>(() => RenderingServer.ViewportCreate());
        Reject<InvalidOperationException>(() => RenderingServer.ViewportGetUpdateMode(default));
        Reject<InvalidOperationException>(() => RenderingServer.ViewportSetActive(default, true));
        Console.WriteLine("Server viewport unavailable-host checks passed.");
    }
    internal static void RunNative()
    {
        var method = ProjectSettings.Get(ProjectSettings.RenderingMethod); var fallback = ProjectSettings.Get(ProjectSettings.RenderingFallback);
        try
        {
            ProjectSettings.Set(ProjectSettings.RenderingFallback, false);
            foreach (var backend in new[] { "gpu", "compatibility" })
            {
                ProjectSettings.Set(ProjectSettings.RenderingMethod, backend);
                var window = new Window { Size = new(64, 64) };
                var driver = new Driver(window); window.AddChild(driver);
                window.Ready += _ => { Check(RenderingServer.GetCurrentRenderingMethod() == backend, "Requested backend without fallback."); RenderingServer.RenderLoopEnabled = false; RenderingServer.SetDefaultClearColor(Colors.Black); };
                try { Check(Engine.Run(window) == 0 && driver.Completed, "RID viewport native host completed."); }
                finally { if (!window.IsDisposed) window.Dispose(); }
                Reject<ArgumentException>(() => Viewport.ResolveViewportRID(driver.Leaked));
                Reject<ArgumentException>(() => RenderingTextureRegistry.Resolve(driver.LeakedTexture));
                Console.WriteLine(JsonSerializer.Serialize(new { scenario = "server-viewports", backend, driver.Frames, driver.WarmBytes, cleanup = window.IsDisposed }));
            }
        }
        finally { ProjectSettings.Set(ProjectSettings.RenderingMethod, method); ProjectSettings.Set(ProjectSettings.RenderingFallback, fallback); }
    }
    private sealed class MutationProbe(RID view) : Entity
    {
        internal bool Checked;
        protected override void OnDraw()
        { Reject<InvalidOperationException>(() => RenderingServer.ViewportSetSize(view, 20, 20)); Reject<InvalidOperationException>(() => RenderingServer.FreeRID(view)); Reject<InvalidOperationException>(() => RenderingServer.ViewportCreate()); Checked = true; }
    }
    private sealed class Driver(Window window) : Node
    {
        internal bool Completed; internal RID Leaked, LeakedTexture; internal ulong Frames; internal long WarmBytes;
        protected override void OnReady() => ProcessEnabled = true;
        protected override void OnProcess(double delta)
        {
            if (Completed) return;
            var initial = Engine.GetFramesDrawn(); var children = window.GetChildCount();
            var view = RenderingServer.ViewportCreate(); var texture = RenderingServer.ViewportGetTexture(view);
            var canvas = RenderingServer.CanvasCreate(); var item = RenderingServer.CanvasItemCreate();
            RenderingServer.CanvasItemSetParent(item, canvas); RenderingServer.ViewportAttachCanvas(view, canvas);
            var resource = RenderingTextureRegistry.Resolve(texture);
            Check(resource.GetSize() == Vector2.Zero && RenderingServer.ViewportGetUpdateMode(view) == ViewportUpdateMode.WhenVisible && window.GetChildCount() == children, "Raw defaults and no scene attachment.");
            Reject<InvalidOperationException>(() => Task.Run(() => RenderingServer.ViewportSetActive(view, true)).GetAwaiter().GetResult());
            using (var foreign = new SubViewport()) Reject<InvalidOperationException>(() => RenderingServer.ViewportSetActive(foreign.GetViewportRID(), true));
            Reject<ArgumentOutOfRangeException>(() => RenderingServer.ViewportSetSize(view, -1, 8));
            Reject<NotSupportedException>(() => RenderingServer.ViewportSetSize(view, 8, 8, 2));
            Reject<NotSupportedException>(() => RenderingServer.ViewportSetSize(view, 16385, 8));
            Reject<ArgumentOutOfRangeException>(() => RenderingServer.ViewportSetUpdateMode(view, (ViewportUpdateMode)77));
            Reject<ArgumentOutOfRangeException>(() => RenderingServer.ViewportSetClearMode(view, (ViewportClearMode)77));
            RenderingServer.ViewportSetSize(view, 32, 32); RenderingServer.ViewportSetUpdateMode(view, ViewportUpdateMode.Once);
            Draw(item, Colors.Red); RenderingServer.ForceDraw(false); Check(RenderingServer.Texture2DGet(texture) is null && RenderingServer.ViewportGetUpdateMode(view) == ViewportUpdateMode.Once, "Inactive preserves pending Once.");
            RenderingServer.ViewportSetActive(view, true); RenderingServer.ForceDraw(false); Pixel(texture, 8, 8, Colors.Red);
            Check(RenderingServer.ViewportGetUpdateMode(view) == ViewportUpdateMode.Disabled, "Once commits Disabled after submission.");
            Draw(item, Colors.Green); RenderingServer.ForceDraw(false); Pixel(texture, 8, 8, Colors.Red);
            RenderingServer.ViewportSetUpdateMode(view, ViewportUpdateMode.Always); RenderingServer.ViewportSetActive(view, false); RenderingServer.ForceDraw(false); Pixel(texture, 8, 8, Colors.Red);
            RenderingServer.ViewportSetActive(view, true); RenderingServer.ForceDraw(false); Pixel(texture, 8, 8, Colors.Green);
            RenderingServer.ViewportSetDisable2D(view, true); RenderingServer.ForceDraw(false); Pixel(texture, 8, 8, Colors.Black);
            RenderingServer.ViewportSetTransparentBackground(view, true); RenderingServer.ForceDraw(false); Pixel(texture, 8, 8, default);
            RenderingServer.ViewportSetDisable2D(view, false); RenderingServer.ViewportSetClearMode(view, ViewportClearMode.Once); Draw(item, Colors.Blue); RenderingServer.ForceDraw(false);
            RenderingServer.CanvasItemClear(item); RenderingServer.ForceDraw(false); Pixel(texture, 8, 8, Colors.Blue);
            RenderingServer.ViewportSetClearMode(view, ViewportClearMode.Always); RenderingServer.ForceDraw(false); Pixel(texture, 8, 8, default);
            RenderingServer.ViewportSetSize(view, 40, 24); Check(resource.GetSize() == new Vector2(40, 24) && RenderingServer.ViewportGetTexture(view) == texture && RenderingServer.Texture2DGet(texture) is null, "Resize exact size, stable identity and retired image.");
            Draw(item, Colors.Red); RenderingServer.ForceDraw(false); Pixel(texture, 8, 8, Colors.Red);
            RenderingServer.ViewportSetSize(view, 0, 24); RenderingServer.ViewportSetUpdateMode(view, ViewportUpdateMode.Once); RenderingServer.ForceDraw(false);
            Check(RenderingServer.Texture2DGet(texture) is null && RenderingServer.ViewportGetUpdateMode(view) == ViewportUpdateMode.Once, "Zero axes do not consume Once.");
            RenderingServer.ViewportSetSize(view, 1, 24); RenderingServer.ForceDraw(false); Check(RenderingServer.Texture2DGet(texture) is null, "One-pixel axis suspends output.");
            RenderingServer.ViewportSetSize(view, 32, 32, 0); RenderingServer.ForceDraw(false); Check(RenderingServer.Texture2DGet(texture) is null, "Zero layers suspend output.");
            RenderingServer.ViewportSetSize(view, 32, 32); RenderingServer.ForceDraw(false); Pixel(texture, 8, 8, Colors.Red);
            RenderingServer.ViewportSetUpdateMode(view, ViewportUpdateMode.WhenVisible); Draw(item, Colors.Green); RenderingServer.ForceDraw(false); Pixel(texture, 8, 8, Colors.Red);
            var probe = new MutationProbe(view); window.AddChild(probe); RenderingServer.ForceDraw(false); Check(probe.Checked, "Capture mutation guards execute in real drawing callback."); probe.Dispose();
            var sceneView = new SubViewport { Size = new(16, 16) }; window.AddChild(sceneView);
            RenderingServer.ViewportSetSize(sceneView.GetViewportRID(), 8, 10); RenderingServer.ViewportSetClearMode(sceneView.GetViewportRID(), ViewportClearMode.Never); RenderingServer.ViewportSetTransparentBackground(sceneView.GetViewportRID(), true); RenderingServer.ViewportSetUpdateMode(sceneView.GetViewportRID(), ViewportUpdateMode.Once);
            Check(sceneView.Size == new Vector2i(8, 10) && sceneView.TransparentBG && sceneView.RenderTargetClearMode == ViewportClearMode.Never && sceneView.RenderTargetUpdateMode == ViewportUpdateMode.Once, "Server setters synchronize borrowed scene properties.");
            RenderingServer.ForceDraw(false); Check(sceneView.RenderTargetUpdateMode == ViewportUpdateMode.Disabled, "Borrowed Once transition."); sceneView.Dispose();
            var rootCanvas = RenderingServer.CanvasCreate(); var rootItem = RenderingServer.CanvasItemCreate(); RenderingServer.CanvasItemSetParent(rootItem, rootCanvas); RenderingServer.ViewportAttachCanvas(window.GetViewportRID(), rootCanvas);
            RenderingServer.CanvasItemAddTextureRect(rootItem, new(Vector2.Zero, new(32, 32)), texture); RenderingServer.ForceDraw(false); Pixel(texture, 8, 8, Colors.Green); Pixel(window.GetTexture().GetRID(), 8, 8, Colors.Green);
            RenderingServer.CanvasItemClear(rootItem);
            RenderingServer.ViewportSetParentViewport(view, window.GetViewportRID()); RenderingServer.ViewportSetUpdateMode(view, ViewportUpdateMode.WhenParentVisible); Draw(item, Colors.Blue); RenderingServer.ForceDraw(false); Pixel(texture, 8, 8, Colors.Blue);
            Reject<ArgumentException>(() => RenderingServer.ViewportSetParentViewport(window.GetViewportRID(), view));
            RenderingServer.ViewportSetParentViewport(view, default); Draw(item, Colors.Red); RenderingServer.ForceDraw(false); Pixel(texture, 8, 8, Colors.Blue);
            RenderingServer.CanvasItemAddTextureRect(rootItem, new(Vector2.Zero, new(32, 32)), texture); RenderingServer.ForceDraw(false); Pixel(texture, 8, 8, Colors.Blue); RenderingServer.CanvasItemClear(rootItem);
            var child = RenderingServer.ViewportCreate(); RenderingServer.ViewportSetSize(child, 16, 16); RenderingServer.ViewportSetActive(child, true); RenderingServer.ViewportSetParentViewport(child, view); RenderingServer.ViewportSetUpdateMode(child, ViewportUpdateMode.WhenParentVisible); RenderingServer.ViewportAttachCanvas(child, canvas);
            RenderingServer.ViewportSetUpdateMode(view, ViewportUpdateMode.Always); RenderingServer.ForceDraw(false); Pixel(RenderingServer.ViewportGetTexture(child), 8, 8, Colors.Red);
            RenderingServer.ViewportSetSize(view, 0, 32); Draw(item, Colors.Blue); RenderingServer.ForceDraw(false); Pixel(RenderingServer.ViewportGetTexture(child), 8, 8, Colors.Red);
            RenderingServer.ViewportSetSize(view, 32, 32); RenderingServer.ForceDraw(false); Pixel(RenderingServer.ViewportGetTexture(child), 8, 8, Colors.Blue);
            window.Visible = false; Draw(item, Colors.Green); RenderingServer.ForceDraw(false); Pixel(texture, 8, 8, Colors.Green); window.Visible = true; Draw(item, Colors.Red);
            Reject<ArgumentException>(() => RenderingServer.ViewportSetParentViewport(view, child));
            RenderingServer.ViewportSetCanvasTransform(view, canvas, new Transform(0, new Vector2(16, 0))); RenderingServer.ForceDraw(false); Pixel(texture, 8, 8, default); Pixel(texture, 24, 8, Colors.Red);
            RenderingServer.ViewportSetCanvasTransform(view, canvas, Transform.Identity);
            RenderingServer.ViewportRemoveCanvas(view, canvas); RenderingServer.ForceDraw(false); Pixel(texture, 8, 8, default); RenderingServer.ViewportAttachCanvas(view, canvas);
            RenderingServer.ForceDraw(false); Pixel(texture, 8, 8, Colors.Red);
            RenderingServer.CanvasItemClear(item); RenderingServer.CanvasItemAddTextureRect(item, new(Vector2.Zero, new(32, 32)), texture); RenderingServer.CanvasItemAddRect(item, new(Vector2.Zero, new(4, 4)), Colors.Blue);
            RenderingServer.ForceDraw(false); Pixel(texture, 8, 8, Colors.Red); Pixel(texture, 2, 2, Colors.Blue);
            Draw(item, Colors.Red);
            for (var i = 0; i < 8; i++) RenderingServer.ForceDraw(false);
            var bytes = GC.GetAllocatedBytesForCurrentThread(); for (var i = 0; i < 64; i++) RenderingServer.ForceDraw(false); WarmBytes = GC.GetAllocatedBytesForCurrentThread() - bytes;
            Check(WarmBytes == 0, "Prepared owner render frames allocate no managed bytes.");
            Reject<InvalidOperationException>(() => RenderingServer.FreeRID(window.GetViewportRID()));
            RenderingServer.FreeRID(view); Reject<ArgumentException>(() => RenderingServer.ViewportGetUpdateMode(view));
            Check(resource.IsDisposed, "Freed viewport disposes its borrowed texture view.");
            Reject<ArgumentException>(() => RenderingServer.Texture2DGet(texture));
            RenderingServer.ForceDraw(false); RenderingServer.FreeRID(child); RenderingServer.FreeRID(item); RenderingServer.FreeRID(canvas); RenderingServer.FreeRID(rootItem); RenderingServer.FreeRID(rootCanvas);
            Leaked = RenderingServer.ViewportCreate(); LeakedTexture = RenderingServer.ViewportGetTexture(Leaked);
            Frames = Engine.GetFramesDrawn() - initial; Completed = true; Tree!.Quit();
        }
        private static void Draw(RID item, Color color) { RenderingServer.CanvasItemClear(item); RenderingServer.CanvasItemAddRect(item, new(Vector2.Zero, new(16, 16)), color); }
        private static void Pixel(RID texture, int x, int y, Color expected)
        { using var image = RenderingServer.Texture2DGet(texture); Check(image is not null, "Completed viewport image exists."); var actual = image!.GetPixel(x, y); Check(Math.Abs(actual.R - expected.R) < .02 && Math.Abs(actual.G - expected.G) < .02 && Math.Abs(actual.B - expected.B) < .02 && Math.Abs(actual.A - expected.A) < .02, $"Viewport pixel ({x},{y}) expected {expected}, got {actual}."); }
    }
}
