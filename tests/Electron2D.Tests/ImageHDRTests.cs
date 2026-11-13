using System.Text;
using Electron2D;

internal static class ImageHDRTests
{
    internal static void Run()
    {
        var folder = Directory.CreateTempSubdirectory("Electron2D-hdr-");
        try
        {
            var path = System.IO.Path.Combine(folder.FullName, "texture.HDR");
            byte[] red = [128, 0, 0, 130], green = [0, 128, 0, 129];
            byte[] blue = [0, 0, 128, 128], black = [255, 255, 255, 0];
            File.WriteAllBytes(path, FileBytes("+X 2 +Y 2", [.. blue, .. red, .. black, .. green], alternate: true));
            using var image = Image.LoadFromFile(path);
            Check(image.Size == new Vector2i(2, 2) && image.PixelFormat == Image.Format.Rgbe9995 && !image.HasMIPMaps,
                "Radiance loads as owned RGB9E5 base pixels.");
            Check(image.GetPixel(0, 0) == new Color(2, 0, 0) && image.GetPixel(1, 0) == Colors.Green &&
                image.GetPixel(0, 1) == new Color(0, 0, .5f) && image.GetPixel(1, 1) == Colors.Black,
                "Column-major RGBE normalizes orientation, including exponent-zero black.");
            Check(ResourceLoader.Exists<ImageTexture>(path) &&
                ResourceLoader.GetRecognizedExtensionsForType<ImageTexture>().Contains("hdr") &&
                ResourceLoader.ListDirectory(folder.FullName).Contains("texture.HDR"), "ImageTexture discovers Radiance files.");
            using var texture = ResourceLoader.Load<ImageTexture>(path, ResourceLoader.CacheMode.Ignore);
            using var retained = texture.GetImage()!;
            Check(retained.PixelFormat == Image.Format.Rgbe9995 && retained.GetData().SequenceEqual(image.GetData()),
                "Resource loading preserves HDR values and owned storage.");
            ResourceLoader.LoadThreadedRequest<ImageTexture>(path, cacheMode: ResourceLoader.CacheMode.Ignore);
            using var threaded = ResourceLoader.LoadThreadedGet<ImageTexture>(path);
            using var threadedPixels = threaded.GetImage()!;
            Check(threadedPixels.PixelFormat == Image.Format.Rgbe9995 &&
                threadedPixels.GetData().SequenceEqual(retained.GetData()), "Threaded loading retains the same HDR snapshot.");
            using var display = retained.RGBEToSRGB();
            Check(display.GetData().AsSpan(0, 3).SequenceEqual(new byte[] { 255, 0, 0 }), "HDR data feeds RGBE-to-sRGB conversion.");

            byte[] scanline = [2, 2, 0, 8, 136, 128, 8, 0, 16, 32, 48, 64, 80, 96, 112, 136, 64, 136, 130];
            File.WriteAllBytes(path, FileBytes("-Y 2 -X 8", [.. scanline, .. red, 1, 1, 1, 7]));
            var changes = 0;
            image.Changed += _ => changes++;
            image.Load(path);
            Check(changes == 1 && image.Size == new Vector2i(8, 2) &&
                image.GetPixel(7, 0) == new Color(2, 0, 1) && image.GetPixel(0, 0) == new Color(2, 1.75f, 1) &&
                image.GetPixel(7, 1) == new Color(2, 0, 0) && image.GetPixel(0, 1) == new Color(2, 0, 0),
                "Planar repeat and literal packets and legacy runs can share one file.");

            File.WriteAllBytes(path, FileBytes("-Y 1 +X 257", [.. red, 1, 1, 1, 0, 1, 1, 1, 1]));
            image.Load(path);
            Check(image.GetPixel(256, 0) == new Color(2, 0, 0), "Legacy run lengths use little-endian digits.");
            File.WriteAllBytes(path, FileBytes("-Y 1 +X 2", [255, 0, 0, 255, 255, 0, 0, 1]));
            image.Load(path);
            Check(image.GetPixel(0, 0) == new Color(65408, 0, 0) && image.GetPixel(1, 0) == Colors.Black,
                "Out-of-range RGBE values saturate or round to black at RGB9E5 precision.");

            var previous = image.GetData();
            changes = 0;
            foreach (var bad in new[]
            {
                FileBytes("-Y 1 +X 2", red),
                FileBytes("-Y 1 +X 2", [1, 1, 1, 2]),
                FileBytes("-Y 1 +X 2", [.. red, 1, 1, 1, 2]),
                FileBytes("-Y 1 +X 8", [2, 2, 0, 9, .. scanline.AsSpan(4)]),
                FileBytes("-Y 1 +X 8", [2, 2, 0, 8, 0, 1]),
                FileBytes("-Y 1 +X 8", [2, 2, 0, 8, 9, 1]),
                FileBytes("-Y 1 +X 8", scanline[..^1]),
                FileBytes("-Y 1 +Y 2", red),
                FileBytes("-Y 16384 +X 16384", red),
                "#?RADIANCE\nFORMAT=32-bit_rle_xyze\n\n-Y 1 +X 1\n"u8.ToArray(),
            })
            {
                File.WriteAllBytes(path, bad);
                RejectInvalid(() => image.Load(path));
            }
            Check(changes == 0 && image.GetData().SequenceEqual(previous),
                "Malformed headers and scanlines leave the current image and event count intact.");
        }
        finally { folder.Delete(recursive: true); }

        Console.WriteLine("Radiance HDR loading, orientation, scanline formats, texture discovery and failure atomicity passed.");
    }

    private static byte[] FileBytes(string resolution, byte[] body, bool alternate = false)
    {
        var signature = alternate ? "#?RGBE" : "#?RADIANCE";
        return [.. Encoding.ASCII.GetBytes($"{signature}\r\n# Fixture\r\nFORMAT=32-bit_rle_rgbe\r\n\r\n{resolution}\r\n"), .. body];
    }

    private static void RejectInvalid(Action load)
    {
        try { load(); }
        catch (InvalidDataException) { return; }
        throw new InvalidOperationException("Malformed Radiance file was accepted.");
    }

    private static void Check(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException(message);
    }
}
