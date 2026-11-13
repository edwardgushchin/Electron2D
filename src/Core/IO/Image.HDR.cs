using System.Buffers.Binary;
using System.Globalization;
using System.Text;

namespace Electron2D;

public sealed partial class Image
{
    private static State DecodeHDR(ReadOnlySpan<byte> input)
    {
        var headerLimit = Math.Min(input.Length, 64 * 1024);
        var header = input[..headerLimit];
        var signature = ReadHDRLine(ref header);
        if (!signature.SequenceEqual("#?RADIANCE"u8) && !signature.SequenceEqual("#?RGBE"u8))
            throw new InvalidDataException("Invalid Radiance signature.");

        scoped ReadOnlySpan<byte> line;
        while (!(line = ReadHDRLine(ref header)).IsEmpty)
            if (line.StartsWith("FORMAT="u8) && !line.SequenceEqual("FORMAT=32-bit_rle_rgbe"u8))
                throw new InvalidDataException("Unsupported Radiance pixel format.");

        var resolution = Encoding.ASCII.GetString(ReadHDRLine(ref header))
            .Split([' ', '\t'], StringSplitOptions.RemoveEmptyEntries);
        if (resolution.Length != 4 || !IsHDRAxis(resolution[0]) || !IsHDRAxis(resolution[2]) ||
            resolution[0][1] == resolution[2][1] ||
            !int.TryParse(resolution[1], NumberStyles.None, CultureInfo.InvariantCulture, out var lines) || lines <= 0 ||
            !int.TryParse(resolution[3], NumberStyles.None, CultureInfo.InvariantCulture, out var samples) || samples <= 0)
            throw new InvalidDataException("Invalid Radiance resolution.");

        var rows = resolution[0][1] == 'Y';
        var width = rows ? samples : lines;
        var height = rows ? lines : samples;
        if (width > MaxWidth || height > MaxHeight || (long)width * height > MaximumEncodedBytes / 4)
            throw new InvalidDataException("Decoded Radiance pixels exceed 64 MiB.");
        var source = input[(headerLimit - header.Length)..];
        if (source.Length / 4 < lines)
            throw new InvalidDataException("Truncated Radiance pixels.");

        var data = new byte[width * height * 4];
        var modernWidth = samples is >= 8 and <= 32767;
        var channels = modernWidth ? new byte[samples * 4] : [];
        var lineStride = rows ? width : 1;
        var sampleStride = rows ? 1 : width;
        var reverseLines = resolution[0][0] != (rows ? '-' : '+');
        var reverseSamples = resolution[2][0] != (rows ? '+' : '-');
        if (reverseSamples)
            sampleStride = -sampleStride;

        for (var scan = 0; scan < lines; scan++)
        {
            if (source.Length < 4)
                throw new InvalidDataException("Truncated Radiance scanline.");
            var position = (reverseLines ? lines - 1 - scan : scan) * lineStride;
            if (reverseSamples)
                position -= (samples - 1) * sampleStride;

            if (modernWidth && source[0] == 2 && source[1] == 2 && (source[2] & 128) == 0)
            {
                if (BinaryPrimitives.ReadUInt16BigEndian(source[2..]) != samples)
                    throw new InvalidDataException("Radiance scanline length disagrees with the resolution.");
                source = source[4..];
                for (var channel = 0; channel < 4; channel++)
                    ReadHDRChannel(ref source, channels.AsSpan(channel * samples, samples));

                for (var i = 0; i < samples; i++, position += sampleStride)
                    BinaryPrimitives.WriteUInt32LittleEndian(data.AsSpan(position * 4),
                        PackHDR(channels[i], channels[samples + i], channels[2 * samples + i], channels[3 * samples + i]));
            }
            else
            {
                ReadHDRLegacyLine(ref source, data, position, sampleStride, samples);
            }
        }
        return new(width, height, Format.Rgbe9995, false, data);
    }

    private static ReadOnlySpan<byte> ReadHDRLine(ref ReadOnlySpan<byte> source)
    {
        var end = source.IndexOf((byte)'\n');
        if (end < 0)
            throw new InvalidDataException("Missing or oversized Radiance header.");
        var line = source[..end];
        source = source[(end + 1)..];
        return !line.IsEmpty && line[^1] == '\r' ? line[..^1] : line;
    }

    private static bool IsHDRAxis(string axis) =>
        axis.Length == 2 && axis[0] is '+' or '-' && axis[1] is 'X' or 'Y';

    private static void ReadHDRChannel(ref ReadOnlySpan<byte> source, Span<byte> channel)
    {
        while (!channel.IsEmpty)
        {
            if (source.Length < 2)
                throw new InvalidDataException("Truncated Radiance channel packet.");
            var code = source[0];
            var repeat = code > 128;
            var count = repeat ? code - 128 : code;
            var bytes = repeat ? 1 : count;
            if (count == 0 || count > channel.Length || bytes >= source.Length)
                throw new InvalidDataException("Invalid Radiance channel packet.");
            if (repeat)
                channel[..count].Fill(source[1]);
            else
                source.Slice(1, count).CopyTo(channel);
            source = source[(bytes + 1)..];
            channel = channel[count..];
        }
    }

    private static void ReadHDRLegacyLine(ref ReadOnlySpan<byte> source, Span<byte> data, int position, int stride, int remaining)
    {
        var hasPixel = false;
        var shift = 0;
        var previous = 0u;
        while (remaining != 0)
        {
            if (source.Length < 4)
                throw new InvalidDataException("Truncated Radiance pixel.");
            int count;
            if (source[0] == 1 && source[1] == 1 && source[2] == 1)
            {
                if (!hasPixel || shift >= 32 || ((long)source[3] << shift) > remaining)
                    throw new InvalidDataException("Invalid Radiance legacy run.");
                count = source[3] << shift;
                shift += 8;
            }
            else
            {
                previous = PackHDR(source[0], source[1], source[2], source[3]);
                hasPixel = true;
                shift = 0;
                count = 1;
            }
            source = source[4..];
            remaining -= count;
            for (var i = 0; i < count; i++, position += stride)
                BinaryPrimitives.WriteUInt32LittleEndian(data[(position * 4)..], previous);
        }
    }

    private static uint PackHDR(byte red, byte green, byte blue, byte exponent)
    {
        if (exponent == 0)
            return 0;
        if (exponent is >= 113 and <= 144)
            return ((uint)(exponent - 113) << 27) | ((uint)blue << 19) | ((uint)green << 10) | ((uint)red << 1);

        var scale = MathF.ScaleB(1f, exponent - 136);
        return ToRGBE9995(new Color(red * scale, green * scale, blue * scale));
    }
}
