namespace Electron2D;

internal static class PortableTextureData
{
    private const int Limit = 64 * 1024 * 1024;
    private const uint Magic = 0x31545045; // EPT1
    internal static (TexturePixels Pixels, byte[] Data, bool HasAlpha) Encode(Image source, PortableCompressedTexture.CompressionMode mode, bool normal, float quality, int level, float rdo)
    {
        var state = source.CopyTexturePixels(Limit);
        using var image = Image.CreateFromData(state.Width, state.Height, state.HasMIPMaps, state.Format, state.Data);
        byte[][] payloads;
        if (mode is PortableCompressedTexture.CompressionMode.Lossless or PortableCompressedTexture.CompressionMode.Lossy)
        {
            if (image.IsCompressed) image.Decompress();
            var pixels = image.CopyPixels(); var count = image.MIPMapCount + 1; payloads = new byte[count][];
            for (int mip = 0, width = state.Width, height = state.Height; mip < count; mip++, width = Math.Max(1, width / 2), height = Math.Max(1, height / 2))
            {
                var bytes = Image.GetRequiredDataSize(width, height, pixels.Format, false);
                using var part = Image.CreateFromData(width, height, false, pixels.Format, pixels.Data.AsSpan(image.GetMIPMapOffset(mip), bytes).ToArray());
                payloads[mip] = mode == PortableCompressedTexture.CompressionMode.Lossless && (width > 16383 || height > 16383) ? part.SavePNGToBuffer() : part.SaveWebPToBuffer(mode == PortableCompressedTexture.CompressionMode.Lossy, quality);
            }
            state = pixels;
        }
        else if (mode == PortableCompressedTexture.CompressionMode.BasisUniversal)
            payloads = [image.EncodeBasis(level, rdo, normal)];
        else
        {
            var compression = mode switch { PortableCompressedTexture.CompressionMode.S3TC => Image.CompressMode.S3tc, PortableCompressedTexture.CompressionMode.ETC2 => Image.CompressMode.Etc2, PortableCompressedTexture.CompressionMode.BPTC => Image.CompressMode.Bptc, _ => Image.CompressMode.Astc };
            if (image.IsCompressed)
            {
                if (!Matches(mode, state.Format)) throw new ArgumentException("Precompressed pixels must match the portable compression mode.", nameof(source));
            }
            else image.Compress(compression);
            state = image.CopyPixels(); payloads = [state.Data];
        }
        if (payloads.Sum(p => (long)p.Length + 4) + 32 > Limit) throw new InvalidDataException("Portable texture data exceeds 64 MiB.");
        using var stream = new MemoryStream(); using var writer = new BinaryWriter(stream);
        writer.Write(Magic); writer.Write((int)mode); writer.Write(state.Width); writer.Write(state.Height); writer.Write((int)state.Format); writer.Write(state.HasMIPMaps ? image.MIPMapCount + 1 : 1); writer.Write(payloads.Length);
        foreach (var payload in payloads) { writer.Write(payload.Length); writer.Write(payload); }
        var data = stream.ToArray(); var decoded = Decode(data); return (decoded.Pixels, data, decoded.HasAlpha);
    }
    private static bool Matches(PortableCompressedTexture.CompressionMode mode, Image.Format format) => mode switch
    {
        PortableCompressedTexture.CompressionMode.S3TC => format is >= Image.Format.Dxt1 and <= Image.Format.RgtcRg or Image.Format.Dxt5RaAsRg,
        PortableCompressedTexture.CompressionMode.ETC2 => format is >= Image.Format.Etc and <= Image.Format.Etc2RaAsRg,
        PortableCompressedTexture.CompressionMode.BPTC => format is >= Image.Format.BptcRgba and <= Image.Format.BptcRgbfu,
        PortableCompressedTexture.CompressionMode.ASTC => format is >= Image.Format.Astc4X4 and <= Image.Format.Astc8X8Hdr,
        _ => false
    };
    internal static (TexturePixels Pixels, PortableCompressedTexture.CompressionMode Mode, bool HasAlpha) Decode(byte[] data)
    {
        if (data.Length is < 32 or > Limit) throw new InvalidDataException("Portable texture envelope size is invalid.");
        using var stream = new MemoryStream(data, writable: false); using var reader = new BinaryReader(stream);
        try
        {
            if (reader.ReadUInt32() != Magic) throw new InvalidDataException("Portable texture envelope version is invalid.");
            var mode = (PortableCompressedTexture.CompressionMode)reader.ReadInt32(); var width = reader.ReadInt32(); var height = reader.ReadInt32(); var format = (Image.Format)reader.ReadInt32(); var levels = reader.ReadInt32(); var count = reader.ReadInt32();
            if (!Enum.IsDefined(mode) || !Enum.IsDefined(format) || format == Image.Format.Max || width is < 1 or > 16384 || height is < 1 or > 16384 || levels is < 1 or > 15 || count is < 1 or > 15) throw new InvalidDataException("Portable texture metadata is invalid.");
            var expectedLevels = 1; for (int w = width, h = height; w > 1 || h > 1; w = Math.Max(1, w / 2), h = Math.Max(1, h / 2)) expectedLevels++;
            if (levels != 1 && levels != expectedLevels) throw new InvalidDataException("Portable texture mip chain is incomplete.");
            var web = mode is PortableCompressedTexture.CompressionMode.Lossless or PortableCompressedTexture.CompressionMode.Lossy;
            if (count != (web ? levels : 1)) throw new InvalidDataException("Portable texture payload count is invalid.");
            var payloads = new byte[count][];
            for (var i = 0; i < count; i++) { var length = reader.ReadInt32(); if (length <= 0 || length > stream.Length - stream.Position) throw new InvalidDataException("Portable texture payload is truncated."); payloads[i] = reader.ReadBytes(length); }
            if (stream.Position != stream.Length) throw new InvalidDataException("Portable texture data has trailing bytes.");
            using var image = new Image();
            if (web)
            {
                if (Image.IsCompressedFormat(format)) throw new InvalidDataException("Portable web payload requires an uncompressed pixel format.");
                var size = Image.GetRequiredDataSize(width, height, format, levels > 1);
                if (size > Limit) throw new InvalidDataException("Portable texture pixels exceed 64 MiB.");
                var pixels = new byte[size]; var offset = 0;
                for (int mip = 0, w = width, h = height; mip < levels; mip++, w = Math.Max(1, w / 2), h = Math.Max(1, h / 2))
                {
                    using var part = new Image();
                    if (payloads[mip].AsSpan().StartsWith(new byte[] { 137, 80, 78, 71 })) part.LoadPNGFromBuffer(payloads[mip]); else part.LoadWebPFromBuffer(payloads[mip]);
                    if (part.Width != w || part.Height != h || part.HasMIPMaps) throw new InvalidDataException("Portable texture mip dimensions are inconsistent.");
                    part.Convert(format); var bytes = part.GetData(); bytes.CopyTo(pixels, offset); offset += bytes.Length;
                }
                using var joined = Image.CreateFromData(width, height, levels > 1, format, pixels); return (TexturePixels.FromImage(joined, Limit), mode, format is Image.Format.La8 or Image.Format.Rgba8);
            }
            if (mode == PortableCompressedTexture.CompressionMode.BasisUniversal) image.LoadKTXFromBuffer(payloads[0]);
            else
            {
                if (!Matches(mode, format)) throw new InvalidDataException("Portable texture block family is inconsistent.");
                using var blocks = Image.CreateFromData(width, height, levels > 1, format, payloads[0]); return (TexturePixels.FromImage(blocks, Limit), mode, format is Image.Format.La8 or Image.Format.Rgba8);
            }
            if (image.Width != width || image.Height != height || image.MIPMapCount + 1 != levels) throw new InvalidDataException("Portable Basis dimensions are inconsistent.");
            return (TexturePixels.FromImage(image, Limit), mode, format is Image.Format.La8 or Image.Format.Rgba8);
        }
        catch (Exception error) when (error is ArgumentException or OverflowException) { throw new InvalidDataException("Portable texture pixel metadata is invalid.", error); }
        catch (EndOfStreamException error) { throw new InvalidDataException("Portable texture data is truncated.", error); }
    }
}
