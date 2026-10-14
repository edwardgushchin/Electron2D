using System.Buffers.Binary;
using Electron2D;
internal static partial class ImageBCnTests
{
    private static byte[] LegacyDDS(int bits, uint flags, uint r, uint g, uint b, uint a, byte[] data)
    {
        var bytes = new byte[128 + data.Length];
        foreach (var (at, value) in new (int, uint)[] { (0, 0x20534444), (4, 124), (8, 0x1007), (12, 1), (16, 1), (28, 1), (76, 32), (80, flags), (88, (uint)bits), (92, r), (96, g), (100, b), (104, a), (108, 0x1000) }) BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(at), value);
        data.CopyTo(bytes, 128); return bytes;
    }
    private static void DDSLayouts()
    {
        var cases = new (int Bits, uint Flags, uint R, uint G, uint B, uint A, byte[] Data, Image.Format Format, byte[] Expected)[]
        {
            (24,64,0xFF0000,0xFF00,0xFF,0,[3,2,1],Image.Format.Rgb8,[1,2,3]),
            (32,64,0xFF0000,0xFF00,0xFF,0,[3,2,1,99],Image.Format.Rgb8,[1,2,3]),
            (32,64,0xFF,0xFF00,0xFF0000,0,[1,2,3,99],Image.Format.Rgb8,[1,2,3]),
            (32,65,0xFF0000,0xFF00,0xFF,0xFF000000,[3,2,1,4],Image.Format.Rgba8,[1,2,3,4]),
            (32,65,0xFF,0xFF00,0xFF0000,0xFF000000,[1,2,3,4],Image.Format.Rgba8,[1,2,3,4]),
            (16,65,0x7C00,0x3E0,0x1F,0x8000,[0xFF,0xFF],Image.Format.Rgba8,[248,248,248,255]),
            (16,65,0xF00,0xF0,0xF,0xF000,[0x23,0x41],Image.Format.Rgba4444,[0x34,0x12]),
            (8,64,0xE0,0x1C,3,0,[255],Image.Format.Rgb8,[224,224,192]),
            (16,65,0xE0,0x1C,3,0xFF00,[255,88],Image.Format.Rgba8,[224,224,192,88]),
            (32,65,0x3FF,0xFFC00,0x3FF00000,0xC0000000,[255,255,255,255],Image.Format.Rgba8,[255,255,255,255]),
            (32,65,0x3FF00000,0xFFC00,0x3FF,0xC0000000,[255,3,0,64],Image.Format.Rgba8,[0,0,255,64]),
            (8,0x20001,15,0,0,240,[0xA3],Image.Format.La8,[51,170]),
            (8,2,0,0,0,255,[83],Image.Format.L8,[83]),
            (16,64,255,0xFF00,0,0,[3,4],Image.Format.Rg8,[3,4]),
            (32,64,0xFFFF,0xFFFF0000,0,0,[1,2,3,4],Image.Format.Rg16,[1,2,3,4]),
            (16,0x20000,0xFFFF,0,0,0,[1,2],Image.Format.R16,[1,2])
        };
        foreach (var c in cases)
        {
            using var image = new Image(); image.LoadDDSFromBuffer(LegacyDDS(c.Bits, c.Flags, c.R, c.G, c.B, c.A, c.Data));
            Check(image.PixelFormat == c.Format && image.GetData().AsSpan().SequenceEqual(c.Expected), "DDS independent mask fixture " + c.Format);
        }
        foreach (var (code, format, length) in new[] { (36u, Image.Format.Rgba16, 8), (111u, Image.Format.Rh, 2), (112u, Image.Format.Rgh, 4), (113u, Image.Format.Rgbah, 8), (114u, Image.Format.Rf, 4), (115u, Image.Format.Rgf, 8), (116u, Image.Format.Rgbaf, 16) })
        {
            var bytes = LegacyDDS(0, 4, 0, 0, 0, 0, new byte[length]); BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(84), code); using var image = new Image(); image.LoadDDSFromBuffer(bytes); Check(image.PixelFormat == format, "Legacy numeric floating/unorm FourCC.");
        }
        foreach (var (code, data, expected) in new (uint, byte[], byte[])[] { (115, [0x23, 0x41], [0x34, 0x12]), (86, [255, 255], [248, 248, 248, 255]), (24, [255, 255, 255, 255], [255, 255, 255, 255]), (65, [83], [83]) })
        {
            using var image = Image.CreateEmpty(1, 1, false, Image.Format.Rgba8); var bytes = image.SaveDDSToBuffer(); Array.Resize(ref bytes, 148 + data.Length); BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(128), code); data.CopyTo(bytes, 148); image.LoadDDSFromBuffer(bytes); Check(image.GetData().AsSpan().SequenceEqual(expected), "DX10 packed color fixture.");
        }
        foreach (var format in new[] { Image.Format.Dxt1, Image.Format.Dxt3, Image.Format.Dxt5, Image.Format.RgtcR, Image.Format.RgtcRg, Image.Format.BptcRgba, Image.Format.BptcRgbf, Image.Format.BptcRgbfu, Image.Format.Dxt5RaAsRg })
        {
            using var image = BlockSource(format); using var texture = ImageTexture.CreateFromImage(image); using var copy = texture.GetImage()!;
            Check(copy.PixelFormat == format && copy.GetData().AsSpan().SequenceEqual(image.GetData()), "Every BC source snapshot retains exact bytes.");
            if (format == Image.Format.Dxt5RaAsRg) Reject<NotSupportedException>(() => image.SaveDDSToBuffer());
            else { using var loaded = new Image(); loaded.LoadDDSFromBuffer(image.SaveDDSToBuffer()); Check(loaded.GetData().AsSpan().SequenceEqual(image.GetData()), "Every BC DDS role preserves blocks."); }
            image.Decompress(); Check(image.HasMIPMaps && !image.IsCompressed, "Every decoder retains a complete usable chain.");
            if (format == Image.Format.Dxt5RaAsRg) { var color = image.GetPixel(0, 0); Check(color.G > .6f && color.B == 0 && color.A == 1, "RA-as-RG semantic decode."); }
        }
        using var original = Source(); var source = original.SaveDDSToBuffer(); var pixels = original.GetData();
        foreach (var cube in new[] { false, true })
        {
            var layers = cube ? 6 : 2; var bytes = new byte[148 + pixels.Length * layers]; source.AsSpan(0, 148).CopyTo(bytes); for (var layer = 0; layer < layers; layer++) pixels.CopyTo(bytes, 148 + pixels.Length * layer);
            BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(140), cube ? 1u : 2u); if (cube) BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(136), 4);
            using var first = new Image(); first.LoadDDSFromBuffer(bytes); Check(first.GetData().AsSpan().SequenceEqual(pixels), "DDS first complete array/cube image.");
            Reject<InvalidDataException>(() => first.LoadDDSFromBuffer(bytes.AsSpan(0, bytes.Length - 1)));
        }
        // Volume mips interleave all slices of a level; the first slice remains a complete Image chain.
        var volume = new List<byte>(source.AsSpan(0, 148).ToArray()); var width = original.Width; var height = original.Height; var depth = 4;
        for (var mip = 0; mip <= original.MIPMapCount; mip++)
        { var start = original.GetMIPMapOffset(mip); var length = width * height * 4; for (var slice = 0; slice < depth; slice++) volume.AddRange(pixels.AsSpan(start, length).ToArray()); width = Math.Max(1, width / 2); height = Math.Max(1, height / 2); depth = Math.Max(1, depth / 2); }
        var volumeBytes = volume.ToArray(); BinaryPrimitives.WriteUInt32LittleEndian(volumeBytes.AsSpan(24), 4); BinaryPrimitives.WriteUInt32LittleEndian(volumeBytes.AsSpan(132), 4);
        using var selected = new Image(); selected.LoadDDSFromBuffer(volumeBytes); Check(selected.GetData().AsSpan().SequenceEqual(pixels), "First slice of every volume mip.");
        using var aliasTest = new Image(); var owned = (byte[])source.Clone(); aliasTest.LoadDDSFromBuffer(owned); owned.AsSpan().Clear(); Check(aliasTest.GetData().AsSpan().SequenceEqual(pixels), "DDS never aliases caller bytes.");
        using var explicitRgb = Source(); explicitRgb.Fill(new(.2f, .4f, .8f, 0)); explicitRgb.CompressFromChannels(Image.CompressMode.S3tc, Image.UsedChannels.Rgb); explicitRgb.Decompress(); Check(explicitRgb.GetPixel(0, 0).A == 1, "RGB compression discards source alpha.");
        using var large = Image.CreateEmpty(4096, 1025, false, Image.Format.R8); Reject<InvalidOperationException>(() => large.Compress(Image.CompressMode.S3tc)); Check(!large.IsCompressed, "Expanded work budget rejects before publication.");
        using var compressed = Image.CreateEmpty(4096, 1025, false, Image.Format.Dxt1); Reject<InvalidOperationException>(() => compressed.Decompress()); Check(compressed.IsCompressed, "Decoder work budget preserves blocks.");
        Check(!typeof(Image).Assembly.GetExportedTypes().Any(type => type.Namespace?.StartsWith("Electron2DBlockCodecs", StringComparison.Ordinal) == true || type.Namespace?.StartsWith("Electron2DBufferTools", StringComparison.Ordinal) == true), "Vendored codec/buffer types remain private.");
    }
    private static Image BlockSource(Image.Format format, bool alternate = false)
    {
        using var raw = Source(format is Image.Format.BptcRgbf or Image.Format.BptcRgbfu ? Image.Format.Rgbf : Image.Format.Rgba8);
        raw.Fill(format == Image.Format.BptcRgbf ? new(-.2f, 1.5f, 3.5f) : alternate ? new(.8f, .2f, .3f, .5f) : new(.2f, .4f, .8f, .7f)); raw.GenerateMIPMaps();
        raw.CompressFromChannels(format is Image.Format.BptcRgba or Image.Format.BptcRgbf or Image.Format.BptcRgbfu ? Image.CompressMode.Bptc : Image.CompressMode.S3tc,
            format switch { Image.Format.RgtcR => Image.UsedChannels.Red, Image.Format.RgtcRg => Image.UsedChannels.RedGreen, Image.Format.Dxt5 or Image.Format.Dxt5RaAsRg or Image.Format.BptcRgba => Image.UsedChannels.Rgba, _ => Image.UsedChannels.Rgb });
        if (format == Image.Format.Dxt5RaAsRg) return Image.CreateFromData(raw.Width, raw.Height, true, format, raw.GetData());
        if (format != Image.Format.Dxt3) return (Image)raw.Duplicate();
        var colors = raw.GetData(); var blocks = new byte[colors.Length * 2]; for (var block = 0; block < colors.Length / 8; block++) { blocks.AsSpan(block * 16, 8).Fill(0xBB); colors.AsSpan(block * 8, 8).CopyTo(blocks.AsSpan(block * 16 + 8)); }
        return Image.CreateFromData(raw.Width, raw.Height, true, Image.Format.Dxt3, blocks);
    }
    private static void Oracle()
    {
        var oracle = Environment.GetEnvironmentVariable("ELECTRON2D_BCN_ORACLE"); if (oracle == null) return;
        var directory = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "bcn-oracle-" + Guid.NewGuid()); Directory.CreateDirectory(directory);
        try
        {
            foreach (var (format, codec) in new[] { (Image.Format.Dxt1, "Bc1WithAlpha"), (Image.Format.Dxt3, "Bc2"), (Image.Format.Dxt5, "Bc3"), (Image.Format.RgtcR, "Bc4"), (Image.Format.RgtcRg, "Bc5"), (Image.Format.BptcRgba, "Bc7"), (Image.Format.BptcRgbf, "Bc6S"), (Image.Format.BptcRgbfu, "Bc6U") })
            {
                using var image = BlockSource(format); var raw = image.GetData(); using var decoded = (Image)image.Duplicate(); decoded.Decompress(); var result = decoded.GetData(); var w = image.Width; var h = image.Height;
                for (var mip = 0; mip <= image.MIPMapCount; mip++)
                {
                    var input = System.IO.Path.Combine(directory, "input.bin"); var output = System.IO.Path.Combine(directory, "output.bin"); var offset = image.GetMIPMapOffset(mip); var end = mip < image.MIPMapCount ? image.GetMIPMapOffset(mip + 1) : raw.Length; File.WriteAllBytes(input, raw.AsSpan(offset, end - offset).ToArray());
                    var info = new System.Diagnostics.ProcessStartInfo("dotnet") { UseShellExecute = false }; foreach (var arg in new[] { oracle, input, w.ToString(), h.ToString(), codec, output }) info.ArgumentList.Add(arg);
                    using var process = System.Diagnostics.Process.Start(info)!; process.WaitForExit(); Check(process.ExitCode == 0, "Independent package decoder exits."); var actual = File.ReadAllBytes(output); var decodedOffset = decoded.GetMIPMapOffset(mip);
                    Check(result.AsSpan(decodedOffset, actual.Length).SequenceEqual(actual), "Unmodified package decode agrees for " + format + " mip " + mip); w = Math.Max(1, w / 2); h = Math.Max(1, h / 2);
                }
            }
            Console.WriteLine("Unmodified BCnEncoder.Net 2.3.0 package: 24 independent mip decode comparisons passed.");
        }
        finally { Directory.Delete(directory, true); }
    }
}
