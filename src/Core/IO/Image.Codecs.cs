using IOPath = System.IO.Path;
using System.Buffers.Binary;
using System.Runtime.InteropServices;
using SDL3;

namespace Electron2D;

public sealed partial class Image
{
    internal const int MaximumEncodedBytes = 64 * 1024 * 1024;

    /// <summary>Replaces this image with decoded pixels from a file.</summary>
    /// <param name="path">An operating-system, res://, user:// or registered UID path to an image file.</param>
    /// <remarks>Input is bounded to 64 MiB. PNG retains its decoded luminance or RGB layout and any alpha channel.
    /// JPEG produces L8 or RGB8 base pixels. WebP, BMP, TGA and SVG produce RGBA8 base pixels.
    /// DDS and KTX retain stored mip layouts; EXR and KTX retain applicable channel precision.
    /// Registered extensions may return other valid Image layouts. Their state is copied before publication.
    /// A failure preserves the previous image. Changed is emitted after a successful replacement.
    /// Radiance HDR files decode to linear RGBE9995 pixels.
    /// Custom decoders execute on this thread using borrowed temporary Image and FileAccess objects.
    /// Removal affects subsequent snapshots; retain registered decoders through pending calls.</remarks>
    /// <exception cref="InvalidDataException">The encoded image is malformed or exceeds supported limits.</exception>
    /// <exception cref="NotSupportedException">The filename extension is not integrated.</exception>
    /// <exception cref="IOException">The file cannot be read or changes while reading.</exception>
    /// <exception cref="ObjectDisposedException">The image is disposed.</exception>
    public void Load(string path)
    {
        ThrowIfDisposed();
        LoadFile(path);
    }

    /// <summary>Creates an image by decoding a supported file.</summary>
    /// <param name="path">An operating-system, res:// or user:// image path.</param>
    /// <returns>An independent caller-owned image.</returns>
    /// <remarks>Uses the same format, size, ownership and error contract as Load.</remarks>
    /// <exception cref="InvalidDataException">The encoded image is malformed or exceeds supported limits.</exception>
    /// <exception cref="NotSupportedException">The filename extension is not integrated.</exception>
    /// <exception cref="IOException">The file cannot be read.</exception>
    public static Image LoadFromFile(string path)
    {
        var image = new Image();
        try { image.Load(path); return image; }
        catch { image.Dispose(); throw; }
    }

    /// <summary>Decodes a PNG buffer and replaces the image atomically.</summary>
    /// <param name="buffer">PNG data, copied before validation and decoding, at most 64 MiB.</param>
    /// <remarks>Produces L8, LA8, RGB8 or RGBA8 base pixels, expanding palettes and transparency and converting to
    /// the sRGB transfer encoding. Decoded pixels are bounded to 64 MiB. Failure preserves pixels; success emits Changed.</remarks>
    /// <exception cref="InvalidDataException">Data is malformed or exceeds image limits.</exception>
    /// <exception cref="IOException">The decoder cannot allocate working memory.</exception>
    /// <exception cref="ObjectDisposedException">The image is disposed.</exception>
    public void LoadPNGFromBuffer(ReadOnlySpan<byte> buffer) => DecodeAndCommit(buffer, "PNG");

    /// <summary>Decodes a JPEG buffer and replaces the image atomically.</summary>
    /// <param name="buffer">JPEG data, copied before validation and decoding, at most 64 MiB.</param>
    /// <remarks>Decodes 8-bit grayscale as L8 and 8-bit color as RGB8. Decoded pixels are bounded to 64 MiB.
    /// Failure preserves pixels; success emits Changed.</remarks>
    /// <exception cref="InvalidDataException">Data is malformed or exceeds image limits.</exception>
    /// <exception cref="NotSupportedException">JPEG sample precision or color component count is unsupported.</exception>
    /// <exception cref="ObjectDisposedException">The image is disposed.</exception>
    public void LoadJPGFromBuffer(ReadOnlySpan<byte> buffer) => DecodeAndCommit(buffer, "JPG");

    /// <summary>Decodes a WebP buffer and replaces the image atomically.</summary>
    /// <param name="buffer">WebP data, copied before validation and decoding, at most 64 MiB.</param>
    /// <remarks>Uses the private native codec to produce RGBA8 without mipmaps. Animated input supplies its first composited frame. Failure preserves pixels; success emits Changed.</remarks>
    /// <exception cref="InvalidDataException">Data is malformed or exceeds image limits.</exception>
    /// <exception cref="PlatformNotSupportedException">The native codec bundle does not support allocation ownership.</exception>
    /// <exception cref="ObjectDisposedException">The image is disposed.</exception>
    public void LoadWebPFromBuffer(ReadOnlySpan<byte> buffer) => DecodeAndCommit(buffer, "WEBP");

    /// <summary>Decodes indexed, direct-color or RLE BMP pixels to top-left RGBA8 and replaces the image atomically.</summary>
    /// <param name="buffer">BMP data, copied before validation and decoding, at most 64 MiB.</param>
    /// <remarks>Produces RGBA8 without mipmaps. Failure preserves pixels; success emits Changed.</remarks>
    /// <exception cref="InvalidDataException">Data is malformed or exceeds image limits.</exception>
    /// <exception cref="ObjectDisposedException">The image is disposed.</exception>
    public void LoadBMPFromBuffer(ReadOnlySpan<byte> buffer) => DecodeAndCommit(buffer, "BMP");

    /// <summary>Decodes a TGA buffer and replaces the image atomically.</summary>
    /// <param name="buffer">TGA data, copied before validation and decoding, at most 64 MiB.</param>
    /// <remarks>Produces RGBA8 without mipmaps, normalizing the image origin to the top left.
    /// Supports raw and run-length encoded color, grayscale, and indexed pixels. Failure preserves pixels; success emits Changed.</remarks>
    /// <exception cref="InvalidDataException">Data is malformed or exceeds image limits.</exception>
    /// <exception cref="ObjectDisposedException">The image is disposed.</exception>
    public void LoadTGAFromBuffer(ReadOnlySpan<byte> buffer) => DecodeAndCommit(buffer, "TGA");

    /// <summary>Rasterizes an uncompressed UTF-8 SVG buffer and replaces the image atomically.</summary>
    /// <param name="buffer">SVG data, copied before validation and decoding, at most 64 MiB.</param>
    /// <param name="scale">Finite positive multiplier for the SVG's intrinsic dimensions.</param>
    /// <remarks>Produces an RGBA8 base image without mipmaps. The root style can supply width and height as positive pixel or physical lengths, percentages, or auto.
    /// XML DTDs are rejected. Failure preserves the previous image.</remarks>
    /// <exception cref="ArgumentOutOfRangeException">Scale is not finite and positive.</exception>
    /// <exception cref="InvalidDataException">The document is malformed or exceeds image limits.</exception>
    /// <exception cref="ObjectDisposedException">The image is disposed.</exception>
    public void LoadSVGFromBuffer(ReadOnlySpan<byte> buffer, float scale = 1f) => DecodeAndCommit(buffer, "SVG", scale);

    /// <summary>Encodes the base image as a PNG byte array.</summary>
    /// <returns>Caller-owned encoded bytes.</returns>
    /// <remarks>Uses a stable base-level snapshot. Supported block formats are decompressed. L8, LA8, RGB8 and RGBA8
    /// retain their channels. Other formats are converted to RGB8 or RGBA8 according to their base-level alpha values.
    /// PNG stores the resulting samples as sRGB without changing the source image.</remarks>
    /// <exception cref="InvalidOperationException">The image is empty or block expansion exceeds the codec work budget.</exception>
    /// <exception cref="InvalidDataException">The source blocks are invalid.</exception>
    /// <exception cref="IOException">Encoding fails or its input or output exceeds 64 MiB.</exception>
    /// <exception cref="ObjectDisposedException">The image is disposed.</exception>
    public byte[] SavePNGToBuffer() => EncodePNG();

    /// <summary>Encodes the base image as JPEG bytes, discarding alpha.</summary>
    /// <param name="quality">Finite quality from 0.01 to 1.0, truncated to an integer percentage.</param>
    /// <returns>Caller-owned encoded bytes.</returns>
    /// <remarks>Uses a stable base-level snapshot, decompressing supported block formats before converting to RGB8.
    /// JPEG remains lossy at maximum quality.</remarks>
    /// <exception cref="ArgumentOutOfRangeException">Quality is outside the accepted range or is not finite.</exception>
    /// <exception cref="InvalidOperationException">The image is empty or block expansion exceeds the codec work budget.</exception>
    /// <exception cref="InvalidDataException">The source blocks are invalid.</exception>
    /// <exception cref="IOException">Encoding fails or its output exceeds 64 MiB.</exception>
    /// <exception cref="ObjectDisposedException">The image is disposed.</exception>
    public byte[] SaveJPGToBuffer(float quality = 0.75f)
    {
        ThrowIfDisposed();
        if (!float.IsFinite(quality) || quality is < 0.01f or > 1) throw new ArgumentOutOfRangeException(nameof(quality));
        return EncodeJPEG((int)(quality * 100));
    }

    /// <summary>Atomically saves the base image to a PNG file.</summary>
    /// <param name="path">An operating-system, res:// or user:// destination path.</param>
    /// <remarks>Uses SavePNGToBuffer, then atomically replaces the destination. Parent directories must exist.</remarks>
    /// <exception cref="IOException">Encoding or saving fails; an existing destination is preserved before replacement.</exception>
    /// <exception cref="InvalidOperationException">The image is empty or block expansion exceeds the codec work budget.</exception>
    /// <exception cref="InvalidDataException">The source blocks are invalid.</exception>
    /// <exception cref="ObjectDisposedException">The image is disposed.</exception>
    public void SavePNG(string path) => SaveEncodedFile(path, SavePNGToBuffer());

    /// <summary>Atomically saves the base image as JPEG, discarding alpha.</summary>
    /// <param name="path">An operating-system, res:// or user:// destination path.</param>
    /// <param name="quality">Finite quality from 0.01 to 1.0.</param>
    /// <remarks>Uses SaveJPGToBuffer, then atomically replaces the destination. Parent directories must exist.</remarks>
    /// <exception cref="ArgumentOutOfRangeException">Quality is outside the accepted range or is not finite.</exception>
    /// <exception cref="IOException">Encoding or saving fails; an existing destination is preserved before replacement.</exception>
    /// <exception cref="InvalidOperationException">The image is empty or block expansion exceeds the codec work budget.</exception>
    /// <exception cref="InvalidDataException">The source blocks are invalid.</exception>
    /// <exception cref="ObjectDisposedException">The image is disposed.</exception>
    public void SaveJPG(string path, float quality = 0.75f) => SaveEncodedFile(path, SaveJPGToBuffer(quality));

    private static void SaveEncodedFile(string path, byte[] data)
    {
        ArgumentException.ThrowIfNullOrEmpty(path);
        ResourcePacks.Runtime.EnsureWritable(path);
        var destination = ProjectSettings.GlobalizePath(path);
        if (System.IO.Directory.Exists(destination))
            throw new IOException("An image file cannot replace a directory.");
        AtomicFile.Write(destination, data);
    }

    private static string? CodecFromPath(string path)
    {
        ArgumentException.ThrowIfNullOrEmpty(path);
        return IOPath.GetExtension(path).ToLowerInvariant() switch
        {
            ".exr" => "EXR",
            ".hdr" => "HDR",
            ".dds" => "DDS",
            ".ktx" or ".ktx2" => "KTX",
            ".png" => "PNG",
            ".jpg" or ".jpeg" => "JPG",
            ".webp" => "WEBP",
            ".bmp" => "BMP",
            ".tga" => "TGA",
            ".svg" => "SVG",
            _ => null
        };
    }

    private unsafe void DecodeAndCommit(ReadOnlySpan<byte> buffer, string codec, float scale = 1f)
    {
        ThrowIfDisposed();
        if (buffer.IsEmpty || buffer.Length > MaximumEncodedBytes) throw new InvalidDataException("Encoded images must contain between 1 byte and 64 MiB.");
        var bytes = buffer.ToArray();
        if (codec == "BMP")
        {
            Commit(DecodeBMP(bytes));
            return;
        }
        if (codec == "TGA")
        {
            Commit(DecodeTGA(bytes));
            return;
        }
        var format = Format.Rgba8;
        var size = codec switch
        {
            "SVG" => PrepareSVG(ref bytes, scale, forRaster: true),
            "JPG" => ReadJPEGLayout(bytes, out format),
            _ => EncodedSize(bytes, codec)
        };
        try { ValidateDimensions(size.X, size.Y); }
        catch (ArgumentOutOfRangeException e) { throw new InvalidDataException("Encoded dimensions exceed the image limits.", e); }
        if (codec == "PNG")
        {
            Commit(DecodePNG(bytes, size));
            return;
        }
        if (codec == "WEBP")
        {
            Commit(DecodeWebP(bytes, size));
            return;
        }
        if (codec == "JPG" && (long)size.X * size.Y * GetBytesPerPixel(format) > MaximumEncodedBytes)
            throw new InvalidDataException("Decoded JPEG pixels exceed 64 MiB.");
        State decoded;
        fixed (byte* pointer = bytes)
        {
            var stream = SDL.IOFromConstMem((nint)pointer, (nuint)bytes.Length);
            if (stream == 0) throw new IOException("Could not create an image stream: " + SDL.GetError());
            try
            {
                var surface = codec switch
                {
                    "JPG" => SDL3.Image.LoadJPGIO(stream),
                    "SVG" => SDL3.Image.LoadSizedSVGIO(stream, size.X, size.Y),
                    _ => throw new NotSupportedException(codec)
                };
                if (surface == 0) throw new InvalidDataException($"Cannot decode {codec}: {SDL.GetError()}");
                try
                {
                    var original = Marshal.PtrToStructure<SDL.Surface>(surface);
                    if (original.Width != size.X || original.Height != size.Y)
                        throw new InvalidDataException("Decoded dimensions disagree with the encoded header.");
                    var converted = SDL.ConvertSurface(surface, format == Format.Rgba8 ? RGBASurfaceFormat : SDL.PixelFormat.RGB24);
                    if (converted == 0) throw new IOException("Could not convert decoded pixels: " + SDL.GetError());
                    try
                    {
                        if (!SDL.LockSurface(converted)) throw new IOException("Could not lock decoded pixels: " + SDL.GetError());
                        try
                        {
                            var data = Marshal.PtrToStructure<SDL.Surface>(converted);
                            var pixels = new byte[GetRequiredDataSize(size.X, size.Y, format, false)];
                            var rowBytes = checked(size.X * (format == Format.Rgba8 ? 4 : 3));
                            if (data.Pixels == 0 || data.Pitch < rowBytes) throw new InvalidDataException("Invalid decoded pixel layout.");
                            for (var y = 0; y < size.Y; y++)
                            {
                                var row = data.Pixels + y * data.Pitch;
                                if (format == Format.L8)
                                {
                                    var rgb = new ReadOnlySpan<byte>((void*)row, rowBytes);
                                    for (var x = 0; x < size.X; x++)
                                        pixels[y * size.X + x] = rgb[x * 3];
                                }
                                else
                                    Marshal.Copy(row, pixels, y * rowBytes, rowBytes);
                            }
                            decoded = new(size.X, size.Y, format, false, pixels);
                        }
                        finally { SDL.UnlockSurface(converted); }
                    }
                    finally { SDL.DestroySurface(converted); }
                }
                finally { SDL.DestroySurface(surface); }
            }
            finally { SDL.CloseIO(stream); }
        }
        Commit(decoded);
    }

    private static SDL.PixelFormat RGBASurfaceFormat => BitConverter.IsLittleEndian ? SDL.PixelFormat.ABGR8888 : SDL.PixelFormat.RGBA8888;

    private State CopyBaseLevelForEncoding(string codec)
    {
        var state = Read(source =>
        {
            if (source.Width == 0)
                throw new InvalidOperationException("An empty image cannot be encoded.");
            var length = GetLevelByteCount(source.Width, source.Height, source.Format);
            if (length > MaximumEncodedBytes)
                throw new IOException($"{codec} source pixels exceed 64 MiB.");
            return source with { HasMIPMaps = false, Data = source.Data.AsSpan(0, length).ToArray() };
        });
        return IsCompressedFormat(state.Format) ? DecompressPixels(state) : state;
    }

    private unsafe byte[] EncodeJPEG(int quality)
    {
        var state = CopyBaseLevelForEncoding("JPEG");
        if ((long)state.Width * state.Height * 3 > MaximumEncodedBytes || state.Width > ushort.MaxValue || state.Height > ushort.MaxValue)
            throw new IOException("JPEG input pixels or dimensions exceed encoding limits.");
        state = ConvertState(state, Format.Rgb8);
        fixed (byte* pointer = state.Data)
        {
            var surface = SDL.CreateSurfaceFrom(state.Width, state.Height, SDL.PixelFormat.RGB24,
                (nint)pointer, checked(state.Width * 3));
            if (surface == 0) throw new IOException("Could not prepare image encoding: " + SDL.GetError());
            try
            {
                var stream = SDL.IOFromDynamicMem();
                if (stream == 0) throw new IOException("Could not create encoded image output: " + SDL.GetError());
                try
                {
                    if (!SDL3.Image.SaveJPGIO(surface, stream, false, quality))
                        throw new IOException("Cannot encode JPEG: " + SDL.GetError());
                    var length = SDL.GetIOSize(stream);
                    if (length is <= 0 or > MaximumEncodedBytes) throw new IOException("Encoded output exceeds the 64 MiB image limit.");
                    var result = new byte[(int)length];
                    if (SDL.SeekIO(stream, 0, SDL.IOWhence.Set) != 0) throw new IOException("Could not rewind encoded output: " + SDL.GetError());
                    fixed (byte* output = result)
                        if (SDL.ReadIO(stream, (nint)output, (nuint)result.Length) != (ulong)result.Length)
                            throw new IOException("Could not read encoded output: " + SDL.GetError());
                    return result;
                }
                finally { SDL.CloseIO(stream); }
            }
            finally { SDL.DestroySurface(surface); }
        }
    }

    private static Vector2i ReadJPEGLayout(ReadOnlySpan<byte> data, out Format format)
    {
        if (data.Length < 4 || data[0] != 0xff || data[1] != 0xd8)
            throw new InvalidDataException("Invalid JPEG signature.");
        Vector2i size = default;
        format = Format.Rgb8;
        var sawFrame = false;
        var sawScan = false;
        var inScan = false;
        for (var offset = 2; offset < data.Length;)
        {
            if (inScan)
            {
                var markerOffset = data[offset..].IndexOf((byte)0xff);
                if (markerOffset < 0)
                    break;
                offset += markerOffset;
            }
            if (data[offset++] != 0xff)
                break;
            while (offset < data.Length && data[offset] == 0xff)
                offset++;
            if (offset == data.Length)
                break;
            var marker = data[offset++];
            // Scan data escapes literal FF bytes and can carry restart markers between blocks.
            if (inScan && marker is 0x00 or >= 0xd0 and <= 0xd7)
                continue;
            if (marker == 0xd9 && sawScan)
                return size;
            if (marker == 0x01)
                continue;
            if (marker is < 0xc0 or 0xd8 or 0xd9 or >= 0xd0 and <= 0xd7 || offset > data.Length - 2)
                break;
            var length = BinaryPrimitives.ReadUInt16BigEndian(data[offset..]);
            if (length < 2 || length > data.Length - offset)
                break;
            if (marker is >= 0xc0 and <= 0xcf and not (0xc4 or 0xc8 or 0xcc))
            {
                if (sawFrame || length < 8)
                    break;
                var components = data[offset + 7];
                if (components == 0 || length != 8 + 3 * components)
                    break;
                if (data[offset + 2] != 8 || components is not (1 or 3))
                    throw new NotSupportedException("JPEG requires 8-bit grayscale or three-component color samples.");
                format = components == 1 ? Format.L8 : Format.Rgb8;
                size = new(BinaryPrimitives.ReadUInt16BigEndian(data[(offset + 5)..]), BinaryPrimitives.ReadUInt16BigEndian(data[(offset + 3)..]));
                sawFrame = true;
            }
            inScan = marker == 0xda;
            if (inScan)
            {
                if (!sawFrame || length < 8 || length != 6 + 2 * data[offset + 2])
                    break;
                sawScan = true;
            }
            offset += length;
        }
        throw new InvalidDataException("Invalid or incomplete JPEG marker stream.");
    }

    private static Vector2i EncodedSize(ReadOnlySpan<byte> data, string codec)
    {
        static InvalidDataException Invalid() => new("Invalid or truncated image header.");
        if (codec == "PNG")
        {
            if (data.Length < 33 || !data[..8].SequenceEqual(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 }) ||
                BinaryPrimitives.ReadUInt32BigEndian(data[8..]) != 13 || !data.Slice(12, 4).SequenceEqual("IHDR"u8)) throw Invalid();
            var width = BinaryPrimitives.ReadUInt32BigEndian(data[16..]);
            var height = BinaryPrimitives.ReadUInt32BigEndian(data[20..]);
            if (width > int.MaxValue || height > int.MaxValue) throw Invalid();
            var ended = false;
            for (var offset = 8; offset <= data.Length - 12;)
            {
                var length = BinaryPrimitives.ReadUInt32BigEndian(data[offset..]);
                if (length > data.Length - offset - 12) throw Invalid();
                var tag = data.Slice(offset + 4, 4);
                if (offset != 8 && tag.SequenceEqual("IHDR"u8)) throw Invalid();
                if (tag.SequenceEqual("IEND"u8))
                {
                    // IEND has a fixed CRC. Some decoders stop at the chunk header.
                    if (length != 0 || BinaryPrimitives.ReadUInt32BigEndian(data[(offset + 8)..]) != 0xAE426082)
                        throw Invalid();
                    ended = true;
                    break;
                }
                offset += (int)length + 12;
            }
            if (!ended) throw Invalid();
            return new((int)width, (int)height);
        }
        if (codec == "WEBP")
        {
            if (data.Length < 20 || !data[..4].SequenceEqual("RIFF"u8) || !data.Slice(8, 4).SequenceEqual("WEBP"u8)) throw Invalid();
            var fileSize = (ulong)BinaryPrimitives.ReadUInt32LittleEndian(data[4..]) + 8;
            if (fileSize > (ulong)data.Length || fileSize < 20) throw Invalid();
            for (var offset = 12; offset <= (long)fileSize - 8;)
            {
                var length = BinaryPrimitives.ReadUInt32LittleEndian(data[(offset + 4)..]);
                if (length > (long)fileSize - offset - 8) throw Invalid();
                var tag = data.Slice(offset, 4);
                var block = data.Slice(offset + 8, (int)length);
                if (tag.SequenceEqual("VP8X"u8) && length >= 10)
                    return new(1 + block[4] + (block[5] << 8) + (block[6] << 16), 1 + block[7] + (block[8] << 8) + (block[9] << 16));
                if (tag.SequenceEqual("VP8L"u8) && length >= 5 && block[0] == 0x2F)
                {
                    var bits = BinaryPrimitives.ReadUInt32LittleEndian(block[1..]);
                    return new(1 + (int)(bits & 0x3FFF), 1 + (int)((bits >> 14) & 0x3FFF));
                }
                if (tag.SequenceEqual("VP8 "u8) && length >= 10 && block.Slice(3, 3).SequenceEqual(new byte[] { 0x9D, 0x01, 0x2A }))
                    return new(BinaryPrimitives.ReadUInt16LittleEndian(block[6..]) & 0x3FFF, BinaryPrimitives.ReadUInt16LittleEndian(block[8..]) & 0x3FFF);
                offset += (int)length + 8 + (int)(length & 1);
            }
            throw Invalid();
        }
        throw new NotSupportedException(codec);
    }
}
