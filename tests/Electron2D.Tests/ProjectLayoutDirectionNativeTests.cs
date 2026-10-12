using Electron2D;
using IOPath = System.IO.Path;

internal static class ProjectLayoutDirectionNativeTests
{
    internal static void Run()
    {
        var backend = Environment.GetEnvironmentVariable("ELECTRON2D_TEST_LAYOUT_BACKEND") ?? "compatibility";
        var method = ProjectSettings.Get(ProjectSettings.RenderingMethod);
        var mode = ProjectSettings.Get(ProjectSettings.RootNodeLayoutDirection);
        var force = ProjectSettings.Get(ProjectSettings.ForceRightToLeftLayoutDirection);
        var testLocale = ProjectSettings.Get(ProjectSettings.LocaleTest);
        try
        {
            ProjectSettings.Set(ProjectSettings.RenderingMethod, backend);
            ProjectSettings.Set(ProjectSettings.LocaleTest, "en");
            Pixels(backend, false); Pixels(backend, true);
        }
        finally
        {
            ProjectSettings.Set(ProjectSettings.RenderingMethod, method);
            ProjectSettings.Set(ProjectSettings.RootNodeLayoutDirection, mode);
            ProjectSettings.Set(ProjectSettings.ForceRightToLeftLayoutDirection, force);
            ProjectSettings.Set(ProjectSettings.LocaleTest, testLocale);
        }
        Console.WriteLine($"Project layout GUI/text pixels passed: {backend}.");
    }

    private static void Pixels(string backend, bool forced)
    {
        ProjectSettings.Set(ProjectSettings.RootNodeLayoutDirection, forced ? 1 : 2);
        ProjectSettings.Set(ProjectSettings.ForceRightToLeftLayoutDirection, forced);
        var window = new Window { Size = new(200, 100), LayoutDirection = forced ? LayoutDirection.LTR : LayoutDirection.Inherited };
        var box = new Box { Position = new(10, 10), Size = new(20, 10) };
        var label = new Label { Text = "AA", Position = new(10, 40), Size = new(80, 30), LayoutDirection = forced ? LayoutDirection.LTR : LayoutDirection.Inherited };
        window.AddChild(box); window.AddChild(label);
        box.SetBegin(new(10, 10)); box.SetEnd(new(30, 20));
        label.SetBegin(new(10, 40)); label.SetEnd(new(90, 70));
        var frame = 0;
        window.Ready += _ =>
        {
            Check(box.Position.X == 170 && window.IsLayoutRTL() == !forced, $"Project policy resolves before ready: x={box.Position.X}, width={window.Size.X}, rootRTL={window.IsLayoutRTL()}, forced={forced}.");
            RenderingServer.SetDefaultClearColor(Colors.Black);
            var renderer = RenderingServer.Service!;
            RenderingServer.FramePostDraw += () =>
            {
                using var image = renderer.Readback(); frame++;
                var rtl = forced || frame == 1;
                var pixel = image.GetPixel(rtl ? 175 : 15, 15);
                Check(pixel.R > .95f && pixel.G < .05f, "Root and inherited policy reaches canvas pixels.");
                Check(image.GetPixel(rtl ? 15 : 175, 15).R < .05f, "Previous rectangle position stays clear.");
                var textRTL = !forced && frame == 1;
                Check(Ink(image, textRTL ? new(150, 40, 40, 30) : new(10, 40, 40, 30)) > 20, "Label begin alignment follows resolved layout direction.");
                var opposite = textRTL ? new Rect2i(110, 40, 20, 30) : new Rect2i(70, 40, 20, 30);
                Check(Ink(image, opposite) == 0, "Label text does not remain aligned to the opposite edge.");
                if (frame == 1)
                {
                    var directory = Environment.GetEnvironmentVariable("ELECTRON2D_TEST_LAYOUT_OUTPUT") ?? IOPath.GetTempPath();
                    Directory.CreateDirectory(directory);
                    File.WriteAllBytes(IOPath.Combine(directory, $"layout-{backend}-{(forced ? "forced" : "root")}.png"), image.SavePNGToBuffer());
                    ProjectSettings.Set(ProjectSettings.RootNodeLayoutDirection, 1);
                    ProjectSettings.Set(ProjectSettings.ForceRightToLeftLayoutDirection, !forced);
                    Check(box.IsLayoutRTL(), "An active tree keeps its captured project settings.");
                    window.LayoutDirection = LayoutDirection.LTR;
                    if (forced) window.Tree!.Quit();
                }
                else window.Tree!.Quit();
            };
        };
        Engine.Run(window);
        Check(window.IsDisposed && DisplayServer.Service is null && RenderingServer.Service is null, "Native runtime teardown releases the root and renderer.");
    }

    private sealed class Box : Control
    {
        protected override void OnNotification(int what) { base.OnNotification(what); if (what == NotificationDraw) DrawRect(new(Vector2.Zero, Size), Colors.Red); }
    }
    private static int Ink(Image image, Rect2i rect)
    {
        var count = 0;
        for (var y = rect.Position.Y; y < rect.End.Y; y++) for (var x = rect.Position.X; x < rect.End.X; x++) if (image.GetPixel(x, y).G > .05f) count++;
        return count;
    }
    private static void Check(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
