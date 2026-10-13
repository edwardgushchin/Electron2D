using Electron2D;

internal static partial class LightingTests
{
    internal static void RunNative(bool includeCompatibility = true)
    {
        var method = ProjectSettings.Get(ProjectSettings.RenderingMethod); var fps = Engine.MaxFPS;
        ProjectSettings.Set(ProjectSettings.RenderingMethod, "gpu"); Engine.MaxFPS = 60;
        try { NativePixels(); NativeSDF(); NativeRIDs(); NativeNormals(); NativeInterpolation(); if (includeCompatibility) NativeCompatibility(); }
        finally { ProjectSettings.Set(ProjectSettings.RenderingMethod, method); Engine.MaxFPS = fps; }
    }
    private sealed class Surface : Entity
    {
        internal Rect2 Area;
        internal Texture? Texture { get; set; }
        protected override void OnDraw() { if (Texture is null) DrawRect(Area, Colors.White); else DrawTextureRect(Texture, Area, false); }
    }
    private static void NativePixels()
    {
        using var image = Image.CreateEmpty(1, 1, false, Image.Format.Rgba8); image.Fill(Colors.White);
        using var mask = ImageTexture.CreateFromImage(image); mask.SetSizeOverride(new(64, 32));
        using var unshaded = new CanvasItemMaterial { LightMode = LightMode.Unshaded };
        using var lightOnly = new CanvasItemMaterial { LightMode = LightMode.LightOnly };
        using var polygon = new OccluderPolygon { Closed = false, Polygon = [new(16, 0), new(16, 32)] };
        var window = new Window { Size = new(64, 32), Title = "Canvas lighting validation" };
        var left = new Surface { Name = "Left", Area = new(4, 4, 16, 24) }; var right = new Surface { Name = "Right", Area = new(40, 4, 16, 24), LightMask = 2 };
        var point = new PointLight { Texture = mask, Position = new(32, 16), Color = Colors.Red, Energy = .25f };
        var directional = new DirectionalLight { Color = Colors.Blue, Energy = .2f, Enabled = false };
        var occluder = new LightOccluder { Occluder = polygon };
        window.AddChild(left); window.AddChild(right); window.AddChild(new Surface { Name = "Unlit", Area = new(24, 8, 8, 8), Material = unshaded }); window.AddChild(point); window.AddChild(directional); window.AddChild(occluder);
        var frames = 0; long bytes = 0;
        window.Ready += _ =>
        {
            Check(RenderingServer.GetCurrentRenderingMethod() == "gpu", "GPU lighting renderer selected");
            RenderingServer.SetDefaultClearColor(Colors.Black); RenderingServer.CanvasSetModulate(window.FindWorld()!.Canvas, new(.1f, .1f, .1f));
            RenderingServer.FramePostDraw += () =>
            {
                frames++;
                if (frames < 7)
                {
                    using var pixels = window.GetTexture().GetImage()!;
                    var expectedLeft = frames switch { 1 => new Color(.35f, .1f, .1f), 2 => new Color(.1f, .1f, .1f), 3 => new Color(.35f, .1f, .1f), 4 => new Color(.1f, .1f, .3f), 5 => new Color(.1f, .1f, .1f), _ => Colors.White };
                    Pixel(pixels, 12, 16, expectedLeft); Pixel(pixels, 48, 16, frames == 4 ? new(.1f, .1f, .3f) : new(.1f, .1f, .1f)); Pixel(pixels, 28, 12, Colors.White);
                }
                if (frames == 1) point.ShadowEnabled = true;
                if (frames == 2) occluder.OccluderLightMask = 2;
                if (frames == 3) { point.Enabled = false; directional.Enabled = true; }
                if (frames == 4) { directional.RangeLayerMin = 2; directional.RangeLayerMax = 1; }
                if (frames == 5) { directional.Enabled = false; point.Enabled = true; left.Material = lightOnly; }
                if (frames == 16) bytes = GC.GetAllocatedBytesForCurrentThread();
                if (frames == 40) { bytes = GC.GetAllocatedBytesForCurrentThread() - bytes; Check(bytes == 0, $"Prepared lighting owner frames allocated {bytes} bytes"); window.Tree!.Quit(); }
            };
        };
        Check(Engine.Run(window) == 0 && frames == 40, "Native lighting workflow completed");
        Console.WriteLine($"Canvas lighting pixels: masks, ambient, point shadow, directional selection, reversed ranges, shading modes and 24 prepared owner frames={bytes} B passed.");
    }
    private static void Pixel(Image image, int x, int y, Color expected)
    {
        var actual = image.GetPixel(x, y);
        Check(MathF.Abs(actual.R - expected.R) < .008f && MathF.Abs(actual.G - expected.G) < .008f && MathF.Abs(actual.B - expected.B) < .008f, $"Pixel {x},{y}: expected {expected}, got {actual}");
    }
}
