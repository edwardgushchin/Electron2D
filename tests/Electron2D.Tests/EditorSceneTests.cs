using Electron2D;
using Electron2D.Editor;
using SDL = SDL3.SDL;
using IOPath = System.IO.Path;

internal static class EditorSceneTests
{
    internal static void Run()
    {
        using var mark = ResourceLoader.Load<ImageTexture>(IOPath.Combine(AppContext.BaseDirectory, "Assets", "mark-dark.svg"));
        using var sparkle = ResourceLoader.Load<ImageTexture>(IOPath.Combine(AppContext.BaseDirectory, "Assets", "sparkle.svg"));
        using var appIcon = ResourceLoader.Load<ImageTexture>(IOPath.Combine(AppContext.BaseDirectory, "Assets", "app-icon.svg"));
        using var semibold = new FontFile();
        semibold.LoadDynamicFont(IOPath.Combine(AppContext.BaseDirectory, "Assets", "IBMPlexSans-SemiBold.ttf"));
        using var regular = new FontFile();
        regular.LoadDynamicFont(IOPath.Combine(AppContext.BaseDirectory, "Assets", "IBMPlexSans-Regular.ttf"));
        Engine.MaxFPS = 60;
        ProjectSettings.Set(ProjectSettings.RenderingFallback, false);
        foreach (var backend in new[] { "gpu", "compatibility" })
        {
            ProjectSettings.Set(ProjectSettings.RenderingMethod, backend);
            var window = EditorScene.CreateWindow(mark, sparkle, appIcon, semibold, regular);
            var brand = (Control)window.GetChild(0);
            var title = (Label)brand.GetChild(1);
            var suffix = (Label)brand.GetChild(2);
            var caption = (Label)brand.GetChild(3);
            Check(window.Size == new Vector2i(1152, 800) && window.Title == "Electron2D", "Initial window contract.");
            Check(title.Text + suffix.Text == "Electron2D" && caption.Text == "Agent-native cross-platform 2D game engine", "Live font-rendered title and complete descriptor.");
            Check(ReferenceEquals(title.GetThemeFont("font"), semibold) && ReferenceEquals(suffix.GetThemeFont("font"), semibold) && ReferenceEquals(caption.GetThemeFont("font"), regular), "Brand labels use the supplied fonts rather than textures.");
            Check(regular.GetStringSize(caption.Text, fontSize: 16).X < brand.Size.X, "The descriptor fits on one line.");
            var frames = 0;
            var resized = false;
            var closed = false;
            window.CloseRequested += () => closed = true;
            window.Ready += _ => RenderingServer.FramePostDraw += () =>
            {
                Check(++frames < 120, "Native resize completes within the frame budget.");
                if (frames < 3 || resized && window.Size != new Vector2i(1281, 901)) return;
                Check(window.Title == "Electron2D", "Native title.");
                Check(brand.Position.IsEqualApprox(((Vector2)window.Size - brand.Size) / 2), "Logo and descriptor remain centered together.");
                using var pixels = RenderingServer.Service!.Readback();
                Check(pixels.Size == window.Size, "Native client dimensions.");
                Pixel(pixels, 10, 10, "#241B2C");
                var origin = (brand.Position + new Vector2(.5f, .5f)).Floor();
                Pixel(pixels, (int)origin.X + 239, (int)origin.Y + 82, "#241B2C");
                Pixel(pixels, (int)origin.X + 247, (int)origin.Y + 82, "#D65D96");
                Pixel(pixels, (int)origin.X + 263, (int)origin.Y + 82, "#FFADCF");
                Pixel(pixels, (int)origin.X + 277, (int)origin.Y + 84, "#3D2749");
                Pixel(pixels, (int)origin.X + 289, (int)origin.Y + 94, "#FFF9F3");
                Pixel(pixels, (int)origin.X + 208, (int)origin.Y + 83, "#FFE4EE");
                Check(semibold.FontWeight == 600 && regular.FontWeight == 400, "Bundled branding fonts have the correct weights.");
                var captionPixels = 0;
                for (var y = (int)origin.Y + 277; y < (int)origin.Y + 310; y++)
                    for (var x = (int)origin.X + 140; x < (int)origin.X + 500; x++)
                    {
                        var pixel = pixels.GetPixel(x, y);
                        if (pixel.R > .8f && pixel.G > .8f && pixel.B > .8f) captionPixels++;
                    }
                Check(captionPixels > 400, "The regular-font descriptor is visibly rendered.");
                var output = IOPath.GetFullPath("bin/editor-smoke");
                Directory.CreateDirectory(output);
                pixels.SavePNG(IOPath.Combine(output, $"{backend}-{(resized ? "resized" : "initial")}.png"));
                if (!resized) { resized = true; window.Size = new(1281, 901); return; }
                var native = SDL.GetWindows(out var count);
                Check(count == 1 && native is { Length: 1 }, "One native root window.");
                var close = new SDL.Event { Window = new SDL.WindowEvent { Type = SDL.EventType.WindowCloseRequested, WindowID = SDL.GetWindowID(native![0]) } };
                Check(SDL.PushEvent(ref close), "Native close event accepted.");
            };
            Check(Engine.Run(window) == 0 && closed, "Close exits successfully.");
            Check(window.IsDisposed && brand.IsDisposed && title.IsDisposed && suffix.IsDisposed && caption.IsDisposed && !mark.IsDisposed && !sparkle.IsDisposed && !appIcon.IsDisposed && !semibold.IsDisposed && !regular.IsDisposed && Engine.MainLoop is null && !DisplayServer.IsAvailable && !RenderingServer.IsAvailable, "Window cleanup preserves borrowed textures and fonts.");
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
