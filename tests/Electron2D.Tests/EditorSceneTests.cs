using Electron2D;
using Electron2D.Editor;
using SDL = SDL3.SDL;
using IOPath = System.IO.Path;

internal static class EditorSceneTests
{
    internal static void Run()
    {
        using var texture = ResourceLoader.Load<ImageTexture>(IOPath.Combine(AppContext.BaseDirectory, "Assets", "logo-stacked-dark.svg"));
        Engine.MaxFPS = 60;
        ProjectSettings.Set(ProjectSettings.RenderingFallback, false);
        foreach (var backend in new[] { "gpu", "compatibility" })
        {
            ProjectSettings.Set(ProjectSettings.RenderingMethod, backend);
            var window = EditorScene.CreateWindow(texture);
            var brand = (Control)window.GetChild(0);
            var logo = (TextureRect)brand.GetChild(0);
            var caption = (Label)brand.GetChild(1);
            Check(window.Size == new Vector2i(1152, 800) && window.Title == "Electron2D", "Initial window contract.");
            Check(caption.Text == "Game engine" && caption.HorizontalAlignment == HorizontalAlignment.Center, "Centered descriptor below the wordmark.");
            var frames = 0;
            var resized = false;
            var closed = false;
            window.CloseRequested += () => closed = true;
            window.Ready += _ => RenderingServer.FramePostDraw += () =>
            {
                Check(++frames < 120, "Native resize completes within the frame budget.");
                if (frames < 3 || resized && window.Size != new Vector2i(1280, 900)) return;
                Check(window.Title == "Electron2D", "Native title.");
                Check(brand.Position.IsEqualApprox(((Vector2)window.Size - brand.Size) / 2), "Logo and descriptor remain centered together.");
                using var pixels = RenderingServer.Service!.Readback();
                Check(pixels.Size == window.Size, "Native client dimensions.");
                Pixel(pixels, 10, 10, "#241B2C");
                Pixel(pixels, (int)brand.Position.X + 243, (int)brand.Position.Y + 50, "#F2A6CC");
                Pixel(pixels, (int)brand.Position.X + 219, (int)brand.Position.Y + 70, "#3D2749");
                var output = IOPath.GetFullPath("bin/editor-smoke");
                Directory.CreateDirectory(output);
                pixels.SavePNG(IOPath.Combine(output, $"{backend}-{(resized ? "resized" : "initial")}.png"));
                if (!resized) { resized = true; window.Size = new(1280, 900); return; }
                var native = SDL.GetWindows(out var count);
                Check(count == 1 && native is { Length: 1 }, "One native root window.");
                var close = new SDL.Event { Window = new SDL.WindowEvent { Type = SDL.EventType.WindowCloseRequested, WindowID = SDL.GetWindowID(native![0]) } };
                Check(SDL.PushEvent(ref close), "Native close event accepted.");
            };
            Check(Engine.Run(window) == 0 && closed, "Close exits successfully.");
            Check(window.IsDisposed && logo.IsDisposed && caption.IsDisposed && !texture.IsDisposed && Engine.MainLoop is null && !DisplayServer.IsAvailable && !RenderingServer.IsAvailable, "Window cleanup preserves the borrowed texture.");
        }
        Console.WriteLine("Editor scene checks passed: native title/size, logo pixels, resize centering and close/cleanup on GPU and compatibility.");
    }

    private static void Pixel(Image image, int x, int y, string html)
    {
        var expected = Color.FromHTML(html);
        var actual = image.GetPixel(x, y);
        Check(Math.Abs(actual.R - expected.R) < .02f && Math.Abs(actual.G - expected.G) < .02f && Math.Abs(actual.B - expected.B) < .02f && actual.A > .99f, $"Pixel ({x}, {y}) matches {html}.");
    }

    private static void Check(bool value, string message)
    {
        if (!value) throw new InvalidOperationException(message);
    }
}
