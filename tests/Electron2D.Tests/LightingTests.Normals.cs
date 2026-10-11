using Electron2D;

internal static partial class LightingTests
{
    private static void NativeNormals()
    {
        using var diffuseImage = Image.CreateEmpty(16, 16, false, Image.Format.Rgba8); diffuseImage.Fill(Colors.White);
        using var normalImage = Image.CreateEmpty(16, 16, false, Image.Format.Rgbaf); normalImage.Fill(new(.5f, .5f, 0, 1));
        using var specularImage = Image.CreateEmpty(16, 16, false, Image.Format.Rgbaf); specularImage.Fill(new(.4f, .4f, .4f, .5f));
        using var diffuse = ImageTexture.CreateFromImage(diffuseImage); using var normal = ImageTexture.CreateFromImage(normalImage); using var specular = ImageTexture.CreateFromImage(specularImage);
        using var canvas = new CanvasTexture { DiffuseTexture = diffuse, NormalTexture = normal };
        using var source = new TileSetAtlasSource { Texture = canvas }; source.CreateTile(Vector2i.Zero);
        var padded = (CanvasTexture)source.GetRuntimeTexture()!; Check(padded.NormalTexture is not null && padded.GetSize() == new Vector2(18, 18), "Padding retains normal channel and dimensions");
        var window = new Window { Size = new(64, 32), Title = "Normal and specular lighting validation" }; var surface = new Surface { Area = new(0, 0, 64, 32), Texture = canvas };
        var light = new PointLight { Texture = diffuse, Position = new(32, 16), TextureScale = 4, Height = 16, Energy = .25f }; window.AddChild(surface); window.AddChild(light);
        var frames = 0; long bytes = 0;
        window.Ready += _ =>
        {
            RenderingServer.CanvasSetModulate(window.FindWorld()!.Canvas, new(.1f, .1f, .1f));
            RenderingServer.FramePostDraw += () =>
            {
                frames++;
                if (frames <= 6)
                {
                    using var pixels = window.GetTexture().GetImage()!;
                    var value = frames switch { 1 => .279515f, 2 => .349870f, 3 => .1f, 4 => .291228f, 5 => 0, _ => .089757f };
                    Pixel(pixels, 16, 16, new(value, value, value));
                }
                if (frames == 1) { normalImage.Fill(new(1, .5f, 0, 1)); normal.Update(normalImage); light.Height = 0; }
                if (frames == 2) { surface.Area = new(0, 0, -64, 32); surface.QueueRedraw(); }
                if (frames == 3) { surface.Area = new(0, 0, 64, 32); surface.QueueRedraw(); normalImage.Fill(new(.5f, .5f, 0, 1)); normal.Update(normalImage); light.Height = 16; canvas.SpecularTexture = specular; canvas.SpecularShininess = .2f; }
                if (frames == 4) { canvas.SpecularTexture = null; canvas.SpecularShininess = 1; light.BlendMode = LightBlendMode.Sub; }
                if (frames == 5) { light.BlendMode = LightBlendMode.Mix; light.Color = new(1, 1, 1, .5f); }
                if (frames == 16) bytes = GC.GetAllocatedBytesForCurrentThread();
                if (frames == 40) { bytes = GC.GetAllocatedBytesForCurrentThread() - bytes; Check(bytes == 0, $"Prepared normal/specular frames allocated {bytes} B"); window.Tree!.Quit(); }
            };
        };
        Check(Engine.Run(window) == 0 && frames == 40, "Normal/specular workflow completed");
        Console.WriteLine($"Lighting channel pixels: normal XY/height/reflection, live channels, specular exponent, Sub/Mix, multichannel padding and 24 prepared owner frames={bytes} B passed.");
    }
}
