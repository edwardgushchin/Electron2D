using Electron2D;
internal static partial class ImageBCnTests
{
    private sealed class Painter(ImageTexture compressed, ImageTexture decoded) : Entity
    {
        protected override void OnDraw()
        {
            DrawTextureRect(compressed, new Rect2(8, 8, 80, 64), false); DrawTextureRect(decoded, new Rect2(108, 8, 80, 64), false);
            DrawTextureRectRegion(compressed, new Rect2(8, 84, 80, 64), new Rect2(1, 1, 4, 3)); DrawTextureRectRegion(decoded, new Rect2(108, 84, 80, 64), new Rect2(1, 1, 4, 3));
        }
    }
    internal static void RunNative(string profile)
    {
        var parts = profile.Split(':'); var backend = parts[0]; var format = parts.Length > 1 ? Enum.Parse<Image.Format>(parts[1]) : Image.Format.Dxt5;
        ProjectSettings.Set(ProjectSettings.RenderingMethod, backend); ProjectSettings.Set(ProjectSettings.RenderingFallback, false);
        using var image = BlockSource(format);
        using var compressed = ImageTexture.CreateFromImage(image); using var plain = (Image)image.Duplicate(); plain.Decompress(); using var decoded = ImageTexture.CreateFromImage(plain);
        var window = new Window { Size = new(200, 160) }; var painter = new Painter(compressed, decoded) { TextureFilter = TextureFilter.Nearest }; window.AddChild(painter); using var target = window.GetTexture();
        using var leftAtlas = new AtlasTexture { Atlas = compressed, Region = new(1, 1, 4, 3) };
        using var rightAtlas = new AtlasTexture { Atlas = decoded, Region = new(1, 1, 4, 3) };
        window.AddChild(new Sprite { Name = "compressedSprite", Texture = leftAtlas, Centered = false, Position = new(8, 152), TextureFilter = TextureFilter.Nearest });
        window.AddChild(new Sprite { Name = "decodedSprite", Texture = rightAtlas, Centered = false, Position = new(108, 152), TextureFilter = TextureFilter.Nearest });
        if (backend == "compatibility" && format is Image.Format.BptcRgbf or Image.Format.BptcRgbfu)
        {
            Reject<NotSupportedException>(() => Engine.Run(window)); Check(window.IsDisposed && RenderingServer.Service == null, "Unavailable Compatibility HDR role releases its host.");
            Console.WriteLine($"BC textures {backend}/{format}: expected HDR precision rejection and cleanup."); return;
        }
        var frames = 0; long before = 0, active = 0, idle = 0; var phase = 0;
        window.Ready += _ =>
        {
            RenderingServer.SetDefaultClearColor(Colors.Black);
            if (backend == "gpu") CheckNativeStorage(compressed, image);
            RenderingServer.FramePreDraw += () => { before = GC.GetAllocatedBytesForCurrentThread(); if (frames < 96) painter.QueueRedraw(); };
            RenderingServer.FramePostDraw += () =>
            {
                if (frames >= 32 && frames < 96) active += GC.GetAllocatedBytesForCurrentThread() - before; if (frames >= 96 && frames < 160) idle += GC.GetAllocatedBytesForCurrentThread() - before;
                if (frames++ < 160) return;
                Check(active == 0 && idle == 0, $"Prepared compressed texture bytes {active}/{idle}."); using var pixels = target.GetImage()!; var visible = 0;
                for (var y = 0; y < 160; y++) for (var x = 0; x < 95; x++)
                    {
                        var a = pixels.GetPixel(x, y); var b = pixels.GetPixel(x + 100, y); Check(MathF.Abs(a.R - b.R) < .012f && MathF.Abs(a.G - b.G) < .012f && MathF.Abs(a.B - b.B) < .012f, "Native BC/decoded comparison."); if (a.R > .05f || a.G > .05f || a.B > .05f) visible++;
                    }
                Check(visible > 100, "Compressed source draws visible pixels.");
                if (phase++ == 0)
                {
                    using var updated = BlockSource(format, true); image.CopyFrom(updated); compressed.Update(image);
                    if (backend == "gpu") CheckNativeStorage(compressed, image);
                    plain.CopyFrom(image); plain.Decompress(); decoded.Update(plain); painter.QueueRedraw(); frames = 0; active = 0; idle = 0;
                }
                else window.Tree!.Quit();
            };
        };
        Check(Engine.Run(window) == 0 && phase == 2, "Native compressed sampling and update finish."); Console.WriteLine($"BC textures {backend}/{format}: source/region/mip/update pixels; 64 active/64 idle managed bytes {active}/{idle}.");
    }
    private static void CheckNativeStorage(ImageTexture texture, Image image)
    {
        var device = RenderingServer.GetRenderingDevice()!; var ordinary = RenderingServer.TextureGetRDTexture(texture.GetRID());
        using var description = device.TextureGetFormat(ordinary);
        var expected = image.PixelFormat switch
        {
            Image.Format.Dxt5RaAsRg => RenderingDevice.DataFormat.R8G8B8A8UNORM,
            Image.Format.Dxt1 => RenderingDevice.DataFormat.BC1RGBAUNORMBLOCK,
            Image.Format.Dxt3 => RenderingDevice.DataFormat.BC2UNORMBLOCK,
            Image.Format.Dxt5 => RenderingDevice.DataFormat.BC3UNORMBLOCK,
            Image.Format.RgtcR => RenderingDevice.DataFormat.BC4UNORMBLOCK,
            Image.Format.RgtcRg => RenderingDevice.DataFormat.BC5UNORMBLOCK,
            Image.Format.BptcRgba => RenderingDevice.DataFormat.BC7UNORMBLOCK,
            Image.Format.BptcRgbf => RenderingDevice.DataFormat.BC6HSFLOATBLOCK,
            _ => RenderingDevice.DataFormat.BC6HUFLOATBLOCK
        };
        Check(description.Format == expected && description.MIPMaps == image.MIPMapCount + 1, "Borrowed renderer identity describes actual BC storage and mip count.");
        using var decoded = (Image)image.Duplicate(); if (image.PixelFormat == Image.Format.Dxt5RaAsRg) decoded.Decompress();
        var expectedBytes = image.PixelFormat == Image.Format.Dxt5RaAsRg ? decoded.GetData() : image.GetData();
        Check(device.TextureGetData(ordinary, 0).AsSpan().SequenceEqual(expectedBytes), "Native compressed mip bytes remain exact.");
        using var array = new TextureArray(); array.CreateFromImages([image, image]); var layered = RenderingServer.TextureGetRDTexture(array.GetRID());
        using var layerDescription = device.TextureGetFormat(layered); Check(layerDescription.Format == expected && layerDescription.ArrayLayers == 2, "BC array uses the same native block storage.");
        Check(device.TextureGetData(layered, 1).AsSpan().SequenceEqual(expectedBytes), "Every native array mip reads back exact blocks.");
        array.Dispose(); Check(!device.TextureIsValid(layered), "Array disposal expires the borrowed identity."); device.FreeRID(layered);
    }
}
