using Electron2D;

internal static class DrawChecks
{
    internal static void Run(PhysicsServer.Backend backend)
    {
        foreach (var renderer in new[] { "gpu", "compatibility" })
        {
            ProjectSettings.Set(ProjectSettings.RenderingMethod, renderer);
            using var world = new World(backend);
            using var shape = new ProceduralShape(ShapeGeometry.Rectangle(new(20, 20)));
            var window = new Window { Title = "Procedural shape drawing", Size = new(240, 160), World = world };
            var painter = new Painter(shape) { Position = new(60, 60) }; window.AddChild(painter);
            var stage = 0;
            Action post = () =>
            {
                using var pixels = window.GetTexture().GetImage()!;
                var color = pixels.GetPixel(stage == 0 ? 60 : 75, 60);
                if (Math.Abs(color.G - 1) > .04f || color.R > .04f || color.B > .04f)
                    throw new InvalidOperationException($"Custom shape rendered pixel mismatch: {color}");
                if (stage++ == 0) { shape.Update(ShapeGeometry.Rectangle(new(40, 20))); painter.QueueRedraw(); }
                else window.Tree!.Quit();
            };
            window.Ready += _ => { RenderingServer.SetDefaultClearColor(Colors.Black); RenderingServer.FramePostDraw += post; };
            try { if (Engine.Run(window) != 0) throw new InvalidOperationException("Procedural drawing window failed."); }
            finally { if (RenderingServer.IsAvailable) RenderingServer.FramePostDraw -= post; }
            if (stage != 2 || shape.Builds != 2) throw new InvalidOperationException("Drawing must consume both geometry revisions.");
            Console.WriteLine($"{backend}/{renderer}: actual window pixels, inherited Shape.Draw and live procedural geometry redraw passed.");
        }
    }
    private sealed class Painter(Shape shape) : Entity
    {
        protected override void OnDraw() => shape.Draw(GetCanvasItem(), Colors.Green);
    }
}
