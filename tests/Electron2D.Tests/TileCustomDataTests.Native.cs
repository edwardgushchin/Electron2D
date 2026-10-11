using Electron2D;

internal static partial class TileCustomDataTests
{
    internal static void RunNative()
    {
        var previous = ProjectSettings.Get(ProjectSettings.RenderingMethod); var fps = Engine.MaxFPS; Engine.MaxFPS = 60;
        try { foreach (var renderer in new[] { "gpu", "compatibility" }) foreach (var backend in new[] { PhysicsServer.Backend.CPU, PhysicsServer.Backend.GPU }) NativeCustom(renderer, backend); }
        finally { ProjectSettings.Set(ProjectSettings.RenderingMethod, previous); Engine.MaxFPS = fps; }
    }
    private static void NativeCustom(string renderer, PhysicsServer.Backend backend)
    {
        ProjectSettings.Set(ProjectSettings.RenderingMethod, renderer); using var library = new TilePatternTests.Library(TileShape.Square, TileLayout.Stacked, TileOffsetAxis.Horizontal); Configure(library.Set, library.Atlas); using var world = new World(backend); var window = new Window { Title = "Custom tile data validation", Size = new(320, 160), World = world };
        var layer = new Layer { TileSet = library.Set, PhysicsQuadrantSize = 1, Position = new(80, 72), TextureFilter = TextureFilter.Nearest }; window.AddChild(layer); layer.SetCell(new(1, 0), 0, Vector2i.Zero); layer.SetCell(new(2, 0), 0, Vector2i.Zero);
        try
        {
            var frames = 0; long bytes = 0; var finished = false;
            window.Ready += _ =>
            {
                RenderingServer.SetDefaultClearColor(Colors.Black);
                RenderingServer.FramePostDraw += () =>
                {
                    frames++;
                    if (frames == 4) CheckCustomPixels(window, layer, world, true);
                    if (frames == 16) bytes = GC.GetAllocatedBytesForCurrentThread();
                    if (frames == 40)
                    {
                        bytes = GC.GetAllocatedBytesForCurrentThread() - bytes; Check(bytes == 0, $"Custom-data owner frames allocated {bytes} B"); using var image = window.GetTexture().GetImage()!; System.IO.Directory.CreateDirectory("bin/custom-data-validation"); image.SavePNG($"bin/custom-data-validation/custom-{renderer}-{backend}.png");
                        var data = library.Atlas.GetTileData(default, 0); data.SetCustomData("Solid", false); data.SetCustomData("Tint", new Color(.85f, .35f, .2f));
                    }
                    if (frames == 41) { CheckCustomPixels(window, layer, world, false); finished = true; window.Tree!.Quit(); }
                    if (frames >= 4 && frames < 40) layer.QueueRedraw();
                };
            };
            Check(Engine.Run(window) == 0 && finished, "Native custom data workflow"); Console.WriteLine($"Native custom data: {renderer}/{backend}, typed Tint/Solid/Score runtime rules, actual pixels/collision/navigation, 24 warmed forced-redraw full owner frames={bytes} B.");
        }
        finally { window.Dispose(); }
    }
    private static void CheckCustomPixels(Window window, Layer layer, World world, bool solid)
    {
        NavigationServer.Synchronize(); VerifyCustomConsumers(layer, world, solid); using var image = window.GetTexture().GetImage()!; var expected = ((TileSetAtlasSource)layer.TileSet!.GetSource(0)).GetTileData(default, 0).GetCustomData<Color>("Tint");
        foreach (var coord in layer.GetUsedCells()) { var point = layer.GlobalTransform * layer.MapToLocal(coord); var actual = image.GetPixel((int)point.X, (int)point.Y); Check(Math.Abs(actual.R - expected.R) < .03f && Math.Abs(actual.G - expected.G) < .03f && Math.Abs(actual.B - expected.B) < .03f, "Custom Tint reaches real retained pixels"); }
    }
}
