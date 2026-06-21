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

internal static class TextureImportResourceFactory
{
    public static Texture2D CreateTexture(TextureImportMetadata metadata)
    {
        ArgumentNullException.ThrowIfNull(metadata);
        return new RuntimeTexture2D(
            metadata.Width,
            metadata.Height,
            metadata.HasAlpha,
            metadata.HasMipmaps,
            metadata.MipmapCount);
    }

    public static IReadOnlyList<AtlasTexture> CreateAtlasTextures(TextureImportMetadata metadata, Texture2D atlas)
    {
        ArgumentNullException.ThrowIfNull(metadata);
        ArgumentNullException.ThrowIfNull(atlas);

        return metadata.AtlasRegions
            .Select(region => new AtlasTexture
            {
                Atlas = atlas,
                Region = region.Region,
                Margin = region.Margin,
                FilterClip = region.FilterClip
            })
            .ToArray();
    }
}
