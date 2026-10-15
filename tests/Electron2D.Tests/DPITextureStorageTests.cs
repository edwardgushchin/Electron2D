using Electron2D;
using Path = System.IO.Path;

internal static class DPITextureStorageTests
{
    internal static void Write(string folder)
    {
        Directory.CreateDirectory(folder);
        using var dpi = DPITexture.CreateFromString(DPITextureTests.Source, 2, colorMap: new() { [Colors.Red] = Colors.Lime });
        dpi.ResourceLocalToScene = true; ResourceSaver.Save(dpi, Path.Combine(folder, "icon.e2dres"));
        using var root = new Window { Name = "SavedDPI", Size = new(240, 96), OversamplingOverride = 3 };
        var icon = new TextureRect { Name = "Icon", Position = new(10, 10), Size = new(32, 16), Texture = dpi }; root.AddChild(icon); icon.Owner = root;
        using var scene = new PackedScene(); scene.Pack(root); ResourceSaver.Save(scene, Path.Combine(folder, "icon.e2dscene"));
        Console.WriteLine("Saved SVG source, typed paint map and scene-local texture graph.");
    }
    internal static void Read(string folder)
    {
        using var dpi = ResourceLoader.Load<DPITexture>(Path.Combine(folder, "icon.e2dres"))!;
        DPITextureTests.Check(dpi.BaseScale == 2 && dpi.GetSource() == DPITextureTests.Source && dpi.ColorMap[Colors.Red] == Colors.Lime, "Fresh process restored exact SVG authoring data.");
        using var image = dpi.GetImage()!; DPITextureTests.Pixel(image, 4, 4, Colors.Lime);
        using var scene = ResourceLoader.Load<PackedScene>(Path.Combine(folder, "icon.e2dscene"))!;
        using var first = (Window)scene.Instantiate(); using var second = (Window)scene.Instantiate();
        var a = (DPITexture)first.GetNode<TextureRect>("Icon").Texture!; var b = (DPITexture)second.GetNode<TextureRect>("Icon").Texture!;
        DPITextureTests.Check(a != b && a != dpi && a.GetRID() != b.GetRID() && a.ResourceLocalToScene && first.GetOversampling() == 3, "Fresh scene instances have independent local resources, identities and viewport policy.");
        using var pixels = a.GetImage()!; DPITextureTests.Pixel(pixels, 4, 4, Colors.Lime);
        Console.WriteLine("Fresh-process resource and scene restoration passed.");
    }
    internal static void Render(string backend, string folder)
    {
        ProjectSettings.Set(ProjectSettings.RenderingMethod, backend); ProjectSettings.Set(ProjectSettings.RenderingFallback, false);
        using var scene = ResourceLoader.Load<PackedScene>(Path.Combine(folder, "icon.e2dscene"))!;
        using var window = (Window)scene.Instantiate(); using var target = window.GetTexture(); var frame = 0;
        void Capture()
        {
            if (++frame != 3) return;
            RenderingServer.FramePostDraw -= Capture; using var image = target.GetImage()!; DPITextureTests.Pixel(image, 15, 15, Colors.Lime);
            if (Environment.GetEnvironmentVariable("ELECTRON2D_CAPTURE_DIR") is { } output) { Directory.CreateDirectory(output); image.SavePNG(Path.Combine(output, "dpi-fresh-" + backend + ".png")); }
            window.Tree!.Quit();
        }
        window.Ready += _ => RenderingServer.FramePostDraw += Capture;
        try { Engine.Run(window); } finally { if (RenderingServer.IsAvailable) RenderingServer.FramePostDraw -= Capture; }
        DPITextureTests.Check(frame == 3, "Fresh scene rendered."); Console.WriteLine("Fresh SVG scene rendered and read back: " + backend);
    }
}
