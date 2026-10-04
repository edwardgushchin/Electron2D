using Electron2D;

internal static partial class RenderingRuntimeTests
{
    private static void VerifyLayoutContainers(string backend)
    {
        var window = new Window { Size = new(180, 130) };
        using var red = new StyleBoxFlat { BGColor = Colors.Red, AntiAliasing = false };
        using var green = new StyleBoxFlat { BGColor = Colors.Green, AntiAliasing = false };
        using var blue = new StyleBoxFlat { BGColor = Colors.Blue, AntiAliasing = false };
        var margin = new MarginContainer { Position = new(10, 10), Size = new(50, 40) };
        foreach (var side in new[] { "left", "top", "right", "bottom" }) margin.AddThemeConstantOverride("margin_" + side, 8);
        var redPanel = new Panel { Name = "Red" }; redPanel.AddThemeStyleBoxOverride("panel", red); margin.AddChild(redPanel);
        var center = new CenterContainer { Position = new(70, 10), Size = new(50, 40) };
        var greenPanel = new Panel { Name = "Green", CustomMinimumSize = new(20, 10) };
        greenPanel.AddThemeStyleBoxOverride("panel", green); center.AddChild(greenPanel);
        var aspect = new AspectRatioContainer { Position = new(10, 60), Size = new(100, 50), Ratio = 1 };
        var bluePanel = new Panel { Name = "Blue" }; bluePanel.AddThemeStyleBoxOverride("panel", blue); aspect.AddChild(bluePanel);
        window.AddChild(margin); window.AddChild(center); window.AddChild(aspect);
        var frames = 0;
        window.Ready += _ =>
        {
            var server = RenderingServer.Service!;
            RenderingServer.SetDefaultClearColor(Colors.Black);
            RenderingServer.FramePostDraw += () =>
            {
                using var image = server.Readback();
                frames++;
                Check(redPanel.Position == new Vector2(8, 8) && greenPanel.Position == new Vector2(15, 15) &&
                      bluePanel.Position == new Vector2(25, 0), $"The {backend} container child positions are ready before drawing.");
                var redInside = image.GetPixel(25, 25); var redOutside = image.GetPixel(12, 12);
                var greenInside = image.GetPixel(92, 30); var greenOutside = image.GetPixel(75, 15);
                var blueInside = image.GetPixel(60, 85); var blueOutside = image.GetPixel(20, 85);
                Check(redInside.R > .8f && redOutside.R < .1f && greenInside.G > .8f && greenOutside.G < .1f &&
                      blueInside.B > .8f && blueOutside.B < .1f,
                    $"The {backend} canvas draws only the allocated margin, center and aspect rectangles.");
                window.Tree!.Quit();
            };
        };
        Engine.Run(window);
        Released(window);
        Check(frames == 1, $"The {backend} layout frame completed.");
        Console.WriteLine($"Margin, center and aspect panels rendered on {backend}.");
    }
}
