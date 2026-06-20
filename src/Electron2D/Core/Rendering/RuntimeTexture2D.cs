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

internal sealed class RuntimeTexture2D : Texture2D
{
    private readonly Func<int, int, bool>? isOpaque;

    public RuntimeTexture2D(
        int width,
        int height,
        bool hasAlpha,
        bool hasMipmaps = false,
        int mipmapCount = 0,
        Func<int, int, bool>? isOpaque = null)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(width);
        ArgumentOutOfRangeException.ThrowIfNegative(height);
        ArgumentOutOfRangeException.ThrowIfNegative(mipmapCount);

        Width = width;
        Height = height;
        Alpha = hasAlpha;
        TextureHasMipmaps = hasMipmaps;
        TextureMipmapCount = hasMipmaps ? Math.Max(1, mipmapCount) : 0;
        this.isOpaque = isOpaque;
    }

    public int Width { get; }

    public int Height { get; }

    public bool Alpha { get; }

    public bool TextureHasMipmaps { get; }

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
        return TextureHasMipmaps;
    }

    public override int GetMipmapCount()
    {
        return TextureMipmapCount;
    }

    public override bool IsPixelOpaque(int x, int y)
    {
        if (x < 0 || y < 0 || x >= Width || y >= Height)
        {
            return false;
        }

        if (!Alpha)
        {
            return true;
        }

        return isOpaque?.Invoke(x, y) == true;
    }
}
