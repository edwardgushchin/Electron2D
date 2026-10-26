using Electron2D;
using Path = System.IO.Path;

internal static partial class MaterialPlaceholderTests
{
    internal static void RunNative(string backend)
    {
        var captures = Environment.GetEnvironmentVariable("ELECTRON2D_CAPTURE_DIR");
        var folder = captures ?? At(Path.GetTempPath(), "electron2d-material-native-" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(folder);
        try { RunNative(backend, folder); }
        finally { ResourceLoader.SetAbortOnMissingResources(true); if (captures is null) Directory.Delete(folder, true); }
    }
    private static void RunNative(string backend, string folder)
    {
        Produce(folder); Preserve(folder); RestoreFiles(folder);
        ResourceLoader.SetAbortOnMissingResources(false);
        using var scene = ResourceLoader.Load<PackedScene>(At(folder, "missing.e2dscene"), ResourceLoader.CacheMode.IgnoreDeep);
        ResourceLoader.SetAbortOnMissingResources(true);
        using var restored = ResourceLoader.Load<Material>(At(folder, "restored-saved.e2dres"), ResourceLoader.CacheMode.IgnoreDeep);
        var root = scene.Instantiate(); root.GetNode<Polygon>("First").Position = new(4, 4); root.GetNode<Polygon>("Second").Position = new(32, 4);
        var first = root.GetNode<Polygon>("First"); var second = root.GetNode<Polygon>("Second"); var placeholder = first.Material!;
        var firstItem = first.GetCanvasItem(); var secondItem = second.GetCanvasItem(); var missingRID = placeholder.GetRID(); var restoredRID = restored.GetRID();
        Check(placeholder is PlaceholderMaterial && placeholder == second.Material, "Actual missing material binds one shared canvas identity.");
        ProjectSettings.Set(ProjectSettings.RenderingMethod, backend); ProjectSettings.Set(ProjectSettings.RenderingFallback, false);
        var window = new Window { Size = new(64, 32), Title = "Material placeholder recovery" }; window.AddChild(root);
        using var target = window.GetTexture(); var frames = 0; long start = 0, allocated = 0;
        window.Ready += _ =>
        {
            Check(RenderingServer.GetCurrentRenderingMethod() == backend, "Selected actual backend without fallback.");
            RenderingServer.SetDefaultClearColor(Colors.Red);
            Reject<InvalidOperationException>(() => RenderingServer.FreeRID(missingRID));
            window.Tree!.ProcessFrameStarted += _ =>
            {
                if (frames < 16) return;
                start = GC.GetAllocatedBytesForCurrentThread();
                var material = (frames & 1) == 0 ? restoredRID : missingRID;
                RenderingServer.CanvasItemSetMaterial(firstItem, material); RenderingServer.CanvasItemSetMaterial(secondItem, material);
            };
            RenderingServer.FramePostDraw += Capture;
        };
        void Snapshot(string name, Color color)
        {
            using var image = target.GetImage()!;
            Pixel(image, 8, 8, color); Pixel(image, 36, 8, color); Pixel(image, 60, 24, Colors.Red);
            image.SavePNG(At(folder, name + ".png"));
        }
        void Capture()
        {
            if (frames >= 16) allocated += GC.GetAllocatedBytesForCurrentThread() - start;
            frames++;
            if (frames == 4)
            {
                Snapshot("missing-material", Colors.Blue);
                RenderingServer.CanvasItemSetMaterial(firstItem, restoredRID); RenderingServer.CanvasItemSetMaterial(secondItem, restoredRID);
            }
            if (frames == 8)
            {
                Snapshot("restored-material", Colors.Magenta);
                RenderingServer.CanvasItemSetMaterial(firstItem, missingRID); RenderingServer.CanvasItemSetMaterial(secondItem, missingRID);
            }
            if (frames != 80) return;
            Check(allocated == 0, "Prepared live material switches allocated " + allocated + " managed bytes.");
            Snapshot("live-switch", Colors.Blue);
            Check(placeholder.GetRID() == missingRID && restored.GetRID() == restoredRID, "Renderer switches preserve borrowed material resource identities.");
            window.Tree!.Quit();
        }
        try { Check(Engine.Run((_, _) => { }, window) == 0, "Native recovery execution completed."); }
        finally { if (RenderingServer.IsAvailable) RenderingServer.FramePostDraw -= Capture; ResourceLoader.SetAbortOnMissingResources(true); }
        Console.WriteLine($"PASS {_checks} material placeholder {backend} contracts, {frames} native frames, 64 prepared switches, {allocated} managed bytes.");
    }
    private static void Pixel(Image image, int x, int y, Color expected)
    {
        var actual = image.GetPixel(x, y);
        Check(MathF.Abs(actual.R - expected.R) < .025f && MathF.Abs(actual.G - expected.G) < .025f && MathF.Abs(actual.B - expected.B) < .025f, $"Material pixel ({x},{y}) is {actual}, expected {expected}.");
    }
}
