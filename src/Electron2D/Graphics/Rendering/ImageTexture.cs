/*
    Electron2D
    MIT License
    Copyright (c) 2025-2026 Eduard Gushchin <eduardgushchin@yandex.ru>
    SPDX-License-Identifier: MIT

    Permission is hereby granted, free of charge, to any person obtaining a copy
    of this software and associated documentation files (the "Software"), to deal
    in the Software without restriction, including without limitation the rights
    to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
    copies of the Software, and to permit persons to whom the Software is
    furnished to do so, subject to the following conditions:

    The above copyright notice and this permission notice shall be included in
    all copies or substantial portions of the Software.

    THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
    IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
    FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
    AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
    LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
    OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
    SOFTWARE.
*/
using System.Buffers.Binary;
using System.IO.Compression;
using System.Text;

namespace Electron2D;

/// <summary>
/// Provides an immutable image-backed 2D texture loaded from a PNG file.
/// </summary>
///
/// <remarks>
/// <para>
/// <see cref="ImageTexture"/> is a concrete <see cref="Texture2D"/> for project
/// assets that need real pixel data during preview runtime rendering,
/// screenshots, <see cref="Sprite2D"/>, <see cref="TextureRect"/>,
/// <see cref="TextureButton"/> and <see cref="CanvasItem.DrawTexture"/>.
/// </para>
/// <para>
/// Electron2D 0.1-preview supports non-interlaced PNG images with 8-bit RGB,
/// RGBA, grayscale-alpha data and 1-bit, 2-bit, 4-bit or 8-bit indexed palette
/// data. Unsupported or malformed files fail before a texture instance is
/// returned.
/// </para>
/// </remarks>
///
/// <threadsafety>
/// Instances are immutable after creation. Metadata and opacity queries are
/// safe to call from any thread.
/// </threadsafety>
///
/// <since>
/// This type is available since Electron2D 0.1-preview.
/// </since>
///
/// <seealso cref="Texture2D"/>
/// <seealso cref="AtlasTexture"/>
public sealed class ImageTexture : Texture2D
{
    private readonly byte[] rgbaPixels;

    private ImageTexture(int width, int height, bool hasAlpha, byte[] rgbaPixels)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(width, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(height, 1);
        ArgumentNullException.ThrowIfNull(rgbaPixels);
        if (rgbaPixels.Length != width * height * 4)
        {
            throw new ArgumentException("RGBA pixel buffer length does not match the image dimensions.", nameof(rgbaPixels));
        }

        Width = width;
        Height = height;
        Alpha = hasAlpha;
        this.rgbaPixels = rgbaPixels;
    }

    /// <summary>
    /// Loads an <see cref="ImageTexture"/> from a PNG file or project resource path.
    /// </summary>
    ///
    /// <param name="path">
    /// The absolute file path, relative file path, or <c>res://</c> resource path
    /// to a PNG image.
    /// </param>
    ///
    /// <returns>
    /// A new immutable <see cref="ImageTexture"/> containing the decoded image pixels.
    /// </returns>
    ///
    /// <remarks>
    /// <para>
    /// Relative paths are resolved by the current process in the normal .NET
    /// file-system manner. Paths beginning with <c>res://</c> are resolved by
    /// the current Electron2D runtime resource source, which may be a project
    /// directory during development or exported resource packages in a player
    /// build. The returned texture is detached from the source file; later file
    /// changes do not mutate the instance.
    /// </para>
    /// <para>
    /// The loader accepts non-interlaced PNG images with 8-bit RGB, RGBA and
    /// grayscale-alpha color data, plus indexed palette data with 1-bit, 2-bit,
    /// 4-bit or 8-bit palette indices. PNG transparency metadata is applied to
    /// <see cref="HasAlpha"/> and <see cref="IsPixelOpaque"/>.
    /// </para>
    /// </remarks>
    ///
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="path"/> is empty.
    /// </exception>
    ///
    /// <exception cref="FileNotFoundException">
    /// Thrown when <paramref name="path"/> does not exist.
    /// </exception>
    ///
    /// <exception cref="FormatException">
    /// Thrown when the PNG file is malformed or uses an unsupported PNG mode.
    /// </exception>
    ///
    /// <threadsafety>
    /// This method performs file IO and should be called before sharing the
    /// texture across runtime systems.
    /// </threadsafety>
    ///
    /// <since>
    /// This method is available since Electron2D 0.1-preview.
    /// </since>
    ///
    /// <seealso cref="Texture2D"/>
    public static ImageTexture LoadFromFile(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        var decoded = PngImageDecoder.Decode(ResourceFileSystem.ReadAllBytes(path));
        return new ImageTexture(decoded.Width, decoded.Height, decoded.HasAlpha, decoded.RgbaPixels);
    }

    /// <summary>
    /// Gets the decoded image width in pixels.
    /// </summary>
    ///
    /// <returns>
    /// The positive width of the decoded PNG image in pixels.
    /// </returns>
    ///
    /// <threadsafety>
    /// This method is safe to call from any thread because <see cref="ImageTexture"/>
    /// instances are immutable after loading.
    /// </threadsafety>
    ///
    /// <since>
    /// This method is available since Electron2D 0.1-preview.
    /// </since>
    public override int GetWidth()
    {
        return Width;
    }

    /// <summary>
    /// Gets the decoded image height in pixels.
    /// </summary>
    ///
    /// <returns>
    /// The positive height of the decoded PNG image in pixels.
    /// </returns>
    ///
    /// <threadsafety>
    /// This method is safe to call from any thread because <see cref="ImageTexture"/>
    /// instances are immutable after loading.
    /// </threadsafety>
    ///
    /// <since>
    /// This method is available since Electron2D 0.1-preview.
    /// </since>
    public override int GetHeight()
    {
        return Height;
    }

    /// <summary>
    /// Checks whether the decoded image contains transparent pixels or alpha metadata.
    /// </summary>
    ///
    /// <returns>
    /// <c>true</c> when the source PNG contained an alpha channel, transparency
    /// chunk or transparent palette entry; otherwise, <c>false</c>.
    /// </returns>
    ///
    /// <threadsafety>
    /// This method is safe to call from any thread because <see cref="ImageTexture"/>
    /// instances are immutable after loading.
    /// </threadsafety>
    ///
    /// <since>
    /// This method is available since Electron2D 0.1-preview.
    /// </since>
    public override bool HasAlpha()
    {
        return Alpha;
    }

    /// <summary>
    /// Checks whether a decoded image pixel is fully opaque.
    /// </summary>
    ///
    /// <param name="x">
    /// The pixel X coordinate in image space.
    /// </param>
    ///
    /// <param name="y">
    /// The pixel Y coordinate in image space.
    /// </param>
    ///
    /// <returns>
    /// <c>true</c> when the coordinate is inside the image and its alpha value is
    /// fully opaque; otherwise, <c>false</c>.
    /// </returns>
    ///
    /// <threadsafety>
    /// This method is safe to call from any thread because <see cref="ImageTexture"/>
    /// instances are immutable after loading.
    /// </threadsafety>
    ///
    /// <since>
    /// This method is available since Electron2D 0.1-preview.
    /// </since>
    public override bool IsPixelOpaque(int x, int y)
    {
        if (x < 0 || y < 0 || x >= Width || y >= Height)
        {
            return false;
        }

        return rgbaPixels[PixelOffset(x, y) + 3] == byte.MaxValue;
    }

    internal int Width { get; }

    internal int Height { get; }

    internal bool Alpha { get; }

    internal bool TryGetPixel(int x, int y, out RuntimeRgba color)
    {
        if (x < 0 || y < 0 || x >= Width || y >= Height)
        {
            color = default;
            return false;
        }

        var offset = PixelOffset(x, y);
        color = new RuntimeRgba(
            rgbaPixels[offset],
            rgbaPixels[offset + 1],
            rgbaPixels[offset + 2],
            rgbaPixels[offset + 3]);
        return true;
    }

    private int PixelOffset(int x, int y)
    {
        return ((y * Width) + x) * 4;
    }
}

internal static class PngImageDecoder
{
    private static readonly byte[] PngSignature = [0x89, 0x50, 0x4e, 0x47, 0x0d, 0x0a, 0x1a, 0x0a];

    public static DecodedImage Decode(byte[] bytes)
    {
        ArgumentNullException.ThrowIfNull(bytes);
        if (bytes.Length < PngSignature.Length || !bytes.AsSpan(0, PngSignature.Length).SequenceEqual(PngSignature))
        {
            throw new FormatException("ImageTexture supports PNG files only.");
        }

        var width = 0;
        var height = 0;
        var bitDepth = 0;
        var colorType = 0;
        byte[]? palette = null;
        byte[]? transparency = null;
        using var idat = new MemoryStream();

        var offset = PngSignature.Length;
        while (offset + 12 <= bytes.Length)
        {
            var length = BinaryPrimitives.ReadInt32BigEndian(bytes.AsSpan(offset, 4));
            if (length < 0 || offset + 12 + length > bytes.Length)
            {
                throw new FormatException("PNG chunk length is malformed.");
            }

            var type = Encoding.ASCII.GetString(bytes, offset + 4, 4);
            var dataOffset = offset + 8;
            var data = bytes.AsSpan(dataOffset, length);
            switch (type)
            {
                case "IHDR":
                    if (length != 13)
                    {
                        throw new FormatException("PNG IHDR chunk length is invalid.");
                    }

                    width = BinaryPrimitives.ReadInt32BigEndian(data[..4]);
                    height = BinaryPrimitives.ReadInt32BigEndian(data.Slice(4, 4));
                    bitDepth = data[8];
                    colorType = data[9];
                    if (width <= 0 || height <= 0)
                    {
                        throw new FormatException("PNG dimensions must be positive.");
                    }

                    if (!IsSupportedBitDepth(colorType, bitDepth))
                    {
                        throw new FormatException($"PNG bit depth '{bitDepth}' is not supported for color type '{colorType}'.");
                    }

                    if (data[10] != 0 || data[11] != 0 || data[12] != 0)
                    {
                        throw new FormatException("ImageTexture supports only standard non-interlaced PNG images.");
                    }

                    if (BytesPerPixel(colorType) == 0)
                    {
                        throw new FormatException($"PNG color type '{colorType}' is not supported.");
                    }

                    break;
                case "PLTE":
                    palette = data.ToArray();
                    break;
                case "tRNS":
                    transparency = data.ToArray();
                    break;
                case "IDAT":
                    idat.Write(bytes, dataOffset, length);
                    break;
                case "IEND":
                    offset = bytes.Length;
                    continue;
            }

            offset += 12 + length;
        }

        if (width <= 0 || height <= 0)
        {
            throw new FormatException("PNG IHDR chunk was not found.");
        }

        var filterBytesPerPixel = BytesPerPixel(colorType);
        var scanlineStride = ScanlineStride(width, colorType, bitDepth);
        var filtered = Inflate(idat.ToArray());
        var expectedLength = checked(height * (scanlineStride + 1));
        if (filtered.Length != expectedLength)
        {
            throw new FormatException("PNG image data length does not match the image dimensions.");
        }

        var unfiltered = Unfilter(filtered, scanlineStride, height, filterBytesPerPixel);
        var rgba = ConvertToRgba(unfiltered, width, height, colorType, bitDepth, palette, transparency, out var hasTransparentPixels);
        var hasAlpha = colorType is 4 or 6 || transparency is not null || hasTransparentPixels;
        return new DecodedImage(width, height, hasAlpha, rgba);
    }

    private static byte[] Inflate(byte[] compressed)
    {
        using var input = new MemoryStream(compressed);
        using var zlib = new ZLibStream(input, CompressionMode.Decompress);
        using var output = new MemoryStream();
        zlib.CopyTo(output);
        return output.ToArray();
    }

    private static byte[] Unfilter(byte[] filtered, int stride, int height, int bytesPerPixel)
    {
        var result = new byte[height * stride];
        for (var row = 0; row < height; row++)
        {
            var filter = filtered[row * (stride + 1)];
            var sourceOffset = (row * (stride + 1)) + 1;
            var targetOffset = row * stride;
            var previousOffset = (row - 1) * stride;

            for (var column = 0; column < stride; column++)
            {
                var raw = filtered[sourceOffset + column];
                var left = column >= bytesPerPixel ? result[targetOffset + column - bytesPerPixel] : 0;
                var up = row > 0 ? result[previousOffset + column] : 0;
                var upLeft = row > 0 && column >= bytesPerPixel ? result[previousOffset + column - bytesPerPixel] : 0;
                result[targetOffset + column] = filter switch
                {
                    0 => raw,
                    1 => unchecked((byte)(raw + left)),
                    2 => unchecked((byte)(raw + up)),
                    3 => unchecked((byte)(raw + ((left + up) / 2))),
                    4 => unchecked((byte)(raw + Paeth(left, up, upLeft))),
                    _ => throw new FormatException($"PNG filter type '{filter}' is not supported.")
                };
            }
        }

        return result;
    }

    private static byte Paeth(int left, int up, int upLeft)
    {
        var estimate = left + up - upLeft;
        var leftDistance = Math.Abs(estimate - left);
        var upDistance = Math.Abs(estimate - up);
        var upLeftDistance = Math.Abs(estimate - upLeft);
        if (leftDistance <= upDistance && leftDistance <= upLeftDistance)
        {
            return (byte)left;
        }

        return upDistance <= upLeftDistance ? (byte)up : (byte)upLeft;
    }

    private static byte[] ConvertToRgba(
        byte[] pixels,
        int width,
        int height,
        int colorType,
        int bitDepth,
        byte[]? palette,
        byte[]? transparency,
        out bool hasTransparentPixels)
    {
        var rgba = new byte[width * height * 4];
        hasTransparentPixels = false;
        var pixelCount = width * height;
        var source = 0;
        var target = 0;
        for (var index = 0; index < pixelCount; index++)
        {
            RuntimeRgba color = colorType switch
            {
                0 => FromGrayscale(pixels[source++], transparency),
                2 => FromRgb(pixels, ref source, transparency),
                3 => FromPalette(ReadPaletteIndex(pixels, width, index, bitDepth), palette, transparency),
                4 => FromGrayscaleAlpha(pixels, ref source),
                6 => FromRgba(pixels, ref source),
                _ => throw new FormatException($"PNG color type '{colorType}' is not supported.")
            };

            rgba[target++] = color.R;
            rgba[target++] = color.G;
            rgba[target++] = color.B;
            rgba[target++] = color.A;
            hasTransparentPixels = hasTransparentPixels || color.A < byte.MaxValue;
        }

        return rgba;
    }

    private static byte ReadPaletteIndex(byte[] pixels, int width, int pixelIndex, int bitDepth)
    {
        if (bitDepth == 8)
        {
            return pixels[pixelIndex];
        }

        var pixelsPerByte = 8 / bitDepth;
        var x = pixelIndex % width;
        var y = pixelIndex / width;
        var stride = ScanlineStride(width, colorType: 3, bitDepth);
        var byteIndex = (y * stride) + (x / pixelsPerByte);
        var packedPixelOffset = x % pixelsPerByte;
        var shift = (pixelsPerByte - packedPixelOffset - 1) * bitDepth;
        var mask = (1 << bitDepth) - 1;
        return (byte)((pixels[byteIndex] >> shift) & mask);
    }

    private static RuntimeRgba FromGrayscale(byte gray, byte[]? transparency)
    {
        var alpha = transparency is { Length: >= 2 } &&
            gray == BinaryPrimitives.ReadUInt16BigEndian(transparency.AsSpan(0, 2))
            ? (byte)0
            : byte.MaxValue;
        return new RuntimeRgba(gray, gray, gray, alpha);
    }

    private static RuntimeRgba FromRgb(byte[] pixels, ref int source, byte[]? transparency)
    {
        var red = pixels[source++];
        var green = pixels[source++];
        var blue = pixels[source++];
        var alpha = byte.MaxValue;
        if (transparency is { Length: >= 6 } &&
            red == BinaryPrimitives.ReadUInt16BigEndian(transparency.AsSpan(0, 2)) &&
            green == BinaryPrimitives.ReadUInt16BigEndian(transparency.AsSpan(2, 2)) &&
            blue == BinaryPrimitives.ReadUInt16BigEndian(transparency.AsSpan(4, 2)))
        {
            alpha = 0;
        }

        return new RuntimeRgba(red, green, blue, alpha);
    }

    private static RuntimeRgba FromPalette(byte paletteIndex, byte[]? palette, byte[]? transparency)
    {
        if (palette is null || palette.Length == 0 || paletteIndex * 3 + 2 >= palette.Length)
        {
            throw new FormatException("PNG indexed image is missing a valid palette.");
        }

        var paletteOffset = paletteIndex * 3;
        var alpha = transparency is not null && paletteIndex < transparency.Length
            ? transparency[paletteIndex]
            : byte.MaxValue;
        return new RuntimeRgba(
            palette[paletteOffset],
            palette[paletteOffset + 1],
            palette[paletteOffset + 2],
            alpha);
    }

    private static RuntimeRgba FromGrayscaleAlpha(byte[] pixels, ref int source)
    {
        var gray = pixels[source++];
        var alpha = pixels[source++];
        return new RuntimeRgba(gray, gray, gray, alpha);
    }

    private static RuntimeRgba FromRgba(byte[] pixels, ref int source)
    {
        return new RuntimeRgba(
            pixels[source++],
            pixels[source++],
            pixels[source++],
            pixels[source++]);
    }

    private static int BytesPerPixel(int colorType)
    {
        return colorType switch
        {
            0 => 1,
            2 => 3,
            3 => 1,
            4 => 2,
            6 => 4,
            _ => 0
        };
    }

    private static int ScanlineStride(int width, int colorType, int bitDepth)
    {
        return colorType == 3
            ? checked(((width * bitDepth) + 7) / 8)
            : checked(width * BytesPerPixel(colorType));
    }

    private static bool IsSupportedBitDepth(int colorType, int bitDepth)
    {
        return colorType switch
        {
            0 or 2 or 4 or 6 => bitDepth == 8,
            3 => bitDepth is 1 or 2 or 4 or 8,
            _ => false
        };
    }

    internal sealed record DecodedImage(int Width, int Height, bool HasAlpha, byte[] RgbaPixels);
}
