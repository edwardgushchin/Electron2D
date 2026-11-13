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
            using var probe = new Probe(window, scene, enabled);
            window.AddChild(scene);
            window.AddChild(probe);
            window.Ready += _ =>
            {
                RenderingServer.SetDefaultClearColor(Colors.Black);
                RenderingServer.RenderLoopEnabled = false;
                ProjectSettings.SetFeatureOverride(ProjectSettings.DebugRedrawColor, Feature, Colors.White);
            };
            Check(Engine.Run(window) == 0 && probe.Complete && !RenderingServer.IsAvailable, "Native run retired.");
            Console.WriteLine($"Canvas redraw {backend}/{profile}: pixels, fading, re-recording, RID and cleanup passed.");
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

    private sealed class Probe(Window window, Swatch scene, bool enabled) : Node
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
            VerifyAccumulatingTarget();
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

        private static void VerifyAccumulatingTarget()
        {
            var view = RenderingServer.ViewportCreate();
            var canvas = RenderingServer.CanvasCreate();
            var item = RenderingServer.CanvasItemCreate();
            try
            {
                RenderingServer.ViewportSetSize(view, 16, 16);
                RenderingServer.ViewportSetActive(view, true);
                RenderingServer.ViewportSetUpdateMode(view, ViewportUpdateMode.Always);
                RenderingServer.ViewportSetClearMode(view, ViewportClearMode.Never);
                RenderingServer.ViewportAttachCanvas(view, canvas);
                RenderingServer.CanvasItemSetParent(item, canvas);
                Record(item);
                RenderingServer.ForceDraw(swapBuffers: false);
                using var pixels = RenderingServer.Texture2DGet(RenderingServer.ViewportGetTexture(view));
                Check(pixels?.GetPixel(5, 5).IsEqualApprox(Colors.Red) == true, "Accumulating targets keep their original pixels.");
            }
            finally
            {
                RenderingServer.FreeRID(item);
                RenderingServer.FreeRID(canvas);
                RenderingServer.FreeRID(view);
            }
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
