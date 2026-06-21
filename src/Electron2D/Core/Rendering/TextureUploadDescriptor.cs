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

internal readonly struct TextureUploadDescriptor
{
    public TextureUploadDescriptor(
        int width,
        int height,
        bool hasAlpha,
        bool hasMipmaps,
        int mipmapCount,
        Rect2 sourceRegion,
        Rect2 margin,
        bool filterClip,
        TextureSamplingOptions sampling,
        TextureResourceUsage usage = TextureResourceUsage.Sampled)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(width);
        ArgumentOutOfRangeException.ThrowIfNegative(height);
        ArgumentOutOfRangeException.ThrowIfNegative(mipmapCount);

        Width = width;
        Height = height;
        HasAlpha = hasAlpha;
        HasMipmaps = hasMipmaps;
        MipmapCount = mipmapCount;
        SourceRegion = sourceRegion;
        Margin = margin;
        FilterClip = filterClip;
        Sampling = sampling;
        Usage = usage;
    }

    public int Width { get; }

    public int Height { get; }

    public bool HasAlpha { get; }

    public bool HasMipmaps { get; }

    public int MipmapCount { get; }

    public Rect2 SourceRegion { get; }

    public Rect2 Margin { get; }

    public bool FilterClip { get; }

    public TextureSamplingOptions Sampling { get; }

    public TextureResourceUsage Usage { get; }

    public static TextureUploadDescriptor FromTexture(Texture2D texture, TextureSamplingOptions sampling)
    {
        ArgumentNullException.ThrowIfNull(texture);

        var width = texture.GetWidth();
        var height = texture.GetHeight();
        var sourceRegion = new Rect2(0f, 0f, width, height);
        var margin = default(Rect2);
        var filterClip = false;

        if (texture is AtlasTexture atlas)
        {
            sourceRegion = atlas.GetSourceRegion();
            margin = atlas.Margin;
            filterClip = atlas.FilterClip;
        }

        return new TextureUploadDescriptor(
            width,
            height,
            texture.HasAlpha(),
            texture.HasMipmaps(),
            texture.GetMipmapCount(),
            sourceRegion,
            margin,
            filterClip,
            sampling,
            TextureResourceUsage.Sampled);
    }

    public static TextureUploadDescriptor ForRenderTarget(
        Vector2I size,
        bool hasAlpha,
        TextureSamplingOptions sampling)
    {
        return new TextureUploadDescriptor(
            size.X,
            size.Y,
            hasAlpha,
            hasMipmaps: false,
            mipmapCount: 0,
            new Rect2(0f, 0f, size.X, size.Y),
            default,
            filterClip: false,
            sampling,
            TextureResourceUsage.RenderTarget);
    }
}
