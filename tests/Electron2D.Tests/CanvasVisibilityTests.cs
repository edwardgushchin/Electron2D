using Electron2D;
using static TextureArrayTests;

internal static class CanvasVisibilityTests
{
    internal static void RunManaged()
    {
        Throws<InvalidOperationException>(() => RenderingServer.CanvasItemSetVisibilityNotifier(default, true, default, null, null));
        Console.WriteLine("Canvas visibility unavailable-renderer boundary passed.");
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
                var window = new Window { Size = new(64, 64) }; var driver = new Driver(window); window.AddChild(driver);
                window.Ready += _ => { Check(RenderingServer.GetCurrentRenderingMethod() == backend, "Requested backend without fallback."); RenderingServer.RenderLoopEnabled = false; RenderingServer.SetDefaultClearColor(Colors.Black); };
                try { Check(Engine.Run(window) == 0 && driver.Complete, "Canvas visibility host cleanup."); Check(driver.CloseNotification is { Enabled: false, Entered: null, Exited: null }, "Renderer close releases retained callbacks."); } finally { if (!window.IsDisposed) window.Dispose(); }
                Console.WriteLine("Canvas visibility " + backend + ": pixels, multi-viewport, bounds/policies, callback lifetime/failure and warm reuse passed.");
            }
        }
        finally { ProjectSettings.Set(ProjectSettings.RenderingMethod, method); ProjectSettings.Set(ProjectSettings.RenderingFallback, fallback); }
    }
    private sealed class Driver(Window window) : Node
    {
        internal bool Complete;
        internal CanvasVisibilityNotification? CloseNotification;
        private int _entered, _exited;
        private RID _canvas, _item;
        protected override void OnReady() => ProcessEnabled = true;
        protected override void OnProcess(double delta)
        {
            _canvas = RenderingServer.CanvasCreate(); RenderingServer.ViewportAttachCanvas(window.GetViewportRID(), _canvas);
            _item = Item(new(8, 8)); RenderingServer.CanvasItemAddRect(_item, new(0, 0, 16, 16), Colors.Red);
            RenderingServer.CanvasItemSetVisibilityNotifier(_item, true, new(0, 0, 16, 16), () =>
            {
                _entered++; RenderingServer.CanvasItemClear(_item); RenderingServer.CanvasItemAddRect(_item, new(0, 0, 16, 16), Colors.Green);
            }, () => _exited++);
            Draw(1, 0); Pixel(window.GetTexture(), 12, Colors.Red); Draw(0, 0); Pixel(window.GetTexture(), 12, Colors.Green);
            RenderingServer.CanvasItemSetTransform(_item, new(0, Vector2.One, 0, new(208, 8))); Draw(0, 1); Pixel(window.GetTexture(), 12, Colors.Black);
            var view = RenderingServer.ViewportCreate(); RenderingServer.ViewportSetSize(view, 32, 32); RenderingServer.ViewportSetActive(view, true);
            RenderingServer.ViewportAttachCanvas(view, _canvas); RenderingServer.ViewportSetCanvasTransform(view, _canvas, new(0, Vector2.One, 0, new(-200, 0)));
            RenderingServer.ViewportSetUpdateMode(view, ViewportUpdateMode.Always); Draw(1, 0);
            var viewTexture = RenderingTextureRegistry.Resolve(RenderingServer.ViewportGetTexture(view)); Pixel(viewTexture, 12, Colors.Green);
            RenderingServer.CanvasItemSetTransform(_item, new(0, Vector2.One, 0, new(8, 8))); Draw(0, 0);
            RenderingServer.CanvasItemSetTransform(_item, new(0, Vector2.One, 0, new(208, 8))); Draw(0, 0);
            RenderingServer.ViewportSetActive(view, false); Draw(0, 1);
            RenderingServer.ViewportSetActive(view, true); Draw(1, 0);
            RenderingServer.ViewportSetDisable2D(view, true); Draw(0, 1);
            RenderingServer.ViewportSetDisable2D(view, false); Draw(1, 0);
            RenderingServer.ViewportSetActive(view, false); RenderingServer.CanvasItemSetTransform(_item, new(0, Vector2.One, 0, new(8, 8))); Draw(0, 0);
            RenderingServer.CanvasItemSetVisible(_item, false);
            var paused = _entered + _exited; Check(_entered + _exited == paused && RenderingCanvasItemRegistry.Resolve(_item).ServerState!.VisibilityNotifier!.OnScreen, "Mutation while rendering is stopped does not sample visibility.");
            Draw(0, 1); RenderingServer.CanvasItemSetVisible(_item, true); Draw(1, 0);
            BoundsAndPolicies(); CallbackChanges(); BorrowedLifetime();
            Throws<ArgumentException>(() => RenderingServer.CanvasItemSetVisibilityNotifier(_item, true, new(float.NaN, 0, 1, 1), null, null));
            Throws<InvalidOperationException>(() => Task.Run(() => RenderingServer.CanvasItemSetVisibilityNotifier(_item, true, default, null, null)).GetAwaiter().GetResult());
            RenderingServer.CanvasItemSetVisibilityNotifier(_item, true, new(0, 0, 16, 16), () => _entered++, () => _exited++);
            for (var i = 0; i < 32; i++) { RenderingServer.CanvasItemSetVisible(_item, (i & 1) == 0); RenderingServer.ForceDraw(false); }
            var bytes = GC.GetAllocatedBytesForCurrentThread();
            for (var i = 0; i < 64; i++) { RenderingServer.CanvasItemSetVisible(_item, (i & 1) == 0); RenderingServer.ForceDraw(false); }
            var allocated = GC.GetAllocatedBytesForCurrentThread() - bytes;
            Check(allocated == 0, $"Warmed raw visibility transitions and full render allocated {allocated} owner bytes.");
            RenderingServer.CanvasItemSetVisibilityNotifier(_item, false, new(float.NaN, 0, 0, 0), null, null);
            var before = _entered + _exited; RenderingServer.CanvasItemSetVisible(_item, true); RenderingServer.ForceDraw(false); Check(_entered + _exited == before, "Disable clears silently and ignores unused bounds.");
            RenderingServer.FreeRID(_item); Throws<ArgumentException>(() => RenderingServer.CanvasItemSetVisibilityNotifier(_item, true, default, null, null));
            RenderingServer.FreeRID(view); RenderingServer.FreeRID(_canvas);
            var closing = RenderingServer.CanvasItemCreate(); RenderingServer.CanvasItemSetVisibilityNotifier(closing, true, new(0, 0, 1, 1), () => _entered++, () => _exited++);
            CloseNotification = RenderingCanvasItemRegistry.Resolve(closing).ServerState!.VisibilityNotifier;
            Complete = true; Tree!.Quit();
        }
        private RID Item(Vector2 position)
        {
            var item = RenderingServer.CanvasItemCreate(); RenderingServer.CanvasItemSetParent(item, _canvas);
            RenderingServer.CanvasItemSetTransform(item, new(0, Vector2.One, 0, position)); return item;
        }
        private void Draw(int entered, int exited)
        {
            var a = _entered; var b = _exited; RenderingServer.ForceDraw(false);
            Check(_entered == a + entered && _exited == b + exited, "One committed transition across all submitted viewports.");
        }
        private void BoundsAndPolicies()
        {
            var parent = Item(new(0, 32)); var child = RenderingServer.CanvasItemCreate(); RenderingServer.CanvasItemSetParent(child, parent);
            RenderingServer.CanvasItemSetCustomRect(parent, true, new(0, 0, 8, 8)); RenderingServer.CanvasItemSetClip(parent, true);
            RenderingServer.CanvasItemSetTransform(child, new(0, Vector2.One, 0, new(12, 0)));
            var enter = 0; var exit = 0; RenderingServer.CanvasItemSetVisibilityNotifier(child, true, new(4, 4, -4, -4), () => enter++, () => exit++);
            RenderingServer.ForceDraw(false); Check(enter == 0, "Notifier-only child outside parent clip stays invisible.");
            RenderingServer.CanvasItemSetClip(parent, false); RenderingServer.ForceDraw(false); Check(enter == 1, "No drawing commands needed; finite negative bounds normalize.");
            RenderingServer.CanvasItemSetVisible(parent, false); RenderingServer.ForceDraw(false); Check(exit == 1, "Parent visibility applies.");
            RenderingServer.CanvasItemSetVisible(parent, true); RenderingServer.CanvasItemSetModulate(parent, new(1, 1, 1, .001f)); RenderingServer.ForceDraw(false); Check(enter == 1, "Inherited alpha threshold applies.");
            RenderingServer.CanvasItemSetModulate(parent, Colors.White); RenderingServer.ForceDraw(false); Check(enter == 2, "Parent opacity restores sampling.");
            RenderingServer.CanvasItemSetVisibilityLayer(child, 0); RenderingServer.ForceDraw(false); Check(exit == 2, "Viewport cull layers apply.");
            RenderingServer.CanvasItemSetVisibilityLayer(child, 1); RenderingServer.ForceDraw(false); Check(enter == 3, "Restored layer enters once.");
            RenderingServer.CanvasItemSetCustomRect(child, true, new(1000, 1000, 1, 1)); RenderingServer.ForceDraw(false); Check(enter == 3 && exit == 2, "Notification bounds extend custom culling bounds.");
            RenderingServer.CanvasItemSetVisibilityNotifier(child, true, new(0, 0, 1, 1), null, null); RenderingServer.ForceDraw(false);
            RenderingServer.FreeRID(child); RenderingServer.FreeRID(parent);
        }
        private void CallbackChanges()
        {
            var a = Item(new(40, 40)); var b = Item(new(40, 40)); var enteredB = 0; var sawCommitted = false;
            RenderingServer.CanvasItemSetVisibilityNotifier(a, true, new(0, 0, 4, 4), () =>
            {
                sawCommitted = RenderingCanvasItemRegistry.Resolve(b).ServerState!.VisibilityNotifier!.OnScreen;
                RenderingServer.CanvasItemSetVisibilityNotifier(b, false, default, null, null); RenderingServer.FreeRID(a);
            }, null);
            RenderingServer.CanvasItemSetVisibilityNotifier(b, true, new(0, 0, 4, 4), () => enteredB++, null);
            RenderingServer.ForceDraw(false); Check(sawCommitted && enteredB == 0, "All states commit before callbacks; disable/free cancels stale delivery.");
            RenderingServer.FreeRID(b);
            var replacement = Item(new(40, 40)); var trigger = Item(new(40, 40)); var stale = 0; var fresh = 0;
            RenderingServer.CanvasItemSetVisibilityNotifier(trigger, true, new(0, 0, 4, 4), () =>
            {
                RenderingServer.CanvasItemSetVisibilityNotifier(replacement, true, new(0, 0, 4, 4), () => fresh++, null);
                Throws<InvalidOperationException>(() => RenderingServer.ForceDraw(false));
            }, null);
            RenderingServer.CanvasItemSetVisibilityNotifier(replacement, true, new(0, 0, 4, 4), () => stale++, null);
            RenderingServer.ForceDraw(false); Check(stale == 0 && fresh == 0, "Replacement cancels queued delivery without inventing a transition.");
            RenderingServer.CanvasItemSetVisible(replacement, false); RenderingServer.ForceDraw(false);
            RenderingServer.CanvasItemSetVisible(replacement, true); RenderingServer.ForceDraw(false); Check(fresh == 1, "Replacement receives the next actual transition.");
            RenderingServer.FreeRID(trigger); RenderingServer.FreeRID(replacement);
            var c = Item(new(40, 40)); var d = Item(new(40, 40)); var later = 0; var post = 0; var completion = 0;
            RenderingServer.CanvasItemSetVisibilityNotifier(c, true, new(0, 0, 4, 4), static () => throw new ArithmeticException(), null);
            RenderingServer.CanvasItemSetVisibilityNotifier(d, true, new(0, 0, 4, 4), () => later++, null);
            Action drawn = () => post++; RenderingServer.FramePostDraw += drawn;
            RenderingServer.RequestFrameDrawnCallback(() => completion++);
            try { Throws<ArithmeticException>(() => RenderingServer.ForceDraw(false)); Check(later == 1 && post == 1 && completion == 1, "Later visibility/readback/frame completion continues after callback failure."); }
            finally { RenderingServer.FramePostDraw -= drawn; }
            RenderingServer.ForceDraw(false); Check(later == 1, "Committed transitions are never replayed.");
            RenderingServer.FreeRID(c); RenderingServer.FreeRID(d);
        }
        private void BorrowedLifetime()
        {
            var node = new Sprite { Name = "Borrowed", Position = new(20, 20) }; window.AddChild(node); var enters = 0; var exits = 0;
            RenderingServer.CanvasItemSetVisibilityNotifier(node.GetCanvasItem(), true, new(0, 0, 4, 4), () => enters++, () => exits++);
            var state = node.ServerState!.VisibilityNotifier!; RenderingServer.ForceDraw(false); Check(enters == 1, "Borrowed scene item supports raw notifications.");
            node.Dispose(); Check(!state.Enabled && state.Entered is null && state.Exited is null, "Node disposal releases callbacks immediately.");
            RenderingServer.ForceDraw(false); Check(exits == 0, "Borrowed disposal is silent.");
        }
        private static void Pixel(Texture texture, int x, Color expected)
        {
            using var image = texture.GetImage(); var actual = image!.GetPixel(x, 12);
            Check(Math.Abs(actual.R - expected.R) < .02 && Math.Abs(actual.G - expected.G) < .02 && Math.Abs(actual.B - expected.B) < .02, "Callback mutation reaches real canvas pixels.");
        }
    }
    private static void Throws<T>(Action action) where T : Exception { try { action(); } catch (T) { return; } throw new InvalidOperationException("Expected " + typeof(T).Name); }
}
