using Electron2D;

internal static class CustomDrawChecks
{
    internal static void Run()
    {
        foreach (var renderer in new[] { "gpu", "compatibility" })
        {
            ProjectSettings.Set(ProjectSettings.RenderingMethod, renderer);
            using var shape = new CustomShape(Annulus.Create(20, 40));
            var window = new Window { Title = "Custom annulus diagnostics", Size = new(240, 160) };
            var painter = new Painter(shape) { Position = new(80, 80) }; window.AddChild(painter);
            var frames = 0;
            Action post = () =>
            {
                using var pixels = window.GetTexture().GetImage()!;
                var outer = Colors.Black; var hole = pixels.GetPixel(80, 80);
                for (var y = 78; y <= 82; y++) for (var x = 118; x <= 122; x++)
                        if (pixels.GetPixel(x, y).G > outer.G) outer = pixels.GetPixel(x, y);
                if (Environment.GetEnvironmentVariable("ELECTRON2D_CUSTOM_CAPTURE") is { } folder)
                { Directory.CreateDirectory(folder); pixels.SavePNG(System.IO.Path.Combine(folder, $"{renderer}-{frames}.png")); }
                if (outer.G < .6f || hole.G > .05f) throw new InvalidOperationException($"Custom diagnostic pixels differ: outer={outer}, hole={hole}");
                if (++frames == 1) { shape.Geometry = Annulus.Create(10, 40); painter.QueueRedraw(); }
                else window.Tree!.Quit();
            };
            window.Ready += _ => { RenderingServer.SetDefaultClearColor(Colors.Black); RenderingServer.FramePostDraw += post; };
            try { if (Engine.Run(window) != 0) throw new InvalidOperationException("Custom drawing host failed."); }
            finally { if (RenderingServer.IsAvailable) RenderingServer.FramePostDraw -= post; }
            if (frames != 2) throw new InvalidOperationException("Custom drawing revisions were not rendered.");
            Console.WriteLine($"Custom annulus {renderer}: actual diagnostic lines and empty-hole pixels passed.");
        }
    }
    private sealed class Painter(Shape shape) : Entity
    {
        protected override void OnDraw() => shape.Draw(GetCanvasItem(), Colors.Green);
    }
}
