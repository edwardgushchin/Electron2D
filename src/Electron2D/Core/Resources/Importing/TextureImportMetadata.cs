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
namespace Electron2D;

internal enum TextureImageFormat
{
    Png,
    Jpeg
}

internal sealed class TextureImportMetadata
{
    public const string FormatName = "Electron2D.TextureImportMetadata";
    public const int CurrentVersion = 1;

    public TextureImportMetadata(
        string sourcePath,
        long uid,
        TextureImageFormat format,
        int width,
        int height,
        bool hasAlpha,
        bool hasMipmaps,
        int mipmapCount,
        TextureSamplingOptions sampling,
        IEnumerable<TextureAtlasRegionMetadata>? atlasRegions = null,
        IEnumerable<TexturePlatformVariant>? platformVariants = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourcePath);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(width);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(height);
        ArgumentOutOfRangeException.ThrowIfNegative(mipmapCount);

        if (uid <= 0 || uid == ResourceUid.InvalidId)
        {
            throw new ArgumentException("Texture import UID must be positive.", nameof(uid));
        }

        SourcePath = sourcePath;
        Uid = uid;
        Format = format;
        Width = width;
        Height = height;
        HasAlpha = hasAlpha;
        HasMipmaps = hasMipmaps;
        MipmapCount = hasMipmaps ? Math.Max(1, mipmapCount) : 0;
        Sampling = sampling;
        AtlasRegions = (atlasRegions ?? Array.Empty<TextureAtlasRegionMetadata>())
            .OrderBy(region => region.Name, StringComparer.Ordinal)
            .ToArray();
        PlatformVariants = (platformVariants ?? Array.Empty<TexturePlatformVariant>())
            .OrderBy(variant => variant.Name, StringComparer.Ordinal)
            .ToArray();
    }

    public string SourcePath { get; }

    public long Uid { get; }

    public string UidText => ResourceUid.IdToText(Uid);

    public TextureImageFormat Format { get; }

    public int Width { get; }

    public int Height { get; }

    public bool HasAlpha { get; }

    public bool HasMipmaps { get; }

    public int MipmapCount { get; }

    public TextureSamplingOptions Sampling { get; }

    public IReadOnlyList<TextureAtlasRegionMetadata> AtlasRegions { get; }

    public IReadOnlyList<TexturePlatformVariant> PlatformVariants { get; }

    public static int CalculateFullMipmapCount(int width, int height)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(width);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(height);

        var levels = 1;
        var size = Math.Max(width, height);
        while (size > 1)
        {
            size /= 2;
            levels++;
        }

        return levels;
    }
}

internal sealed class TextureAtlasRegionMetadata
{
    public TextureAtlasRegionMetadata(string name, Rect2 region, Rect2 margin, bool filterClip)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        Name = name;
        Region = region;
        Margin = margin;
        FilterClip = filterClip;
    }

    public string Name { get; }

    public Rect2 Region { get; }

    public Rect2 Margin { get; }

    public bool FilterClip { get; }
}

internal sealed class TexturePlatformVariant
{
    public TexturePlatformVariant(string name, string format, int quality)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(format);

        if (quality is < 0 or > 100)
        {
            throw new ArgumentOutOfRangeException(nameof(quality), "Texture platform variant quality must be in the 0..100 range.");
        }

        Name = name;
        Format = format;
        Quality = quality;
    }

    public string Name { get; }

    public string Format { get; }

    public int Quality { get; }
}
