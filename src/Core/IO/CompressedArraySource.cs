using System.Buffers.Binary;

namespace Electron2D;

internal static class CompressedArraySource
{
    internal const int MaximumBytes = 64 * 1024 * 1024;

    internal static LayeredTexturePixels Read(string path)
    {
        using var file = FileAccess.Open(path, FileAccessModeFlags.Read);
        var length = file.Length;
        if (length is < 36 or > MaximumBytes) throw new InvalidDataException("Compressed array file size is outside its bounds.");
        var data = file.ReadBytes((int)length);
        if (data.Length != length || file.Length != length) throw new IOException("Compressed array source changed while it was read.");
        return Read(data);
    }

    internal static LayeredTexturePixels Read(ReadOnlySpan<byte> data)
    {
        if (data.Length is < 36 or > MaximumBytes || !data[..4].SequenceEqual("GSTL"u8)) throw new InvalidDataException("Invalid compressed array header.");
        var source = new Reader(data[4..]);
        if (source.UInt32() > 1) throw new InvalidDataException("Compressed array version is unsupported.");
        var count = source.UInt32(); var type = source.UInt32();
        if (type != 0) throw new NotSupportedException("Only independent image-array containers are applicable.");
        if (count == 0 || count > (data.Length - 36) / 16) throw new InvalidDataException("Invalid compressed array layer count.");
        _ = source.UInt32(); _ = source.UInt32();
        for (var i = 0; i < 3; ++i) _ = source.UInt32();
        var layers = new TexturePixels[(int)count]; var remaining = LayeredTexturePixels.MaximumBytes;
        try
        {
            for (var layer = 0; layer < layers.Length; ++layer)
            {
                using var image = ReadImage(ref source);
                layers[layer] = TexturePixels.FromImage(image, remaining);
                remaining -= layers[layer].Source.Data.Length + layers[layer].Upload.Data.Length;
            }
            if (!source.AtEnd) throw new InvalidDataException("Trailing compressed array data.");
            return new LayeredTexturePixels(layers);
        }
        catch (ArgumentException error) { throw new InvalidDataException("Compressed array layers exceed their limits or are heterogeneous.", error); }
    }

    private static Image ReadImage(ref Reader source)
    {
        var mode = source.UInt32(); var width = source.UInt16(); var height = source.UInt16();
        var mips = source.UInt32(); var format = (Image.Format)source.UInt32();
        if (width == 0 || height == 0 || mips != Image.ImportedMIPCount(width, height, mips != 0))
            throw new InvalidDataException("Invalid imported image dimensions or mip count.");
        if (mode == 3)
        {
            var state = Image.ReadImportedBasis(source.Bytes(source.Size()));
            if (state.Width != width || state.Height != height || state.HasMIPMaps != (mips != 0))
                throw new InvalidDataException("Basis image disagrees with its layer header.");
            return Image.FromImportedState(state);
        }
        if (mode == 0)
        {
            var size = Image.ImportedDataSize(width, height, format, mips != 0);
            return Image.FromImportedState(new Image.State(width, height, format, mips != 0, source.Bytes(size).ToArray()));
        }
        if (mode is not (1 or 2)) throw new InvalidDataException("Unknown imported image encoding.");
        var expected = Image.ImportedDataSize(width, height, format, mips != 0);
        if (expected > MaximumBytes) throw new InvalidDataException("Imported mip pixels exceed their limit.");
        var data = new byte[expected]; var offset = 0;
        for (var mip = 0; mip <= mips; ++mip)
        {
            var payload = source.Bytes(source.Size());
            using var image = new Image();
            if (mode == 1) image.LoadPNGFromBuffer(payload); else image.LoadWebPFromBuffer(payload);
            if (image.Width != Math.Max(1, width >> (int)mip) || image.Height != Math.Max(1, height >> (int)mip) || image.HasMIPMaps)
                throw new InvalidDataException("Encoded mip dimensions disagree with their layer.");
            image.Convert(format);
            var pixels = image.GetData();
            if (pixels.Length > data.Length - offset) throw new InvalidDataException("Encoded mip storage exceeds its declaration.");
            pixels.CopyTo(data, offset); offset += pixels.Length;
        }
        if (offset != data.Length) throw new InvalidDataException("Encoded mip storage is incomplete.");
        return Image.FromImportedState(new Image.State(width, height, format, mips != 0, data));
    }

    private ref struct Reader(ReadOnlySpan<byte> data)
    {
        private ReadOnlySpan<byte> _remaining = data;
        internal bool AtEnd => _remaining.IsEmpty;
        internal ReadOnlySpan<byte> Bytes(int count)
        {
            if (count < 0 || count > _remaining.Length) throw new InvalidDataException("Truncated compressed array data.");
            var result = _remaining[..count]; _remaining = _remaining[count..]; return result;
        }
        internal uint UInt32() => BinaryPrimitives.ReadUInt32LittleEndian(Bytes(4));
        internal ushort UInt16() => BinaryPrimitives.ReadUInt16LittleEndian(Bytes(2));
        internal int Size() { var count = UInt32(); if (count > MaximumBytes) throw new InvalidDataException("Imported payload exceeds its limit."); return (int)count; }
    }
}
