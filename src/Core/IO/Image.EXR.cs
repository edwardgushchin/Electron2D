using System.Buffers.Binary;
using EXR = Electron2DCodecs.EXR;

namespace Electron2D;

public sealed partial class Image
{
    private static readonly EXR.ReaderOptions EXRReadOptions = new(new EXR.ReaderLimits(
        maximumParts: 1, maximumChannelsPerPart: 1024, maximumDimension: MaxWidth,
        maximumCompressedBlockByteCount: MaximumEncodedBytes, maximumUncompressedBlockByteCount: MaximumEncodedBytes,
        maximumMaterializedByteCount: MaximumEncodedBytes));
    private static readonly EXR.WriterOptions EXRWriteOptions = new(new EXR.WriterLimits(
        maximumParts: 1, maximumChannelsPerPart: 4, maximumDimension: MaxWidth,
        maximumUncompressedBlockByteCount: MaximumEncodedBytes, maximumEncodedBlockByteCount: MaximumEncodedBytes));

    /// <summary>Replaces this image with the base level of a flat OpenEXR image.</summary>
    /// <param name="buffer">Encoded EXR bytes, copied before parsing, at most 64 MiB.</param>
    /// <remarks>Preserves one through four R/G/B/A channels, or expands Y to RGB, and half/float precision.
    /// UINT samples convert numerically to float. Scanline and tiled base levels are supported; source mipmaps are not loaded.
    /// Metadata and decoded storage are bounded before allocation. Deep, multipart and unavailable compression reject.
    /// Failure preserves this image; success emits Changed after complete replacement.</remarks>
    /// <exception cref="InvalidDataException">The data is malformed or exceeds supported limits.</exception>
    /// <exception cref="NotSupportedException">The file uses an unsupported image or compression role.</exception>
    /// <exception cref="ObjectDisposedException">The image is disposed.</exception>
    public void LoadEXRFromBuffer(ReadOnlySpan<byte> buffer)
    {
        ThrowIfDisposed(); CheckEncodedLength(buffer.Length);
        var encoded = buffer.ToArray();
        if (encoded.Length < 8) throw new InvalidDataException("The EXR header is truncated.");
        using var reader = EXR.ExrReader.OpenMemory(encoded, EXRReadOptions);
        var parsed = reader.ParseHeader();
        if (!parsed.IsSuccess) throw EXRReadFailure(parsed.Status, parsed.Error);
        var header = reader.GetHeader(0);
        if (header.IsDeep || (BinaryPrimitives.ReadUInt32LittleEndian(encoded.AsSpan(4)) & 0x1000) != 0)
            throw new NotSupportedException("An Image requires a single flat EXR part.");
        if (header.DataWindow.Width > MaxWidth || header.DataWindow.Height > MaxHeight) throw new InvalidDataException("EXR dimensions exceed image limits.");
        var width = checked((int)header.DataWindow.Width); var height = checked((int)header.DataWindow.Height);
        var descriptions = header.Channels.ToDictionary(channel => channel.Name, StringComparer.Ordinal);
        string[] names;
        if (descriptions.ContainsKey("Y")) names = descriptions.ContainsKey("A") ? ["Y", "Y", "Y", "A"] : ["Y", "Y", "Y"];
        else
        {
            if (!descriptions.ContainsKey("R")) throw new InvalidDataException("EXR pixels require R or Y.");
            names = descriptions.ContainsKey("A") ? ["R", "G", "B", "A"] : descriptions.ContainsKey("B") ? ["R", "G", "B"] : descriptions.ContainsKey("G") ? ["R", "G"] : ["R"];
            if (names.Length == 3 && !descriptions.ContainsKey("G")) throw new InvalidDataException("RGB pixels require all three channels.");
        }
        var half = header.Channels.Any(channel => channel.PixelType == EXR.PixelType.Half);
        var size = checked((long)width * height * names.Length * (half ? 2 : 4));
        if (size > MaximumEncodedBytes || (long)width * height * names.Length * 4 > MaximumEncodedBytes)
            throw new InvalidDataException("Decoded EXR pixels exceed 64 MiB.");
        var result = reader.ReadPart(0);
        if (!result.IsSuccess || result.Value is null) throw EXRReadFailure(result.Status, result.Error);
        var part = result.Value;
        var level = part.GetLevel(0, 0);
        var selected = names.Distinct().Where(descriptions.ContainsKey).ToArray();
        var subsetHeader = new EXR.Header(header.PartType, header.DataWindow, selected.Select(name => descriptions[name]), header.Compression, tiles: header.Tiles);
        var subset = new EXR.Part(subsetHeader, [new EXR.FlatLevel(0, 0, level.Region, selected.Select(level.GetChannel))], true);
        var pixels = EXR.PartConversion.ToInterleavedFloat(subset);
        var channelNames = pixels.ChannelNames.ToList();
        var indices = names.Select(name => channelNames.IndexOf(name)).ToArray();
        var data = new byte[(int)size];
        for (var i = 0; i < width * height; i++)
            for (var channel = 0; channel < names.Length; channel++)
            {
                var value = indices[channel] < 0 ? 0 : pixels.Data[i * pixels.Channels + indices[channel]];
                var offset = (i * names.Length + channel) * (half ? 2 : 4);
                if (half) BinaryPrimitives.WriteUInt16LittleEndian(data.AsSpan(offset), BitConverter.HalfToUInt16Bits((Half)value));
                else BinaryPrimitives.WriteSingleLittleEndian(data.AsSpan(offset), value);
            }
        var format = (half, names.Length) switch
        {
            (true, 1) => Format.Rh,
            (true, 2) => Format.Rgh,
            (true, 3) => Format.Rgbh,
            (true, _) => Format.Rgbah,
            (false, 1) => Format.Rf,
            (false, 2) => Format.Rgf,
            (false, 3) => Format.Rgbf,
            _ => Format.Rgbaf
        };
        Commit(new State(width, height, format, false, data));
    }

    private static Exception EXRReadFailure(EXR.ExrResult status, Exception? error) => status == EXR.ExrResult.Unsupported
        ? new NotSupportedException("The EXR codec role is not supported.", error)
        : new InvalidDataException($"EXR decoding failed: {status}.", error);

    /// <summary>Encodes the base image as a lossless PIZ-compressed OpenEXR buffer.</summary>
    /// <param name="grayscale">Uses the Y channel name for a single-channel image; multichannel input rejects.</param>
    /// <param name="colorImage">Clamps negative floating-point components to zero.</param>
    /// <param name="maxLinearValue">Upper component bound, or a negative value to leave the upper range unchanged.</param>
    /// <returns>Independent caller-owned EXR bytes.</returns>
    /// <remarks>Accepts R/RG/RGB/RGBA 8-bit, half and float source formats. Byte components normalize to half;
    /// half and float sources retain their precision and channels. Clamp options also apply to alpha.
    /// Uses a stable base-level snapshot without changing source pixels, mipmaps or events.
    /// Base channel storage and encoded output are each bounded to 64 MiB.</remarks>
    /// <exception cref="ArgumentException">Grayscale is requested for more than one channel.</exception>
    /// <exception cref="ArgumentOutOfRangeException">The upper bound is NaN.</exception>
    /// <exception cref="InvalidOperationException">The image is empty.</exception>
    /// <exception cref="NotSupportedException">The source format is unsupported.</exception>
    /// <exception cref="IOException">Encoding fails or exceeds supported limits.</exception>
    /// <exception cref="ObjectDisposedException">The image is disposed.</exception>
    public byte[] SaveEXRToBuffer(bool grayscale = false, bool colorImage = false, float maxLinearValue = -1f)
    {
        ThrowIfDisposed();
        if (float.IsNaN(maxLinearValue)) throw new ArgumentOutOfRangeException(nameof(maxLinearValue));
        var state = Snapshot();
        if (state.Width == 0) throw new InvalidOperationException("An empty image cannot be encoded.");
        var (channels, bytesPerChannel) = state.Format switch
        {
            Format.R8 => (1, 1),
            Format.Rg8 => (2, 1),
            Format.Rgb8 => (3, 1),
            Format.Rgba8 => (4, 1),
            Format.Rh => (1, 2),
            Format.Rgh => (2, 2),
            Format.Rgbh => (3, 2),
            Format.Rgbah => (4, 2),
            Format.Rf => (1, 4),
            Format.Rgf => (2, 4),
            Format.Rgbf => (3, 4),
            Format.Rgbaf => (4, 4),
            _ => throw new NotSupportedException("EXR saving requires R, RG, RGB or RGBA byte, half or float pixels.")
        };
        if (grayscale && channels != 1) throw new ArgumentException("Grayscale EXR requires one channel.", nameof(grayscale));
        var type = bytesPerChannel == 4 ? EXR.PixelType.Float : EXR.PixelType.Half;
        var targetSize = bytesPerChannel == 4 ? 4 : 2;
        var pixelCount = checked(state.Width * state.Height);
        if ((long)pixelCount * channels * targetSize > MaximumEncodedBytes) throw new IOException("EXR source pixels exceed 64 MiB.");
        var descriptions = new EXR.Channel[channels]; var planes = new EXR.ChannelBuffer[channels];
        for (var channel = 0; channel < channels; channel++)
        {
            var name = grayscale ? "Y" : "RGBA"[channel].ToString(); descriptions[channel] = new(name, type);
            var data = new byte[pixelCount * targetSize];
            for (var i = 0; i < pixelCount; i++)
            {
                var offset = (i * channels + channel) * bytesPerChannel;
                if (bytesPerChannel == 2 && !colorImage && maxLinearValue < 0)
                {
                    state.Data.AsSpan(offset, 2).CopyTo(data.AsSpan(i * 2)); continue;
                }
                var value = bytesPerChannel switch
                {
                    1 => state.Data[offset] / 255f,
                    2 => (float)BitConverter.UInt16BitsToHalf(BinaryPrimitives.ReadUInt16LittleEndian(state.Data.AsSpan(offset))),
                    _ => BinaryPrimitives.ReadSingleLittleEndian(state.Data.AsSpan(offset))
                };
                if (maxLinearValue >= 0) value = float.MinNumber(maxLinearValue, value);
                if (colorImage) value = float.MaxNumber(0, value);
                if (targetSize == 2) BinaryPrimitives.WriteUInt16LittleEndian(data.AsSpan(i * 2), BitConverter.HalfToUInt16Bits((Half)value));
                else BinaryPrimitives.WriteSingleLittleEndian(data.AsSpan(i * 4), value);
            }
            planes[channel] = EXR.ChannelBuffer.Adopt(name, type, data);
        }
        var window = new EXR.Box2i(0, 0, state.Width - 1, state.Height - 1);
        var header = new EXR.Header(EXR.PartType.Scanline, window, descriptions, EXR.Compression.PIZ);
        var image = new EXR.Image([new EXR.Part(header, [new EXR.FlatLevel(0, 0, window, planes)], true)]);
        using var output = new EXROutput();
        var result = EXR.ExrFile.SaveToStream(image, output, EXRWriteOptions);
        if (!result.IsSuccess) throw new IOException($"EXR encoding failed: {result.Status}.", result.Error);
        return output.ToArray();
    }

    /// <summary>Atomically saves the base image as a lossless OpenEXR file.</summary>
    /// <param name="path">Operating-system, res:// or user:// destination; its parent directory must exist.</param>
    /// <param name="grayscale">Names a single channel Y; multichannel input rejects.</param>
    /// <param name="colorImage">Clamps negative floating-point components to zero.</param>
    /// <param name="maxLinearValue">Upper component bound, or a negative value to disable the upper bound.</param>
    /// <remarks>Uses SaveEXRToBuffer before replacing the file. Encoding failure preserves an existing destination.</remarks>
    /// <exception cref="ArgumentException">Grayscale is requested for more than one channel.</exception>
    /// <exception cref="ArgumentOutOfRangeException">The upper bound is NaN.</exception>
    /// <exception cref="InvalidOperationException">The image is empty.</exception>
    /// <exception cref="NotSupportedException">The source format is unsupported.</exception>
    /// <exception cref="IOException">Encoding or saving fails.</exception>
    /// <exception cref="ObjectDisposedException">The image is disposed.</exception>
    public void SaveEXR(string path, bool grayscale = false, bool colorImage = false, float maxLinearValue = -1f) =>
        SaveEncodedFile(path, SaveEXRToBuffer(grayscale, colorImage, maxLinearValue));

    private sealed class EXROutput : MemoryStream
    {
        public override void Write(ReadOnlySpan<byte> buffer) { CheckWrite(buffer.Length); base.Write(buffer); }
        public override void Write(byte[] buffer, int offset, int count) { CheckWrite(count); base.Write(buffer, offset, count); }
        public override void WriteByte(byte value) { CheckWrite(1); base.WriteByte(value); }
        public override void SetLength(long value) { CheckPosition(value); base.SetLength(value); }
        public override long Position { get => base.Position; set { CheckPosition(value); base.Position = value; } }
        public override long Seek(long offset, SeekOrigin origin)
        {
            var position = checked(offset + (origin == SeekOrigin.Begin ? 0 : origin == SeekOrigin.Current ? Position : Length));
            CheckPosition(position); return base.Seek(offset, origin);
        }
        private void CheckWrite(int count) => CheckPosition(checked(Position + count));
        private static void CheckPosition(long value) { if (value is < 0 or > MaximumEncodedBytes) throw new IOException("Encoded EXR output exceeds 64 MiB."); }
    }
}
