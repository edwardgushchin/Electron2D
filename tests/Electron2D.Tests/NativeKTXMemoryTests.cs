using System.Buffers.Binary;
using System.Runtime.InteropServices;
using System.Text.Json;
using Electron2D;

internal static unsafe class NativeKTXMemoryTests
{
    internal static void RunProduction()
    {
        const int edge = 256, bytes = edge * edge * 4;
        var source = new byte[68 + bytes];
        byte[] magic = [0xab, 0x4b, 0x54, 0x58, 0x20, 0x31, 0x31, 0xbb, 0x0d, 0x0a, 0x1a, 0x0a];
        magic.CopyTo(source, 0);
        uint[] header = [0x04030201, 0x1401, 1, 0x1908, 0x8058, 0x1908, edge, edge, 0, 0, 1, 1, 0, bytes];
        for (var i = 0; i < header.Length; i++) BinaryPrimitives.WriteUInt32LittleEndian(source.AsSpan(12 + i * 4), header[i]);
        source.AsSpan(68).Fill(127);
        var before = NativeAllocation.Snapshot();
        using (var image = new Image())
        {
            image.LoadKTXFromBuffer(source);
            Check(image.GetData().AsSpan().SequenceEqual(source.AsSpan(68)), "Production raw KTX pixels");
        }
        var retired = NativeAllocation.Snapshot();
        Check(retired.Current == before.Current && retired.Peak >= before.Current + bytes && retired.Peak > before.Peak,
            "Production KTX records an unsampled native peak and retires decoder storage before publication");

        const int lumaEdge = 1024;
        var luminance = RawKTXL8(lumaEdge);
        var lumaBefore = NativeAllocation.Snapshot();
        using (var image = new Image())
        {
            image.LoadKTXFromBuffer(luminance);
            Check(image.PixelFormat == Image.Format.L8 && image.GetData().AsSpan().SequenceEqual(luminance.AsSpan(68)),
                "Production legacy luminance pixels");
        }
        var lumaRetired = NativeAllocation.Snapshot();
        Check(lumaRetired.Current == lumaBefore.Current && lumaRetired.Peak >= lumaBefore.Current + 2 * lumaEdge * lumaEdge,
            "Production legacy normalization records overlapping C++ scratch and pixels before retirement");

        using var stream = typeof(NativeKTXMemoryTests).Assembly.GetManifestResourceStream("TestKTX.oracle.json")!;
        using var oracle = JsonDocument.Parse(stream);
        foreach (var entry in oracle.RootElement.GetProperty("fixtures").EnumerateArray())
        {
            using var image = new Image();
            image.LoadKTXFromBuffer(entry.GetProperty("encoded").GetBytesFromBase64());
            Check(image.GetData().AsSpan().SequenceEqual(entry.GetProperty("pixels").GetBytesFromBase64()), "Production independent mip pixels");
        }
        var baseline = NativeAllocation.Snapshot().Current;
        using (var gzip = new StreamPeerGZIP())
        {
            gzip.StartCompression();
            var active = NativeAllocation.Snapshot().Current;
            Check(active > baseline, "GZIP shares the production native ledger");
            for (var iteration = 0; iteration < 8; iteration++)
                foreach (var entry in oracle.RootElement.GetProperty("fixtures").EnumerateArray())
                {
                    using var image = new Image();
                    image.LoadKTXFromBuffer(entry.GetProperty("encoded").GetBytesFromBase64());
                    Check(image.GetData().AsSpan().SequenceEqual(entry.GetProperty("pixels").GetBytesFromBase64()) &&
                        NativeAllocation.Snapshot().Current == active, "KTX temporary drain preserves live GZIP ownership");
                }
            gzip.Clear();
        }
        Check(NativeAllocation.Snapshot().Current == baseline, "Production owners retire into the same ledger");
        Console.WriteLine($"Production KTX: raw peak {retired.Peak}, legacy C++ peak {lumaRetired.Peak}, nine mip oracles, 72 repeated loads with overlapping GZIP ownership passed.");
    }

    internal static void Run()
    {
        if (!OperatingSystem.IsLinux()) throw new PlatformNotSupportedException("The KTX allocation-refusal fixture requires Linux.");
        var file = System.IO.Path.Combine(AppContext.BaseDirectory, "runtimes", NativeLibraries.RuntimeRID, "native", "libElectron2D.so");
        var library = NativeLibrary.Load(file);
        try
        {
            var arm = (delegate* unmanaged[Cdecl]<nuint, void>)NativeLibrary.GetExport(library, "e2d_test_arm");
            var attempts = (delegate* unmanaged[Cdecl]<nuint>)NativeLibrary.GetExport(library, "e2d_test_attempts");
            Run(arm, attempts);
        }
        finally { NativeLibrary.Free(library); }
    }

    private static void Run(delegate* unmanaged[Cdecl]<nuint, void> arm, delegate* unmanaged[Cdecl]<nuint> attempts)
    {
        using var stream = typeof(NativeKTXMemoryTests).Assembly.GetManifestResourceStream("TestKTX.oracle.json")!;
        using var oracle = JsonDocument.Parse(stream);
        nuint refusals = 0;
        foreach (var entry in oracle.RootElement.GetProperty("fixtures").EnumerateArray())
        {
            var name = entry.GetProperty("name").GetString()!;
            var source = entry.GetProperty("encoded").GetBytesFromBase64();
            var pristine = source.ToArray();
            var expected = entry.GetProperty("pixels").GetBytesFromBase64();
            using (var warm = new Image()) warm.LoadKTXFromBuffer(source);
            var baseline = NativeAllocation.Snapshot().Current;
            nuint requests;
            using (var ready = new Image())
            {
                arm(0); ready.LoadKTXFromBuffer(source); requests = attempts();
                Check(ready.GetData().AsSpan().SequenceEqual(expected), "Independent pixels before refusal: " + name);
                Check(NativeAllocation.Snapshot().Current == baseline, "Native decoder storage retires before Image publication: " + name);
            }
            using var previous = Image.CreateFromData(2, 2, false, Image.Format.Rgba8, new byte[16]);
            var old = previous.GetData(); var changes = 0;
            previous.Changed += _ => changes++;
            for (nuint failure = 1; failure <= requests; failure++)
            {
                arm(failure); var rejected = false;
                try { previous.LoadKTXFromBuffer(source); }
                catch (IOException) { rejected = true; }
                finally { arm(0); }
                Check(rejected, "Native allocation refusal projects IOException: " + name);
                Check(previous.Width == 2 && previous.Height == 2 && previous.MIPMapCount == 0 && previous.PixelFormat == Image.Format.Rgba8 &&
                    previous.GetData().AsSpan().SequenceEqual(old) && changes == 0 && source.AsSpan().SequenceEqual(pristine),
                    "Refusal preserves the public Image, source and Changed boundary: " + name);
                Check(NativeAllocation.Snapshot().Current == baseline, "Refusal retires every temporary native allocation: " + name);
                using var retry = new Image(); retry.LoadKTXFromBuffer(source);
                Check(retry.GetData().AsSpan().SequenceEqual(expected) && NativeAllocation.Snapshot().Current == baseline,
                    "A healthy retry retains complete independent mip pixels: " + name);
                refusals++;
            }
            previous.LoadKTXFromBuffer(source);
            Check(changes == 1 && previous.GetData().AsSpan().SequenceEqual(expected), "A healthy load publishes once after refusal: " + name);
        }
        Console.WriteLine($"KTX public Image: {refusals} native refusals preserve pixels/metadata/source/Changed, drain storage and retry all mip oracles.");
        RunWorkingStorage(arm, attempts);
    }

    private static void RunWorkingStorage(delegate* unmanaged[Cdecl]<nuint, void> arm, delegate* unmanaged[Cdecl]<nuint> attempts)
    {
        using var stream = typeof(NativeKTXMemoryTests).Assembly.GetManifestResourceStream("TestBlocks.oracle.json")!;
        using var oracle = JsonDocument.Parse(stream);
        nuint blockRefusals = 0, encoderRefusals = 0;
        var luminance = RawKTXL8(8);
        var legacyRefusals = CheckOperation("Legacy luminance normalization", () => Image.CreateFromData(2, 2, false, Image.Format.Rgba8, new byte[16]),
            image => image.LoadKTXFromBuffer(luminance), arm, attempts, luminance.AsSpan(68).ToArray(), Image.Format.L8);
        foreach (var entry in oracle.RootElement.GetProperty("fixtures").EnumerateArray())
        {
            var source = entry.GetProperty("encoded").GetBytesFromBase64();
            var expected = entry.GetProperty("pixels").GetBytesFromBase64();
            var format = Enum.Parse<Image.Format>(entry.GetProperty("source_format").GetString()!);
            var decoded = Enum.Parse<Image.Format>(entry.GetProperty("format").GetString()!);
            blockRefusals += CheckOperation(entry.GetProperty("name").GetString()!,
                () => Image.CreateFromData(7, 5, true, format, source), image => image.Decompress(), arm, attempts, expected, decoded);
        }
        (Image.CompressMode Mode, Image.UsedChannels Channels, Image.ASTCFormat Footprint)[] modes = [
            (Image.CompressMode.Etc, Image.UsedChannels.Rgb, Image.ASTCFormat.Format4X4),
            (Image.CompressMode.Etc2, Image.UsedChannels.Rgb, Image.ASTCFormat.Format4X4),
            (Image.CompressMode.Etc2, Image.UsedChannels.Rgba, Image.ASTCFormat.Format4X4),
            (Image.CompressMode.Etc2, Image.UsedChannels.Red, Image.ASTCFormat.Format4X4),
            (Image.CompressMode.Etc2, Image.UsedChannels.RedGreen, Image.ASTCFormat.Format4X4),
            (Image.CompressMode.Astc, Image.UsedChannels.Rgba, Image.ASTCFormat.Format4X4),
            (Image.CompressMode.Astc, Image.UsedChannels.Rgba, Image.ASTCFormat.Format8X8)];
        foreach (var mode in modes)
            encoderRefusals += CheckOperation(mode.ToString(), () => WorkingSource(false),
                image => image.CompressFromChannels(mode.Mode, mode.Channels, mode.Footprint), arm, attempts);
        foreach (var footprint in new[] { Image.ASTCFormat.Format4X4, Image.ASTCFormat.Format8X8 })
            encoderRefusals += CheckOperation("HDR " + footprint, () => WorkingSource(true),
                image => image.Compress(Image.CompressMode.Astc, Image.CompressSource.Generic, footprint), arm, attempts);
        Console.WriteLine($"Public Image working storage: {legacyRefusals} normalization, {blockRefusals} block and {encoderRefusals} encoder refusals preserve all mips/metadata/Changed, drain storage and retry.");
        RunPortableStorage(arm, attempts);
    }

    private static void RunPortableStorage(delegate* unmanaged[Cdecl]<nuint, void> arm, delegate* unmanaged[Cdecl]<nuint> attempts)
    {
        nuint total = 0;
        foreach (var hdr in new[] { false, true })
            foreach (var mode in new[] { PortableCompressedTexture.CompressionMode.BasisUniversal,
                PortableCompressedTexture.CompressionMode.ETC2, PortableCompressedTexture.CompressionMode.ASTC })
            {
                if (hdr && mode == PortableCompressedTexture.CompressionMode.ETC2) continue;
                using var source = WorkingSource(hdr); var originalSource = source.GetData();
                using var texture = new PortableCompressedTexture { KeepCompressedBuffer = true };
                texture.SetBASISUCompressorParams(0, 0);
                var data = (PropertyDescriptor<PortableCompressedTexture, byte[]>)texture.GetPropertyList().Single(p => p.Name == "portable_data");
                texture.CreateFromImage(source, mode);
                using var healthy = texture.GetImage()!;
                var expected = healthy.GetData(); var expectedFormat = healthy.PixelFormat; var encoded = data.GetValue(texture);
                arm(0); texture.CreateFromImage(source, mode); var requests = attempts();
                using var oldSource = Image.CreateFromData(2, 2, false, Image.Format.Rgba8, new byte[16]);
                texture.CreateFromImage(oldSource, PortableCompressedTexture.CompressionMode.ETC2);
                texture.SizeOverride = new(9, 7);
                using var old = texture.GetImage()!;
                var oldPixels = old.GetData(); var oldEncoded = data.GetValue(texture); var oldAlpha = texture.HasAlpha;
                var identity = texture.GetRID(); var changes = 0; var properties = 0;
                texture.Changed += _ => changes++; texture.PropertyListChanged += _ => properties++;
                var baseline = NativeAllocation.Snapshot().Current;
                for (nuint failure = 1; failure <= requests; failure++)
                {
                    arm(failure); var rejected = false;
                    try { texture.CreateFromImage(source, mode); }
                    catch (IOException) { rejected = true; }
                    finally { arm(0); }
                    using var preserved = texture.GetImage()!;
                    Check(rejected && texture.GetSize() == new Vector2(2, 2) && texture.SizeOverride == new Vector2(9, 7) &&
                        texture.GetRID() == identity && texture.GetCompressionMode() == PortableCompressedTexture.CompressionMode.ETC2 &&
                        texture.MIPMapCount == 0 && texture.HasAlpha == oldAlpha && changes == 0 && properties == 0 &&
                        preserved.PixelFormat == old.PixelFormat && preserved.GetData().AsSpan().SequenceEqual(oldPixels) &&
                        data.GetValue(texture).AsSpan().SequenceEqual(oldEncoded) && source.GetData().AsSpan().SequenceEqual(originalSource) &&
                        NativeAllocation.Snapshot().Current == baseline, "Portable refusal preserves published state and retires working storage: " + mode);
                    using var retry = new PortableCompressedTexture { KeepCompressedBuffer = true };
                    retry.SetBASISUCompressorParams(0, 0); retry.CreateFromImage(source, mode);
                    using var pixels = retry.GetImage()!;
                    Check(pixels.PixelFormat == expectedFormat && pixels.GetData().AsSpan().SequenceEqual(expected) &&
                        data.GetValue(retry).AsSpan().SequenceEqual(encoded) && NativeAllocation.Snapshot().Current == baseline,
                        "Portable refusal permits byte-identical retry: " + mode);
                }
                texture.CreateFromImage(source, mode);
                using var published = texture.GetImage()!;
                Check(texture.GetRID() == identity && texture.GetSize() == new Vector2(4, 4) && texture.SizeOverride == new Vector2(4, 4) &&
                    texture.MIPMapCount == 2 && texture.GetCompressionMode() == mode && changes == 1 && properties == 1 &&
                    published.PixelFormat == expectedFormat && published.GetData().AsSpan().SequenceEqual(expected) &&
                    data.GetValue(texture).AsSpan().SequenceEqual(encoded) && NativeAllocation.Snapshot().Current == baseline,
                    "Portable creation publishes complete mips once after refusal: " + mode);
                total += requests;
            }
        Console.WriteLine($"Public portable texture: {total} Basis/ETC2/ASTC LDR/HDR refusals preserve RID/buffer/mips/metadata/events, drain storage and retry.");
    }

    private static byte[] RawKTXL8(int edge)
    {
        var data = new byte[68 + edge * edge];
        byte[] magic = [0xab, 0x4b, 0x54, 0x58, 0x20, 0x31, 0x31, 0xbb, 0x0d, 0x0a, 0x1a, 0x0a]; magic.CopyTo(data, 0);
        uint[] header = [0x04030201, 0x1401, 1, 0x1909, 0x1909, 0x1909, (uint)edge, (uint)edge, 0, 0, 1, 1, 0, (uint)(edge * edge)];
        for (var i = 0; i < header.Length; i++) BinaryPrimitives.WriteUInt32LittleEndian(data.AsSpan(12 + i * 4), header[i]);
        data.AsSpan(68).Fill(127); return data;
    }

    private static Image WorkingSource(bool hdr)
    {
        var data = new byte[21 * (hdr ? 16 : 4)];
        for (var pixel = 0; pixel < 21; pixel++)
            if (hdr)
            {
                float[] channels = [.25f, .5f, 1, .25f];
                for (var channel = 0; channel < 4; channel++) BinaryPrimitives.WriteSingleLittleEndian(data.AsSpan(pixel * 16 + channel * 4), channels[channel]);
            }
            else { data[pixel * 4] = 224; data[pixel * 4 + 1] = 64; data[pixel * 4 + 2] = 32; data[pixel * 4 + 3] = 255; }
        return Image.CreateFromData(4, 4, true, hdr ? Image.Format.Rgbaf : Image.Format.Rgba8, data);
    }

    private static nuint CheckOperation(string name, Func<Image> source, Action<Image> operation,
        delegate* unmanaged[Cdecl]<nuint, void> arm, delegate* unmanaged[Cdecl]<nuint> attempts,
        byte[]? independent = null, Image.Format? independentFormat = null)
    {
        using var healthy = source(); operation(healthy);
        var expected = healthy.GetData(); var format = healthy.PixelFormat;
        var expectedWidth = healthy.Width; var expectedHeight = healthy.Height; var expectedMips = healthy.MIPMapCount;
        Check(independent is null || expected.AsSpan().SequenceEqual(independent) && format == independentFormat,
            "Independent block pixels: " + name);
        var baseline = NativeAllocation.Snapshot().Current;
        nuint requests;
        using (var calibrated = source()) { arm(0); operation(calibrated); requests = attempts(); }
        Check(NativeAllocation.Snapshot().Current == baseline, "Healthy working storage retires: " + name);
        using var previous = source(); var original = previous.GetData(); var oldFormat = previous.PixelFormat;
        var width = previous.Width; var height = previous.Height; var mips = previous.MIPMapCount; var changes = 0;
        previous.Changed += _ => changes++;
        for (nuint failure = 1; failure <= requests; failure++)
        {
            arm(failure); var rejected = false;
            try { operation(previous); }
            catch (IOException) { rejected = true; }
            finally { arm(0); }
            Check(rejected && previous.Width == width && previous.Height == height && previous.MIPMapCount == mips &&
                previous.PixelFormat == oldFormat && previous.GetData().AsSpan().SequenceEqual(original) && changes == 0,
                "Working allocation refusal preserves the Image: " + name);
            Check(NativeAllocation.Snapshot().Current == baseline, "Refused working storage retires: " + name);
            using var retry = source(); operation(retry);
            Check(retry.Width == expectedWidth && retry.Height == expectedHeight && retry.MIPMapCount == expectedMips &&
                retry.PixelFormat == format && retry.GetData().AsSpan().SequenceEqual(expected) && NativeAllocation.Snapshot().Current == baseline,
                "Working allocation retry: " + name);
        }
        operation(previous);
        Check(changes == 1 && previous.Width == expectedWidth && previous.Height == expectedHeight && previous.MIPMapCount == expectedMips &&
            previous.PixelFormat == format && previous.GetData().AsSpan().SequenceEqual(expected),
            "Working operation publishes once: " + name);
        return requests;
    }

    private static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
}
