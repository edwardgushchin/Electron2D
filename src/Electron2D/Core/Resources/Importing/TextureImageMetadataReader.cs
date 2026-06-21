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
using System.Text;

namespace Electron2D;

internal static class TextureImageMetadataReader
{
    private static readonly byte[] PngSignature = [0x89, 0x50, 0x4e, 0x47, 0x0d, 0x0a, 0x1a, 0x0a];

    public static TextureImageInfo Read(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        var bytes = File.ReadAllBytes(path);
        return IsPng(bytes)
            ? ReadPng(bytes)
            : ReadJpeg(bytes);
    }

    private static bool IsPng(ReadOnlySpan<byte> bytes)
    {
        return bytes.Length >= PngSignature.Length && bytes[..PngSignature.Length].SequenceEqual(PngSignature);
    }

    private static TextureImageInfo ReadPng(ReadOnlySpan<byte> bytes)
    {
        if (!IsPng(bytes))
        {
            throw new FormatException("Texture source is not a PNG image.");
        }

        var offset = PngSignature.Length;
        var width = 0;
        var height = 0;
        var hasAlpha = false;
        var sawHeader = false;

        while (offset + 12 <= bytes.Length)
        {
            var length = BinaryPrimitives.ReadInt32BigEndian(bytes.Slice(offset, 4));
            if (length < 0 || offset + 12 + length > bytes.Length)
            {
                throw new FormatException("PNG chunk length is malformed.");
            }

            var type = Encoding.ASCII.GetString(bytes.Slice(offset + 4, 4));
            var data = bytes.Slice(offset + 8, length);

            if (type == "IHDR")
            {
                if (length != 13)
                {
                    throw new FormatException("PNG IHDR chunk length is invalid.");
                }

                width = BinaryPrimitives.ReadInt32BigEndian(data[..4]);
                height = BinaryPrimitives.ReadInt32BigEndian(data.Slice(4, 4));
                if (width <= 0 || height <= 0)
                {
                    throw new FormatException("PNG dimensions must be positive.");
                }

                var colorType = data[9];
                hasAlpha = colorType is 4 or 6;
                sawHeader = true;
            }
            else if (type == "tRNS" && sawHeader)
            {
                hasAlpha = true;
            }
            else if (type == "IEND")
            {
                break;
            }

            offset += 12 + length;
        }

        if (!sawHeader)
        {
            throw new FormatException("PNG IHDR chunk was not found.");
        }

        return new TextureImageInfo(TextureImageFormat.Png, width, height, hasAlpha);
    }

    private static TextureImageInfo ReadJpeg(ReadOnlySpan<byte> bytes)
    {
        if (bytes.Length < 4 || bytes[0] != 0xff || bytes[1] != 0xd8)
        {
            throw new FormatException("Texture source is not a supported PNG or JPEG image.");
        }

        var offset = 2;
        while (offset + 4 <= bytes.Length)
        {
            if (bytes[offset] != 0xff)
            {
                offset++;
                continue;
            }

            while (offset < bytes.Length && bytes[offset] == 0xff)
            {
                offset++;
            }

            if (offset >= bytes.Length)
            {
                break;
            }

            var marker = bytes[offset++];
            if (marker == 0xd9)
            {
                break;
            }

            if (marker is 0x01 or >= 0xd0 and <= 0xd7)
            {
                continue;
            }

            if (offset + 2 > bytes.Length)
            {
                throw new FormatException("JPEG segment length is malformed.");
            }

            var length = BinaryPrimitives.ReadUInt16BigEndian(bytes.Slice(offset, 2));
            if (length < 2 || offset + length > bytes.Length)
            {
                throw new FormatException("JPEG segment length is malformed.");
            }

            var data = bytes.Slice(offset + 2, length - 2);
            if (IsStartOfFrame(marker))
            {
                if (data.Length < 6)
                {
                    throw new FormatException("JPEG start-of-frame segment is malformed.");
                }

                var height = BinaryPrimitives.ReadUInt16BigEndian(data.Slice(1, 2));
                var width = BinaryPrimitives.ReadUInt16BigEndian(data.Slice(3, 2));
                if (width == 0 || height == 0)
                {
                    throw new FormatException("JPEG dimensions must be positive.");
                }

                return new TextureImageInfo(TextureImageFormat.Jpeg, width, height, hasAlpha: false);
            }

            offset += length;
        }

        throw new FormatException("JPEG start-of-frame segment was not found.");
    }

    private static bool IsStartOfFrame(byte marker)
    {
        return marker is 0xc0 or 0xc1 or 0xc2 or 0xc3 or 0xc5 or 0xc6 or 0xc7 or
            0xc9 or 0xca or 0xcb or 0xcd or 0xce or 0xcf;
    }
}

internal readonly struct TextureImageInfo
{
    public TextureImageInfo(TextureImageFormat format, int width, int height, bool hasAlpha)
    {
        Format = format;
        Width = width;
        Height = height;
        HasAlpha = hasAlpha;
    }

    public TextureImageFormat Format { get; }

    public int Width { get; }

    public int Height { get; }

    public bool HasAlpha { get; }
}
