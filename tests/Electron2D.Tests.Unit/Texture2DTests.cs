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
using Xunit;

namespace Electron2D.Tests.Unit;

public sealed class Texture2DTests
{
    [Fact]
    public void Texture2DReportsElectron2DSizeAlphaMipmapsAndOpacity()
    {
        var texture = new TestTexture2D(
            width: 16,
            height: 8,
            hasAlpha: true,
            hasMipmaps: true,
            mipmapCount: 3,
            isOpaque: (x, y) => x == 1 && y == 2);

        Assert.Equal(16, texture.GetWidth());
        Assert.Equal(8, texture.GetHeight());
        Assert.Equal(new Electron2D.Vector2(16f, 8f), texture.GetSize());
        Assert.True(texture.HasAlpha());
        Assert.True(texture.HasMipmaps());
        Assert.Equal(3, texture.GetMipmapCount());
        Assert.True(texture.IsPixelOpaque(1, 2));
        Assert.False(texture.IsPixelOpaque(2, 2));
    }

    [Fact]
    public void AtlasTextureUsesRegionSizeAndFallsBackToAtlasAxisWhenRegionAxisIsZero()
    {
        var atlas = new TestTexture2D(width: 64, height: 32, hasAlpha: true, hasMipmaps: true, mipmapCount: 2);
        var texture = new Electron2D.AtlasTexture
        {
            Atlas = atlas,
            Region = new Electron2D.Rect2(8f, 4f, 16f, 0f)
        };

        Assert.Equal(16, texture.GetWidth());
        Assert.Equal(32, texture.GetHeight());
        Assert.Equal(new Electron2D.Vector2(16f, 32f), texture.GetSize());
        Assert.True(texture.HasAlpha());
        Assert.True(texture.HasMipmaps());
        Assert.Equal(2, texture.GetMipmapCount());
    }

    [Fact]
    public void AtlasTextureMapsPixelOpacityThroughRegion()
    {
        var atlas = new TestTexture2D(
            width: 64,
            height: 32,
            hasAlpha: true,
            isOpaque: (x, y) => x == 9 && y == 5);
        var texture = new Electron2D.AtlasTexture
        {
            Atlas = atlas,
            Region = new Electron2D.Rect2(8f, 4f, 16f, 16f)
        };

        Assert.True(texture.IsPixelOpaque(1, 1));
        Assert.False(texture.IsPixelOpaque(2, 1));
        Assert.False(texture.IsPixelOpaque(-1, 1));
        Assert.False(texture.IsPixelOpaque(16, 1));
    }

    [Fact]
    public void AtlasTextureKeepsMarginAndFilterClipProperties()
    {
        var texture = new Electron2D.AtlasTexture
        {
            Margin = new Electron2D.Rect2(1f, 2f, 3f, 4f),
            FilterClip = true
        };

        Assert.Equal(new Electron2D.Rect2(1f, 2f, 3f, 4f), texture.Margin);
        Assert.True(texture.FilterClip);
    }

    [Fact]
    public void AtlasTextureWithoutAtlasReportsEmptyTexture()
    {
        var texture = new Electron2D.AtlasTexture();

        Assert.Equal(0, texture.GetWidth());
        Assert.Equal(0, texture.GetHeight());
        Assert.Equal(Electron2D.Vector2.Zero, texture.GetSize());
        Assert.False(texture.HasAlpha());
        Assert.False(texture.HasMipmaps());
        Assert.Equal(0, texture.GetMipmapCount());
        Assert.False(texture.IsPixelOpaque(0, 0));
    }

    private sealed class TestTexture2D : Electron2D.Texture2D
    {
        private readonly Func<int, int, bool> isOpaque;

        public TestTexture2D(
            int width,
            int height,
            bool hasAlpha,
            bool hasMipmaps = false,
            int mipmapCount = 0,
            Func<int, int, bool>? isOpaque = null)
        {
            Width = width;
            Height = height;
            Alpha = hasAlpha;
            HasTextureMipmaps = hasMipmaps;
            TextureMipmapCount = mipmapCount;
            this.isOpaque = isOpaque ?? ((x, y) => x >= 0 && y >= 0 && x < width && y < height && !hasAlpha);
        }

        public int Width { get; }

        public int Height { get; }

        public bool Alpha { get; }

        public bool HasTextureMipmaps { get; }

        public int TextureMipmapCount { get; }

        public override int GetWidth()
        {
            return Width;
        }

        public override int GetHeight()
        {
            return Height;
        }

        public override bool HasAlpha()
        {
            return Alpha;
        }

        public override bool HasMipmaps()
        {
            return HasTextureMipmaps;
        }

        public override int GetMipmapCount()
        {
            return TextureMipmapCount;
        }

        public override bool IsPixelOpaque(int x, int y)
        {
            return isOpaque(x, y);
        }
    }
}
