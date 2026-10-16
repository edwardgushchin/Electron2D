using System.Buffers.Binary;
using System.Text;
using System.Text.Json;
using Electron2D;
using Path = System.IO.Path;

internal static partial class ImageKTXTests
{
    private static void Check(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
    private static void Reject<T>(Action action) where T : Exception { try { action(); } catch (T) { return; } throw new InvalidOperationException("Expected " + typeof(T).Name); }
    internal static void RunManaged()
    {
        using var stream = typeof(ImageKTXTests).Assembly.GetManifestResourceStream("TestKTX.oracle.json")!;
        using var oracle = JsonDocument.Parse(stream);
        var basis = oracle.RootElement.GetProperty("fixtures").EnumerateArray().First(entry => entry.GetProperty("name").GetString() == "basis-lz");
        var basisBytes = basis.GetProperty("encoded").GetBytesFromBase64(); var basisPixels = basis.GetProperty("pixels").GetBytesFromBase64();
        using (var ready = new CountdownEvent(8))
        using (var start = new ManualResetEventSlim())
        {
            var jobs = Enumerable.Range(0, 8).Select(_ => Task.Factory.StartNew(() =>
            {
                ready.Signal(); start.Wait(); using var image = new Image(); image.LoadKTXFromBuffer(basisBytes);
                Check(image.GetData().AsSpan().SequenceEqual(basisPixels), "Concurrent first Basis initialization and owners.");
            }, TaskCreationOptions.LongRunning)).ToArray();
            var allReady = ready.Wait(TimeSpan.FromSeconds(10)); start.Set(); Task.WaitAll(jobs);
            Check(allReady, "Concurrent KTX workers started.");
        }
        foreach (var entry in oracle.RootElement.GetProperty("fixtures").EnumerateArray())
        {
            var name = entry.GetProperty("name").GetString()!;
            var bytes = entry.GetProperty("encoded").GetBytesFromBase64();
            using var image = new Image(); image.LoadKTXFromBuffer(bytes);
            Check(image.Width == 7 && image.Height == 5 && image.MIPMapCount == 2 && image.PixelFormat.ToString() == entry.GetProperty("format").GetString(), "KTX2 oracle layout: " + name);
            Check(image.GetData().AsSpan().SequenceEqual(entry.GetProperty("pixels").GetBytesFromBase64()), "Independent KTX2 decoder parity: " + name);
            if (name.StartsWith("hdr", StringComparison.Ordinal)) Check(image.GetPixel(0, 0).B > 3, "Basis HDR range is retained.");
        }
        using (var previous = Source(Image.Format.Rgba8))
        {
            var bytes = previous.GetData(); var changes = 0; previous.Changed += _ => ++changes;
            foreach (var failure in oracle.RootElement.GetProperty("failures").EnumerateArray())
                Reject<InvalidDataException>(() => previous.LoadKTXFromBuffer(failure.GetProperty("encoded").GetBytesFromBase64()));
            Check(changes == 0 && previous.GetData().AsSpan().SequenceEqual(bytes), "Invalid DFD and Basis codebooks preserve the published image.");
        }
        RawLayouts(); Transformations(); Failure(); FileFlow();
        Console.WriteLine("KTX1/KTX2 raw, Basis LDR/HDR, mips, padding, orientation, swizzle and atomic resource loading checks passed.");
    }

    private static void RawLayouts()
    {
        foreach (var format in new[] { Image.Format.L8, Image.Format.La8, Image.Format.R8, Image.Format.Rg8, Image.Format.Rgb8, Image.Format.Rgba8,
            Image.Format.Rgba4444, Image.Format.Rgb565, Image.Format.Rf, Image.Format.Rgf, Image.Format.Rgbf, Image.Format.Rgbaf,
            Image.Format.Rh, Image.Format.Rgh, Image.Format.Rgbh, Image.Format.Rgbah, Image.Format.Rgbe9995, Image.Format.R16, Image.Format.Rg16,
            Image.Format.Rgb16, Image.Format.Rgba16, Image.Format.R16I, Image.Format.Rg16I, Image.Format.Rgb16I, Image.Format.Rgba16I })
        {
            using var source = Source(format); var bytes = KTX1(source);
            using var image = new Image();
            try { image.LoadKTXFromBuffer(bytes); } catch (Exception error) { throw new InvalidOperationException("KTX1 format " + format, error); }
            Check(image.PixelFormat == format && image.GetData().AsSpan().SequenceEqual(source.GetData()) && image.MIPMapCount == 2, "KTX1 raw channel/mip layout: " + format);
            if (Layout(format).TypeSize is 2 or 4)
            {
                bytes = KTX1(source, bigEndian: true);
                try { image.LoadKTXFromBuffer(bytes); } catch (Exception error) { throw new InvalidOperationException("KTX1 big-endian format " + format, error); }
                Check(image.GetData().AsSpan().SequenceEqual(source.GetData()), "KTX1 opposite-endian channel layout: " + format);
            }
        }
        foreach (var format in new[] { Image.Format.Dxt1, Image.Format.Dxt3, Image.Format.Dxt5, Image.Format.RgtcR, Image.Format.RgtcRg,
            Image.Format.BptcRgba, Image.Format.BptcRgbf, Image.Format.BptcRgbfu, Image.Format.Etc, Image.Format.Etc2R11, Image.Format.Etc2Rg11,
            Image.Format.Etc2Rgb8, Image.Format.Etc2Rgba8, Image.Format.Astc4X4, Image.Format.Astc4X4Hdr, Image.Format.Astc8X8, Image.Format.Astc8X8Hdr })
        {
            using var empty = Image.CreateEmpty(7, 5, true, format); var data = empty.GetData();
            for (var i = 0; i < data.Length; ++i) data[i] = (byte)(i * 17);
            using var source = Image.CreateFromData(7, 5, true, format, data);
            using var loaded = new Image();
            try { loaded.LoadKTXFromBuffer(KTX1(source)); } catch (Exception error) { throw new InvalidOperationException("KTX1 block format " + format, error); }
            Check(loaded.PixelFormat == format && loaded.GetData().AsSpan().SequenceEqual(data), "KTX1 complete raw block storage: " + format);
        }
    }

    private static void Transformations()
    {
        using var source = Source(Image.Format.Rgba8);
        foreach (var x in new[] { false, true }) foreach (var y in new[] { false, true })
            {
                using var expected = (Image)source.Duplicate(); if (x) expected.FlipX(); if (y) expected.FlipY();
                using var image = new Image(); image.LoadKTXFromBuffer(KTX1(source, "KTXorientation", $"S={(x ? 'l' : 'r')},T={(y ? 'u' : 'd')}"));
                Check(image.GetData().AsSpan().SequenceEqual(expected.GetData()), "KTX logical orientation applies to every mip.");
            }
        using var swizzled = new Image(); swizzled.LoadKTXFromBuffer(KTX1(source, "KTXswizzle", "bg01"));
        var color = swizzled.GetPixel(0, 0); var original = source.GetPixel(0, 0);
        Check(color == new Color(original.B, original.G, 0, 1), "KTX channel/constants swizzle.");
        using var srgb = new Image(); srgb.LoadKTXFromBuffer(KTX1(source, srgb: true));
        using var linear = (Image)source.Duplicate(); linear.SRGBToLinear();
        Check(srgb.GetData().AsSpan().SequenceEqual(linear.GetData()), "KTX sRGB conversion retains alpha and every mip.");
    }

    private static void Failure()
    {
        using var source = Source(Image.Format.Rgba8); var bytes = KTX1(source);
        var original = source.GetData(); var changed = 0; source.Changed += _ => ++changed;
        for (var length = 0; length < bytes.Length; ++length) Reject<InvalidDataException>(() => source.LoadKTXFromBuffer(bytes.AsSpan(0, length)));
        foreach (var offset in new[] { 12, 36, 40, 56, 60, 64 })
        {
            var bad = (byte[])bytes.Clone(); BinaryPrimitives.WriteUInt32LittleEndian(bad.AsSpan(offset), uint.MaxValue);
            Reject<InvalidDataException>(() => source.LoadKTXFromBuffer(bad));
        }
        foreach (var offset in new[] { 44, 48, 52 })
        {
            var bad = (byte[])bytes.Clone(); BinaryPrimitives.WriteUInt32LittleEndian(bad.AsSpan(offset), offset == 52 ? 6u : 1u);
            Reject<NotSupportedException>(() => source.LoadKTXFromBuffer(bad));
        }
        Reject<InvalidDataException>(() => source.LoadKTXFromBuffer(KTX1(source, "KTXswizzle", "rgb!")));
        Check(changed == 0 && source.GetData().AsSpan().SequenceEqual(original), "All KTX failures preserve pixels and notifications.");
        source.LoadKTXFromBuffer(bytes); Check(changed == 1, "Successful KTX publication notifies once.");
        source.Changed += _ => throw new ArithmeticException("observer");
        Reject<ArithmeticException>(() => source.LoadKTXFromBuffer(bytes));
        Check(source.GetData().AsSpan().SequenceEqual(original), "Observer failure retains committed KTX pixels.");
        source.Dispose(); Reject<ObjectDisposedException>(() => source.LoadKTXFromBuffer(bytes));
    }

    private static void FileFlow()
    {
        var directory = Path.Combine(Path.GetTempPath(), "electron2d-ktx-" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(directory);
        try
        {
            using var source = Source(Image.Format.Rgba8); var path = Path.Combine(directory, "image.KTX"); File.WriteAllBytes(path, KTX1(source));
            using var image = Image.LoadFromFile(path); Check(image.GetData().AsSpan().SequenceEqual(source.GetData()), "KTX file decoder.");
            Check(ResourceLoader.GetRecognizedExtensionsForType<ImageTexture>().Contains("ktx") && ResourceLoader.GetRecognizedExtensionsForType<ImageTexture>().Contains("ktx2"), "Resource discovery shares the KTX image loader.");
            using var texture = ResourceLoader.Load<ImageTexture>(path); var identity = texture.GetRID();
            using var pixels = texture.GetImage()!; Check(pixels.HasMIPMaps && pixels.GetData().AsSpan().SequenceEqual(source.GetData()), "ResourceLoader preserves source and mips.");
            source.Fill(new(.7f, .2f, .4f, .8f)); File.WriteAllBytes(path, KTX1(source));
            Check(ReferenceEquals(texture, ResourceLoader.Load<ImageTexture>(path, ResourceLoader.CacheMode.Replace)) && texture.GetRID() == identity, "KTX cache replacement preserves resource identity.");
            using var replacement = texture.GetImage()!; Check(replacement.GetData().AsSpan().SequenceEqual(source.GetData()), "KTX replacement refreshes prepared pixels.");
            ResourceLoader.LoadThreadedRequest<ImageTexture>(path, cacheMode: ResourceLoader.CacheMode.Ignore);
            using var threaded = ResourceLoader.LoadThreadedGet<ImageTexture>(path); using var threadedPixels = threaded.GetImage()!;
            Check(threadedPixels.GetData().AsSpan().SequenceEqual(source.GetData()), "KTX file loading runs through the resource worker.");
            File.WriteAllBytes(path, [0]); Reject<InvalidDataException>(() => ResourceLoader.Load<ImageTexture>(path, ResourceLoader.CacheMode.Replace));
            using var retained = texture.GetImage()!; Check(retained.GetData().AsSpan().SequenceEqual(source.GetData()) && texture.GetRID() == identity, "Failed KTX replacement retains cached source and RID.");
        }
        finally { Directory.Delete(directory, true); }
    }

    private static Image Source(Image.Format format)
    {
        var image = Image.CreateEmpty(7, 5, false, format);
        for (var y = 0; y < 5; ++y) for (var x = 0; x < 7; ++x) image.SetPixel(x, y, new(.1f + x * .1f, .15f + y * .1f, .6f, .7f));
        image.GenerateMIPMaps(); return image;
    }

    internal static byte[] KTX1(Image source, string? key = null, string? value = null, bool bigEndian = false, bool srgb = false)
    {
        var layout = Layout(source.PixelFormat); var raw = source.GetData();
        using var stream = new MemoryStream(); using var writer = new BinaryWriter(stream);
        writer.Write(new byte[] { 0xab, 0x4b, 0x54, 0x58, 0x20, 0x31, 0x31, 0xbb, 13, 10, 26, 10 });
        void Word(uint word) { Span<byte> bytes = stackalloc byte[4]; if (bigEndian) BinaryPrimitives.WriteUInt32BigEndian(bytes, word); else BinaryPrimitives.WriteUInt32LittleEndian(bytes, word); writer.Write(bytes); }
        var metadata = key is null ? [] : Encoding.UTF8.GetBytes(key + "\0" + value + "\0"); var padded = (metadata.Length + 3) & ~3;
        Word(0x04030201); Word(layout.Type); Word(layout.TypeSize); Word(layout.Type == 0 ? 0 : source.PixelFormat >= Image.Format.R16I ? layout.Base switch { 0x1903 => 0x8d94u, 0x8227 => 0x8228u, 0x1907 => 0x8d98u, _ => 0x8d99u } : layout.Base); Word(srgb ? 0x8c43u : layout.Internal); Word(layout.Base);
        Word((uint)source.Width); Word((uint)source.Height); Word(0); Word(0); Word(1); Word((uint)source.MIPMapCount + 1); Word(metadata.Length == 0 ? 0u : (uint)padded + 4);
        if (metadata.Length != 0) { Word((uint)metadata.Length); writer.Write(metadata); writer.Write(new byte[padded - metadata.Length]); }
        for (var mip = 0; mip <= source.MIPMapCount; ++mip)
        {
            var width = Math.Max(1, source.Width >> mip); var height = Math.Max(1, source.Height >> mip);
            var offset = source.GetMIPMapOffset(mip); var length = (mip == source.MIPMapCount ? raw.Length : source.GetMIPMapOffset(mip + 1)) - offset;
            var rowBytes = layout.Type == 0 ? length : length / height; var pitch = layout.Type == 0 ? rowBytes : (rowBytes + 3) & ~3;
            var rows = layout.Type == 0 ? 1 : height; Word((uint)(pitch * rows));
            for (var row = 0; row < rows; ++row)
            {
                var data = raw.AsSpan(offset + row * rowBytes, rowBytes).ToArray();
                if (bigEndian && layout.TypeSize > 1) for (var i = 0; i < data.Length; i += (int)layout.TypeSize) data.AsSpan(i, (int)layout.TypeSize).Reverse();
                writer.Write(data); writer.Write(new byte[pitch - rowBytes]);
            }
        }
        return stream.ToArray();
    }

    private static (uint Internal, uint Type, uint TypeSize, uint Base) Layout(Image.Format format)
    {
        var index = (int)format;
        uint[] raw = [0x1909, 0x190a, 0x8229, 0x822b, 0x8051, 0x8058, 0x8056, 0x8d62, 0x822e, 0x8230, 0x8815, 0x8814, 0x822d, 0x822f, 0x881b, 0x881a, 0x8c3d];
        uint[] blocks = [0x83f0, 0x83f2, 0x83f3, 0x8dbb, 0x8dbd, 0x8e8c, 0x8e8e, 0x8e8f, 0x8d64, 0x9270, 0x9271, 0x9272, 0x9273, 0x9274, 0x9278, 0x9276, 0, 0, 0x93d0, 0x93b0, 0x93d7, 0x93b7];
        if (index is >= 17 and <= 38) return (blocks[index - 17], 0, 1, format is Image.Format.Dxt1 or Image.Format.Etc or Image.Format.Etc2Rgb8 or Image.Format.BptcRgbf or Image.Format.BptcRgbfu ? 0x1907u : 0x1908u);
        uint[] words = [0x822a, 0x822c, 0x8054, 0x805b, 0x8234, 0x823a, 0x8d77, 0x8d76];
        var type = index switch { <= 5 => 0x1401u, 6 => 0x8033u, 7 => 0x8363u, >= 8 and <= 11 => 0x1406u, >= 12 and <= 15 => 0x140bu, 16 => 0x8c3eu, _ => 0x1403u };
        var typeSize = index <= 5 ? 1u : index is >= 8 and <= 11 or 16 ? 4u : 2u;
        var baseFormat = format switch
        {
            Image.Format.L8 => 0x1909u,
            Image.Format.La8 => 0x190au,
            Image.Format.R8 or Image.Format.Rf or Image.Format.Rh or Image.Format.R16 or Image.Format.R16I => 0x1903u,
            Image.Format.Rg8 or Image.Format.Rgf or Image.Format.Rgh or Image.Format.Rg16 or Image.Format.Rg16I => 0x8227u,
            Image.Format.Rgb8 or Image.Format.Rgb565 or Image.Format.Rgbf or Image.Format.Rgbh or Image.Format.Rgbe9995 or Image.Format.Rgb16 or Image.Format.Rgb16I => 0x1907u,
            _ => 0x1908u
        };
        return (index < 17 ? raw[index] : words[index - 39], type, typeSize, baseFormat);
    }
}
