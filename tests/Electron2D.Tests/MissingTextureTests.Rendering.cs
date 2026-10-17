using Electron2D;
using static TextureArrayTests;

internal static partial class MissingTextureTests
{
    internal static void RunNative(string backend)
    {
        var folder = At(System.IO.Path.GetTempPath(), "electron2d-missing-texture-native-" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(folder);
        try
        {
            Child("produce", folder); Preserve(folder); Render(folder, false, backend); Child("restore-native-" + backend, folder);
        }
        finally { ResourceLoader.SetAbortOnMissingResources(true); Directory.Delete(folder, true); }
    }

    private static void Render(string folder, bool restored, string backend)
    {
        ProjectSettings.Set(ProjectSettings.RenderingMethod, backend); ProjectSettings.Set(ProjectSettings.RenderingFallback, false); ResourceLoader.SetAbortOnMissingResources(restored);
        using var scene = ResourceLoader.Load<PackedScene>(At(folder, "saved.e2dscene"), ResourceLoader.CacheMode.IgnoreDeep);
        var instance = scene.Instantiate(); var first = (Sprite)instance.GetNode("First"); var second = (Sprite)instance.GetNode("Second"); var texture = first.Texture!;
        Check(restored ? texture is RestoredTexture : texture is PlaceholderTexture, "Native consumer has the expected restored or missing texture type.");
        Check(texture == second.Texture && texture.GetSize() == new Vector2(24, 12), "Native consumer retains logical size and scene-local aliases.");
        var window = new Window { Size = new(72, 24), Title = "Missing texture recovery" }; window.AddChild(instance);
        using var target = window.GetTexture(); var frames = 0; long before = 0, bytes = 0;
        window.Ready += _ =>
        {
            Check(RenderingServer.GetCurrentRenderingMethod() == backend, "Recovery renderer selected without fallback."); RenderingServer.SetDefaultClearColor(Colors.Green);
            window.Tree!.ProcessFrameStarted += _ =>
            {
                if (frames < 16) return;
                before = GC.GetAllocatedBytesForCurrentThread();
                if (texture is PlaceholderTexture placeholder) placeholder.Size = new((frames & 1) == 0 ? 24 : 12, 12);
                else { first.QueueRedraw(); second.QueueRedraw(); }
            };
            RenderingServer.FramePostDraw += Capture;
        };
        void Capture()
        {
            if (frames >= 16) bytes += GC.GetAllocatedBytesForCurrentThread() - before;
            frames++;
            if (frames == 4)
            {
                using var image = target.GetImage()!; Pixel(image, 5, 5, restored ? Colors.Blue : Colors.Magenta); Pixel(image, 11, 5, restored ? Colors.Blue : Colors.Black); Pixel(image, 37, 5, restored ? Colors.Blue : Colors.Magenta);
            }
            if (frames == 12) { GC.Collect(); GC.WaitForPendingFinalizers(); GC.Collect(); GC.WaitForPendingFinalizers(); }
            if (frames != 80) return;
            Check(bytes == 0, $"Prepared missing/restored texture frame allocation: {bytes} owner-thread managed bytes.");
            using var final = target.GetImage()!; Pixel(final, 5, 5, restored ? Colors.Blue : Colors.Magenta);
            if (!restored) Pixel(final, 17, 5, Colors.Green);
            if (Environment.GetEnvironmentVariable("ELECTRON2D_CAPTURE_DIR") is { } output) { Directory.CreateDirectory(output); final.SavePNG(At(output, (restored ? "restored-" : "missing-") + backend + ".png")); }
            RenderingServer.FramePostDraw -= Capture; window.Tree!.Quit();
        }
        Engine.Run(window);
        Check(window.IsDisposed && texture.IsDisposed, "Native shutdown releases scene-local decoded resources.");
        Console.WriteLine($"Missing texture {backend}, restored={restored}: actual pixels and 64 active frames at {bytes} owner-thread managed bytes passed.");
    }
    private static void Pixel(Image image, int x, int y, Color expected)
    {
        var actual = image.GetPixel(x, y); Check(MathF.Abs(actual.R - expected.R) < .012f && MathF.Abs(actual.G - expected.G) < .012f && MathF.Abs(actual.B - expected.B) < .012f, $"Recovery pixel {x},{y}: {actual}, expected {expected}.");
    }
}
