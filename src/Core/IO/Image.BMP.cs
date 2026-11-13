using System.Numerics;
using static System.Buffers.Binary.BinaryPrimitives;

namespace Electron2D;

public sealed partial class Image
{
    private static State DecodeBMP(ReadOnlySpan<byte> data)
    {
        const int fileHeader = 14;
        const int infoHeader = 40;
        const uint rgb = 0, rle8 = 1, rle4 = 2, bitFields = 3, alphaBitFields = 6;

        if (data.Length < fileHeader + 12 || !data[..2].SequenceEqual("BM"u8))
            throw new InvalidDataException("Invalid BMP file header.");

        var length = ReadUInt32LittleEndian(data[2..]);
        var offset = ReadUInt32LittleEndian(data[10..]);
        var header = ReadUInt32LittleEndian(data[fileHeader..]);
        if (length == 0)
            length = (uint)data.Length;
        if (length > data.Length || offset > length || offset < fileHeader ||
            header > offset - fileHeader || (header != 12 && header < infoHeader))
            throw new InvalidDataException("Invalid BMP header or pixel bounds.");

        var core = header == 12;
        var width = core ? ReadUInt16LittleEndian(data[18..]) : ReadInt32LittleEndian(data[18..]);
        var signedHeight = core ? ReadUInt16LittleEndian(data[20..]) : ReadInt32LittleEndian(data[22..]);
        var planes = ReadUInt16LittleEndian(data[(core ? 22 : 26)..]);
        var depth = ReadUInt16LittleEndian(data[(core ? 24 : 28)..]);
        var compression = core ? rgb : ReadUInt32LittleEndian(data[30..]);
        var rle = compression is rle4 or rle8;
        var topDown = signedHeight < 0;
        if (planes != 1 || depth is not (1 or 2 or 4 or 8 or 15 or 16 or 24 or 32) ||
            !(compression switch
            {
                rgb => true,
                rle8 => depth == 8 && !topDown,
                rle4 => depth == 4 && !topDown,
                bitFields or alphaBitFields => depth is 16 or 32 && header != 64,
                _ => false
            }))
            throw new InvalidDataException("Unsupported BMP pixel layout.");
        if (width <= 0 || width > MaxWidth || signedHeight == 0 || signedHeight < -MaxHeight || signedHeight > MaxHeight)
            throw new InvalidDataException("Invalid BMP dimensions.");
        var height = Math.Abs(signedHeight);
        var pixelCount = (long)width * height;
        if (pixelCount > MaxPixels || pixelCount * 4 > MaximumEncodedBytes)
            throw new InvalidDataException("Decoded BMP pixels exceed the image limits.");

        var metadataEnd = fileHeader + (int)header;
        var paletteCount = 0u;
        Span<uint> palette = stackalloc uint[256];
        if (depth <= 8)
        {
            var maximum = 1u << depth;
            paletteCount = core ? 0 : ReadUInt32LittleEndian(data[46..]);
            if (paletteCount == 0)
                paletteCount = maximum;
            var entrySize = core ? 3 : 4;
            if (paletteCount > maximum || paletteCount * entrySize > offset - metadataEnd)
                throw new InvalidDataException("Invalid BMP palette length.");
            for (var i = 0; i < paletteCount; i++)
            {
                var color = data[(metadataEnd + i * entrySize)..];
                palette[i] = PackBMPColor(color[2], color[1], color[0], 255);
            }
        }

        var red = new BMPChannel(0x7c00);
        var green = new BMPChannel(0x03e0);
        var blue = new BMPChannel(0x001f);
        var alpha = new BMPChannel(0);
        var masked = compression is bitFields or alphaBitFields;
        if (masked)
        {
            var hasAlphaMask = compression == alphaBitFields || header >= 56;
            if ((header != infoHeader && header < (hasAlphaMask ? 56 : 52)) ||
                offset < fileHeader + infoHeader + (hasAlphaMask ? 16 : 12))
                throw new InvalidDataException("Truncated BMP channel masks.");
            var r = ReadUInt32LittleEndian(data[54..]);
            var g = ReadUInt32LittleEndian(data[58..]);
            var b = ReadUInt32LittleEndian(data[62..]);
            var a = hasAlphaMask ? ReadUInt32LittleEndian(data[66..]) : 0;
            if (r == 0 || g == 0 || b == 0 || (compression == alphaBitFields && a == 0) ||
                (r & g) != 0 || ((r | g) & b) != 0 || ((r | g | b) & a) != 0 ||
                (depth == 16 && (r | g | b | a) > ushort.MaxValue))
                throw new InvalidDataException("Invalid BMP channel masks.");
            red = new BMPChannel(r);
            green = new BMPChannel(g);
            blue = new BMPChannel(b);
            alpha = new BMPChannel(a);
        }

        var source = data[(int)offset..(int)length];
        var stride = ((width * (depth == 15 ? 16 : depth) + 31) / 32) * 4;
        if (!rle && (long)stride * height > source.Length)
            throw new InvalidDataException("Truncated BMP rows.");
        if (rle)
        {
            var compressedSize = ReadUInt32LittleEndian(data[34..]);
            if (compressedSize > source.Length)
                throw new InvalidDataException("Truncated BMP RLE stream.");
            if (compressedSize != 0)
                source = source[..(int)compressedSize];
        }

        var pixels = new byte[(int)pixelCount * 4];
        if (rle)
        {
            DecodeBMPRLE(source, pixels, width, height, depth, palette[..(int)paletteCount]);
            return new(width, height, Format.Rgba8, false, pixels);
        }

        var anyAlpha = false;
        for (var y = 0; y < height; y++)
        {
            var row = source.Slice((topDown ? y : height - 1 - y) * stride, stride);
            for (var x = 0; x < width; x++)
            {
                uint color;
                if (depth <= 8)
                {
                    var bit = x * depth;
                    var index = (row[bit / 8] >> (8 - depth - bit % 8)) & ((1 << depth) - 1);
                    if (index >= paletteCount)
                        throw new InvalidDataException("BMP pixel uses an undefined palette index.");
                    color = palette[index];
                }
                else if (masked || depth <= 16)
                {
                    uint packed = depth == 32 ? ReadUInt32LittleEndian(row[(x * 4)..]) : ReadUInt16LittleEndian(row[(x * 2)..]);
                    color = PackBMPColor(red.Read(packed), green.Read(packed), blue.Read(packed), alpha.Read(packed));
                }
                else
                {
                    var sample = row[(x * (depth / 8))..];
                    var opacity = depth == 32 ? sample[3] : (byte)255;
                    anyAlpha |= opacity != 0;
                    color = PackBMPColor(sample[2], sample[1], sample[0], opacity);
                }
                WriteUInt32LittleEndian(pixels.AsSpan((y * width + x) * 4), color);
            }
        }

        // Unmasked 32-bit BMPs also use the high byte as padding. Preserve the legacy all-zero convention.
        if (depth == 32 && !masked && !anyAlpha)
            for (var i = 3; i < pixels.Length; i += 4)
                pixels[i] = 255;

        return new(width, height, Format.Rgba8, false, pixels);
    }

    private static void DecodeBMPRLE(ReadOnlySpan<byte> source, Span<byte> pixels, int width, int height, int depth, ReadOnlySpan<uint> palette)
    {
        // Delta and end-of-line escapes can leave areas untouched. These use palette entry zero.
        for (var i = 0; i < pixels.Length; i += 4)
            WriteUInt32LittleEndian(pixels[i..], palette[0]);

        var x = 0;
        var y = height - 1;
        while (source.Length >= 2)
        {
            var count = (int)source[0];
            var value = source[1];
            source = source[2..];
            if (count == 0 && value <= 2)
            {
                if (value == 1)
                    return;
                if (value == 0 && y >= 0)
                {
                    x = 0;
                    y--;
                    continue;
                }
                if (value == 2 && source.Length >= 2 && source[0] <= width - x && source[1] <= y)
                {
                    x += source[0];
                    y -= source[1];
                    source = source[2..];
                    continue;
                }
                throw new InvalidDataException("Invalid BMP RLE cursor move.");
            }

            var literal = count == 0;
            if (literal)
                count = value;
            var bytes = (count * depth + 7) / 8;
            var packetSize = (bytes + 1) & ~1;
            if (y < 0 || count > width - x || (literal && packetSize > source.Length))
                throw new InvalidDataException("BMP RLE packet exceeds the row or input.");

            for (var i = 0; i < count; i++)
            {
                var sample = literal ? source[i * depth / 8] : value;
                var index = depth == 8 ? sample : (i % 2 == 0 ? sample >> 4 : sample & 15);
                if (index >= palette.Length)
                    throw new InvalidDataException("BMP RLE pixel uses an undefined palette index.");
                WriteUInt32LittleEndian(pixels[((y * width + x) * 4)..], palette[index]);
                x++;
            }
            if (literal)
                source = source[packetSize..];
        }
        throw new InvalidDataException("Missing BMP RLE end marker.");
    }

    private static uint PackBMPColor(byte red, byte green, byte blue, byte alpha) =>
        red | ((uint)green << 8) | ((uint)blue << 16) | ((uint)alpha << 24);

    private readonly struct BMPChannel
    {
        private readonly int _shift;
        private readonly uint _maximum;

        internal BMPChannel(uint mask)
        {
            _shift = mask == 0 ? 0 : BitOperations.TrailingZeroCount(mask);
            _maximum = mask >> _shift;
            if ((_maximum & unchecked(_maximum + 1)) != 0)
                throw new InvalidDataException("BMP channel masks must be contiguous.");
        }

        internal byte Read(uint value) => _maximum == 0 ? (byte)255 :
            (byte)((((ulong)((value >> _shift) & _maximum) * 255) + _maximum / 2) / _maximum);
    }
}
