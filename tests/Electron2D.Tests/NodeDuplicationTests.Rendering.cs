using Electron2D;

internal static partial class NodeDuplicationTests
{
    internal static void Render(string backend)
    {
        var folder = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "electron2d-duplicate-render-" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(folder);
        try
        {
            Process("produce", folder); Register(); ProjectSettings.Set(ProjectSettings.RenderingMethod, backend); ProjectSettings.Set(ProjectSettings.RenderingFallback, false);
            using var packed = ResourceLoader.Load<PackedScene>(System.IO.Path.Combine(folder, "copy.e2dscene"), ResourceLoader.CacheMode.IgnoreDeep);
            var original = (Actor)packed.Instantiate(); original.Position = new(2, 2); original.Modulate = Colors.Blue;
            var copy = (Actor)original.Duplicate(); copy.Position = new(28, 2); copy.Modulate = Colors.Red;
            var texture = ((Sprite)copy.GetNode("Visual")).Texture!;
            Check(texture == ((Sprite)original.GetNode("Visual")).Texture, "Native duplicate borrows the actual source texture"); var rid = texture.GetRID();
            var window = new Window { Size = new(64, 24), Title = "Node duplication" }; window.AddChild(original); window.AddChild(copy); using var target = window.GetTexture();
            var frames = 0; long before = 0, bytes = 0;
            window.Ready += _ =>
            {
                Check(RenderingServer.GetCurrentRenderingMethod() == backend, "Requested renderer without fallback"); RenderingServer.SetDefaultClearColor(Colors.Green);
                window.Tree!.ProcessFrameStarted += _ => { if (frames < 16) return; before = GC.GetAllocatedBytesForCurrentThread(); copy.QueueRedraw(); };
                RenderingServer.FramePostDraw += Capture;
            };
            void Capture()
            {
                if (frames >= 16) bytes += GC.GetAllocatedBytesForCurrentThread() - before;
                frames++;
                if (frames == 4) { using var image = target.GetImage()!; Pixel(image, 5, 5, Colors.Blue); Pixel(image, 31, 5, Colors.Red); original.QueueFree(); }
                if (frames == 10) { using var image = target.GetImage()!; Pixel(image, 5, 5, Colors.Green); Pixel(image, 31, 5, Colors.Red); Check(original.IsDisposed && !texture.IsDisposed && texture.GetRID() == rid, "Shared native texture survives original node disposal"); }
                if (frames == 12) { GC.Collect(); GC.WaitForPendingFinalizers(); GC.Collect(); GC.WaitForPendingFinalizers(); }
                if (frames != 80) return;
                Check(bytes == 0, "64 active duplicate render frames allocate zero managed owner bytes: " + bytes);
                using var final = target.GetImage()!; Pixel(final, 31, 5, Colors.Red);
                if (Environment.GetEnvironmentVariable("ELECTRON2D_CAPTURE_DIR") is { } output) { Directory.CreateDirectory(output); final.SavePNG(System.IO.Path.Combine(output, "duplicate-" + backend + ".png")); }
                RenderingServer.FramePostDraw -= Capture; window.Tree!.Quit();
            }
            Engine.Run(window); Check(copy.IsDisposed && texture.IsDisposed, "Native teardown releases last shared resource owner");
            Console.WriteLine($"Node duplication {backend}: fresh-file typed actor copies, actual shared texture pixels after source disposal and 64 full redraw frames={bytes} owner bytes passed.");
        }
        finally { Directory.Delete(folder, true); }
    }
    private static void Pixel(Image image, int x, int y, Color expected)
    {
        var color = image.GetPixel(x, y); Check(MathF.Abs(color.R - expected.R) < .015f && MathF.Abs(color.G - expected.G) < .015f && MathF.Abs(color.B - expected.B) < .015f, $"Pixel {x},{y}: {color}, expected {expected}");
    }
}
