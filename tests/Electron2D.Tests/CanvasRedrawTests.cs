using Electron2D;

internal static class CanvasRedrawTests
{
    private const string Feature = "test_canvas_redraw";

    internal static void Run(string profile)
    {
        using var project = DirAccess.CreateTemp();
        using var user = DirAccess.CreateTemp();
        using (var settings = new ProjectSettingsRegistry(project.GetCurrentDir(), user.GetCurrentDir()))
        {
            Check(settings.Get(ProjectSettings.DebugRedrawTime) == 1d &&
                settings.Get(ProjectSettings.DebugRedrawColor) == new Color(1f, .2f, .2f, .5f), "Typed defaults.");
            foreach (var invalid in new[] { -1d, double.NaN, double.PositiveInfinity })
            {
                try { settings.Set(ProjectSettings.DebugRedrawTime, invalid); throw new InvalidOperationException("Invalid duration accepted."); }
                catch (ArgumentException) { }
            }
            settings.SetFeatureOverride(ProjectSettings.DebugRedrawColor, Feature, Colors.Green);
            settings.Set(ProjectSettings.DebugRedrawTime, 2d);
            settings.Save();
        }
        using (var settings = new ProjectSettingsRegistry(project.GetCurrentDir(), user.GetCurrentDir()))
        {
            settings.Load();
            Check(settings.Get(ProjectSettings.DebugRedrawTime) == 2d &&
                settings.GetWithOverride(ProjectSettings.DebugRedrawColor, [Feature]) == Colors.Green, "Typed values round-trip.");
        }

        var enabled = profile == "enabled";
        ProjectSettings.Set(ProjectSettings.DebugRedrawColor, Colors.Green);
        ProjectSettings.Set(ProjectSettings.DebugRedrawTime, profile == "zero" ? 0d : 2d);
        ProjectSettings.AddCustomFeature(Feature);
        ProjectSettings.Set(ProjectSettings.RenderingFallback, false);
        foreach (var backend in new[] { "gpu", "compatibility" })
        {
            ProjectSettings.SetFeatureOverride(ProjectSettings.DebugRedrawColor, Feature, Colors.Blue);
            ProjectSettings.Set(ProjectSettings.RenderingMethod, backend);
            using var window = new Window { Size = new(64, 48) };
            using var scene = new Swatch { Position = new(4, 4) };
            using var probe = new Probe(window, scene, enabled, backend == "gpu");
            window.AddChild(scene);
            window.AddChild(probe);
            window.Ready += _ =>
            {
                RenderingServer.SetDefaultClearColor(Colors.Black);
                RenderingServer.RenderLoopEnabled = false;
                ProjectSettings.SetFeatureOverride(ProjectSettings.DebugRedrawColor, Feature, Colors.White);
            };
            Check(Engine.Run(window) == 0 && probe.Complete && !RenderingServer.IsAvailable, "Native run retired.");
            Console.WriteLine($"Canvas redraw {backend}/{profile}: pixels, fading, viewport history and cleanup passed.");
        }
    }

    private sealed class Swatch : Entity
    {
        internal int Recordings;
        protected override void OnDraw()
        {
            Recordings++;
            DrawRect(new(0, 0, 12, 12), Colors.Red);
        }
    }

    private sealed class Probe(Window window, Swatch scene, bool enabled, bool gpu) : Node
    {
        internal bool Complete;

        protected override void OnReady() => ProcessEnabled = true;

        protected override void OnProcess(double delta)
        {
            if (Complete) return;
            var canvas = RenderingServer.CanvasCreate();
            var item = RenderingServer.CanvasItemCreate();
            var clip = RenderingServer.CanvasItemCreate();
            RenderingServer.ViewportAttachCanvas(window.GetViewportRID(), canvas);
            RenderingServer.CanvasItemSetParent(clip, canvas);
            RenderingServer.CanvasItemSetCustomRect(clip, true, new(28, 4, 8, 12));
            RenderingServer.CanvasItemSetClip(clip, true);
            RenderingServer.CanvasItemSetParent(item, clip);
            RenderingServer.CanvasItemSetTransform(item, new Transform(0, new Vector2(28, 4)));
            Record(item);
            RenderingServer.ForceDraw();
            Pixel(6, enabled ? Colors.Blue : Colors.Red);
            Pixel(30, enabled ? Colors.Blue : Colors.Red);
            Pixel(23, Colors.Black);
            Pixel(38, Colors.Black);
            if (enabled) Check(RenderingServer.HasChanged(), "The live highlight schedules another frame.");
            RenderingServer.ForceDraw(frameStep: 1d);
            Pixel(6, enabled ? new Color(.5f, 0, .5f) : Colors.Red);
            Pixel(30, enabled ? new Color(.5f, 0, .5f) : Colors.Red);
            RenderingServer.ForceDraw(frameStep: 1d);
            Pixel(6, Colors.Red);
            Pixel(30, Colors.Red);
            Check(scene.Recordings == 1 && !RenderingServer.HasChanged(), "Expiry stops frame scheduling without recording commands again.");

            scene.QueueRedraw();
            RenderingServer.ForceDraw();
            Pixel(6, enabled ? Colors.Blue : Colors.Red);
            Pixel(30, Colors.Red);
            Check(scene.Recordings == 2, "Explicit redraw re-records the scene item only.");
            Record(item);
            RenderingServer.ForceDraw();
            Pixel(30, enabled ? Colors.Blue : Colors.Red);
            RenderingServer.FreeRID(item);
            RenderingServer.ForceDraw();
            Pixel(30, Colors.Black);

            for (var i = 0; i < 24; i++) Frame(scene);
            var before = GC.GetAllocatedBytesForCurrentThread();
            for (var i = 0; i < 64; i++) Frame(scene);
            var bytes = GC.GetAllocatedBytesForCurrentThread() - before;
            Check(bytes == 0, $"Warmed redraws allocated {bytes} owner-thread bytes.");
            before = GC.GetAllocatedBytesForCurrentThread();
            for (var i = 0; i < 64; i++) RenderingServer.ForceDraw(swapBuffers: false);
            bytes = GC.GetAllocatedBytesForCurrentThread() - before;
            Check(bytes == 0, $"Warmed retained frames allocated {bytes} owner-thread bytes.");
            VerifyAccumulatingTarget(ViewportClearMode.Never);
            VerifyAccumulatingTarget(ViewportClearMode.Once);
            if (gpu) VerifyAccumulatingTarget(ViewportClearMode.Once, multisampledHDR: true);
            VerifyKeptBackground();
            Complete = true;
            Tree!.Quit();
        }

        private static void Record(RID item)
        {
            RenderingServer.CanvasItemClear(item);
            RenderingServer.CanvasItemAddRect(item, new(0, 0, 12, 12), Colors.Red);
        }

        private static void Frame(Swatch scene)
        {
            scene.QueueRedraw();
            RenderingServer.ForceDraw(swapBuffers: false);
        }

        private void VerifyAccumulatingTarget(ViewportClearMode clear, bool multisampledHDR = false)
        {
            var view = RenderingServer.ViewportCreate();
            var canvas = RenderingServer.CanvasCreate();
            var item = RenderingServer.CanvasItemCreate();
            try
            {
                RenderingServer.ViewportSetSize(view, 32, 24);
                RenderingServer.ViewportSetActive(view, true);
                RenderingServer.ViewportSetUpdateMode(view, ViewportUpdateMode.Always);
                RenderingServer.ViewportSetClearMode(view, clear);
                if (multisampledHDR)
                {
                    RenderingServer.ViewportSetUseHDR2D(view, true);
                    RenderingServer.ViewportSetMSAA2D(view, Viewport.MSAA.Samples4X);
                    RenderingServer.ViewportSetScreenSpaceAA(view, ScreenSpaceAAMode.FXAA);
                }
                RenderingServer.ViewportAttachCanvas(view, canvas);
                RenderingServer.CanvasItemSetParent(item, canvas);
                var texture = RenderingServer.ViewportGetTexture(view);
                Record(item);
                RenderingServer.ForceDraw(swapBuffers: false);
                TargetPixel(texture, 5, enabled ? Colors.Blue : Colors.Red);
                RenderingServer.CanvasItemSetTransform(item, new Transform(0, new Vector2(16, 0)));
                RenderingServer.ForceDraw(swapBuffers: false, frameStep: 1d);
                TargetPixel(texture, 5, Colors.Red);
                TargetPixel(texture, 20, enabled ? new Color(.5f, 0, .5f) : Colors.Red);
                if (clear == ViewportClearMode.Never)
                {
                    for (var i = 0; i < 24; i++) RecordAndDraw(item);
                    var before = GC.GetAllocatedBytesForCurrentThread();
                    for (var i = 0; i < 64; i++) RecordAndDraw(item);
                    var bytes = GC.GetAllocatedBytesForCurrentThread() - before;
                    Check(bytes == 0, $"Warmed accumulating redraws allocated {bytes} owner-thread bytes.");
                }
                RenderingServer.FreeRID(item);
                item = default;
                RenderingServer.ForceDraw(swapBuffers: false, frameStep: 1d);
                TargetPixel(texture, 5, Colors.Red);
                TargetPixel(texture, 20, Colors.Red);
                RenderingServer.ViewportSetClearMode(view, ViewportClearMode.Always);
                RenderingServer.ForceDraw(swapBuffers: false);
                TargetPixel(texture, 5, Colors.Black);
                TargetPixel(texture, 20, Colors.Black);
            }
            finally
            {
                if (item.IsValid()) RenderingServer.FreeRID(item);
                RenderingServer.FreeRID(canvas);
                RenderingServer.FreeRID(view);
            }
        }

        private static void RecordAndDraw(RID item)
        {
            Record(item);
            RenderingServer.ForceDraw(swapBuffers: false);
        }

        private void VerifyKeptBackground()
        {
            using var environment = new CanvasEnvironment { BackgroundMode = CanvasBackgroundMode.Keep };
            using var world = new WorldEnvironment { Environment = environment };
            window.AddChild(world);
            scene.QueueRedraw();
            RenderingServer.ForceDraw(swapBuffers: false);
            Pixel(6, enabled ? Colors.Blue : Colors.Red);
            scene.Position = new(20, 4);
            RenderingServer.ForceDraw(swapBuffers: false, frameStep: 1d);
            Pixel(6, Colors.Red);
            Pixel(22, enabled ? new Color(.5f, 0, .5f) : Colors.Red);
            scene.Hide();
            RenderingServer.ForceDraw(swapBuffers: false, frameStep: 1d);
            Pixel(22, Colors.Red);
            window.RemoveChild(world);
        }

        private static void TargetPixel(RID texture, int x, Color expected)
        {
            using var image = RenderingServer.Texture2DGet(texture)!;
            var actual = image.GetPixel(x, 5);
            Check(Math.Abs(actual.R - expected.R) < .02f && Math.Abs(actual.G - expected.G) < .02f &&
                Math.Abs(actual.B - expected.B) < .02f, $"Accumulating target pixel {x}: expected {expected}, got {actual}.");
        }
        private void Pixel(int x, Color expected)
        {
            using var image = window.GetTexture().GetImage()!;
            var actual = image.GetPixel(x, 6);
            Check(Math.Abs(actual.R - expected.R) < .02f && Math.Abs(actual.G - expected.G) < .02f &&
                Math.Abs(actual.B - expected.B) < .02f, $"Canvas pixel {x}: expected {expected}, got {actual}.");
        }
    }

    private static void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
