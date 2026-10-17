using Electron2D;
using static TextureArrayTests;

internal static partial class LinkedSceneTests
{
    internal static void RunNative(string backend)
    {
        var folder = At(System.IO.Path.GetTempPath(), "electron2d-linked-render-" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(folder);
        try
        {
            Child("produce", folder); Register(); ProjectSettings.Set(ProjectSettings.RenderingMethod, backend); ProjectSettings.Set(ProjectSettings.RenderingFallback, false);
            using var scene = ResourceLoader.Load<PackedScene>(At(folder, "relative.e2dscene"), ResourceLoader.CacheMode.IgnoreDeep);
            var root = scene.Instantiate(); var one = (Probe)root.GetNode("One"); var two = (Probe)root.GetNode("Two"); var visual = (Sprite)one.GetNode("Visual"); var receiver = (Probe)one.GetNode("Receiver"); var texture = visual.Texture!;
            Check(texture.GetRID() == one.PrimedTextureRID && texture.GetLocalScene() == one && ((Sprite)two.GetNode("Visual")).Texture!.GetLocalScene() == two, "Native overrides retain the nested resource identity and association established before the overlay.");
            var window = new Window { Size = new(72, 24), Title = "Linked scene runtime" }; window.AddChild(root); using var target = window.GetTexture();
            var frames = 0; var calls = 0; long before = 0, bytes = 0;
            window.Ready += _ =>
            {
                Check(RenderingServer.GetCurrentRenderingMethod() == backend, "Requested linked-scene renderer selected without fallback."); RenderingServer.SetDefaultClearColor(Colors.Green);
                window.Tree!.ProcessFrameStarted += _ =>
                {
                    if (frames < 16) return;
                    before = GC.GetAllocatedBytesForCurrentThread(); visual.QueueRedraw(); one.Emit();
                };
                RenderingServer.FramePostDraw += Capture;
            };
            void Capture()
            {
                if (frames >= 16) bytes += GC.GetAllocatedBytesForCurrentThread() - before;
                frames++;
                if (frames == 4) { using var image = target.GetImage()!; Pixel(image, 5, 5, Colors.Blue); Pixel(image, 37, 5, Colors.Red); Pixel(image, 25, 5, Colors.Green); }
                if (frames == 12) { GC.Collect(); GC.WaitForPendingFinalizers(); GC.Collect(); GC.WaitForPendingFinalizers(); }
                if (frames == 16) calls = receiver.Calls;
                if (frames != 80) return;
                Check(bytes == 0 && receiver.Calls == calls + 64 * 3, $"Linked scene prepared frames: {bytes} owner-thread managed bytes, {receiver.Calls - calls} deliveries.");
                using var final = target.GetImage()!; Pixel(final, 5, 5, Colors.Blue); Pixel(final, 37, 5, Colors.Red);
                if (Environment.GetEnvironmentVariable("ELECTRON2D_CAPTURE_DIR") is { } output) { Directory.CreateDirectory(output); final.SavePNG(At(output, "linked-" + backend + ".png")); }
                RenderingServer.FramePostDraw -= Capture; window.Tree!.Quit();
            }
            Engine.Run(window);
            Check(window.IsDisposed && one.IsDisposed && texture.IsDisposed, "Native shutdown releases linked nodes and their copied local resources.");
            Console.WriteLine($"Linked scene {backend}: fresh-file pixels, retained local RID and 64 active frames at {bytes} owner-thread managed bytes passed.");
        }
        finally { Directory.Delete(folder, true); }
    }
    private static void Pixel(Image image, int x, int y, Color expected)
    {
        var actual = image.GetPixel(x, y); Check(MathF.Abs(actual.R - expected.R) < .012f && MathF.Abs(actual.G - expected.G) < .012f && MathF.Abs(actual.B - expected.B) < .012f, $"Linked pixel {x},{y}: {actual}, expected {expected}.");
    }
}
