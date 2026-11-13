using System.Buffers.Binary;

namespace Electron2D;

public sealed partial class Image
{
    private static State DecodeTGA(ReadOnlySpan<byte> data)
    {
        if (data.Length < 18)
            throw new InvalidDataException("Truncated TGA header.");

        var type = data[2];
        var encoded = (type & 8) != 0;
        var role = type & ~8;
        var indexed = role == 1;
        var depth = data[16];
        var descriptor = data[17];
        var alphaBits = descriptor & 15;
        var validDepth = role switch
        {
            1 => depth is 8 or 16,
            2 => depth is 15 or 16 or 24 or 32,
            3 => depth is 8 or 16,
            _ => false
        };
        if (!validDepth || data[1] != (indexed ? 1 : 0) || (descriptor & 0xc0) != 0)
            throw new InvalidDataException("Unsupported TGA image layout.");

        var width = BinaryPrimitives.ReadUInt16LittleEndian(data[12..]);
        var height = BinaryPrimitives.ReadUInt16LittleEndian(data[14..]);
        if (width == 0 || height == 0 || (long)width * height * 4 > MaximumEncodedBytes)
            throw new InvalidDataException("Invalid TGA dimensions or decoded pixels exceed 64 MiB.");
        var remaining = (int)width * height;
        var firstColor = BinaryPrimitives.ReadUInt16LittleEndian(data[3..]);
        var colorCount = indexed ? BinaryPrimitives.ReadUInt16LittleEndian(data[5..]) : 0;
        var colorDepth = data[7];
        var colorBytes = (colorDepth + 7) / 8;
        if (indexed && (colorCount == 0 || colorDepth is not (15 or 16 or 24 or 32) ||
            firstColor + colorCount > (1 << depth)))
            throw new InvalidDataException("Invalid TGA palette.");

        var paletteOffset = 18 + data[0];
        var pixelOffset = paletteOffset + colorCount * colorBytes;
        var end = data.Length;
        if (end >= 26 && data[^18..].SequenceEqual("TRUEVISION-XFILE.\0"u8))
        {
            end -= 26;
            var extension = BinaryPrimitives.ReadUInt32LittleEndian(data[end..]);
            var developer = BinaryPrimitives.ReadUInt32LittleEndian(data[(end + 4)..]);
            if (extension > end || developer > end)
                throw new InvalidDataException("Invalid TGA footer offsets.");
            if (extension != 0)
                end = Math.Min(end, (int)extension);
            if (developer != 0)
                end = Math.Min(end, (int)developer);
        }
        var sampleBytes = (depth + 7) / 8;
        var minimumBytes = encoded ? ((remaining + 127) / 128) * (1 + sampleBytes) : remaining * sampleBytes;
        if (pixelOffset > end || minimumBytes > end - pixelOffset)
            throw new InvalidDataException("Truncated TGA palette or pixels.");
        var palette = data[paletteOffset..pixelOffset];
        var source = data[pixelOffset..end];
        var pixels = new byte[remaining * 4];
        var right = (descriptor & 0x10) != 0;
        var top = (descriptor & 0x20) != 0;
        var x = right ? width - 1 : 0;
        var xStart = x;
        var y = top ? 0 : height - 1;
        var xStep = right ? -1 : 1;
        var yStep = top ? 1 : -1;
        while (remaining > 0)
        {
            if (source.IsEmpty)
                throw new InvalidDataException("Truncated TGA pixel packet.");
            var packet = encoded ? source[0] : 0;
            if (encoded)
                source = source[1..];
            var count = encoded ? (packet & 127) + 1 : remaining;
            var repeat = (packet & 128) != 0;
            var bytes = (repeat ? 1 : count) * sampleBytes;
            if (count > remaining || bytes > source.Length)
                throw new InvalidDataException("TGA packet exceeds the available pixels or bytes.");

            uint pixel = 0;
            for (var i = 0; i < count; i++)
            {
                if (!repeat || i == 0)
                {
                    var sample = source[(repeat ? 0 : i * sampleBytes)..];
                    if (indexed)
                    {
                        var index = (depth == 8 ? sample[0] : BinaryPrimitives.ReadUInt16LittleEndian(sample)) - firstColor;
                        if ((uint)index >= colorCount)
                            throw new InvalidDataException("TGA palette index is outside the declared range.");
                        pixel = ReadTGAPixel(palette[(index * colorBytes)..], colorDepth, colorDepth == 16);
                    }
                    else if (role == 3)
                        pixel = (uint)sample[0] * 0x010101u | (uint)(depth == 16 ? sample[1] : 255) << 24;
                    else
                        pixel = ReadTGAPixel(sample, depth, depth == 16 && alphaBits != 0);
                }
                BinaryPrimitives.WriteUInt32LittleEndian(pixels.AsSpan((y * width + x) * 4), pixel);
                x += xStep;
                if ((uint)x >= width)
                {
                    x = xStart;
                    y += yStep;
                }
            }
            remaining -= count;
            source = source[bytes..];
        }
        return new(width, height, Format.Rgba8, false, pixels);
    }

    private static uint ReadTGAPixel(ReadOnlySpan<byte> data, int depth, bool hasAlphaBit)
    {
        if (depth >= 24)
            return data[2] | (uint)data[1] << 8 | (uint)data[0] << 16 | (uint)(depth == 32 ? data[3] : 255) << 24;

        var packed = (uint)BinaryPrimitives.ReadUInt16LittleEndian(data);
        var alpha = !hasAlphaBit || (packed & 0x8000) != 0 ? 0xff000000u : 0u;
        return alpha | ((packed >> 7) & 0xf8) | ((packed << 6) & 0xf800) | ((packed << 19) & 0xf80000);
    }
}
