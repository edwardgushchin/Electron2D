using Electron2D;

internal static class GUIPrimitivesRenderingTests
{
    internal static void Run(string backend)
    {
        ProjectSettings.Set(ProjectSettings.RenderingMethod, backend); ProjectSettings.Set(ProjectSettings.RenderingFallback, false);
        var path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "e2d-primitives-render-" + Guid.NewGuid() + ".e2dscene");
        try
        {
            using (var source = new Window { Size = new(180, 130) })
            using (var scene = new PackedScene())
            {
                var color = new ColorRect { Name = "Fill", Position = new(8, 8), Size = new(50, 60), Color = Colors.Red };
                var horizontal = new HSeparator { Name = "H", Position = new(8, 75), Size = new(90, 20) };
                var vertical = new VSeparator { Name = "V", Position = new(125, 8), Size = new(20, 80) };
                foreach (var child in new Control[] { color, horizontal, vertical }) { source.AddChild(child); child.Owner = source; }
                scene.Pack(source); ResourceSaver.Save(scene, path, SaverFlags.Compress | SaverFlags.SaveBigEndian);
            }
            using var packed = ResourceLoader.Load<PackedScene>(path, ResourceLoader.CacheMode.Ignore); using var window = (Window)packed.Instantiate(); packed.Dispose();
            var fill = (ColorRect)window.GetNode("Fill"); var h = (HSeparator)window.GetNode("H"); var v = (VSeparator)window.GetNode("V");
            using var hs = new StyleBoxLine { Color = Colors.Blue, Thickness = 3 }; using var vs = new StyleBoxLine { Color = Colors.Yellow, Thickness = 3, Vertical = true };
            using var target = window.GetTexture(); var frames = 0; long before = 0, allocated = 0;
            window.Ready += _ =>
            {
                RenderingServer.SetDefaultClearColor(Colors.Black);
                window.Tree!.ProcessFrameStarted += _ => { before = GC.GetAllocatedBytesForCurrentThread(); if (frames >= 4) fill.Color = (frames & 1) == 0 ? Colors.Red : Colors.Green; };
                RenderingServer.FramePostDraw += Capture;
            };
            void Capture()
            {
                if (frames >= 68) allocated += GC.GetAllocatedBytesForCurrentThread() - before;
                frames++;
                if (frames > 4)
                {
                    if (frames < 132) return;
                    Check(allocated == 0, "64 rendered primitive frame bytes=" + allocated); RenderingServer.FramePostDraw -= Capture; window.Tree!.Quit(); return;
                }
                using var image = target.GetImage() ?? throw new InvalidOperationException("Primitive readback.");
                switch (frames)
                {
                    case 1:
                        Pixel(image, 20, 20, Colors.Red); Pixel(image, 20, 85, new(.5f, .5f, .5f)); Pixel(image, 135, 20, new(.5f, .5f, .5f)); Pixel(image, 80, 20, Colors.Black);
                        fill.Color = Colors.Green; h.AddThemeStyleBoxOverride("separator", hs); v.AddThemeStyleBoxOverride("separator", vs); break;
                    case 2:
                        Pixel(image, 20, 20, Colors.Green); Pixel(image, 20, 83, Colors.Blue); Pixel(image, 133, 20, Colors.Yellow); hs.Thickness = 5; vs.Thickness = 5; break;
                    case 3:
                        Pixel(image, 20, 82, Colors.Blue); Pixel(image, 132, 20, Colors.Yellow); fill.Visible = false; h.Visible = false; v.Visible = false; break;
                    case 4:
                        Pixel(image, 20, 20, Colors.Black); Pixel(image, 20, 82, Colors.Black); Pixel(image, 132, 20, Colors.Black); fill.Visible = true; h.Visible = true; v.Visible = true; break;
                }
                if (Environment.GetEnvironmentVariable("ELECTRON2D_CAPTURE_DIR") is { } folder) { Directory.CreateDirectory(folder); image.SavePNG(System.IO.Path.Combine(folder, "primitives-" + backend + "-" + frames + ".png")); }
            }
            try { Check(Engine.Run(window) == 0 && frames == 132, "Native primitive scene completed."); }
            finally { if (RenderingServer.IsAvailable) RenderingServer.FramePostDraw -= Capture; }
            Check(!hs.IsDisposed && !vs.IsDisposed, "Window teardown does not dispose borrowed styles.");
            Console.WriteLine("GUI primitives " + backend + ": saved/loaded concrete controls, default/live color/style/thickness/visibility pixels and 64 warm rendered frames=0 managed bytes passed.");
        }
        finally { File.Delete(path); }
    }
    private static void Pixel(Image image, int x, int y, Color expected)
    {
        var actual = image.GetPixel(x, y);
        Check(Math.Abs(actual.R - expected.R) < .06f && Math.Abs(actual.G - expected.G) < .06f && Math.Abs(actual.B - expected.B) < .06f, "Pixel " + x + "," + y + " expected " + expected + " actual " + actual);
    }
    private static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
}
