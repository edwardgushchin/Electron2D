using System.Buffers.Binary;
using Electron2D;

internal static class ImageRGBETests
{
    internal static void Run()
    {
        // The 3x2 base has one complete 2x2 footprint and one odd trailing column.
        // Exponent 16 makes the mantissa unit 1/256. The authored 1x1 level is green.
        var source = new byte[7 * sizeof(uint)];
        BinaryPrimitives.WriteUInt32LittleEndian(source.AsSpan(0), Pack(0, 64, 128, 16));
        BinaryPrimitives.WriteUInt32LittleEndian(source.AsSpan(4), Pack(256, 128, 64, 16));
        BinaryPrimitives.WriteUInt32LittleEndian(source.AsSpan(8), Pack(0, 0, 511, 31));
        BinaryPrimitives.WriteUInt32LittleEndian(source.AsSpan(12), Pack(64, 256, 128, 16));
        BinaryPrimitives.WriteUInt32LittleEndian(source.AsSpan(16), Pack(128, 0, 256, 16));
        BinaryPrimitives.WriteUInt32LittleEndian(source.AsSpan(20), Pack(0, 0, 511, 31));
        BinaryPrimitives.WriteUInt32LittleEndian(source.AsSpan(24), Pack(0, 256, 0, 16));

        // Shared-exponent values 0, .25, .5, 1 and HDR white become 0, 136, 187, 254 and 255.
        byte[] baseRGB =
        [
            0, 136, 187, 254, 187, 136, 0, 0, 255,
            136, 254, 187, 187, 0, 254, 0, 0, 255
        ];
        byte[] expected = [.. baseRGB, 144, 144, 191];
        using var hdr = Image.CreateFromData(3, 2, true, Image.Format.Rgbe9995, source);
        var changed = 0;
        hdr.Changed += _ => changed++;

        using var rgb = hdr.RGBEToSRGB();
        Check(rgb.Size == hdr.Size && rgb.PixelFormat == Image.Format.Rgb8 && rgb.HasMIPMaps &&
            rgb.GetMIPMapOffset(1) == baseRGB.Length && rgb.GetData().AsSpan().SequenceEqual(expected),
            "RGBE base conversion and regenerated RGB8 mip must match the independent byte vector.");
        Check(hdr.GetData().AsSpan().SequenceEqual(source) && changed == 0, "RGBE source and notifications stay untouched.");

        using var texture = ImageTexture.CreateFromImage(rgb);
        rgb.Dispose();
        using var retained = texture.GetImage()!;
        Check(retained.PixelFormat == Image.Format.Rgb8 && retained.HasMIPMaps &&
            retained.GetData().AsSpan().SequenceEqual(expected), "ImageTexture retains the rebuilt chain after the result is disposed.");

        using var plain = Image.CreateFromData(3, 2, false, Image.Format.Rgbe9995, source[..24]);
        using var convertedPlain = plain.RGBEToSRGB();
        Check(!convertedPlain.HasMIPMaps && convertedPlain.GetData().AsSpan().SequenceEqual(baseRGB), "A base-only source yields a base-only result.");

        using var empty = new Image();
        Reject<InvalidOperationException>(() => empty.RGBEToSRGB());
        Reject<InvalidOperationException>(() => convertedPlain.RGBEToSRGB());
        hdr.Dispose();
        Reject<ObjectDisposedException>(() => hdr.RGBEToSRGB());
    }

    private static uint Pack(uint red, uint green, uint blue, uint exponent) =>
        red | (green << 9) | (blue << 18) | (exponent << 27);

    private static void Check(bool value, string message)
    {
        if (!value)
            throw new InvalidOperationException(message);
    }

    private static void Reject<T>(Func<Image> convert) where T : Exception
    {
        try { using var result = convert(); }
        catch (T) { return; }
        throw new InvalidOperationException($"Expected {typeof(T).Name}.");
    }
}
