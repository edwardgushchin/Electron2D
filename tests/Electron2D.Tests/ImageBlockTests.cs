using System.Diagnostics;
using System.Text.Json;
using Electron2D;
using Path = System.IO.Path;

internal static partial class ImageBlockTests
{
    private static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    private static void Reject<T>(Action action) where T : Exception { try { action(); } catch (T) { return; } throw new InvalidOperationException("Expected " + typeof(T).Name); }
    private static JsonDocument Oracle() => JsonDocument.Parse(typeof(ImageBlockTests).Assembly.GetManifestResourceStream("TestBlocks.oracle.json")!);
    private static Image Source(JsonElement entry) => Image.CreateFromData(7, 5, true, Enum.Parse<Image.Format>(entry.GetProperty("source_format").GetString()!), entry.GetProperty("encoded").GetBytesFromBase64());
    private static Image Expected(JsonElement entry) => Image.CreateFromData(7, 5, true, Enum.Parse<Image.Format>(entry.GetProperty("format").GetString()!), entry.GetProperty("pixels").GetBytesFromBase64());

    internal static void RunManaged()
    {
        using var document = Oracle();
        var directory = Path.Combine(Path.GetTempPath(), "electron2d-blocks-" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(directory);
        try
        {
            foreach (var entry in document.RootElement.GetProperty("fixtures").EnumerateArray())
            {
                var name = entry.GetProperty("name").GetString()!;
                using var image = Source(entry); using var expected = Expected(entry);
                var encoded = image.GetData(); var changes = 0; image.Changed += _ => changes++;
                using var texture = ImageTexture.CreateFromImage(image); using var copy = texture.GetImage()!;
                Check(copy.PixelFormat == image.PixelFormat && copy.GetData().AsSpan().SequenceEqual(encoded), "Texture retains original blocks: " + name);
                var prepared = texture.CapturePixels()!;
                using (var upload = Image.CreateFromData(7, 5, true, prepared.Upload.Format, prepared.Upload.Data))
                using (var converted = (Image)expected.Duplicate())
                {
                    converted.Convert(upload.PixelFormat);
                    Check(upload.GetData().AsSpan().SequenceEqual(converted.GetData()), "Prepared sampling includes every decoded mip: " + name);
                }
                for (var y = 0; y < 5; ++y) for (var x = 0; x < 7; ++x)
                        Check(texture.IsPixelOpaque(x, y) == (expected.GetPixel(x, y).A > .1f), "Compressed cached alpha: " + name);
                using var array = new TextureArray(); array.CreateFromImages([image, image]); array.UpdateLayer(image, 1);
                using (var layer = array.GetLayerData(1)!) Check(layer.PixelFormat == image.PixelFormat && layer.GetData().AsSpan().SequenceEqual(encoded), "Array preserves source layers: " + name);
                var path = Path.Combine(directory, name + ".ctexarray"); File.WriteAllBytes(path, CompressedArrayTests.Container([(image, 0, null), (image, 0, null)]));
                using var imported = new CompressedTextureArray(); imported.Load(path);
                using (var layer = imported.GetLayerData(0)!) Check(layer.GetData().AsSpan().SequenceEqual(encoded) && imported.HasMIPMaps(), "Imported block array sampling: " + name);
                using (var loaded = ResourceLoader.Load<CompressedTextureArray>(path, ResourceLoader.CacheMode.Ignore))
                    Check(loaded.GetLayers() == 2 && loaded.GetFormat() == image.PixelFormat, "Resource factory loads block array: " + name);
                ResourceSaver.Save(texture, Path.Combine(directory, name + ".texture.e2dres"));
                ResourceSaver.Save(array, Path.Combine(directory, name + ".array.e2dres"));
                ResourceSaver.Save(imported, Path.Combine(directory, name + ".imported.e2dres"));
                if (image.PixelFormat != Image.Format.Etc2RaAsRg) Transformations(image, expected);
                if (expected.PixelFormat == Image.Format.Rgba8)
                {
                    using var webp = new Image(); webp.LoadWebPFromBuffer(image.SaveWebPToBuffer());
                    using var expectedWebP = new Image(); expectedWebP.LoadWebPFromBuffer(expected.SaveWebPToBuffer());
                    Check(webp.GetData().AsSpan().SequenceEqual(expectedWebP.GetData()), "WebP accepts blocks without changing its source: " + name);
                }
                Check(changes == 0 && image.GetData().AsSpan().SequenceEqual(encoded), "Consumers do not mutate source: " + name);
                image.Decompress();
                Check(changes == 1 && image.PixelFormat == expected.PixelFormat && image.MIPMapCount == 2 && image.GetData().AsSpan().SequenceEqual(expected.GetData()), "Independent complete-chain decoder parity: " + name);
                image.Decompress(); Check(changes == 1, "Repeated decode is silent.");
            }
            var info = new ProcessStartInfo("dotnet") { UseShellExecute = false };
            foreach (var argument in new[] { typeof(ImageBlockTests).Assembly.Location, "--image-block-archive", directory }) info.ArgumentList.Add(argument);
            using var child = Process.Start(info)!; child.WaitForExit(); Check(child.ExitCode == 0, "Fresh process restores all original block roles.");
            Failure();
        }
        finally { Directory.Delete(directory, true); }
        Console.WriteLine("ETC1/ETC2/EAC/ASTC: 13 independent mip oracles, precision/alpha, textures/arrays/imports, KTX transforms, WebP, fresh archives and atomic failures passed.");
    }

    private static void Transformations(Image source, Image expected)
    {
        using var raw = new Image(); raw.LoadKTXFromBuffer(ImageKTXTests.KTX1(source));
        Check(raw.PixelFormat == source.PixelFormat && raw.GetData().AsSpan().SequenceEqual(source.GetData()), "KTX keeps identity-oriented blocks.");
        using var flipped = (Image)expected.Duplicate(); flipped.FlipX(); flipped.FlipY();
        using var decoded = new Image(); decoded.LoadKTXFromBuffer(ImageKTXTests.KTX1(source, "KTXorientation", "S=l,T=u"));
        Check(decoded.GetData().AsSpan().SequenceEqual(flipped.GetData()), "KTX decodes and flips all block mip levels.");
        using var swizzled = new Image(); swizzled.LoadKTXFromBuffer(ImageKTXTests.KTX1(source, "KTXswizzle", "bg01"));
        Check(swizzled.GetPixel(0, 0) == new Color(expected.GetPixel(0, 0).B, expected.GetPixel(0, 0).G, 0, 1), "KTX block channel swizzle.");
    }

    private static void Failure()
    {
        // ETC1 differential red underflow is invalid; the second mip fails after a valid base level.
        using var previous = Image.CreateEmpty(7, 5, true, Image.Format.Etc); var bytes = previous.GetData(); bytes[previous.GetMIPMapOffset(1)] = 4; bytes[previous.GetMIPMapOffset(1) + 3] = 2;
        using var invalid = Image.CreateFromData(7, 5, true, Image.Format.Etc, bytes); var changes = 0; invalid.Changed += _ => changes++;
        Reject<InvalidDataException>(() => invalid.Decompress());
        Check(changes == 0 && invalid.IsCompressed && invalid.GetData().AsSpan().SequenceEqual(bytes), "Later mip failure never publishes a partial image.");
        using var valid = Image.CreateEmpty(7, 5, true, Image.Format.Etc);
        using var array = new TextureArray(); array.CreateFromImages([valid]); var rid = array.GetRID(); var callbacks = 0; array.Changed += _ => callbacks++;
        Reject<InvalidDataException>(() => array.UpdateLayer(invalid, 0)); Reject<InvalidDataException>(() => array.CreateFromImages([valid, invalid]));
        Check(callbacks == 0 && array.GetRID() == rid && array.GetLayers() == 1, "Array decoder failure preserves prior storage and identity.");
        using var tooLarge = Image.CreateEmpty(2049, 2049, false, Image.Format.Etc); var original = tooLarge.GetData();
        Reject<InvalidOperationException>(() => tooLarge.Decompress()); Check(tooLarge.IsCompressed && tooLarge.GetData().AsSpan().SequenceEqual(original), "Conservative codec budget rejects before publication.");
        using var observed = Image.CreateEmpty(4, 4, false, Image.Format.Etc); observed.Changed += _ => throw new Marker();
        Reject<Marker>(() => observed.Decompress()); Check(!observed.IsCompressed && observed.PixelFormat == Image.Format.Rgba8, "Observer failure keeps committed decoded state.");
        using var malformedASTC = Image.CreateFromData(4, 4, false, Image.Format.Astc4X4Hdr, new byte[16]); malformedASTC.Decompress();
        Check(malformedASTC.PixelFormat == Image.Format.Rgbah && float.IsNaN(malformedASTC.GetPixel(0, 0).R), "ASTC error blocks retain HDR NaN semantics.");
        observed.Dispose(); Reject<ObjectDisposedException>(() => observed.Decompress());
    }

    internal static void RunArchive(string directory)
    {
        using var oracle = Oracle();
        foreach (var entry in oracle.RootElement.GetProperty("fixtures").EnumerateArray())
        {
            var name = entry.GetProperty("name").GetString()!; using var expected = Source(entry);
            using var texture = ResourceLoader.Load<ImageTexture>(Path.Combine(directory, name + ".texture.e2dres"), ResourceLoader.CacheMode.Ignore);
            using var image = texture.GetImage()!; Check(image.PixelFormat == expected.PixelFormat && image.GetData().AsSpan().SequenceEqual(expected.GetData()), "Fresh original compressed texture.");
            using var array = ResourceLoader.Load<TextureArray>(Path.Combine(directory, name + ".array.e2dres"), ResourceLoader.CacheMode.Ignore);
            using var imported = ResourceLoader.Load<CompressedTextureArray>(Path.Combine(directory, name + ".imported.e2dres"), ResourceLoader.CacheMode.Ignore);
            File.Delete(Path.Combine(directory, name + ".ctexarray"));
            using var layer = imported.GetLayerData(1)!;
            Check(array.GetLayers() == 2 && imported.GetLayers() == 2 && layer.GetData().AsSpan().SequenceEqual(expected.GetData()), "Fresh original arrays retain pixels without source I/O.");
        }
    }
    private sealed class Marker : Exception { }
}
