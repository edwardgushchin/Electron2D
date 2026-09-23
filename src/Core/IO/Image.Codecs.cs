using IOPath = System.IO.Path;
using System.Buffers.Binary;
using System.Runtime.InteropServices;
using SDL3;

namespace Electron2D;

public sealed partial class Image
{
    private const int MaximumEncodedBytes = 64 * 1024 * 1024;

    /// <summary>Replaces this image with decoded pixels from a file.</summary>
    /// <param name="path">An operating-system, res:// or user:// path to PNG, JPEG, WebP, BMP, TGA or SVG data.</param>
    /// <remarks>Input is bounded to 64 MiB. Decoding produces a copied RGBA8 base image without mipmaps.
    /// A failure preserves the previous image. Changed is emitted after a successful replacement.
    /// Formats requiring further codec integration are rejected explicitly.</remarks>
    /// <exception cref="InvalidDataException">The encoded image is malformed or exceeds supported limits.</exception>
    /// <exception cref="NotSupportedException">The filename extension is not integrated.</exception>
    /// <exception cref="IOException">The file cannot be read or changes while reading.</exception>
    /// <exception cref="ObjectDisposedException">The image is disposed.</exception>
    public void Load(string path)
    {
        ThrowIfDisposed();
        var codec = CodecFromPath(path);
        using var file = FileAccess.Open(path, FileAccessMode.Read);
        var length = file.Length;
        if (length is <= 0 or > MaximumEncodedBytes) throw new InvalidDataException("Encoded images must contain between 1 byte and 64 MiB.");
        var bytes = file.ReadBytes((int)length);
        if (bytes.Length != length || file.Length != length) throw new IOException("The encoded image changed while it was read.");
        DecodeAndCommit(bytes, codec);
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
    /// <remarks>Produces RGBA8 without mipmaps. Failure preserves pixels; success emits Changed.</remarks>
    /// <exception cref="InvalidDataException">Data is malformed or exceeds image limits.</exception>
    /// <exception cref="ObjectDisposedException">The image is disposed.</exception>
    public void LoadPNGFromBuffer(ReadOnlySpan<byte> buffer) => DecodeAndCommit(buffer, "PNG");

    /// <summary>Decodes a JPEG buffer and replaces the image atomically.</summary>
    /// <param name="buffer">JPEG data, copied before validation and decoding, at most 64 MiB.</param>
    /// <remarks>Produces RGBA8 with opaque alpha and no mipmaps. Failure preserves pixels; success emits Changed.</remarks>
    /// <exception cref="InvalidDataException">Data is malformed or exceeds image limits.</exception>
    /// <exception cref="ObjectDisposedException">The image is disposed.</exception>
    public void LoadJPGFromBuffer(ReadOnlySpan<byte> buffer) => DecodeAndCommit(buffer, "JPG");

    /// <summary>Decodes a WebP buffer and replaces the image atomically.</summary>
    /// <param name="buffer">WebP data, copied before validation and decoding, at most 64 MiB.</param>
    /// <remarks>Produces RGBA8 without mipmaps. Animated input supplies its first frame. Failure preserves pixels; success emits Changed.</remarks>
    /// <exception cref="InvalidDataException">Data is malformed or exceeds image limits.</exception>
    /// <exception cref="ObjectDisposedException">The image is disposed.</exception>
    public void LoadWebPFromBuffer(ReadOnlySpan<byte> buffer) => DecodeAndCommit(buffer, "WEBP");

    /// <summary>Decodes a BMP buffer and replaces the image atomically.</summary>
    /// <param name="buffer">BMP data, copied before validation and decoding, at most 64 MiB.</param>
    /// <remarks>Produces RGBA8 without mipmaps. Failure preserves pixels; success emits Changed.</remarks>
    /// <exception cref="InvalidDataException">Data is malformed or exceeds image limits.</exception>
    /// <exception cref="ObjectDisposedException">The image is disposed.</exception>
    public void LoadBMPFromBuffer(ReadOnlySpan<byte> buffer) => DecodeAndCommit(buffer, "BMP");

    /// <summary>Decodes a TGA buffer and replaces the image atomically.</summary>
    /// <param name="buffer">TGA data, copied before validation and decoding, at most 64 MiB.</param>
    /// <remarks>Produces RGBA8 without mipmaps. Failure preserves pixels; success emits Changed.</remarks>
    /// <exception cref="InvalidDataException">Data is malformed or exceeds image limits.</exception>
    /// <exception cref="ObjectDisposedException">The image is disposed.</exception>
    public void LoadTGAFromBuffer(ReadOnlySpan<byte> buffer) => DecodeAndCommit(buffer, "TGA");

    /// <summary>Rasterizes an uncompressed UTF-8 SVG buffer and replaces the image atomically.</summary>
    /// <param name="buffer">SVG data, copied before validation and decoding, at most 64 MiB.</param>
    /// <param name="scale">Finite positive multiplier for the SVG's intrinsic dimensions.</param>
    /// <remarks>Produces an RGBA8 base image without mipmaps. The document must have finite intrinsic dimensions or a viewBox; external XML entities are rejected. Failure preserves the previous image.</remarks>
    /// <exception cref="ArgumentOutOfRangeException">Scale is not finite and positive.</exception>
    /// <exception cref="InvalidDataException">The document is malformed or exceeds image limits.</exception>
    /// <exception cref="ObjectDisposedException">The image is disposed.</exception>
    public void LoadSVGFromBuffer(ReadOnlySpan<byte> buffer, float scale = 1f) => DecodeAndCommit(buffer, "SVG", scale);

    /// <summary>Encodes the base image as a PNG byte array.</summary>
    /// <returns>Caller-owned encoded bytes.</returns>
    /// <remarks>Uses a stable snapshot converted to RGBA8; mipmaps are not encoded. Does not mutate this image.</remarks>
    /// <exception cref="InvalidOperationException">The image is empty.</exception>
    /// <exception cref="NotSupportedException">The image is compressed.</exception>
    /// <exception cref="IOException">Encoding fails or its output exceeds 64 MiB.</exception>
    /// <exception cref="ObjectDisposedException">The image is disposed.</exception>
    public byte[] SavePNGToBuffer() => Encode("PNG", 100);

    /// <summary>Encodes the base image as JPEG bytes, discarding alpha.</summary>
    /// <param name="quality">Finite quality from 0.01 to 1.0, rounded to a percentage.</param>
    /// <returns>Caller-owned encoded bytes.</returns>
    /// <remarks>JPEG remains lossy at maximum quality. Pixels are converted to RGB8; mipmaps are not encoded.</remarks>
    /// <exception cref="ArgumentOutOfRangeException">Quality is outside the accepted range or is not finite.</exception>
    /// <exception cref="InvalidOperationException">The image is empty.</exception>
    /// <exception cref="NotSupportedException">The image is compressed.</exception>
    /// <exception cref="IOException">Encoding fails or its output exceeds 64 MiB.</exception>
    /// <exception cref="ObjectDisposedException">The image is disposed.</exception>
    public byte[] SaveJPGToBuffer(float quality = 0.75f)
    {
        ThrowIfDisposed();
        if (!float.IsFinite(quality) || quality is < 0.01f or > 1) throw new ArgumentOutOfRangeException(nameof(quality));
        return Encode("JPG", (int)Mathf.Round(quality * 100));
    }

    /// <summary>Atomically saves the base image to a PNG file.</summary>
    /// <param name="path">An operating-system, res:// or user:// destination path.</param>
    /// <remarks>Uses SavePNGToBuffer, then atomically replaces the destination. Parent directories must exist.</remarks>
    /// <exception cref="IOException">Encoding or saving fails; an existing destination is preserved before replacement.</exception>
    /// <exception cref="InvalidOperationException">The image is empty.</exception>
    /// <exception cref="NotSupportedException">The image is compressed.</exception>
    /// <exception cref="ObjectDisposedException">The image is disposed.</exception>
    public void SavePNG(string path) => SaveEncodedFile(path, SavePNGToBuffer());

    /// <summary>Atomically saves the base image as JPEG, discarding alpha.</summary>
    /// <param name="path">An operating-system, res:// or user:// destination path.</param>
    /// <param name="quality">Finite quality from 0.01 to 1.0.</param>
    /// <remarks>Uses SaveJPGToBuffer, then atomically replaces the destination. Parent directories must exist.</remarks>
    /// <exception cref="ArgumentOutOfRangeException">Quality is outside the accepted range or is not finite.</exception>
    /// <exception cref="IOException">Encoding or saving fails; an existing destination is preserved before replacement.</exception>
    /// <exception cref="InvalidOperationException">The image is empty.</exception>
    /// <exception cref="NotSupportedException">The image is compressed.</exception>
    /// <exception cref="ObjectDisposedException">The image is disposed.</exception>
    public void SaveJPG(string path, float quality = 0.75f) => SaveEncodedFile(path, SaveJPGToBuffer(quality));

    private static void SaveEncodedFile(string path, byte[] data)
    {
        ArgumentException.ThrowIfNullOrEmpty(path);
        AtomicFile.Write(ProjectSettings.Instance.GlobalizePath(path), data);
    }

    private static string CodecFromPath(string path)
    {
        ArgumentException.ThrowIfNullOrEmpty(path);
        return IOPath.GetExtension(path).ToLowerInvariant() switch
        {
            ".png" => "PNG",
            ".jpg" or ".jpeg" => "JPG",
            ".webp" => "WEBP",
            ".bmp" => "BMP",
            ".tga" => "TGA",
            ".svg" => "SVG",
            _ => throw new NotSupportedException("This image loader accepts PNG, JPEG, WebP, BMP, TGA and SVG files.")
        };
    }

    private unsafe void DecodeAndCommit(ReadOnlySpan<byte> buffer, string codec, float scale = 1f)
    {
        ThrowIfDisposed();
        if (buffer.IsEmpty || buffer.Length > MaximumEncodedBytes) throw new InvalidDataException("Encoded images must contain between 1 byte and 64 MiB.");
        var bytes = buffer.ToArray();
        var size = codec == "SVG" ? SVGSize(bytes, scale) : EncodedSize(bytes, codec);
        try { ValidateDimensions(size.X, size.Y); }
        catch (ArgumentOutOfRangeException e) { throw new InvalidDataException("Encoded dimensions exceed the image limits.", e); }
        State decoded;
        fixed (byte* pointer = bytes)
        {
            var stream = SDL.IOFromConstMem((nint)pointer, (nuint)bytes.Length);
            if (stream == 0) throw new IOException("Could not create an image stream: " + SDL.GetError());
            try
            {
                var surface = codec switch
                {
                    // SDL_image 3.4.6 corrupts grayscale and RGB16 PNG colors. SDL's PNG
                    // decoder already ships with the engine and preserves the 8-bit result.
                    "PNG" => SDL.LoadPNGIO(stream, false),
                    "JPG" => SDL3.Image.LoadJPGIO(stream),
                    "WEBP" => SDL3.Image.LoadWEBPIO(stream),
                    "BMP" => SDL3.Image.LoadBMPIO(stream),
                    "TGA" => SDL3.Image.LoadTGAIO(stream),
                    "SVG" => SDL3.Image.LoadSizedSVGIO(stream, size.X, size.Y),
                    _ => throw new NotSupportedException(codec)
                };
                if (surface == 0) throw new InvalidDataException($"Cannot decode {codec}: {SDL.GetError()}");
                try
                {
                    var original = Marshal.PtrToStructure<SDL.Surface>(surface);
                    if (original.Width != size.X || original.Height != size.Y)
                        throw new InvalidDataException("Decoded dimensions disagree with the encoded header.");
                    var converted = SDL.ConvertSurface(surface, RGBASurfaceFormat);
                    if (converted == 0) throw new IOException("Could not convert decoded pixels: " + SDL.GetError());
                    try
                    {
                        if (!SDL.LockSurface(converted)) throw new IOException("Could not lock decoded pixels: " + SDL.GetError());
                        try
                        {
                            var data = Marshal.PtrToStructure<SDL.Surface>(converted);
                            var pixels = new byte[GetRequiredDataSize(size.X, size.Y, Format.Rgba8, false)];
                            var rowBytes = checked(size.X * 4);
                            if (data.Pixels == 0 || data.Pitch < rowBytes) throw new InvalidDataException("Invalid decoded pixel layout.");
                            for (var y = 0; y < size.Y; y++) Marshal.Copy(data.Pixels + y * data.Pitch, pixels, y * rowBytes, rowBytes);
                            decoded = new(size.X, size.Y, Format.Rgba8, false, pixels);
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

    private unsafe byte[] Encode(string codec, int quality)
    {
        var state = Snapshot();
        if (state.Width == 0) throw new InvalidOperationException("An empty image cannot be encoded.");
        if (IsCompressedFormat(state.Format)) throw new NotSupportedException("Decompress image pixels before encoding.");
        state = ConvertState(state, codec == "JPG" ? Format.Rgb8 : Format.Rgba8);
        fixed (byte* pointer = state.Data)
        {
            var pixelBytes = codec == "JPG" ? 3 : 4;
            var surface = SDL.CreateSurfaceFrom(state.Width, state.Height, codec == "JPG" ? SDL.PixelFormat.RGB24 : RGBASurfaceFormat,
                (nint)pointer, checked(state.Width * pixelBytes));
            if (surface == 0) throw new IOException("Could not prepare image encoding: " + SDL.GetError());
            try
            {
                var stream = SDL.IOFromDynamicMem();
                if (stream == 0) throw new IOException("Could not create encoded image output: " + SDL.GetError());
                try
                {
                    var success = codec == "JPG" ? SDL3.Image.SaveJPGIO(surface, stream, false, quality) : SDL3.Image.SavePNGIO(surface, stream, false);
                    if (!success) throw new IOException($"Cannot encode {codec}: {SDL.GetError()}");
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

    private static Vector2I EncodedSize(ReadOnlySpan<byte> data, string codec)
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
                if (tag.SequenceEqual("IEND"u8)) { if (length != 0) throw Invalid(); ended = true; break; }
                offset += (int)length + 12;
            }
            if (!ended) throw Invalid();
            return new((int)width, (int)height);
        }
        if (codec == "JPG")
        {
            if (data.Length < 4 || data[0] != 255 || data[1] != 216) throw Invalid();
            for (var offset = 2; offset < data.Length;)
            {
                if (data[offset++] != 255) throw Invalid();
                while (offset < data.Length && data[offset] == 255) offset++;
                if (offset >= data.Length) throw Invalid();
                var marker = data[offset++];
                if (marker is 0xD8 or 0x01 or >= 0xD0 and <= 0xD7) continue;
                if (marker is 0xD9 or 0xDA || offset > data.Length - 2) throw Invalid();
                var length = BinaryPrimitives.ReadUInt16BigEndian(data[offset..]);
                if (length < 2 || length > data.Length - offset) throw Invalid();
                if (marker is >= 0xC0 and <= 0xCF and not (0xC4 or 0xC8 or 0xCC))
                {
                    if (length < 8) throw Invalid();
                    return new(BinaryPrimitives.ReadUInt16BigEndian(data[(offset + 5)..]), BinaryPrimitives.ReadUInt16BigEndian(data[(offset + 3)..]));
                }
                offset += length;
            }
            throw Invalid();
        }
        if (codec == "BMP")
        {
            if (data.Length < 26 || !data[..2].SequenceEqual("BM"u8)) throw Invalid();
            var header = BinaryPrimitives.ReadUInt32LittleEndian(data[14..]);
            if (header == 12) return new(BinaryPrimitives.ReadUInt16LittleEndian(data[18..]), BinaryPrimitives.ReadUInt16LittleEndian(data[20..]));
            if (header < 40 || header > data.Length - 14) throw Invalid();
            var width = BinaryPrimitives.ReadInt32LittleEndian(data[18..]);
            var height = BinaryPrimitives.ReadInt32LittleEndian(data[22..]);
            if (height == int.MinValue) throw Invalid();
            return new(width, Math.Abs(height));
        }
        if (codec == "TGA")
        {
            if (data.Length < 18 || data[2] is not (1 or 2 or 3 or 9 or 10 or 11)) throw Invalid();
            return new(BinaryPrimitives.ReadUInt16LittleEndian(data[12..]), BinaryPrimitives.ReadUInt16LittleEndian(data[14..]));
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
