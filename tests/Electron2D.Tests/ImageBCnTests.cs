using System.Buffers.Binary;
using Electron2D;
internal static partial class ImageBCnTests
{
    private static void Check(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
    private static void Reject<T>(Action action) where T : Exception { try { action(); } catch (T) { return; } throw new InvalidOperationException("Expected " + typeof(T).Name); }
    private static Image Source(Image.Format format = Image.Format.Rgba8, bool mips = true)
    {
        var image = Image.CreateEmpty(7, 5, false, format); image.Fill(new(.2f, .4f, .8f, .7f)); if (mips) image.GenerateMIPMaps(); return image;
    }
    internal static void RunManaged()
    {
        foreach (var mode in new[] { Image.CompressMode.S3tc, Image.CompressMode.Bptc }) foreach (var channels in Enum.GetValues<Image.UsedChannels>())
            {
                using var image = Source(); image.CompressFromChannels(mode, channels); var raw = image.GetData(); var changes = 0; image.Changed += _ => changes++;
                var bytes = image.SaveDDSToBuffer(); using var loaded = new Image(); loaded.LoadDDSFromBuffer(bytes);
                Check(loaded.PixelFormat == image.PixelFormat && loaded.GetData().AsSpan().SequenceEqual(raw) && loaded.HasMIPMaps && loaded.Width == 7 && loaded.Height == 5, "DDS preserves compressed payload and complete ragged mip layout.");
                using var texture = ImageTexture.CreateFromImage(loaded); using var copy = texture.GetImage()!; Check(copy.PixelFormat == image.PixelFormat && copy.GetData().AsSpan().SequenceEqual(raw) && texture.IsPixelOpaque(0, 0), "Compressed source, readback and cached opacity.");
                loaded.Decompress(); Check(!loaded.IsCompressed && loaded.HasMIPMaps && loaded.GetPixel(0, 0).R > .1f, "Decompression produces usable CPU pixels.");
                image.Decompress(); Check(changes == 1 && image.GetData().AsSpan().SequenceEqual(loaded.GetData()), "Atomic decoder publication."); image.Decompress(); Check(changes == 1, "Uncompressed decompression is silent.");
            }
        foreach (var format in new[] { Image.Format.L8, Image.Format.La8, Image.Format.R8, Image.Format.Rg8, Image.Format.Rgb8, Image.Format.Rgba8, Image.Format.Rgb565, Image.Format.Rgba4444, Image.Format.Rf, Image.Format.Rgf, Image.Format.Rgbf, Image.Format.Rgbaf, Image.Format.Rh, Image.Format.Rgh, Image.Format.Rgbah, Image.Format.R16, Image.Format.Rg16, Image.Format.Rgba16, Image.Format.R16I, Image.Format.Rg16I, Image.Format.Rgba16I, Image.Format.Rgbe9995 })
        {
            using var image = Source(format); var bytes = image.SaveDDSToBuffer(); using var loaded = new Image(); loaded.LoadDDSFromBuffer(bytes);
            Check(loaded.PixelFormat == format && loaded.GetData().AsSpan().SequenceEqual(image.GetData()), "Exact raw DDS source role " + format);
            Check(loaded.GetPixel(0, 0) == image.GetPixel(0, 0), "Exact raw DDS color layout " + format);
        }
        foreach (var negative in new[] { false, true })
        {
            using var hdr = Source(Image.Format.Rgbf); hdr.Fill(new(negative ? -.2f : .2f, 1.5f, 3.5f)); hdr.GenerateMIPMaps(); hdr.CompressFromChannels(Image.CompressMode.Bptc, Image.UsedChannels.Rgb);
            Check(hdr.PixelFormat == (negative ? Image.Format.BptcRgbf : Image.Format.BptcRgbfu), "HDR sign role."); hdr.Decompress(); var pixel = hdr.GetPixel(0, 0);
            Check(hdr.PixelFormat == Image.Format.Rgbf && MathF.Abs(pixel.G - 1.5f) < .1f && MathF.Abs(pixel.B - 3.5f) < .2f && (!negative || pixel.R < 0), "HDR dynamic range and sign.");
        }
        DDSLayouts(); Oracle(); Failure(); FileFlow(); Console.WriteLine("BC1–BC7, DDS raw/HDR/mips, source storage and atomic failure checks passed.");
    }
    private static void Failure()
    {
        using var image = Source(); var raw = image.GetData(); var changes = 0; image.Changed += _ => changes++;
        Reject<ArgumentOutOfRangeException>(() => image.Compress((Image.CompressMode)99));
        Check(changes == 0 && image.GetData().AsSpan().SequenceEqual(raw), "Invalid compression mode preserves state.");
        var bytes = image.SaveDDSToBuffer();
        foreach (var at in new[] { 0, 1, 127, 128, 147, bytes.Length - 1 }) { Reject<InvalidDataException>(() => image.LoadDDSFromBuffer(bytes.AsSpan(0, at))); Check(changes == 0, "Truncated source is never published."); }
        foreach (var field in new[] { 4, 12, 16, 76 }) { var bad = (byte[])bytes.Clone(); BinaryPrimitives.WriteUInt32LittleEndian(bad.AsSpan(field), uint.MaxValue); Reject<InvalidDataException>(() => image.LoadDDSFromBuffer(bad)); }
        var invalid = (byte[])bytes.Clone(); BinaryPrimitives.WriteUInt32LittleEndian(invalid.AsSpan(140), 2); Reject<InvalidDataException>(() => image.LoadDDSFromBuffer(invalid));
        invalid = (byte[])bytes.Clone(); BinaryPrimitives.WriteUInt32LittleEndian(invalid.AsSpan(28), 2); Reject<InvalidDataException>(() => image.LoadDDSFromBuffer(invalid));
        Check(image.GetData().AsSpan().SequenceEqual(raw) && changes == 0, "Corrupt metadata preserves source.");
        image.Changed += Fail; Reject<Marker>(() => image.CompressFromChannels(Image.CompressMode.S3tc, Image.UsedChannels.Rgba)); image.Changed -= Fail; Check(image.IsCompressed && changes == 1, "Observer failure preserves committed blocks.");
        Reject<InvalidOperationException>(() => image.Compress(Image.CompressMode.S3tc));
        using var etc = Image.CreateFromData(4, 4, false, Image.Format.Etc, new byte[8]); etc.Decompress(); Check(etc.PixelFormat == Image.Format.Rgba8, "ETC decoder is available independently of BC compression.");
        using var empty = new Image(); Reject<InvalidOperationException>(() => empty.SaveDDSToBuffer()); Reject<InvalidOperationException>(() => empty.Compress(Image.CompressMode.S3tc));
        using var nan = Source(Image.Format.Rgbf, false); nan.SetPixel(0, 0, new(float.NaN, 1, 1)); Reject<InvalidDataException>(() => nan.CompressFromChannels(Image.CompressMode.Bptc, Image.UsedChannels.Rgb));
    }
    private static void FileFlow()
    {
        var directory = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "e2d-bcn-" + Guid.NewGuid()); Directory.CreateDirectory(directory);
        try
        {
            using var image = Source(); image.Compress(Image.CompressMode.S3tc); var path = System.IO.Path.Combine(directory, "image.dds"); image.SaveDDS(path);
            using var loaded = Image.LoadFromFile(path); Check(loaded.IsCompressed && loaded.GetData().AsSpan().SequenceEqual(image.GetData()), "Image file discovery.");
            using var texture = ResourceLoader.Load<ImageTexture>(path, ResourceLoader.CacheMode.Ignore); using var restored = texture.GetImage()!; Check(restored.GetData().AsSpan().SequenceEqual(image.GetData()), "DDS resource consumer.");
            var archive = System.IO.Path.Combine(directory, "texture.e2dres"); ResourceSaver.Save(texture, archive); using var stored = ResourceLoader.Load<ImageTexture>(archive, ResourceLoader.CacheMode.Ignore); using var storedImage = stored.GetImage()!;
            Check(storedImage.IsCompressed && storedImage.GetData().AsSpan().SequenceEqual(image.GetData()), "Compressed source archive.");
            using var array = new TextureArray(); array.CreateFromImages([image, image]); var layered = System.IO.Path.Combine(directory, "array.e2dres"); ResourceSaver.Save(array, layered);
            var info = new System.Diagnostics.ProcessStartInfo(Environment.ProcessPath!) { UseShellExecute = false }; if (System.IO.Path.GetFileNameWithoutExtension(info.FileName).Equals("dotnet", StringComparison.OrdinalIgnoreCase)) info.ArgumentList.Add(typeof(ImageBCnTests).Assembly.Location); foreach (var arg in new[] { "--image-bcn-archive", directory }) info.ArgumentList.Add(arg);
            using var child = System.Diagnostics.Process.Start(info)!; child.WaitForExit(); Check(child.ExitCode == 0, "Fresh process reads DDS and compressed image/array archives.");
            var before = File.ReadAllBytes(path); Reject<DirectoryNotFoundException>(() => image.SaveDDS(System.IO.Path.Combine(directory, "absent", "image.dds")));
            Check(File.ReadAllBytes(path).AsSpan().SequenceEqual(before), "Failed save preserves existing output.");
        }
        finally { Directory.Delete(directory, true); }
    }
    internal static void RunArchive(string directory)
    {
        using var expected = Image.LoadFromFile(System.IO.Path.Combine(directory, "image.dds"));
        using var texture = ResourceLoader.Load<ImageTexture>(System.IO.Path.Combine(directory, "texture.e2dres"), ResourceLoader.CacheMode.Ignore); using var actual = texture.GetImage()!;
        Check(actual.PixelFormat == expected.PixelFormat && actual.GetData().AsSpan().SequenceEqual(expected.GetData()), "Fresh compressed texture archive.");
        using var array = ResourceLoader.Load<TextureArray>(System.IO.Path.Combine(directory, "array.e2dres"), ResourceLoader.CacheMode.Ignore); using var layer = array.GetLayerData(1)!;
        Check(array.GetLayers() == 2 && layer.GetData().AsSpan().SequenceEqual(expected.GetData()), "Fresh compressed layered archive.");
    }
    private sealed class Marker : Exception { }
    private static void Fail(Resource value) => throw new Marker();
}
